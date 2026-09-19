using Longblack.Domain.Catalogue;

namespace Longblack.Domain.StockTake;

public class StockTake
{
    public Guid Id { get; set; }

    // DB-generated via a Postgres sequence; ReferenceNumber (e.g. "ST-0001") is derived at read time.
    public int SequenceNumber { get; set; }

    // Scope filter, persisted as descriptive metadata (see ADR-0002) — both null means the whole
    // catalogue. Not used to re-derive Items after InProgress starts; Items are the source of truth
    // for what's actually in scope from that point on.
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Status { get; set; } = StockTakeStatus.Draft;

    // Set when Draft -> InProgress (the moment Items are generated and ExpectedQuantity snapshotted).
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? CompletionDate { get; set; }
    public string? CompletedBy { get; set; }
    public DateTimeOffset? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<StockTakeItem> Items { get; set; } = [];
}
