using FiapGames.Modules.Users.Application.Abstractions;
using FiapGames.Modules.Users.Application.Dtos;
using FiapGames.Modules.Users.Domain;
using FiapGames.Shared.Infrastructure.Auth;
using FiapGames.Shared.Infrastructure.Settings;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;
using Microsoft.Extensions.Options;

namespace FiapGames.Modules.Users.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public UserService(IUserRepository repository, IPasswordHasher passwordHasher, ITokenService tokenService, IOptions<JwtSettings> jwtSettings)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result.Failure<UserResponse>(Error.Conflict("A user with this email already exists."));

        var user = new User(request.Name, request.Email, _passwordHasher.Hash(request.Password));

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(UserResponse.FromDomain(user));
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(Error.Unauthorized("Invalid email or password."));

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return Result.Success(new LoginResponse(token, DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)));
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is null
            ? Result.Failure<UserResponse>(Error.NotFound($"User '{id}' was not found."))
            : Result.Success(UserResponse.FromDomain(user));
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _repository.GetPagedAsync(request, cancellationToken);
        var items = paged.Items.Select(UserResponse.FromDomain).ToList();
        return new PagedResult<UserResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(Error.NotFound($"User '{id}' was not found."));

        var emailOwner = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (emailOwner is not null && emailOwner.Id != id)
            return Result.Failure<UserResponse>(Error.Conflict("A user with this email already exists."));

        user.UpdateProfile(request.Name, request.Email);
        _repository.Update(user);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(UserResponse.FromDomain(user));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Result.Failure(Error.NotFound($"User '{id}' was not found."));

        _repository.Remove(user);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
