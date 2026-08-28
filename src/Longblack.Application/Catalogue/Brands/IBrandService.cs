namespace Longblack.Application.Catalogue.Brands;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> ListAsync(string? status = null, CancellationToken ct = default);
    Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BrandDto> CreateAsync(CreateBrandDto dto, string createdBy, CancellationToken ct = default);
    Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto dto, string updatedBy, CancellationToken ct = default);
    Task<BrandDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
