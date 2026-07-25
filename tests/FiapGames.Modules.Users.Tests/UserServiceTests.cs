using FiapGames.Modules.Users.Application.Abstractions;
using FiapGames.Modules.Users.Application.Dtos;
using FiapGames.Modules.Users.Application.Services;
using FiapGames.Modules.Users.Domain;
using FiapGames.Shared.Infrastructure.Auth;
using FiapGames.Shared.Infrastructure.Settings;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FiapGames.Modules.Users.Tests;

public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings { ExpiryMinutes = 60 });
        _sut = new UserService(_repository, _passwordHasher, _tokenService, jwtSettings);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailIsNew_CreatesUser()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);
        _passwordHasher.Hash("Password123").Returns("hashed-password");

        var result = await _sut.RegisterAsync(new RegisterUserRequest("Jane Doe", "new@example.com", "Password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", result.Value.Email);
        await _repository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var existing = new User("Existing", "taken@example.com", "hash");
        _repository.GetByEmailAsync("taken@example.com").Returns(existing);

        var result = await _sut.RegisterAsync(new RegisterUserRequest("Jane Doe", "taken@example.com", "Password123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = new User("Jane Doe", "jane@example.com", "hashed-password");
        _repository.GetByEmailAsync("jane@example.com").Returns(user);
        _passwordHasher.Verify("Password123", "hashed-password").Returns(true);
        _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString()).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest("jane@example.com", "Password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        var user = new User("Jane Doe", "jane@example.com", "hashed-password");
        _repository.GetByEmailAsync("jane@example.com").Returns(user);
        _passwordHasher.Verify("WrongPassword", "hashed-password").Returns(false);

        var result = await _sut.LoginAsync(new LoginRequest("jane@example.com", "WrongPassword"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.Type);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_RemovesUser()
    {
        var user = new User("Jane Doe", "jane@example.com", "hash");
        _repository.GetByIdAsync(user.Id).Returns(user);

        var result = await _sut.DeleteAsync(user.Id);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(user);
    }

    [Fact]
    public async Task GetPagedAsync_MapsDomainUsersToResponses()
    {
        var user = new User("Jane Doe", "jane@example.com", "hash");
        var pagedRequest = new PagedRequest { Page = 1, PageSize = 10 };
        _repository.GetPagedAsync(Arg.Any<PagedRequest>())
            .Returns(new PagedResult<User>([user], 1, 1, 10));

        var result = await _sut.GetPagedAsync(pagedRequest);

        Assert.Single(result.Items);
        Assert.Equal(user.Email, result.Items.First().Email);
    }
}
