namespace Longblack.Application.Catalogue.ProductImages;

public record ProductImageDto(
    Guid Id,
    Guid ProductId,
    Guid? ColourId,
    string ImageUrl,
    string ThumbnailUrl,
    DateTimeOffset UpdatedAt);

public interface IProductImageService
{
    Task<IReadOnlyList<ProductImageDto>> ListAsync(Guid productId, CancellationToken ct = default);

    // colourId null sets the Product Image; otherwise the Colour Image for that colour.
    Task<ProductImageDto> SetAsync(Guid productId, Guid? colourId, Stream content, string updatedBy, CancellationToken ct = default);

    Task DeleteAsync(Guid productId, Guid? colourId, CancellationToken ct = default);
}

// Seam over where image files physically live, so a move to object storage stays cheap (ADR-0006).
public interface IImageStorage
{
    Task SaveAsync(string fileName, byte[] content, CancellationToken ct = default);

    void Delete(string fileName);

    string GetUrl(string fileName);
}
