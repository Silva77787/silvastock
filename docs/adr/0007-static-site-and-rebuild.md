# ADR-0007 — Site público estático + rebuild por `workflow_dispatch`

**Estado:** Aceite · 2026-11-14

## Contexto
A API escala a zero (cold start). O catálogo público não deve ter cold start e deve ser indexável. O inventário muda poucas vezes por dia.

## Decisão
- O Next.js é exportado como **HTML estático** (`output: 'export'`). No build, o site lê `GET /api/public/vehicles` (todas as páginas) e gera:
  - a página de catálogo com os dados de todos os veículos publicados embutidos (JSON);
  - uma página por veículo via `generateStaticParams` (`/carros/{id}-{texto}`), com metadata/OG e JSON-LD.
- **Filtros, pesquisa, ordenação e paginação do catálogo correm no browser** sobre esses dados.
- Quando um veículo é publicado, retirado, vendido, reaberto ou um veículo **publicado** é editado (incluindo fotos), a API chama a **GitHub API `workflow_dispatch`** do workflow `deploy-site.yml`, que constrói e publica no Cloudflare Pages (1–3 min).
  - Disparo **assíncrono e com debounce** (ex.: agrupa alterações durante 60 s) para não lançar um build por cada clique; falha no disparo é registada em log e não falha o pedido do admin.
  - Token GitHub de permissão mínima (fine-grained PAT só com `actions:write` neste repo), guardado como segredo.
- O backoffice (`/admin`) é uma SPA no mesmo export, que chama `api.<dominio>` em runtime com cookie.

## Consequências
- + Zero cold start e excelente performance/SEO no público; o site continua a funcionar com a API em baixo.
- − Alterações demoram 1–3 min a aparecer. Um veículo vendido continua visível durante esse intervalo (aceite; a página de detalhe pode, opcionalmente, confirmar estado no cliente — fora do MVP).
- − O build depende da API acordada (cold start no CI é aceitável).
- − Com centenas de veículos o JSON do catálogo cresce; reavaliar (paginação no build ou API no cliente) se ultrapassar ~1 000 veículos. O endpoint público mantém paginação no servidor para isso.
- Limites do Cloudflare Pages free (builds/mês) e minutos do GitHub Actions: builds acontecem no GitHub Actions e só o upload vai para o Pages.
