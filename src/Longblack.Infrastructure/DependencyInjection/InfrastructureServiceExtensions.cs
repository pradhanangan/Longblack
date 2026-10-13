using Longblack.Application.Catalogue.Brands;
using Longblack.Application.Catalogue.Categories;
using Longblack.Application.Catalogue.Colours;
using Longblack.Application.Catalogue.Products;
using Longblack.Application.Catalogue.ProductVariants;
using Longblack.Application.Catalogue.Sizes;
using Longblack.Application.Catalogue.Suppliers;
using Longblack.Application.Dashboard;
using Longblack.Application.Inventory;
using Longblack.Application.Receiving.GoodsReceipts;
using Longblack.Application.StockTake;
using Longblack.Application.StockTransfers;
using Longblack.Application.Stores;
using Longblack.Domain.Identity;
using Longblack.Infrastructure.Catalogue;
using Longblack.Infrastructure.Dashboard;
using Longblack.Infrastructure.Inventory;
using Longblack.Infrastructure.Persistence;
using Longblack.Infrastructure.Receiving;
using Longblack.Infrastructure.StockTake;
using Longblack.Infrastructure.StockTransfers;
using Longblack.Infrastructure.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Longblack.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IColourService, ColourService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<ISizeService, SizeService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IStockTakeService, StockTakeService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IStockTransferService, StockTransferService>();

        return services;
    }
}
