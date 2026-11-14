# ADR-0003 — Database: PostgreSQL on Neon

**Status:** Accepted · 2026-11-14 · **Free-tier quotas to re-check (issue F1-08)**

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
