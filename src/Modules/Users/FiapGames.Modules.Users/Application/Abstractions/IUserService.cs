using FiapGames.Modules.Users.Application.Dtos;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;

namespace FiapGames.Modules.Users.Application.Abstractions;

public interface IUserService
{
    Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<UserResponse>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
