using System.Text.Json;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;
using Npgsql;
using NpgsqlTypes;

namespace FcaHandbookAssistant.Core.Data;

/// <summary>Writes audit records to the <c>answer_audit</c> table (retrieved/cited as jsonb).</summary>
public sealed class PostgresAuditSink : IAuditSink
{
    readonly NpgsqlDataSource _dataSource;

    public PostgresAuditSink(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO answer_audit
                (asked_at, question, retrieved, cited, refused, refusal_reason, model, prompt_version,
                 input_safety, output_safety, latency_ms, prompt_tokens, completion_tokens)
            VALUES
                (@asked_at, @question, @retrieved, @cited, @refused, @refusal_reason, @model, @prompt_version,
                 @input_safety, @output_safety, @latency_ms, @prompt_tokens, @completion_tokens);
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("asked_at", record.Timestamp);
        command.Parameters.AddWithValue("question", record.Question);
        command.Parameters.Add(new NpgsqlParameter("retrieved", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(record.Retrieved) });
        command.Parameters.Add(new NpgsqlParameter("cited", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(record.CitedReferences) });
        command.Parameters.AddWithValue("refused", record.Refused);
        command.Parameters.AddWithValue("refusal_reason", (object?)record.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("model", record.Model);
        command.Parameters.AddWithValue("prompt_version", record.PromptVersion);
        command.Parameters.AddWithValue("input_safety", (object?)record.InputSafety ?? DBNull.Value);
        command.Parameters.AddWithValue("output_safety", (object?)record.OutputSafety ?? DBNull.Value);
        command.Parameters.AddWithValue("latency_ms", record.LatencyMs);
        command.Parameters.AddWithValue("prompt_tokens", record.PromptTokens);
        command.Parameters.AddWithValue("completion_tokens", record.CompletionTokens);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
