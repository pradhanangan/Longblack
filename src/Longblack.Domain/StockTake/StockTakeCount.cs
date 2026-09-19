namespace Longblack.Domain.StockTake;

// An immutable audit record — never edited or deleted. Recounting always appends a new row
// rather than overwriting the previous one (see CONTEXT.md: Stock Take Count).
public class StockTakeCount
{
    public Guid Id { get; set; }
    public Guid StockTakeItemId { get; set; }
    public StockTakeItem? StockTakeItem { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CountedAt { get; set; }
    public string CountedBy { get; set; } = string.Empty;
}
