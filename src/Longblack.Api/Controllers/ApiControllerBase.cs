using Longblack.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUser =>
        User.Identity?.Name ?? "unknown";

    protected IActionResult HandleException(Exception ex) => ex switch
    {
        NotFoundException nfe => NotFound(new { message = nfe.Message }),
        DuplicateException de => Conflict(new { message = de.Message }),
        InvalidReferenceException ire => UnprocessableEntity(new { message = ire.Message }),
        BatchConflictException bce => Conflict(new { message = bce.Message, conflictingSkus = bce.ConflictingSkus }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
