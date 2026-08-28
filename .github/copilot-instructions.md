# Longblack — Copilot Instructions

## Project Overview

Longblack is a stock take and inventory management system for clothing retail businesses. The product vision is in `docs/stocktake-prd.md` and should be the primary reference for domain logic, terminology, and feature scope.

## Architecture

Lightweight modular monolith targeting .NET 10 with a React SPA hosted by ASP.NET Core.

```
Longblack.Api          — ASP.NET Core Web API + React SPA host
Longblack.Application  — Application services (no CQRS/MediatR; use simple services)
Longblack.Domain       — Domain entities and business rules
Longblack.Infrastructure — EF Core, PostgreSQL, Identity
```

The React frontend lives at `src/Longblack.Api/ClientApp/` and is built as static files served by the .NET host at runtime. There is no separate frontend server in production.

## Key Domain Concepts

- **ProductVariant** identifies _what_ a stock item is (the countable SKU).
- **GoodsReceipt** records _what arrived_ (with purchase cost per line).
- **Inventory** records _what the system currently has_ (one row per variant, quantity only).
- **StockTake** records _what was physically counted_ and drives adjustments.
- Inventory changes must always create an `InventoryTransaction` record (traceable source + user).
- Purchase cost lives on `GoodsReceiptLine`, not on the product or variant.
- A Goods Receipt must work **without** a purchase order (PO is a future feature). The `GoodsReceipt` entity should have an optional `purchase_order_id`.

## Tech Stack

**Backend:** ASP.NET Core · .NET 10 · C# · EF Core · PostgreSQL · ASP.NET Core Identity  
**Frontend:** React 19 · TypeScript · Vite · (planned: Material UI, TanStack Query, React Hook Form, Zod)  
**Linter (frontend):** oxlint

## Build & Run Commands

### Backend
```bash
dotnet build Longblack.slnx
dotnet run --project src/Longblack.Api
```

### Frontend (from `src/Longblack.Api/ClientApp/`)
```bash
npm install
npm run dev       # Vite dev server
npm run build     # tsc + vite build
npm run lint      # oxlint
```

## Key Conventions

- Use **simple application services** — do not introduce MediatR, CQRS pipelines, or event-driven patterns unless the PRD explicitly requires them.
- **Nullable reference types** are enabled across all projects (`<Nullable>enable</Nullable>`).
- **Implicit usings** are enabled — no need to add common `using` directives manually.
- API routes follow the pattern `/api/{resource}` (e.g., `/api/products`, `/api/goods-receipts`).
- Database schema uses **snake_case** table and column names (e.g., `product_variants`, `goods_receipt_lines`).
- The architecture must accommodate future multi-store support: `Inventory` should be designed so a `location_id`/`store_id` FK can be added later without breaking existing data.
- Status fields use **string enums** defined in the domain (e.g., `Draft`, `Received`, `Cancelled` for `GoodsReceipt`).
- Audit fields (`CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`) are required on all transactional entities.

## Authentication

- **JWT Bearer tokens** — issued at `POST /api/auth/login`, validated on all protected endpoints
- **Roles:** `Admin`, `Manager`, `Staff` — seeded at startup via `DatabaseSeeder`
- JWT settings live in `appsettings.json` under `JwtSettings` (`Issuer`, `Audience`, `Key`, `ExpiryMinutes`)
- **Never commit real secrets** — override `JwtSettings:Key`, `ConnectionStrings:Default`, and `Seed:AdminPassword` via environment variables or `dotnet user-secrets`
- `POST /api/auth/register` is Admin-only (`[Authorize(Roles = "Admin")]`)
- `AppUser` (extends `IdentityUser`) lives in `Longblack.Domain.Identity`
- `AppDbContext` (extends `IdentityDbContext<AppUser>`) lives in `Longblack.Infrastructure.Persistence`
- Identity tables use snake_case names (`users`, `roles`, `user_roles`, etc.) — configured in `OnModelCreating`
- Infrastructure DI is wired via `services.AddInfrastructure(configuration)` in `Program.cs`
- EF Core migration command: `dotnet tool restore && dotnet ef migrations add <Name> --project src/Longblack.Infrastructure --startup-project src/Longblack.Api`

## What Is Intentionally Out of Scope (MVP)

Do not implement: multi-store, purchase orders, sales/POS, customer management, promotions, native mobile apps, or container orchestration. See `docs/stocktake-prd.md` section 2.2 for the full list.
