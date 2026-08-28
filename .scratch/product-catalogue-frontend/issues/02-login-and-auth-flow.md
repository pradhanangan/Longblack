# 02: Login page and auth flow

**What to build:** A working login page that authenticates users against the API, stores the JWT, and establishes the session. Includes logout and the 401 auto-redirect behaviour.

**Blocked by:** 01 — Frontend foundation

**Status:** ready-for-agent

- [ ] `/login` renders an MUI-styled form with Email and Password fields
- [ ] Form validated with Zod: both fields required, email must be a valid email format
- [ ] Submitting calls `POST /api/auth/login`; on success token is stored and user redirected to `/products`
- [ ] Wrong credentials shows an inline error message ("Invalid email or password")
- [ ] Visiting `/login` when already authenticated redirects to `/products`
- [ ] Logout button in sidebar clears the token and redirects to `/login`
- [ ] Any API request returning 401 clears the token and redirects to `/login`
- [ ] `npm run build` succeeds; `npm run lint` passes
