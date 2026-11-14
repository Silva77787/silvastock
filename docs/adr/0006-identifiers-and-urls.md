# ADR-0006 — `bigint identity` IDs and slug-less public URLs

**Status:** Accepted · 2026-11-14

## Context
Public URLs should be stable and readable for SEO. Persisted slugs complicate edits (make/model change) and require uniqueness and redirects.

## Decision
- `vehicles.id` is **`bigint GENERATED ALWAYS AS IDENTITY`**. All other tables also use `bigint identity`.
- **No slug column.** Public URL: `/carros/{id}-{generated-text}` (e.g. `/carros/42-bmw-320d-2019`). The text is generated in the frontend from make, model, version and year (lowercase, no accents, `-`). The `carros` segment stays in Portuguese for the local audience.
- Only the `id` is used to fetch the vehicle; the text is cosmetic. The page declares `<link rel="canonical">` with the current text.

## Consequences
- + Editing make/model does not break links (the id stays); no uniqueness logic.
- − Sequential IDs reveal the approximate inventory size and are enumerable. Accepted: the public API only returns `Published`, and every admin endpoint requires authorisation and checks the vehicle↔resource relationship (anti-IDOR).
- − With a static site, `generateStaticParams` generates one path per vehicle, so `/carros/42-old-text` does not exist as a file. Mitigation: a Cloudflare Pages redirect rule/`404.html` that extracts the id and redirects to the current path (details in Phase 5).
