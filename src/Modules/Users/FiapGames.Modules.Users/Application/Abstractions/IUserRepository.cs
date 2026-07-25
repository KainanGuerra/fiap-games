using FiapGames.Modules.Users.Domain;
using FiapGames.Shared.Kernel.Repositories;

namespace FiapGames.Modules.Users.Application.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
