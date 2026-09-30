namespace Wallet.Issuer;

public sealed class IssuerOptions
{
    /// <summary>DID del emisor (<c>Issuer__Did</c>).</summary>
    public required string Did { get; init; }

    /// <summary>Secreto HMAC (<c>Issuer__SigningKey</c>). Nunca se versiona.</summary>
    public required string SigningKey { get; init; }

    /// <summary>Identificador de la clave vigente (<c>Issuer__KeyId</c>). Ver ADR 011.</summary>
    public required string KeyId { get; init; }

    /// <summary>Base del <c>id</c> de la credencial (<c>Issuer__CredentialBaseUri</c>).</summary>
    public required string CredentialBaseUri { get; init; }
}
