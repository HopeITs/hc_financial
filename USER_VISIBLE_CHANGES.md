# What people using the app will notice

For whoever announces this release, trains users, or answers "what changed?".
No file names, no SQL. The numbers are measured, not estimated.

---

## The short version

The app got faster, and six screens gained a date filter.

A working session that used to spend **13 minutes** waiting on the database now
spends **2 minutes**. Most of that is invisible — screens simply arrive sooner.
The visible part is the new filter bar on six screens, and two screens that used
to take twenty seconds now open immediately.

---

## 1. Everything is faster, everywhere

This needs no explanation to users and no training. It just happens.

| | before | after |
|---|---|---|
| database operations in a session | 518 | 107 |
| time waiting to connect | 9 min 44 s | 47 s |
| time waiting for data | 3 min 33 s | 1 min 15 s |
| **total waiting** | **13 min 17 s** | **2 min 2 s** |

Three things did this, and none of them changed what the app shows:

- **The connection is reused** instead of being rebuilt before every single
  operation. This was the biggest one.
- **Results are compressed** on the way from the server — 79% fewer bytes.
- **Screens ask once** instead of three or five times where they used to.

### Who notices most

**People on slow internet notice most.** That is worth saying out loud, because
the usual assumption is the opposite. Both main changes remove round trips and
bytes, which is exactly what a weak connection charges for.

On a 1 Mbit/s line:

| screen | before | after |
|---|---|---|
| till, showing all movements | 30 s | **4 s** |
| all payments | 12 s | **3 s** |
| cash flow, largest account | 4.5 s | **1.3 s** |

---

## 2. Two screens that were unusable are now instant

Offline, on the local database.

| screen | before | after |
|---|---|---|
| **Cash balances** | 22 seconds | **under 1 second** |
| **All budget lines** | 16 seconds | **2 seconds** |

The cash report behind the first went from **70 seconds to half a second**.

**The numbers on these screens have not changed.** Every value was compared
against the old version before and after, row by row and column by column, and
then checked independently. They are the same figures, arriving sooner.

> These two fixes are applied to the **offline** database only. The online
> database already had the first one and does not need it; the second is prepared
> but not yet applied there.

---

## 3. Six screens gained a date filter

This is the part users will see and ask about.

| screen | opens showing |
|---|---|
| Lines report | the current year |
| Log | the current year |
| Monthly salaries | the current year |
| Revenue / exchange report | the current year |
| **Till** | the newest month that has movements |
| **Fixed assets report** | **everything** |

### What the controls do

A **Year** box, **From** and **To** date pickers, and a blue **Apply** button.

- Pick a year and the two dates fill in for you.
- Type a date by hand and the year box clears — you are now on a custom range.
- Pick **All** in the year box to go back to everything.

They are two ways into one filter, not two filters.

### Two screens work differently, on purpose

**The till** has a month picker with tick boxes instead of a year box. In practice
a till belongs to one year, so months are the useful unit. The list shows only
months that actually have movements, with a count beside each, and you can tick
several. Nothing is fetched until you close the list, so ticking four months is
one wait rather than four.

**The fixed assets report opens on everything.** An asset bought three years ago
is still an asset today. Opening it on one year would hide most of what the report
is for. The filter is there to narrow it when you want to, not to hide things by
default.

### The thing most likely to be asked about

**"The screen looks empty when I open it."**

That is the filter doing its job. The screen opens on the current year, and if
that year has few records so far, few records is what you see. Change the year
box, or pick **All**.

### The counters at the top now follow the filter

The row counts above the grid used to show whatever was loaded when the screen
first opened and never change again. They now update every time the filter
changes, including down to zero.

If anyone previously learned to distrust those numbers, they can stop.

---

## 4. One number that deliberately does not follow the filter

On the **till** screen, the month picker narrows the two grids. **The totals stay
for the whole till.**

This is intentional. A balance is the till's balance — the way a bank statement
shows a period of movements against the account's own balance, not a balance
recalculated for the period you happened to select. The screen names the selected
period so the two are not read as one number.

**Worth mentioning in training**, because it is the kind of thing that looks like
a bug until it is explained once.

---

## 5. What did not change

Say this plainly, because it is what people worry about:

- **No figure the app calculates has changed.** No total, no balance, no rate, no
  report line.
- **Nothing was deleted, moved, or renamed.** No screen lost a column or a button.
- **How you enter data is exactly as it was.** Adding, editing, approving,
  importing from Excel — untouched.
- **Excel imports behave the same.** Their time limit was left alone deliberately.

Where a screen now shows fewer rows, it is because a date filter is applied and
the screen says so. Set it to **All** and everything is there.

---

## 6. One rough edge that is still there

**The first click after a long break is slow.** Coming back from lunch, or after a
meeting, the first thing you do may take a few seconds — occasionally longer —
before the app settles back to normal speed.

This is known, measured, and left alone on purpose. The available fixes each carry
a risk for users on weak connections, and that trade was judged not worth making.

If a user reports "it hangs when I come back to it, then it's fine" — that is
this, it is understood, and it is not new.

---

## 7. If something looks wrong

Both database changes can be undone immediately; the originals are kept and
nothing was thrown away. The application needs no change either way.

The most useful thing a user can report is **a specific number on a specific
screen that they know the right value for**. Everything was checked by comparing
new against old, which proves they match — it cannot prove the old one was right
to begin with.
