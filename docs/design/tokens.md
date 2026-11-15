# Design tokens

Machine-readable source: [`tokens.json`](tokens.json). CSS custom properties: [`tokens.css`](tokens.css) (generated — run `node docs/design/tools/build-tokens.mjs` after editing `tokens.json`; it also re-checks contrast and exits non-zero on failure).

**Rule for implementers:** components use **semantic tokens only** (`--color-text-primary`, `--space-4`, …). Never use a primitive colour or a raw hex/px value in `web/`. If a value you need is missing, it is a design gap — raise it, do not invent one.

## 1. Colour

Palette: **teal** (primary / actions), **orange** (price accent, focus ring), **stone** (warm neutrals), plus green / amber / red / blue / violet for feedback and status.

### 1.1 Primitives (do not use directly)

| Ramp | Values |
|---|---|
| teal | 50 `#EEF7F6` · 100 `#D3EBE8` · 200 `#A6D6D1` · 300 `#6FBBB4` · 400 `#3A9A92` · 500 `#1F7C75` · **600 `#0F5E5A`** · 700 `#0C4E4B` · 800 `#0A3F3D` · 900 `#082F2E` · 950 `#041C1B` |
| orange | 50 `#FFF7ED` · 300 `#FDBA74` · 400 `#FB923C` · **700 `#C2410C`** · 800 `#9A3412` · 950 `#431407` |
| stone | 50 `#FAFAF9` · 100 `#F5F5F4` · 200 `#E7E5E4` · 300 `#D6D3D1` · 400 `#A8A29E` · 450 `#8A837E` · 500 `#78716C` · 600 `#57534E` · 700 `#44403C` · 800 `#292524` · 900 `#1C1917` · 950 `#0C0A09` |
| green | 50 `#F0FDF4` · 100 `#DCFCE7` · 300 `#86EFAC` · 400 `#4ADE80` · 700 `#15803D` · 800 `#166534` · 950 `#052E16` |
| amber | 50 `#FFFBEB` · 300 `#FCD34D` · 700 `#B45309` · 800 `#92400E` · 950 `#451A03` |
| red | 50 `#FEF2F2` · 300 `#FCA5A5` · 400 `#F87171` · 700 `#B91C1C` · 800 `#991B1B` · 950 `#450A0A` |
| blue | 50 `#EFF6FF` · 300 `#93C5FD` · 700 `#1D4ED8` · 800 `#1E40AF` · 950 `#172554` |
| violet | 100 `#EDE9FE` · 300 `#C4B5FD` · 800 `#5B21B6` · 950 `#2E1065` |

### 1.2 Semantic tokens (light / dark)

CSS name = `--color-` + path in kebab-case (e.g. `action.primary.bgHover` → `--color-action-primary-bg-hover`).

| Token | Light | Dark | Use |
|---|---|---|---|
| `bg.canvas` | `#FFFFFF` | `#0C0A09` | Page background (detail page, admin login card area) |
| `bg.subtle` | `#FAFAF9` | `#0C0A09` | Behind card grids (catalogue) and admin content area |
| `bg.surface` | `#FFFFFF` | `#1C1917` | Cards, panels, inputs, tables, header |
| `bg.raised` | `#FFFFFF` | `#292524` | Dialogs, menus, toasts, drawers, bottom sheet |
| `bg.muted` | `#F5F5F4` | `#292524` | Hover rows, skeletons, image placeholders, footer, tab track |
| `bg.overlay` | `rgba(12,10,9,.60)` | `rgba(0,0,0,.70)` | Backdrop behind dialogs/drawers |
| `text.primary` | `#1C1917` | `#FAFAF9` | Body text, headings |
| `text.secondary` | `#57534E` | `#A8A29E` | Meta, helper text, labels of facts |
| `text.disabled` | `#A8A29E` | `#57534E` | Disabled controls only |
| `text.inverse` | `#FFFFFF` | `#0C0A09` | Text on `bg.overlay` (gallery counter) |
| `text.link` / `text.linkHover` | `#0F5E5A` / `#0C4E4B` | `#6FBBB4` / `#A6D6D1` | Links, ghost buttons |
| `border.subtle` | `#E7E5E4` | `#292524` | Dividers, table rows (decorative) |
| `border.default` | `#D6D3D1` | `#44403C` | Card / panel outlines (decorative) |
| `border.strong` | `#78716C` | `#8A837E` | Input, checkbox, secondary-button borders (≥ 3:1) |
| `action.primary.bg` / `bgHover` / `bgActive` | `#0F5E5A` / `#0C4E4B` / `#0A3F3D` | `#6FBBB4` / `#A6D6D1` / `#D3EBE8` | Primary buttons, checked checkbox, current page |
| `action.primary.fg` | `#FFFFFF` | `#041C1B` | Label on primary |
| `action.danger.bg` / `bgHover` | `#B91C1C` / `#991B1B` | `#F87171` / `#FCA5A5` | Destructive buttons |
| `action.danger.fg` | `#FFFFFF` | `#0C0A09` | Label on danger |
| `action.selected.bg` / `fg` | `#EEF7F6` / `#0C4E4B` | `#082F2E` / `#A6D6D1` | Active nav item, selected chip, drag-over |
| `accent.price` | `#C2410C` | `#FB923C` | **Price text only** |
| `accent.bg` / `accent.fg` | `#FFF7ED` / `#9A3412` | `#431407` / `#FDBA74` | "Principal" photo tag |
| `feedback.{success,warning,danger,info}.fg` | `#15803D` · `#B45309` · `#B91C1C` · `#1D4ED8` | `#4ADE80` · `#FCD34D` · `#FCA5A5` · `#93C5FD` | Icons/text in alerts, toasts, field errors |
| `feedback.*.bg` | `#F0FDF4` · `#FFFBEB` · `#FEF2F2` · `#EFF6FF` | `#052E16` · `#451A03` · `#450A0A` · `#172554` | Alert backgrounds |
| `feedback.*.border` | `#86EFAC` · `#FCD34D` · `#FCA5A5` · `#93C5FD` | `#166534` · `#92400E` · `#991B1B` · `#1E40AF` | Alert borders |
| `status.draft.fg/bg` | `#44403C` / `#F5F5F4` | `#D6D3D1` / `#292524` | Badge **Rascunho** |
| `status.published.fg/bg` | `#166534` / `#DCFCE7` | `#86EFAC` / `#052E16` | Badge **Publicado** |
| `status.sold.fg/bg` | `#5B21B6` / `#EDE9FE` | `#C4B5FD` / `#2E1065` | Badge **Vendido** |
| `focus.ring` | `#C2410C` | `#FDBA74` | Focus outline on every interactive element |

### 1.3 Theming

- Light is the default. Dark applies through `@media (prefers-color-scheme: dark)` only — **no toggle in the MVP** and no JavaScript, so a static page never flashes the wrong theme.
- `:root { color-scheme: light dark; }` is set so native controls (`<select>`, scrollbars) follow the theme.
- Photos are not dimmed in dark mode.
- Shadows: dark uses darker, stronger shadows (`theme.dark.shadow.*`) and relies more on `border.default`.

### 1.4 Contrast verification (WCAG 2.2 AA)

Checked by `tools/build-tokens.mjs` (pairs in `tools/contrast-pairs.json`). Text needs 4.5:1; large text, UI component boundaries and focus indicators need 3:1. **All 41 pairs pass in both themes.**

| Foreground | Background | Use | Min | Light | Dark |
|---|---|---|---|---|---|
| `text.primary` | `bg.canvas` | Body text | 4.5:1 | 17.49:1 ✅ | 18.92:1 ✅ |
| `text.primary` | `bg.surface` | Body text on cards | 4.5:1 | 17.49:1 ✅ | 16.74:1 ✅ |
| `text.primary` | `bg.raised` | Text in dialogs/toasts | 4.5:1 | 17.49:1 ✅ | 14.52:1 ✅ |
| `text.primary` | `bg.muted` | Text on hover rows/chips | 4.5:1 | 16.03:1 ✅ | 14.52:1 ✅ |
| `text.secondary` | `bg.canvas` | Secondary text | 4.5:1 | 7.63:1 ✅ | 7.83:1 ✅ |
| `text.secondary` | `bg.subtle` | Secondary text on grid bg | 4.5:1 | 7.30:1 ✅ | 7.83:1 ✅ |
| `text.secondary` | `bg.surface` | Meta lines on cards | 4.5:1 | 7.63:1 ✅ | 6.93:1 ✅ |
| `text.secondary` | `bg.raised` | Secondary text in dialogs | 4.5:1 | 7.63:1 ✅ | 6.01:1 ✅ |
| `text.secondary` | `bg.muted` | Secondary text on muted | 4.5:1 | 6.99:1 ✅ | 6.01:1 ✅ |
| `text.link` | `bg.canvas` | Links | 4.5:1 | 7.58:1 ✅ | 8.88:1 ✅ |
| `text.link` | `bg.surface` | Links on cards | 4.5:1 | 7.58:1 ✅ | 7.86:1 ✅ |
| `text.link` | `bg.subtle` | Links on grid bg | 4.5:1 | 7.26:1 ✅ | 8.88:1 ✅ |
| `action.primary.fg` | `action.primary.bg` | Primary button label | 4.5:1 | 7.58:1 ✅ | 7.95:1 ✅ |
| `action.primary.fg` | `action.primary.bgHover` | Primary button hover | 4.5:1 | 9.52:1 ✅ | 11.09:1 ✅ |
| `action.primary.fg` | `action.primary.bgActive` | Primary button pressed | 4.5:1 | 11.72:1 ✅ | 14.17:1 ✅ |
| `action.primary.bg` | `bg.canvas` | Primary button edge vs page | 3:1 | 7.58:1 ✅ | 8.88:1 ✅ |
| `action.primary.bg` | `bg.surface` | Primary/checkbox fill vs card | 3:1 | 7.58:1 ✅ | 7.86:1 ✅ |
| `action.danger.fg` | `action.danger.bg` | Danger button label | 4.5:1 | 6.47:1 ✅ | 7.14:1 ✅ |
| `action.danger.fg` | `action.danger.bgHover` | Danger button hover | 4.5:1 | 8.31:1 ✅ | 10.41:1 ✅ |
| `action.selected.fg` | `action.selected.bg` | Selected nav/chip | 4.5:1 | 8.73:1 ✅ | 9.04:1 ✅ |
| `accent.price` | `bg.surface` | Price on cards | 4.5:1 | 5.18:1 ✅ | 7.73:1 ✅ |
| `accent.price` | `bg.canvas` | Price on detail page | 4.5:1 | 5.18:1 ✅ | 8.73:1 ✅ |
| `accent.price` | `bg.subtle` | Price on grid bg | 4.5:1 | 4.96:1 ✅ | 8.73:1 ✅ |
| `accent.fg` | `accent.bg` | Accent tag | 4.5:1 | 6.88:1 ✅ | 9.28:1 ✅ |
| `border.strong` | `bg.surface` | Input borders | 3:1 | 4.80:1 ✅ | 4.69:1 ✅ |
| `border.strong` | `bg.canvas` | Input borders on page | 3:1 | 4.80:1 ✅ | 5.29:1 ✅ |
| `border.strong` | `bg.raised` | Input borders in dialogs | 3:1 | 4.80:1 ✅ | 4.07:1 ✅ |
| `feedback.success.fg` | `feedback.success.bg` | Success banner | 4.5:1 | 4.79:1 ✅ | 8.55:1 ✅ |
| `feedback.warning.fg` | `feedback.warning.bg` | Warning banner | 4.5:1 | 4.84:1 ✅ | 10.39:1 ✅ |
| `feedback.danger.fg` | `feedback.danger.bg` | Error banner | 4.5:1 | 5.91:1 ✅ | 8.51:1 ✅ |
| `feedback.info.fg` | `feedback.info.bg` | Info banner | 4.5:1 | 6.16:1 ✅ | 8.15:1 ✅ |
| `feedback.danger.fg` | `bg.surface` | Inline field error | 4.5:1 | 6.47:1 ✅ | 9.21:1 ✅ |
| `feedback.danger.fg` | `bg.raised` | Field error in dialog | 4.5:1 | 6.47:1 ✅ | 7.99:1 ✅ |
| `feedback.success.fg` | `bg.raised` | Toast icon | 3:1 | 5.02:1 ✅ | 8.71:1 ✅ |
| `status.draft.fg` | `status.draft.bg` | Badge Rascunho | 4.5:1 | 9.42:1 ✅ | 10.18:1 ✅ |
| `status.published.fg` | `status.published.bg` | Badge Publicado | 4.5:1 | 6.49:1 ✅ | 10.62:1 ✅ |
| `status.sold.fg` | `status.sold.bg` | Badge Vendido | 4.5:1 | 7.57:1 ✅ | 8.25:1 ✅ |
| `focus.ring` | `bg.canvas` | Focus ring | 3:1 | 5.18:1 ✅ | 11.71:1 ✅ |
| `focus.ring` | `bg.surface` | Focus ring on cards | 3:1 | 5.18:1 ✅ | 10.37:1 ✅ |
| `focus.ring` | `bg.muted` | Focus ring on muted | 3:1 | 4.75:1 ✅ | 8.99:1 ✅ |
| `focus.ring` | `bg.raised` | Focus ring in dialogs | 3:1 | 5.18:1 ✅ | 8.99:1 ✅ |

Not checked (intentionally): `border.subtle` / `border.default` (decorative separators — cards are identified by their content, not their outline) and `text.disabled` (WCAG exempts disabled controls).

**Never rely on colour alone:** status badges always show their label; errors always have an icon + message; the current page has `aria-current` plus a filled style.

## 2. Typography

**Inter** (variable, Google Fonts, SIL OFL — free), self-hosted with `next/font/google` (`subsets: ['latin', 'latin-ext']`, `display: 'swap'`, `variable: '--font-family-sans'`). No other font. Fallback stack: `ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`.

| Style (class) | Mobile < 768 | ≥ 768 | Weight | Letter-spacing | Use |
|---|---|---|---|---|---|
| `display` | 30/36 | 36/40 | 700 | −0.02em | (reserved; not used in MVP screens) |
| `h1` | 24/32 | 30/36 | 700 | −0.02em | Page title (one per page) |
| `h2` | 20/28 | 24/32 | 600 | −0.01em | Section titles on the detail page |
| `h3` | 20/28 | 20/28 | 600 | −0.01em | Card/section titles in admin, dialog title, empty-state title |
| `h4` | 18/28 | 18/28 | 600 | 0 | Vehicle card title, wordmark, "Contacto pelo Messenger" |
| `bodyLg` | 18/28 | 18/28 | 400 | 0 | Version line on detail page |
| `body` | 16/24 | 16/24 | 400 | 0 | Default text, input values |
| `bodySm` | 14/20 | 14/20 | 400 | 0 | Meta lines, helper text, table cells |
| `label` | 14/20 | 14/20 | 500 | 0 | Form labels, fact values, links in nav |
| `caption` | 12/16 | 12/16 | 500 | 0.01em | Badges, fact labels, counters |
| `button` | 16/24 | 16/24 | 600 | 0 | Button md |
| `buttonSm` | 14/20 | 14/20 | 600 | 0 | Button sm, active nav item |
| `priceLg` | 24/32 | 30/36 | 700 | −0.01em | Price on detail page |
| `price` | 20/28 | 20/28 | 700 | −0.01em | Price on cards |
| `stat` | 36/40 | 36/40 | 700 | −0.02em | Dashboard counters |

Rules:
- `font-variant-numeric: tabular-nums` on prices, mileage, years, counters, table numeric columns.
- Base `html { font-size: 100% }` (16px). Use `rem` in CSS (`16px = 1rem`); px values above are the reference.
- Text must reflow at 200% zoom and 320 CSS px width without horizontal scrolling (WCAG 1.4.10). Line length for prose ≤ 720px.
- Never set text below 12px. Never use all-caps except the tiny "NESTA PÁGINA" overline (caption, `letter-spacing: 0.04em`).

## 3. Spacing (4px base)

| Token | px | Typical use |
|---|---|---|
| `--space-0` | 0 | — |
| `--space-1` | 4 | Label ↔ control, icon ↔ text in badges |
| `--space-2` | 8 | Button icon gap, chip gap, card body gap |
| `--space-3` | 12 | Input padding-x, alert gap, dialog actions gap |
| `--space-4` | 16 | Card padding, mobile gutter, form row gap (mobile) |
| `--space-5` | 20 | Button padding-x (md), form grid gap, filter panel padding |
| `--space-6` | 24 | Grid gap (cards), section card padding, dialog padding, tablet gutter |
| `--space-8` | 32 | Desktop gutter, gap filters ↔ results, page top padding |
| `--space-10` | 40 | Admin content padding-x (desktop), empty state padding-y |
| `--space-12` | 48 | — |
| `--space-16` | 64 | Bottom padding of public pages |
| `--space-20` | 80 | — |

## 4. Radii, borders, shadows

| Token | Value | Use |
|---|---|---|
| `--radius-sm` | 4px | Checkbox, menu items, skeleton bars |
| `--radius-md` | 8px | Buttons, inputs, selects, thumbnails, toasts, alerts, menus |
| `--radius-lg` | 12px | Cards, panels, dialogs, gallery image, uploader |
| `--radius-xl` | 16px | Login card, bottom-sheet top corners |
| `--radius-full` | 9999px | Badges, chips, pagination current page, gallery arrows |
| `--border-width-default` | 1px | All borders |
| `--border-width-focus` | 2px | Focus outline; error border on inputs; primary photo |
| `--shadow-sm` | see JSON | Cards at rest, selected tab |
| `--shadow-md` | see JSON | Card hover, login card, gallery arrows |
| `--shadow-lg` | see JSON | Dialogs, menus, toasts, drawers, sticky save bar |

**Focus indicator (all interactive elements):** `outline: var(--border-width-focus) solid var(--color-focus-ring); outline-offset: 2px;` on `:focus-visible` only. Never remove outlines without this replacement. Must not be obscured by sticky header/save bar (use `scroll-margin-top: 88px` / `scroll-margin-bottom: 96px` — WCAG 2.4.11).

## 5. Sizes

| Token | Value |
|---|---|
| `--size-control-sm` / `md` / `lg` | 36 / **44** / 52px — default control height is 44 |
| `--size-icon-sm` / `md` / `lg` | 16 / 20 / 24px |
| `--size-header-public` | 64px |
| `--size-header-admin` | 56px (mobile/tablet top bar) |
| `--size-sidebar-admin` | 240px |
| `--size-sidebar-filters` | 280px |
| `--size-container-public` | 1200px (content width, excluding gutters) |
| `--size-container-admin` | 1120px |
| `--size-container-form` | 880px |
| `--size-container-prose` | 720px |

Minimum target size: 24×24 (WCAG 2.5.8); **design target 44×44** for anything tappable on mobile. 36px controls are allowed only on desktop toolbars/tables and in groups with ≥ 8px spacing.

## 6. Breakpoints and grid

Mobile-first `min-width` queries:

| Name | Range | Gutter | Columns (catalogue) | Admin shell |
|---|---|---|---|---|
| mobile | < 768 | 16px | 1 | Top bar 56 + drawer |
| tablet | 768–1023 | 24px | 2 | Top bar 56 + drawer |
| desktop | ≥ 1024 | 32px | 3 (with 280px filter sidebar) | Sidebar 240 |
| xl | ≥ 1280 | 32px | 3 | Sidebar 240 |

Tokens also expose `sm = 640px` (used for: dialog actions inline vs stacked; 2-col fields in admin forms on large phones/landscape).

Public container: `max-width: 1200px; margin-inline: auto; padding-inline: var(--gutter-*)`. At 1440: 120px margins. Catalogue desktop: filters 280 + gap 32 + results 888 (3 × 280 cards + 2 × 24 gaps).

## 7. Motion

| Token | Value | Use |
|---|---|---|
| `--motion-duration-fast` | 120ms | Hover/colour changes |
| `--motion-duration-base` | 200ms | Menus, toasts, dialog fade/scale (0.98 → 1) |
| `--motion-duration-slow` | 300ms | Drawer / bottom sheet slide |
| `--motion-easing-standard` | `cubic-bezier(0.2, 0, 0, 1)` | Enter |
| `--motion-easing-exit` | `cubic-bezier(0.4, 0, 1, 1)` | Exit |

`prefers-reduced-motion: reduce` sets all durations to 0 (in `tokens.css`) and disables skeleton pulse and gallery slide.

## 8. Z-index

`--z-index-sticky` 100 (public header, admin top bar, save bar) · `--z-index-dropdown` 200 (menus, select popovers) · `--z-index-overlay` 300 (dialogs, drawers, bottom sheet, lightbox) · `--z-index-toast` 400.

## 9. Icons

[Lucide](https://lucide.dev) (ISC licence, free), outline style, `stroke-width: 1.75` (1.5 at 16px), `stroke-linecap/linejoin: round`, colour `currentColor`. Sizes from `--size-icon-*`. Decorative icons get `aria-hidden="true"`; icon-only buttons need an `aria-label` (pt-PT). Adding the `lucide-react` dependency needs the usual justification (CLAUDE.md); inline SVGs of the same icons are an acceptable alternative.

Icons used: `search`, `sliders-horizontal` (Filtros), `x`, `chevron-down/left/right`, `arrow-left`, `check`, `image` (photo placeholder), `car`, `house` (Painel), `plus`, `menu`, `more-horizontal`, `upload`, `grip-vertical`, `star`, `trash-2`, `file-text`, `download`, `lock`, `log-out`, `info`, `triangle-alert`, `circle-check`, `message-circle` (Messenger line — generic icon, **not** the Messenger logo).
