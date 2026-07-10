using Xunit;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>
/// A <see cref="FactAttribute"/> that runs only when a Postgres connection string is available in
/// the <c>FCA_TEST_DB</c> environment variable. Database-backed tests are skipped (not failed)
/// otherwise, so CI without a database stays green while local and deploy runs exercise them.
/// </summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public const string EnvVar = "FCA_TEST_DB";

    public RequiresDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Skip = $"Set {EnvVar} to a Postgres connection string to run database-backed tests.";
        }
    }

    public static string? ConnectionString => Environment.GetEnvironmentVariable(EnvVar);
}
