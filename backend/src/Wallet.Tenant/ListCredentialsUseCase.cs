namespace Wallet.Tenant;

/// <summary>UC02: listado de las credenciales del tenant. El orden y el estado vacío los resuelve el repositorio.</summary>
public sealed class ListCredentialsUseCase(ICredentialRepository credentials, TenantOptions options)
{
    /// <exception cref="ArgumentException">Si un filtro informado no es válido (la API lo valida antes).</exception>
    public Task<IReadOnlyList<CredentialSummary>> HandleAsync(ListCredentialsQuery query, CancellationToken ct = default)
    {
        string? dni = null;
        if (!string.IsNullOrWhiteSpace(query.Dni))
        {
            if (!Wallet.Tenant.Dni.TryNormalize(query.Dni, out var normalized))
            {
                throw new ArgumentException("El DNI del filtro no es válido.", nameof(query));
            }

            dni = normalized;
        }

        long? numeroSocio = null;
        if (!string.IsNullOrWhiteSpace(query.NumeroSocio))
        {
            if (!NumeroSocio.TryParse(query.NumeroSocio, out var parsed))
            {
                throw new ArgumentException("El número de socio del filtro no es válido.", nameof(query));
            }

            numeroSocio = parsed;
        }

        return credentials.ListAsync(new CredentialFilter(options.Id, dni, numeroSocio), ct);
    }
}
