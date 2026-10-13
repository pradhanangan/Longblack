namespace Longblack.Application.Dashboard;

public interface IDashboardService
{
    // storeId scopes only the stock figures and null means all Stores; the outstanding-work
    // counts and recent transactions always cover every Store.
    Task<DashboardDto> GetAsync(Guid? storeId, CancellationToken ct = default);
}
