# ADR-0002 — Hosting: Azure Container Apps (API) + Cloudflare Pages (site)

**Status:** Accepted · 2026-11-14 · **Free-tier quotas to re-check (issue F1-08)**

## Context
Production cost close to zero (only the domain is accepted). The public catalogue should have no cold start; the back office may. Low, intermittent traffic.

## Decision
- **API:** Docker container on **Azure Container Apps** (Consumption plan), `minReplicas: 0`, `maxReplicas: 1`, with a **budget alert** on the subscription.
- **Site (public + /admin):** static files on **Cloudflare Pages**.
- **Domain:** deferred. `www.<domain>` (or apex) → Pages; `api.<domain>` → Container Apps. Everything on localhost until then.

## Consequences
- + Public catalogue served from a CDN, with no cold start and independent of the API (see ADR-0007).
- + Scale to zero → ~zero cost within the monthly Container Apps free grant.
- − The first back-office request after inactivity hits an API cold start (seconds) — accepted.
- − `maxReplicas: 1` → rate limiting and other in-memory structures work, but there is no high availability (accepted for the MVP).
- − The cookie session requires the site and the API to share the same *site* (same registrable domain) → **the first deployment needs our own domain**. Do not use `*.pages.dev` + `*.azurecontainerapps.io` in production.
- Container image: GitHub Container Registry (free for a public repo), avoiding the Azure Container Registry cost.
