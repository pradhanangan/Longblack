namespace Longblack.Application.Catalogue.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(string? status = null, CancellationToken ct = default);
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, string createdBy, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, string updatedBy, CancellationToken ct = default);
    Task<CategoryDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
