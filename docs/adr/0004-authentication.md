# ADR-0004 — Autenticação: Google OIDC + sessão por cookie + allowlist

**Estado:** Aceite · 2026-11-14 (confirmado pelo dono)

## Contexto
Só 2–3 administradores, sem registo público. A alternativa (ASP.NET Identity com email + palavra-passe) obriga a gerir passwords, lockout e recuperação de conta (SMTP ou procedimento manual).

## Decisão
- **Login com Google (OpenID Connect)** no backend ASP.NET Core (`/api/auth/login/google` → Google → callback).
- **Allowlist:** tabela `admin_users`. Depois do callback, se `email_verified` não for `true` ou o email não existir em `admin_users` com `is_active = true` → **recusar sem criar sessão** e redirecionar para `/admin/login?error=not_allowed`.
- **Sessão:** cookie de autenticação ASP.NET, `HttpOnly`, `Secure`, `SameSite=Lax`, expiração deslizante (proposta: 8 h, máximo 7 dias — a confirmar). Sem JWT, sem tokens no browser.
- **CSRF:** antiforgery do ASP.NET. O frontend obtém o token (cookie legível `XSRF-TOKEN`) e envia-o no cabeçalho `X-XSRF-TOKEN` em `POST/PUT/PATCH/DELETE`.
- **Autorização:** policy `Admin` em todos os `/api/admin/*`; a cada pedido confirma que o utilizador continua ativo na allowlist (remover da tabela revoga o acesso).
- **Gestão:** comando CLI `SilvaStock.Cli add-admin <email> [--name]` / `remove-admin <email>` executado com a connection string de produção. Ecrã de gestão fica fora do MVP.
- **Rate limiting** (ASP.NET `RateLimiter`) nos endpoints `/api/auth/*`.
- **CORS:** só a origem do site, com credenciais.

## Consequências
- + Zero passwords guardadas; MFA e recuperação de conta ficam a cargo da Google.
- + Recuperação se todos ficarem sem acesso: correr `add-admin` com outra conta Google.
- − Cada admin precisa de conta Google; dependência de um fornecedor externo.
- − Requer projeto Google Cloud com OAuth consent screen (grátis); redirect URIs de localhost e do domínio de produção.
- − API em domínio diferente do site → requer o mesmo domínio registável (ADR-0002).
- Respostas: `401` sem sessão; `403` com sessão mas fora da allowlist/inativo. Testes provam ambos para cada endpoint admin.
