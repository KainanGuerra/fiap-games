using FiapGames.Modules.Games.Application.Dtos;
using FiapGames.Modules.Games.Application.Validators;

namespace FiapGames.Modules.Games.Tests;

public class CreateGameRequestValidatorTests
{
    private readonly CreateGameRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var request = new CreateGameRequest("Hollow Knight", "Metroidvania", "PC", 14.99m, new DateOnly(2017, 2, 24), "desc");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Metroidvania", "PC", 14.99)]
    [InlineData("Hollow Knight", "", "PC", 14.99)]
    [InlineData("Hollow Knight", "Metroidvania", "", 14.99)]
    [InlineData("Hollow Knight", "Metroidvania", "PC", -1)]
    public void Validate_WithInvalidRequest_Fails(string title, string genre, string platform, decimal price)
    {
        var request = new CreateGameRequest(title, genre, platform, price, new DateOnly(2017, 2, 24), null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
