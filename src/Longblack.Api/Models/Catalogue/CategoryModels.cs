namespace Longblack.Api.Models.Catalogue;

public record CreateCategoryRequest(string Name, Guid? ParentCategoryId);
public record UpdateCategoryRequest(string Name, Guid? ParentCategoryId);

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
