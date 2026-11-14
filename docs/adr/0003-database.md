# ADR-0003 — Database: PostgreSQL on Neon

**Status:** Accepted · 2026-11-14 · Free limits verified 2026-11-14 (issue F1-08)

## Context
We need a free, managed relational database with good EF Core support, for a small volume (tens to hundreds of vehicles).

## Decision
- **PostgreSQL** on **Neon** (free plan) in production.
- **Do not use Supabase:** it pauses projects after 7 days of inactivity.
- Local: Postgres in Docker. Integration tests: **Testcontainers** with a real Postgres (not SQLite/InMemory).
- Schema managed by **EF Core migrations**, applied as an explicit pipeline step (not at API startup). Migrations are owned by the main session.
- `snake_case` naming; `timestamptz` timestamps in UTC; price as `numeric(10,2)`; status with a `CHECK` constraint.

## Consequences
- + Postgres is standard and portable (leaving Neon is a `pg_dump`).
- − Neon also suspends compute after inactivity: the first request has extra latency (~seconds). This only affects the API/back office, not the static site.
- − Free-tier backups are limited (short restore window). **Phase 7:** scheduled GitHub Actions job with an encrypted `pg_dump` to the private R2 bucket, and a periodically **tested restore**.
- Connection string only in secrets (user-secrets locally, an Azure secret in production).

## Free limits verified on 2026-11-14
| Limit | Value | Source |
|---|---|---|
| Storage | 1 GB per project (20 GB across all projects) | [Neon pricing](https://neon.com/pricing) |
| Compute | 100 CU-hours per project per month; autoscaling up to 2 CU | [Neon pricing](https://neon.com/pricing) |
| Scale to zero | After 5 min of inactivity; fixed (cannot be disabled) on Free | [Scale to zero](https://neon.com/docs/introduction/scale-to-zero) |
| Cold start | Reactivates "within a few hundred milliseconds" | [Scale to zero](https://neon.com/docs/introduction/scale-to-zero) |
| Projects / branches | 100 projects; 10 branches per project | [Neon pricing](https://neon.com/pricing) |
| Restore window | 6 hours (1 GB limit); no instant restore | [Neon pricing](https://neon.com/pricing) |
| Egress | 5 GB per project per month | [Neon pricing](https://neon.com/pricing) |
| Over limit | Compute/egress limit → compute suspended until next month; storage limit → writes blocked. No charge, no data deleted | [Neon pricing](https://neon.com/pricing) |

Result: fits ~zero cost. Neon Free never bills; the risks are availability, not money.

### Cost risks and mitigations
- **Autoscaling to 2 CU** can burn 100 CU-h quickly (2 CU-h per hour) → cap the compute at 0.25–0.5 CU (≥ 200–400 active hours/month); scale to zero stays on.
- **Compute or egress exhausted → API down until next month** → no polling/keep-alive against the DB; `/health` must not query the DB on every probe (or use a cheap liveness probe); the static site does not depend on the DB.
- **6-hour restore window** is not a backup → Phase 7 encrypted `pg_dump` to the private R2 bucket with a tested restore (unchanged decision); the dump job wakes the compute briefly (negligible CU-h).
- **1 GB storage** → ample for hundreds of vehicles (images live in R2, not in the DB); prune `audit_log` only if it ever grows significantly.
