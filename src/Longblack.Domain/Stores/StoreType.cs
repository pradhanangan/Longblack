namespace Longblack.Domain.Stores;

public static class StoreType
{
    // Exactly one Store has this type — the hub for Goods Receiving and the Stock Transfer
    // hub-and-spoke rule. Seeded automatically; cannot be created again via the API.
    public const string Warehouse = "Warehouse";
    public const string LongStanding = "LongStanding";
    public const string OneOff = "OneOff";
    public const string Online = "Online";
}
