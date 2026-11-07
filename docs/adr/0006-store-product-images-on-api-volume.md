# Store product images on an API-served volume

Product and Colour Images are files on a Docker volume mounted into the API container and served by the API at public, unguessable (GUID-named) URLs. Longblack deploys to a single VPS, so this adds no extra service and keeps the database small. Rejected: object storage (S3/R2/Blob), an extra moving part we don't need yet; and Postgres `bytea`, which bloats the database and backups.

Access goes through a storage interface so a move to object storage stays cheap. Consequence: the volume must be included in VPS backups, since the database alone no longer holds everything.
