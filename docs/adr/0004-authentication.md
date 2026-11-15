# ADR-0004 — Authentication: Google OIDC + cookie session + allowlist

**Status:** Accepted · 2026-11-14 (confirmed by the owner) · Amended 2026-11-15 when the Phase 2 contract was closed (F2-01; see "Amendments")

## Context
Only 2–3 administrators, no public sign-up. The alternative (ASP.NET Identity with email + password) means managing passwords, lockout and account recovery (SMTP or a manual procedure).

## Decision
- **Google login (OpenID Connect)** in the ASP.NET Core backend (`/api/auth/login/google` → Google → `/api/auth/callback/google`), authorization code flow with state and nonce.
- **Allowlist:** `admin_users` table. After the callback, if `email_verified` is not `true` or the email is not in `admin_users` with `is_active = true` → **reject without creating a session** and redirect to `{site}/admin/login?error=not_allowed`. Emails are stored lower-case and compared case-insensitively.
- **Session:** ASP.NET authentication cookie `__Host-silvastock-auth`, `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/`, host-only on the API host. **30-day sliding expiration** (owner decision): renewed while used, expires after 30 days without use. No JWT, no long-lived tokens in the browser.
- **Data Protection keys** (which encrypt the session cookie and antiforgery tokens) are **persisted in Postgres** (`data_protection_keys` table). Otherwise every container restart or scale-to-zero (ADR-0002) would invalidate all sessions.
- **CSRF:** ASP.NET antiforgery. `GET /api/admin/me` returns a `csrfToken` in its JSON body; the frontend keeps it in memory and sends it in the `X-XSRF-TOKEN` header on `POST/PUT/PATCH/DELETE` (including logout). The antiforgery cookie itself stays `HttpOnly` on the API host.
- **Authorisation:** `Admin` policy on every `/api/admin/*`; on every request it checks that the user is still active on the allowlist (removing them from the table revokes access on the next request). API calls without a session get `401` ProblemDetails, never a redirect to Google.
- **Browser-navigation errors:** the login and callback endpoints always redirect to the site, never return JSON: `{site}/admin/login?error=not_allowed | rate_limited | login_failed`. `{site}` is a configured base URL; `returnUrl` only accepts relative paths under `/admin`.
- **Display name:** `admin_users.display_name` if set, else the Google account name, else the email.
- **Last login:** `admin_users.last_login_at` is updated on every successful login (owner decision), and shown by `list-admins`.
- **Management:** CLI commands `add-admin <email> [--name]`, `remove-admin <email>` and `list-admins`, run with the production connection string. A management screen is outside the MVP.
- **Rate limiting** (ASP.NET `RateLimiter`, fixed window per client IP) on `/api/auth/*`. Initial value: 20 requests per 5 minutes per IP, configurable.
- **CORS:** only the site origin, with credentials. **Host filtering:** `AllowedHosts` is set to the API host in production (it is `*` only for local development).

## Consequences
- + Zero stored passwords; MFA and account recovery are handled by Google.
- + Recovery if everyone is locked out: run `add-admin` with another Google account.
- + Sessions survive deployments and scale-to-zero thanks to persisted Data Protection keys.
- − Every admin needs a Google account; dependency on an external provider.
- − Requires a Google Cloud project with an OAuth consent screen (free); redirect URIs for localhost and the production domain.
- − API on a different host from the site → requires the same registrable domain (ADR-0002).
- − A 30-day session means a stolen cookie stays usable for long. Mitigations: `HttpOnly` + `Secure` + host-only cookie, allowlist re-checked on every request (`remove-admin` revokes immediately), logout deletes the cookie. Admins should not use shared computers.
- − Data Protection keys in Postgres are not encrypted at rest by ASP.NET on Linux; they are protected by the database's access control (connection string only in secrets).
- Responses: `401` without a session; `403` with a session but outside the allowlist/inactive; `400` for a missing/invalid antiforgery token. Tests prove them for every admin endpoint.

## Amendments
**2026-11-15 — F2-01 (Phase 2 contract closed with the owner)**
| Topic | Before | Now | Why |
|---|---|---|---|
| Session lifetime | Proposal: 8 h sliding, max 7 days | 30 days sliding | Owner decision |
| CSRF token delivery | Readable `XSRF-TOKEN` cookie | `csrfToken` in the `GET /api/admin/me` body | The site and the API run on different hosts (`www.` vs `api.`), so JavaScript on the site cannot read a host-only API cookie; the original design could not work |
| Login errors | 429 JSON from the login endpoint | Redirect with `?error=rate_limited` / `login_failed` | Login is a full-page navigation; a JSON 429 would show raw JSON to the user (code review of PR #56) |
| Data Protection keys | Not specified | Persisted in Postgres | Required for sessions to survive restarts and scale-to-zero |
| `admin_users` | id, email, display_name, is_active, created_at | + `last_login_at`; email lower-case unique | Owner decision; case-insensitive matching |
| Host filtering | `AllowedHosts: *` | Restricted in production | Code review of PR #53 (Host header poisoning of redirect URIs) |
