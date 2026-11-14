# CLAUDE.md — SilvaStock

Showroom público + backoffice para **um** stand de usados (2–3 administradores). PT-PT, EUR.

## Fontes de verdade (por ordem de precedência)
1. `docs/DECISIONS.md` — decisões aprovadas. Prevalece sobre tudo o resto.
2. `docs/adr/` — uma decisão por ficheiro. Uma decisão nova ou alterada → ADR novo (ou `Superseded by`).
3. `docs/api/openapi.yaml` — contrato da API. Backend e frontend implementam **contra este ficheiro**.
4. `docs/REQUIREMENTS.md` — critérios de aceitação, requisitos não funcionais, testes e riscos.
5. `docs/design/` — aspeto visual (produzido pela sessão de Design). O frontend não decide aspeto.

Se encontrares uma contradição entre estas fontes, **pára e pergunta**. Não resolvas por conta própria.

## Stack
| Camada | Tecnologia |
|---|---|
| Backend | C# / ASP.NET Core .NET 10, EF Core, Npgsql. Monólito modular em camadas |
| Frontend | Next.js + React + TypeScript, `output: 'export'` (estático); backoffice SPA em `/admin` |
| BD | PostgreSQL (Neon em produção; Testcontainers nos testes; Docker local) |
| Ficheiros | Cloudflare R2 via SDK S3: bucket público (fotos) e privado (documentos) |
| Hosting | API: Azure Container Apps (min 0 / max 1). Site: Cloudflare Pages |
| CI/CD | GitHub Actions |
| Testes | xUnit (+ FluentAssertions), Testcontainers, Vitest, Playwright |

## Estrutura do repositório (alvo)
```
src/
  SilvaStock.Domain/          entidades, enums, regras de estado (sem dependências externas)
  SilvaStock.Application/     casos de uso, DTOs, validação, interfaces (portas)
  SilvaStock.Infrastructure/  EF Core, migrações, R2, Google OIDC, GitHub dispatch
  SilvaStock.Api/             endpoints, auth, ProblemDetails, composição
  SilvaStock.Cli/             add-admin / remove-admin
tests/
  SilvaStock.UnitTests/
  SilvaStock.IntegrationTests/  (Testcontainers/Postgres, WebApplicationFactory)
web/                          Next.js (site público + /admin), Vitest
tests/e2e/                    Playwright
docs/                         DECISIONS, REQUIREMENTS, adr/, api/, design/
infra/                        IaC / scripts de deploy
.github/workflows/
```
Dependências entre camadas: `Api → Application → Domain`; `Infrastructure → Application, Domain`. O Domain não referencia nada.

## Convenções
- **Código, identificadores, rotas, estados: inglês.** Texto visível ao utilizador: PT-PT. Docs: PT-PT.
- C#: nullable ativado, `TreatWarningsAsErrors`, `dotnet format`. Async em todo o I/O. Sem lógica de negócio em controllers/endpoints.
- TypeScript: `strict`, ESLint + Prettier. Tipos da API gerados a partir do `openapi.yaml` (não escrever à mão).
- Tabelas e colunas em `snake_case`; propriedades C# em `PascalCase`.
- Datas sempre em UTC (`timestamptz`); apresentar em `Europe/Lisbon`.
- Dinheiro: `decimal(10,2)`. Nunca `float`/`double`.
- Unidades explícitas no nome: `mileage_km`, `engine_cc`, `power_kw`.
- Erros: ProblemDetails (RFC 9457). Validação → 400; regra de estado/publicação → 422 com campos em falta.
- Paginação: `{ items, page, pageSize, totalCount }`.
- Commits pequenos, mensagem em inglês, imperativo (`Add vehicle publish validation`).

## Regras de segurança (inegociáveis)
- **Nunca** expor na API pública ou no site: matrícula, VIN, notas internas, documentos, dados de utilizadores. DTOs públicos e admin são classes distintas; nunca devolver entidades EF.
- Todos os `/api/admin/*` exigem sessão de um email na tabela `admin_users` ativo. Autorização no backend; o frontend não é fronteira de segurança.
- Sessão por cookie `HttpOnly`, `Secure`, `SameSite=Lax` + antiforgery em pedidos que alteram estado. **Nunca** tokens em `localStorage`.
- Uploads: validar extensão + MIME + magic bytes; limites de tamanho; chaves de storage aleatórias; re-codificar imagens (remove EXIF).
- Documentos: só bucket privado; download por URL pré-assinada curta, emitida após verificar sessão e que o documento pertence ao `vehicleId` (anti-IDOR).
- Nenhum segredo no Git (connection strings, chaves R2, client secret Google, PAT GitHub). Local: `dotnet user-secrets` / `.env.local` (ignorado). Produção: segredos do Azure / GitHub.
- Logs estruturados sem segredos, tokens, VIN, matrícula ou conteúdos de documentos.
- Nunca usar dados reais (pessoas, matrículas, VIN, documentos) em testes, fixtures ou screenshots.

## Definition of Done
Uma tarefa só está concluída quando:
- O comportamento está implementado de ponta a ponta, com validação no backend e erros tratados (sem UI de demonstração com dados hardcoded).
- Tem testes unitários **e** de integração; **todos os testes passam** (`dotnet test`, `npm test`).
- A autorização foi verificada (testes de 401/403 para endpoints novos em `/api/admin/*`).
- Não há segredos nem dados reais no repositório.
- `openapi.yaml`, ADRs e docs foram atualizados se o comportamento mudou.
- A sessão principal reviu a alteração (e o `reviewer` para alterações relevantes).

## Nunca fazer
- Criar microserviços, filas, caches distribuídas ou multi-tenancy.
- Acrescentar funcionalidades fora do MVP (Leads, DealershipProfile, estado Reserved, favoritos, comparação…) sem aprovação explícita.
- Adicionar uma dependência sem justificar a necessidade e confirmar que não existe alternativa no projeto.
- Alterar `openapi.yaml`, migrações, `.github/`, `Dockerfile` ou `infra/` a partir de um subagente.
- Usar serviços pagos ou sair do free tier sem aprovação do dono.
- Usar Supabase (pausa por inatividade).
- Fazer push para `main`, force push, ou commits sem pedido do utilizador.

## Delegação a subagentes
Agentes em `.claude/agents/`. A sessão principal é dona de: `docs/api/openapi.yaml`, migrações EF, integração, revisão final, CI/CD e infra (`.github/`, `Dockerfile`, `infra/`).

| Agente | Pode alterar | Quando usar |
|---|---|---|
| `backend` | `src/` (exceto migrações) | Implementar endpoints/casos de uso já definidos no OpenAPI |
| `frontend` | `web/` | Implementar páginas/componentes definidos em `docs/design/` |
| `qa-security` | `tests/` (inclui `tests/e2e/`) | Testes de integração, autorização, IDOR, uploads, E2E; reporta falhas |
| `reviewer` | nada | Revisão com contexto novo antes de fechar uma fase ou alteração sensível |

Regras:
- Só delegar depois de o contrato (OpenAPI + modelo) da funcionalidade estar fechado.
- Dois agentes nunca editam os mesmos ficheiros ao mesmo tempo.
- Cada brief indica: objetivo, ficheiros/pastas permitidos, contrato a seguir, critérios de aceitação.
- Cada agente devolve: resumo, ficheiros alterados, testes executados (com resultado), decisões pendentes.
- A sessão principal **re-executa os testes** antes de aceitar. "Terminei" de um agente não é prova.
- Se um agente precisar de mudar o contrato ou uma migração, devolve o pedido à sessão principal em vez de o fazer.

## Comandos (a preencher na Fase 1)
```
dotnet build / dotnet test
cd web && npm run dev / npm test
cd tests/e2e && npx playwright test
```
