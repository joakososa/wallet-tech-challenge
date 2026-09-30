using Wallet.Issuer;

namespace Wallet.Tenant;

/// <summary>Una fila del listado: lo desnormalizado de <c>credentials</c>, sin join (ADR 002).</summary>
public sealed record CredentialSummary(
    Guid Id,
    string VcId,
    string Nombre,
    string Apellido,
    string Dni,
    long NumeroSocio,
    Categoria Categoria,
    string Foto,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus Status,
    string Document);
