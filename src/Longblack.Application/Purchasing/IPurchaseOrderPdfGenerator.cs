namespace Longblack.Application.Purchasing;

public record PurchaseOrderPdfModel(
    string CompanyName,
    string CompanyAddress,
    string PoNumber,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpectedDeliveryDate,
    string SupplierName,
    string SupplierEmail,
    string SupplierPhone,
    string? Notes,
    IReadOnlyList<PurchaseOrderPdfLine> Lines);

public record PurchaseOrderPdfLine(string Sku, string Description, int Quantity, decimal UnitCost);

public interface IPurchaseOrderPdfGenerator
{
    byte[] Generate(PurchaseOrderPdfModel model);
}
