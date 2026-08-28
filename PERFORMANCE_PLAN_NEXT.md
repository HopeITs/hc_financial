# Financial_WPF_App — Performance Plan: What Comes Next

Companion to `PERFORMANCE_WORK_DONE.md`. Read that first — it carries the
measurements this plan rests on.

Branch: `Faster` · last commit `8777a6b` · one change uncommitted (section 1)

---

## How to use this file in a new conversation

Give the assistant **both** files. Then state which section you want to work on.
The working method that produced the results so far, and that should continue:

1. **Measure before proposing.** This codebase has repeatedly defeated confident
   estimates. Two proposals were withdrawn after measurement (index: predicted
   20–50%, actual 46 ms · `Select_Sums`: predicted 70–85%, actual 153 ms).
2. **Interleave A/B runs.** The link to the server swings 3× between readings.
   A single before/after pair proves nothing.
3. **Prove behaviour preservation, do not assert it.** For anything touching
   query results, compare cell by cell against the old path before committing.
4. **One change, one commit, measured separately.** Do not stack unverified
   changes — a session that spans a rebuild produced a misleading comparison once.
5. **Build with MSBuild and check for `error MSB3021`/`MSB3027`, not just
   `error CS`.** If the app is running, the compile succeeds but the copy to
   `bin\Debug` fails silently and you test the old binary. This cost a full
   debugging cycle once.

```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" \
  "D:\Hope Programs\Financial\Financial_App\Financial_WPF_App.sln" -t:Build -p:Configuration=Debug
```

---

## The one fact that governs every decision here

```
SELECT 1   (a query that does nothing)   =   206–406 ms
```

`action` is only 28,409 rows. The server executes nearly anything faster than the
round trip to reach it. **Optimise round trips and bytes, not SQL cleverness.**
Everything below is ordered by that principle.

---

## 1. `Lines_Report_Window` — DONE

Shipped in `69e6214`. `SelectLines_Report` takes an optional From/To; both empty
reads everything, as before. The screen has a year combo box beside From/To
pickers and opens on the current calendar year.

**One thing to watch.** The test database holds 282 rows for 2026 against 10,499
for 2024, so the report opens looking almost empty here. On real data the current
year is the right default. If it proves wrong in use, the alternative is "the most
recent year that has data", which costs one extra grouped-count query.

---

## 2. Date filters on the remaining screens

From the per-screen survey. **Seven screens deserve a filter; nineteen do not.**

### Do these, in this order

Every row below is measured. Two entries in the first version of this table were
wrong, and both were caught only by measuring — so measure before implementing,
every time.

| # | screen | rows it renders | span | verdict |
|---|---|---|---|---|
| 1 | `Lines_Report_Window` | whole `action` table | 6 years | **done** — From/To + year, defaults to the current year |
| 2 | `Till_Window` | 28,409 · largest till 10,039 | 4 years | **done** — multi-select month picker |
| 3 | `Revenue_Exchange_Sub_Report_Window` | **6,624** | 5 years | **worth doing** — and see the note below |
| 4 | `Monthly_Salaries_Window` | 3,174 (busiest year 1,478) | 5 years | marginal — a year filter saves maybe 100 ms |
| 5 | `Fixed_Assets_Report_Sub_Window` | not yet measured | — | measure first |
| 6 | `Syr_Balances_Sub_Window` | not yet measured | — | a balance is "as of" — single date, not a range |

### Withdrawn from this list, with the measurement that withdrew it

| screen | the claim | what it measures | verdict |
|---|---|---|---|
| `All_Accounts_Window` | "reads `Select_All_Actions`, 28,431 rows" | reads **`Account.Select`** — the `account` table, **75 rows**, and its only date is `BeginDate` | **wrong screen.** Reference data; no filter |
| `CashTill_Window` | "`cashaction`, 6,245 rows over 3.2 years" | calls the stored procedure `Cash_Flow`. Biggest cash account: 5,161 rows in **427 ms**, against a floor of 116 ms | real but small — see below |
| `CashAction_Window` | listed with the above | a **single-record editor** (`Select_CashAction(id)`) | nothing to filter |
| `Log_Window` | "639 rows but grows forever" | 656 rows: **292 in 2025, 364 in 2026** | grows ~330 rows a year. Not a performance problem this decade |
| `ExpensesReport_Window` | "year filter; budgets are annual" | its tables are tiny — 18 budgets, 509 budget lines, 298 lines — yet its queries cost **1.4-5.6 s of server time** | a filter cannot help. See section 5 |

**`CashTill_Window` in detail.** A filter there is possible — `Cash_Flow` returns a
`Counting date` column — but the payoff is ~300 ms on **one** of seven cash
accounts, and nothing on the other six (which hold 1, 2, 2, 60, 83 and 934 rows).
The cost is that `Cash_Flow` is a stored procedure that **lives only on the
server**: it is not in this repository, not in version control, and `git revert`
cannot undo an edit to it. Changing it applies instantly to everyone, including
users still on an older build. Revisit only if real data produces several large
cash accounts.

### Do NOT add filters to these

Reference data — bounded by nature, a filter is complexity for nothing:

| table | rows | screen |
|---|---|---|
| `city` | 7 | City_Window |
| `donor` | 17 | Donors_Window |
| `budget` | 18 | Budget_Window, Account_Budgets |
| `line` | 298 | Line_Window, BudgetLineType |
| `employee` | 347 | Employee_Window |
| `budget_line` | 509 | Account_Budget_Lines |
| `beneficiary` | 1,131 | Account_Beneficiaries |

**And especially not the approval screens.** `action_not_approved` holds **22
rows** in the entire database — what waits for approval gets approved and leaves.
A filter there could hide something waiting on the user's decision.

### Why From → To rather than a fixed window

The user's assumption that a till stays within one year is true only for the
*small* tills. The ones that matter span years:

| till | rows | span |
|---|---|---|
| 76 | 10,039 | **4.0 years** |
| 77 | 6,532 | **5.2 years** |
| 78 | 2,896 | 2.1 years |
| 311 | 578 | 1.0 year |

No fixed window fits both. Year buttons are a convenience on top, not a
replacement — actions per year: 2021: 1,254 · 2022: 5,187 · 2023: 8,091 ·
2024: 10,499 · 2025: 3,096 · 2026: 282.

### The pattern to copy

`Lines_Report_Window` after section 1, which itself follows `Payments_Window`:

- filter controls in the **left `WrapPanel`**, not beside the search box on the right
- `statistic_lableStyle` for labels — it is already `DarkGray #6E6F72` and is
  **not** repainted by `UIElementColoring`
- `ActionComboboxStyle` for the year box, `DatePickerActionBar` for the pickers
- Apply button: `AddButtonStyle` + `Apply_blue.png`, `Height="25" Width="55"`
- no separate "All" button — `All` is an item in the year box
- `_settingDates` guard so the year box and the pickers do not fight
- **update the row counter inside `LoadGrid`**, never rely on a `Loaded` handler

### `Till_Window` has one extra decision

`Select_Sums` has **no date filter**, so the totals above the grid cover the whole
till while the grid shows a period.

**Recommended: leave the totals covering everything** — that is correct
accounting, exactly how a bank statement behaves, and it avoids touching
`Select_Sums` (see the warning in section 5). Put the period on screen so nobody
misreads the total as belonging to it.

---

## 3. Merge the account combo-box queries — measured, ready

**File:** `Views/Account_Window.xaml.cs` → `Load_Comboboxes_Data`

Five independent reference queries run one after another, each opening its own
connection: `Project.Select`, `City.Select`, `SubProject.Select`,
`SubProject_Donor.Select`, `Account.Select_AmountType`.

| approach | measured | needs |
|---|---|---|
| sequential, a connection each — today | **5,610 ms** | — |
| parallel, own connection per task | 1,287 ms | removing static `MyConn` + threads |
| **batched, one connection** | **1,218 ms** | **no threads at all** |

**Batching beats parallelism here** — the dominant cost is opening connections,
and one batch opens one. Same technique as `Select_Till_Screen` (section 7 of the
done file) and as `City.Select_Tree_View`, which already merges five queries.

Risk is the same and the mitigation is known: extract shared query builders so
there is one copy of each statement, read results through named constants, check
the table count before indexing.

---

## 4. Remove `Helper/PerfLog.cs` — when the work is finished

Diagnostic scaffolding, not production code. It touches:

- `Helper/PerfLog.cs` (the file)
- `Login.cs` — `BeginOpen()`, `EndOpen()`, `EndOperation()`
- `Financial_WPF_App.csproj` — one `<Compile Include>` line

**Do not remove it before the remaining measurements are taken** — it is the only
source of real session data. `PerfLog.Enabled = false` switches it off without
deleting anything.

---

## 5. Deferred, with a warning

### `P-04` — `Select_Sums`: 20 aggregate scans → 1

Worth **~153 ms per call**, ~11 s per session at 72 calls.

**This carries the highest correctness risk in the codebase.** The screen reads
the results **by numeric index**:

```csharp
Income_textBlock.Text     = sum_dt.Rows[0].Field<double>(0);
Income_textBlockusd.Text  = sum_dt.Rows[0].Field<double>(1);
Transfered_textBlock.Text = sum_dt.Rows[0].Field<double>(2);
Exchange_textBlock.Text   = sum_dt.Rows[0].Field<double>(4);
...
Net_textBlockusd.Text     = sum_dt.Rows[0].Field<double>(10);
```

Any reordering of the eleven columns shows **wrong financial figures with no
error message**.

And a subtlety that defeats the obvious rewrite: it looks as though
`s_I_all = s_I_nonusd + s_I_usd`, but the condition
`NOT (tt.Name='SYR' AND IFNULL(ct.Name,'')='USD')` evaluates to `NULL` when
`tt.Name` is null, which excludes that row from **both** sides. The totals must
be computed with their original conditions independently, not derived by addition.

If attempted: preserve column order exactly, and verify by comparing all eleven
values across every till the way `Select_Till_Screen` was verified.

### The expenses report is slow because of views, not volume

This is the only place found so far where the server itself is the bottleneck
rather than the network, so the usual answer — fetch fewer rows — does not apply.

Measured cost of its queries, from `perf.csv`:

| query | total | of which server-side |
|---|---|---|
| `Budget_Lines.Select_unique_Budget_types` | 7,513 ms | **5,617 ms** |
| `Budget.Select` | 5,458 ms | **5,060 ms** |
| `Budget.Select_Lines_ExpensesReport` | 3,302 ms | 2,802 ms |
| `Budget_Lines.Select_All` | 2,810 ms | 2,280 ms |
| `Budget.Select_LineTypes_ExpensesReport` | 2,025 ms | 1,766 ms |

Against the network floor of 206-406 ms, these are doing real work. But the data
is tiny: 18 budgets, 509 budget lines, 298 lines, 165 rates.

The reason is that they read **views**, and the views are large:

| view | joins | nested selects |
|---|---|---|
| `all_budgetlines` | 14 (13 LEFT) | 3 |
| `all_avgrates` | 22 (16 LEFT) | 6 |

`cash_balances` is a view too.

MySQL has no materialised views, so every touch rebuilds the whole thing. The
clearest example is the app's own query:

```sql
SELECT DISTINCT (all_budgetlines.BudgetType_ID), all_budgetlines.BudgetType
FROM all_budgetlines;
```

It returns **one row** and costs **1,455 ms**. Counting the view alone costs
1,159 ms — so nearly all of it is building a 14-join view in order to read one
distinct value out of it.

**Do not add a date filter here.** The view is rebuilt whether or not the result
is filtered.

Three real options, none of them started:

1. **Read the base table instead.** That one query wants distinct budget types —
   they live in a small base table. This is a per-query fix, low risk, and does
   not touch the view. Start here.
2. **Cache in the app.** Budget types change rarely; read once per session
   instead of per screen open. Needs a rule for when the cache is stale.
3. **Rewrite or index behind the views.** Biggest payoff, and the only one that
   helps every caller — but the views live on the server, outside this
   repository and outside `git revert`, exactly like `Cash_Flow`. Needs `EXPLAIN`
   on each view first, and a way to version the definitions.

### `P-06` / async — blocked behind a prerequisite

Two different things hide under "async":

| | what it fixes | status |
|---|---|---|
| not freezing the UI | responsiveness, not speed | not done — every query is on the UI thread |
| true parallel execution | wall-clock time | **impossible today** — `Login.MyConn` is a shared static connection |

The server *does* accept parallel work (5 queries took roughly the time of the
slowest). But batching already reaches that result more cheaply, so parallelism
is not worth the threads.

**Async for responsiveness is still worth doing eventually** — the 412 s freeze
was one stalled call on the UI thread. But it requires removing the static
connection first, or you get *connection is already in use* errors — the family
of bug already hit in commit `b5f1f08`.

### Removing `Login.MyConn` (static shared connection)

Not a speedup by itself, but **the gate** for async, parallelism, and guaranteed
connection cleanup on exception. Touches 202 call sites. Large, mechanical,
and best done in one deliberate pass rather than incidentally.

### Withdrawn — do not re-propose

| proposal | why |
|---|---|
| index on `action(Till_ID, ActionDate)` | 46 ms; the server already uses the `Till_ID` index and the filesort is cheap at 28k rows |
| `AutoGenerateColumns="False"` | 53 ms; `SfDataGrid` virtualises, the extra columns are never realised |
| classic pagination (`LIMIT`/`OFFSET`) | 49 ms on the UI; and it was removed *because it was broken* — search and sort only covered one page. Date filters solve the real problem instead |
| `DataView.RowFilter` for search | 883 vs 965 ms — inside noise; it scans the same cells |

---

## 6. Open issues — correctness, not performance

Found while profiling. **None have been touched.** They are not speed problems,
but two of them affect data.

### a. "Delete Selected" does nothing while a search is active

`Delete_Selected_btn_Click` reads the original table:

```csharp
DataView newView = new DataView(dt);
newView.RowFilter = "IsSelected = 1";
```

But during a search the grid is bound to a **filtered copy**. Ticks land on the
copy, `dt` has none, and nothing is deleted. **Pre-existing**, and P-05 did not
change it. Fixing it properly means making the search filter a *view over* `dt`
rather than a new table.

### b. `PerDonor_Rates` is a shared permanent table

`Select_PerDonor_AvgRate` runs, on every call:

```sql
DROP TABLE IF EXISTS PerDonor_Rates;
CREATE TABLE IF NOT EXISTS PerDonor_Rates AS SELECT ...
```

It is a normal table, not temporary, and **shared across all sessions**. Two users
opening the rates screen at once can drop each other's table mid-read. This is a
data-correctness hazard, not a performance one.

### e. `Login.MyConn` is a shared static field, rewritten on every call

```csharp
public static MySqlConnection MyConn;               // one field, shared
MyConn = new MySqlConnection(ConnectionString);     // overwritten per call
```

`BuildConnection()` assigns the static field, then the caller uses it and calls
`CloseAndClearConnection()`. Two callers running at once would have the second
overwrite the first's connection, and the first would then close a connection it
no longer owns.

Nothing in the app deliberately queries in parallel, and today it does not
happen: a few paths run off the UI thread (`Login_Window2` sign-in,
`SaveDocument` uploads) but each awaits before the next database call. The window
is real but never entered.

**It predates this branch.** Pooling (P-01) did not create it. It is recorded
because it widens on a slow link: the slower the connection, the longer each
caller holds the field, so the odds of an overlap rise for exactly the users who
can least afford it.

Fixing it means giving each caller its own `MySqlConnection` rather than sharing
one static field. That is a change to every one of the ~200 call sites, so it is
a project, not a patch — and it should not be started until something actually
demonstrates a collision.

### c. ~4,236 payments dated beyond 2030

`payment` is the largest table (41,257 rows). Its date range ends at
**2123-07-07**, with one row per year from 2116 to 2123 — the signature of
instalment generation with no end date. Roughly **10% of the largest table** looks
like data with no meaning. Worth investigating; cleaning it would also shrink the
table.

### d. `CommandTimeout = 120` on Excel imports

186 sites use 120 s. If an import takes longer it **already fails today** —
`Connect Timeout` never protected it. Left unchanged at the user's explicit
request; revisit only if imports actually time out.

---

## 6b. The connection findings — reviewed, mostly closed

A full review of every connection change on this branch, done because some users
are on very slow links.

### The idle stall — real, measured, deliberately left alone

The first query after a pause costs far more than the rest:

| gap since the last query | n | average open |
|---|---|---|
| under 10 s | 1,170 | 275 ms |
| 10-60 s | 97 | 317 ms |
| 1-5 min | 39 | 382 ms |
| 5-15 min | 10 | 1,269 ms |
| over 15 min | 21 | **3,002 ms** (worst 11,055) |

Two causes, both confirmed:

1. **The app shortens its own idle limit.** All 177 query sites send
   `SET SESSION wait_timeout = 600` first. The server's own default is **28800**
   (8 hours), so the app is only ever cutting it to 10 minutes. Session variables
   survive in the pool, so the server kills connections that are sitting idle
   there, and the pool hands the dead one out anyway.
2. **NAT drops idle mappings**, commonly around 5 minutes, and does it silently.
   Silent death is what makes it expensive: with no close notification the client
   waits through TCP retransmissions. The 2 → 4 → 7 → 8 → 11 second ladder in the
   log is that backoff.

**Left unfixed on purpose.** Both fixes carry risk for users on bad links:

- Raising `wait_timeout` also extends how long a crashed client holds row locks.
  Four sites in `Payment.cs` open a transaction that spans several round trips
  (`START TRANSACTION` … `ReGenerate_AllPayments` … `COMMIT`). At 600 s the locks
  clear in 10 minutes; at 28800 they would sit for 8 hours. That 600 is probably
  deliberate. Every other transaction in the app sends
  `START TRANSACTION … COMMIT` as one batch and cannot outlive its command.
- `Keepalive` would fix the NAT half, but a probe that fails repeatedly on a
  lossy link makes the OS tear down a connection that still worked — so it can
  hurt precisely the users it was meant to help.

If this is ever revisited: raise `wait_timeout` on read paths only, leave the four
`Payment.cs` sites at 600, and measure before and after.

### What the review confirmed is safe

Both changes that altered connection behaviour help slow links **more** than fast
ones, because both remove round trips or bytes — the two things a weak link
charges most for. On a 1 Mbit/s link:

| screen | uncompressed | compressed |
|---|---|---|
| till grid, all actions | 29.6 s | **4.0 s** |
| all payments | 11.7 s | **2.9 s** |
| cash flow, biggest account | 4.5 s | **1.3 s** |

And pooling replaced a constant cost with a rare one: of 1,344 opens on record,
only **16 (1.2%)** are slower than the *median* open before pooling existed.

### What the review changed

`Connect Timeout` 60 → 180 (`9d0886f`). 60 s had been picked from measurements on
a fast link. The handshake moves 5,389 bytes — 43 ms even at 1 Mbit/s — so
bandwidth does not limit it; it costs about 16 round trips, so latency does. 180 s
covers a round trip of 11 s. Higher buys nothing: `Open()` blocks the UI thread,
so the number is also the freeze duration, and a failure past three minutes means
unreachable rather than slow.

---

## 7. Recommended order

Reordered after measuring. The filter work has reached the end of what it can
usefully do — most remaining screens are too small to benefit.

| # | item | measured value | risk | section |
|---|---|---|---|---|
| 1 | merge the account combo-box queries | 5,610 → 1,218 ms per account open | low | 3 |
| 2 | `all_budgetlines` → read the base table for the distinct-types query | ~1,400 ms per call, one query changed | low | 5 |
| 3 | filter on `Revenue_Exchange_Sub_Report_Window` | 6,624 rows over 5 years | low, pattern settled | 2 |
| 4 | measure the two unmeasured sub-reports | unknown | none — measurement only | 2 |
| 5 | remove `PerfLog` | — | none | 4 |
| 6 | the correctness issues | not speed | needs decisions | 6 |

Deliberately **not** on the list, each with its measurement in section 2 or 5:
`All_Accounts_Window`, `CashTill_Window`, `CashAction_Window`, `Log_Window`,
`Monthly_Salaries_Window` (marginal), `ExpensesReport_Window` (a filter cannot
help it), and the two connection fixes in section 6b.

**Note on ordering.** Items 1-3 are all "one query, measured, low risk". Item 5
should wait until the work stops, because it is the only instrument left for
telling whether any of this helped.

---

## 8. Reference — where things are

| what | where |
|---|---|
| connection setup, pooling, timeouts | `Login.cs` → `Set_ConnectionString`, `BuildConnection`, `CloseAndClearConnection` |
| all SQL | `Models/*.cs` — raw strings, `MySqlDataAdapter.Fill` |
| the batched till query | `Models/Action.cs` → `Select_Till_Screen` + the three `Build*Query` helpers |
| the till screen | `Views/Till_Window.xaml.cs` → `LoadGrid`, `OnTillRefresh` |
| lazy tabs | `Views/Account_Window.xaml.cs` → `Fill_Account_Tabs`, `MaterializeTab`, `_pendingTabs` |
| grid search | `Helper/SearchDataGrid.cs` — used by 23 windows, same call pattern in each |
| refresh broadcast | `Helper/TillNotifier.cs` — static event; subscribers are `Till_Window` instances |
| instrumentation | `Helper/PerfLog.cs` → `%LOCALAPPDATA%\Hope_Center\perf.csv` |
| theme colours | `App.xaml` — `GreenBlue #17778D`, `HopeRed #BE1F24`, `DarkGray #6E6F72` |
| button styles | `App.xaml` — `AddButtonStyle` (flat, image background), `ButtonColoredStyle` (rounded, themed) |

### Reading `perf.csv`

```
time,caller,open_ms,query_ms,total_ms
```

- it **accumulates across restarts** — split sessions where the timestamp goes backwards
- `open_ms ≈ 0` means the pool served the connection; ~300 ms means a real handshake
- a session spanning a rebuild contains **both old and new code** — split on the
  first appearance of a new method name before comparing
- outliers matter: two connections once accounted for 417 s of 518 s
