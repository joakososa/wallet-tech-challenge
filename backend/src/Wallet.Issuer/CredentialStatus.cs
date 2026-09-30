namespace Wallet.Issuer;

/// <summary>Estado de la credencial. Los valores numéricos son los que se firman (ADR 012).</summary>
public enum CredentialStatus
{
    Active = 0,
    Revoked = 1,
    Suspended = 2
}
