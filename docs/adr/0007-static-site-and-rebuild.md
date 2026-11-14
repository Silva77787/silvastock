# ADR-0007 — Static public site + rebuild via `workflow_dispatch`

**Status:** Accepted · 2026-11-14

## Context
The API scales to zero (cold start). The public catalogue should have no cold start and must be indexable. The inventory changes a few times a day.

## Decision
- Next.js is exported as **static HTML** (`output: 'export'`). At build time the site reads `GET /api/public/vehicles` (all pages) and generates:
  - the catalogue page with the data of every published vehicle embedded (JSON);
  - one page per vehicle via `generateStaticParams` (`/carros/{id}-{text}`), with metadata/OG and JSON-LD.
- **Catalogue filtering, search, sorting and pagination run in the browser** over that data.
- When a vehicle is published, unpublished, sold or reopened, or a **published** vehicle is edited (including photos), the API calls the **GitHub API `workflow_dispatch`** for the `deploy-site.yml` workflow, which builds and publishes to Cloudflare Pages (1–3 min).
  - The trigger is **asynchronous and debounced** (e.g. it groups changes for 60 s) so a build is not started on every click; a failed trigger is logged and does not fail the admin's request.
  - Least-privilege GitHub token (fine-grained PAT with only `actions:write` on this repo), stored as a secret.
- The back office (`/admin`) is an SPA in the same export, calling `api.<domain>` at runtime with a cookie.

## Consequences
- + Zero cold start and excellent performance/SEO on the public side; the site keeps working when the API is down.
- − Changes take 1–3 min to appear. A sold vehicle stays visible during that window (accepted; the detail page could optionally confirm the status client-side — outside the MVP).
- − The build depends on the API being awake (a cold start in CI is acceptable).
- − With hundreds of vehicles the catalogue JSON grows; reassess (pagination at build time or client-side API calls) beyond ~1,000 vehicles. The public endpoint keeps server-side pagination for that.
- Cloudflare Pages free limits (builds/month) and GitHub Actions minutes: builds run on GitHub Actions and only the upload goes to Pages.
