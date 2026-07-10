using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>An in-memory <see cref="IProvisionStore"/> keyed by reference (case-insensitive).</summary>
public sealed class InMemoryProvisionStore : IProvisionStore
{
    readonly Dictionary<string, Provision> _byReference = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(Provision provision, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        _byReference[provision.Reference] = provision;
        return Task.CompletedTask;
    }

    public Task<Provision?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byReference.GetValueOrDefault(reference));
}
