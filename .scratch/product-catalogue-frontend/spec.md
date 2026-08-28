# Product Catalogue Frontend

Status: ready-for-agent

## Problem Statement

The Longblack system has a complete product catalogue API (Brands, Categories, Colours, Sizes, Products, ProductVariants) but no user interface. Staff and managers currently have no way to interact with the system except by calling the API directly. The business cannot use the product catalogue until a web UI exists.

## Solution

Build a React SPA inside the existing `ClientApp` skeleton that gives authenticated users a working product catalogue UI: a login page, a sidebar-nav shell, and full CRUD for Products and ProductVariants. The SPA is served by the existing ASP.NET Core host, so no separate deployment is needed.

## User Stories

1. As a user, I want to see a login page when I open the app unauthenticated, so that I can enter my credentials and access the system.
2. As a user, I want to submit my email and password to log in, so that I receive a JWT and can access protected pages.
3. As a user, I want to see an error message when my credentials are wrong, so that I know the login failed.
4. As a user, I want my session to persist across page refreshes, so that I don't have to log in every time I reload the browser.
5. As a user, I want to be automatically redirected to the login page when my session expires (401 response), so that I'm never stuck on a broken page.
6. As a user, I want a "Log out" button in the navigation, so that I can end my session.
7. As a user, I want to see a sidebar with navigation links (Dashboard, Products, Receiving, Inventory, Stock Take, Suppliers, Settings), so that I can understand the full scope of the system even if some sections aren't built yet.
8. As a user, I want non-functional sidebar links to be visually present but clearly not navigable (disabled or marked "coming soon"), so that the nav feels intentional rather than broken.
9. As a Manager or Admin, I want to see a list of all active Products, so that I can browse the catalogue.
10. As a Manager or Admin, I want to filter the product list by Brand, so that I can narrow results to a specific supplier.
11. As a Manager or Admin, I want to filter the product list by Category, so that I can view products in a particular department.
12. As a Manager or Admin, I want to filter the product list by status (Active / Inactive / All), so that I can see inactive products when needed.
13. As a Manager or Admin, I want to search products by name, code, SKU, or barcode, so that I can find a specific item quickly.
14. As a Manager or Admin, I want to open a Product detail view showing the product's fields and its Variants, so that I can review everything about a product in one place.
15. As a Manager or Admin, I want to open an "Add Product" form, so that I can register a new product in the catalogue.
16. As a Manager or Admin, I want the Add Product form to include: Product Code, Name, Description, Brand (dropdown), and Category (dropdown), so that all required fields are captured.
17. As a Manager or Admin, I want form fields to show validation errors inline (e.g. required fields, duplicate product code), so that I know what to fix before submitting.
18. As a Manager or Admin, I want to see a success notification (snackbar) after a product is created, so that I have confirmation the action worked.
19. As a Manager or Admin, I want to open an "Edit Product" form pre-filled with the product's current values, so that I can update a product's details.
20. As a Manager or Admin, I want the Product Code field to be read-only in the Edit form, so that it cannot be accidentally changed.
21. As a Manager or Admin, I want to deactivate a Product from the product detail view, so that it is no longer available for new transactions.
22. As a Manager or Admin, I want a confirmation dialog before deactivating a Product, so that I don't do it accidentally.
23. As a Manager or Admin, I want to reactivate an inactive Product from its detail view (without confirmation), so that I can restore it if it was deactivated in error.
24. As a Manager or Admin, I want to see the list of ProductVariants for a Product on the product detail page, so that I can review all SKUs at a glance.
25. As a Manager or Admin, I want to see each Variant's SKU, barcode, colour, size, selling price, and status in the variant list, so that I have all key information without opening each variant.
26. As a Manager or Admin, I want to open an "Add Variant" modal from the product detail page, so that I can register a new SKU under a product.
27. As a Manager or Admin, I want the Add Variant modal to include: SKU, Barcode (optional), Colour (dropdown), Size (dropdown), and Selling Price, so that all required fields are captured.
28. As a Manager or Admin, I want the Variant form to show inline validation errors (e.g. required SKU, duplicate SKU, duplicate barcode), so that I know what to fix.
29. As a Manager or Admin, I want to see a success snackbar after a Variant is created, so that I have confirmation the action worked.
30. As a Manager or Admin, I want to open an "Edit Variant" modal pre-filled with the variant's current values, so that I can update selling price, colour, size, or barcode.
31. As a Manager or Admin, I want the SKU field to be read-only in the Edit Variant modal, so that it cannot be changed.
32. As a Manager or Admin, I want to deactivate a Variant from the product detail page, with a confirmation dialog, so that it is no longer available for new transactions.
33. As a Manager or Admin, I want to reactivate an inactive Variant (without confirmation), so that I can restore it if deactivated in error.
34. As a Staff member, I want to view the product list and product detail (including Variants), so that I can look up stock information.
35. As a Staff member, I want the add/edit/deactivate controls to be hidden or disabled for my role, so that I cannot modify the catalogue.
36. As any user, I want failed API calls to show an error snackbar with a meaningful message, so that I understand what went wrong.

## Implementation Decisions

### Tech stack additions

The following packages are added to `ClientApp`:

- **Material UI** (`@mui/material`, `@mui/icons-material`, `@emotion/react`, `@emotion/styled`) — component library
- **React Router v7** (`react-router-dom`) — client-side routing
- **TanStack Query** (`@tanstack/react-query`) — data fetching, caching, and mutation state
- **React Hook Form** (`react-hook-form`) — form state management
- **Zod** (`zod`) + **`@hookform/resolvers`** — schema-based form validation

### Vite dev proxy

Vite is configured to proxy all `/api` requests to `https://localhost:7145` (the .NET dev server) during development. This avoids CORS and matches the production setup where the SPA is served from the same origin as the API.

### Auth

- JWT is stored in `localStorage` under a fixed key.
- An Axios (or `fetch`) wrapper reads the token from `localStorage` and attaches it as a `Bearer` header to every API request.
- If any request returns 401, the token is cleared and the user is redirected to `/login`.
- A React context (`AuthContext`) exposes the current user identity (decoded from the JWT) and a `logout` function throughout the app.
- Protected routes are implemented with a wrapper component: unauthenticated users are redirected to `/login`.

### Routing

| Path | Component | Auth required |
|---|---|---|
| `/login` | LoginPage | No |
| `/` | Redirect to `/products` | Yes |
| `/products` | ProductListPage | Yes |
| `/products/:id` | ProductDetailPage | Yes |

### App shell

A persistent sidebar renders all nav items from the PRD (Dashboard, Products, Receiving, Inventory, Stock Take, Suppliers, Settings). Only "Products" is navigable; all others are rendered as disabled links with a "Coming soon" label. The sidebar also contains the "Log out" button.

### Product list

- Calls `GET /api/products` with query params: `q`, `brandId`, `categoryId`, `status`.
- Filter bar at the top of the page: text search input, Brand dropdown (populated from `GET /api/brands`), Category dropdown (populated from `GET /api/categories`), Status select (Active / Inactive / All — defaults to Active).
- Results rendered in an MUI `DataGrid` or `Table`. Each row links to the product detail page.
- Inactive products shown when status filter includes Inactive; hidden by default.

### Product detail

- Calls `GET /api/products/:id` for product fields.
- Calls `GET /api/products/:id/variants` for the variant list.
- Deactivate/Reactivate button visible to Manager and Admin only.
- "Add Variant" button opens the Add Variant modal.
- Each Variant row has Edit and Deactivate/Reactivate actions.

### Product forms (Add / Edit)

- Add Product opens as an MUI Dialog (modal).
- Edit Product opens as an MUI Dialog pre-filled with existing values.
- Zod schema validates: `productCode` required, `name` required.
- API errors (409 Conflict for duplicate product code) are surfaced as inline field errors.
- On success, the dialog closes, the product list is invalidated (TanStack Query), and a success Snackbar is shown.

### Variant forms (Add / Edit)

- Add Variant and Edit Variant open as MUI Dialogs on the product detail page.
- Zod schema validates: `sku` required (Add only — read-only on Edit), `colourId` required, `sizeId` required, `sellingPrice` required and positive.
- API errors (409 for duplicate SKU/barcode) are surfaced as inline field errors.
- Colour and Size dropdowns populated from `GET /api/colours` and `GET /api/sizes`.
- On success, the variant list is invalidated and a success Snackbar is shown.

### Notifications

- A global MUI `Snackbar` is managed by a context (`SnackbarContext`) so any component can trigger a success or error toast.
- Success: green, auto-dismisses after 4 seconds.
- Error: red, requires manual dismissal.

### Role-based UI

- The current user's roles are decoded from the JWT payload.
- Write actions (Add, Edit, Deactivate, Reactivate) are rendered only for Manager and Admin roles. Staff see a read-only view.

### Reference data

- Brands, Categories, Colours, and Sizes are fetched once on first use and cached by TanStack Query (long `staleTime`). They are consumed only in dropdowns — no management screens are built in this spec.

## Testing Decisions

No automated tests in this spec. The frontend is validated manually against the running .NET API. Future work may add React Testing Library + MSW for component-level tests or Playwright for E2E tests.

## Out of Scope

- Reference data management screens (Brands, Categories, Colours, Sizes)
- Dashboard, Receiving, Inventory, Stock Take, Suppliers, Settings pages (nav links present but non-functional)
- Pagination of the product list
- Advanced sorting of the product list
- Product images or media uploads
- PWA / offline support
- Accessibility audit beyond basic MUI defaults
- Automated tests

## Further Notes

- The `ClientApp` is a pure skeleton (default Vite + React template); all existing boilerplate in `App.tsx`, `App.css`, and `index.css` is replaced.
- In production, `npm run build` produces static files that the ASP.NET Core host serves via `UseStaticFiles` / SPA fallback — no separate frontend server.
- The `.NET` dev server at `https://localhost:7145` uses a self-signed certificate; Vite's proxy must be configured to accept it (`secure: false`).
