using FiapGames.Shared.Kernel.Entities;

namespace FiapGames.Modules.Games.Domain;

public class Game : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string Genre { get; private set; } = string.Empty;

    public string Platform { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public DateOnly ReleaseDate { get; private set; }

    private Game() { }

    public Game(string title, string genre, string platform, decimal price, DateOnly releaseDate, string? description = null)
    {
        Title = title;
        Genre = genre;
        Platform = platform;
        Price = price;
        ReleaseDate = releaseDate;
        Description = description;
    }

    public void UpdateDetails(string title, string genre, string platform, decimal price, DateOnly releaseDate, string? description)
    {
        Title = title;
        Genre = genre;
        Platform = platform;
        Price = price;
        ReleaseDate = releaseDate;
        Description = description;
        Touch();
    }
}
