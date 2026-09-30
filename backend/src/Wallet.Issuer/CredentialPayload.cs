namespace Wallet.Issuer;

/// <summary>Todo lo que se firma: la credencial sin su <c>proof</c>.</summary>
internal sealed record CredentialPayload(
    string Id,
    IReadOnlyList<string> Types,
    string Issuer,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus CredentialStatus,
    IReadOnlyDictionary<string, string> CredentialSubject)
{
    /// <summary>JSON canónico sobre el que se calcula la firma (ADR 004, 012 y 014).</summary>
    public byte[] ToCanonicalBytes() => new CredentialJsonBuilder(this).Build();
}
