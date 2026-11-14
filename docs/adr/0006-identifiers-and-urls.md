# ADR-0006 — IDs `bigint identity` e URLs públicos sem slug

**Estado:** Aceite · 2026-11-14

## Contexto
URLs públicos devem ser estáveis e legíveis para SEO. Slugs persistidos complicam edições (marca/modelo mudam) e exigem unicidade e redirecionamentos.

## Decisão
- `vehicles.id` é **`bigint GENERATED ALWAYS AS IDENTITY`**. Restantes tabelas também usam `bigint identity`.
- **Sem coluna slug.** URL público: `/carros/{id}-{texto-gerado}` (ex.: `/carros/42-bmw-320d-2019`). O texto é gerado no frontend a partir de marca, modelo, versão e ano (minúsculas, sem acentos, `-`).
- Só o `id` é usado para obter o veículo; o texto é cosmético. A página declara `<link rel="canonical">` com o texto atual.

## Consequências
- + Editar marca/modelo não parte links (o id mantém-se); sem lógica de unicidade.
- − IDs sequenciais revelam o volume aproximado de inventário e são enumeráveis. Aceite: a API pública só devolve `Published`, e todos os endpoints admin exigem autorização e verificam a relação veículo↔recurso (anti-IDOR).
- − Com site estático, como `generateStaticParams` gera um caminho por veículo, `/carros/42-texto-antigo` não existe como ficheiro. Mitigação: regra de redirect/`404.html` no Cloudflare Pages que extrai o id e redireciona para o caminho atual (detalhe na Fase 5).
