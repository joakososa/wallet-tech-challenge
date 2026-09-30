using System.Text;

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

    /// <summary>
    /// Documento completo listo para persistir y devolver: es el JSON que se firmó más <c>proof</c> (ADR 013).
    /// </summary>
    public string ToJson()
    {
        var payload = new CredentialPayload(
            Id, Type, Issuer, ValidFrom, ValidUntil, CredentialStatus, CredentialSubject);

        return Encoding.UTF8.GetString(new CredentialJsonBuilder(payload).WithProof(Proof).Build());
    }
}
