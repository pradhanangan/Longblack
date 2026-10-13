namespace Longblack.Api.Models.Users;

public record UserResponse(string Id, string Email, string FirstName, string LastName, string Role, bool IsActive);

public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);

public record ChangeRoleRequest(string Role);

public record SetPasswordRequest(string NewPassword);

public record ChangeOwnPasswordRequest(string CurrentPassword, string NewPassword);
