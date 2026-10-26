using Longblack.Application.Catalogue.Suppliers;
using Longblack.Application.Common.Exceptions;
using Longblack.Application.Common.Paging;
using Longblack.Domain.Catalogue;
using Longblack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Catalogue;

public class SupplierService(AppDbContext db) : ISupplierService
{
    public async Task<PagedResult<SupplierDto>> ListAsync(string? status = null, PageRequest? page = null, CancellationToken ct = default)
    {
        var query = db.Suppliers.AsQueryable();

        if (status is not null)
            query = query.Where(s => s.Status == status);
        else
            query = query.Where(s => s.Status == ReferenceDataStatus.Active);

        return await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToPagedResultAsync(page, ToDto, ct);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FindAsync([id], ct);
        return supplier is null ? null : ToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto, string createdBy, CancellationToken ct = default)
    {
        var codeDuplicate = await db.Suppliers.AnyAsync(s => s.Code == dto.Code, ct);
        if (codeDuplicate)
            throw new DuplicateException(nameof(Supplier), "code", dto.Code);

        var now = DateTimeOffset.UtcNow;
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Status = ReferenceDataStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto dto, string updatedBy, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Supplier), id);

        var codeDuplicate = await db.Suppliers.AnyAsync(s => s.Code == dto.Code && s.Id != id, ct);
        if (codeDuplicate)
            throw new DuplicateException(nameof(Supplier), "code", dto.Code);

        supplier.Code = dto.Code;
        supplier.Name = dto.Name;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;
        supplier.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> SetStatusAsync(Guid id, string status, string updatedBy, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Supplier), id);

        supplier.Status = status;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;
        supplier.UpdatedBy = updatedBy;

        await db.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    private static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.Code, s.Name, s.Email, s.Phone, s.Status, s.CreatedAt, s.UpdatedAt, s.CreatedBy, s.UpdatedBy);
}
