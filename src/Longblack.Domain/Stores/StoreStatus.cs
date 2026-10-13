namespace Longblack.Domain.Stores;

public static class StoreStatus
{
    public const string Active = "Active";

    // Terminal for the MVP — no reopen story. Rejected while the Store still holds nonzero
    // Inventory for any variant.
    public const string Closed = "Closed";
}
