namespace Wallet.Tenant;

/// <summary>Filtros del listado: el tenant es obligatorio; el DNI y el número de socio, opcionales.</summary>
public sealed record CredentialFilter(string TenantId, string? Dni = null, long? NumeroSocio = null);
