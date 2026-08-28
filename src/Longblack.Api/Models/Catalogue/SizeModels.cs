namespace Longblack.Api.Models.Catalogue;

public record CreateSizeRequest(string Name, string Code, int SortOrder);
public record UpdateSizeRequest(string Name, string Code, int SortOrder);

public record SizeResponse(
    Guid Id,
    string Name,
    string Code,
    int SortOrder,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
