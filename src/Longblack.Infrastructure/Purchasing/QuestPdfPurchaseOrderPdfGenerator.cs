using System.Globalization;
using Longblack.Application.Purchasing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Longblack.Infrastructure.Purchasing;

public class QuestPdfPurchaseOrderPdfGenerator : IPurchaseOrderPdfGenerator
{
    static QuestPdfPurchaseOrderPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(PurchaseOrderPdfModel model)
    {
        var culture = CultureInfo.GetCultureInfo("en-NZ");
        var total = model.Lines.Sum(l => l.Quantity * l.UnitCost);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(model.CompanyName).FontSize(16).SemiBold();
                        col.Item().Text(model.CompanyAddress);
                    });
                    row.ConstantItem(200).AlignRight().Column(col =>
                    {
                        col.Item().Text("PURCHASE ORDER").FontSize(16).SemiBold();
                        col.Item().Text(model.PoNumber).FontSize(12);
                        col.Item().Text($"Date: {model.IssuedAt.ToLocalTime():dd MMM yyyy}");
                        if (model.ExpectedDeliveryDate is not null)
                            col.Item().Text($"Expected delivery: {model.ExpectedDeliveryDate.Value.ToLocalTime():dd MMM yyyy}");
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Column(s =>
                    {
                        s.Item().Text("Supplier").SemiBold();
                        s.Item().Text(model.SupplierName);
                        s.Item().Text(model.SupplierEmail);
                        s.Item().Text(model.SupplierPhone);
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(90);
                            c.RelativeColumn();
                            c.ConstantColumn(50);
                            c.ConstantColumn(70);
                            c.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("SKU");
                            h.Cell().Element(HeaderCell).Text("Description");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit cost");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Line total");
                        });

                        foreach (var line in model.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.Sku);
                            table.Cell().Element(BodyCell).Text(line.Description);
                            table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString(culture));
                            table.Cell().Element(BodyCell).AlignRight().Text(line.UnitCost.ToString("C", culture));
                            table.Cell().Element(BodyCell).AlignRight().Text((line.Quantity * line.UnitCost).ToString("C", culture));
                        }
                    });

                    col.Item().AlignRight().Text($"Total: {total.ToString("C", culture)}").FontSize(12).SemiBold();

                    if (!string.IsNullOrWhiteSpace(model.Notes))
                    {
                        col.Item().Column(n =>
                        {
                            n.Item().Text("Notes").SemiBold();
                            n.Item().Text(model.Notes);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.BorderBottom(1).PaddingVertical(4).DefaultTextStyle(t => t.SemiBold());

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);
}
