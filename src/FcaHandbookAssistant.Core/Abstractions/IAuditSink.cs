using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Abstractions;

/// <summary>Persists the per-answer audit record for traceability.</summary>
public interface IAuditSink
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
