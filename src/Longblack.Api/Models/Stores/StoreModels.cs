namespace Longblack.Api.Models.Stores;

public record CreateStoreRequest(string Name, string Type);
public record UpdateStoreRequest(string Name);
public record SetStoreStatusRequest(string Status);

public record StoreResponse(
    Guid Id,
    string Name,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);
