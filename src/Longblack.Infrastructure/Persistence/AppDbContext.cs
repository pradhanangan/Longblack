using Longblack.Domain.Catalogue;
using Longblack.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Colour> Colours => Set<Colour>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Size> Sizes => Set<Size>();

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
        });
    }
}
