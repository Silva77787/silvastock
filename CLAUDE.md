# CLAUDE.md — SilvaStock

Public showroom + back office for **one** used-car dealership (2–3 administrators). EUR.

## Language
- **English:** code, identifiers, commits, docs, ADRs, GitHub issues/PRs, and every API resource: routes (`/api/admin/vehicles`, never `/carros`), DTOs, fields, enums, statuses, log messages.
- **Portuguese (pt-PT):** all frontend UI text, frontend URLs (e.g. `/carros/42-bmw-320d-2019`), and user-facing API error messages (ProblemDetails `title`/`detail`, validation messages).

## Sources of truth (in order of precedence)
1. `docs/DECISIONS.md` — approved decisions. Overrides everything else.
2. `docs/adr/` — one decision per file. A new or changed decision → new ADR (or `Superseded by`).
3. `docs/api/openapi.yaml` — API contract. Backend and frontend implement **against this file**. Each phase closes its part of the contract with the owner before implementation.
4. `docs/REQUIREMENTS.md` — acceptance criteria, non-functional requirements, tests and risks.
5. `docs/design/` — visual design (produced by the Design session). The frontend does not decide the look.
6. GitHub issues (`[F<phase>-<n>]`) — task specs and status.

If you find a contradiction between these sources, **stop and ask**. Do not resolve it on your own.

## Stack
| Layer | Technology |
|---|---|
| Backend | C# / ASP.NET Core .NET 10, EF Core, Npgsql. Layered modular monolith |
| Frontend | Next.js + React + TypeScript, `output: 'export'` (static); back-office SPA under `/admin` |
| DB | PostgreSQL (Neon in production; Testcontainers in tests; Docker locally) |
| Files | Cloudflare R2 via the S3 SDK: public bucket (photos) and private bucket (documents) |
| Hosting | API: Azure Container Apps (min 0 / max 1). Site: Cloudflare Pages |
| CI/CD | GitHub Actions |
| Tests | xUnit, Testcontainers, Vitest, Playwright |

## Repository layout (target)
```
src/
  SilvaStock.Domain/          entities, enums, state rules (no external dependencies)
  SilvaStock.Application/     use cases, DTOs, validation, interfaces (ports)
  SilvaStock.Infrastructure/  EF Core, migrations, R2, Google OIDC, GitHub dispatch
  SilvaStock.Api/             endpoints, auth, ProblemDetails, composition
  SilvaStock.Cli/             add-admin / remove-admin
tests/
  SilvaStock.UnitTests/
  SilvaStock.IntegrationTests/  (Testcontainers/Postgres, WebApplicationFactory)
  e2e/                          Playwright
web/                          Next.js (public site + /admin), Vitest
docs/                         DECISIONS, REQUIREMENTS, adr/, api/, design/
infra/                        IaC / deploy scripts
.github/workflows/
```
Layer dependencies: `Api → Application → Domain`; `Infrastructure → Application, Domain`. Domain references nothing.

## Conventions
- C#: nullable enabled, `TreatWarningsAsErrors`, `dotnet format`. Async for all I/O. No business logic in endpoints.
- TypeScript: `strict`, ESLint + Prettier. API types generated from `openapi.yaml` (never hand-written).
- Tables and columns in `snake_case`; C# properties in `PascalCase`.
- Dates always in UTC (`timestamptz`); displayed in `Europe/Lisbon`.
- Money: `decimal(10,2)`. Never `float`/`double`.
- Units explicit in names: `mileage_km`, `engine_cc`, `power_kw`.
- Errors: ProblemDetails (RFC 9457). Validation → 400; invalid state transition → 409; publish rules → 422 with missing fields.
- Pagination: `{ items, page, pageSize, totalCount }`.
- Small commits, imperative English messages (`Add vehicle publish validation`). PRs reference their issue (`Closes #N`).

## Security rules (non-negotiable)
- **Never** expose in the public API or on the site: registration number, VIN, internal notes, documents, user data. Public and admin DTOs are separate classes; never return EF entities.
- Every `/api/admin/*` endpoint requires a session for an email that is active in `admin_users`. Authorisation in the backend; the frontend is not a security boundary.
- Cookie session `HttpOnly`, `Secure`, `SameSite=Lax` + antiforgery on state-changing requests. **Never** tokens in `localStorage`.
- Uploads: validate extension + MIME + magic bytes; size limits; random storage keys; re-encode images (strips EXIF).
- Documents: private bucket only; download via a short-lived pre-signed URL, issued after checking the session and that the document belongs to the `vehicleId` (anti-IDOR).
- No secrets in Git (connection strings, R2 keys, Google client secret, GitHub PAT). Locally: `dotnet user-secrets` / `.env.local` (ignored). Production: Azure / GitHub secrets.
- Structured logs without secrets, tokens, VIN, registration numbers or document contents.
- Never use real data (people, registration numbers, VINs, documents) in tests, fixtures or screenshots.

## Definition of Done
A task is done only when:
- The behaviour is implemented end to end, with backend validation and error handling (no demo UI with hard-coded data).
- It has unit **and** integration tests; **all tests pass** (`dotnet test`, `npm test`).
- Authorisation has been verified (401/403 tests for new `/api/admin/*` endpoints).
- There are no secrets or real data in the repository.
- `openapi.yaml`, ADRs and docs are updated if behaviour changed.
- The main session reviewed the change (and the `reviewer` for significant changes).
- The issue's acceptance criteria are all checked and the PR closes the issue.

## Never
- Create microservices, queues, distributed caches or multi-tenancy.
- Add features outside the MVP (Leads, DealershipProfile, Reserved status, favourites, comparison…) without explicit approval.
- Add a dependency without justifying it and confirming there is no existing alternative in the project.
- Change `openapi.yaml`, migrations, `.github/`, `Dockerfile` or `infra/` from a subagent.
- Use paid services or exceed the free tier without the owner's approval.
- Use Supabase (pauses on inactivity).
- Push to `main`, force push, or commit without the user asking.

## Delegating to subagents
Agents live in `.claude/agents/`. The main session owns: `docs/api/openapi.yaml`, EF migrations, integration, final review, CI/CD and infra (`.github/`, `Dockerfile`, `infra/`).

| Agent | May change | When to use |
|---|---|---|
| `backend` | `src/` (except migrations) | Implement endpoints/use cases already defined in the OpenAPI |
| `frontend` | `web/` | Implement pages/components defined in `docs/design/` |
| `qa-security` | `tests/` (incl. `tests/e2e/`) | Integration, authorisation, IDOR, upload and E2E tests; reports failures |
| `reviewer` | nothing | Fresh-context review before closing a phase or a sensitive change |

Rules:
- Only delegate once the feature's contract (OpenAPI + model) is closed.
- Two agents never edit the same files at the same time.
- Every brief states: goal, allowed files/folders, contract to follow, acceptance criteria (link the GitHub issue).
- Every agent returns: summary, changed files, tests run (with results), pending decisions.
- The main session **re-runs the tests** before accepting. An agent saying "done" is not proof.
- If an agent needs to change the contract or a migration, it hands the request back to the main session instead.

## Commands (to be filled in during Phase 1)
```
dotnet build / dotnet test
cd web && npm run dev / npm test
cd tests/e2e && npx playwright test
```
