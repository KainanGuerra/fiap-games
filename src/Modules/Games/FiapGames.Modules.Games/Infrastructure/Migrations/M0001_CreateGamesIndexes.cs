using FiapGames.Shared.Infrastructure.Migrations;
using MongoDB.Driver;

namespace FiapGames.Modules.Games.Infrastructure.Migrations;

public sealed class M0001_CreateGamesIndexes : IMongoMigration
{
    public long Version => 1;

    public string Name => "Create indexes on games.title and games.genre";

    public Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("games");

        var models = new[]
        {
            new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Ascending("Title"),
                new CreateIndexOptions { Name = "ix_games_title" }),
            new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Ascending("Genre"),
                new CreateIndexOptions { Name = "ix_games_genre" })
        };

        return collection.Indexes.CreateManyAsync(models, cancellationToken);
    }
}
