namespace Wallet.Tenant;

/// <param name="Dni">Filtro opcional; se normaliza como en el alta.</param>
/// <param name="NumeroSocio">Filtro opcional, con o sin ceros a la izquierda (<c>000123</c> o <c>123</c>).</param>
public sealed record ListCredentialsQuery(string? Dni = null, string? NumeroSocio = null);
