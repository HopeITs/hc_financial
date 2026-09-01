# What people using the app will notice

For whoever announces this release, trains users, or answers "what changed?".
No file names, no SQL. The numbers are measured, not estimated.

---

## The short version

The app got faster, several screens that used to crawl now open at once, and eight
screens gained a date filter.

A working session that used to spend **13 minutes** waiting on the database now
spends **2 minutes**. Most of that is invisible — screens simply arrive sooner.
The visible parts are the new filter bar, the approval screen opening instantly
instead of after a wait, and two screens that took twenty seconds now taking under
one.

---

## 1. Everything is faster, everywhere

This needs no explanation to users and no training. It just happens.

| | before | after |
|---|---|---|
| database operations in a session | 518 | 107 |
| time waiting to connect | 9 min 44 s | 47 s |
| time waiting for data | 3 min 33 s | 1 min 15 s |
| **total waiting** | **13 min 17 s** | **2 min 2 s** |

Four things did this, and none of them changed what the app shows:

- **The connection is reused** instead of being rebuilt before every single
  operation. This was the biggest one.
- **Results are compressed** on the way from the server — 79% fewer bytes.
- **Screens ask once** instead of three or five times where they used to.
- **Tabs are built when opened**, not all at once — see section 3.

### Who notices most

**People on slow internet notice most.** That is worth saying out loud, because
the usual assumption is the opposite. The main changes remove round trips and
bytes, which is exactly what a weak connection charges for.

On a 1 Mbit/s line:

| screen | before | after |
|---|---|---|
| till, showing all movements | 30 s | **4 s** |
| all payments | 12 s | **3 s** |
| cash flow, largest account | 4.5 s | **1.3 s** |

---

## 2. Screens that were unusable are now instant

| screen | before | after |
|---|---|---|
| **Cash balances** | 22 seconds | **under 1 second** |
| **All budget lines** | 16 seconds | **2 seconds** |
| **Syrian balances**, USD | 15 seconds | **under 1 second** |
| the cash report behind the first | 70 seconds | **half a second** |

**The numbers on these screens have not changed.** Every value was compared
against the old version, at full precision, and then checked independently. They
are the same figures, arriving sooner.

### And two tabs that never worked at all

On the Syrian balances screen, the **EUR** tab always failed with an error, and on
the online database the **SYR** tab did too. Both now open. They had been broken
since before this work; it was found while making the screen faster.

---

## 3. The approval screen

The screen used most, and the one that changed most.

**It used to build all sixteen tabs as it opened** — every currency inside every
category, whether or not anyone looked at them. On real data, where thousands of
rows can be waiting, it fetched all of them for fifteen tabs nobody opened.

Now:

- **It opens immediately, showing nothing.** You choose a period and ask for it.
- **A tab loads the first time you open it**, not before.

### Why it starts empty

Approval is a work queue. A screen that opened on "this year" would quietly hide
anything waiting outside it. So there is no default period — nothing is hidden,
because nothing is shown until you ask.

**This is the question most likely to come back**: "the approval screen is empty".
It is meant to be. Pick months, or a From/To range, and it fills.

The same "build the tab when it is opened" change also went to the revenue and
exchange report, the fixed assets report, the Syrian balances and the budget lines
screens.

---

## 4. The filter, on eight screens

| screen | opens showing |
|---|---|
| Lines report | the current year |
| Log | the current year |
| Monthly salaries | the current year |
| Revenue / exchange report | the current year |
| **Till (accounts)** | the newest month that has movements |
| **Fixed assets report** | **everything** |
| **Approval**, all five tabs | **nothing, until you ask** |

### How it works

A **Month** box with tick boxes, listing only the months that actually have
records, with a count beside each and a **Select All** at the top. Beside it,
**From** and **To** date pickers and an **Apply** button.

- **Ticking months applies on its own** when you close the list. No Apply needed.
- **Apply is for the From/To dates**, which are not a request until they are typed.
- **The two are one filter**: choosing months clears the dates, typing a date
  clears the months.
- **The X clears everything and empties the screen.** It does not fall back to
  showing the whole table.

### Two screens work differently, on purpose

**The till** has no From/To, just the months — in practice a till belongs to one
year, so months are the useful unit. It opens on the newest month that has
movements.

**The fixed assets report opens on everything.** An asset bought three years ago is
still an asset today. Opening it on one year would hide most of what the report is
for. The filter narrows it when you want; it does not hide by default.

### The counters at the top follow the filter

The row counts above the grid used to show whatever was loaded when the screen
first opened and never change again. They now update every time the filter changes,
including down to zero.

If anyone previously learned to distrust those numbers, they can stop.

### The count beside each month is what you will get

Worth knowing because it was briefly wrong: on the approval screens the number
beside a month counts only the rows **you** can act on — not everything in that
month. Two people with different approval limits will correctly see different
numbers for the same month.

---

## 5. One number that deliberately does not follow the filter

On the **till** screen, the month picker narrows the two grids. **The totals stay
for the whole till.**

This is intentional. A balance is the till's balance — the way a bank statement
shows a period of movements against the account's own balance, not a balance
recalculated for the period you happened to select. The screen names the selected
period so the two are not read as one number.

**Worth mentioning in training**, because it is the kind of thing that looks like a
bug until it is explained once.

---

## 6. What did not change

Say this plainly, because it is what people worry about:

- **No figure the app calculates has changed.** No total, no balance, no rate, no
  report line.
- **Nothing was deleted, moved, or renamed.** No screen lost a column or a button.
- **How you enter data is exactly as it was.** Adding, editing, approving,
  importing from Excel — untouched.
- **Excel imports behave the same.** Their time limit was left alone deliberately.

Where a screen now shows fewer rows, it is because a filter is applied and the
screen says so. Clear it, or widen it, and everything is there.

---

## 7. One rough edge that is still there

**The first click after a long break is slow.** Coming back from lunch, or after a
meeting, the first thing you do may take a few seconds — occasionally longer —
before the app settles back to normal speed.

This is known, measured, and left alone on purpose. The available fixes each carry
a risk for users on weak connections, and that trade was judged not worth making.

If a user reports "it hangs when I come back to it, then it's fine" — that is this,
it is understood, and it is not new.

**A related note for anyone comparing days.** The speed of the line to the server
varies a lot. On one measurement it was four and a half times slower in the
afternoon than it had been that morning, which made every screen feel slower
without a single thing having changed in the app. When something feels slow again,
that is worth ruling out before anything else.

---

## 8. If something looks wrong

The database changes can be undone immediately; the originals are kept and nothing
was thrown away. The application needs no change either way.

The most useful thing a user can report is **a specific number on a specific screen
that they know the right value for**. Everything was checked by comparing new
against old, which proves they match — it cannot prove the old one was right to
begin with.
