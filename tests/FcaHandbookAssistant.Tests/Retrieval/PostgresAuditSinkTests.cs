using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Retrieval;

[Collection("database")]
public class PostgresAuditSinkTests
{
    readonly DatabaseFixture _db;

    public PostgresAuditSinkTests(DatabaseFixture db) => _db = db;

    [RequiresDatabaseFact]
    public async Task Writes_an_audit_record_that_reads_back()
    {
        await using (var reset = _db.DataSource!.CreateCommand("TRUNCATE answer_audit;"))
        {
            await reset.ExecuteNonQueryAsync();
        }

        var sink = new PostgresAuditSink(_db.DataSource!);
        var record = new AuditRecord(
            DateTimeOffset.UtcNow,
            "redacted question",
            [new RetrievedReference("PRIN 2.1.1", 0.9)],
            ["PRIN 2.1.1"],
            Refused: false,
            Reason: null,
            Model: "test-model",
            PromptVersion: "2026-07-10.1",
            InputSafety: "safe",
            OutputSafety: "safe",
            LatencyMs: 42,
            PromptTokens: 100,
            CompletionTokens: 20);

        await sink.WriteAsync(record);

        await using var command = _db.DataSource!.CreateCommand(
            """
            SELECT question, refused, model, prompt_version, latency_ms, prompt_tokens, completion_tokens, cited::text
            FROM answer_audit ORDER BY id DESC LIMIT 1;
            """);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("redacted question", reader.GetString(0));
        Assert.False(reader.GetBoolean(1));
        Assert.Equal("test-model", reader.GetString(2));
        Assert.Equal("2026-07-10.1", reader.GetString(3));
        Assert.Equal(42, reader.GetInt64(4));
        Assert.Equal(100, reader.GetInt32(5));
        Assert.Equal(20, reader.GetInt32(6));
        Assert.Contains("PRIN 2.1.1", reader.GetString(7));
    }
}
