using Longblack.Domain.Stores;

namespace Longblack.Domain.StockTransfers;

public class StockTransfer
{
    public Guid Id { get; set; }

    // DB-generated via a Postgres sequence; TransferNumber (e.g. "TR-0001") is derived at read time.
    public int SequenceNumber { get; set; }

    // Hub-and-spoke: enforced at the application layer — one of these two must be the Warehouse
    // Store, and they must differ. See CONTEXT.md / ADR-0003.
    public Guid SourceStoreId { get; set; }
    public Store? SourceStore { get; set; }
    public Guid DestinationStoreId { get; set; }
    public Store? DestinationStore { get; set; }

    public string Status { get; set; } = StockTransferStatus.Draft;

    // Set only on Draft -> Dispatched; always the acting user, never user-selectable.
    public DateTimeOffset? DispatchedDate { get; set; }
    public string? DispatchedBy { get; set; }

    // Set only on Dispatched -> Received; always the acting user, never user-selectable.
    public DateTimeOffset? ReceivedDate { get; set; }
    public string? ReceivedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<StockTransferLine> Lines { get; set; } = [];
}
