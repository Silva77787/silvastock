# ADR-0001 — Stack: .NET 10 + Next.js, modular monolith

**Status:** Accepted · 2026-11-14

## Context
Portfolio project with potential real-world use, for 2–3 admin users. The author is proficient in C#/.NET and React. SEO matters for the public catalogue, and the cost must be close to zero.

## Decision
- **Backend:** ASP.NET Core **.NET 10 (LTS)**, EF Core + Npgsql. **Modular monolith** in 4 layers: `Domain`, `Application`, `Infrastructure`, `Api`, plus a `Cli` project for administration.
- **Frontend:** **Next.js + React + TypeScript** with `output: 'export'` (static HTML). One project for both the public site and the back office under `/admin`.
- **Tests:** xUnit, Testcontainers (Postgres), Vitest + React Testing Library, Playwright. Assertion library to be chosen in F1-02 (FluentAssertions 8+ is commercially licensed).
- **CI/CD:** GitHub Actions (public repository → free minutes).

## Consequences
- + Builds on existing skills; a single API deployment; easy to test end to end.
- + Next.js provides routing, `generateStaticParams` and per-page metadata for SEO without a Node server in production.
- − No SSR: no API routes, middleware or server actions in Next.js (see ADR-0007).
- − Logical modules (Vehicles, Media, Documents, Identity, Audit) are folders/namespaces, not separate projects, to avoid over-engineering. Split later if it grows.
- Microservices, queues and distributed caches are explicitly excluded.
