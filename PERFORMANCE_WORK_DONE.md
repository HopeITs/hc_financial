# What was done, and what it measured

Everything on the `Faster` branch, from the first performance commit to the
current one. Written so the work can be picked up without this conversation.

Companion documents:

- `USER_VISIBLE_CHANGES.md` — what a person using the app will actually notice
- `PERFORMANCE_PLAN_NEXT.md` — what is left, and what was deliberately not done
- `db/changes/README.md` — the two server-side objects, old and new

---

## The one fact that shaped every decision

`SELECT 1` against the online server costs **206–406 ms**.

The `action` table holds 28,409 rows. The server executes almost anything over it
faster than the trip to reach it. So for this application the thing that costs
money is **the number of round trips and the number of bytes**, not the
cleverness of the SQL.

That single measurement explains why most of what worked here was removing
round trips and shrinking payloads, and why several "obvious" SQL optimisations
measured as nothing.

There is exactly one place where that rule does not hold: the server-side views
and procedures, where the server really was the bottleneck. Those are section 4.

---

## 1. The connection layer

### P-01 — stop destroying the connection pool after every query

`96726a2`

`CloseAndClearConnection()` called `ClearAllPoolsAsync()`, which wipes every
pool in the process. All 202 call sites therefore paid a full TCP handshake plus
MySQL authentication **before every single query**.

```csharp
MyConn.Close();
MyConn.ClearAllPoolsAsync();   // removed
```

| | before | after |
|---|---|---|
| median time to open a connection | 1,982 ms | **289 ms** |
| a 37-operation session, database time | 101 s | **39.8 s** |

This is the largest single change in the project. It also helps a slow link more
than a fast one, because what it removes is round trips, and round trips are what
a weak connection charges most for.

**It also introduced the one regression in this work.** Before it, every query
built a fresh connection, so a stale pooled connection was impossible. Now the
pool can hand back a connection the server has already closed. See section 6.

### P-15 — compress the wire, cap the connect timeout

`1e8acf3`, revised in `9d0886f`

**Compression.** `UseCompression=True` on the online connection string only; over
localhost there is no link to saturate. The MySQL protocol zlib-compresses result
sets, so the data is byte-for-byte identical and only the byte count changes.

Measured with the server's `Bytes_sent` counter on `SELECT * FROM action LIMIT 200`:
**33,188 bytes → 6,801**, a 79% reduction, and timings stopped swinging
(1,280–4,188 ms became a steady 754–777 ms).

What that means on a 1 Mbit/s link:

| screen | uncompressed | compressed |
|---|---|---|
| till grid, all actions | 29.6 s | **4.0 s** |
| all payments | 11.7 s | **2.9 s** |
| cash flow, largest account | 4.5 s | **1.3 s** |
| all beneficiaries | 1.4 s | **0.6 s** |

**Connect timeout.** It was 21600 seconds — six hours. `Connect Timeout` governs
opening a connection, not query duration; that is `CommandTimeout`, unchanged at
120 s, so long Excel imports are unaffected either way.

Six hours meant a stalled handshake could block the UI indefinitely, and one
logged session froze for **412 seconds**. It went to 60 s, then to **180 s** after
a review found 60 was measured on the wrong machine:

| | |
|---|---|
| bytes the handshake moves | **5,389** — 43 ms even at 1 Mbit/s |
| round trips it costs | about **16** |
| here, at a 128 ms round trip | 2,042 ms |

Bandwidth does not limit the handshake; latency does. Scaling by round trip:
500 ms gives 8 s, 1 s gives 16 s, 3 s gives 48 s. **180 s covers a round trip of
11 seconds**, worse than any real network, and still leaves room for
retransmissions on a lossy link.

Higher would buy nothing. `Open()` blocks the UI thread, so the number is also
how long the window freezes when the server cannot be reached, and a failure past
three minutes means unreachable rather than slow.

---

## 2. Round trips

### P-15b — load the till screen in one round trip instead of three

`8777a6b`

`Till_Window` issued three separate queries on open: the grid, the unapproved
list, and the totals. They now go as one batch filled into a `DataSet` through
`MySqlDataAdapter.Fill`, later extended to five tables when the period picker and
the cash balances joined it.

The SQL is unchanged — the builders were factored out so the single-query methods
and the batched one share one copy of each query rather than two that can drift.

### P-02 — build account tabs on demand

`b70eb91`

`Account_Window` created every tab's contents up front, so opening an account
built windows the user might never look at. Tabs now materialise on first
selection.

```csharp
private readonly Dictionary<TabItem, Func<Window>> _pendingTabs;
private void SetLazyContent(TabItem tab, Func<Window> factory) => _pendingTabs[tab] = factory;
```

Two things about it are easy to get wrong and are worth knowing:

- `SetLazyContent(...)` must be called **before** `tabControl1.Items.Add(...)`.
- `SelectionChanged` bubbles from child grids, so the handler starts with
  `if (!ReferenceEquals(e.Source, tabControl1)) return;`

### P-03 — reload a hidden till when it is looked at, not when it is told

`e2b56d9`, corrected by `f822573`

A static refresh event made every open till reload whenever any till changed,
including tills nobody was looking at. A hidden window now records that it is
stale and reloads when it next becomes visible.

`f822573` fixes what the first version broke: linked tills stopped refreshing
after an insert and after an approval.

---

## 3. The interface

### P-05 — debounce the grid search

`a9e8b65`

`SearchDataGrid` rebuilt the whole grid on every keystroke, including a full
`DataTable.Copy()`. It now waits 250 ms after typing stops, drops the copy, and
scans with `IndexOf` and `OrdinalIgnoreCase`.

All 23 calling windows are unchanged — the debounce state hangs off a
`ConditionalWeakTable<SfDataGrid, Pending>` holding a `WeakReference` to the grid,
so nothing needs to be disposed and no window keeps another alive.

### Date filters — six screens

`69e6214`, `7eb5b19`

Every one of these screens read an entire table on every open, with no way to ask
for less. Each query now takes an optional range and each screen defaults to a
narrow one. Passing nothing still reads everything, so no other caller changed.

| screen | opens on |
|---|---|
| `Lines_Report_Window` | current year |
| `Till_Window` | newest month with movements |
| `Log_Window` | current year |
| `Monthly_Salaries_Window` | current year |
| `Revenue_Exchange_Sub_Report_Window` | current year |
| `Fixed_Assets_Report_Sub_Window` | **everything** |

The fixed-assets report is the deliberate exception: an asset bought years ago is
still an asset today, so opening it on one year would hide most of what the report
exists to show. The filter is there to narrow, not to hide.

**The controls are the same everywhere**: a year combo box beside From/To
pickers — one filter underneath, two ways to set it — and a blue Apply button.
Picking a year fills the two dates; typing a date by hand clears the year box.
`All` is an item in the year box rather than a separate button.

`Till_Window` differs on purpose: a till belongs to one year in practice, so it
gets a **multi-select month picker** listing only months that actually have
movements, with a count each. Runs of consecutive months collapse into a single
`BETWEEN`, so a whole year is one range rather than twelve. Loading is deferred to
`DropDownClosed`, so ticking four months is one query rather than four.

Every condition compares the date column directly rather than wrapping it in
`DATE()` or `DATE_FORMAT`, so an index on that column stays usable as the tables
grow.

**One decision worth stating.** On `Till_Window` the months narrow the two grids
only; the totals stay for the whole till. A balance is the till's balance, the way
a bank statement shows a period of movements against the account's own balance.
The screen names the period so the two are not read as one number.

### The row counters — a bug found six times

Every screen that got a filter had the same fault: the row count was written in
the grid's `Loaded` handler, which runs **once**. It therefore showed whatever the
first load found, no matter what the filter did afterwards.

Counters now update where the data is set, including when a filter matches
nothing. Two screens also kept the previous rows on display when a load returned
nothing; they now clear.

This was not visible before the filters existed, because nothing changed the row
count after load.

---

## 4. The database — where the server really was the bottleneck

`8a00102`, `7ee6a59`, `57e87a3`, `15e4559`

The rule in the opening section is that the network dominates. These are the
exceptions: two server-side objects doing enormous amounts of work over small
tables.

### First, a baseline

Fifteen stored procedures and eight views live only on the servers. They were in
**no version control at all** — editing one could not be reviewed, compared, or
undone. `8a00102` dumps both servers with `SHOW CREATE` before anything was
touched. Every comparison and every offer to undo since then rests on it.

### `cash_balances` — a cross product

The view hung two unrelated branches off `cashaccount`:

```
cashaccount ca
  |- cashaction -> cashaction_denominations
  |- cashaccount_subproject -> subproject -> account -> till
```

Nothing connects them, so the server built the **cross product** and let
`GROUP BY` collapse it again:

```
cash account  1 :  10,749 denomination rows x 556 account rows = 5,976,444
cash account 22 :     385                   x 579              =   222,915
cash account 17 :     935                   x  94              =    87,890
...
total 6,304,983 rows built, to return 781
```

The cash branch is now totalled in its own derived table keyed by
`cashaccount_id` and joined once — which is exactly how the action side was
already written.

| | before | after |
|---|---|---|
| `cash_balances` | 23,215 ms | **131 ms** |
| the CashBalances grid | 22,556 ms | **124 ms** |
| `get_cash_report()` | **70,412 ms** | **475 ms** |
| `Select_CashAcc_Tills` | 1,076 ms | **110 ms** |

### `Select_All_Budget_Lines_Details` — a million subqueries

The procedure builds its columns dynamically, three per donor, and each one
carried a correlated subquery for that donor's rate:

```sql
Expected_Total * (SELECT rate FROM budget_donor
                  WHERE budget_id = all_budgetlines.budget_ID
                    AND donor_id  = <n>) / 100
```

13 donors × 3 columns × 28,426 rows ≈ **1.1 million subqueries**. `budget_donor`
holds 116 rows and is already indexed on `(budget_id, donor_id)`, so each lookup
was fast — there were simply far too many of them.

It now joins `budget_donor` once and picks the rate with a conditional aggregate,
which the surrounding `GROUP BY` already supports:

```sql
Expected_Total * MAX(CASE WHEN bd.donor_id = <n> THEN bd.rate END) / 100
```

**15,501 ms → 2,291 ms**, seven times faster.

### How they were verified

Nothing was applied before the output was compared. Both objects were installed
beside the originals under `_old` names and every query the application issues was
run against both:

| check | result |
|---|---|
| the whole `cash_balances` view, 781 rows × 21 columns | identical |
| `Select_Cash_Report`, both queries, every cash account | identical |
| `Select_CashAcc_Tills`, every cash account | identical |
| `get_cash_report()` | identical |
| `Select_All_Budget_Lines_Details`, every budget type | identical |
| the same, on the online server | identical |

And the conditions the rewrites depend on, checked on both servers:

| | |
|---|---|
| accounts owning more than one SYR/PAY till | 0 |
| duplicate `(budget_id, donor_id)` rows | 0 — and a unique key forbids them |
| columns varying inside a `GROUP BY` group | 0 |
| `SUM`/`COUNT`/`AVG` the added join could inflate | none exist |
| budgets with no donor | 1 online — covered by the comparison |

**The user then ran the same comparison independently and confirmed it.**

### What is applied where

| | offline (MariaDB 10.4) | online (Percona 8.4) |
|---|---|---|
| `cash_balances` | rewritten, verified | **untouched — already correct there** |
| `Select_All_Budget_Lines_Details` | rewritten, verified | **not applied** |

Nothing on the online database has been modified. `db/schema/online/` still
matches it exactly.

`cash_balances` has three different definitions: the bad offline one, the offline
fix, and the online one — which was always written correctly. Only the offline
server ever had the bad version.

---

## 5. The whole thing, measured end to end

A real session before all of this, against the same session shape after:

| | before | after |
|---|---|---|
| database operations | 518 | **107** |
| time spent connecting | 583.8 s | **46.8 s** |
| time spent querying | 212.9 s | **75.4 s** |
| total | **796.7 s** | **122.2 s** |

---

## 6. The one thing knowingly left broken

The first query after a pause costs far more than the rest:

| gap since the last query | n | average open |
|---|---|---|
| under 10 s | 1,170 | 275 ms |
| 10–60 s | 97 | 317 ms |
| 1–5 min | 39 | 382 ms |
| 5–15 min | 10 | 1,269 ms |
| over 15 min | 21 | **3,002 ms** (worst 11,055) |

Two causes, both confirmed:

1. **The app shortens its own idle limit.** All 177 query sites send
   `SET SESSION wait_timeout = 600` first. The server's own default is **28800**
   (8 hours), so the app only ever cuts it to 10 minutes. Session variables
   survive in the pool, so the server kills connections sitting idle there and the
   pool hands the dead one out anyway.
2. **NAT drops idle mappings**, commonly around 5 minutes, and silently. Silence
   is what makes it expensive: with no close notification the client waits through
   TCP retransmissions. The 2 → 4 → 7 → 8 → 11 second ladder in the log is that
   backoff.

**Left unfixed deliberately**, because both fixes carry risk for users on bad
links — which is the group they would be meant to help:

- Raising `wait_timeout` also extends how long a crashed client holds row locks.
  Four sites in `Payment.cs` open a transaction spanning several round trips
  (`START TRANSACTION` … `ReGenerate_AllPayments` … `COMMIT`). At 600 s locks
  clear in ten minutes; at 28800 they would sit for eight hours. That 600 is
  probably deliberate. Every other transaction in the app sends
  `START TRANSACTION … COMMIT` as one batch and cannot outlive its command.
- `Keepalive` would fix the NAT half, but a probe that fails repeatedly on a lossy
  link makes the OS tear down a connection that still worked.

If revisited: raise `wait_timeout` on read paths only, leave the four `Payment.cs`
sites at 600, and measure before and after.

---

## 7. The estimates that were wrong

Kept because this is the most useful part of the record. Several changes that
looked obviously worthwhile measured as noise, and one was badly underestimated.

| proposal | predicted | measured | outcome |
|---|---|---|---|
| P-01, removing `ClearAllPoolsAsync` | ~300 ms per query | **1,693 ms** | **2.5× better than predicted** |
| an index on `action(Till_ID, ActionDate)` | 20–50% | 46 ms | withdrawn |
| `Select_Sums`, 20 aggregate scans → 1 | 70–85% | 153 ms | deferred |
| `DataView.RowFilter` for search | large | 883 vs 965 ms — noise | withdrawn; debounce was the answer |
| `AutoGenerateColumns=False` | 311k cells saved | 53 ms | withdrawn — SfDataGrid virtualises |
| removing a dead loop (P-13) | a win | 7 ms | reclassified as cleanup |
| pushing a filter into a derived table | 1.6–9.9× | **nothing on MariaDB** | see below |

The last one is the sharpest lesson. `Select_Budget_Details` looked like the cause
of the offline slowness because a filter sat outside a derived table. Moving it
inside is 1.6–9.9× on the online MySQL 8.4. On the offline MariaDB 10.4 it changes
**nothing** — `condition_pushdown_for_derived` is on and the optimizer already
does it. The real cause was found only by timing the actual screens.

**Also wrong, and worth remembering:**

- `All_Accounts_Window` was listed as reading 28,431 actions. It reads
  `Account.Select` — the `account` table, **75 rows**.
- `CashTill_Window` was listed as 6,245 rows of `cashaction`. It calls a stored
  procedure; the largest cash account is 5,161 rows in 427 ms against a 116 ms
  floor, and the other six hold 1, 2, 2, 60, 83 and 934.
- `Log_Window` was listed as growing forever. It grows about 330 rows a year.
- The two servers were claimed to differ only in `DEFINER` and blank lines. That
  was checked on one procedure and generalised. It is not true of `cash_balances`.

---

## 8. Method, in five lines

1. **Measure the screen, not the theory.** Every correct finding here came from
   timing the calls a window actually makes.
2. **Dump server-side definitions before touching them.** `git revert` cannot
   reach a server object.
3. **Compare results in a deterministic order.** One check showed 22,390
   differences and nearly sank a good change; the report's `ORDER BY` omits
   `line_ID`, so tied rows come back in whatever order the plan produces.
4. **Check the conditions a rewrite depends on**, not just its output.
5. **Ask again when the diagnosis changes.** Approval for one fix is not approval
   for a different one found later.

---

## 9. Instrumentation — remove before release

`Helper/PerfLog.cs` is temporary. It writes
`%LOCALAPPDATA%\Hope_Center\perf.csv` with `time,caller,open_ms,query_ms,total_ms`,
never throws, and is switched off with `PerfLog.Enabled = false`.

Every number in this document came from it. It should be removed when the
performance work stops — and not before, because it is the only way to tell
whether any of this helped.
