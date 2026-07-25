using FiapGames.Modules.Games.Application.Abstractions;
using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Modules.Games.Application.Services;
using FiapGames.Modules.Games.Domain;
using FiapGames.Shared.Kernel.Pagination;
using FiapGames.Shared.Kernel.Results;
using NSubstitute;

namespace FiapGames.Modules.Games.Tests;

public class GameServiceTests
{
    private readonly IGameRepository _repository = Substitute.For<IGameRepository>();
    private readonly GameService _sut;

    public GameServiceTests()
    {
        _sut = new GameService(_repository);
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsGame()
    {
        var request = new CreateGameRequest("Hollow Knight", "Metroidvania", "PC", 14.99m, new DateOnly(2017, 2, 24), "Great game");

        var response = await _sut.CreateAsync(request);

        Assert.Equal(request.Title, response.Title);
        await _repository.Received(1).AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenGameDoesNotExist_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((Game?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateAsync_WhenGameExists_UpdatesDetails()
    {
        var game = new Game("Old Title", "Genre", "PC", 10m, new DateOnly(2020, 1, 1));
        _repository.GetByIdAsync(game.Id).Returns(game);

        var request = new UpdateGameRequest("New Title", "New Genre", "PS5", 20m, new DateOnly(2021, 1, 1), "desc");
        var result = await _sut.UpdateAsync(game.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", result.Value.Title);
        _repository.Received(1).Update(game);
    }

    [Fact]
    public async Task DeleteAsync_WhenGameExists_RemovesGame()
    {
        var game = new Game("Title", "Genre", "PC", 10m, new DateOnly(2020, 1, 1));
        _repository.GetByIdAsync(game.Id).Returns(game);

        var result = await _sut.DeleteAsync(game.Id);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(game);
    }

    [Fact]
    public async Task GetPagedAsync_MapsDomainGamesToResponses()
    {
        var game = new Game("Title", "Genre", "PC", 10m, new DateOnly(2020, 1, 1));
        _repository.GetPagedAsync(Arg.Any<PagedRequest>())
            .Returns(new PagedResult<Game>([game], 1, 1, 10));

        var result = await _sut.GetPagedAsync(new PagedRequest { Page = 1, PageSize = 10 });

        Assert.Single(result.Items);
        Assert.Equal(game.Title, result.Items.First().Title);
    }
}
