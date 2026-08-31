# Moving to the web with Laravel — would it be faster, and is it worth it?

A study, not a recommendation to act. Every number here was measured against the
actual database and the actual code, not estimated from experience.

**Short answer:** yes, a web app would be roughly **three times faster** than the
desktop app is today — but almost all of that win comes from the code sitting next
to the database, not from being a web app. **The same win is available on the
desktop for a fraction of the work and a fraction of the risk.**

---

## 1. Where the time actually goes

This is the measurement the whole study rests on. For each real query: how long
the wall clock says it took, how long the **server** says it spent doing the work,
and the difference — which is the trip across the internet.

| query | wall | server | trip | trip share |
|---|---|---|---|---|
| `SELECT 1` (the floor) | 751 ms | 0.4 ms | 751 ms | **100%** |
| one till, whole history | 3,549 ms | 71 ms | 3,478 ms | **98%** |
| the accounts list | 275 ms | 0.8 ms | 274 ms | **100%** |
| all payments | 3,510 ms | 929 ms | 2,581 ms | **74%** |
| `cash_balances` | 840 ms | 293 ms | 547 ms | **65%** |
| all actions, the wide grid | 5,387 ms | 2,183 ms | 3,204 ms | **59%** |
| `all_budgetlines` | 2,748 ms | 1,193 ms | 1,555 ms | **57%** |

> The two stored-procedure rows are omitted: `SHOW PROFILES` does not report time
> spent inside a procedure, so their server figures came back as zero and are not
> usable. Their trip share is certainly high, but it is not measured.

**Between 57% and 100% of the time is the trip, not the work.** The weighted
middle is around **70%**.

That is the entire case for the web, in one table. A web application runs on a
machine in the same building as the database, so its round trip is a fraction of a
millisecond instead of 250–750 ms.

---

## 2. So how much faster, precisely?

### What a screen costs today

Opening a screen is several queries in a row, and **each one pays the trip
separately**. That is why the till screen batching (three queries into one) was
worth what it was: it removed two trips, not two queries.

### What it would cost on the web

| | desktop today | web, co-located |
|---|---|---|
| round trips the **user** pays | 3–10 per screen | **1** — the page request |
| cost of each | 250–750 ms | 50–200 ms |
| round trips to the **database** | 3–10 across the internet | 3–10, at well under 1 ms |
| the server's own work | unchanged | unchanged |
| bytes to the user | the raw result set | the rendered page |

### The arithmetic

A session that today spends **122 seconds** waiting on the database — that is the
figure *after* all the optimisation work; it was 797 seconds before — would spend
roughly:

- **~37 seconds** of actual server work, which does not go away
- plus one browser round trip per screen instead of several database ones

**Expect something in the range of 3× faster than today**, and around 20× faster
than the application was before this year's work.

### The honest qualifier

That 3× is the ceiling, and it assumes the web app is written carefully. A
carelessly written Laravel app makes the same mistake this one did — Eloquent's
default lazy loading produces N+1 queries very easily, and 300 queries at 0.5 ms
is still 150 ms of avoidable work per page. **The gain comes from the location,
not from the framework.**

---

## 3. Can the desktop app reach the same speed?

**Yes. Most of it, for far less work.**

The win is co-location. Nothing about it requires a browser.

### The approach

Put a small API on the same host as the database. The desktop app calls it once
per screen instead of making 3–10 database round trips across the internet. The
API makes those trips locally, where they cost nothing.

```
today   desktop  ──(3-10 trips × 250-750 ms)──▶  database
after   desktop  ──(1 request  × 250-750 ms)──▶  API ──(3-10 trips × <1 ms)──▶  database
```

The user still pays one trip per screen. That is the same number a web page pays.

### What it would recover

| | |
|---|---|
| the round-trip cost of extra queries | **recovered in full** |
| the server's own work | unchanged, as with the web |
| bytes on the wire | recovered further — the API sends what the screen needs, not raw result sets |

**Realistically 70–85% of what a full web rewrite would deliver.**

### What it costs to build

| | keep | rewrite |
|---|---|---|
| 60 screens, 11,683 lines of XAML | ✅ | |
| 42,325 lines of screen logic | ✅ | |
| 23 model classes, 10,538 lines | | ✅ |
| 188 model methods, 177 SQL statements | | ✅ — they become API endpoints |
| 23 stored procedures and views | ✅ | |

**One layer out of four.** The screens, the grids, the Excel export, the printing,
the attachments, the offline mode — all untouched.

---

## 4. What the codebase actually is

Because any estimate has to start here.

| | |
|---|---|
| screens | **60** |
| user controls | 7 |
| model classes | 23 |
| lines of C# in screens | **42,325** |
| lines of C# in models | 10,538 |
| lines of XAML | 11,683 |
| **total C#** | **65,761** |
| SQL statements in the models | 177 |
| public model methods | 188 |
| stored procedures and views | 23 |

That is not a small application. The 42,325 lines behind the screens are the part
that matters most: they are years of business rules, validation, approval flows
and edge cases that nobody wrote down anywhere else.

---

## 5. The risks of going to the web

Ordered by how much they should worry you.

### Severe

**Offline mode disappears.** The app runs against a local MariaDB today, and that
is used. A web app has one database, on one server. If the connection is down,
nobody works. For an organisation whose users already have unreliable internet,
this trades a known annoyance for a hard stop.

**The 42,325 lines of screen logic have to be rebuilt and re-verified.** Not
translated — rebuilt. Every validation, every approval rule, every "except when
the account is closed" that exists only as a line of C#. The compiler does not
help you; only testing against real financial data does.

**Financial correctness has to be re-established from scratch.** The work in this
project proved how easy it is to be wrong about this system: seven separate
performance estimates were wrong, one by a factor of 2.5, and one diagnosis was
correct on one database and worthless on the other. A rewrite is that risk applied
to every calculation in the application at once.

### Significant

**The data grid.** The app leans heavily on Syncfusion's `SfDataGrid` — grouping,
summaries, column show/hide, Excel export, row virtualisation across 28,000 rows.
Web grids that match it exist, and they are not free and not identical. Users will
notice the difference before they notice the speed.

**Excel import and export.** Currently desktop libraries working on local files.
On the web this becomes upload, server-side processing, and download — different
code and a different user experience.

**Attachments.** Currently FTP from the desktop. Rebuilt entirely.

**Printing.** Desktop print dialogs become browser printing or server-side PDF.

### Manageable

**Retraining.** 60 screens of muscle memory.

**Hosting.** A shared host running MySQL is not the same as a host that will run
PHP with the resources this needs. `max_connections` is 300 and shared.

**Two systems running at once** during migration, with the data staying consistent
between them.

---

## 6. How long, with AI help

Estimates for rewrites are wrong more often than they are right, and they are
wrong in one direction. Treat these as the optimistic end.

### Option A — the API, keeping the desktop app

| stage | |
|---|---|
| API skeleton, authentication, deployment | 1–2 weeks |
| port 188 model methods to endpoints | 3–5 weeks |
| swap the desktop data layer to call it | 2–3 weeks |
| verify every screen against the current app | 2–3 weeks |
| **total** | **8–13 weeks** |

Verification is not padding. Every screen must show the same numbers it shows
today, and the only proof is comparing them — the same method used for the two
database objects in this project.

Can be done incrementally, one screen at a time, with the old path still working.

### Option B — the full Laravel rewrite

| stage | |
|---|---|
| schema, models, relationships, the 23 procedures and views | 3–4 weeks |
| authentication, roles, the permission rules | 2 weeks |
| **60 screens** | **20–30 weeks** |
| Excel import and export | 3–4 weeks |
| attachments, printing, reports | 3–4 weeks |
| verification against the current app | 6–8 weeks |
| migration, parallel running, cutover | 3–4 weeks |
| **total** | **40–56 weeks** |

**Nine to thirteen months**, with AI help, assuming the person driving it knows the
domain. Without domain knowledge, longer — most of the time goes into discovering
what the existing screens actually do, and AI cannot read intent out of code that
does not state it.

### What AI does and does not accelerate

**Does:** writing endpoints from existing SQL, generating models from a schema,
converting screen layouts, writing tests, finding all the places a rule is
implemented.

**Does not:** deciding what the business rules *should* be where the code is
ambiguous; verifying that a financial figure is correct; the meetings; the
retraining; the discovery that a screen has behaved a certain way for three years
and someone depends on it.

The second list is where rewrites actually spend their time.

---

## 7. What the measurements say to do

**1. The database work is not finished, and it is nearly free.**

Two server-side objects were rewritten this month for 23,215 ms → 131 ms and
15,501 ms → 2,291 ms. Four more have never been examined: `Lines_ExpensesReport`,
`LineTypes_ExpensesReport`, `Select_Syr_Balances`, `Cash_Flow`. On the table
above, `all_budgetlines` alone spends **1,193 ms of server time** — that is work
no amount of relocation removes, and it is the part a web app would still pay.

Days of work, no architectural risk.

**2. Then the API, if more speed is needed.**

Two to three months, recovers 70–85% of what a rewrite would, keeps every screen,
keeps offline mode, and can be done a screen at a time with the ability to stop
halfway and still have gained something.

**3. Consider the full web rewrite only for reasons that are not speed.**

Access from anywhere, no installation, phones and tablets, one version for
everyone, no per-machine setup. Those are real and the desktop cannot give them.

But if the goal is speed, the study says the same thing the whole project has
said: **the cost is the distance to the database, and you can shorten that
distance without rewriting the application.**

---

## 8. The single sentence

A Laravel rewrite would make the app about **three times faster** and take **nine
to thirteen months**; an API next to the database would make it about **two and a
half times faster** and take **two to three months**, while keeping the 42,325
lines of business logic that took years to get right.
