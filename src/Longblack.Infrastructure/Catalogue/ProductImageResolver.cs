using Longblack.Application.Catalogue.ProductImages;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public record ResolvedImage(string ImageUrl, string ThumbnailUrl);

// Resolved Image: the variant's Colour Image, else the Product Image, else none (the UI shows a placeholder).
public class ProductImageResolver
{
    private readonly Dictionary<(Guid ProductId, Guid? ColourId), ResolvedImage> _images;

    private ProductImageResolver(Dictionary<(Guid, Guid?), ResolvedImage> images) => _images = images;

    public static async Task<ProductImageResolver> LoadAsync(
        AppDbContext db, IImageStorage storage, IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        var rows = ids.Count == 0
            ? []
            : await db.ProductImages
                .Where(i => ids.Contains(i.ProductId))
                .Select(i => new { i.ProductId, i.ColourId, i.FileName, i.ThumbnailFileName })
                .ToListAsync(ct);

        return new ProductImageResolver(rows.ToDictionary(
            r => (r.ProductId, r.ColourId),
            r => new ResolvedImage(storage.GetUrl(r.FileName), storage.GetUrl(r.ThumbnailFileName))));
    }

    public ResolvedImage? ForProduct(Guid productId) => _images.GetValueOrDefault((productId, null));

    public ResolvedImage? ForVariant(Guid productId, Guid colourId) =>
        _images.GetValueOrDefault((productId, colourId)) ?? ForProduct(productId);
}
