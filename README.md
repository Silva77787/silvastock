# SilvaStock

Public showroom and back office for a single used-car dealership.

- **Public site:** a fast, static catalogue of published vehicles with in-browser search and filters, plus a detail page per vehicle.
- **Back office (`/admin`):** inventory management (drafts, publishing, sold vehicles), photos and private documents, for 2–3 administrators who sign in with Google.

Portfolio project built to production standards on a ~zero monthly budget.

## Stack

| Layer | Technology |
|---|---|
| API | C# / ASP.NET Core .NET 10, EF Core, PostgreSQL (Neon) |
| Frontend | Next.js + React + TypeScript, static export (Cloudflare Pages) |
| Files | Cloudflare R2 (public photos, private documents) |
| Hosting | Azure Container Apps (scale to zero) |
| Tests | xUnit, Testcontainers, Vitest, Playwright |
| CI/CD | GitHub Actions |

Architecture and decisions: [`docs/DECISIONS.md`](docs/DECISIONS.md), [`docs/adr/`](docs/adr/README.md). API contract: [`docs/api/openapi.yaml`](docs/api/openapi.yaml). Requirements: [`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md).

## Status

Phase 1 (Foundation) in progress. The backlog lives in the [SilvaStock — MVP project](https://github.com/users/Silva77787/projects/5), with one milestone per phase.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS or newer) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (local Postgres/MinIO and integration tests)
- Git

## Getting started

```bash
git clone https://github.com/Silva77787/silvastock.git
cd silvastock
cp .env.example .env   # local-only values; never commit .env
```

The commands below become available as the Phase 1 tasks land:

| What | Command | Available after |
|---|---|---|
| Start Postgres + MinIO | `docker compose up -d` | F1-04 |
| Build and test the API | `dotnet build` · `dotnet test` | F1-02 / F1-05 |
| Run the API | `dotnet run --project src/SilvaStock.Api` | F1-03 |
| Frontend dev server | `cd web && npm install && npm run dev` | F1-06 |
| Frontend tests | `cd web && npm test` | F1-06 |

## Repository layout

```
src/        .NET solution (Domain, Application, Infrastructure, Api, Cli)
tests/      unit, integration and end-to-end tests
web/        Next.js app (public site + /admin)
docs/       decisions, ADRs, requirements, API contract, design
infra/      deployment scripts
```

## Contributing

- Work happens on short-lived branches with one PR per issue (`Closes #N`); `main` only receives reviewed, green changes.
- Secrets never go into Git: use `dotnet user-secrets` and `.env` locally, Azure/GitHub secrets in production.
- Working rules for humans and AI agents: [`CLAUDE.md`](CLAUDE.md).

## Language

Code, docs and API resources are in English. The user interface is in Portuguese (pt-PT).
