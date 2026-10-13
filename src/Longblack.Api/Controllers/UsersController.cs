using System.Security.Claims;
using Longblack.Api.Models.Users;
using Longblack.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Api.Controllers;

[Authorize]
public class UsersController(UserManager<AppUser> userManager) : ApiControllerBase
{
    private static readonly string[] ValidRoles = ["Admin", "Manager", "Staff"];
    private const string AdminRole = "Admin";

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;

    [HttpGet]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(ct);
        var result = new List<UserResponse>();
        foreach (var u in users)
            result.Add(await ToResponseAsync(u));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ValidRoles.Contains(request.Role))
            return UnprocessableEntity(new { message = "Invalid role." });

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return UnprocessableEntity(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });

        await userManager.AddToRoleAsync(user, request.Role);
        return Created($"api/users/{user.Id}", await ToResponseAsync(user));
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest request)
    {
        if (!ValidRoles.Contains(request.Role))
            return UnprocessableEntity(new { message = "Invalid role." });

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (id == CurrentUserId)
            return UnprocessableEntity(new { message = "You cannot change your own role." });
        if (await userManager.IsInRoleAsync(user, AdminRole) && request.Role != AdminRole
            && await IsLastActiveAdminAsync(user))
            return UnprocessableEntity(new { message = "The last active Admin cannot be demoted." });

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(await ToResponseAsync(user));
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (id == CurrentUserId)
            return UnprocessableEntity(new { message = "You cannot deactivate yourself." });
        if (await userManager.IsInRoleAsync(user, AdminRole) && await IsLastActiveAdminAsync(user))
            return UnprocessableEntity(new { message = "The last active Admin cannot be deactivated." });

        user.IsActive = false;
        await userManager.UpdateAsync(user);
        return Ok(await ToResponseAsync(user));
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = true;
        await userManager.UpdateAsync(user);
        return Ok(await ToResponseAsync(user));
    }

    [HttpPut("{id}/password")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> SetPassword(string id, [FromBody] SetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        return result.Succeeded
            ? NoContent()
            : UnprocessableEntity(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }

    [HttpPost("me/password")]
    public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangeOwnPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded
            ? NoContent()
            : UnprocessableEntity(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }

    private async Task<bool> IsLastActiveAdminAsync(AppUser user)
    {
        var admins = await userManager.GetUsersInRoleAsync(AdminRole);
        return !admins.Any(a => a.IsActive && a.Id != user.Id);
    }

    private async Task<UserResponse> ToResponseAsync(AppUser u)
    {
        var roles = await userManager.GetRolesAsync(u);
        return new UserResponse(u.Id, u.Email ?? string.Empty, u.FirstName, u.LastName,
            roles.FirstOrDefault() ?? string.Empty, u.IsActive);
    }
}
