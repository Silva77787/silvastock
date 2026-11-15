# Public site — page layouts

Static site (Next.js `output: 'export'`). Figma: page **Layouts**, sections **01–03**. Components: [`components.md`](components.md). Copy: [`copy.md`](copy.md). Breakpoints: mobile < 768, tablet 768–1023, desktop ≥ 1024 (reference frames 375, 768, 1440).

## 0. Page shell (all public pages)

```
┌ Skip link (hidden until focus) ───────────────────────────┐
│ PublicHeader (64, sticky)                                  │
├────────────────────────────────────────────────────────────┤
│ <main id="conteudo">  container max 1200 + gutters         │
│   …page content…                                           │
├────────────────────────────────────────────────────────────┤
│ PublicFooter ("Contacto pelo Messenger", privacy, ©)       │
└────────────────────────────────────────────────────────────┘
```

- Container: `max-width: 1200px; margin: 0 auto; padding-inline: 16 / 24 / 32` (mobile / tablet / desktop).
- `<html lang="pt-PT">`. One `<h1>` per page. Landmarks: `header`, `main`, `footer`, `nav`.
- Page background: catalogue and states `bg.subtle`; detail and privacy `bg.canvas`.
- Main padding: top 24 (mobile) / 32; bottom 40 (mobile) / 64.
- Footer always at the bottom (min-height layout: `body` flex column, `main` flex 1).

---

## 1. Catalogue — `/` (Figma 01)

Data: all published vehicles fetched at build time from `GET /api/public/vehicles`; filtering, sorting and pagination run **in the browser** (no API call at runtime). State lives in the URL query so results are shareable and the back button works:

| Filter | Query param | Values |
|---|---|---|
| Search | `q` | free text; matches make, model, version; case- and accent-insensitive |
| Min / max price | `precoMin`, `precoMax` | integers in euros; vehicles with "Preço sob consulta" are **excluded** when a price filter is set |
| Min year | `ano` | year |
| Max km | `kmMax` | 25000 / 50000 / 75000 / 100000 / 150000 / 200000 |
| Fuel | `combustivel` | gasolina, gasoleo, hibrido, hibrido-plug-in, eletrico, gpl, outro |
| Transmission | `caixa` | manual, automatica |
| Sort | `ordem` | recentes (default), preco-asc, preco-desc, ano-desc, ano-asc — "Preço sob consulta" goes last when sorting by price |
| Page | `pagina` | 1… (24 per page) |

Invalid values are ignored (fall back to defaults). Without JavaScript the page still shows the first 24 vehicles (static HTML); filtering, sorting and pagination need JavaScript.

### Desktop (≥ 1024) — frame "Catálogo — Desktop 1440"

```
Header
┌ container 1200 ───────────────────────────────────────────────────────┐
│ H1 "Viaturas disponíveis"                          [Ordenar por ▾ 240] │
│ "42 viaturas" (bodySm secondary, aria-live=polite)                     │
│                                                                        │
│ ┌ aside 280 ──────┐  32  ┌ results (888) ───────────────────────────┐ │
│ │ "Filtros" h4    │      │ [Gasóleo ×] [Até 20 000 € ×] Limpar      │ │
│ │ Pesquisar       │      │ ┌card┐ 24 ┌card┐ 24 ┌card┐               │ │
│ │ Preço mín|máx   │      │ │280 │    │    │    │    │               │ │
│ │ Ano mínimo      │      │ └────┘    └────┘    └────┘  (gap 24/24)  │ │
│ │ Quilómetros máx │      │ … 24 per page                            │ │
│ │ Combustível     │      │      ‹ Anterior  1 2 3 … 8  Seguinte ›   │ │
│ │ Caixa           │      │      A mostrar 1–24 de 42 viaturas       │ │
│ └─────────────────┘      └──────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
Footer
```

- Title row: H1 + count on the left, sort select (width 240, label "Ordenar por") right-aligned to the bottom of the title block. Gap to body 24.
- Aside: bg `bg.surface`, 1px `border.default`, radius `lg`, padding 20, gap 20; fields full width; price min/max side by side (gap 12). `position: sticky; top: 88px` (header 64 + 24); if taller than the viewport it scrolls with the page (no inner scroll).
- Desktop filters **apply immediately** (on input with 250 ms debounce for text/price, on change for selects). Results region has `aria-live="polite"` only on the count ("42 viaturas"), not on the grid.
- Results: chip row (only when ≥ 1 filter active) → grid `repeat(3, 1fr)` gap 24 → pagination block (centred, gap 12, top margin 32).
- Grid at 1024–1279: still 3 columns, cards shrink (min 220) — the aside stays 280.

### Tablet (768–1023) — frame "Catálogo — Tablet 768"

- Content width 720 (gutter 24). H1 + count; then toolbar row: Secondary "Filtros (n)" button (opens the bottom sheet, see mobile) + sort select filling the remaining width (gap 12).
- Chips row, then grid `repeat(2, 1fr)` gap 24, then pagination (desktop variant).

### Mobile (< 768) — frames "Catálogo — Mobile 375" and "… · Folha de filtros"

- Gutter 16. H1 (`h1` mobile size) + count. Toolbar: "Filtros (n)" Secondary + sort select (fills), gap 12.
- Chips row is shown under the toolbar (wraps).
- Grid: 1 column, gap 24, cards full width.
- Pagination: mobile variant + "A mostrar…" line.
- Filters open in the **bottom sheet** (components §22): same fields in the same order, stacked; price min/max side by side. Footer "Limpar" / "Ver {n} resultados". Applying closes the sheet, updates the URL and moves focus to the results count.

### Behaviour details

- Count text and "Ver {n} resultados" use plural rules (copy.md §1).
- Changing filters or sort resets `pagina` to 1.
- Page change: update URL, scroll to and focus the H1 (`tabindex="-1"`).
- No results → EmptyState (§3.1) in the results column (desktop) / below the toolbar (mobile); filters stay visible.
- Images: first 3 cards eager (`fetchpriority="high"` on the first), rest lazy. Reserve the 4:3 box to avoid layout shift (CLS).

---

## 2. Vehicle detail — `/carros/{id}-{texto}` (Figma 02)

Data: `GET /api/public/vehicles/{id}` at build time (`generateStaticParams`). Only public fields exist here — **never** registration number, VIN, notes or documents. The canonical URL is `/carros/{id}-{make}-{model}-{year}` (lower-case, accents removed, spaces → `-`); any other text after the id still resolves (lookup by id only).

SEO (NFR-12): `<title>` "{Marca} {Modelo} {Ano} — SilvaStock"; meta description "{Marca} {Modelo} {Versão}, {ano}, {km}, {combustível}, {caixa}. {preço}."; `og:image` = primary photo `url`; `<link rel="canonical">`.

### Desktop (≥ 1024) — frame "Detalhe — Desktop 1440"

```
┌ container 1200 ───────────────────────────────────────────────────────┐
│ ← Voltar às viaturas (link, 44 tall; returns to the catalogue with    │
│   the previous query string if known, else "/")                       │
│ ┌ gallery 768 ─────────────────────────────┐ 32 ┌ summary 400 ──────┐│
│ │ main image 4:3 (768×576, radius lg)      │    │ H1 BMW 320d       ││
│ │  ‹ (44 round)              (44 round) ›  │    │ version bodyLg    ││
│ │                               [1 / 12]   │    │ 18 900 € priceLg  ││
│ ├──────────────────────────────────────────┤    │ Ano    Quilómetros││
│ │ thumbs 96×72 gap 8 (scroll-x)            │    │ Combust. Caixa    ││
│ └──────────────────────────────────────────┘    │ [💬 Contacto pelo ││
│                                                 │   Messenger]      ││
│                                                 │ Referência: 42    ││
│                                                 └── sticky top 88 ──┘│
│ H2 Especificações  — dl in 2 columns (gap 24), rows 44+ with bottom  │
│                       border.subtle: label secondary | value label    │
│ H2 Equipamento      — ul in 3 columns, check icon 20 success.fg      │
│ H2 Descrição        — prose max 720, white-space: pre-line           │
└───────────────────────────────────────────────────────────────────────┘
```

- Vertical gap between blocks: 40. Section title → content: 16.
- Summary card: bg `bg.surface`, 1px `border.default`, radius `lg`, padding 24, gap 20. Key facts: `<dl>` grid 2×2, gap 16; label `caption` secondary, value `label` primary.
- Contact box: bg `bg.muted`, radius `md`, padding 16, icon 24 + "Contacto pelo Messenger" (`h4`). Static text — no link, button or handle.
- "Referência: {id}" (`caption` secondary) helps the visitor mention the car on Messenger.
- Specs list shows only non-null fields, in this order: Marca, Modelo, Versão, Ano, Quilómetros, Combustível, Caixa, Cilindrada, Potência, Carroçaria, Tração, Cor exterior, Cor interior, Portas, Lugares, Emissões. Sections with no data (Equipamento empty, Descrição null) are omitted entirely.

### Gallery behaviour

- Images sorted as returned (primary first). Main image uses `url`; thumbnails use `thumbUrl`. Reserve the 4:3 box.
- Arrows: 44×44 round buttons, bg `bg.raised`, `shadow-md`, chevron 20 `text.primary`, vertically centred, 12px from the edges; hidden when there is one photo. Keyboard ←/→ when the gallery has focus. Counter "{i} / {n}": `caption` `text.inverse` on `bg.overlay` pill, bottom-right 12/12.
- Thumbnails: buttons 96×72 radius `md`; current one has a 2px `action.primary.bg` border and `aria-current="true"`; row scrolls horizontally (no wrap).
- Click/Enter on the main image opens a **lightbox** (`<dialog>`, full viewport, bg `#000` at 92%, image `object-fit: contain`, arrows, counter, close 44×44 top-right "Fechar", Esc closes, swipe on touch).
- No photos (should not happen for published cars): `bg.muted` box with image icon + "Sem fotografias".

### Tablet (768–1023) — frame "Detalhe — Tablet 768"

Single column, content 720: back link → gallery (720×540, 6 visible thumbs) → summary card (full width, facts 2×2) → specs (2 columns) → equipment (2 columns) → description. Gap 40.

### Mobile (< 768) — frame "Detalhe — Mobile 375"

- Gallery is **full-bleed** (375×281, no radius), swipeable (scroll-snap), arrows still visible (≥ 44), counter visible; **no thumbnail strip**.
- Back link sits above the gallery inside the gutter (16).
- Summary without card chrome (no border/padding): H1 mobile size, version, price (`priceLg` mobile), facts 2×2, contact box full width, reference.
- Specs and equipment in 1 column. Gap between sections 32.

---

## 3. States and secondary pages (Figma 03)

### 3.1 No results (filters) — catalogue
EmptyState in place of the grid: search icon, "Nenhuma viatura encontrada", "Experimente alargar o preço ou remover alguns filtros.", Secondary "Limpar filtros". Active chips remain above it. Desktop keeps the aside.

### 3.2 Empty catalogue (0 published vehicles)
No filters, no sort, no count. EmptyState with car icon: "Ainda não há viaturas publicadas" / "Volte em breve para ver as novidades." — no button.

### 3.3 Vehicle not available (404 for `/carros/…`)
Static 404 page (Next `not-found`), `bg.subtle`, EmptyState centred vertically in a min-height 560 (desktop) / 520 (mobile) area: "Esta viatura já não está disponível" / "Pode ter sido vendida ou retirada do site." / [Ver viaturas disponíveis] → `/`. Vehicles removed between rebuilds are handled by the rebuild itself; this page covers stale links.

### 3.4 Generic 404
Same layout: "Página não encontrada" / "O endereço pode estar errado ou a página foi removida." / [Ir para as viaturas].

### 3.5 Loading
The public site is static: there is no loading state for the catalogue or detail. Filtering is synchronous. Images show the `bg.muted` placeholder until loaded.

### 3.6 Privacy notice — `/privacidade`
Prose page: container 720 centred, H1 "Aviso de privacidade", "Última atualização: {data}" (`bodySm` secondary), then `h2` sections + `body` paragraphs (gap 16; 32 before each `h2`). Links underlined. **Text to be supplied/approved by the owner in Phase 5** (REQUIREMENTS §5.6); the design only fixes the layout.
