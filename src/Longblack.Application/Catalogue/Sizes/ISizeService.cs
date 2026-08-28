namespace Longblack.Application.Catalogue.Sizes;

public interface ISizeService
{
    Task<IReadOnlyList<SizeDto>> ListAsync(string? status = null, CancellationToken ct = default);
    Task<SizeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SizeDto> CreateAsync(CreateSizeDto dto, string createdBy, CancellationToken ct = default);
    Task<SizeDto> UpdateAsync(Guid id, UpdateSizeDto dto, string updatedBy, CancellationToken ct = default);
    Task<SizeDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
