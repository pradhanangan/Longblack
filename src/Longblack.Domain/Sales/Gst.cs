namespace Longblack.Domain.Sales;

public static class Gst
{
    // Selling Prices are GST-inclusive, so GST is the portion of the inclusive amount: amount * rate / (1 + rate).
    public static decimal Extract(decimal inclusiveAmount, decimal rate) =>
        Math.Round(inclusiveAmount * rate / (1 + rate), 2, MidpointRounding.AwayFromZero);
}
