# SilvaStock — web

Next.js (App Router, TypeScript `strict`) exported as static files (`output: 'export'`) for
Cloudflare Pages. Public site at `/`, back office at `/admin/`.

No server features: no API routes, middleware/proxy, server actions, `headers()`/`cookies()`
or dynamic rendering.

## Setup

```bash
cp .env.example .env.local   # set NEXT_PUBLIC_API_BASE_URL
npm ci
```

## Scripts

| Script                  | What it does                                                          |
| ----------------------- | --------------------------------------------------------------------- |
| `npm run dev`           | Development server                                                    |
| `npm run build`         | Static export to `out/`                                               |
| `npm start`             | Serves `out/` locally (after `build`)                                 |
| `npm run lint`          | ESLint (Next.js config), no warnings allowed                          |
| `npm run typecheck`     | Generates Next.js route types, then `tsc --noEmit`                    |
| `npm test`              | Vitest + React Testing Library (single run)                           |
| `npm run format`        | Prettier (write)                                                      |
| `npm run format:check`  | Prettier (check only)                                                 |
| `npm run gen:api`       | Regenerates `src/lib/api/schema.d.ts` from `../docs/api/openapi.yaml` |
| `npm run gen:api:check` | Fails if the committed `schema.d.ts` is out of date (CI)              |

`src/lib/api/schema.d.ts` is generated: never edit it by hand.
