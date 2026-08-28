namespace Longblack.Application.Catalogue.Colours;

public interface IColourService
{
    Task<IReadOnlyList<ColourDto>> ListAsync(string? status = null, CancellationToken ct = default);
    Task<ColourDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ColourDto> CreateAsync(CreateColourDto dto, string createdBy, CancellationToken ct = default);
    Task<ColourDto> UpdateAsync(Guid id, UpdateColourDto dto, string updatedBy, CancellationToken ct = default);
    Task<ColourDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
