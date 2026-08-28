namespace Longblack.Api.Models.Catalogue;

public record CreateBrandRequest(string Name);
public record UpdateBrandRequest(string Name);
public record SetStatusRequest(string Status);

public record BrandResponse(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
