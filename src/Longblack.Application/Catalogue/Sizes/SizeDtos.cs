namespace Longblack.Application.Catalogue.Sizes;

public record SizeDto(
    Guid Id,
    string Name,
    string Code,
    int SortOrder,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateSizeDto(string Name, string Code, int SortOrder);

public record UpdateSizeDto(string Name, string Code, int SortOrder);
