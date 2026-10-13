using Longblack.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? storeId, CancellationToken ct) =>
        Ok(await dashboardService.GetAsync(storeId, ct));
}
