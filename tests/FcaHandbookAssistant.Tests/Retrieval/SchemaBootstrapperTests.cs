using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Retrieval;

[Collection("database")]
public class SchemaBootstrapperTests
{
    readonly DatabaseFixture _db;

    public SchemaBootstrapperTests(DatabaseFixture db) => _db = db;

    [RequiresDatabaseFact]
    public async Task Applying_schema_twice_is_idempotent_and_creates_tables()
    {
        // The fixture already applied it once; applying again must not throw.
        await SchemaBootstrapper.EnsureAsync(_db.DataSource!);

        await using var command = _db.DataSource!.CreateCommand(
            "SELECT to_regclass('public.provisions') IS NOT NULL AND to_regclass('public.answer_audit') IS NOT NULL;");
        var bothExist = (bool)(await command.ExecuteScalarAsync())!;
        Assert.True(bothExist);
    }
}
