# SilvaStock — Requirements and acceptance criteria

> Derived from the original specification (Notion) and aligned with `DECISIONS.md` (2026-11-14).
> If they conflict, `DECISIONS.md` wins. Fields, routes and HTTP codes: see `api/openapi.yaml`.

## 1. Users
- **Visitor:** browses, searches, filters and sorts published vehicles; sees the detail page and photos. No account. Cannot access the back office or private data. The site only shows the message "Contacto pelo Messenger".
- **Administrator (2–3):** individual Google account on the allowlist, all with the same permissions. Manages inventory, photos and documents.

## 2. Acceptance criteria

**AC-01 — Login (Phase 2)**
- Given a verified Google email that is active in `admin_users`, when the user logs in, then they reach the back office.
- An email that is not on the allowlist, inactive or unverified does not get a session, and the message is generic (it does not reveal whether the email exists).
- Repeated attempts are rate limited (429).
- Without a session, any `/api/admin/*` returns 401; with a session outside the allowlist, 403.
- Logout invalidates the session.

**AC-02 — Create a Draft (Phase 3)**
- With a valid session and make + model filled in, the vehicle is created as `Draft` and does not appear in the public API or on the site.

**AC-03 — Publish (Phases 3/4)**
- The backend validates make, model, year, mileage, fuel, transmission, a valid price OR "price on request", and ≥1 primary photo.
- On success: status `Published`, `publishedAt` set, site rebuild triggered, vehicle visible on the site after the rebuild.
- On failure: 422 with the list of missing fields; the status does not change.

**AC-04 — Unpublish, sell, reopen (Phase 3)**
- `Published → Draft` and `Published → Sold` remove the vehicle from the public API immediately (404 on the detail endpoint) and from the site after the rebuild.
- A sold vehicle stays in the admin inventory, with `soldAt`.
- `Sold → Draft` needs no extra confirmation and is recorded in the audit log.
- Any other transition returns 409. Deleting is only allowed in `Draft`.

**AC-05 — Photos (Phase 4)**
- Files with a fake extension, MIME type or content are rejected (415).
- Above 10 MB or above 30 photos per vehicle: rejected.
- Stored photos have no EXIF; a WebP thumbnail exists; the storage key is random.
- Photos can be reordered, set as primary and deleted.

**AC-06 — Private documents (Phase 6)**
- A valid document (PDF/JPEG/PNG, ≤15 MB) is stored in the private bucket, with metadata in the DB.
- Without a session there is no download, even when the IDs or URL are known.
- A document belonging to another vehicle returns 404 (anti-IDOR).
- The pre-signed URL expires in ~60 s.

**AC-07 — Internal data is never public (all phases)**
- For a vehicle with a registration number, VIN, internal notes and documents, none of these appear in the public API JSON or in the generated site HTML/JSON.

## 3. Non-functional requirements
| ID | Requirement | Verification |
|---|---|---|
| NFR-01 | LCP of public pages < 2.5 s (mobile, simulated 4G) | Lighthouse in Phase 5 |
| NFR-02 | P95 of regular endpoints < 500 ms with a warm API (excludes uploads and cold start) | Simple test in Phase 7 |
| NFR-03 | Pagination in the catalogue and inventory | Integration tests |
| NFR-04 | HTTPS on every exposed environment | Deploy (Phase 7) |
| NFR-05 | Backend authorisation on every private endpoint | 401/403 tests |
| NFR-06 | Protection against brute force, CSRF, XSS, SQL injection, IDOR | Tests + security review |
| NFR-07 | Validated and size-limited uploads | Upload tests |
| NFR-08 | Automatic DB backups and a **tested restore** | Phase 7 |
| NFR-09 | Storage ↔ DB consistency on partial upload/delete failures | Integration tests |
| NFR-10 | Separation of Domain / Application / Infrastructure / Api | Review |
| NFR-11 | Responsive (mobile, tablet, desktop); modern browsers | Playwright (viewports) |
| NFR-12 | SEO: stable URLs, `canonical`, per-vehicle metadata and OG, sitemap | Phase 5 |
| NFR-13 | WCAG 2.2 AA accessibility on the main flows | axe + manual review |
| NFR-14 | Structured logs and `/health`, without secrets, tokens, VIN, registration numbers or personal data | Review |

## 4. Test strategy
- **Unit (xUnit):** domain rules, transitions and publish validation.
- **Integration (Testcontainers/Postgres + WebApplicationFactory):** endpoints, authorisation, validation, persistence, filters/pagination, public DTOs (check the serialised JSON), invalid uploads, IDOR.
- **Frontend (Vitest + RTL):** forms and validation; loading, empty and error states; catalogue filters.
- **E2E (Playwright, Phase 7):**
  1. Admin login (with a simulated OIDC provider in the test environment).
  2. Create a Draft vehicle.
  3. Upload photos and set the primary one.
  4. Upload a private document.
  5. Publish.
  6. As a visitor, see the vehicle in the catalogue and on its detail page.
  7. Confirm that the document, VIN, registration number and notes are not public.
  8. Mark it as sold.
  9. Confirm it is no longer public and is still in the admin inventory.
- Test data is always fictitious.

## 5. Security and privacy risks
1. **Document exposure** (registration certificate, purchase documents): they contain personal data → private bucket, short-lived URLs, anti-IDOR.
2. **Internal data exposure:** explicit, tested public DTOs.
3. **Malicious uploads:** validate the real content and re-encode images.
4. **Login brute force / abuse:** rate limiting; login is delegated to Google.
5. **CSRF / XSS / IDOR:** antiforgery, React escaping, checking the vehicle↔resource relationship.
6. **GDPR:** with no Leads in the MVP, the only personal data is the admins' emails and the documents. The site needs a minimal privacy notice (e.g. hosting, logs) — review in Phase 5.
7. **Backups:** test the recovery, not just the creation.
8. **Secrets:** only in user-secrets / Azure and GitHub secrets.
9. **Logs:** never log tokens, documents, VIN or registration numbers.

## 6. Outside the MVP (do not implement without approval)
Leads and contact form · editable DealershipProfile · phone/WhatsApp · Reserved status · full change history (only the minimal status audit log) · duplicate vehicle · CSV export · favourites/comparison · videos · AI features · multi-dealership · admin management screen · email notifications.
