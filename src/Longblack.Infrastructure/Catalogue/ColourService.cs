using Longblack.Application.Catalogue.Colours;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class ColourService(AppDbContext db) : IColourService
{
    public async Task<IReadOnlyList<ColourDto>> ListAsync(string? status = null, CancellationToken ct = default)
    {
        var query = db.Colours.AsQueryable();

        if (status is not null)
            query = query.Where(c => c.Status == status);
        else
            query = query.Where(c => c.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync(ct);
    }

    public async Task<ColourDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var colour = await db.Colours.FindAsync([id], ct);
        return colour is null ? null : ToDto(colour);
    }

    public async Task<ColourDto> CreateAsync(CreateColourDto dto, string createdBy, CancellationToken ct = default)
    {
        var duplicate = await db.Colours.AnyAsync(c => c.Name == dto.Name, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Colour), "name", dto.Name);

        var now = DateTimeOffset.UtcNow;
        var colour = new Colour
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Colours.Add(colour);
        await db.SaveChangesAsync(ct);
        return ToDto(colour);
    }

    public async Task<ColourDto> UpdateAsync(Guid id, UpdateColourDto dto, string updatedBy, CancellationToken ct = default)
    {
        var colour = await db.Colours.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Colour), id);

        var duplicate = await db.Colours.AnyAsync(c => c.Name == dto.Name && c.Id != id, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Colour), "name", dto.Name);

        colour.Name = dto.Name;
        colour.Code = dto.Code;
        colour.UpdatedAt = DateTimeOffset.UtcNow;
        colour.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(colour);
    }

    public async Task<ColourDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var colour = await db.Colours.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Colour), id);

        colour.Status = status;
        colour.UpdatedAt = DateTimeOffset.UtcNow;
        colour.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(colour);
    }

    private static ColourDto ToDto(Colour c) =>
        new(c.Id, c.Name, c.Code, c.Status, c.CreatedAt, c.UpdatedAt, c.CreatedBy, c.UpdatedBy);
}
