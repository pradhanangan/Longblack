namespace Longblack.Application.Catalogue.Suppliers;

public record SupplierDto(
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

public record CreateSupplierDto(string Code, string Name, string Email, string Phone);

public record UpdateSupplierDto(string Code, string Name, string Email, string Phone);
