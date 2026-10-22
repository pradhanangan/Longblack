using Longblack.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Longblack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    // The token's name claim is blank for users without a first/last name, so fall back to their email.
    protected string CurrentUser =>
        FirstNonBlank(User.Identity?.Name, User.FindFirst("name")?.Value, User.FindFirst("email")?.Value) ?? "unknown";

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    protected IActionResult HandleException(Exception ex) => ex switch
    {
        NotFoundException nfe => NotFound(new { message = nfe.Message }),
        DuplicateException de => Conflict(new { message = de.Message }),
        InvalidReferenceException ire => UnprocessableEntity(new { message = ire.Message }),
        BatchConflictException bce => Conflict(new { message = bce.Message, conflictingSkus = bce.ConflictingSkus }),
        InvalidStateException ise => UnprocessableEntity(new { message = ise.Message }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
