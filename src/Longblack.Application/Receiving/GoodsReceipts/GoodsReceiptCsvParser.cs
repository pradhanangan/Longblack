using System.Globalization;
using System.Text;
using Longblack.Application.Common.Exceptions;

namespace Longblack.Application.Receiving.GoodsReceipts;

public record GoodsReceiptCsvRow(int Row, string? Sku, string? Barcode, int Quantity, decimal UnitCost);

// Parses a goods receipt CSV: header row required (sku, quantity, unit_cost; barcode optional), comma-delimited, UTF-8.
public static class GoodsReceiptCsvParser
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxRows = 1000;

    public static IReadOnlyList<GoodsReceiptCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var records = ReadRecords(reader);

        if (records.Count == 0)
            throw Fail(0, "The file is empty.");

        var header = records[0].Fields.Select(h => h.Trim().ToLowerInvariant()).ToList();
        var sku = header.IndexOf("sku");
        var barcode = header.IndexOf("barcode");
        var quantity = header.IndexOf("quantity");
        var unitCost = header.IndexOf("unit_cost");

        var missing = new List<string>();
        if (quantity < 0) missing.Add("quantity");
        if (unitCost < 0) missing.Add("unit_cost");
        if (sku < 0 && barcode < 0) missing.Add("sku");
        if (missing.Count > 0)
            throw Fail(1, $"Header is missing required column(s): {string.Join(", ", missing)}.");

        var dataRecords = records.Skip(1).ToList();
        if (dataRecords.Count == 0)
            throw Fail(0, "The file contains no data rows.");
        if (dataRecords.Count > MaxRows)
            throw Fail(0, $"The file has {dataRecords.Count} rows; the maximum is {MaxRows}.");

        var rows = new List<GoodsReceiptCsvRow>();
        var errors = new List<UploadRowError>();

        foreach (var record in dataRecords)
        {
            string? Cell(int index) =>
                index >= 0 && index < record.Fields.Count && !string.IsNullOrWhiteSpace(record.Fields[index])
                    ? record.Fields[index].Trim()
                    : null;

            var skuValue = Cell(sku);
            var barcodeValue = Cell(barcode);
            if (skuValue is null && barcodeValue is null)
            {
                errors.Add(new UploadRowError(record.Line, "SKU (or barcode) is required."));
                continue;
            }

            if (!int.TryParse(Cell(quantity), NumberStyles.None, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
            {
                errors.Add(new UploadRowError(record.Line, "Quantity must be a whole number greater than zero."));
                continue;
            }

            if (!decimal.TryParse(Cell(unitCost), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            {
                errors.Add(new UploadRowError(record.Line, "Unit cost must be a number that is zero or greater."));
                continue;
            }

            rows.Add(new GoodsReceiptCsvRow(record.Line, skuValue, barcodeValue, qty, cost));
        }

        if (errors.Count > 0)
            throw new UploadValidationException(errors);

        return rows;
    }

    private static UploadValidationException Fail(int row, string message) =>
        new([new UploadRowError(row, message)]);

    private record Record(int Line, List<string> Fields);

    // Minimal RFC 4180 reader: quoted fields may contain commas, quotes ("") and newlines. Blank lines are skipped.
    private static List<Record> ReadRecords(TextReader reader)
    {
        var records = new List<Record>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var recordLine = 1;
        var hasContent = false;

        void EndRecord()
        {
            fields.Add(field.ToString());
            field.Clear();
            if (hasContent)
                records.Add(new Record(recordLine, fields));
            fields = [];
            hasContent = false;
        }

        int c;
        while ((c = reader.Read()) != -1)
        {
            var ch = (char)c;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); field.Append('"'); }
                    else inQuotes = false;
                }
                else
                {
                    if (ch == '\n') line++;
                    field.Append(ch);
                }
                continue;
            }

            switch (ch)
            {
                case '"':
                    inQuotes = true;
                    hasContent = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    hasContent = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRecord();
                    line++;
                    recordLine = line;
                    break;
                default:
                    field.Append(ch);
                    if (!char.IsWhiteSpace(ch)) hasContent = true;
                    break;
            }
        }

        EndRecord();
        return records;
    }
}
