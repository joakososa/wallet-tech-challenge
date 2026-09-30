namespace Wallet.Tenant;

public interface ISocioRepository
{
    /// <summary>Busca por DNI ya normalizado. Devuelve <c>null</c> si no existe.</summary>
    Task<Socio?> FindByDniAsync(string dni, CancellationToken ct = default);

    /// <summary>
    /// Toma el próximo número de la secuencia. No participa de ninguna transacción: si el alta falla,
    /// el número no se devuelve y queda un hueco (ADR 003).
    /// </summary>
    Task<long> NextNumeroSocioAsync(CancellationToken ct = default);
}
