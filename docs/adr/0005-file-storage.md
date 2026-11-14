# ADR-0005 — Files: Cloudflare R2 with a public and a private bucket

**Status:** Accepted · 2026-11-14

## Context
Photos are public and read often; documents (registration certificate, purchase, inspection…) contain personal data and are strictly private. Cost ~zero; no egress fees.

## Decision
- **Cloudflare R2**, accessed through the **AWS SDK for .NET (S3)**, behind an `IObjectStorage` interface in Application (testable with a fake/MinIO).
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
- − ImageSharp uses the "Six Labors Split License": free for open source / low-revenue projects — **confirm eligibility** before commercial use (issue F1-08).
- Antivirus scanning of documents: outside the MVP (only authenticated admins upload); reassess if the risk changes.
