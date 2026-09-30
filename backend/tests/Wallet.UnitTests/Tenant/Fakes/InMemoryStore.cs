using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant.Fakes;

/// <summary>Implementa los dos puertos de persistencia en memoria, con contadores y un gancho para simular la carrera de DNI.</summary>
public sealed class InMemoryStore : ISocioRepository, ICredentialRepository
{
    private readonly Dictionary<string, Socio> _socios = [];
    private readonly List<Credential> _credentials = [];
    private long _lastNumero;

    public IReadOnlyCollection<Socio> Socios => _socios.Values;

    public IReadOnlyList<Credential> Credentials => _credentials;

    public int AddCalls { get; private set; }

    public int NextNumeroCalls { get; private set; }

    public CancellationToken LastToken { get; private set; }

    /// <summary>Se ejecuta al inicio de <see cref="AddAsync"/>, antes de guardar. Puede lanzar <see cref="DuplicateDniException"/>.</summary>
    public Action<Socio, Credential>? OnAdd { get; set; }

    /// <summary>Simula que otra alta ya guardó este socio (el ganador de la carrera).</summary>
    public void Seed(Socio socio) => _socios[socio.Dni] = socio;

    public Task<Socio?> FindByDniAsync(string dni, CancellationToken ct = default)
    {
        LastToken = ct;
        return Task.FromResult(_socios.GetValueOrDefault(dni));
    }

    public Task<long> NextNumeroSocioAsync(CancellationToken ct = default)
    {
        LastToken = ct;
        NextNumeroCalls++;
        return Task.FromResult(++_lastNumero);
    }

    public Task AddAsync(Socio socio, Credential credential, CancellationToken ct = default)
    {
        LastToken = ct;
        AddCalls++;
        OnAdd?.Invoke(socio, credential);

        _socios[socio.Dni] = socio;
        _credentials.Add(credential);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CredentialSummary>> ListAsync(CredentialFilter filter, CancellationToken ct = default)
    {
        LastToken = ct;
        IReadOnlyList<CredentialSummary> result = _credentials
            .Where(c => c.TenantId == filter.TenantId)
            .Where(c => filter.Dni is null || c.Dni == filter.Dni)
            .Where(c => filter.NumeroSocio is null || c.NumeroSocio == filter.NumeroSocio)
            .OrderByDescending(c => c.ValidFrom)
            .Select(c => new CredentialSummary(
                c.Id, c.VcId, c.Nombre, c.Apellido, c.Dni, c.NumeroSocio, c.Categoria, c.Foto,
                c.ValidFrom, c.ValidUntil, c.Status, c.Document))
            .ToList();

        return Task.FromResult(result);
    }
}
