#!/usr/bin/env bash
set -Eeuo pipefail
cd "$(dirname "$0")"

schema_files=(
  db/identity-initial.sql
  db/001-catalogue-schema.sql
  db/003-goods-receiving-schema.sql
  db/004-stock-take-schema.sql
  db/005-multi-store-transfers-schema.sql
  db/008-suppliers-schema.sql
  db/009-sales-schema.sql
  db/010-purchase-orders-schema.sql
  db/011-product-images-schema.sql
)

for file in "${schema_files[@]}"; do
  [[ -f "$file" ]] || { echo "Missing file: $file" >&2; exit 1; }
done

docker exec longblack-prod-db pg_isready -U postgres -d Longblack.Prod

for file in "${schema_files[@]}"; do
  echo "Applying $file"
  docker exec -i longblack-prod-db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.Prod < "$file"
done

echo "Database scripts completed."