# Database routines and views

Every stored procedure, function and view, as the server reports it, dumped with
`SHOW CREATE`. Two servers are recorded because the app talks to two:

| directory | server | engine |
|---|---|---|
| `local/`  | `localhost`, the offline database   | MariaDB 10.4.24 |
| `online/` | `mysql.s1893.sureserver.com:3308`   | Percona MySQL 8.4.7 |

The definitions are the same logic on both. They differ only in `DEFINER`
(`hcfinancial@%` locally, `hcfi@%` online) and in blank lines, because the two
engines format `SHOW CREATE` differently.

## Why this directory exists

These objects live on the server, not in the application. Until now they were in
no version control at all: editing one could not be reviewed, compared, or undone
with `git revert`, and there was no record of what it used to say.

This is that record. It is a **baseline, not a deployment**. Nothing here runs on
its own, and nothing in the build reads it.

## Restoring one

Take the file's contents, add `DROP` first, and run it against the server you want:

```sql
DROP PROCEDURE IF EXISTS `Select_Budget_Details`;
-- then paste the CREATE from local/Select_Budget_Details.sql
```

Check the `DEFINER` line matches a user that exists on that server before running
it, or drop the `DEFINER=` clause and let it default to the connected user.

## Re-dumping after a change

Whatever is changed on a server, dump it again and commit the new file in the same
change, so this directory keeps matching what is actually deployed.
