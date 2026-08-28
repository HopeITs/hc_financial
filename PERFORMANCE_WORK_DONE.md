# Financial_WPF_App — Performance Work: What Was Done

Branch: `Faster` · diverged from `ba3c8ca` on `Add-Action-Refactoring`
Scope: 7 commits + 1 uncommitted change in progress
Every number below was measured on the production server and the real database,
not estimated.

---

## 0. Context — the system being optimised

| | |
|---|---|
| Stack | C# / WPF / .NET Framework 4.8, Syncfusion SfDataGrid |
| Database | MySQL at `mysql.s1893.sureserver.com:3308` (shared hosting, remote) |
| Data layer | `Models/*.cs`, raw SQL strings, `MySqlDataAdapter.Fill` into `DataTable` |
| Data size | `action` 28,409 rows · `payment` 41,257 · `action_payment` 21,831 |
| Date span | 2021-02-16 → 2026-08-27 (5.5 years) |
| Largest till | Till 76 — 10,039 actions over 4 years |

**The single most important measured fact about this system:**

```
SELECT 1   (a query that does nothing at all)   =   206–406 ms
```

Any query, however trivial, costs 200–400 ms. That is the network floor to this
host. Because `action` is only 28k rows, the server executes almost anything
faster than the round trip to reach it. **Therefore: the bottleneck was never
query complexity or missing indexes — it was the number of round trips and the
number of bytes inside them.** Two proposals were dropped after measurement
proved this (see section 9).

---

## 1. `P-01` — Stop destroying the connection pool after every query

**Commit** `96726a2` · **File** `Login.cs` (+ new `Helper/PerfLog.cs`)

### The problem

```csharp
public static void CloseAndClearConnection()
{
    MyConn.Close();
    MyConn.ClearAllPoolsAsync();   // wipes EVERY pool in the process
}
```

Called from **202 sites** across the model layer. `ClearAllPoolsAsync` does not
close just this connection — it empties every connection pool in the process, so
the pool was never able to hand a connection back. Every query paid a full
TCP + MySQL auth handshake.

### The fix

Delete the `ClearAllPoolsAsync()` line. Keep `Close()`.

### Verification before changing

1. `SHOW CREATE`/docs of MySql.Data 9.4.0 confirmed it clears *all* pools
2. Neither connection string sets `Pooling=false` → defaults active
3. Online/offline use different connection strings → different pools → toggling is safe
4. Shutdown does not depend on it
5. No other `ClearAllPools`/`ClearPool` call anywhere in the codebase

### Measured

Reproduced the old code exactly (`Close()` + fire-and-forget `ClearAllPoolsAsync()`):

| | median time to open a connection |
|---|---|
| Before | **1,982 ms** |
| After | **289 ms** |

A 37-operation session went from **101 s → 39.8 s** of database time.

### Also added: `Helper/PerfLog.cs`

Temporary diagnostic instrumentation. Logs one CSV line per database operation
to `%LOCALAPPDATA%\Hope_Center\perf.csv`:

```
time,caller,open_ms,query_ms,total_ms
```

Never throws, switched off with `PerfLog.Enabled = false`. **This is not
production code and should be removed when the work is finished.** Every number
in this document that says "measured in a real session" came from it.

---

## 2. `P-02` — Build account tabs on demand instead of all at once

**Commit** `b70eb91` · **Files** `Views/Account_Window.xaml`, `.xaml.cs`

### The problem

`Fill_Account_Tabs` constructed a full `Window` object for **every** tab up
front — `Till_Window` per till, plus `Rate_Window`, `Account_Beneficiaries_Window`,
`AddBudgetLines_Windows`, `AllAccountBudgetLines_Windows`, `ExpensesReport_Window`,
`Add_till`, `CashTill_Window`, `CashBalances_Window` — and each constructor ran
its own full data load.

```csharp
Till_Window w = new Till_Window(...);   // constructor runs Load_Data()
newTabItem.Content = w.Content;         // only the content is kept
```

Opening one account = **~30 synchronous queries** before the first tab appeared.
The user sees one tab and waits for ten.

### The fix

Tabs register a factory and build on first selection:

```csharp
private readonly Dictionary<TabItem, Func<Window>> _pendingTabs;
private void SetLazyContent(TabItem tab, Func<Window> factory) => _pendingTabs[tab] = factory;
// TabControl1_SelectionChanged -> MaterializeTab -> tab.Content = factory().Content
```

Applied to all 10 sites. Same tabs, same order, same privilege checks, same data —
only the moment of loading changed.

### Four pitfalls that had to be handled

1. **Registration must precede `Items.Add`** — adding the first tab to an empty
   `TabControl` selects it immediately, which is what triggers the build. Register
   after, and the first tab renders empty.
2. **`SelectionChanged` bubbles** — every `ComboBox` and `DataGrid` inside a tab
   raises it too. Guard: `if (!ReferenceEquals(e.Source, tabControl1)) return;`
3. **Exceptions cannot escape a WPF handler** — some windows throw
   `ArgumentException` on data-fetch failure. A `_buildingTabs` flag keeps the
   *first* tab's exception propagating to `OnLoad` exactly as before, while
   deferred tabs show the same message box the rest of the file uses.
4. **A failed tab must stay retryable** — the factory is removed from the
   dictionary only *after* success.

### Two regressions this exposed, fixed in the same commit

- **Deferred tab content lost its theme colour.** `Apply_Color_Theme` walks the
  tab contents once at `ApplicationIdle`; a tab built later was never reached.
  Each tab now re-applies the theme when it is built.
- **Tab underline stayed red.** `tabItemStyle` hard-codes
  `Background="{StaticResource HopeRed}"` on `BottomBorder`, and
  `NewTabItem_Loaded` overrides it via `Template.FindName` — which returns `null`
  before a tab has been rendered, so the red default stayed. The colour is now
  remembered per tab and re-applied on selection, when the template is guaranteed
  to exist.

### Measured, against real usage

`Rate.Select_PerDonor` — a stored procedure costing **20.1 s per session** — was
called **zero** times after the change, because the Rates tab was never opened.

Per 100 operations, comparing real sessions before and after:

| query | before | after |
|---|---|---|
| `Rate.Select_PerDonor` | 0.9 | **0** |
| `Budget.Select` | 2.1 | 0.4 |
| `Project.Select` | 5.5 | 2.1 |
| `SubProject.Select` | 5.5 | 2.1 |
| `Account.Select_AmountType` | 5.5 | 2.1 |

---

## 3. `P-15` — Network transport: compression + connect timeout

**Commit** `1e8acf3` · **File** `Login.cs`

### 3a. Protocol compression

```diff
- Connect Timeout=21600;AllowUserVariables=True;
+ Connect Timeout=60;AllowUserVariables=True;UseCompression=True;
```

`UseCompression` is a MySQL **wire protocol** feature: client and server agree at
handshake time to zlib-compress packets. The data is byte-for-byte identical —
only the bytes crossing the network change.

**Online connection string only.** Over `localhost` there is no link to saturate,
so it would be pure CPU cost.

#### Measured with the server's own `Bytes_sent` counter

`SELECT * FROM action ORDER BY ID DESC LIMIT 200` — same 200 rows, same 33 columns:

| | bytes on the wire |
|---|---|
| Compression OFF | **33,188** |
| Compression ON | **6,801** |

**79% fewer bytes.** And timings stopped swinging:

| | readings |
|---|---|
| OFF | 4188, 2150, 1332, 1280, 2127 ms — median **2,127** |
| ON | 777, 756, 760, 754 ms — median **760** |

Not just faster — *stable*. Fewer bytes means a narrower window for link
congestion to hit.

#### Whole-app projection: ~23%, not 64%

The 64% is one large query. Across the app, weighted by real call frequency:

| caller | query time | result shape | expected gain |
|---|---|---|---|
| `Action.Select` | 36.7 s | 200 rows × ~70 cols | 52% |
| `Action.Select_Sums` | 29.1 s | 11 scalars | 7% |
| `Action.Select_action_not_approved` | 23.3 s | table has 9 rows | ~8% |
| `Budget.Select` | 21.5 s | stored proc, 1 row | 9% |
| everything else | 50.3 s | mixed | ~25% |
| **total** | **160.9 s** | | **~23%** |

**Compression helps when transfer is the bottleneck. It does nothing when the
server is busy computing** (stored procedures) or when the result is tiny.

### 3b. Connect timeout: 21600 s → 60 s

`Connect Timeout` governs **opening** a connection, not query duration — that is
`CommandTimeout`, unchanged at 120 s. The old value was 6 hours.

#### Why it mattered — measured from a real session

```
10:01:09   Action.Select_Sums   open_ms = 412,900     ← the app was frozen 6.9 minutes
```

Two connections consumed **417 s of the session's 518 s** of connection time.

#### The failure mode, reproduced

A blackhole address is capped by Windows at ~21 s regardless of the setting. The
mode the OS *cannot* cut short is a server that **accepts the TCP connection then
never answers** — tested with a local listener that accepts and stays silent:

| setting | result |
|---|---|
| `Connect Timeout=30` | gave up after 30.6 s ✓ |
| `Connect Timeout=21600` | **still blocked after 60 s** |

#### Why 60 s is safe — from 854 logged connections

| threshold | connections above it |
|---|---|
| over 2 s | 8 (0.94%) |
| over 10 s | 2 (0.23%) |
| **over 30 s** | **1 (0.12%)** |

The slowest legitimate connection ever recorded was **10.5 s**; then a clean gap
to the 412.9 s outlier. 60 s leaves 5.7× headroom.

#### Verified: long imports are unaffected

| test | result |
|---|---|
| `Connect Timeout=30`, query runs 45 s | **completed** in 50.3 s |
| `Connect Timeout=30`, query runs 90 s | **completed** in 92 s |
| `CommandTimeout=15`, query runs 60 s | failed at 15.3 s |

Excel imports use `CommandTimeout = 120` (186 sites do). **Left untouched at the
user's request** — but note: if an import takes over 2 minutes it was already
failing, and `Connect Timeout` never protected it.

---

## 4. `P-05` — Debounce the grid search

**Commit** `a9e8b65` · **File** `Helper/SearchDataGrid.cs`

### The problem

The search box raises `TextChanged` on **every character**. Each run:

```csharp
_dataTable = dataTable.Copy();                        // full deep copy
foreach (DataRow row in _dataTable.Rows)
    if (row.ItemArray.Any(i => i.ToString().ToLower().Contains(text)))  // 2 strings per cell
        filtered.Rows.Add(row.ItemArray);
dataGrid.ItemsSource = filtered.DefaultView;          // grid rebuilds
```

Tolerable at 200 rows. Since paging was removed from the till grid it runs over
**10,039 rows × 62 columns = 622,418 cells per character**.

### Measured

| per keystroke | |
|---|---|
| current code (copy + scan) | 173 ms |
| built once (scan only) | 120 ms |
| tuned scan (no allocations) | 100 ms |

| typing a 6-letter word | |
|---|---|
| current code | **1,097 ms** of frozen UI |
| built once | 713 ms |
| tuned scan | 556 ms |
| **tuned + 250 ms debounce** | **92 ms** |

**The problem was frequency, not cost.** Halving the cost got 1,097 → 556.
Running it once instead of six times got it to 92.

### The fix

All three, inside `SearchDataGrid` — **none of the 23 calling windows changed**:

1. no deep copy
2. scan drops `ItemArray` and per-cell `ToLower`
3. 250 ms debounce; clearing the box still restores instantly

Debounce state hangs off a `ConditionalWeakTable` keyed by the grid, with a weak
reference back to it, so a closed window is not kept alive by having been searched.

### One behavioural note

Matching moved from `ToLower().Contains()` to `IndexOf(OrdinalIgnoreCase)`.
Identical for Arabic and English; differs only under locale-specific casing
rules such as Turkish dotted I.

### A proposal that was dropped

`DataView.RowFilter` was measured at 883 ms vs 965 ms — inside noise. It scans
the same cells, so it solves nothing. Withdrawn.

---

## 5. `P-03` — Reload a hidden till when it is looked at

**Commit** `e2b56d9` · **File** `Views/Till_Window.xaml.cs`

### The problem — the largest single waste found

`TillNotifier.RefreshTill` is a **static** event every `Till_Window` subscribes to
in its constructor. The unsubscribe sits in `OnClosed` — but these windows are
never shown and never closed (only their `Content` is moved into a `TabItem`), so
`OnClosed` never runs. The subscriber list only grows.

They cannot be garbage collected either: the content moved into the tab still
holds the window through its own event handlers. **A weak event would not have
helped** — this was checked before designing the fix.

### The evidence — from a real 3-hour session

Bursts of consecutive `Action.Select` calls within 3 s of each other:

```
runs of 2, 2, 8, 3, 3, 3, 12, 13
```

And what preceded them:

```
12:16:17.357   Action.Update          ← one save
12:16:20.473   Action.Select          ┐
12:16:21.273   Action.Select_not_approved   │  12 full grid reloads
12:16:21.903   Action.Select_Sums     ┘

12:18:26.310   Action.Update          ← another save
12:18:28.928   Action.Select          ┐  13 reloads
```

| | |
|---|---|
| time spent on redundant reloads | **466.3 s** |
| share of all till-grid time | **78%** |
| **share of ALL database time in the session** | **58%** |

### The fix

```csharp
private void OnTillRefresh()
{
    if (IsContentVisible()) Load_Data();     // the till you are looking at
    else _refreshPending = true;             // hidden: mark stale
}
// content.IsVisibleChanged -> if (_refreshPending) { _refreshPending = false; Load_Data(); }
```

What is on screen is always current: the visible till reloads immediately as
before, a hidden one reloads before it can be seen. Only the moment moved.

### Verified after the change

```
before : bursts of 2, 2, 8, 3, 3, 3, 12, 13
after  : NONE — every load is a separate user action
```

Comparing the clean before/after segments of real sessions:

| | before | after |
|---|---|---|
| operations | 518 | 107 |
| connection time | 583.8 s | **46.8 s** |
| query time | 212.9 s | 75.4 s |
| **total** | **796.7 s** | **122.2 s** |

Connection time fell **92%** — the combined effect of P-01, P-03 and P-15b.

---

## 6. Bug fix — refresh the linked tills after insert and after approval

**Commit** `f822573` · **Files** `AddAction_Window2.xaml.cs`,
`Approval_Sub_Window.xaml.cs`, `Approval_Sub_Pay_Window.xaml.cs`

**Both predate the performance work** — verified before touching anything.
`TillNotifier.RaiseRefresh()` was called from **`Till_Window` only**, 5 sites:

| operation | broadcast? |
|---|---|
| `EditButton_Click` | yes |
| `DeleteButton_Click` | yes |
| `Delete_Click` | yes |
| `MouseDoubleClick` | yes |
| `Delete_Selected_btn_Click` | yes |
| **`AddNewAction_btn_Click`** | **no** — does nothing after the dialog |
| **approval screens** | **no** — never |

This is exactly why edit worked and insert did not: on edit the dialog closes and
`EditButton_Click` resumes and broadcasts; on insert the dialog stays open and
`AddAction_Window2` called only `_parentTillWindow.Load_Data()` — the till you
opened *from*, never the other side of a transfer or exchange.

### Fix

```diff
- if (_parentTillWindow != null) _parentTillWindow.Load_Data();
+ TillNotifier.RaiseRefresh();
```

plus `TillNotifier.RaiseRefresh()` at the six approval completion sites.

Broadcasting reaches the parent too, so no separate call is needed. It also fixes
a silently broken case: opening the window from the **menu** (not from a till)
leaves `_parentTillWindow` null, and the old null guard meant nothing refreshed.

**Order mattered:** adding three more broadcast sites would have been expensive
*before* P-03, when every broadcast reloaded every till ever opened.

---

## 7. `P-15b` — Load the till screen in one round trip instead of three

**Commit** `8777a6b` · **Files** `Models/Action.cs`, `Views/Till_Window.xaml.cs`

### The problem

`LoadGrid` ran three separate calls, each opening its own connection:

```csharp
a.Select(...)                      // the grid
a.Select_action_not_approved(...)  // the not-approved grid
a.Select_Sums(...)                 // the totals + cash balances
```

Server work is the same either way; the extra cost is two connections and two
round trips, at ~220 ms each against this host.

### Measured, with compression already on

| | |
|---|---|
| three calls, a connection each | **3,950 ms** |
| one connection, one batch | **2,044 ms** |

**48% off every till screen load** — the screen opened most often in a day.

### How the duplication risk was handled

The three single-query methods **cannot be deleted** —
`Select_action_not_approved` has four callers including the approval screens. So
the SQL text was extracted into shared builders:

```
BuildSelectQuery()  ·  BuildNotApprovedQuery()  ·  BuildSumsQuery()
        ↓                                              ↓
  the old single-query methods              Select_Till_Screen()  (new)
```

One copy of each statement, not two that could drift apart.

### Parameter safety — checked, not assumed

| parameter | used by | verdict |
|---|---|---|
| `@SubProject_ID` | `Select` + `Select_action_not_approved` | same value from the same line — safe |
| `@t` | all three | always 600 — safe |
| `@Account_ID`, `@ActionDate` | sums only | unique — safe |

### Index-coupling risk

Results are read through named constants after an explicit count check:

```csharp
if (screen.Tables.Count < Models.Action.TillScreenTableCount)
    throw new InvalidOperationException(...);
DataTable grid = screen.Tables[Models.Action.TillScreenGrid];
```

Reading the totals from the wrong table would otherwise show wrong money with no
error.

### Verified before committing

A tool called the app's own methods **through reflection on the built assembly**,
ran both paths against **7 tills** from 10,039 rows down to 552, and compared
every cell of all four result sets — row counts, column counts, column names and
values.

```
tills identical : 7
tills differing : 0
~1.7 million cells compared, no differences
```

---

## 8. In progress (uncommitted) — date filter on `Lines_Report_Window`

**Files** `Models/Action.cs`, `Views/Lines_Report_Window.xaml`, `.xaml.cs`,
`Financial_WPF_App.csproj`, new `Images/Buttons/Apply_blue.png`

`SelectLines_Report` read the **entire** `action` table on every open — no date
filter, no till filter, nothing.

### What was added

```csharp
public DataTable SelectLines_Report(string FromDate = "", string ToDate = "")
// both empty -> reads everything, exactly as before
if (FromDate != "") query += " AND action.ActionDate >= @FromDate";
if (ToDate   != "") query += " AND action.ActionDate <= @ToDate";
```

UI, in the left `WrapPanel` matching `Payments_Window`:

- **Year** combo box (`All` + years, newest first)
- **From** / **To** date pickers
- **Apply** button — `AddButtonStyle` + `Apply_blue.png`

Year and dates are **one filter with two ways in**: picking a year fills the
dates; editing a date by hand clears the year box. A `_settingDates` guard stops
them fighting each other.

**Default: current year.** Opening the report reads ~282 rows (2026) instead of
28,409.

### `Apply_blue.png`

The button's background is an image, not a colour — `apply.png` is the word
"Apply" in HopeRed `#BE1F24`. A blue copy was generated by swapping the colour
channel and **keeping the alpha channel untouched**, so the antialiasing is
identical. New colour `#17778D` = `GreenBlue`, the app's general theme blue.
The original `apply.png` is unchanged and still used by `Payments_Window`.

### Also fixed

`All_textBlock` (the row counter at the top) was written only in
`myDataGrid_Loaded`, which fires **once**. It kept showing the first load's count
no matter how the filter changed. Now updated in `LoadGrid()` on every load.

### Open question for the user

2026 has only **282** actions while 2024 has **10,499**. Defaulting to the current
year opens a nearly empty screen. Consider defaulting to the **most recent year
that has data** instead.

---

## 9. Two proposals that measurement killed

Recorded so they are not re-proposed.

### Index on `action(Till_ID, ActionDate)` — worth 46 ms

Estimated at "20–50%". `EXPLAIN` showed the server **already uses** the `Till_ID`
index and then filesorts 10,039 rows:

```
table=action  type=ref  key=FK_action_till2  rows=10039  extra=Using filesort
```

| | |
|---|---|
| sorted by `ActionDate` (no index) | 374 ms |
| sorted by primary key (index exists) | 328 ms |

**46 ms.** The filesort is cheap because the table is small. Withdrawn.

### `Select_Sums` 20 scans → 1 — worth ~153 ms

`Select_Sums` contains 20 separate aggregate subqueries, many literally
duplicated. Estimated at "70–85%". Measured:

| | |
|---|---|
| one aggregate scan of `action` | 326 ms |
| twenty aggregate scans | 479 ms |

**153 ms.** At 72 calls per session that is ~11 s — not nothing, but it carries
the highest correctness risk in the codebase (see the warning in the plan file).
Deferred, not withdrawn.

### And two UI proposals that measured as noise

| | before | after |
|---|---|---|
| `AutoGenerateColumns=False` (93 → 48 columns) | 290 ms | 237 ms |
| paging (10,039 → 200 rows) | 290 ms | 241 ms |

`SfDataGrid` virtualises properly — it only realises visible rows, so row count
barely affects binding. **Both withdrawn as performance measures.** (Paging
survives for a different reason — see the plan file.)

---

## 10. Cumulative result

| # | change | measured |
|---|---|---|
| P-01 | connection pool | 1,982 → 289 ms per connection |
| P-02 | tabs on demand | eager waste eliminated; `Rate.Select_PerDonor` 20.1 s → 0 |
| P-15 | compression | 79% fewer bytes; ~23% of query time |
| P-15 | connect timeout | worst-case freeze bounded at 60 s instead of open-ended |
| P-05 | search debounce | 1,097 → 92 ms per word typed |
| P-03 | deferred refresh | 466 s of redundant reloads removed — 58% of all DB time |
| P-15b | batched till screen | 3,950 → 2,044 ms per till open |

**Real session, before all of it vs after all of it:**

```
operations   518  →  107
connection   583.8 s  →   46.8 s
query        212.9 s  →   75.4 s
TOTAL        796.7 s  →  122.2 s
```

---

## 11. Method notes

- **Nothing was changed without measuring first**, and several confident
  estimates turned out wrong. `EXPLAIN`, the server's `Bytes_sent` counter, and
  `PerfLog` on real sessions decided every call.
- **Interleaved A/B runs** were used for anything network-dependent, because this
  link swings by 3× between readings.
- **Behaviour preservation was verified, not asserted** — the batching change was
  proved by comparing 1.7 million cells across 7 tills.
- **`perf.csv` accumulates across app restarts.** Split it on the timestamp going
  backwards to separate sessions, and note that a session spanning a rebuild
  contains both old and new code — this caught out one comparison.
