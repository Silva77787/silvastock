# SilvaStock design system (DES-01)

Visual design for the public showroom and the `/admin` back office. The `frontend` agent implements **only** what is specified here and never decides the look (CLAUDE.md, sources of truth #5). If something is missing or contradicts `DECISIONS.md`, `openapi.yaml` or `REQUIREMENTS.md`, stop and ask.

## Approved direction (owner, 2026-11-14)

- **Palette:** teal `#0F5E5A` (primary/actions) + orange `#C2410C` (price accent and focus ring) on warm stone neutrals.
- **Typography:** Inter only (free, self-hosted via `next/font`).
- **Themes:** light (default) + dark, following `prefers-color-scheme` (no toggle).
- **Look:** clean, calm and photo-first; white cards on a warm off-white page, 12px card radius, soft shadows, generous spacing, 44px touch targets. Accessibility target WCAG 2.2 AA — every colour pair is verified.

## Files

| File | What it is | Who uses it |
|---|---|---|
| [`tokens.json`](tokens.json) | Machine-readable design tokens (colours light/dark, typography, spacing, radii, shadows, sizes, breakpoints, motion, z-index). **Source of truth for values.** | Frontend (import or read), tooling |
| [`tokens.css`](tokens.css) | CSS custom properties + text-style classes **generated** from `tokens.json`. Import once in the root layout. | Frontend |
| [`tokens.md`](tokens.md) | Human-readable explanation of the tokens, usage rules and the **contrast verification table** (41 pairs, both themes, all pass AA). | Everyone |
| [`components.md`](components.md) | Component catalogue: exact sizes, states, behaviour and accessibility for buttons, inputs, selects, badges, pagination, dialogs, toasts, uploader, cards, tiles, etc. | Frontend |
| [`layouts-public.md`](layouts-public.md) | Public pages at mobile / tablet / desktop: catalogue + filters, vehicle detail + gallery, empty/error states, footer, privacy page. | Frontend |
| [`layouts-admin.md`](layouts-admin.md) | Back-office pages: login, shell/navigation, dashboard, inventory, vehicle form, photos, documents. | Frontend |
| [`copy.md`](copy.md) | Every UI string in pt-PT, enum labels, and number/price/date formatting rules (pt-PT, EUR). | Frontend, owner (review wording) |
| [`tools/build-tokens.mjs`](tools/build-tokens.mjs) | Regenerates `tokens.css` and re-checks contrast (`node docs/design/tools/build-tokens.mjs`; exits 1 if a pair fails). | Whoever edits tokens |
| [`tools/contrast-pairs.json`](tools/contrast-pairs.json) | The foreground/background pairs the script checks. | Tooling |

## Figma

File **"SilvaStock — Design System"**: <https://www.figma.com/design/AJyqbib0ALROWaWMadmyYo>

| Page | Contents |
|---|---|
| Components | 20 components/variant sets bound to variables: Button, StatusBadge, Checkbox, TextField, Select, Alert, Toast, ConfirmDialog, Pagination, VehicleCard, SkeletonCard, FilterChip, Uploader, PhotoTile, DocumentRow, NavItem, StatCard, EmptyState, PublicHeader, PublicFooter. Each has its spec in the component description. |
| Layouts | 01 Catálogo · 02 Detalhe · 03 Estados públicos · 04 Login · 05 Estrutura + Painel · 06 Inventário · 07 Formulário da viatura |
| Foundations | (pending) |

Variables: collections **Primitives**, **Color** (light), **Color (Dark)**, **Dimensions**; 19 text styles (`Text/*`) and 6 effect styles (`Shadow/Light|Dark/*`). Each variable's code syntax is the CSS variable from `tokens.css`, so Dev Mode shows `var(--color-…)`.

Notes:
- The Figma Starter plan allows one mode per variable collection, so the dark theme is a separate collection (`Color (Dark)`) instead of a mode. `tokens.json` is authoritative for both themes.
- **If Figma and these documents disagree, these documents win.**
- Pending in Figma (blocked by the Starter plan's monthly MCP limit): section 08 (photos and documents frames — fully specified in `layouts-admin.md` §5.7–5.8), the Foundations specimen page, dark-theme sample frames, and two small fixes (vehicle-form field grid shows 1 column instead of 2 on desktop/tablet; inventory table thumbnails are squashed).

## How to implement (frontend)

1. Import `tokens.css` in the root layout; load Inter with `next/font/google` and map it to `--font-family-sans`.
2. Build components from `components.md` in `web/` using only semantic tokens; write Vitest/RTL tests for states and a11y attributes.
3. Build pages from the layout docs; take all text from `copy.md` and use the shared pt-PT formatters.
4. Check: keyboard-only pass, 200% zoom / 320px reflow, `prefers-color-scheme: dark`, `prefers-reduced-motion`, and axe (NFR-13).

## Out of scope (MVP)

Leads, contact forms, phone/WhatsApp links, Messenger links/handles (the site only shows the text "Contacto pelo Messenger"), favourites, comparison, Reserved status, admin user management screen, theme toggle, dealership profile editing. Do not design or build them without approval.

## Changing the design

Edit `tokens.json` → run the build script → update the affected `.md` files → update Figma. Copy changes go in `copy.md`. Any change of direction (palette, font, layout pattern) needs the owner's approval.
