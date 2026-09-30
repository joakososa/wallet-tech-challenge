namespace Wallet.Issuer;

/// <summary>Un Issuer la lanza cuando no puede firmar la credencial (UC01 5a).</summary>
public sealed class IssuerSigningException : Exception
{
    public IssuerSigningException(string message)
        : base(message)
    {
    }

    public IssuerSigningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
