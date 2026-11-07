# Deployment Runbook (Pilot) — Hostinger VPS + Docker Compose

This runbook deploys the Longblack API, its React SPA, Caddy, and PostgreSQL to one Hostinger VPS. It is intended for a small pilot, not a high-availability production environment.

```text
Internet -> Caddy :80/:443 -> longblack-prod-api :8080 -> longblack-prod-db :5432
```

On the VPS, Caddy publishes ports 80 and 443. PostgreSQL is published only on VPS loopback at `127.0.0.1:45432`; do not open database port 5432 to the internet.

Replace `<DOMAIN>`, `<VPS_IP>`, and `<DEPLOY_USER>` throughout. Keep secrets in the VPS `.env` file and out of source control.

## 1. Provision the VPS

Use Ubuntu 24.04. Create a non-root deployment user, install the local machine's SSH public key in that user's `authorized_keys`, grant the user sudo access, and verify a new SSH login. Confirm `sudo whoami` prints `root` before closing the original root session.

Root SSH login and SSH password authentication were not disabled during the setup walkthrough. After key-based deployment-user access and sudo are verified, disable them as a hardening step; keep an existing session open and test a fresh login before closing it.

Allow SSH and web traffic in both UFW and the Hostinger firewall, if enabled:

```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

Install Docker Engine and the Compose plugin. Verify with `docker compose version`. Adding the deployment user to the `docker` group is convenient, but grants root-equivalent privileges. Consider `fail2ban` and `unattended-upgrades` as additional host hardening.

## 2. Configure DNS and domain

At the DNS provider for the registered domain, create:

- An `A` record for the apex (`@`) pointing to `<VPS_IP>`.
- A `CNAME` record named `www` pointing to `<DOMAIN>` if both the apex and `www.<DOMAIN>` should work. The repository Caddyfile is configured for both.

Set `DOMAIN=<DOMAIN>` in `~/longblack/.env`. The value must match the apex domain exactly, without `https://` or a trailing slash. Wait for DNS to resolve before starting Caddy; it needs the public DNS records and reachable ports 80/443 to obtain TLS certificates.

Check from local PowerShell:

```powershell
Resolve-DnsName <DOMAIN> -Type A
Resolve-DnsName www.<DOMAIN> -Type A
```

Both names should resolve to the VPS address (the `www` lookup may follow its CNAME).

## 3. Copy deployment files and configure `.env`

From local PowerShell at the repository root, copy the Compose/Caddy configuration and database scripts:

```powershell
ssh <DEPLOY_USER>@<VPS_IP> "mkdir -p ~/longblack"
scp .\docker-compose.prod.yml .\Caddyfile .\.env.example <DEPLOY_USER>@<VPS_IP>:~/longblack/
scp -r .\db <DEPLOY_USER>@<VPS_IP>:~/longblack/
scp .\db\apply-db-scripts.sh <DEPLOY_USER>@<VPS_IP>:~/longblack/apply-db-scripts.sh
```

The production Compose file must publish Caddy as `80:80` and `443:443`. PostgreSQL should remain bound to `127.0.0.1:45432:5432`.

On the VPS, create `.env` only if it does not already exist. Do not overwrite an existing `.env` with `.env.example`:

```bash
cd ~/longblack
if [ ! -f .env ]; then cp .env.example .env; fi
chmod 600 .env
nano .env
```

Set `DOMAIN`, `POSTGRES_DB=Longblack.Prod`, a strong `POSTGRES_PASSWORD`, a `JWT_KEY` of at least 32 characters, `ADMIN_EMAIL`, and a strong `ADMIN_PASSWORD`. Generate random values with `openssl rand -hex 32`. Never paste secrets into chat or commit `.env`. Changing `POSTGRES_PASSWORD` in `.env` does not change the password inside an already-initialized database volume.

Compose reads `.env` for interpolation when commands are run from `~/longblack` with `-f docker-compose.prod.yml`.

## 4. Build and transfer the API image

Build on the local machine so the VPS does not compile the .NET and React applications. For an x86-64 VPS, run from the repository root in PowerShell:

```powershell
docker build --platform linux/amd64 -f src/Longblack.Api/Dockerfile -t longblack-prod-api:latest .
docker save -o "$env:TEMP\longblack-prod-api.tar" longblack-prod-api:latest
scp "$env:TEMP\longblack-prod-api.tar" <DEPLOY_USER>@<VPS_IP>:~/longblack-prod-api.tar
```

On the VPS, load and verify the image:

```bash
docker load -i ~/longblack-prod-api.tar
docker image ls longblack-prod-api
```

The ImageSharp 4.1.3 dependency produced a build error during this setup because no Six Labors license was available to the build. Resolve the package's licensing/build requirement or replace the dependency before rebuilding. Do not commit a license key or put it in the image.

## 5. Start PostgreSQL and apply the database SQL

Run all Compose commands from the VPS project directory and include the Compose file explicitly:

```bash
cd ~/longblack
docker compose -f docker-compose.prod.yml config --quiet
docker compose -f docker-compose.prod.yml up -d longblack-prod-db
docker exec longblack-prod-db pg_isready -U postgres -d Longblack.Prod
```

Wait for `pg_isready` to report `accepting connections`. Check that `db/identity-initial.sql` and the numbered schema scripts are present on the VPS. The Identity SQL includes the `IsActive` column. The helper also applies the business schemas, including sales (`009`) and product images (`011`); it skips the seed/test scripts (`002`, `006`, and `007`).

The helper is copied to `~/longblack/apply-db-scripts.sh` and expects the `db/` directory beside it:

```bash
cd ~/longblack
chmod 700 apply-db-scripts.sh
bash -n apply-db-scripts.sh
bash ./apply-db-scripts.sh
```

`bash -n` only checks shell syntax; it prints nothing on success. The next command runs the SQL and stops at the first SQL error. Do not start the API unless the helper completes successfully. The schema scripts are designed to be idempotent, so they can be rerun after fixing a failed script. Confirm any required initial reference data, especially the Warehouse Store, exists; the skipped test seed files are not production data.

## 6. Start the application

Confirm `DOMAIN` is set in the VPS `.env`, DNS resolves for both configured hostnames, and TCP ports 80/443 are open. Start the services using the already-loaded API image:

```bash
cd ~/longblack
docker compose -f docker-compose.prod.yml up -d --no-build
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail=100 longblack-prod-api
docker compose -f docker-compose.prod.yml logs --tail=100 longblack-prod-caddy
```

If the Caddyfile or `DOMAIN` changes after Caddy is already running, recreate only Caddy so it reloads the mounted file and environment:

```bash
docker compose -f docker-compose.prod.yml up -d --no-build --force-recreate longblack-prod-caddy
```

The API's startup seeder creates the roles and initial Admin. Caddy obtains TLS certificates after DNS is correct and ports 80/443 are reachable.

## 7. Verify and connect

- Open `https://<DOMAIN>` and, if configured, `https://www.<DOMAIN>`; confirm the certificate is valid and the SPA loads.
- Sign in with the seeded Admin account, then change its password.
- Use the Admin-only `POST /api/auth/register` endpoint to create Manager/Staff users for pilot testers.
- From local PowerShell, `Test-NetConnection <VPS_IP> -Port 5432` should fail; PostgreSQL must not be public.

To connect from pgAdmin without exposing PostgreSQL, open an SSH tunnel in local PowerShell and keep that window running:

```powershell
ssh -N -L 5433:127.0.0.1:45432 <DEPLOY_USER>@<VPS_IP>
```

In pgAdmin use host `127.0.0.1`, port `5433`, maintenance database `Longblack.Prod`, user `postgres`, and the `POSTGRES_PASSWORD` from the VPS `.env`.

## 8. Reset and troubleshooting

`docker compose` reports `no configuration file provided` when run from a directory without a Compose file. Use `cd ~/longblack` and `docker compose -f docker-compose.prod.yml ...`.

Do not use `docker compose down -v` for normal deploys or updates. It permanently removes all Compose volumes, including PostgreSQL data, uploaded product images, and Caddy's certificate/configuration data. Only use it when intentionally wiping all of that data. It does not remove `.env`, SQL files, or the separately loaded Docker image.

If a clean database is intentionally required, back up anything needed first. After resetting the volumes, start only PostgreSQL, wait for readiness, and rerun the SQL helper before starting the API.

## 9. Backups and updates

Back up both PostgreSQL and the `longblack-prod-images` Docker volume; the database does not contain uploaded image files. Keep an off-VPS copy and perform a restore test before inviting users. A weekly Hostinger snapshot is additional protection, not a substitute for an offsite backup.

For an update, build and transfer the new image, load it on the VPS, apply any new numbered SQL scripts, then recreate the API without building on the VPS:

```bash
docker compose -f docker-compose.prod.yml up -d --no-build longblack-prod-api
```

Later improvements may include GitHub Actions/GHCR, automated ordered SQL migration tracking, offsite backups, and uptime monitoring.
