namespace FiapGames.Modules.Users.Application.Dtos;

public sealed record RegisterUserRequest(string Name, string Email, string Password);

public sealed record UpdateUserRequest(string Name, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

public sealed record UserResponse(Guid Id, string Name, string Email, string Role, DateTime CreatedAtUtc)
{
    public static UserResponse FromDomain(Domain.User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAtUtc);
}
