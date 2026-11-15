# SilvaStock — Approved decisions (Claude Code handoff)

> This file takes precedence over every other document (`REQUIREMENTS.md`, ADRs, OpenAPI) in case of conflict.
> The original specification (Notion) is summarised in `REQUIREMENTS.md`.
> Date: 2026-11-14. Product name: **SilvaStock**.

## 1. Product
Public showroom + back office for ONE used-car dealership, used by the owner and family members (2–3 people).
Goals: professional portfolio and possible real-world use. Currency: EUR.
Language: documentation, code and GitHub in **English**. The API is always in English: routes/resources (e.g. `/api/admin/vehicles`, never `/carros`), DTOs, fields and enums. **Portuguese (pt-PT)**: the frontend UI and its URLs (e.g. `/carros/`), and the user-facing API error messages (ProblemDetails `title`/`detail`, validation messages).
Modular monolith. No microservices. No features outside the MVP without justification.

## 2. Constraints
- Production cost close to zero (the only accepted cost is the domain, bought later).
- Cold start is acceptable in the back office. Preferably none in the public catalogue.
- Domain: **deferred**. Everything is developed on localhost; the first deployment needs a domain
  (the session cookie must be on the same site as the API).
- **Public** GitHub repository (free GitHub Actions).

## 3. Stack (confirmed)
| Layer | Choice |
|---|---|
| Backend | C# / ASP.NET Core **.NET 10** (LTS), EF Core, Npgsql |
| Frontend | **Next.js** (React + TypeScript) with `output: 'export'` (static), `generateStaticParams` for car pages |
| Database | **PostgreSQL on Neon** (free). Do not use Supabase (pauses after 7 days of inactivity) |
| API hosting | **Azure Container Apps**, minReplicas 0, maxReplicas 1, budget alert |
| Site hosting | **Cloudflare Pages** |
| Files | **Cloudflare R2**: public bucket (photos) + private bucket (documents), via the S3 SDK |
| CI/CD | GitHub Actions |
| Tests | xUnit + integration (Testcontainers/Postgres), Vitest, Playwright |

## 4. Architecture
- Static site: catalogue and car pages generated at build time from the API; filtering in the browser.
- When a car is published/sold/edited while published, the API triggers a GitHub Actions `workflow_dispatch` to rebuild the site (1–3 min).
- Back office: SPA under `/admin` in the same Next.js project, calling `api.<domain>` with a cookie.
- API: layered modular monolith (Domain, Application, Infrastructure, Api).

## 5. Authentication
- **Google (OIDC)** login + cookie session (`HttpOnly`, `Secure`, `SameSite=Lax`) + antiforgery. No JWT in localStorage.
- **Allowlist**: `admin_users` table (email). If the Google-verified email is not in the table → reject without creating a session.
- User management via CLI commands (`add-admin`, `remove-admin`). A management screen comes later.
- Check `email_verified`. Rate limiting on login.
- Every `/api/admin/*` endpoint requires authorisation; tests prove 401/403 without a valid session.
- **Confirmed by the owner (2026-11-14):** Google (OIDC) login. No email + password (no ASP.NET Identity).

## 6. Data model (PostgreSQL)
- `admin_users`: id, email (unique), display_name, is_active, created_at.
- `vehicles`: **id bigint identity (PK)**, status (Draft|Published|Sold, CHECK), published_at, sold_at,
  make, model, version, year, mileage_km, price (decimal 10,2), price_on_request, fuel, transmission,
  engine_cc, power_kw, body_type, color (exterior), interior_color, doors, seats, drivetrain, emissions, description,
  equipment (`text[]`, list of extras, optional — added in Phase 0),
  **private:** registration_number, vin, internal_notes; created_at, updated_at, created_by, updated_by.
- `vehicle_images`: id, vehicle_id, object_key, thumb_key, content_type, size_bytes, sort_order,
  is_primary (partial unique index: 1 primary per vehicle).
- `vehicle_documents`: id, vehicle_id, category, display_name, object_key, content_type, size_bytes, uploaded_at, uploaded_by.
- `audit_log`: at, actor, entity, entity_id, action, from_status, to_status.
- Indexes: (status, published_at), (make, model), year, price.
- **No slug.** Public URL: `/carros/{id}-{generated-text}` (e.g. `/carros/42-bmw-320d-2019`; `carros` stays in Portuguese for the local audience and SEO). Only the `id` is used for lookup; the text is cosmetic.
- **Outside the MVP:** Leads, DealershipProfile (name/logo live in the frontend).
- **Contact on the site (confirmed 2026-11-14):** no channel at all (no phone, WhatsApp, email or form). Only a fixed message in the frontend: "Contacto pelo Messenger".

## 7. State rules
Transitions: Draft→Published (validated), Published→Draft, Published→Sold, Sold→Draft (**no extra confirmation**).
Delete only in Draft.
To save as Draft: only **make and model** are required (confirmed 2026-11-14).
Invalid transition (e.g. selling a Draft) → 409; publishing with missing fields → 422.
To publish: make, model, year, mileage, fuel, transmission, a valid price OR "price on request", ≥1 valid primary photo.
Description is **optional**.

## 8. Files
- Photos: validate extension + MIME + magic bytes; re-encode (ImageSharp) to strip EXIF; WebP thumbnails; random key; public bucket. Max 10 MB, 30 per vehicle.
- Documents: private bucket; download via a short-lived pre-signed URL, only after checking the session and the `vehicleId` (anti-IDOR). Max 15 MB.
- Never public: registration number, VIN, internal notes, documents. Separate public and admin DTOs.

## 9. API (source of truth: `docs/api/openapi.yaml`)
Errors as ProblemDetails (RFC 9457). Pagination `{items,page,pageSize,totalCount}`.
- Public: `GET /api/public/vehicles` (q, minPrice, maxPrice, year, maxKm, fuel, transmission, sort, page, pageSize); `GET /api/public/vehicles/{id}` (404 if not Published).
- Session: `GET /api/auth/login/google`, callback, `POST /api/auth/logout`, `GET /api/admin/me`, `GET /api/admin/dashboard`.
- Admin: CRUD `/api/admin/vehicles`; `POST …/{id}/publish|unpublish|sold|reopen` (422 with missing fields).
- Images: upload, `PATCH …/images/order`, `PUT …/images/{id}/primary`, `DELETE`.
- Documents: `POST`, `GET` list, `GET …/{docId}/download`, `DELETE`.

## 10. Subagents (start with 4) in `.claude/agents/`
| Agent | May change | May not |
|---|---|---|
| `backend` | `src/` (Api, Domain, Application, Infrastructure) | migrations without main-session approval |
| `frontend` | `web/` (implements what is in `docs/design/`) | decide the look |
| `qa-security` | `tests/` | product code (reports only) |
| `reviewer` | nothing (read-only, fresh context) | anything |

Main session: owns the OpenAPI, migrations, integration, final review, **CI/CD and infra** (`.github/`, Dockerfile, `infra/`).
Two agents never edit the same files. Each returns: summary, changed files, tests, pending decisions.
**Design** is a separate session (not a subagent) that produces `docs/design/` (tokens, per-page layouts, components).

## 11. Phased backlog (tracked as GitHub milestones and issues)
0. Docs: REQUIREMENTS, ADRs, OpenAPI, CLAUDE.md, agents — owner approval.
1. Foundation: .NET solution + Next.js, CI, config/secrets, structured logs, `/health`. Green CI, no secrets in the repo.
2. DB + domain + Google login + allowlist + CLI. Admin endpoints return 401 without a session.
3. Vehicle CRUD + transitions. Tests cover every transition and the publish validation.
4. Photos. Fake files rejected; EXIF stripped.
5. Public showroom (list, filters, detail, SEO, WCAG 2.2 AA). Published only, no private fields.
6. Private documents. No download without a session; IDOR tested.
7. Dashboard, E2E, security review, deploy, backups and a **tested restore**.

Each phase starts by closing that phase's API contract with the owner (`openapi.yaml` is a draft until then).
Definition of Done: unit + integration tests passing, authorisation verified, no secrets in the repo.

## 12. Still open
- Domain (.com on Cloudflare ~$11/year vs .pt) — decide in Phase 7, before the first deployment.
- ~~Login: Google vs Identity~~ → resolved: Google (see §5 and ADR-0004).
- ~~Free-tier prices/quotas~~ → verified on 2026-11-14 (ADRs 0002, 0003, 0005): everything fits ~zero cost. Budget alerts do not cap spending; production photos need our own subdomain (`r2.dev` is development-only).
- **ImageSharp licence (owner decision):** free use requires the repo to be open source/source-available (no `LICENSE` yet) or the dealership to have < 1M USD annual revenue; recent versions also need a free build-time licence key. Decide before F4-03.

## 13. Accounts
Neon: created. Azure: created. Cloudflare: to confirm. Google Cloud: only in the login phase.
