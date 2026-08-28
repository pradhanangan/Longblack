using Longblack.Application.Catalogue.Sizes;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class SizeService(AppDbContext db) : ISizeService
{
    public async Task<IReadOnlyList<SizeDto>> ListAsync(string? status = null, CancellationToken ct = default)
    {
        var query = db.Sizes.AsQueryable();

        if (status is not null)
            query = query.Where(s => s.Status == status);
        else
            query = query.Where(s => s.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(s => s.SortOrder)
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    public async Task<SizeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var size = await db.Sizes.FindAsync([id], ct);
        return size is null ? null : ToDto(size);
    }

    public async Task<SizeDto> CreateAsync(CreateSizeDto dto, string createdBy, CancellationToken ct = default)
    {
        var duplicate = await db.Sizes.AnyAsync(s => s.Name == dto.Name, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Size), "name", dto.Name);

        var now = DateTimeOffset.UtcNow;
        var size = new Size
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code,
            SortOrder = dto.SortOrder,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Sizes.Add(size);
        await db.SaveChangesAsync(ct);
        return ToDto(size);
    }

    public async Task<SizeDto> UpdateAsync(Guid id, UpdateSizeDto dto, string updatedBy, CancellationToken ct = default)
    {
        var size = await db.Sizes.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Size), id);

        var duplicate = await db.Sizes.AnyAsync(s => s.Name == dto.Name && s.Id != id, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Size), "name", dto.Name);

        size.Name = dto.Name;
        size.Code = dto.Code;
        size.SortOrder = dto.SortOrder;
        size.UpdatedAt = DateTimeOffset.UtcNow;
        size.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(size);
    }

    public async Task<SizeDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var size = await db.Sizes.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Size), id);

        size.Status = status;
        size.UpdatedAt = DateTimeOffset.UtcNow;
        size.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(size);
    }

    private static SizeDto ToDto(Size s) =>
        new(s.Id, s.Name, s.Code, s.SortOrder, s.Status, s.CreatedAt, s.UpdatedAt, s.CreatedBy, s.UpdatedBy);
}
