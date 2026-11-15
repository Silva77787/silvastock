# Back office (`/admin`) — page layouts

SPA under `/admin` in the same Next.js project, calling the API with the session cookie. Figma: page **Layouts**, sections **04–07** (section 08 — photos & documents — is specified here; Figma frames pending, see README). Components: [`components.md`](components.md). Copy: [`copy.md`](copy.md).

Routes (pt-PT frontend URLs):

| Route | Screen | API |
|---|---|---|
| `/admin/login` | Login | `GET /api/auth/login/google?returnUrl=…` |
| `/admin` | Painel (dashboard) | `GET /api/admin/me`, `GET /api/admin/dashboard`, `GET /api/admin/vehicles?status=Draft&sort=updated_desc&pageSize=5` |
| `/admin/viaturas` | Inventário | `GET /api/admin/vehicles` (`q`, `status`, `sort`, `page`, `pageSize=24`) |
| `/admin/viaturas/nova` | Nova viatura | `POST /api/admin/vehicles` |
| `/admin/viaturas/{id}` | Viatura (form + fotos + documentos) | `GET/PUT/DELETE /api/admin/vehicles/{id}`, transitions, images, documents |

Admin URL query params: inventory `?q=&estado=rascunho|publicada|vendida&ordem=&pagina=`.

## 0. Global admin behaviour

- **Auth guard:** on load call `GET /api/admin/me`. 401 → redirect to `/admin/login` (keep `returnUrl`); 403 → login page with the not-allowed alert. Any later 401 → same redirect with the "A sua sessão terminou." alert. The UI is not a security boundary — it just reacts to API responses.
- **Cold start:** the API may take several seconds to wake (Azure min 0). Every request shows its loading state; after 3 s show, under the skeleton, "A ligar ao servidor… pode demorar alguns segundos." (`bodySm` secondary). Never time out before 30 s.
- **Mutations:** buttons show loading; success → toast; 400 → field errors inline (from `errors`); 409 → toast (copy.md) + refetch; 422 → publish banner (§4.3); 5xx/network → error toast, keep user input.
- **Antiforgery:** every state-changing request sends `X-XSRF-TOKEN` (not a visual concern; listed so it is not forgotten).
- **Site rebuild notice:** after publish / unpublish / sold / edit-while-published, the success toast says the site updates in 1–3 minutes.
- Page background `bg.subtle`; content cards `bg.surface`. `<html lang="pt-PT">`, `<title>` "{Página} — Back office SilvaStock". `noindex` meta on all admin pages.

---

## 1. Login — `/admin/login` (Figma 04)

```
           bg.subtle, full viewport, content centred both ways
        ┌ card 400 (mobile: 100% − 32) ───────────────┐
        │ [S] SilvaStock                               │  padding 32, gap 24,
        │ H2 Entrar no back office                     │  radius xl, shadow md,
        │ Acesso reservado aos administradores…        │  1px border.default
        │ [Alert — only on error/429/expired]          │
        │ [ G  Entrar com Google ]  (52 tall, full w)  │
        │ Ao entrar, é usada a sua conta Google…       │  bodySm secondary
        └──────────────────────────────────────────────┘
```

- The page heading is visually an `h2` style but is the page's `<h1>`.
- Button: use **Google's official "Sign in with Google" button asset/branding** (light theme, "Entrar com Google" label, Google "G" logo, 52px tall here, full width, radius `md`, 1px `border.strong`). It is a plain link to `GET /api/auth/login/google?returnUrl=/admin/...` (full-page navigation, not fetch).
- States (Alert above the button, full width):
  - `?error=not_allowed` → Danger "Não foi possível entrar." + generic text (never says whether the email exists).
  - 429 → Warning "Demasiadas tentativas."
  - Session expired (redirect with `?sessao=expirada`) → Info "A sua sessão terminou."
- Mobile: card width `100% − 32`, same content; vertically centred with min top padding 24.
- If `/api/admin/me` already returns 200, redirect to `/admin` immediately.

---

## 2. Shell (Figma 05)

### Desktop (≥ 1024)

```
┌ sidebar 240 (sticky, 100vh) ┐┌ main (bg.subtle) ─────────────────────────┐
│ [S] SilvaStock / Back office ││ padding 32 top, 40 sides, 80 bottom       │
│                              ││ content max-width 1120 (left-aligned;     │
│ ▣ Painel                     ││ centred when the viewport is wider)       │
│ ▢ Inventário                 ││                                            │
│ ▢ Nova viatura               ││                                            │
│                              ││                                            │
│ ───────────────              ││                                            │
│ Ana Silva                    ││                                            │
│ ana@exemplo.pt               ││                                            │
│ Terminar sessão (ghost sm)   ││                                            │
└──────────────────────────────┘└────────────────────────────────────────────┘
```

- Sidebar: bg `bg.surface`, right 1px `border.subtle`, padding 20/16, gap 24. Logo block: 32px mark + "SilvaStock" (`h4`) / "Back office" (`caption` secondary). `<nav aria-label="Principal">` with NavItems (gap 4). User block pinned to the bottom (top border `border.subtle`, padding-top 12, gap 8): display name (`label`), email (`caption` secondary), Ghost sm "Terminar sessão" (`POST /api/auth/logout` → `/admin/login`).
- "Inventário" is active on `/admin/viaturas*` except `/admin/viaturas/nova` (→ "Nova viatura").

### Tablet and mobile (< 1024)

- Top bar 56 (components §21) + drawer 288 with the same content as the sidebar.
- Main padding: 24 (tablet) / 16 (mobile); bottom 32 (+ save bar height when visible).

### Page header pattern (all admin pages)

Row: title block (H1 + optional subtitle `body` secondary) left, actions right (gap 12), align centre; margin-bottom 24. Mobile: stacked, primary action full width below the title.

---

## 3. Painel — `/admin` (Figma 05)

- Header: H1 "Painel", subtitle "Olá, {primeiro nome}." (from `displayName`), action Primary "+ Nova viatura".
- **Stats:** 4 StatCards — Total, Rascunho, Publicadas, Vendidas (`total`, `draft`, `published`, `sold`). Desktop 4 columns gap 24; tablet and mobile 2×2 gap 12. Each card links to the inventory (`?estado=…`; Total → no filter).
- **Rascunhos recentes:** card (bg `bg.surface`, border, radius `lg`) with header row ("Rascunhos recentes" `h4` + "Ver todos" link → `/admin/viaturas?estado=rascunho`, padding 16/20, bottom border) and up to 5 rows: thumb 64×48 radius `md` (`primaryThumbUrl` or placeholder) · title (`label`: make model version) + "Atualizado {relative}" (`bodySm` secondary) · StatusBadge (hidden on mobile) · chevron. Row padding 12/20, bottom border `border.subtle`, whole row is a link; hover bg `bg.muted`.
- Empty drafts: the card shows "Não há rascunhos. Bom trabalho." (`bodySm` secondary, padding 20).
- Loading: StatCards show skeleton values (bar 48×36); list shows 3 skeleton rows.
- Error: EmptyState error variant inside the content with "Tentar novamente".

---

## 4. Inventário — `/admin/viaturas` (Figma 06)

### Desktop

- Header: H1 "Inventário", subtitle "{totalCount} viaturas", Primary "+ Nova viatura".
- Toolbar (gap 16, align centre, margin-bottom 16): search TextField without visible label (has `aria-label="Pesquisar viaturas"`; placeholder "Marca, modelo, matrícula ou VIN"; width 320; search on Enter and 300 ms debounce) · status tabs (segmented control: track `bg.muted` radius `md` padding 4; tabs 36 tall, padding-x 16; selected tab bg `bg.surface` + `shadow-sm` + `text.primary`; others `text.secondary`; counts from the dashboard endpoint) · spacer · sort Select (width 220, `aria-label="Ordenar"`).
- **Table** (`<table>` with `<caption class="sr-only">Inventário</caption>`), container bg `bg.surface`, 1px `border.default`, radius `lg`, overflow hidden.
  - `<thead>` bg `bg.muted`, padding 12/16, text `caption` secondary, bottom border.
  - Rows: padding 8/16, min-height 64, bottom 1px `border.subtle`, hover bg `bg.muted`. Gap between cells 16.
  - Columns (widths): Foto 64 (thumb 64×48 radius `md`) · **Viatura** (fills; title `label` `text.link` is the `<a>` to `/admin/viaturas/{id}` — make + model; version below `bodySm` secondary) · Matrícula 96 · Ano 48 (right) · Km 96 (right) · Preço 104 (right; "Sob consulta" when `priceOnRequest`) · Estado 104 (StatusBadge) · Fotos 48 (right; `imageCount`) · Atualizado 96 (`dd/MM/yyyy`) · Ações 44 (menu, components §11).
  - Missing values: "—".
- Below: "A mostrar {a}–{b} de {n}" left, Pagination right.
- Row actions menu: items depend on status (copy.md §4); destructive and state-changing items open ConfirmDialog as listed in copy.md; "Ver no site" opens `/carros/{id}-…` in a new tab (only Published).

### Tablet (768–1023)

Search full width; second row: tabs + spacer + sort. Table keeps only: Foto · Viatura · Preço · Estado · Atualizado · Ações (Matrícula moves under the version line as `caption` secondary).

### Mobile (< 768)

- Header stacked; Primary full width. Search full width. Tabs full width (4 equal tabs, `caption` text).
- List of cards instead of the table: card bg `bg.surface`, border, radius `lg`, padding 12, gap 12: thumb 88×66 · text column (StatusBadge; title `label`; `{ano} · {km} · {preço}` `bodySm` secondary; `Matrícula {x} · {n} fotos` `caption` secondary) · actions menu 44×44. Card is a link (except the menu button). Gap between cards 12.
- Pagination mobile variant.

### States (frames "A carregar", "Vazio", "Erro ao carregar")

- Loading: 4–6 skeleton rows/cards, `aria-busy`.
- Empty (no vehicles at all): EmptyState "Ainda não há viaturas" + [Nova viatura].
- Empty search/filter: "Nenhuma viatura corresponde à pesquisa." + [Limpar pesquisa].
- Error: EmptyState error + [Tentar novamente] (copy mentions cold start).

---

## 5. Viatura — `/admin/viaturas/{id}` and `/admin/viaturas/nova` (Figma 07)

One page, sections in this order: **Dados gerais · Especificações · Equipamento · Fotos · Documentos · Dados internos**. Each section is a card (bg `bg.surface`, 1px `border.default`, radius `lg`, padding 24, gap 20; header = `h3` title + optional `bodySm` secondary description, gap 4). Gap between cards 24.

### 5.1 Layout

**Desktop:** header across the content width (1120). Below: form column **880** + gap 32 + in-page nav **184** (sticky top 32): overline "NESTA PÁGINA" (`caption` secondary) then anchor links (padding 8/12, radius `md`; current section — via IntersectionObserver — uses `action.selected` bg/fg). Field grid inside cards: **2 columns**, column gap 20, row gap 20; fields marked "full" span both columns.

**Tablet:** no in-page nav; cards full width (720); field grid 2 columns.

**Mobile:** cards full width (343 at 375), field grid **1 column**. Card padding 16.

### 5.2 Page header

- Back link "← Inventário" (`bodySm` `text.link`, 32 tall).
- Title row: H1 "{Marca} {Modelo}" + StatusBadge (gap 12); meta line "N.º {id} · Atualizado a {data-hora} por {email}" (`bodySm` secondary). New vehicle: H1 "Nova viatura", no badge/meta.
- Status actions on the right (mobile: below, left-aligned, wrap), based on `status`:

| Status | Actions (left → right) |
|---|---|
| (new) | — (only the save bar "Guardar") |
| Draft | Ghost **Eliminar** (danger confirm) · Primary **Publicar** |
| Published | Ghost **Ver no site** · Secondary **Despublicar** · Secondary **Marcar como vendida** |
| Sold | Secondary **Reabrir como rascunho** (no confirmation) |

  If the form has unsaved changes, transition buttons first ask to save ("Guarde as alterações antes de publicar." inline toast) — the save bar stays visible.
- **Readiness banner (Draft only):** when `publishReadiness` is not empty, show Alert Warning "Faltam dados para publicar." with the missing items as links that scroll to and focus the field (for `primaryImage` → the Fotos section). When empty, show nothing and Publicar is enabled. Publicar stays clickable even with missing data (the server decides; 422 is handled below).

### 5.3 Publish failure (422) — frame "Erro ao publicar (422) — Mobile 375"

Alert Danger at the top replaces the warning: "O veículo não pode ser publicado." + "Corrija os campos assinalados: {lista}". Each `missingFields` entry → its field shows the error state with the message from `errors[field]` (server pt-PT text). Focus moves to the alert (`tabindex="-1"`). Status does not change.

### 5.4 Dados gerais (fields, grid order)

| Field | Control | Width | Notes |
|---|---|---|---|
| Marca * | TextField | 1 col | required (only make + model are required to save) · max 60 |
| Modelo * | TextField | 1 col | required · max 60 |
| Versão | TextField | full | max 120 |
| Ano | TextField `inputmode=numeric` | 1 col | 1900 … current year + 1 · placeholder "Ex.: 2019" |
| Quilómetros | TextField numeric | 1 col | 0 … 2 000 000 · helper "Em km." · formatted on blur (`98 000`) |
| Preço (€) | TextField `inputmode=decimal` | 1 col | 0 … 99 999 999,99 · placeholder "Ex.: 18 900" · disabled when "Preço sob consulta" is checked |
| Combustível | Select | 1 col | FuelType labels |
| Caixa | Select | 1 col | Transmission labels |
| Preço sob consulta | Checkbox | full | label "Preço sob consulta (oculta o preço no site)" |
| Descrição | Textarea (min 160) | full | max 5000 · counter · helper about line breaks |

### 5.5 Especificações

Cilindrada (cm³) · Potência (kW, live helper "≈ {cv} cv") · Carroçaria (Select) · Tração (Select) · Cor exterior (max 40) · Cor interior (max 40) · Portas (1–7) · Lugares (1–9) · Emissões (max 40, helper "Ex.: Euro 6d ou 120 g/km"). All optional, 2-column grid.

### 5.6 Equipamento

Row: TextField "Adicionar equipamento" (fills) + Secondary "Adicionar" (aligned to the input bottom). Enter in the field adds too. Below: chips (FilterChip style) in insertion order, each removable. Duplicates (case-insensitive) rejected with inline error. Max 100 items × 100 chars. Saved as the `equipment` array on "Guardar".

### 5.7 Fotos (section 08 — photos management)

- Header: "Fotos" + count "{n} de 30" (`bodySm` secondary) + description "A primeira foto é a principal no site. Arraste para reordenar."
- Uploader (components §15) full width, then the PhotoTile grid (`repeat(auto-fill, minmax(176px, 1fr))`, gap 16; mobile 2 columns gap 12). Primary first.
- New vehicle (not saved yet): Uploader disabled with "Guarde a viatura para poder adicionar fotos."
- Empty: Uploader + text "Ainda não há fotos. A primeira foto enviada fica como principal."
- 30 photos: Uploader disabled, hint "Atingiu o limite de 30 fotos."
- Upload flow: select/drop → client checks → `POST …/images` (multipart, all files at once) with progress → on 201 replace the grid with the returned list + toast "Fotos enviadas." → on 400/413/415 Uploader error state (nothing stored).
- Photo changes save immediately (they are not part of the save bar).

### 5.8 Documentos (private documents section)

- Header: lock icon + "Documentos" + description "Privados: nunca aparecem no site."
- Upload form (one document at a time), grid: Categoria (Select, required, DocumentCategory labels) · Nome (TextField, optional, max 200, placeholder "Se ficar vazio, usa o nome do ficheiro") · file picker (Secondary "Escolher ficheiro" + chosen file name `bodySm` + hint "PDF, JPEG ou PNG · máx. 15 MB") · Primary sm "Enviar documento" (disabled until a file and a category are chosen). Mobile: 1 column.
- List: DocumentRows (components §17) sorted by `uploadedAt` desc inside the card (top border). Empty: "Ainda não há documentos." (`bodySm` secondary).
- New vehicle: section disabled with "Guarde a viatura para poder adicionar documentos."
- Document changes save immediately (not part of the save bar).

### 5.9 Dados internos

Card with a lock icon before the title; description "Visíveis apenas no back office. Nunca aparecem no site." Fields: Matrícula (max 16) · VIN (17 chars, pattern `^[A-HJ-NPR-Z0-9]{17}$`, uppercase on input, helper "17 caracteres, sem I, O ou Q.") · Notas internas (Textarea full, max 5000, counter).

### 5.10 Saving

- "Guardar" sends the whole `VehicleInput` (`PUT` for existing, `POST` for new). New vehicle → after 201 navigate to `/admin/viaturas/{id}` (toast "Viatura criada."), where Fotos and Documentos become available.
- Save bar (components §23) appears when the form is dirty; mobile: full width, only "Guardar".
- Errors (400): fields in error state + focus first invalid field; no banner needed unless the field is not visible (then an Alert Danger at the top listing them).

### 5.11 Delete

Only for Draft (button and menu item hidden otherwise). ConfirmDialog Danger → `DELETE` → toast "Viatura eliminada." → navigate to `/admin/viaturas`.
