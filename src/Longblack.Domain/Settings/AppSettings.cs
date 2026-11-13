namespace Longblack.Domain.Settings;

// Single-row table (Id is always 1) holding application-wide settings editable by an Admin.
public class AppSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    // Fraction, not a percentage: 0.15 means 15%. Applied to GST-inclusive Selling Prices when a Sale is completed.
    public decimal GstRate { get; set; }

    // A variant at a Store is Low Stock when its quantity is at or below this value. Global, not per variant.
    public int LowStockThreshold { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
