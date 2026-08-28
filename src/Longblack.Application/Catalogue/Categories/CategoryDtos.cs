namespace Longblack.Application.Catalogue.Categories;

public record CategoryDto(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateCategoryDto(string Name, Guid? ParentCategoryId);

public record UpdateCategoryDto(string Name, Guid? ParentCategoryId);
