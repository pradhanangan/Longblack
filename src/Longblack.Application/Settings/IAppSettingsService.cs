namespace Longblack.Application.Settings;

public record AppSettingsDto(decimal GstRate, DateTimeOffset UpdatedAt, string UpdatedBy);

public interface IAppSettingsService
{
    Task<AppSettingsDto> GetAsync(CancellationToken ct = default);

    // GST rate as a fraction (0.15 = 15%); 0 to 1 inclusive, at most 4 decimal places.
    Task<decimal> GetGstRateAsync(CancellationToken ct = default);

    Task<AppSettingsDto> UpdateGstRateAsync(decimal gstRate, string updatedBy, CancellationToken ct = default);
}
