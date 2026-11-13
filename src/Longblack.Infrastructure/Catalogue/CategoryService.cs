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

        if (!string.Equals(status, ReferenceDataStatus.All, StringComparison.OrdinalIgnoreCase))
            query = query.Where(c => c.Status == (status ?? ReferenceDataStatus.Active));

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
        await ValidateParentAsync(null, dto.ParentCategoryId, ct);

        var duplicate = await db.Categories.AnyAsync(c => c.Name == dto.Name && c.ParentCategoryId == dto.ParentCategoryId, ct);
        if (duplicate)
            throw new DuplicateException(nameof(Category), "name", dto.Name);

        var codeDuplicate = await db.Categories.AnyAsync(c => c.Code == dto.Code, ct);
        if (codeDuplicate)
            throw new DuplicateException(nameof(Category), "code", dto.Code);

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            ParentCategoryId = dto.ParentCategoryId,
            Name = dto.Name,
            Code = dto.Code,
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

        if (dto.Code != category.Code)
            throw new InvalidStateException("A category's code cannot be changed once created.");

        await ValidateParentAsync(id, dto.ParentCategoryId, ct);

        var duplicate = await db.Categories.AnyAsync(
            c => c.Name == dto.Name && c.ParentCategoryId == dto.ParentCategoryId && c.Id != id, ct);
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

    // Categories are at most two levels: a root, or a child of a root.
    private async Task ValidateParentAsync(Guid? categoryId, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return;

        if (parentId == categoryId)
            throw new InvalidStateException("A category cannot be its own parent.");

        var parent = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId, ct)
            ?? throw new InvalidReferenceException(nameof(Category), "parentCategoryId", parentId);

        if (parent.ParentCategoryId is not null)
            throw new InvalidStateException("A category can only be placed under a top-level category.");

        if (categoryId is not null && await db.Categories.AnyAsync(c => c.ParentCategoryId == categoryId, ct))
            throw new InvalidStateException("A category that has sub-categories cannot itself become a sub-category.");
    }

    private static CategoryDto ToDto(Category c) =>
        new(c.Id, c.ParentCategoryId, c.Name, c.Code, c.Status, c.CreatedAt, c.UpdatedAt, c.CreatedBy, c.UpdatedBy);
}
