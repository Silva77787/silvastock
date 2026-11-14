# ADR-0004 — Authentication: Google OIDC + cookie session + allowlist

**Status:** Accepted · 2026-11-14 (confirmed by the owner)

## Context
Only 2–3 administrators, no public sign-up. The alternative (ASP.NET Identity with email + password) means managing passwords, lockout and account recovery (SMTP or a manual procedure).

## Decision
- **Google login (OpenID Connect)** in the ASP.NET Core backend (`/api/auth/login/google` → Google → callback).
- **Allowlist:** `admin_users` table. After the callback, if `email_verified` is not `true` or the email is not in `admin_users` with `is_active = true` → **reject without creating a session** and redirect to `/admin/login?error=not_allowed`.
- **Session:** ASP.NET authentication cookie, `HttpOnly`, `Secure`, `SameSite=Lax`, sliding expiration (proposal: 8 h, max 7 days — to confirm in F2-01). No JWT, no tokens in the browser.
- **CSRF:** ASP.NET antiforgery. The frontend reads the token (readable `XSRF-TOKEN` cookie) and sends it in the `X-XSRF-TOKEN` header on `POST/PUT/PATCH/DELETE`.
- **Authorisation:** `Admin` policy on every `/api/admin/*`; on every request it checks that the user is still active on the allowlist (removing them from the table revokes access).
- **Management:** CLI command `SilvaStock.Cli add-admin <email> [--name]` / `remove-admin <email>`, run with the production connection string. A management screen is outside the MVP.
- **Rate limiting** (ASP.NET `RateLimiter`) on the `/api/auth/*` endpoints.
- **CORS:** only the site origin, with credentials.

## Consequences
- + Zero stored passwords; MFA and account recovery are handled by Google.
- + Recovery if everyone is locked out: run `add-admin` with another Google account.
- − Every admin needs a Google account; dependency on an external provider.
- − Requires a Google Cloud project with an OAuth consent screen (free); redirect URIs for localhost and the production domain.
- − API on a different host from the site → requires the same registrable domain (ADR-0002).
- Responses: `401` without a session; `403` with a session but outside the allowlist/inactive. Tests prove both for every admin endpoint.
