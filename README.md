# Longblack

Longblack is a stock take and inventory management system for clothing retailers. It consists of an ASP.NET Core API, a React SPA, and PostgreSQL.

## Run locally for development

This workflow runs the API from Visual Studio or `dotnet run`, uses Vite for the React development server, and runs PostgreSQL in Docker.

### Prerequisites

- .NET 10 SDK
- Node.js and npm
- Docker Desktop with Docker Compose

### Start PostgreSQL

From the repository root:

```powershell
docker compose up -d longblack-db
```

The development database is available to the host at `localhost:35432`. The API's Development configuration is set up for this database.

### Initialize a fresh database

Apply the hand-written application schema scripts. Skip `002`, `006` and `007`, which are seed/test scripts:

```powershell
foreach ($f in "001-catalogue-schema","003-goods-receiving-schema","004-stock-take-schema","005-multi-store-transfers-schema","008-suppliers-schema","010-purchase-orders-schema") {
  Get-Content "db\$f.sql" -Raw | docker exec -i Longblack.Db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.App
}
```

Apply the ASP.NET Identity migration from the repository root:

```powershell
dotnet ef database update `
  --project .\src\Longblack.Infrastructure\Longblack.Infrastructure.csproj `
  --startup-project .\src\Longblack.Api\Longblack.Api.csproj
```

The existing initial Identity migration does not include `AppUser.IsActive`. Add that column to a fresh database:

```powershell
docker exec Longblack.Db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.App -c 'ALTER TABLE users ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;'
```

### Run the API and React app

From the repository root:

```powershell
dotnet run --project .\src\Longblack.Api
```

Or start the **API + React** profile in Visual Studio. The ASP.NET Core SPA proxy starts Vite; browse to the API launch URL, normally `https://localhost:7145`. In Development, OpenAPI/Scalar is available at `/scalar`.

Build or lint the frontend from `src\Longblack.Api\ClientApp`:

```powershell
npm ci
npm run build
npm run lint
```

Build the solution from the repository root:

```powershell
dotnet build Longblack.slnx
```

## List pagination

The Products, Inventory, Suppliers, Stores, Goods Receipts, Purchase Orders,
Sales, Stock Takes, and Stock Transfers list pages use server-side pagination.
The default is 25 rows, with 10, 25, 50, and 100 rows available. Changing a filter
or the page size returns to page 1; lists recover to the last available page if
records are removed from the current result set.

Pass `page` (1-based) and `pageSize` to a list API to receive
`{ items, total, page, pageSize }`. `total` counts all matching records before
paging. Invalid or missing paging values default to page 1 and 25 rows, and
page sizes above 100 are capped. Results retain their existing sort order, with
an ID tie-breaker to keep page boundaries deterministic.

Except for `/api/products`, which always returns a paged result, omitting both
paging parameters preserves the full-array response used by dropdowns and
other lookup callers. Detail-page line items and settings tables are not paged.

## Run the production-like stack locally

This runs Caddy, the API with the built React app, and PostgreSQL in Docker. It has a separate Compose project, database (`Longblack.Prod`) and data volume from the development stack.

Create a local environment file from the template:

```powershell
Copy-Item .env.example .env
```

Edit `.env`: set `DOMAIN=localhost`, and replace the example values for `POSTGRES_PASSWORD`, `JWT_KEY` (at least 32 characters), and `ADMIN_PASSWORD`. Never commit `.env`.

Start the production database:

```powershell
docker compose -f docker-compose.prod.yml up -d longblack-prod-db
```

Apply the hand-written application schema using the same scripts listed above, changing the container name and database to `longblack-prod-db` and `Longblack.Prod`.

Run the Identity migration from the host. Postgres is published on host port `45432`; inside Docker its port remains `5432`:

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=45432;Database=Longblack.Prod;Username=postgres;Password=<POSTGRES_PASSWORD>"
dotnet ef database update `
  --project .\src\Longblack.Infrastructure\Longblack.Infrastructure.csproj `
  --startup-project .\src\Longblack.Api\Longblack.Api.csproj
```

Then add the `IsActive` column:

```powershell
docker exec longblack-prod-db psql -v ON_ERROR_STOP=1 -U postgres -d Longblack.Prod -c 'ALTER TABLE users ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;'
```

Build and start the stack:

```powershell
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml logs -f longblack-prod-api
```

Open **<https://localhost:8443>**. Caddy uses a local certificate, which may trigger a browser warning. The seeded Admin credentials come from `ADMIN_EMAIL` and `ADMIN_PASSWORD` in `.env`. Change the Admin password before sharing access.

To stop the stack and preserve its data:

```powershell
docker compose -f docker-compose.prod.yml down
```

To remove its database and all named volumes too (destructive):

```powershell
docker compose -f docker-compose.prod.yml down -v
```

## Deploy to a production VPS

The pilot deployment uses an Ubuntu VPS, Docker Compose, Caddy for HTTPS, and PostgreSQL. Follow the [deployment runbook](docs/deployment-runbook.md) for the complete, current procedure, including VPS provisioning, DNS, secrets, image transfer, SQL setup, startup, backups, and updates.

Production networking requirements: publish Caddy on host ports `80:80` and `443:443`, and keep PostgreSQL bound to `127.0.0.1:45432:5432` rather than exposing it publicly. Keep the VPS `.env` and database backups private.

## Database schema policy

Identity uses EF Core migrations. Business tables use ordered, hand-written SQL scripts in `db/`; do not use EF migrations for those tables. Apply new business-schema scripts manually in sequence.
