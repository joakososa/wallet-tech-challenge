namespace Wallet.Issuer;

public sealed record VerifiableCredential(
    string Id,
    IReadOnlyList<string> Type,
    string Issuer,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus CredentialStatus,
    IReadOnlyDictionary<string, string> CredentialSubject,
    Proof Proof)
{
    internal VerifiableCredential(CredentialPayload payload, Proof proof)
        : this(
            payload.Id,
            payload.Types,
            payload.Issuer,
            payload.ValidFrom,
            payload.ValidUntil,
            payload.CredentialStatus,
            payload.CredentialSubject,
            proof)
    {
    }
}
