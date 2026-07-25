using FiapGames.Modules.Games.Domain;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace FiapGames.Modules.Games.Infrastructure.Persistence;

public sealed class GamesDbContext : DbContext
{
    public DbSet<Game> Games => Set<Game>();

    public GamesDbContext(DbContextOptions<GamesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(builder =>
        {
            builder.ToCollection("games");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Title).IsRequired();
            builder.Property(g => g.Genre).IsRequired();
            builder.Property(g => g.Platform).IsRequired();
        });
    }
}
