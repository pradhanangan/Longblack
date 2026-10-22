# 01: Reusable paging contract and grid control

**What to build:** The prefactor for pagination. A shared paged-result shape and paging parameters (page, page size, default 25, cap 100, clamping) in the backend, and a shared paginated-grid control and paged-result type in the frontend, so any list grid can adopt paging later. No user-visible change yet. See `.scratch/product-pagination/spec.md`.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] Backend paged-result type (items, total, page, page size) and paging parameters with default page 1, page size 25, cap 100, invalid values clamped
- [ ] Frontend paged-result type and a reusable paginated-grid control with rows-per-page 10, 25, 50, 100 (default 25) and a total count
- [ ] No existing screen or endpoint behaviour changes
- [ ] Builds and lint pass
