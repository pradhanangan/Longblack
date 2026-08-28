using Longblack.Application.Catalogue.Categories;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(string? status = null, CancellationToken ct = default)
    {
        var query = db.Categories.AsQueryable();

        if (status is not null)
            query = query.Where(c => c.Status == status);
        else
            query = query.Where(c => c.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync(ct);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct);
        return category is null ? null : ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, string createdBy, CancellationToken ct = default)
    {
        if (dto.ParentCategoryId is not null)
        {
            var parentExists = await db.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId, ct);
            if (!parentExists)
                throw new InvalidReferenceException(nameof(Category), "parentCategoryId", dto.ParentCategoryId);
        }

        var duplicate = await db.Categories.AnyAsync(c => c.Name == dto.Name, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Category), "name", dto.Name);

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = dto.ParentCategoryId,
            Name = dto.Name,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, string updatedBy, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Category), id);

        if (dto.ParentCategoryId is not null && dto.ParentCategoryId != category.ParentCategoryId)
        {
            var parentExists = await db.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId, ct);
            if (!parentExists)
                throw new InvalidReferenceException(nameof(Category), "parentCategoryId", dto.ParentCategoryId);
        }

        var duplicate = await db.Categories.AnyAsync(c => c.Name == dto.Name && c.Id != id, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Category), "name", dto.Name);

        category.Name = dto.Name;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task<CategoryDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Category), id);

        category.Status = status;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(category);
    }

    private static CategoryDto ToDto(Category c) =>
        new(c.Id, c.ParentCategoryId, c.Name, c.Status, c.CreatedAt, c.UpdatedAt, c.CreatedBy, c.UpdatedBy);
}
