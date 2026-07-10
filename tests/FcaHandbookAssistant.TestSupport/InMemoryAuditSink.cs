using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>An <see cref="IAuditSink"/> that records everything in memory for assertions.</summary>
public sealed class InMemoryAuditSink : IAuditSink
{
    readonly List<AuditRecord> _records = [];

    public IReadOnlyList<AuditRecord> Records => _records;

    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }
}
