# ADR-0004 — Authentication: Google OIDC + cookie session + allowlist

**Status:** Accepted · 2026-11-14 (confirmed by the owner) · Amended 2026-11-15 when the Phase 2 contract was closed (F2-01; see "Amendments")

## Context
Only 2–3 administrators, no public sign-up. The alternative (ASP.NET Identity with email + password) means managing passwords, lockout and account recovery (SMTP or a manual procedure). The static site runs on `www.<domain>` and the API on `api.<domain>` (same registrable domain, ADR-0002), behind the Azure Container Apps ingress, which terminates TLS.

## Decision

### Login (Google OpenID Connect)
- ASP.NET Core OpenID Connect handler: `ResponseType = code`, **`ResponseMode = query`** (so the callback is a top-level `GET /api/auth/callback/google`), `UsePkce = true`, **`SaveTokens = false`** (no Google tokens in the session cookie), `GetClaimsFromUserInfoEndpoint = false`, scopes `openid email profile`. Issuer `https://accounts.google.com` and audience = client ID, validated by the handler. Correlation and nonce cookies are `Secure` (always) and short-lived.
- The OIDC handler is **never the default challenge scheme**: it is only challenged explicitly by `GET /api/auth/login/google`.
- After the token is validated:
  1. `email_verified` must be present and equal to `true`; otherwise `not_allowed`.
  2. The email is normalised with `ToLowerInvariant()` and looked up in `admin_users` (unique index on the normalised value); it must exist with `is_active = true`; otherwise `not_allowed`.
  3. **Google account binding:** if `admin_users.google_sub` is null it is set to the token's `sub` (first login); if it is set and differs, `not_allowed`. This stops a different Google account that later owns the same email (e.g. a lapsed custom domain) from signing in.
  4. `last_login_at` is updated and the session cookie is issued.
- Cancelled at Google (`access_denied`), invalid/expired state or nonce, or any other remote failure: handled in `OnRemoteFailure`/`OnAccessDenied` with `HandleResponse()` and a redirect to `{site}/admin/login?error=login_failed`. Google's `error`/`error_description` and exception text are never reflected.
- **Errors always redirect** (the login is a full-page navigation): `{site}/admin/login?error=not_allowed | rate_limited | login_failed`. The site shows a generic message for any unknown code.
- **`returnUrl`:** accepted only if it is a relative path equal to `/admin` or starting with `/admin/` or `/admin?` (segment-aware: `/adminx` is rejected); max 512 characters; no control characters (including tab, CR, LF), no backslash, no `//`, no `..` segments, also after percent-decoding. The final URL is built as `new Uri(siteBase, returnUrl)` and must have the configured site's scheme, host and port. Anything else falls back to `/admin`. The value travels only inside the protected `AuthenticationProperties` (OIDC state), never in a plain cookie.
- `{site}` is a configured base URL (`Site:BaseUrl`, e.g. `http://localhost:3000` locally, `https://www.<domain>` in production).

### Session
- ASP.NET cookie authentication. Cookie **`__Host-silvastock-auth`**: `HttpOnly`, `Secure` (`CookieSecurePolicy.Always`), `SameSite=Lax`, `Path=/`, no `Domain` (host-only on the API host), **persistent** (`IsPersistent = true`).
- **Lifetime (owner decisions):** `ExpireTimeSpan` = 30 days with `SlidingExpiration`. ASP.NET renews the cookie only on a request made after more than half of the window has passed, so in practice it expires after 15–30 days without use. **Absolute maximum of 90 days** from the original login (`issued_at` claim): after that a new Google login is always required.
- Claims: admin `id`, email, `session_version`, the Google `name` claim (used as the display-name fallback; not persisted), `issued_at`. No tokens.
- **Validation on every request** (`OnValidatePrincipal`, reusing the allowlist lookup by admin `id`), in this order — the first match wins:
  1. admin not found → `401`;
  2. `session_version` different from the database → `401` (revoked by logout, `remove-admin` or `reset-google`);
  3. more than 90 days since `issued_at` → `401`;
  4. admin inactive (`is_active = false`) → `403`.
  For the 401 cases the principal is rejected (`RejectPrincipal`) and the cookie deleted. For the inactive case the principal is **kept**, the cookie is deleted and not renewed, and the `Admin` policy fails → Forbid → `403` ProblemDetails (rejecting the principal there would turn it into a 401). Because the CLI always increments `session_version` when it deactivates someone, an admin removed with `remove-admin` gets **401**; the **403** path covers an admin deactivated without a version change (e.g. a direct database update), which is how tests reach it.
- **Revocation (owner decision):** `admin_users.session_version` (integer). `POST /api/auth/logout`, `remove-admin` and `reset-google` increment it, which **signs that admin out on every device** (acceptable for 2–3 admins) and makes a copied cookie useless.
- API calls never redirect: `OnRedirectToLogin` → `401` ProblemDetails, `OnRedirectToAccessDenied` → `403` ProblemDetails, both without a `Location` header.
- The OIDC handler's built-in sign-out endpoints are disabled (`RemoteSignOutPath` and `SignedOutCallbackPath` set to empty): there is no OIDC sign-out flow, and `GET /signout-oidc` would otherwise let any site delete the session cookie without the antiforgery check.
- Every `/api/admin/*` response carries `Cache-Control: no-store` (set once on the route group), so private data (VIN, registration numbers, notes, pre-signed URLs) is not kept in browser or proxy caches.
- **Display name:** `admin_users.display_name` if set, else the Google `name` claim from the session, else the email.

### Authorisation and error precedence
- `Admin` policy on the whole `/api/admin` route group (no admin endpoint can be added without it).
- For **every** method, checks run in this order and the first failure wins: **401** (no/invalid/revoked session) → **403** (not active on the allowlist) → **400** (missing or invalid antiforgery token, unsafe methods only).

### CSRF
- `GET /api/admin/me` returns a `csrfToken` in its JSON body. The site cannot read API cookies (different host), so it keeps the token in memory and sends it as **`X-XSRF-TOKEN`** (`AntiforgeryOptions.HeaderName`) on `POST/PUT/PATCH/DELETE`.
- Antiforgery cookie `__Host-silvastock-xsrf`: `HttpOnly`, `Secure` (always), `SameSite=Strict`, `Path=/`.
- **Enforced explicitly** by an endpoint filter on every unsafe method of the `/api/admin` group and on `POST /api/auth/logout`, calling `IAntiforgery.ValidateRequestAsync` and returning `400` ProblemDetails (pt-PT). The default `UseAntiforgery()` middleware only validates form-bound endpoints and is not relied on.

### Rate limiting
- ASP.NET `RateLimiter`, registered **before authentication** (the callback is handled by the authentication middleware and never reaches an endpoint).
- Global limiter partitioned by **client IP** for requests whose path starts with `/api/auth/login/` or `/api/auth/callback/`: fixed window, 20 requests per 5 minutes per IP (configurable). `OnRejected` answers `302` to `{site}/admin/login?error=rate_limited` (never 429/503 to the browser).
- `POST /api/auth/logout` is **not** rate limited.

### Proxy, hosts and CORS
- **Forwarded headers** first in the pipeline: `XForwardedFor | XForwardedProto` (not `XForwardedHost`), `ForwardLimit = 1`. On the Consumption environment the ingress addresses are not known in advance and the container is reachable **only** through the ingress, so `KnownNetworks`/`KnownProxies` are cleared and `ForwardLimit = 1` takes only the hop the ingress appended (Microsoft's guidance for Container Apps); an optional configurable trusted CIDR can replace this if a custom VNet is added. Enabled only outside Development (and in tests that exercise it). The rate-limit key is the resolved `RemoteIpAddress`; behind the proxy the OIDC `redirect_uri` must be `https://`. If Cloudflare ever proxies `api.<domain>`, the trusted client-IP source is revisited in F7-05.
- **`AllowedHosts`** = the API host in production (`*` only in Development). Container Apps probes use TCP so they are not rejected (see ADR-0003: `/health` wakes the database).
- **CORS:** exactly the site origin (`https://www.<domain>` in production; `http://localhost:3000` in Development), with credentials and `Vary: Origin`. No wildcards, patterns, preview origins or `http:` origins outside Development.
- Google redirect URIs registered exactly: `https://api.<domain>/api/auth/callback/google` and `http://localhost:5080/api/auth/callback/google`.

### Data Protection
- Keys persisted in Postgres with **`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`** (justified dependency: the official way to share keys across restarts and instances), table `data_protection_keys`. This framework-owned table keeps the package's `int` key, an accepted exception to the "bigint identity" rule.
- `SetApplicationName("SilvaStock")`. One key ring per environment: production keys are never copied into development or preview databases.

### Management and logging
- CLI: `add-admin <email> [--name]`, `remove-admin <email>` (deactivates and increments `session_version`), `reset-google <email>` (clears `google_sub` so a different Google account can be bound on the next login, and increments `session_version`, so sessions of the old account end), `list-admins` (shows `last_login_at` and whether a Google account is bound). A management screen is outside the MVP.
- Failed logins log the outcome and error code only, never the full email, tokens or cookie values (at most the email domain).
- **Local development:** the API runs on `http://localhost:5080`; `Secure`/`__Host-` cookies over `http://localhost` work in Chromium-based browsers and Firefox (not Safari). Integration tests use `https://localhost` as the client base address.

## Consequences
- + Zero stored passwords; MFA and account recovery are handled by Google.
- + Recovery if everyone is locked out: run `add-admin` with another Google account.
- + Sessions survive deployments and scale-to-zero thanks to persisted Data Protection keys.
- + Logout and `remove-admin` revoke sessions server-side on all devices; the 90-day cap bounds a stolen cookie's lifetime.
- − Every admin needs a Google account; dependency on an external provider. Changing an admin's Google account requires `reset-google` (CLI, F2-05).
- − Requires a Google Cloud project with an OAuth consent screen (free); redirect URIs for localhost and the production domain.
- − API on a different host from the site → requires the same registrable domain (ADR-0002).
- − Logging out on one device logs that admin out everywhere.
- − Data Protection keys in Postgres are not encrypted at rest by ASP.NET on Linux: anyone with database read access could forge a session. Mitigations: connection string only in secrets, separate key rings per environment, `session_version` and the 90-day cap. Certificate-protected keys can be added later.
- − Any XSS on the site could read `csrfToken` and act as the admin; a strict Content-Security-Policy is tracked as a Phase 5 task.

## Amendments
**2026-11-15 — F2-01 (Phase 2 contract closed with the owner, after three review rounds)**
| Topic | Before | Now | Why |
|---|---|---|---|
| Session lifetime | Proposal: 8 h sliding, max 7 days | 30 days sliding (renewed after half the window), absolute max 90 days | Owner decisions |
| Logout / revocation | Logout deletes the cookie | `session_version` in `admin_users`, incremented by logout and `remove-admin` (all devices) | A stateless cookie stays valid after logout; owner decision |
| Google account binding | Email only | `google_sub` bound on first login | Owner decision; email addresses can change hands |
| CSRF token delivery | Readable `XSRF-TOKEN` cookie | `csrfToken` in the `GET /api/admin/me` body, enforced by an explicit filter | The site cannot read host-only API cookies; ASP.NET does not validate JSON endpoints by default |
| Login errors | 429 JSON from the login endpoint | Redirect with `?error=rate_limited` / `login_failed` | Login is a full-page navigation (code review of PR #56) |
| OIDC details | Not specified | `code` + `query` + PKCE, no saved tokens | Matches the GET callback; keeps tokens out of the cookie |
| Proxy / hosts / CORS | `AllowedHosts: *`; not specified | Forwarded headers from the ingress only, restricted hosts, exact CORS origin | Code review of PR #53; TLS ends at the ingress |
| Data Protection keys | Not specified | Persisted in Postgres (EF Core package) | Sessions must survive restarts and scale-to-zero |
| `admin_users` | id, email, display_name, is_active, created_at | + `last_login_at`, `session_version`, `google_sub`; email lower-case unique | Owner decisions |
