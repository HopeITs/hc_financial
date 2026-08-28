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

## 1. Finish the date filter on `Lines_Report_Window` — IN PROGRESS

**Status:** implemented, builds clean, **not yet tested by the user, not committed**

### Open decision

2026 has **282** actions; 2024 has **10,499**. The current default (calendar year)
opens a nearly empty screen. Options:

- keep the calendar year
- default to the **most recent year that has data** — one line in `SetCurrentYear()`
- default to `All` and let the user filter

**This is the user's call.** Ask before changing.

### Before committing

- open the report → defaults applied, counter matches
- change year → data *and* counter update together
- edit a date by hand → year box clears; Apply works
- `All` → everything returns
- search and Excel export still work on what is shown

### Suggested commit message

```
Add a date range to the lines report

SelectLines_Report read the entire action table on every open - no date filter,
no till filter. It now takes an optional range; both dates empty reads everything,
exactly as before.

The screen gains a year combo box and From/To pickers that drive one filter
between them, and opens on <the chosen default> instead of all 28,409 actions.

Also fixes the row counter, which was written only in myDataGrid_Loaded - a
once-only event - so it kept showing the first load's count regardless of filter.

Apply_blue.png is apply.png recoloured to GreenBlue #17778D with the alpha
channel untouched; the original is unchanged and still used by Payments_Window.
```

---

## 2. Date filters on the remaining screens

From the per-screen survey. **Seven screens deserve a filter; nineteen do not.**

### Do these, in this order

| # | screen | source | size | why here |
|---|---|---|---|---|
| 1 | `Lines_Report_Window` | whole `action` table | no filter at all | section 1 — in progress |
| 2 | **`Till_Window`** | `action` | 28,409 · largest till 10,039 over 4 years | most-used screen in the app |
| 3 | `All_Accounts_Window` | `Select_All_Actions` | 28,431 (UNION of two tables) | no filter |
| 4 | `CashTill_Window` / `CashAction_Window` | `cashaction` | 6,245 over 3.2 years | no filter |
| 5 | `ExpensesReport_Window` | budget lines | — | year filter; budgets are annual |
| 6 | `Fixed_Assets_Report_Sub_Window` | — | — | From → To |
| 7 | `Revenue_Exchange_Sub_Report_Window` | — | — | From → To |
| 8 | `Syr_Balances_Sub_Window` | — | — | **single date** — a balance is "as of", not "between" |
| 9 | `Monthly_Salaries_Window` | `employee_salary` | 3,174 | month / year |
| 10 | `Log_Window` | `log` | 639 but grows forever | From → To, default last month |

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

## 7. Recommended order

| # | item | measured value | risk | section |
|---|---|---|---|---|
| 1 | finish + commit the lines report filter | reads 282 rows instead of 28,409 | low | 1 |
| 2 | date filter on `Till_Window` | biggest remaining, most-used screen | medium — UI change | 2 |
| 3 | merge the combo-box queries | 5,610 → 1,218 ms per account open | low | 3 |
| 4 | date filters on the other five screens | varies | low once the pattern is settled | 2 |
| 5 | remove `PerfLog` | — | none | 4 |
| 6 | the correctness issues in section 6 | not speed | needs decisions | 6 |

**Settle the filter design on screens 1 and 2 before spreading it to seven** —
changing the pattern later means touching all of them again.

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
