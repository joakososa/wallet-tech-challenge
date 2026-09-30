using Wallet.Issuer;

namespace Wallet.Api.Simulation;

/// <summary>
/// Reemplaza al Issuer real y siempre lanza <see cref="IssuerSigningException"/>. Como nunca delega,
/// no necesita envolver al Issuer real. Sirve para demostrar UC01 5a a mano; el Issuer no sabe que existe (ADR 012).
/// </summary>
public sealed class SimulatedFailureCredentialIssuer : ICredentialIssuer
{
    public Task<VerifiableCredential> IssueAsync(
        IReadOnlyDictionary<string, string> credentialSubject,
        IReadOnlyList<string> types,
        CredentialStatus status = CredentialStatus.Active,
        CancellationToken ct = default) =>
        throw new IssuerSigningException("Falla de firma simulada (Issuer__SimulateFailure).");
}
