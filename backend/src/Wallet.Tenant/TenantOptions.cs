namespace Wallet.Tenant;

public sealed class TenantOptions
{
    /// <summary>Identificador del tenant (<c>Tenant__Id</c>). No hay autenticación: es una constante de configuración.</summary>
    public string Id { get; init; } = "club-futbol";
}
