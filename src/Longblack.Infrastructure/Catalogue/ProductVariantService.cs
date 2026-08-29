using Longblack.Application.Catalogue.ProductVariants;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class ProductVariantService(AppDbContext db) : IProductVariantService
{
    public async Task<IReadOnlyList<ProductVariantDto>> ListAsync(Guid productId, string? status = null, CancellationToken ct = default)
    {
        var query = db.ProductVariants
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .Where(v => v.ProductId == productId);

        if (status is not null)
            query = query.Where(v => v.Status == status);
        else
            query = query.Where(v => v.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(v => v.Size!.SortOrder)
            .ThenBy(v => v.Colour!.Name)
            .Select(v => ToDto(v))
            .ToListAsync(ct);
    }

    public async Task<ProductVariantDto?> GetByIdAsync(Guid productId, Guid variantId, CancellationToken ct = default)
    {
        var variant = await db.ProductVariants
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct);

        return variant is null ? null : ToDto(variant);
    }

    public async Task<ProductVariantDto> CreateAsync(Guid productId, CreateProductVariantDto dto, string createdBy, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([productId], ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (product.Status != ReferenceDataStatus.Active)
            throw new InvalidReferenceException(nameof(Product), "status", "inactive — variants cannot be added to an inactive Product");

        var skuTaken = await db.ProductVariants.AnyAsync(v => v.Sku == dto.Sku, ct);
        if (skuTaken)
            throw new DuplicateException(nameof(ProductVariant), "sku", dto.Sku);

        if (dto.Barcode is not null)
        {
            var barcodeTaken = await db.ProductVariants.AnyAsync(v => v.Barcode == dto.Barcode, ct);
            if (barcodeTaken)
                throw new DuplicateException(nameof(ProductVariant), "barcode", dto.Barcode);
        }

        var colourExists = await db.Colours.AnyAsync(c => c.Id == dto.ColourId, ct);
        if (!colourExists)
            throw new InvalidReferenceException(nameof(Colour), "colourId", dto.ColourId);

        var sizeExists = await db.Sizes.AnyAsync(s => s.Id == dto.SizeId, ct);
        if (!sizeExists)
            throw new InvalidReferenceException(nameof(Size), "sizeId", dto.SizeId);

        var now = DateTimeOffset.UtcNow;
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = dto.Sku,
            Barcode = dto.Barcode,
            ColourId = dto.ColourId,
            SizeId = dto.SizeId,
            SellingPrice = dto.SellingPrice,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(productId, variant.Id, ct) ?? ToDto(variant);
    }

    public async Task<ProductVariantDto> UpdateAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, string updatedBy, CancellationToken ct = default)
    {
        var variant = await db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        if (dto.Barcode is not null && dto.Barcode != variant.Barcode)
        {
            var barcodeTaken = await db.ProductVariants.AnyAsync(v => v.Barcode == dto.Barcode && v.Id != variantId, ct);
            if (barcodeTaken)
                throw new DuplicateException(nameof(ProductVariant), "barcode", dto.Barcode);
        }

        if (dto.ColourId != variant.ColourId)
        {
            var colourExists = await db.Colours.AnyAsync(c => c.Id == dto.ColourId, ct);
            if (!colourExists)
                throw new InvalidReferenceException(nameof(Colour), "colourId", dto.ColourId);
        }

        if (dto.SizeId != variant.SizeId)
        {
            var sizeExists = await db.Sizes.AnyAsync(s => s.Id == dto.SizeId, ct);
            if (!sizeExists)
                throw new InvalidReferenceException(nameof(Size), "sizeId", dto.SizeId);
        }

        variant.Barcode = dto.Barcode;
        variant.ColourId = dto.ColourId;
        variant.SizeId = dto.SizeId;
        variant.SellingPrice = dto.SellingPrice;
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(productId, variant.Id, ct) ?? ToDto(variant);
    }

    public async Task<ProductVariantDto> SetStatusAsync(Guid productId, Guid variantId, string status, string updatedBy, CancellationToken ct = default)
    {
        var variant = await db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        variant.Status = status;
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(productId, variant.Id, ct) ?? ToDto(variant);
    }

    private static ProductVariantDto ToDto(ProductVariant v) =>
        new(v.Id, v.ProductId, v.Sku, v.Barcode,
            v.ColourId, v.Colour?.Name,
            v.SizeId, v.Size?.Name,
            v.SellingPrice, v.Status,
            v.CreatedAt, v.UpdatedAt, v.CreatedBy, v.UpdatedBy);

    public async Task<IReadOnlyList<ProductVariantDto>> CreateBatchAsync(
        Guid productId,
        IReadOnlyList<CreateProductVariantDto> dtos,
        string createdBy,
        CancellationToken ct = default)
    {
        if (dtos.Count == 0)
            throw new ArgumentException("Batch must contain at least one variant.", nameof(dtos));

        var product = await db.Products.FindAsync([productId], ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (product.Status != ReferenceDataStatus.Active)
            throw new InvalidReferenceException(nameof(Product), "status", "inactive");

        // Validate all reference FKs and collect conflicts before inserting anything
        var batchSkus = dtos.Select(d => d.Sku).ToList();
        var existingSkus = await db.ProductVariants
            .Where(v => batchSkus.Contains(v.Sku))
            .Select(v => v.Sku)
            .ToListAsync(ct);

        var batchBarcodes = dtos.Where(d => d.Barcode is not null).Select(d => d.Barcode!).ToList();
        var existingBarcodes = batchBarcodes.Count > 0
            ? await db.ProductVariants
                .Where(v => v.Barcode != null && batchBarcodes.Contains(v.Barcode))
                .Select(v => v.Barcode!)
                .ToListAsync(ct)
            : [];

        var conflictingSkus = existingSkus
            .Concat(dtos
                .Where(d => d.Barcode is not null && existingBarcodes.Contains(d.Barcode!))
                .Select(d => d.Sku))
            .Distinct()
            .ToList();

        if (conflictingSkus.Count > 0)
            throw new BatchConflictException(conflictingSkus);

        var colourIds = dtos.Select(d => d.ColourId).Distinct().ToList();
        var sizeIds = dtos.Select(d => d.SizeId).Distinct().ToList();

        var validColourIds = await db.Colours.Where(c => colourIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        var validSizeIds = await db.Sizes.Where(s => sizeIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);

        var invalidColour = colourIds.FirstOrDefault(id => !validColourIds.Contains(id));
        if (invalidColour != default)
            throw new InvalidReferenceException(nameof(Colour), "colourId", invalidColour);

        var invalidSize = sizeIds.FirstOrDefault(id => !validSizeIds.Contains(id));
        if (invalidSize != default)
            throw new InvalidReferenceException(nameof(Size), "sizeId", invalidSize);

        var now = DateTimeOffset.UtcNow;
        var variants = dtos.Select(dto => new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = dto.Sku,
            Barcode = dto.Barcode,
            ColourId = dto.ColourId,
            SizeId = dto.SizeId,
            SellingPrice = dto.SellingPrice,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        }).ToList();

        db.ProductVariants.AddRange(variants);
        await db.SaveChangesAsync(ct);

        // Reload with navigation props
        var ids = variants.Select(v => v.Id).ToList();
        var created = await db.ProductVariants
            .Include(v => v.Colour)
            .Include(v => v.Size)
            .Where(v => ids.Contains(v.Id))
            .ToListAsync(ct);

        return created.Select(ToDto).ToList();
    }
}
