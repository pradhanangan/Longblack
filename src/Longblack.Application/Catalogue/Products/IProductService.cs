namespace Longblack.Application.Catalogue.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> ListAsync(ListProductsFilter filter, CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, string createdBy, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, string updatedBy, CancellationToken ct = default);
    Task<ProductDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
