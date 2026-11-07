using Longblack.Application.Catalogue.ProductImages;
using Longblack.Application.Common.Exceptions;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Longblack.Infrastructure.Catalogue;

public class ProductImageService(AppDbContext db, IImageStorage storage) : IProductImageService
{
    public const long MaxUploadBytes = 5 * 1024 * 1024;
    private const int MaxImageSize = 1600;
    private const int ThumbnailSize = 256;

    public async Task<IReadOnlyList<ProductImageDto>> ListAsync(Guid productId, CancellationToken ct = default)
    {
        var images = await db.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync(ct);

        return images.Select(ToDto).ToList();
    }

    public async Task<ProductImageDto> SetAsync(
        Guid productId, Guid? colourId, Stream content, string updatedBy, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new NotFoundException(nameof(Product), productId);

        if (colourId is not null && !await db.Colours.AnyAsync(c => c.Id == colourId, ct))
            throw new InvalidReferenceException(nameof(Colour), "colourId", colourId);

        var (full, thumbnail) = await ProcessAsync(content, ct);

        var id = Guid.NewGuid();
        var fileName = $"{id}.webp";
        var thumbnailFileName = $"{id}.thumb.webp";
        await storage.SaveAsync(fileName, full, ct);
        await storage.SaveAsync(thumbnailFileName, thumbnail, ct);

        var existing = await db.ProductImages
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.ColourId == colourId, ct);
        var now = DateTimeOffset.UtcNow;
        string? replacedFile = null;
        string? replacedThumbnail = null;

        ProductImage image;
        if (existing is null)
        {
            image = new ProductImage
            {
                Id = id,
                ProductId = productId,
                ColourId = colourId,
                CreatedAt = now,
                CreatedBy = updatedBy
            };
            db.ProductImages.Add(image);
        }
        else
        {
            image = existing;
            replacedFile = existing.FileName;
            replacedThumbnail = existing.ThumbnailFileName;
        }

        image.FileName = fileName;
        image.ThumbnailFileName = thumbnailFileName;
        image.UpdatedAt = now;
        image.UpdatedBy = updatedBy;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Delete(fileName);
            storage.Delete(thumbnailFileName);
            throw;
        }

        if (replacedFile is not null) storage.Delete(replacedFile);
        if (replacedThumbnail is not null) storage.Delete(replacedThumbnail);

        return ToDto(image);
    }

    public async Task DeleteAsync(Guid productId, Guid? colourId, CancellationToken ct = default)
    {
        var image = await db.ProductImages
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.ColourId == colourId, ct)
            ?? throw new NotFoundException(nameof(ProductImage), colourId ?? productId);

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(ct);

        storage.Delete(image.FileName);
        storage.Delete(image.ThumbnailFileName);
    }

    // Decodes and re-encodes to WebP so metadata and unexpected payloads never reach the public URL.
    private static async Task<(byte[] Full, byte[] Thumbnail)> ProcessAsync(Stream content, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            throw new InvalidStateException("The uploaded image is empty.");
        if (buffer.Length > MaxUploadBytes)
            throw new InvalidStateException("The image must be 5 MB or smaller.");

        // The codec check restricts uploads to JPEG, PNG and WebP; Skia itself can decode more.
        using var codec = SKCodec.Create(new SKMemoryStream(buffer.ToArray()));
        if (codec?.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new InvalidStateException("The image must be a JPEG, PNG or WebP file.");

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
            throw new InvalidStateException("The image file is corrupt or unreadable.");

        using var source = ApplyOrientation(decoded, codec.EncodedOrigin);
        return (Encode(source, MaxImageSize), Encode(source, ThumbnailSize));
    }

    // Phone photos store rotation in EXIF; bake it into the pixels since the re-encoded WebP drops metadata.
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
            return bitmap.Copy();

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var w = swap ? bitmap.Height : bitmap.Width;
        var h = swap ? bitmap.Width : bitmap.Height;

        var result = new SKBitmap(w, h);
        using var canvas = new SKCanvas(result);
        canvas.SetMatrix(origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, w, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, w, -1, 0, h, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, h, 0, 0, 1),
            _ => SKMatrix.Identity
        });
        canvas.DrawBitmap(bitmap, 0, 0);
        return result;
    }

    private static byte[] Encode(SKBitmap source, int maxSize)
    {
        var scale = Math.Min(1.0, (double)maxSize / Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        using var resized = source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var image = SKImage.FromBitmap(resized ?? source);
        using var data = image.Encode(SKEncodedImageFormat.Webp, 80);
        return data.ToArray();
    }

    private ProductImageDto ToDto(ProductImage i) =>
        new(i.Id, i.ProductId, i.ColourId, storage.GetUrl(i.FileName), storage.GetUrl(i.ThumbnailFileName), i.UpdatedAt);
}
