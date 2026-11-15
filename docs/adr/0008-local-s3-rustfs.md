# ADR-0008 — Local S3-compatible storage: RustFS (replaces MinIO)

**Status:** Accepted · 2026-11-15 (chosen by the owner)

## Context
Development and integration tests must not touch the real Cloudflare R2 (ADR-0005). We need an S3-compatible server that runs in Docker locally and in CI, and that can reproduce our two bucket modes: `photos` public-read and `documents` private with pre-signed URLs.

The plan was MinIO. In 2025 MinIO stopped distributing its free Docker images: on 2026-11-15 `minio/minio` and `minio/mc` return 404 on Docker Hub, and the `quay.io` repository lists no active tags. It is no longer a viable option.

Alternatives considered:
- **RustFS** (Apache 2.0): designed as a MinIO-compatible replacement, actively released (1.0.5).
- **SeaweedFS** (Apache 2.0): mature, but its bucket-policy support is weaker, and public vs private buckets is a central security rule for us.
- **LocalStack / S3Mock**: emulators aimed at AWS. Less faithful to R2 public-bucket behaviour, and LocalStack is moving to mandatory accounts.

## Decision
- Use **RustFS** (`rustfs/rustfs:1.0.5-glibc`) as the local and CI S3 server, through `docker-compose.yml` and, from F4-02, Testcontainers.
  - The plain `1.0.5` tag does not start: the image references a non-existent container user. The `-glibc` variant works.
- Buckets are created by a one-shot `s3-init` job (`amazon/aws-cli`, `infra/local/s3-init.sh`):
  - `photos` gets an anonymous `s3:GetObject` policy;
  - `documents` has no policy.
- Clients use **path-style** addressing locally (RustFS only supports virtual-hosted style when `RUSTFS_SERVER_DOMAINS` is set).

## Verified on 2026-11-15
| Check | Result |
|---|---|
| Anonymous GET on a `photos` object | 200 |
| Anonymous GET on a `documents` object | 403 |
| Anonymous listing of either bucket | 403 |
| Pre-signed GET on a `documents` object | 200 |
| Unsigned GET on a `documents` object | 403 |
| Re-running `s3-init` | Idempotent |

## Consequences
- + Only local development and tests are affected: production stays on R2, and the API code is identical because it talks plain S3.
- + Swapping to another S3 server later only touches `docker-compose.yml`, the init script and the Testcontainers setup.
- − RustFS is younger than MinIO or SeaweedFS. If a behaviour differs from R2, R2's documented behaviour wins and the test is adjusted.
- − Pre-signed URLs embed the host they were signed for (`s3:9000` inside Docker, `localhost:9000` from the host), so the API must sign with the endpoint the client will call.
