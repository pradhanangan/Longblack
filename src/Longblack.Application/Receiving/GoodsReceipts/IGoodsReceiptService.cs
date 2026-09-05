namespace Longblack.Application.Receiving.GoodsReceipts;

public interface IGoodsReceiptService
{
    Task<IReadOnlyList<GoodsReceiptDto>> ListAsync(ListGoodsReceiptsFilter filter, CancellationToken ct = default);
    Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto dto, string createdBy, CancellationToken ct = default);
    Task<GoodsReceiptDto> UpdateAsync(Guid id, UpdateGoodsReceiptDto dto, string updatedBy, CancellationToken ct = default);

    Task<GoodsReceiptDto> AddLineAsync(Guid id, AddGoodsReceiptLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<GoodsReceiptDto> UpdateLineAsync(Guid id, Guid lineId, UpdateGoodsReceiptLineDto dto, string updatedBy, CancellationToken ct = default);
    Task<GoodsReceiptDto> RemoveLineAsync(Guid id, Guid lineId, string updatedBy, CancellationToken ct = default);

    // Draft -> Received: requires >=1 line and all variants Active; posts one InventoryTransaction per line.
    Task<GoodsReceiptDto> MarkReceivedAsync(Guid id, string updatedBy, CancellationToken ct = default);

    // Draft -> Cancelled only; soft, preserves the row for audit history.
    Task<GoodsReceiptDto> CancelAsync(Guid id, string updatedBy, CancellationToken ct = default);
}
