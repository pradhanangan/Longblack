using Longblack.Application.Catalogue.ProductImages;
using Microsoft.Extensions.Options;

namespace Longblack.Infrastructure.Catalogue;

public class ProductImageOptions
{
    public const string SectionName = "ProductImages";
    public const string RequestPath = "/media/product-images";

    // Relative paths resolve against the current directory; point this at a mounted volume in production.
    public string RootPath { get; set; } = "App_Data/product-images";

    public string FullRootPath => Path.GetFullPath(RootPath);
}

public class LocalImageStorage(IOptions<ProductImageOptions> options) : IImageStorage
{
    private readonly string _root = Directory.CreateDirectory(options.Value.FullRootPath).FullName;

    public async Task SaveAsync(string fileName, byte[] content, CancellationToken ct = default) =>
        await File.WriteAllBytesAsync(PathFor(fileName), content, ct);

    public void Delete(string fileName)
    {
        var path = PathFor(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public string GetUrl(string fileName) => $"{ProductImageOptions.RequestPath}/{fileName}";

    private string PathFor(string fileName) => Path.Combine(_root, Path.GetFileName(fileName));
}
