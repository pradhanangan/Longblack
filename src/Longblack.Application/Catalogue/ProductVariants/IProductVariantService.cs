namespace Longblack.Application.Catalogue.ProductVariants;

public interface IProductVariantService
{
    Task<IReadOnlyList<ProductVariantDto>> ListAsync(Guid productId, string? status = null, CancellationToken ct = default);
    Task<ProductVariantDto?> GetByIdAsync(Guid productId, Guid variantId, CancellationToken ct = default);
    Task<ProductVariantDto> CreateAsync(Guid productId, CreateProductVariantDto dto, string createdBy, CancellationToken ct = default);
    Task<ProductVariantDto> UpdateAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, string updatedBy, CancellationToken ct = default);
    Task<ProductVariantDto> SetStatusAsync(Guid productId, Guid variantId, string status, string updatedBy, CancellationToken ct = default);
}
