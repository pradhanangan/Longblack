namespace Longblack.Api.Models.Catalogue;

public record CreateColourRequest(string Name, string Code);
public record UpdateColourRequest(string Name, string Code);

public record ColourResponse(
    Guid Id,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
