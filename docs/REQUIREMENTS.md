# SilvaStock — Requisitos e critérios de aceitação

> Deriva da especificação original (Notion) e está alinhado com `DECISIONS.md` (2026-11-14).
> Em caso de conflito prevalece o `DECISIONS.md`. Campos, rotas e códigos HTTP: ver `api/openapi.yaml`.

## 1. Utilizadores
- **Visitante:** consulta, pesquisa, filtra e ordena veículos publicados; vê o detalhe e as fotos. Sem conta. Não acede ao backoffice nem a dados privados. O site mostra apenas a mensagem "Contacto pelo Messenger".
- **Administrador (2–3):** conta Google individual na allowlist, todos com as mesmas permissões. Gere o inventário, as fotos e os documentos.

## 2. Critérios de aceitação

**AC-01 — Login (Fase 2)**
- Dado um email Google verificado e ativo em `admin_users`, quando faz login, então acede ao backoffice.
- Um email fora da allowlist, inativo ou não verificado não cria sessão, e a mensagem é genérica (não revela se o email existe).
- Tentativas repetidas são limitadas (429).
- Sem sessão, qualquer `/api/admin/*` dá 401; com sessão fora da allowlist, dá 403.
- O logout invalida a sessão.

**AC-02 — Criar Draft (Fase 3)**
- Com sessão válida e marca + modelo preenchidos, a viatura é criada em `Draft` e não aparece na API pública nem no site.

**AC-03 — Publicar (Fase 3/4)**
- O backend valida marca, modelo, ano, km, combustível, caixa, preço válido OU "sob consulta", e ≥1 foto principal.
- Se passar: estado `Published`, `publishedAt` definido, rebuild do site disparado, viatura visível no site após o rebuild.
- Se falhar: 422 com a lista de campos em falta; o estado não muda.

**AC-04 — Retirar, vender, reabrir (Fase 3)**
- `Published → Draft` e `Published → Sold` retiram a viatura da API pública de imediato (404 no detalhe) e do site após o rebuild.
- A viatura vendida continua no inventário admin, com `soldAt`.
- `Sold → Draft` não pede confirmação extra e fica no audit log.
- Qualquer outra transição dá 409. Apagar só é permitido em `Draft`.

**AC-05 — Fotos (Fase 4)**
- Ficheiros com extensão, MIME ou conteúdo falsos são rejeitados (415).
- Acima de 10 MB ou de 30 fotos por veículo: rejeitado.
- As fotos guardadas não têm EXIF; existe thumbnail WebP; a chave de storage é aleatória.
- É possível reordenar, definir a principal e apagar.

**AC-06 — Documentos privados (Fase 6)**
- Um documento válido (PDF/JPEG/PNG, ≤15 MB) fica no bucket privado, com os metadados na BD.
- Sem sessão não há download, mesmo conhecendo IDs ou URL.
- Um documento de outro veículo dá 404 (anti-IDOR).
- A URL pré-assinada expira em ~60 s.

**AC-07 — Dados internos nunca públicos (todas as fases)**
- Numa viatura com matrícula, VIN, notas internas e documentos, nenhum destes dados aparece no JSON da API pública nem no HTML/JSON gerado do site.

## 3. Requisitos não funcionais
| ID | Requisito | Verificação |
|---|---|---|
| NFR-01 | LCP das páginas públicas < 2,5 s (mobile, 4G simulado) | Lighthouse na Fase 5 |
| NFR-02 | P95 dos endpoints normais < 500 ms com a API acordada (exclui uploads e cold start) | Teste simples na Fase 7 |
| NFR-03 | Paginação no catálogo e no inventário | Testes de integração |
| NFR-04 | HTTPS em todos os ambientes expostos | Deploy (Fase 7) |
| NFR-05 | Autorização no backend em todos os endpoints privados | Testes 401/403 |
| NFR-06 | Proteção contra brute force, CSRF, XSS, SQL injection, IDOR | Testes + revisão de segurança |
| NFR-07 | Uploads validados e limitados | Testes de upload |
| NFR-08 | Backups automáticos da BD e **restore testado** | Fase 7 |
| NFR-09 | Consistência storage ↔ BD em falhas parciais de upload/eliminação | Testes de integração |
| NFR-10 | Separação Domain / Application / Infrastructure / Api | Revisão |
| NFR-11 | Responsivo (mobile, tablet, desktop); browsers modernos | Playwright (viewports) |
| NFR-12 | SEO: URLs estáveis, `canonical`, metadata e OG por viatura, sitemap | Fase 5 |
| NFR-13 | Acessibilidade WCAG 2.2 AA nos fluxos principais | axe + revisão manual |
| NFR-14 | Logs estruturados e `/health`, sem segredos, tokens, VIN, matrícula ou dados pessoais | Revisão |

## 4. Estratégia de testes
- **Unitários (xUnit):** regras de domínio, transições e validação de publicação.
- **Integração (Testcontainers/Postgres + WebApplicationFactory):** endpoints, autorização, validação, persistência, filtros/paginação, DTOs públicos (verificar o JSON serializado), uploads inválidos, IDOR.
- **Frontend (Vitest + RTL):** formulários e validação; estados de loading, vazio e erro; filtros do catálogo.
- **E2E (Playwright, Fase 7):**
  1. Login admin (com o fornecedor OIDC simulado em ambiente de teste).
  2. Criar viatura em Draft.
  3. Carregar fotos e definir a principal.
  4. Carregar um documento privado.
  5. Publicar.
  6. Como visitante, ver a viatura no catálogo e no detalhe.
  7. Confirmar que documento, VIN, matrícula e notas não são públicos.
  8. Marcar como vendida.
  9. Confirmar que deixou de ser pública e continua no inventário admin.
- Dados de teste sempre fictícios.

## 5. Riscos de segurança e privacidade
1. **Exposição de documentos** (DUA, compra): contêm dados pessoais → bucket privado, URLs curtas, anti-IDOR.
2. **Exposição de dados internos:** DTOs públicos explícitos e testados.
3. **Uploads maliciosos:** validar o conteúdo real e re-codificar imagens.
4. **Brute force / abuso no login:** rate limiting; o login é delegado à Google.
5. **CSRF / XSS / IDOR:** antiforgery, escaping no React, verificação da relação veículo↔recurso.
6. **RGPD:** sem Leads no MVP, os únicos dados pessoais são os emails dos admins e os documentos. O site precisa de um aviso de privacidade mínimo (ex.: alojamento, logs) — rever na Fase 5.
7. **Backups:** testar a recuperação, não só a criação.
8. **Segredos:** só em user-secrets / segredos do Azure e do GitHub.
9. **Logs:** nunca registar tokens, documentos, VIN ou matrícula.

## 6. Fora do MVP (não implementar sem aprovação)
Leads e formulário de contacto · DealershipProfile editável · telefone/WhatsApp · estado Reserved · histórico completo de alterações (só o audit log mínimo de estados) · duplicar viatura · exportar CSV · favoritos/comparação · vídeos · funcionalidades de IA · multi-stand · ecrã de gestão de admins · notificações por email.
