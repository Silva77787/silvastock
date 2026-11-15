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
| Files | Cloudflare R2 via the S3 SDK: public bucket (photos) and private bucket (documents); RustFS locally (ADR-0008) |
| Hosting | API: Azure Container Apps (min 0 / max 1). Site: Cloudflare Pages |
| CI/CD | GitHub Actions (build, tests, CodeQL SAST, dependency audits, secret scanning) |
| Tests | xUnit (plain `Assert`), Testcontainers, Vitest, Playwright |

## Repository layout
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
infra/                        IaC / deploy scripts, local helpers
.github/workflows/
```
Layer dependencies: `Application → Domain`; `Infrastructure → Application, Domain`; Domain references nothing. `Api` and `Cli` are **composition roots**: they reference `Application` and `Infrastructure` only to wire dependencies; business logic never lives there.

## Conventions
- C#: nullable enabled, `TreatWarningsAsErrors`, `dotnet format`. Async for all I/O. No business logic in endpoints.
- TypeScript: `strict`, ESLint + Prettier. API types generated from `openapi.yaml` (never hand-written; `npm run gen:api`).
- Tables and columns in `snake_case`; C# properties in `PascalCase`.
- Dates always in UTC (`timestamptz`); displayed in `Europe/Lisbon`.
- Money: `decimal(10,2)`. Never `float`/`double`.
- Units explicit in names: `mileage_km`, `engine_cc`, `power_kw`.
- Errors: ProblemDetails (RFC 9457). Validation → 400; invalid state transition → 409; publish rules → 422 with missing fields.
- Pagination: `{ items, page, pageSize, totalCount }`.
- Small commits, imperative English messages (`Add vehicle publish validation`). One branch and one PR per issue (`Closes #N`).

## Security rules (non-negotiable)
- **Never** expose in the public API or on the site: registration number, VIN, internal notes, documents, user data. Public and admin DTOs are separate classes; never return EF entities.
- Every `/api/admin/*` endpoint requires a session for an email that is active in `admin_users`. Authorisation in the backend; the frontend is not a security boundary.
- Cookie session `HttpOnly`, `Secure`, `SameSite=Lax` + antiforgery on state-changing requests. **Never** tokens in `localStorage`.
- Uploads: validate extension + MIME + magic bytes; size limits; random storage keys; re-encode images (strips EXIF).
- Documents: private bucket only; download via a short-lived pre-signed URL, issued after checking the session and that the document belongs to the `vehicleId` (anti-IDOR).
- No secrets in Git (connection strings, R2 keys, Google client secret, GitHub PAT). Locally: `dotnet user-secrets`, `.env` and `web/.env.local` (all ignored). Production: Azure / GitHub secrets.
- Structured logs without secrets, tokens, VIN, registration numbers or document contents.
- Never use real data (people, registration numbers, VINs, documents) in tests, fixtures or screenshots. Use reserved domains (`example.test`).

## Definition of Done
A task is done only when:
- The behaviour is implemented end to end, with backend validation and error handling (no demo UI with hard-coded data).
- It has unit **and** integration tests; **all tests pass** (`dotnet test`, `npm test`).
- Authorisation has been verified (401/403 tests for new `/api/admin/*` endpoints).
- There are no secrets or real data in the repository.
- `openapi.yaml`, ADRs and docs are updated if behaviour changed.
- The fresh-context `reviewer` approved it through the task workflow below (and `qa-security` for sensitive tasks).
- The issue's acceptance criteria are all checked, CI is green ("CI passed") and the PR closes the issue.

## Never
- Create microservices, queues, distributed caches or multi-tenancy.
- Add features outside the MVP (Leads, DealershipProfile, Reserved status, favourites, comparison…) without explicit approval.
- Add a dependency without justifying it and confirming there is no existing alternative in the project.
- Change `openapi.yaml`, migrations, `.github/`, `Dockerfile` or `infra/` from a subagent.
- Use paid services or exceed the free tier without the owner's approval.
- Use Supabase (pauses on inactivity).
- Merge PRs, push to `main` or force push. Commit and push only on a task branch, as part of the task workflow or when the user asks. (`main` is protected: PR + "CI passed" required.)

## Task workflow (develop → review loop → PR)
Every task (GitHub issue) is delivered with the `/deliver-task <task>` command (e.g. `/deliver-task F2-03` or `/deliver-task 13`). The main session orchestrates; subagents cannot start other subagents.

1. **Prepare (main session):** find the issue, check its dependencies are merged and that the phase contract is closed, update `main`, mark the issue *In Progress* on the board.
2. **Develop:** a fresh `backend` or `frontend` agent (own worktree and branch `<type>/<task-id>-<slug>`) implements the issue with its tests, runs build/tests, commits. It does not push yet.
3. **Review:** a **new** fresh-context `reviewer` reviews the branch diff against the docs and the issue. For sensitive tasks (auth, sessions, uploads, documents, public DTOs, secrets) a fresh `qa-security` agent also reviews it in parallel.
4. **Fix:** the main session sends the findings to the **same** developer agent (it keeps its context). It fixes each finding or justifies declining it, re-runs the tests and commits.
5. **Re-review:** a new reviewer receives the previous findings and the developer's responses, verifies them and reviews the new changes. Repeat 4–5.
6. **Stop rule:** the loop ends when the verdict is **approve** with no critical/high/medium findings open. Low findings go into the PR description (or a follow-up issue). **Max 3 review rounds**; if important findings remain, the main session stops and brings the disagreement to the owner.
7. **PR:** the developer agent pushes its branch and opens the PR (`Closes #N`, summary, tests, review rounds and remaining low findings). The main session checks CI and reports to the owner, who merges.

Independent tasks may run their loops in parallel; two agents never edit the same files.

## Subagents
Agents live in `.claude/agents/`. The main session owns: `docs/api/openapi.yaml`, EF migrations, integration, CI/CD and infra (`.github/`, `Dockerfile`, `infra/`), and the task workflow.

| Agent | May change | Role |
|---|---|---|
| `backend` | `src/` (except migrations) and the tests for its feature in `tests/` | Developer for API/Domain/Infrastructure/CLI tasks |
| `frontend` | `web/` (including its Vitest tests) | Developer for site and back-office tasks |
| `qa-security` | `tests/` (incl. `tests/e2e/`) | Security reviewer for sensitive tasks; cross-cutting security and E2E tests |
| `reviewer` | nothing | Fresh-context code reviewer in every task loop |

Rules:
- Only start a task once its contract (OpenAPI + model) is closed and its dependencies are merged.
- Every brief states: the issue, allowed files/folders, contract to follow, acceptance criteria.
- Every developer report contains: summary, changed files, tests run (with results), branch and commit, pending decisions.
- The main session checks the evidence (tests, CI) before accepting. An agent saying "done" is not proof.
- If an agent needs to change the contract or a migration, it hands the request back to the main session instead.

## Commands
```
docker compose up -d                       # local Postgres + RustFS (copy .env.example to .env first)
dotnet build                               # whole solution
dotnet test                                # unit + integration (Docker must be running)
dotnet format --verify-no-changes          # formatting as in CI
dotnet run --project src/SilvaStock.Api    # API on http://localhost:5080
cd web && npm run dev                      # Next.js dev server
cd web && npm run lint && npm run typecheck && npm test && npm run build
cd web && npm run gen:api                  # regenerate API types after changing openapi.yaml
```
