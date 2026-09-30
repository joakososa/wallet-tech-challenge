namespace Wallet.Issuer;

public interface ICredentialIssuer
{
    /// <summary>
    /// Agrega <c>id</c>, <c>type</c>, <c>issuer</c>, <c>validFrom</c>, <c>validUntil</c> y <c>proof</c>
    /// al <paramref name="credentialSubject"/> y devuelve la credencial firmada.
    /// </summary>
    /// <exception cref="IssuerSigningException">Si no se puede firmar la credencial.</exception>
    Task<VerifiableCredential> IssueAsync(
        IReadOnlyDictionary<string, string> credentialSubject,
        IReadOnlyList<string> types,
        CredentialStatus status = CredentialStatus.Active,
        CancellationToken ct = default);
}
