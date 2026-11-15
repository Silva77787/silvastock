# ADR-0005 — Files: Cloudflare R2 with a public and a private bucket

**Status:** Accepted · 2026-11-14

## Context
Photos are public and read often; documents (registration certificate, purchase, inspection…) contain personal data and are strictly private. Cost ~zero; no egress fees.

## Decision
- **Cloudflare R2**, accessed through the **AWS SDK for .NET (S3)**, behind an `IObjectStorage` interface in Application (testable with a fake or a local RustFS container, see ADR-0008).
- **Public bucket (`photos`)**: served from R2's public domain (or our own subdomain once we have a domain).
  - Uploads go **through the API** (multipart): validate extension + MIME + magic bytes; accept JPEG, PNG, WebP; max **10 MB**, max **30 photos per vehicle**.
  - Re-encoding with **ImageSharp** (strips EXIF/GPS, applies orientation), a web version + **WebP thumbnail**.
  - Random keys (`vehicles/{vehicleId}/{guid}.webp`), never the client-supplied file name.
- **Private bucket (`documents`)**: never public.
  - Accepts PDF, JPEG, PNG (validated by magic bytes); max **15 MB**.
  - Download: `GET /api/admin/vehicles/{id}/documents/{docId}/download` checks the session **and** that `docId` belongs to `vehicleId`, then responds `302` to a short-lived **pre-signed URL** (proposal: 60 s) with `Content-Disposition: attachment`.
- Operation order: write to storage → save metadata in the DB; on failure, delete the object (compensation). On delete: DB first, object second (orphans are tolerated and cleaned by a job, never the other way round).

## Consequences
- + Physical separation between public and private; no egress fees; photos stripped of metadata.
- + Uploading through the API keeps validation in the backend (no trust in pre-signed upload URLs).
- − Photo uploads use API CPU/memory (ImageSharp). Acceptable with `maxReplicas: 1` and size limits.
- − ImageSharp uses the "Six Labors Split License": free for open source / low-revenue projects, and recent versions require a build-time licence key — see "Free limits verified on 2026-11-14" (**owner action pending**).
- Antivirus scanning of documents: outside the MVP (only authenticated admins upload); reassess if the risk changes.

## Free limits verified on 2026-11-14
| Limit | Value | Source |
|---|---|---|
| R2 Standard storage | 10 GB-month free | [R2 pricing](https://developers.cloudflare.com/r2/pricing/) |
| Class A operations (writes/lists) | 1 M/month free | [R2 pricing](https://developers.cloudflare.com/r2/pricing/) |
| Class B operations (reads) | 10 M/month free | [R2 pricing](https://developers.cloudflare.com/r2/pricing/) |
| Egress | Free (no cap listed) | [R2 pricing](https://developers.cloudflare.com/r2/pricing/) |
| Public access via `r2.dev` | Rate-limited, **development only**; production needs a custom domain (enables Cloudflare cache) | [Public buckets](https://developers.cloudflare.com/r2/buckets/public-buckets/) |
| Pre-signed URLs | GET/HEAD/PUT/DELETE; expiry 1 s – 7 days; **S3 endpoint only, not custom domains** | [Presigned URLs](https://developers.cloudflare.com/r2/api/s3/presigned-urls/) |
| ImageSharp licence | Six Labors Split License 1.0: Apache 2.0 if used in open-source/source-available software, as a transitive dependency, or directly by a for-profit with < 1 M USD annual gross revenue (or a non-profit); otherwise commercial licence | [ImageSharp LICENSE](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE), [Six Labors pricing](https://sixlabors.com/pricing/) |
| ImageSharp licence key | From 3.2.0 (3.x) / 4.0.0 (4.x), direct references need a licence key at build time; free community keys (assembly-scoped keys may be committed) via licensing.sixlabors.com | [ImageSharp docs](https://docs.sixlabors.com/articles/imagesharp/) |

Result: R2 fits ~zero cost (free tier applies to Standard storage only, not Infrequent Access).

### Cost risks and mitigations
- **Storage over 10 GB** (photos + documents + Phase 7 `pg_dump` backups) → existing limits (10 MB/photo before re-encoding, 30 photos/vehicle, 15 MB/document); store only re-encoded web + WebP thumbnail, never the original; orphan-cleanup job; backup retention with an R2 lifecycle rule.
- **Class A/B operations** → negligible at expected volume; photos cached by Cloudflare once served from a custom domain.
- **`r2.dev` is not for production** → serve photos from our own subdomain (e.g. `img.<domain>`) at the first deployment, which already requires a domain (ADR-0002). `r2.dev` only for development/preview.
- **Pre-signed document URLs** point to the R2 S3 endpoint (`<account>.r2.cloudflarestorage.com`), not to our domain → matches the `302` design; allow that host where needed (e.g. CSP) and keep the 60 s expiry.
- **ImageSharp licence** → eligibility depends on facts the owner must confirm (see the open action in Consequences): either the repository gets an open-source/source-available licence (it currently has no `LICENSE` file) and a community key is requested, or the dealership's annual gross revenue is < 1 M USD. Recent versions also need the key in CI (secret or committed assembly-scoped key).
