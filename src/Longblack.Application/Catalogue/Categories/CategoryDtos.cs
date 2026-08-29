namespace Longblack.Application.Catalogue.Categories;

public record CategoryDto(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateCategoryDto(string Name, string Code, Guid? ParentCategoryId);

public record UpdateCategoryDto(string Name, string Code, Guid? ParentCategoryId);
