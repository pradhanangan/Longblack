using Longblack.Application.Catalogue.Brands;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class BrandService(AppDbContext db) : IBrandService
{
    public async Task<IReadOnlyList<BrandDto>> ListAsync(string? status = null, CancellationToken ct = default)
    {
        var query = db.Brands.AsQueryable();

        if (status is not null)
            query = query.Where(b => b.Status == status);
        else
            query = query.Where(b => b.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => ToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var brand = await db.Brands.FindAsync([id], ct);
        return brand is null ? null : ToDto(brand);
    }

    public async Task<BrandDto> CreateAsync(CreateBrandDto dto, string createdBy, CancellationToken ct = default)
    {
        var duplicate = await db.Brands.AnyAsync(b => b.Name == dto.Name, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Brand), "name", dto.Name);

        var now = DateTimeOffset.UtcNow;
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return ToDto(brand);
    }

    public async Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto, string updatedBy, CancellationToken ct = default)
    {
        var brand = await db.Brands.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Brand), id);

        var duplicate = await db.Brands.AnyAsync(b => b.Name == dto.Name && b.Id != id, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Brand), "name", dto.Name);

        brand.Name = dto.Name;
        brand.UpdatedAt = DateTimeOffset.UtcNow;
        brand.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(brand);
    }

    public async Task<BrandDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var brand = await db.Brands.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Brand), id);

        brand.Status = status;
        brand.UpdatedAt = DateTimeOffset.UtcNow;
        brand.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(brand);
    }

    private static BrandDto ToDto(Brand b) =>
        new(b.Id, b.Name, b.Status, b.CreatedAt, b.UpdatedAt, b.CreatedBy, b.UpdatedBy);
}
