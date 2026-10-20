# 01: Selling Price on product variants

**What to build:** An Admin or Manager can set and edit a GST-inclusive Selling Price on a product variant, through the variant API and the variant form. A variant with no Selling Price is shown as unsellable. See `.scratch/sales/spec.md`.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] Hand-written idempotent SQL script under `db/` (next number in run order) adds the nullable selling price column, snake_case
- [ ] `AppDbContext.OnModelCreating` matches the script
- [ ] Variant create/edit/get API accepts and returns Selling Price; negative values rejected
- [ ] Variant form shows a Selling Price field; unpriced variants are visibly marked unsellable
- [ ] Existing variant behaviour is unchanged
- [ ] `dotnet build Longblack.slnx`, `npm run build` and `npm run lint` pass; verified manually (no automated tests for now)


Note: Selling Price already existed on variants (required, > 0 in the form), so no changes were needed.
