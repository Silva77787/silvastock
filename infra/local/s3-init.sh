#!/bin/sh
# Creates the local buckets on RustFS (idempotent).
# - photos:    public read (anonymous GET on objects), like the public R2 bucket.
# - documents: private (no policy), like the private R2 bucket.
set -eu

ENDPOINT="http://s3:9000"
aws configure set default.s3.addressing_style path

s3() { aws --endpoint-url "$ENDPOINT" "$@"; }

echo "Waiting for RustFS at $ENDPOINT..."
i=0
until s3 s3api list-buckets >/dev/null 2>&1; do
  i=$((i + 1))
  if [ "$i" -ge 30 ]; then
    echo "RustFS did not become ready in time." >&2
    exit 1
  fi
  sleep 2
done

for bucket in "$PHOTOS_BUCKET" "$DOCUMENTS_BUCKET"; do
  if s3 s3api head-bucket --bucket "$bucket" >/dev/null 2>&1; then
    echo "Bucket '$bucket' already exists."
  else
    s3 s3api create-bucket --bucket "$bucket" >/dev/null
    echo "Created bucket '$bucket'."
  fi
done

s3 s3api put-bucket-policy --bucket "$PHOTOS_BUCKET" --policy "{
  \"Version\": \"2012-10-17\",
  \"Statement\": [{
    \"Sid\": \"PublicReadPhotos\",
    \"Effect\": \"Allow\",
    \"Principal\": {\"AWS\": [\"*\"]},
    \"Action\": [\"s3:GetObject\"],
    \"Resource\": [\"arn:aws:s3:::$PHOTOS_BUCKET/*\"]
  }]
}"
echo "Bucket '$PHOTOS_BUCKET' is publicly readable; '$DOCUMENTS_BUCKET' stays private."
