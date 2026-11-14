# ADR-0005 — Ficheiros: Cloudflare R2 com bucket público e bucket privado

**Estado:** Aceite · 2026-11-14

## Contexto
Fotos são públicas e muito lidas; documentos (DUA, compra, inspeção…) contêm dados pessoais e são estritamente privados. Custo ~0; sem custo de egress.

## Decisão
- **Cloudflare R2**, acedido pelo **AWS SDK for .NET (S3)**, atrás de uma interface `IObjectStorage` na Application (testável com fake/MinIO).
- **Bucket público (`photos`)**: servido por domínio público do R2 (ou subdomínio próprio quando houver domínio).
  - Upload passa **pela API** (multipart): valida extensão + MIME + magic bytes; aceita JPEG, PNG, WebP; máx. **10 MB**, máx. **30 fotos por veículo**.
  - Re-codificação com **ImageSharp** (remove EXIF/GPS, aplica orientação), versão web + **thumbnail WebP**.
  - Chaves aleatórias (`vehicles/{vehicleId}/{guid}.webp`), nunca o nome enviado pelo cliente.
- **Bucket privado (`documents`)**: nunca público.
  - Aceita PDF, JPEG, PNG (validados por magic bytes); máx. **15 MB**.
  - Download: `GET /api/admin/vehicles/{id}/documents/{docId}/download` verifica sessão **e** que `docId` pertence a `vehicleId`, depois responde `302` para uma **URL pré-assinada** de curta duração (proposta: 60 s) com `Content-Disposition: attachment`.
- Ordem nas operações: escrever no storage → gravar metadados na BD; em falha, apagar o objeto (compensação). Ao apagar: BD primeiro, objeto depois (órfãos tolerados e limpos por job, nunca o contrário).

## Consequências
- + Separação física entre público e privado; sem egress; fotos limpas de metadados.
- + Upload via API mantém a validação no backend (sem confiar em URLs pré-assinadas de upload).
- − Uploads de fotos consomem CPU/memória da API (ImageSharp). Aceitável com `maxReplicas: 1` e limites de tamanho.
- − ImageSharp tem licença "Six Labors Split License": grátis para projetos open source/receita baixa — **confirmar enquadramento** antes de uso comercial.
- Antivírus em documentos: fora do MVP (só admins autenticados fazem upload); reavaliar se o risco mudar.
