namespace FcaHandbookAssistant.Core.Domain;

/// <summary>A provision returned by retrieval, with its similarity score normalised to [0, 1].</summary>
public sealed record RetrievedProvision(Provision Provision, double Score);
