namespace Longblack.Application.Stores;

public record StoreDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public record CreateStoreDto(string Name, string Type);

public record UpdateStoreDto(string Name);
