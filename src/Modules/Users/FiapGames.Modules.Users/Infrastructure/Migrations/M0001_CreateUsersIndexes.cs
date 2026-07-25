using FiapGames.Shared.Infrastructure.Migrations;
using MongoDB.Driver;

namespace FiapGames.Modules.Users.Infrastructure.Migrations;

public sealed class M0001_CreateUsersIndexes : IMongoMigration
{
    public long Version => 1;

    public string Name => "Create unique index on users.email";

    public Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("users");
        var indexKeys = Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Ascending("Email");
        var indexModel = new CreateIndexModel<MongoDB.Bson.BsonDocument>(
            indexKeys,
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        return collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}
