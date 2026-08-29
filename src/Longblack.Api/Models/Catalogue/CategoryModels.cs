namespace Longblack.Api.Models.Catalogue;

public record CreateCategoryRequest(string Name, string Code, Guid? ParentCategoryId);
public record UpdateCategoryRequest(string Name, string Code, Guid? ParentCategoryId);

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
