namespace Wallet.Issuer;

public sealed record VerifiableCredential(
    string Id,
    IReadOnlyList<string> Type,
    string Issuer,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus CredentialStatus,
    IReadOnlyDictionary<string, string> CredentialSubject,
    Proof Proof);
