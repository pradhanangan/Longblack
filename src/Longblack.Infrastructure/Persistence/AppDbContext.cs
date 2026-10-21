using Longblack.Domain.Catalogue;
using Longblack.Domain.Identity;
using Longblack.Domain.Inventory;
using Longblack.Domain.Purchasing;
using Longblack.Domain.Receiving;
using Longblack.Domain.StockTransfers;
using Longblack.Domain.Sales;
using Longblack.Domain.Stores;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockTakeEntity = Longblack.Domain.StockTake.StockTake;

namespace Longblack.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Colour> Colours => Set<Colour>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<Longblack.Domain.Inventory.Inventory> Inventory => Set<Longblack.Domain.Inventory.Inventory>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockTakeEntity> StockTakes => Set<StockTakeEntity>();
    public DbSet<Longblack.Domain.StockTake.StockTakeItem> StockTakeItems => Set<Longblack.Domain.StockTake.StockTakeItem>();
    public DbSet<Longblack.Domain.StockTake.StockTakeCount> StockTakeCounts => Set<Longblack.Domain.StockTake.StockTakeCount>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseOrderSend> PurchaseOrderSends => Set<PurchaseOrderSend>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity tables — snake_case
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("user_tokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("role_claims");

        // Brand
        builder.Entity<Brand>(e =>
        {
            e.ToTable("brands");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.Name).HasColumnName("name").IsRequired();
            e.Property(b => b.Code).HasColumnName("code").IsRequired();
            e.Property(b => b.Status).HasColumnName("status").IsRequired();
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.Property(b => b.UpdatedAt).HasColumnName("updated_at");
            e.Property(b => b.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(b => b.UpdatedBy).HasColumnName("updated_by").IsRequired();
        });

        // Category (self-referential hierarchy)
        builder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id");
            e.Property(c => c.Name).HasColumnName("name").IsRequired();
            e.Property(c => c.Code).HasColumnName("code").IsRequired();
            e.Property(c => c.Status).HasColumnName("status").IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(c => c.ParentCategory)
             .WithMany()
             .HasForeignKey(c => c.ParentCategoryId)
             .IsRequired(false);
        });

        // Supplier
        builder.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Code).HasColumnName("code").IsRequired();
            e.Property(s => s.Name).HasColumnName("name").IsRequired();
            e.Property(s => s.Email).HasColumnName("email").IsRequired();
            e.Property(s => s.Phone).HasColumnName("phone").IsRequired();
            e.Property(s => s.Status).HasColumnName("status").IsRequired();
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        });

        // Colour
        builder.Entity<Colour>(e =>
        {
            e.ToTable("colours");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").IsRequired();
            e.Property(c => c.Code).HasColumnName("code").IsRequired();
            e.Property(c => c.Status).HasColumnName("status").IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
        });

        // Size
        builder.Entity<Size>(e =>
        {
            e.ToTable("sizes");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Name).HasColumnName("name").IsRequired();
            e.Property(s => s.Code).HasColumnName("code").IsRequired();
            e.Property(s => s.SortOrder).HasColumnName("sort_order");
            e.Property(s => s.Status).HasColumnName("status").IsRequired();
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        });

        // Store
        builder.Entity<Store>(e =>
        {
            e.ToTable("stores");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Name).HasColumnName("name").IsRequired();
            e.Property(s => s.Type).HasColumnName("type").IsRequired();
            e.Property(s => s.Status).HasColumnName("status").IsRequired();
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        });

        // Product
        builder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ProductCode).HasColumnName("product_code").IsRequired();
            e.Property(p => p.Name).HasColumnName("name").IsRequired();
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.BrandId).HasColumnName("brand_id");
            e.Property(p => p.CategoryId).HasColumnName("category_id");
            e.Property(p => p.Status).HasColumnName("status").IsRequired();
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId).IsRequired(false);
            e.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).IsRequired(false);
            e.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        });

        // ProductVariant
        builder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variants");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).HasColumnName("id");
            e.Property(v => v.ProductId).HasColumnName("product_id");
            e.Property(v => v.Sku).HasColumnName("sku").IsRequired();
            e.Property(v => v.Barcode).HasColumnName("barcode");
            e.Property(v => v.ColourId).HasColumnName("colour_id");
            e.Property(v => v.SizeId).HasColumnName("size_id");
            e.Property(v => v.SellingPrice).HasColumnName("selling_price").HasPrecision(10, 2);
            e.Property(v => v.Status).HasColumnName("status").IsRequired();
            e.Property(v => v.CreatedAt).HasColumnName("created_at");
            e.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            e.Property(v => v.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(v => v.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(v => v.Colour).WithMany().HasForeignKey(v => v.ColourId);
            e.HasOne(v => v.Size).WithMany().HasForeignKey(v => v.SizeId);
        });

        // Backs GoodsReceipt.SequenceNumber so receipt numbers (GR-0001) are assigned atomically by Postgres.
        builder.HasSequence<int>("goods_receipt_seq").StartsAt(1);

        // GoodsReceipt
        builder.Entity<GoodsReceipt>(e =>
        {
            e.ToTable("goods_receipts");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.SequenceNumber)
                .HasColumnName("sequence_number")
                .HasDefaultValueSql("nextval('goods_receipt_seq')")
                .ValueGeneratedOnAdd();
            e.HasIndex(g => g.SequenceNumber).IsUnique();
            e.Property(g => g.StoreId).HasColumnName("store_id");
            e.Property(g => g.SupplierId).HasColumnName("supplier_id");
            e.Property(g => g.PurchaseOrderId).HasColumnName("purchase_order_id");
            e.Property(g => g.ReceivedDate).HasColumnName("received_date").IsRequired();
            e.Property(g => g.Status).HasColumnName("status").IsRequired();
            e.Property(g => g.ReceivedBy).HasColumnName("received_by");
            e.Property(g => g.CreatedAt).HasColumnName("created_at");
            e.Property(g => g.UpdatedAt).HasColumnName("updated_at");
            e.Property(g => g.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(g => g.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(g => g.Store).WithMany().HasForeignKey(g => g.StoreId);
            e.HasOne(g => g.Supplier).WithMany().HasForeignKey(g => g.SupplierId);
            e.HasOne(g => g.PurchaseOrder).WithMany().HasForeignKey(g => g.PurchaseOrderId);
            e.HasMany(g => g.Lines).WithOne(l => l.GoodsReceipt).HasForeignKey(l => l.GoodsReceiptId);
        });

        // GoodsReceiptLine
        builder.Entity<GoodsReceiptLine>(e =>
        {
            e.ToTable("goods_receipt_lines");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.GoodsReceiptId).HasColumnName("goods_receipt_id");
            e.Property(l => l.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(l => l.UnitCost).HasColumnName("unit_cost").HasPrecision(10, 2);
            e.Property(l => l.CreatedAt).HasColumnName("created_at");
            e.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            e.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(l => l.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId);
        });

        // Inventory
        builder.Entity<Longblack.Domain.Inventory.Inventory>(e =>
        {
            e.ToTable("inventory");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(i => i.StoreId).HasColumnName("store_id");
            e.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.Property(i => i.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(i => i.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasIndex(i => new { i.ProductVariantId, i.StoreId }).IsUnique();
            e.HasOne(i => i.ProductVariant).WithMany().HasForeignKey(i => i.ProductVariantId);
            e.HasOne(i => i.Store).WithMany().HasForeignKey(i => i.StoreId);
        });

        // InventoryTransaction
        builder.Entity<InventoryTransaction>(e =>
        {
            e.ToTable("inventory_transactions");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(t => t.StoreId).HasColumnName("store_id");
            e.Property(t => t.Type).HasColumnName("type").IsRequired();
            e.Property(t => t.QuantityDelta).HasColumnName("quantity_delta").IsRequired();
            e.Property(t => t.SourceType).HasColumnName("source_type").IsRequired();
            e.Property(t => t.SourceId).HasColumnName("source_id").IsRequired();
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
            e.HasOne(t => t.ProductVariant).WithMany().HasForeignKey(t => t.ProductVariantId);
            e.HasOne(t => t.Store).WithMany().HasForeignKey(t => t.StoreId);
            e.HasIndex(t => new { t.SourceType, t.SourceId });
        });

        // Backs StockTake.SequenceNumber so reference numbers (ST-0001) are assigned atomically by Postgres.
        builder.HasSequence<int>("stock_take_seq").StartsAt(1);

        // StockTake
        builder.Entity<StockTakeEntity>(e =>
        {
            e.ToTable("stock_takes");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.SequenceNumber)
                .HasColumnName("sequence_number")
                .HasDefaultValueSql("nextval('stock_take_seq')")
                .ValueGeneratedOnAdd();
            e.HasIndex(s => s.SequenceNumber).IsUnique();
            e.Property(s => s.StoreId).HasColumnName("store_id");
            e.Property(s => s.BrandId).HasColumnName("brand_id");
            e.Property(s => s.CategoryId).HasColumnName("category_id");
            e.Property(s => s.Status).HasColumnName("status").IsRequired();
            e.Property(s => s.StartDate).HasColumnName("start_date");
            e.Property(s => s.CompletionDate).HasColumnName("completion_date");
            e.Property(s => s.CompletedBy).HasColumnName("completed_by");
            e.Property(s => s.ApprovedDate).HasColumnName("approved_date");
            e.Property(s => s.ApprovedBy).HasColumnName("approved_by");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId);
            e.HasOne(s => s.Brand).WithMany().HasForeignKey(s => s.BrandId).IsRequired(false);
            e.HasOne(s => s.Category).WithMany().HasForeignKey(s => s.CategoryId).IsRequired(false);
            e.HasMany(s => s.Items).WithOne(i => i.StockTake).HasForeignKey(i => i.StockTakeId);
        });

        // StockTakeItem
        builder.Entity<Longblack.Domain.StockTake.StockTakeItem>(e =>
        {
            e.ToTable("stock_take_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.StockTakeId).HasColumnName("stock_take_id");
            e.Property(i => i.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(i => i.ExpectedQuantity).HasColumnName("expected_quantity").IsRequired();
            e.Property(i => i.CountedQuantity).HasColumnName("counted_quantity");
            e.Property(i => i.Status).HasColumnName("status").IsRequired();
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.Property(i => i.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(i => i.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(i => i.ProductVariant).WithMany().HasForeignKey(i => i.ProductVariantId);
            e.HasMany(i => i.Counts).WithOne(c => c.StockTakeItem).HasForeignKey(c => c.StockTakeItemId);
        });

        // StockTakeCount — immutable audit rows, no updated_at/updated_by.
        builder.Entity<Longblack.Domain.StockTake.StockTakeCount>(e =>
        {
            e.ToTable("stock_take_counts");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.StockTakeItemId).HasColumnName("stock_take_item_id");
            e.Property(c => c.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(c => c.CountedAt).HasColumnName("counted_at");
            e.Property(c => c.CountedBy).HasColumnName("counted_by").IsRequired();
        });

        // Backs StockTransfer.SequenceNumber so transfer numbers (TR-0001) are assigned atomically by Postgres.
        builder.HasSequence<int>("stock_transfer_seq").StartsAt(1);

        // StockTransfer
        builder.Entity<StockTransfer>(e =>
        {
            e.ToTable("stock_transfers");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.SequenceNumber)
                .HasColumnName("sequence_number")
                .HasDefaultValueSql("nextval('stock_transfer_seq')")
                .ValueGeneratedOnAdd();
            e.HasIndex(t => t.SequenceNumber).IsUnique();
            e.Property(t => t.SourceStoreId).HasColumnName("source_store_id");
            e.Property(t => t.DestinationStoreId).HasColumnName("destination_store_id");
            e.Property(t => t.Status).HasColumnName("status").IsRequired();
            e.Property(t => t.DispatchedDate).HasColumnName("dispatched_date");
            e.Property(t => t.DispatchedBy).HasColumnName("dispatched_by");
            e.Property(t => t.ReceivedDate).HasColumnName("received_date");
            e.Property(t => t.ReceivedBy).HasColumnName("received_by");
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(t => t.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(t => t.SourceStore).WithMany().HasForeignKey(t => t.SourceStoreId);
            e.HasOne(t => t.DestinationStore).WithMany().HasForeignKey(t => t.DestinationStoreId);
            e.HasMany(t => t.Lines).WithOne(l => l.StockTransfer).HasForeignKey(l => l.StockTransferId);
        });

        // StockTransferLine — unique per (StockTransferId, ProductVariantId): at most one line per
        // variant per transfer.
        builder.Entity<StockTransferLine>(e =>
        {
            e.ToTable("stock_transfer_lines");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.StockTransferId).HasColumnName("stock_transfer_id");
            e.Property(l => l.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(l => l.DispatchedQuantity).HasColumnName("dispatched_quantity").IsRequired();
            e.Property(l => l.ReceivedQuantity).HasColumnName("received_quantity");
            e.Property(l => l.CreatedAt).HasColumnName("created_at");
            e.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            e.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(l => l.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasIndex(l => new { l.StockTransferId, l.ProductVariantId }).IsUnique();
            e.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId);
        });

        // Backs Sale.SequenceNumber so sale numbers (SL-0001) are assigned atomically by Postgres.
        builder.HasSequence<int>("sale_seq").StartsAt(1);

        builder.Entity<Sale>(e =>
        {
            e.ToTable("sales");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.SequenceNumber)
                .HasColumnName("sequence_number")
                .HasDefaultValueSql("nextval('sale_seq')")
                .ValueGeneratedOnAdd();
            e.HasIndex(s => s.SequenceNumber).IsUnique();
            e.Property(s => s.StoreId).HasColumnName("store_id");
            e.Property(s => s.Status).HasColumnName("status").IsRequired();
            e.Property(s => s.PaymentType).HasColumnName("payment_type");
            e.Property(s => s.PaymentAmount).HasColumnName("payment_amount").HasPrecision(10, 2);
            e.Property(s => s.CompletedDate).HasColumnName("completed_date");
            e.Property(s => s.CompletedBy).HasColumnName("completed_by");
            e.Property(s => s.VoidedDate).HasColumnName("voided_date");
            e.Property(s => s.VoidedBy).HasColumnName("voided_by");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId);
            e.HasMany(s => s.Lines).WithOne(l => l.Sale).HasForeignKey(l => l.SaleId);
        });

        // SaleLine — unique per (SaleId, ProductVariantId): at most one line per variant per sale.
        builder.Entity<SaleLine>(e =>
        {
            e.ToTable("sale_lines");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.SaleId).HasColumnName("sale_id");
            e.Property(l => l.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);
            e.Property(l => l.Discount).HasColumnName("discount").HasPrecision(10, 2);
            e.Property(l => l.CreatedAt).HasColumnName("created_at");
            e.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            e.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(l => l.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasIndex(l => new { l.SaleId, l.ProductVariantId }).IsUnique();
            e.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId);
        });
        // Backs PurchaseOrder.SequenceNumber so PO numbers (PO-0001) are assigned atomically by Postgres on issue.
        builder.HasSequence<int>("purchase_order_seq").StartsAt(1);

        builder.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("purchase_orders");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.SequenceNumber).HasColumnName("sequence_number");
            e.HasIndex(p => p.SequenceNumber).IsUnique();
            e.Property(p => p.SupplierId).HasColumnName("supplier_id");
            e.Property(p => p.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
            e.Property(p => p.Notes).HasColumnName("notes");
            e.Property(p => p.Status).HasColumnName("status").IsRequired();
            e.Property(p => p.IssuedAt).HasColumnName("issued_at");
            e.Property(p => p.IssuedBy).HasColumnName("issued_by");
            e.Property(p => p.SentAt).HasColumnName("sent_at");
            e.Property(p => p.SentBy).HasColumnName("sent_by");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId);
            e.HasMany(p => p.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId);
            e.HasMany(p => p.Sends).WithOne(s => s.PurchaseOrder).HasForeignKey(s => s.PurchaseOrderId);
        });

        // PurchaseOrderLine — unique per (PurchaseOrderId, ProductVariantId): at most one line per variant per PO.
        builder.Entity<PurchaseOrderLine>(e =>
        {
            e.ToTable("purchase_order_lines");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.PurchaseOrderId).HasColumnName("purchase_order_id");
            e.Property(l => l.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(l => l.UnitCost).HasColumnName("unit_cost").HasPrecision(10, 2);
            e.Property(l => l.CreatedAt).HasColumnName("created_at");
            e.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            e.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
            e.Property(l => l.UpdatedBy).HasColumnName("updated_by").IsRequired();
            e.HasIndex(l => new { l.PurchaseOrderId, l.ProductVariantId }).IsUnique();
            e.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId);
        });

        builder.Entity<PurchaseOrderSend>(e =>
        {
            e.ToTable("purchase_order_sends");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.PurchaseOrderId).HasColumnName("purchase_order_id");
            e.Property(s => s.Recipient).HasColumnName("recipient").IsRequired();
            e.Property(s => s.Cc).HasColumnName("cc");
            e.Property(s => s.Succeeded).HasColumnName("succeeded");
            e.Property(s => s.Error).HasColumnName("error");
            e.Property(s => s.SentAt).HasColumnName("sent_at");
            e.Property(s => s.SentBy).HasColumnName("sent_by").IsRequired();
        });
    }
}
