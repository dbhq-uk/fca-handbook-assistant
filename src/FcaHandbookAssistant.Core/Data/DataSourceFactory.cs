using Npgsql;
using Pgvector.Npgsql;

namespace FcaHandbookAssistant.Core.Data;

/// <summary>
/// Builds an <see cref="NpgsqlDataSource"/> with the pgvector type mapping enabled, so
/// <see cref="Pgvector.Vector"/> parameters and results map to the Postgres <c>vector</c> type.
/// </summary>
public static class DataSourceFactory
{
    public static NpgsqlDataSource Create(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        return builder.Build();
    }
}
