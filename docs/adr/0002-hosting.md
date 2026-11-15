# ADR-0002 — Hosting: Azure Container Apps (API) + Cloudflare Pages (site)

**Status:** Accepted · 2026-11-14 · Free limits verified 2026-11-14 (issue F1-08)

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

## Free limits verified on 2026-11-14
| Limit | Value | Source |
|---|---|---|
| Container Apps Consumption free grant (per subscription, per calendar month) | 180,000 vCPU-s · 360,000 GiB-s · 2 M HTTP requests | [Billing docs](https://learn.microsoft.com/en-us/azure/container-apps/billing), [Pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/) |
| What the grant buys | ≈ 200 h/month of a running replica at 0.25 vCPU / 0.5 GiB (≈ 100 h at 0.5 vCPU / 1 GiB) | Derived from the grant |
| Scaled to zero (`minReplicas: 0`) | No resource charges; idle rate only applies when `minReplicas > 0` | [Billing docs](https://learn.microsoft.com/en-us/azure/container-apps/billing) |
| Billable requests | Only from outside the environment; health probes are not billed | [Billing docs](https://learn.microsoft.com/en-us/azure/container-apps/billing) |
| Budgets | Available on pay-as-you-go subscriptions; **alert only** (resources are not stopped); evaluated every ~24 h, data delayed 8–24 h | [Budgets tutorial](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/tutorial-acm-create-budgets) |
| Log Analytics (if used for container logs) | 5 GB ingestion/month free per billing account; 31 days retention | [Azure Monitor pricing](https://azure.microsoft.com/en-us/pricing/details/monitor/) |
| Cloudflare Pages (Free) | 500 builds/month (Pages CI), 1 concurrent build, 20,000 files/site, 25 MiB/file, 100 custom domains/project | [Pages limits](https://developers.cloudflare.com/pages/platform/limits/) |
| Pages Direct Upload | Wrangler from GitHub Actions (`cloudflare/wrangler-action`, token with *Cloudflare Pages: Edit*); a Direct Upload project **cannot switch to Git integration later** | [Direct Upload](https://developers.cloudflare.com/pages/get-started/direct-upload/), [CI guide](https://developers.cloudflare.com/pages/how-to/use-direct-upload-with-continuous-integration/) |
| GitHub Actions | Free for standard GitHub-hosted runners in public repositories | [Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions) |
| GitHub Container Registry | Free for public packages; container storage and bandwidth "currently free" (≥ 1 month notice before change) | [Packages billing](https://docs.github.com/en/billing/concepts/product-billing/github-packages) |

Result: the hosting decision fits ~zero monthly cost for the expected traffic.

### Cost risks and mitigations
- **Replica kept awake** (bots, uptime pingers, a `minReplicas > 0` mistake) burns the vCPU/GiB grant → keep `minReplicas: 0`, `maxReplicas: 1`, smallest size (0.25 vCPU / 0.5 GiB); no external uptime pinger; the public site never calls the API at runtime (ADR-0007).
- **Budget alerts do not stop spending** and arrive with up to ~24–48 h delay → subscription budget of a few EUR with actual and forecast alerts at low thresholds (e.g. 1 EUR); review Cost Management monthly.
- **Hidden Azure resources** (Log Analytics over 5 GB, Azure Container Registry, custom VNet, Dedicated workload profile) → no ACR (use public GHCR image), no VNet, Consumption profile only, Log Analytics with a daily cap or console-only logs; structured logs kept terse.
- **GHCR image must be public** for an anonymous pull (new packages default to private) → set package visibility to public; the image contains no secrets.
- **Pages project type** → create it as Direct Upload (built in GitHub Actions, ADR-0007); Pages CI build quota is then not on the critical path. Watch the 20,000-file limit only if the catalogue grows to thousands of pages.
- GitHub may change GHCR/Actions pricing → re-check before Phase 7 deploy.
