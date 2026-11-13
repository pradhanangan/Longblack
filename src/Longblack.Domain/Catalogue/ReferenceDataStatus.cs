namespace Longblack.Domain.Catalogue;

public static class ReferenceDataStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    // Query-only filter value meaning "every status"; never stored on a row.
    public const string All = "All";
}
