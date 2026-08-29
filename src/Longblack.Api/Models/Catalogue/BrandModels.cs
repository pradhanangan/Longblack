namespace Longblack.Api.Models.Catalogue;

public record CreateBrandRequest(string Name, string Code);
public record UpdateBrandRequest(string Name, string Code);
public record SetStatusRequest(string Status);

public record BrandResponse(
    Guid Id,
    string Name,
    string Code,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
