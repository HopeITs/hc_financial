# Changes to server-side objects

Two objects were rewritten. Each is here twice: as it is now, and as it was.
Every file carries its own `DEFINER`, matched to the server it is meant for.

## The files

    new/cash_balances.OFFLINE-ONLY.sql                   the rewritten view
    new/Select_All_Budget_Lines_Details.offline.sql      the rewritten procedure
    new/Select_All_Budget_Lines_Details.online.sql       the same, DEFINER for online

    old/cash_balances.offline.sql                        the view as it was offline
    old/cash_balances.online.REFERENCE.sql               what is live online - leave it
    old/Select_All_Budget_Lines_Details.offline.sql      the procedure as it was
    old/Select_All_Budget_Lines_Details.online.sql       the same, DEFINER for online

    compare-old-vs-new.sql                               installs the originals beside
                                                         the current ones under _old
                                                         names, with the queries to
                                                         compare them

## What is applied where

| | offline (MariaDB 10.4) | online (Percona 8.4) |
|---|---|---|
| `cash_balances` | rewritten, verified | **untouched — already correct there** |
| `Select_All_Budget_Lines_Details` | rewritten, verified | not applied |

Nothing on the online database has been modified.

## The one trap

`cash_balances` has **three** different definitions in this repository:

1. what the offline server used to have — the bad one, the cross product
2. what the offline server has now — the fix
3. what the online server has, and has always had — a different shape, already correct

Only the offline server ever had the bad version. **Do not run the offline fix on
the online server**; it would replace a working view to gain about 200 ms. The
file is named `OFFLINE-ONLY` for that reason.

## DEFINER

Both objects are `SQL SECURITY DEFINER`, so they run with the privileges of the
user named in the clause rather than the caller's. Setting a `DEFINER` other than
yourself needs `SUPER` (or `SET_USER_ID`).

Run these as an account that has it. If you cannot, remove the `DEFINER` clause -
the object is then created as whoever runs the file, and you should check that
user can read every table it touches before relying on it.

The offline `cash_balances` currently says `jj@localhost`, because that is the
account that created it. The original said `hcfinancial@%`, and the file in
`new/` sets it back.

## Before applying anything to the online server

Run `compare-old-vs-new.sql` there first. It installs the originals beside the
current objects without replacing anything, and gives you the queries to compare
them yourself.
