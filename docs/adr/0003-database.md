# ADR-0003 — Base de dados: PostgreSQL no Neon

**Estado:** Aceite · 2026-11-14 · **Quotas free a reverificar antes da Fase 2**

## Contexto
Precisamos de uma base relacional gerida e grátis, com bom suporte EF Core, para um volume pequeno (dezenas a centenas de veículos).

## Decisão
- **PostgreSQL** via **Neon** (plano free) em produção.
- **Não usar Supabase:** pausa projetos após 7 dias sem uso.
- Local: Postgres em Docker. Testes de integração: **Testcontainers** com Postgres real (não SQLite/InMemory).
- Esquema gerido por **migrações EF Core**, aplicadas por passo explícito no pipeline (não no arranque da API). Migrações são da responsabilidade da sessão principal.
- Convenção `snake_case`; timestamps `timestamptz` em UTC; preço `numeric(10,2)`; estado com `CHECK`.

## Consequências
- + Postgres é standard, portátil (sair do Neon é um `pg_dump`).
- − O Neon também suspende o compute após inatividade: o primeiro pedido tem latência extra (~segundos). Afeta só a API/backoffice, não o site estático.
- − Backups do free tier são limitados (janela curta de restore). **Fase 7:** job agendado no GitHub Actions com `pg_dump` cifrado para o bucket privado do R2, e **restore testado** periodicamente.
- Connection string só em segredos (user-secrets local, segredo no Azure em produção).
