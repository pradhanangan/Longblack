using Longblack.Domain.Stores;

namespace Longblack.Domain.Sales;

public class Sale
{
    public Guid Id { get; set; }

    // DB-generated via a Postgres sequence; SaleNumber (e.g. "SL-0001") is derived at read time.
    public int SequenceNumber { get; set; }

    // Never the Warehouse Store; enforced at the application layer.
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public string Status { get; set; } = SaleStatus.Draft;

    // Payment is recorded for reference only, set on Draft -> Completed.
    public string? PaymentType { get; set; }
    public decimal? PaymentAmount { get; set; }

    public DateTimeOffset? CompletedDate { get; set; }
    public string? CompletedBy { get; set; }

    public DateTimeOffset? VoidedDate { get; set; }
    public string? VoidedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<SaleLine> Lines { get; set; } = [];
}
