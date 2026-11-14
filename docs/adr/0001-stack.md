# ADR-0001 — Stack: .NET 10 + Next.js, monólito modular

**Estado:** Aceite · 2026-11-14

## Contexto
Projeto de portefólio com potencial de uso real, para 2–3 utilizadores admin. O autor domina C#/.NET e React. Requisitos de SEO no catálogo público e custo próximo de zero.

## Decisão
- **Backend:** ASP.NET Core **.NET 10 (LTS)**, EF Core + Npgsql. **Monólito modular** em 4 camadas: `Domain`, `Application`, `Infrastructure`, `Api`, mais um projeto `Cli` para administração.
- **Frontend:** **Next.js + React + TypeScript** com `output: 'export'` (HTML estático). Mesmo projeto para o site público e para o backoffice em `/admin`.
- **Testes:** xUnit + FluentAssertions, Testcontainers (Postgres), Vitest + React Testing Library, Playwright.
- **CI/CD:** GitHub Actions (repositório público → minutos grátis).

## Consequências
- + Aproveita competências existentes; um só deploy de API; fácil de testar de ponta a ponta.
- + Next.js dá rotas, `generateStaticParams` e metadata por página para SEO sem servidor Node em produção.
- − Sem SSR: nada de API routes, middleware ou server actions no Next.js (ver ADR-0007).
- − Módulos lógicos (Vehicles, Media, Documents, Identity, Audit) ficam em pastas/namespaces, não em projetos separados, para não sobre-engenheirar. Separar mais tarde se crescer.
- Microserviços, filas e cache distribuída estão explicitamente excluídos.
