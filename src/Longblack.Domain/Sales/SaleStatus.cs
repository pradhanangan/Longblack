namespace Longblack.Domain.Sales;

public static class SaleStatus
{
    public const string Draft = "Draft";
    public const string Completed = "Completed";
    public const string Voided = "Voided";
    public const string Cancelled = "Cancelled";
}

public static class PaymentType
{
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string BankTransfer = "BankTransfer";
    public const string Other = "Other";

    public static readonly string[] All = [Cash, Card, BankTransfer, Other];
}
