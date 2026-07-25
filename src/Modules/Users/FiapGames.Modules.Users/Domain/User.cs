using FiapGames.Shared.Kernel.Entities;

namespace FiapGames.Modules.Users.Domain;

public class User : Entity
{
    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    private User() { }

    public User(string name, string email, string passwordHash, UserRole role = UserRole.Player)
    {
        Name = name;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }

    public void UpdateProfile(string name, string email)
    {
        Name = name;
        Email = email.ToLowerInvariant();
        Touch();
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }
}
