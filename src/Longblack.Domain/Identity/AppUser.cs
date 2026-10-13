using Microsoft.AspNetCore.Identity;

namespace Longblack.Domain.Identity;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Users are never deleted (audit fields reference them); deactivating blocks sign-in instead.
    public bool IsActive { get; set; } = true;
}
