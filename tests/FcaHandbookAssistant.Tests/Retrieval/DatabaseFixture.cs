using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.TestSupport;
using Npgsql;

namespace FcaHandbookAssistant.Tests.Retrieval;

/// <summary>
/// Shared database for the DB-backed tests: builds the pgvector-enabled data source from
/// <c>FCA_TEST_DB</c> and applies the schema once. When the variable is unset, the data source
/// is null and the tests that need it are skipped by <see cref="RequiresDatabaseFactAttribute"/>.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public NpgsqlDataSource? DataSource { get; private set; }

    public async Task InitializeAsync()
    {
        var connectionString = RequiresDatabaseFactAttribute.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        DataSource = DataSourceFactory.Create(connectionString);
        // Tests seed their own 1536-dimension data, so start from a known schema regardless of any
        // prior ingestion (which may have created the table at a different embedding dimension).
        await using (var drop = DataSource.CreateCommand("DROP TABLE IF EXISTS provisions;"))
        {
            await drop.ExecuteNonQueryAsync();
        }

        await SchemaBootstrapper.EnsureAsync(DataSource);
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }
    }
}

[CollectionDefinition("database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
