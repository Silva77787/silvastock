# SilvaStock — Decisões aprovadas (handoff para o Claude Code)

> Este ficheiro prevalece sobre todos os outros documentos (`REQUIREMENTS.md`, ADRs, OpenAPI) onde houver conflito.
> A especificação original (Notion) foi resumida em `REQUIREMENTS.md`.
> Data: 2026-11-14. Nome do produto: **SilvaStock**.

## 1. Produto
Showroom público + backoffice para UM stand de usados, usado pelo dono e familiares (2–3 pessoas).
Objetivos: portefólio profissional e eventual uso real. Idioma: PT-PT. Moeda: EUR.
Monólito modular. Sem microserviços. Sem funcionalidades fora do MVP sem justificação.

## 2. Restrições
- Custo de produção próximo de zero (único custo aceite: domínio, comprado mais tarde).
- Cold start aceitável no backoffice. No catálogo público, preferencialmente nenhum.
- Domínio: **adiado**. Desenvolver tudo em localhost; a primeira publicação precisa de domínio
  (cookie de sessão tem de ficar no mesmo site que a API).
- Repositório GitHub **público** (GitHub Actions grátis).

## 3. Stack (confirmada)
| Camada | Escolha |
|---|---|
| Backend | C# / ASP.NET Core **.NET 10** (LTS), EF Core, Npgsql |
| Frontend | **Next.js** (React + TypeScript) com `output: 'export'` (estático), `generateStaticParams` para as páginas de carro |
| Base de dados | **PostgreSQL no Neon** (free). Não usar Supabase (pausa após 7 dias sem uso) |
| API hosting | **Azure Container Apps**, minReplicas 0, maxReplicas 1, alerta de orçamento |
| Site hosting | **Cloudflare Pages** |
| Ficheiros | **Cloudflare R2**: bucket público (fotos) + bucket privado (documentos), via SDK S3 |
| CI/CD | GitHub Actions |
| Testes | xUnit + integração (Testcontainers/Postgres), Vitest, Playwright |

## 4. Arquitetura
- Site estático: catálogo e páginas de carro geradas no build a partir da API; filtros no browser.
- Ao publicar/vender/editar carro publicado, a API dispara `workflow_dispatch` no GitHub Actions para reconstruir o site (1–3 min).
- Backoffice: SPA em `/admin` no mesmo projeto Next.js, a chamar `api.<dominio>` com cookie.
- API: monólito modular em camadas (Domain, Application, Infrastructure, Api).

## 5. Autenticação
- Login **com Google (OIDC)** + sessão por cookie (`HttpOnly`, `Secure`, `SameSite=Lax`) + antiforgery. Sem JWT em localStorage.
- **Allowlist**: tabela `admin_users` (email). Se o email verificado pelo Google não estiver na tabela → recusar, sem criar sessão.
- Gestão de utilizadores por comando CLI (`add-admin`, `remove-admin`). Ecrã de gestão fica para depois.
- Verificar `email_verified`. Rate limiting no login.
- Todos os `/api/admin/*` exigem autorização; testes provam 401/403 sem sessão.
- **Confirmado pelo dono (2026-11-14):** login com Google (OIDC). Sem email + palavra-passe (sem ASP.NET Identity).

## 6. Modelo de dados (PostgreSQL)
- `admin_users`: id, email (único), display_name, is_active, created_at.
- `vehicles`: **id bigint identity (PK)**, status (Draft|Published|Sold, CHECK), published_at, sold_at,
  make, model, version, year, mileage_km, price (decimal 10,2), price_on_request, fuel, transmission,
  engine_cc, power_kw, body_type, color (exterior), interior_color, doors, seats, drivetrain, emissions, description,
  equipment (`text[]`, lista de extras, opcional — acrescentado na Fase 0),
  **privados:** registration_number, vin, internal_notes; created_at, updated_at, created_by, updated_by.
- `vehicle_images`: id, vehicle_id, object_key, thumb_key, content_type, size_bytes, sort_order,
  is_primary (índice único parcial: 1 principal por veículo).
- `vehicle_documents`: id, vehicle_id, category, display_name, object_key, content_type, size_bytes, uploaded_at, uploaded_by.
- `audit_log`: at, actor, entity, entity_id, action, from_status, to_status.
- Índices: (status, published_at), (make, model), year, price.
- **Sem slug.** URL público: `/carros/{id}-{texto-gerado}` (ex.: `/carros/42-bmw-320d-2019`). Só o `id` é usado na pesquisa; o texto é cosmético.
- **Fora do MVP:** Leads, DealershipProfile (nome/logo ficam no frontend).
- **Contactos no site (confirmado 2026-11-14):** nenhum canal (sem telefone, WhatsApp, email ou formulário). Só uma mensagem fixa no frontend: "Contacto pelo Messenger".

## 7. Regras de estado
Transições: Draft→Published (validado), Published→Draft, Published→Sold, Sold→Draft (**sem confirmação extra**).
Apagar só em Draft.
Para guardar em Draft: só **marca e modelo** obrigatórios (confirmado 2026-11-14).
Transição inválida (ex.: vender um Draft) → 409; publicação com campos em falta → 422.
Para publicar: marca, modelo, ano, km, combustível, caixa, preço válido OU "sob consulta", ≥1 foto principal válida.
Descrição **opcional**.

## 8. Ficheiros
- Fotos: validar extensão + MIME + magic bytes; re-codificar (ImageSharp) para remover EXIF; thumbnails WebP; chave aleatória; bucket público. Máx. 10 MB, 30 por veículo.
- Documentos: bucket privado; download por URL pré-assinada curta, só após verificar sessão e `vehicleId` (anti-IDOR). Máx. 15 MB.
- Nunca públicos: matrícula, VIN, notas internas, documentos. DTOs públicos e admin separados.

## 9. API (fonte de verdade: `docs/api/openapi.yaml`)
Erros em ProblemDetails (RFC 9457). Paginação `{items,page,pageSize,totalCount}`.
- Público: `GET /api/public/vehicles` (q, minPrice, maxPrice, year, maxKm, fuel, transmission, sort, page, pageSize); `GET /api/public/vehicles/{id}` (404 se não Published).
- Sessão: `GET /api/auth/login/google`, callback, `POST /api/auth/logout`, `GET /api/admin/me`, `GET /api/admin/dashboard`.
- Admin: CRUD `/api/admin/vehicles`; `POST …/{id}/publish|unpublish|sold|reopen` (422 com campos em falta).
- Imagens: upload, `PATCH …/images/order`, `PUT …/images/{id}/primary`, `DELETE`.
- Documentos: `POST`, `GET` lista, `GET …/{docId}/download`, `DELETE`.

## 10. Subagentes (começar com 4) em `.claude/agents/`
| Agente | Pode alterar | Não pode |
|---|---|---|
| `backend` | `src/` (Api, Domain, Application, Infrastructure) | migrações sem aprovação da sessão principal |
| `frontend` | `web/` (implementa o que está em `docs/design/`) | decidir aspeto |
| `qa-security` | `tests/` | código de produto (só reporta) |
| `reviewer` | nada (só leitura, contexto novo) | tudo |
Sessão principal: dona do OpenAPI, migrações, integração, revisão final, **CI/CD e infra** (`.github/`, Dockerfile, `infra/`).
Dois agentes nunca editam os mesmos ficheiros. Cada um devolve: resumo, ficheiros alterados, testes, decisões pendentes.
**Design** é uma sessão própria (não subagente) que produz `docs/design/` (tokens, layouts por página, componentes).

## 11. Backlog faseado
0. Docs: REQUIREMENTS, ADRs, OpenAPI, CLAUDE.md, agentes — aprovação do dono.
1. Fundação: solução .NET + Next.js, CI, config/segredos, logs estruturados, `/health`. CI verde, sem segredos no repo.
2. BD + domínio + login Google + allowlist + CLI. Endpoints admin dão 401 sem sessão.
3. CRUD de veículos + transições. Testes cobrem todas as transições e validação de publicação.
4. Fotos. Ficheiros falsos rejeitados; EXIF removido.
5. Showroom público (lista, filtros, detalhe, SEO, WCAG 2.2 AA). Só Published, sem campos privados.
6. Documentos privados. Sem sessão não há download; IDOR testado.
7. Dashboard, E2E, revisão de segurança, deploy, backups e **restore testado**.
Definition of Done: testes unitários + integração a passar, autorização verificada, sem segredos no repo.

## 12. Ainda por decidir
- Domínio (.com Cloudflare ~11 $/ano vs .pt) — decidir na Fase 7, antes da primeira publicação.
- ~~Login: Google vs Identity~~ → resolvido: Google (ver §5 e ADR-0004).
- Preços/quotas free (Azure Container Apps, Neon, R2): **reverificar antes de implementar**.

## 13. Contas
Neon: criada. Azure: criada. Cloudflare: confirmar. Google Cloud: só na fase de login.
