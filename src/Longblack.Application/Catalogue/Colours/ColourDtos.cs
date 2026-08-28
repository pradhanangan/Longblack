namespace Longblack.Application.Catalogue.Colours;

public record ColourDto(
    Guid Id,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateColourDto(string Name, string Code);

public record UpdateColourDto(string Name, string Code);
