# Architecture Decision Records

Short format: Context · Decision · Consequences. Status: `Proposed` → `Accepted` → (`Superseded by ADR-xxxx`).
The ADRs derive from `docs/DECISIONS.md` (2026-11-14). A changed decision creates a new ADR; the old one is marked as superseded.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-stack.md) | Stack: .NET 10 + Next.js, modular monolith | Accepted |
| [0002](0002-hosting.md) | Hosting: Azure Container Apps + Cloudflare Pages | Accepted |
| [0003](0003-database.md) | Database: PostgreSQL on Neon | Accepted |
| [0004](0004-authentication.md) | Authentication: Google OIDC + cookie + allowlist | Accepted |
| [0005](0005-file-storage.md) | Files: Cloudflare R2, public and private buckets | Accepted |
| [0006](0006-identifiers-and-urls.md) | bigint IDs and slug-less URLs | Accepted |
| [0007](0007-static-site-and-rebuild.md) | Static site + rebuild via workflow_dispatch | Accepted |
