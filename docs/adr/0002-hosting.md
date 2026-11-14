# ADR-0002 — Hosting: Azure Container Apps (API) + Cloudflare Pages (site)

**Estado:** Aceite · 2026-11-14 · **Quotas free a reverificar antes da Fase 7**

## Contexto
Custo de produção próximo de zero (só o domínio é aceite). O catálogo público não deve ter cold start; no backoffice é aceitável. Tráfego baixo e intermitente.

## Decisão
- **API:** contentor Docker em **Azure Container Apps** (plano Consumption), `minReplicas: 0`, `maxReplicas: 1`, com **alerta de orçamento** na subscrição.
- **Site (público + /admin):** ficheiros estáticos em **Cloudflare Pages**.
- **Domínio:** adiado. `www.<dominio>` (ou apex) → Pages; `api.<dominio>` → Container Apps. Tudo em localhost até lá.

## Consequências
- + Catálogo público servido por CDN, sem cold start, independente da API (ver ADR-0007).
- + Escala a zero → custo ~0 com o free grant mensal do Container Apps.
- − Primeiro pedido ao backoffice após inatividade sofre cold start da API (segundos) — aceite.
- − `maxReplicas: 1` → rate limiting e outras estruturas em memória funcionam, mas sem alta disponibilidade (aceite para o MVP).
- − A sessão por cookie exige que site e API partilhem o mesmo *site* (mesmo domínio registável) → **a primeira publicação precisa de domínio próprio**. Não usar `*.pages.dev` + `*.azurecontainerapps.io` em produção.
- Imagem de contentor: GitHub Container Registry (grátis para repo público), evitando o custo do Azure Container Registry.
