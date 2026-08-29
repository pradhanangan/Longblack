using Longblack.Application.Catalogue.ProductVariants;
using Longblack.Application.Catalogue.Products;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class ProductService(AppDbContext db) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> ListAsync(ListProductsFilter filter, CancellationToken ct = default)
    {
        var query = db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (filter.Status is not null)
            query = query.Where(p => p.Status == filter.Status);
        else
            query = query.Where(p => p.Status == ReferenceDataStatus.Active);

        if (filter.BrandId is not null)
            query = query.Where(p => p.BrandId == filter.BrandId);

        if (filter.CategoryId is not null)
            query = query.Where(p => p.CategoryId == filter.CategoryId);

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var q = filter.SearchQuery.ToLower();
            query = query
                .Include(p => p.Variants).ThenInclude(v => v.Colour)
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Where(p =>
                    p.Name.ToLower().Contains(q) ||
                    p.ProductCode.ToLower().Contains(q) ||
                    p.Variants.Any(v => v.Sku.ToLower().Contains(q) ||
                                        (v.Barcode != null && v.Barcode.ToLower().Contains(q))));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync(ct);

        return products.Select(p => ToDtoWithVariants(p, filter.SearchQuery)).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return product is null ? null : ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, string createdBy, CancellationToken ct = default)
    {
        var duplicate = await db.Products.AnyAsync(p => p.ProductCode == dto.ProductCode, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Product), "productCode", dto.ProductCode);

        if (dto.BrandId is not null)
        {
            var brandExists = await db.Brands.AnyAsync(b => b.Id == dto.BrandId, ct);
            if (!brandExists)
                throw new InvalidReferenceException(nameof(Brand), "brandId", dto.BrandId);
        }

        if (dto.CategoryId is not null)
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                throw new InvalidReferenceException(nameof(Category), "categoryId", dto.CategoryId);
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductCode = dto.ProductCode,
            Name = dto.Name,
            Description = dto.Description,
            BrandId = dto.BrandId,
            CategoryId = dto.CategoryId,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct) ?? ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, string updatedBy, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (dto.BrandId is not null && dto.BrandId != product.BrandId)
        {
            var brandExists = await db.Brands.AnyAsync(b => b.Id == dto.BrandId, ct);
            if (!brandExists)
                throw new InvalidReferenceException(nameof(Brand), "brandId", dto.BrandId);
        }

        if (dto.CategoryId is not null && dto.CategoryId != product.CategoryId)
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                throw new InvalidReferenceException(nameof(Category), "categoryId", dto.CategoryId);
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.BrandId = dto.BrandId;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct) ?? ToDto(product);
    }

    public async Task<ProductDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Status = status;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(product.Id, ct) ?? ToDto(product);
    }

    public async Task<string> SuggestCodeAsync(Guid? brandId, Guid? categoryId, CancellationToken ct = default)
    {
        if (brandId is null || categoryId is null)
            return string.Empty;

        var brand = await db.Brands.FindAsync([brandId], ct);
        var category = await db.Categories.FindAsync([categoryId], ct);

        if (brand is null || category is null ||
            string.IsNullOrWhiteSpace(brand.Code) ||
            string.IsNullOrWhiteSpace(category.Code))
            return string.Empty;

        var count = await db.Products.CountAsync(
            p => p.BrandId == brandId && p.CategoryId == categoryId, ct);

        var sequence = (count + 1).ToString("D3");
        return $"{brand.Code}-{category.Code}-{sequence}";
    }

    private static ProductDto ToDtoWithVariants(Product p, string? searchQuery) =>
        ToDto(p) with
        {
            Variants = string.IsNullOrWhiteSpace(searchQuery)
                ? null
                : p.Variants.Select(v => new ProductVariantDto(
                    v.Id, v.ProductId, v.Sku, v.Barcode,
                    v.ColourId, v.Colour?.Name,
                    v.SizeId, v.Size?.Name,
                    v.SellingPrice, v.Status,
                    v.CreatedAt, v.UpdatedAt, v.CreatedBy, v.UpdatedBy)).ToList()
        };

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.ProductCode, p.Name, p.Description,
            p.BrandId, p.Brand?.Name,
            p.CategoryId, p.Category?.Name,
            p.Status, p.CreatedAt, p.UpdatedAt, p.CreatedBy, p.UpdatedBy);
}