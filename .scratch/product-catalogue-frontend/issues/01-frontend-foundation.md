# 01: Frontend foundation

**What to build:** Replace the Vite skeleton with a real app foundation: install all dependencies, configure the Vite dev proxy, set up React Router with protected routes, wire up an auth context (JWT in localStorage), a typed API client (attaches Bearer token, auto-redirects on 401), a global Snackbar notification context, and the sidebar shell (Products navigable, all other nav items present but disabled with "Coming soon").

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] MUI, React Router v7, TanStack Query, React Hook Form, Zod, and @hookform/resolvers installed
- [ ] Vite proxy configured: `/api` → `https://localhost:7145` (with `secure: false`)
- [ ] Existing boilerplate in App.tsx, App.css, index.css replaced
- [ ] `AuthContext` provides `token`, `user` (decoded from JWT), `login(token)`, and `logout()` functions; token stored in localStorage
- [ ] API client wraps fetch/axios: attaches `Authorization: Bearer <token>` on every request; clears token and redirects to `/login` on 401
- [ ] `SnackbarContext` provides `showSuccess(message)` and `showError(message)`; renders a global MUI Snackbar (green/auto-dismiss for success, red/manual-dismiss for error)
- [ ] React Router set up with routes: `/login` (public), `/` → redirect to `/products`, `/products` (protected), `/products/:id` (protected)
- [ ] Protected route wrapper redirects unauthenticated users to `/login`
- [ ] Sidebar renders all PRD nav items: Dashboard, Products, Receiving, Inventory, Stock Take, Suppliers, Settings; only Products is a working link; others are disabled with "Coming soon" label
- [ ] Sidebar contains a "Log out" button that calls `logout()` and redirects to `/login`
- [ ] TanStack Query `QueryClient` and `QueryClientProvider` wrapping the app
- [ ] `npm run build` succeeds with no TypeScript errors
- [ ] `npm run lint` passes
