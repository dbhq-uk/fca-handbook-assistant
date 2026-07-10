namespace FcaHandbookAssistant.Core.Domain;

/// <summary>
/// A citation in a grounded answer: the provision <see cref="Reference"/>, its Handbook
/// <see cref="Url"/>, and the <see cref="Quote"/> the answer relied on.
/// </summary>
public sealed record Citation(string Reference, string Url, string Quote);
