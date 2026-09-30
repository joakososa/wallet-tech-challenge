namespace Wallet.Tenant;

/// <summary>El repositorio la lanza cuando el <c>UNIQUE(dni)</c> rechaza el alta de un socio (ADR 003).</summary>
public sealed class DuplicateDniException : Exception
{
    public DuplicateDniException(string dni)
        : base($"Ya existe un socio con el DNI {dni}.")
    {
    }

    public DuplicateDniException(string dni, Exception innerException)
        : base($"Ya existe un socio con el DNI {dni}.", innerException)
    {
    }
}
