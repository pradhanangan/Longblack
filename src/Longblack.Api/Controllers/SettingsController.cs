using Longblack.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[Authorize]
[Route("api/settings")]
public class SettingsController(IAppSettingsService settingsService) : ApiControllerBase
{
    public record UpdateGstRateRequest(decimal GstRate);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await settingsService.GetAsync(ct));

    [HttpPut("gst-rate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateGstRate([FromBody] UpdateGstRateRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await settingsService.UpdateGstRateAsync(request.GstRate, CurrentUser, ct));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
