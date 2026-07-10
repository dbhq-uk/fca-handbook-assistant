using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Abstractions;

/// <summary>Persists provisions and their embeddings, and fetches a provision by its reference.</summary>
public interface IProvisionStore
{
    /// <summary>Inserts or updates the provision, keyed by <see cref="Provision.Reference"/>.</summary>
    Task UpsertAsync(Provision provision, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default);

    /// <summary>Returns the provision with the given reference, or <c>null</c> if it is not stored.</summary>
    Task<Provision?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
}
