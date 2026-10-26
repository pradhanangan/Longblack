using Longblack.Application.Common.Paging;

namespace Longblack.Application.Catalogue.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> ListAsync(string? status = null, PageRequest? page = null, CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto, string createdBy, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto dto, string updatedBy, CancellationToken ct = default);
    Task<SupplierDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default);
}
