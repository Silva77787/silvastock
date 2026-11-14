# Architecture Decision Records

Formato curto: Contexto · Decisão · Consequências. Estado: `Proposto` → `Aceite` → (`Substituído por ADR-xxxx`).
Os ADRs derivam de `docs/DECISIONS.md` (2026-11-14). Uma alteração de decisão gera um ADR novo; o antigo fica marcado como substituído.

| ADR | Título | Estado |
|---|---|---|
| [0001](0001-stack.md) | Stack: .NET 10 + Next.js, monólito modular | Aceite |
| [0002](0002-hosting.md) | Hosting: Azure Container Apps + Cloudflare Pages | Aceite (quotas a reverificar) |
| [0003](0003-database.md) | Base de dados: PostgreSQL no Neon | Aceite (quotas a reverificar) |
| [0004](0004-authentication.md) | Autenticação: Google OIDC + cookie + allowlist | Aceite |
| [0005](0005-file-storage.md) | Ficheiros: Cloudflare R2, bucket público e privado | Aceite |
| [0006](0006-identifiers-and-urls.md) | IDs bigint e URLs sem slug | Aceite |
| [0007](0007-static-site-and-rebuild.md) | Site estático + rebuild por workflow_dispatch | Aceite |
