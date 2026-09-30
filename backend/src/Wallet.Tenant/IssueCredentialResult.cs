using Wallet.Issuer;

namespace Wallet.Tenant;

/// <param name="Vc">La credencial firmada.</param>
/// <param name="Document">El documento tal como se persistió (<see cref="VerifiableCredential.ToJson"/>).</param>
/// <param name="NumeroSocio">Con relleno de ceros a 6 dígitos.</param>
/// <param name="IsNewSocio"><c>false</c> si el DNI ya existía y se reutilizó el socio (ADR 006).</param>
public sealed record IssueCredentialResult(
    VerifiableCredential Vc,
    string Document,
    string NumeroSocio,
    bool IsNewSocio);
