namespace Longblack.Domain.Catalogue;

public class Category
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = ReferenceDataStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    // "Parent / Child" so the UI never shows a bare child name (e.g. "Jeans") without
    // its parent (e.g. "Men"). Requires ParentCategory to be included by the caller.
    public string DisplayName => ParentCategory is null ? Name : $"{ParentCategory.Name} / {Name}";
}
