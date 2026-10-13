namespace Longblack.Api.Models.Catalogue;

public record CreateSupplierRequest(string Code, string Name, string Email, string Phone);
public record UpdateSupplierRequest(string Code, string Name, string Email, string Phone);

public record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string Email,
    string Phone,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
