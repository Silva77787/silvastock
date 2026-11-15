# Component catalogue

Every component below exists in the Figma file (page **Components**) with the same name, bound to the variables in [`tokens.json`](tokens.json). Measurements here are authoritative; Figma is the visual reference. All tokens are referenced by their semantic name (CSS: `--color-…`, `--space-…`, `--radius-…`). Copy: [`copy.md`](copy.md).

General rules for every interactive component:
- States: default, hover (pointer only, `@media (hover: hover)`), `:focus-visible` (2px `focus.ring` outline, 2px offset), active/pressed, disabled (`aria-disabled` or native `disabled`; no hover effect; cursor `not-allowed`), loading where relevant.
- Hit area ≥ 44×44 on touch layouts; 36px controls only on desktop toolbars/tables.
- Transitions: `background-color, border-color, box-shadow` over `--motion-duration-fast`.

---

## 1. Button

| Property | md (default) | sm |
|---|---|---|
| Height | 44 | 36 |
| Padding-x | 20 (`space-5`) | 12 (`space-3`) |
| Gap (icon ↔ label) | 8 | 8 |
| Icon | 20 | 16 |
| Text style | `button` | `buttonSm` |
| Radius | `md` | `md` |

| Variant | Default | Hover | Active | Disabled |
|---|---|---|---|---|
| **Primary** | bg `action.primary.bg`, fg `action.primary.fg` | `bgHover` | `bgActive` | bg `bg.muted`, fg `text.disabled` |
| **Secondary** | bg `bg.surface`, 1px `border.strong`, fg `text.primary` | bg `bg.muted` | bg `bg.muted` | border `border.subtle`, fg `text.disabled` |
| **Ghost** | transparent, fg `text.link` | bg `bg.muted`, fg `text.linkHover` | same | fg `text.disabled` |
| **Danger** | bg `action.danger.bg`, fg `action.danger.fg` | `bgHover` | `bgHover` | bg `bg.muted`, fg `text.disabled` |

- One Primary per view region (page header, dialog, save bar).
- Loading: keep width, replace the label by a 16px spinner + loading text ("A guardar…"), `aria-busy="true"`, ignore clicks.
- Full-width on mobile when it is the main action of the screen (e.g. "+ Nova viatura", sheet "Ver N resultados").
- Icon-only button (menu, close, row actions): 44×44, radius `md`, ghost style, icon 20 `text.primary`, mandatory `aria-label`.
- Links styled as buttons are `<a>`; actions are `<button type="button">`.

## 2. TextField and Textarea

Structure: label → control → helper **or** error. Gap 4.

| Part | Spec |
|---|---|
| Label | `label` style, `text.primary`. Required: red `*` (`feedback.danger.fg`) + `aria-required="true"` on the input. Label always visible (no placeholder-as-label). |
| Control | Height 44, padding-x 12, radius `md`, bg `bg.surface`, 1px `border.strong`, value `body` `text.primary`, placeholder `text.secondary`. |
| Focus | 2px `focus.ring` border (replaces the 1px border; no layout shift — use `box-shadow: inset 0 0 0 1px` or outline). |
| Error | 2px `feedback.danger.fg` border, `aria-invalid="true"`, message row: 16px alert icon + `bodySm` `feedback.danger.fg`, linked with `aria-describedby`. |
| Disabled | bg `bg.muted`, border `border.subtle`, text `text.disabled`. |
| Helper | `bodySm` `text.secondary`, linked with `aria-describedby`. |
| Suffix/prefix | Unit text inside the control on the right (`€`, `km`, `cm³`, `kW`) in `text.secondary` — optional; the label already contains the unit. |
| Textarea | Same, min-height 120, padding 12, `resize: vertical`. Counter right-aligned under it ("214 / 5000", `bodySm` secondary); turns `feedback.warning.fg` from 90% and is announced once via `aria-live="polite"`. |

Validation timing: validate on blur and on submit; once a field shows an error, re-validate on input. On submit with errors: focus the first invalid field and show the summary alert at the top of the form (for 422) — see layouts-admin §4.

Numeric fields use `inputmode="numeric"` (year, km, cc, kW, doors, seats) or `inputmode="decimal"` (price), never `type="number"` (spinner and locale issues).

## 3. Select

Native `<select>` styled like TextField: height 44, padding-left 12, padding-right 40, chevron-down 20 `text.secondary` at right 12 (`appearance: none` + background icon or wrapper). First option is the placeholder "Selecione…" (admin) or "Qualquer" (public filters). Same label / helper / error / disabled rules as TextField. No custom listbox in the MVP.

## 4. Checkbox

Box 20×20, radius `sm`, 1px `border.strong`, bg `bg.surface`. Checked: bg `action.primary.bg`, white check (16px, stroke 2). Label `body` `text.primary`, gap 8; the whole row (min-height 44) is clickable (`<label>` wraps both). Focus ring around the box. Disabled: bg `bg.muted`, border `border.subtle`.

Used for "Preço sob consulta": when checked, the price field becomes disabled and its value is cleared on save (`price: null`, `priceOnRequest: true`).

## 5. StatusBadge

Pill (`radius-full`), padding 4/8, gap 4, 6px dot + label in `caption`.

| Status | Label | fg / bg |
|---|---|---|
| Draft | Rascunho | `status.draft.fg` / `status.draft.bg` |
| Published | Publicado | `status.published.fg` / `status.published.bg` |
| Sold | Vendido | `status.sold.fg` / `status.sold.bg` |

Not interactive. Never shown on the public site.

## 6. FilterChip (active filter)

Height 36 inside a 44px row, padding-x 12, radius full, `label` text. Selected (the only state used in lists): bg `action.selected.bg`, 1px `action.selected.fg`, fg `action.selected.fg`, trailing 16px `x`. It is a `<button>` that removes the filter, `aria-label="Remover filtro: {texto}"`. Row of chips wraps, gap 8, followed by Ghost sm "Limpar filtros". Also reused for the equipment list in the admin form (`aria-label="Remover {item}"`).

## 7. Pagination

`<nav aria-label="Paginação">`.
- Desktop/tablet: "Anterior" (chevron + text) · page numbers · "Seguinte". Page buttons 44×44, radius full, `label`. Current: bg `action.primary.bg`, fg `action.primary.fg`, `aria-current="page"`. Show first, last, current ±1, `…` (non-interactive) for gaps. Prev/next disabled at the ends (`text.disabled`, `aria-disabled="true"`).
- Mobile (< 768): prev icon button · "Página X de Y" (`label`) · next icon button.
- Under the list: "A mostrar {a}–{b} de {n} viaturas" (`bodySm` secondary).
- Public catalogue: page size **24**, pagination done in the browser on the pre-built data; page in the URL as `?pagina=2`. Changing a filter resets to page 1. After changing page, move focus to the results heading and scroll to it.
- Admin inventory: server pagination (`page`, `pageSize=24`).

## 8. Alert (inline banner)

Padding 12/16, gap 12, radius `md`, 1px `feedback.{tone}.border`, bg `feedback.{tone}.bg`. Icon 20 in `feedback.{tone}.fg` (info → `info`, success → `circle-check`, warning/danger → `triangle-alert`). Title `label` `text.primary`; description `bodySm` `text.secondary`; may contain a list of links (missing fields). Optional close button (44×44) only where noted. `role="status"`; Danger uses `role="alert"`.

## 9. Toast

Width 360 (mobile: `calc(100vw - 32px)`), padding 12/12/12/16, gap 12, radius `md`, bg `bg.raised`, 1px `border.default`, `shadow-lg`. Icon 20 in `feedback.{tone}.fg`, message `bodySm` `text.primary`, close icon button (aria-label "Fechar").
- Position: bottom-right 24px (desktop); bottom-centre 16px above the safe area and above the sticky save bar (mobile).
- Success / Info auto-dismiss after 5 s (paused on hover/focus); Danger stays until closed.
- Container is a live region: `aria-live="polite"` (Danger: `assertive`). Max 3 visible, newest at the bottom, gap 8.
- Never put the only path to an action inside a toast.

## 10. ConfirmDialog

Native `<dialog>` opened with `showModal()`. Width 440 (mobile `calc(100% - 32px)`), padding 24, gap 16, radius `lg`, bg `bg.raised`, `shadow-lg`, backdrop `bg.overlay`. Title `h3`, body `body` `text.secondary`, actions right-aligned (gap 12): Secondary "Cancelar" + Primary or Danger confirm. Below 640px actions stack full-width, confirm on top.
- `role="alertdialog"` for destructive confirmations, `aria-labelledby` title, `aria-describedby` body.
- Initial focus on **Cancelar**; Esc and backdrop click cancel; focus returns to the trigger.
- While the request runs: confirm button loading, both buttons disabled; on error keep the dialog open and show an Alert Danger inside it.

## 11. Menu (row actions)

Button 44×44 with `more-horizontal` → popover menu (`role="menu"`, items `role="menuitem"`). Width 232, padding 4, radius `md`, bg `bg.raised`, 1px `border.default`, `shadow-lg`. Items height 40, padding-x 12, radius `sm`, `bodySm`; hover/focus bg `bg.muted`; destructive item in `feedback.danger.fg`; separator 1px `border.subtle`. Keyboard: Enter/Space/↓ open, ↑/↓ move, Esc closes and returns focus. Only show actions valid for the vehicle's status (see copy.md §4).

## 12. VehicleCard (public)

Whole card is one link (`<a>` wrapping an `<article>`; the title is the accessible name). bg `bg.surface`, 1px `border.default`, radius `lg`, `shadow-sm`, `overflow: hidden`.
- Image: 4:3, `object-fit: cover`, primary photo `thumbUrl` with `srcset` (`thumbUrl` 1x / `url` 2x), `loading="lazy"` except the first row (`fetchpriority="high"` for the first card), `alt="{Marca} {Modelo} {Ano}"`. No photo: `bg.muted` + 32px car icon.
- Body padding 16, gap 8: title `h4` (make + model, 1 line, ellipsis) · version `bodySm` secondary (1 line; omit if null) · meta `bodySm` secondary, tabular: `{ano} · {km} · {combustível} · {caixa}` (max 2 lines) · price `price` `accent.price`, or "Preço sob consulta" in `label` `text.primary`.
- Hover: `shadow-md`, border `border.strong`, title underlined. Focus-visible: outline on the card.
- Width: fluid — fills its grid column (280 at desktop, ~348 tablet, 343 mobile at 375).

## 13. SkeletonCard / skeleton rows

Same box as the card/row; image area and three bars (14px tall, radius 4, widths 60% / 80% / 40%) in `bg.muted`. Pulse opacity 1 → 0.5, 1.2 s infinite; none with reduced motion. `aria-hidden="true"`; the list has `aria-busy="true"` and a visually hidden "A carregar…".

## 14. EmptyState

Centred column, max-width 480, padding 40/24, gap 12. 64px circle `bg.muted` with a 32px icon in `text.secondary` (search for no results, car for empty lists, triangle-alert for errors). Title `h3`, description `body` `text.secondary` (centred), at most one Secondary button. Copy variants in copy.md.

## 15. Uploader (photos and documents)

Drop zone: padding 24, gap 12, radius `lg`, 2px dashed `border.strong`, bg `bg.subtle`, content centred: upload icon 24, title `label`, Secondary sm "Escolher ficheiros" (opens the hidden `<input type="file">`; this button is the keyboard path), hint `bodySm` secondary.

| State | Change |
|---|---|
| DragOver | border `action.primary.bg`, bg `action.selected.bg`, title "Largue as fotos para as enviar" |
| Uploading | title "A enviar {i} de {n} fotos…", 8px progress bar (track `bg.muted`, fill `action.primary.bg`, radius full, `role="progressbar"` with `aria-valuenow`), percentage caption; drop zone disabled |
| Error | border `feedback.danger.fg`, hint replaced by the error in `feedback.danger.fg`; the error is also announced (`role="alert"`) |
| Disabled | (new vehicle not saved yet, or 30 photos reached) opacity of content `text.disabled`, button disabled, hint explains why |

Client pre-checks before sending (server still validates): photos — `image/jpeg, image/png, image/webp`, ≤ 10 MB each, total ≤ 30; documents — `application/pdf, image/jpeg, image/png`, ≤ 15 MB, one file. The photo upload is all-or-nothing on the server: if any file fails, show which one and state that no photo was stored.

Accept attributes: photos `accept="image/jpeg,image/png,image/webp" multiple`; documents `accept="application/pdf,image/jpeg,image/png"`.

## 16. PhotoTile (admin)

Card 192 wide in a responsive grid (`repeat(auto-fill, minmax(176px, 1fr))`, gap 16; mobile 2 columns). Padding 8, gap 8, radius `lg`, bg `bg.surface`, 1px `border.default`. Thumb 4:3 radius `md`.
- Primary: 2px `action.primary.bg` border + tag "Principal" (star icon 16, `caption`, bg `accent.bg`, fg `accent.fg`, radius full) at top-left 8/8.
- Action bar: drag handle (36×36, `grip-vertical`) · "Tornar principal" Ghost sm (non-primary) or "Foto {i} de {n}" caption (primary) · delete icon button (36×36, `trash-2` in `feedback.danger.fg`).
- Reordering: pointer drag **and** keyboard (handle focused → Space lifts, ←/→ moves, Space drops, Esc cancels) **and** "Mover para a esquerda/direita" items in an overflow for users who cannot drag (WCAG 2.5.7). Live region announces moves. Save with `PATCH …/images/order` (all IDs) after each drop; optimistic update, rollback + error toast on failure.
- Deleting the primary photo: the next photo becomes primary on the server; reflect the returned list.

## 17. DocumentRow (admin)

Row padding 12/16, gap 12, bottom 1px `border.subtle`. File icon 24 · text column (name `label`, truncate; meta `bodySm` secondary: `{categoria} · {tipo} · {tamanho} · {data-hora} · {email}`) · Secondary sm "Transferir" · delete icon button 44×44.
- "Transferir": on click call `GET …/documents/{docId}/download`, then navigate to the returned pre-signed URL (new tab for PDF). Never prefetch or cache the URL (expires ~60 s). Button shows loading while waiting.
- Mobile: actions wrap below the text, aligned left.

## 18. NavItem (admin sidebar)

Height 40, padding-x 12, gap 12, radius `md`, icon 20. Default: icon `text.secondary`, label `label` `text.primary`. Hover: bg `bg.muted`. Active: bg `action.selected.bg`, icon + label `action.selected.fg`, label weight 600, `aria-current="page"`.

## 19. StatCard (dashboard)

Padding 20, gap 8, radius `lg`, bg `bg.surface`, 1px `border.default`, `shadow-sm`. Label `label` secondary, value `stat` `text.primary` (tabular), link text `bodySm` `text.link`. Entire card is the link (to the inventory with the matching `?estado=` filter). Hover `shadow-md`.

## 20. PublicHeader / PublicFooter

- **Header:** height 64, bg `bg.surface`, bottom 1px `border.subtle`, `position: sticky; top: 0`, z `sticky`. Inside the container: logo (32px rounded mark `action.primary.bg` with "S" + wordmark `h4`; link to `/`, `aria-label="SilvaStock — página inicial"`) on the left; "Viaturas" link (`label`, `text.link`, `aria-current` on the catalogue) on the right. Skip link "Saltar para o conteúdo" is the first focusable element (visually hidden until focused, then shown top-left as a Primary sm button).
- **Footer:** bg `bg.muted`, padding 32 (mobile 24/16), gap 16. Line 1: message-circle icon 24 + "Contacto pelo Messenger" (`h4`) — **plain text, not a link, no handle or URL** (DECISIONS §6). Line 2: "Aviso de privacidade" (link to `/privacidade`, underlined) and "© {ano} SilvaStock" (`bodySm` secondary). Mobile: the two lines stack.
- The wordmark "SilvaStock" is a placeholder until the dealership provides its name/logo (DealershipProfile is out of MVP, so it lives in frontend code).

## 21. Admin top bar and drawer (mobile/tablet)

- Top bar: height 56, padding-x 8, bg `bg.surface`, bottom 1px `border.subtle`, sticky. Menu icon button (44×44, `menu` 24, `aria-label="Abrir menu"`, `aria-expanded`, `aria-controls`) + small logo (28px mark + `label` wordmark).
- Drawer: width 288, full height, bg `bg.raised`, `shadow-lg`, from the left over `bg.overlay`; slide `--motion-duration-slow`. Contains the same blocks as the desktop sidebar (logo, nav, user block). Modal: focus trap, Esc/overlay/close button closes, focus returns to the menu button. Closes on navigation.

## 22. Bottom sheet (public mobile filters)

Full-width panel from the bottom, max-height 90vh, top radius `xl`, bg `bg.raised`, `shadow-lg`, over `bg.overlay`. Header (title `h3` "Filtros" + close 44×44), scrollable body (padding 8/16/16), sticky footer with top 1px `border.subtle`: Secondary "Limpar" + Primary "Ver {n} resultados" (fills the rest; the count updates live). Modal `<dialog>` behaviour as in §10. Filters inside the sheet apply only when "Ver {n} resultados" is pressed (the count is a live preview); on desktop the sidebar filters apply immediately.

## 23. Save bar (admin vehicle form)

Sticky to the bottom of the viewport, shown only when the form has unsaved changes. Padding 12/24 (mobile 12/16), bg `bg.raised`, top 1px `border.default`, `shadow-lg`. Left: "Alterações por guardar" (`label` secondary). Right: Ghost "Descartar" (desktop/tablet only; confirm if any) + Primary "Guardar". Warn on navigation away with unsaved changes (ConfirmDialog "Sair sem guardar?" for in-app navigation, `beforeunload` for tab close).
