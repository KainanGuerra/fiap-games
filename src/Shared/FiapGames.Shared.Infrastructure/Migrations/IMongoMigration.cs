using MongoDB.Driver;

namespace FiapGames.Shared.Infrastructure.Migrations;

/// <summary>
/// A single, ordered, idempotent change applied to a Mongo database
/// (index creation, seed data, collection setup). Executed once and
/// tracked in the "_migrationHistory" collection.
/// </summary>
public interface IMongoMigration
{
    long Version { get; }

    string Name { get; }

    Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken);
}
