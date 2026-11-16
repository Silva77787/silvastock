# Recover access to the back office

Use this runbook when an administrator cannot sign in. It covers four cases:
- everyone is locked out;
- an admin changed Google account;
- a Google account was lost or compromised;
- an admin must be removed at once.

Administrators are managed only with the `silvastock` CLI (DECISIONS §5, ADR-0004); there is no admin screen.

## Before you start
- **Code and tools:** you need the .NET 10 SDK and a clone of the repository at the commit currently deployed (`git checkout main && git pull`).
- **Production connection string:** read the Neon connection string from the password manager, or from the Azure Container Apps secret (needs an Azure role that can read secrets). It is also stored as a GitHub Actions secret, but those are write-only and cannot be read back.
  - Never paste it into a file in the repository, a command argument, a chat or an issue.
  - The CLI reads it **only** from the environment variable `ConnectionStrings__Default`, or, for local development, from the API project's user-secrets (`ConnectionStrings:Default`). It never prints it.
- **Migrations:** the CLI does **not** apply them. If it fails with `SQLSTATE 42P01`, the database schema is missing: apply the migrations through the normal deploy step first.

## 1. Point the CLI at production (in one terminal session only)
Bash / Git Bash (`read -s` keeps the value out of the shell history and off the screen):
```bash
read -rs ConnectionStrings__Default && export ConnectionStrings__Default
```
PowerShell:
```powershell
$env:ConnectionStrings__Default = Read-Host "Connection string"
```
Use the Neon connection string with `SSL Mode=Require`.

> **Check the target before every command.** If the variable is empty (for example after an empty `Read-Host` on Windows), the CLI falls back to your **local** user-secrets database. Every command first prints `Target database: Host=…, Port=…, Database=… (source: …)` to stderr: make sure the source is `environment variable ConnectionStrings__Default` and the host is the Neon one before trusting the result. When you are done, close the terminal, or run `unset ConnectionStrings__Default` (Bash) / `Remove-Item Env:ConnectionStrings__Default` (PowerShell).

## 2. See who has access
```bash
dotnet run --project src/SilvaStock.Cli -- list-admins
```
For each admin it shows the email, the name, whether they are active, their last login (UTC) and whether a Google account is bound.

## 3. Pick the case

### Everyone is locked out, or a new admin is needed
Add (or reactivate) an email that has a Google account:
```bash
dotnet run --project src/SilvaStock.Cli -- add-admin someone@example.test --name "Nome Apelido"
```
- The email is stored in lower case.
- Running the command again is safe (it is idempotent).
- If the admin was removed earlier, this reactivates them.

Then sign in at `/admin/login` with that Google account. The first login binds the Google account to the admin.

### An admin changed or lost their Google account
Symptom: the login is refused with `not_allowed`, even though the email is on the list.
```bash
dotnet run --project src/SilvaStock.Cli -- reset-google someone@example.test
```
This unbinds the old Google account and signs that admin out on every device. The next Google login with that email binds the new account. Use it also if the old Google account may be compromised.

### Remove an admin immediately (left, lost laptop)
```bash
dotnet run --project src/SilvaStock.Cli -- remove-admin someone@example.test
```
- The admin is deactivated; the record is kept for the audit trail.
- Every session is revoked at once: any existing cookie gets 401 on its next request.
- To give access back later, run `add-admin` again.

## 4. Check the result
Run `list-admins` again and confirm the expected state. Then remove the connection string from the environment (step 1).

## Exit codes (for scripts)
| Code | Meaning |
|---|---|
| 0 | Success (including "already active, nothing to change") |
| 1 | Invalid usage: no or unknown command, wrong arguments, invalid email or name |
| 2 | No administrator with that email |
| 3 | Missing or invalid connection string, database unreachable or other database error (no changes saved) |

`dotnet run --project src/SilvaStock.Cli -- help` prints the usage.
