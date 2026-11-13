using Longblack.Application.Common.Exceptions;
using Longblack.Application.Settings;
using Longblack.Domain.Settings;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Settings;

public class AppSettingsService(AppDbContext db) : IAppSettingsService
{
    public async Task<AppSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await RequireAsync(ct);
        return new(settings.GstRate, settings.UpdatedAt, settings.UpdatedBy);
    }

    public async Task<decimal> GetGstRateAsync(CancellationToken ct = default) =>
        (await RequireAsync(ct)).GstRate;

    public async Task<AppSettingsDto> UpdateGstRateAsync(decimal gstRate, string updatedBy, CancellationToken ct = default)
    {
        if (gstRate < 0 || gstRate > 1)
            throw new InvalidStateException("GST rate must be between 0% and 100%.");
        if (decimal.Round(gstRate, 4) != gstRate)
            throw new InvalidStateException("GST rate can have at most 2 decimal places as a percentage.");

        var settings = await RequireAsync(ct);
        settings.GstRate = gstRate;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        settings.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);

        return new(settings.GstRate, settings.UpdatedAt, settings.UpdatedBy);
    }

    private async Task<AppSettings> RequireAsync(CancellationToken ct) =>
        await db.AppSettings.FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, ct)
            ?? throw new InvalidStateException("Application settings have not been initialised. Run db/012-gst-settings.sql.");
}
