# Deployment Runbook (Pilot) — Hostinger VPS + Docker Compose

Target: a handful of pilot users, no SLA. One VPS runs Caddy (TLS), the Longblack API (serving the React SPA) and PostgreSQL.

```
Internet → Caddy :80/:443 → longblack-prod-api :8080 → longblack-prod-db :5432 (internal only)
```

For local production-like runs, Caddy uses host ports `8080`/`8443`. On the VPS, change its mappings in `docker-compose.prod.yml` to `80:80` and `443:443` so public HTTPS works. PostgreSQL stays bound to `127.0.0.1:45432:5432`.

Placeholders: `<DOMAIN>` (e.g. `stock.example.com`, TBD), `<VPS_IP>`, `<DEPLOY_USER>`.

## 0. Files in the repo (done)

- `src/Longblack.Api/Dockerfile` — builds the React app and bundles it into `wwwroot`.
- `src/Longblack.Api/Program.cs` — serves the SPA when `wwwroot/index.html` exists, forwarded headers, HTTPS redirect only in Development.
- `docker-compose.prod.yml`, `Caddyfile`, `.env.example` — copy `.env.example` to `.env` on the server and fill in real values (`DOMAIN`, `POSTGRES_PASSWORD`, `JWT_KEY` >= 32 chars, `ADMIN_PASSWORD`).

Files to place on the VPS in `~/longblack/`: `docker-compose.prod.yml`, `Caddyfile`, `.env`, `db/`, and the generated `identity-initial.sql`.

## 1. Provision the VPS

1. Buy Hostinger KVM 2 (KVM 1 is enough if images are built off-box), Ubuntu 24.04.
2. Create `<DEPLOY_USER>`, add your SSH public key, disable root login and password auth (`/etc/ssh/sshd_config`).
3. `ufw allow 22,80,443/tcp && ufw enable`; install `fail2ban`; enable `unattended-upgrades`.
4. Install Docker Engine + compose plugin; add `<DEPLOY_USER>` to the `docker` group.

## 2. DNS

Create an `A` record `<DOMAIN>` → `<VPS_IP>` at your DNS host. Wait until `nslookup <DOMAIN>` resolves (Caddy needs this to get a certificate).

## 3. Build and ship the image (manual)

Build locally (so the VPS doesn't compile), from repo root:

```powershell
docker build -f src/Longblack.Api/Dockerfile -t longblack-prod-api:latest .
docker save longblack-prod-api:latest | ssh <DEPLOY_USER>@<VPS_IP> "docker load"
```

(Later: push to GHCR from GitHub Actions and `docker compose pull` on the VPS.)

## 4. First start

```bash
# on VPS, in ~/longblack/ with docker-compose.prod.yml, Caddyfile, .env
docker compose -f docker-compose.prod.yml up -d longblack-prod-db   # DB only; apply the schema (step 5) before starting the API
```

## 5. Apply the hand-written business schema

Copy `db/` to the VPS and apply in order. **Skip seed/test files `002`, `006`, `007`.**

```bash
for f in 001-catalogue-schema 003-goods-receiving-schema 004-stock-take-schema 005-multi-store-transfers-schema 008-suppliers-schema 010-purchase-orders-schema; do
  docker exec -i longblack-prod-db psql -U postgres -d Longblack.Prod < db/$f.sql
done
```

Scripts are idempotent, so re-running is safe. Check whether any needed reference data (e.g. initial Store) lives only in a skipped seed file, and insert it by hand.

## 6. Apply the Identity migration

Identity tables use EF Core migrations. From the repository root on your development machine, generate the idempotent SQL for the initial Identity migration:

```powershell
dotnet ef migrations script 0 20260826064245_InitialIdentity `
  --idempotent `
  --project .\src\Longblack.Infrastructure\Longblack.Infrastructure.csproj `
  --startup-project .\src\Longblack.Api\Longblack.Api.csproj `
  --output .\identity-initial.sql
```

Transfer and apply it:

```powershell
scp .\identity-initial.sql <DEPLOY_USER>@<VPS_IP>:~/longblack/identity-initial.sql
ssh <DEPLOY_USER>@<VPS_IP> "cd ~/longblack && docker exec -i longblack-prod-db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.Prod < identity-initial.sql"
```

The initial migration predates `AppUser.IsActive`. Add its column as well:

```bash
docker exec longblack-prod-db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.Prod -c 'ALTER TABLE users ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;'
```

## 7. Start the app

```bash
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f longblack-prod-api   # DatabaseSeeder creates roles + Admin on startup
```

## 8. Verify

- `https://<DOMAIN>` loads the SPA with a valid certificate.
- Log in as the seeded Admin; then **change the admin password**.
- `POST /api/auth/register` (Admin-only) to create Manager/Staff users for testers.
- `nc -zv <VPS_IP> 5432` from your machine must fail (DB not exposed).

## 9. Backups

Nightly cron on the VPS:

```bash
0 2 * * * docker exec longblack-prod-db pg_dump -U postgres Longblack.Prod | gzip > /home/<DEPLOY_USER>/backups/lb-$(date +\%F).sql.gz && find /home/<DEPLOY_USER>/backups -mtime +7 -delete
```

Plus weekly Hostinger VPS snapshot. Later: `rclone` the dumps off-box. **Do a test restore once** before inviting users.

## 10. Updating

1. Build + `docker save | ssh ... docker load` the new image.
2. Apply any new `db/NNN-*.sql` scripts.
3. `docker compose -f docker-compose.prod.yml up -d longblack-prod-api`.

## 11. Later (not now)

GitHub Actions: build → push to GHCR → SSH deploy; a small script that applies unapplied `db/` files; off-box backups; uptime monitor (e.g. UptimeRobot); migrate to Azure App Service + managed Postgres if uptime requirements grow.
