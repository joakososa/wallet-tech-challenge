using Wallet.Issuer;
using Wallet.Tenant;
using Wallet.UnitTests.Tenant.Fakes;

namespace Wallet.UnitTests.Tenant;

public class IssueCredentialUseCaseTests
{
    private static readonly DateTimeOffset _now = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);

    private readonly InMemoryStore _store = new();
    private readonly FakeCredentialIssuer _issuer = new(_now);
    private readonly IssueCredentialUseCase _useCase;

    public IssueCredentialUseCaseTests()
    {
        _useCase = new IssueCredentialUseCase(
            _store, _store, _issuer, new FixedTimeProvider(_now), new TenantOptions());
    }

    [Fact]
    public async Task HandleAsync_ConUnDniNuevo_CreaElSocioConNumeroYDidYPersisteLaCredencial()
    {
        var result = await _useCase.HandleAsync(Command());

        Assert.True(result.IsNewSocio);
        Assert.Equal("000001", result.NumeroSocio);
        var socio = Assert.Single(_store.Socios);
        Assert.Equal("30123456", socio.Dni);
        Assert.Equal(1, socio.NumeroSocio);
        Assert.Equal($"did:example:{socio.Id}", socio.SubjectDid);
        var credential = Assert.Single(_store.Credentials);
        Assert.Equal(socio.Id, credential.SocioId);
        Assert.Equal("club-futbol", credential.TenantId);
    }

    [Fact]
    public async Task HandleAsync_ConUnDniNuevo_ArmaLosClaimsYLosTiposParaElIssuer()
    {
        await _useCase.HandleAsync(Command());

        var socio = Assert.Single(_store.Socios);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["id"] = socio.SubjectDid,
                ["nombre"] = "Juan",
                ["apellido"] = "Pérez",
                ["dni"] = "30123456",
                ["numeroSocio"] = "000001",
                ["categoria"] = "adulto",
                ["foto"] = "https://cdn.futbol.com.ar/socios/8f14e45f.jpg"
            },
            _issuer.LastClaims);
        Assert.Equal(["VerifiableCredential", "SocioCredential"], _issuer.LastTypes);
    }

    [Fact]
    public async Task HandleAsync_ConUnDniExistente_ReutilizaDidYNumeroYActualizaLosDatos()
    {
        var first = await _useCase.HandleAsync(Command());
        var socio = Assert.Single(_store.Socios);
        var did = socio.SubjectDid;

        var second = await _useCase.HandleAsync(Command() with { Nombre = "Juan Carlos", Categoria = Categoria.Juvenil });

        Assert.False(second.IsNewSocio);
        Assert.Equal(first.NumeroSocio, second.NumeroSocio);
        Assert.Equal(1, _store.NextNumeroCalls);
        Assert.Same(socio, Assert.Single(_store.Socios));
        Assert.Equal(did, socio.SubjectDid);
        Assert.Equal("Juan Carlos", socio.Nombre);
        Assert.Equal(Categoria.Juvenil, socio.Categoria);
        Assert.Equal(2, _store.Credentials.Count);
        Assert.Equal(did, _issuer.LastClaims!["id"]);
    }

    [Fact]
    public async Task HandleAsync_ConUnDniConPuntos_LoNormalizaYNoCreaUnSegundoSocio()
    {
        await _useCase.HandleAsync(Command());

        var result = await _useCase.HandleAsync(Command() with { Dni = "30.123.456" });

        Assert.False(result.IsNewSocio);
        Assert.Single(_store.Socios);
        Assert.Equal("30123456", _issuer.LastClaims!["dni"]);
    }

    [Fact]
    public async Task HandleAsync_ConUnDniInvalido_LanzaArgumentExceptionYNoHaceNada()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.HandleAsync(Command() with { Dni = "abc" }));

        Assert.Equal(0, _issuer.IssueCalls);
        Assert.Equal(0, _store.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_SiFallaLaFirmaConUnSocioNuevo_PropagaLaExcepcionYNoPersisteNada()
    {
        _issuer.OnIssue = () => throw new IssuerSigningException("falla simulada");

        await Assert.ThrowsAsync<IssuerSigningException>(() => _useCase.HandleAsync(Command()));

        Assert.Equal(0, _store.AddCalls);
        Assert.Empty(_store.Socios);
        Assert.Empty(_store.Credentials);
    }

    [Fact]
    public async Task HandleAsync_SiFallaLaFirmaConUnSocioExistente_NoModificaAlSocioNiAgregaCredenciales()
    {
        await _useCase.HandleAsync(Command());
        var socio = Assert.Single(_store.Socios);
        _issuer.OnIssue = () => throw new IssuerSigningException("falla simulada");

        await Assert.ThrowsAsync<IssuerSigningException>(
            () => _useCase.HandleAsync(Command() with { Nombre = "Otro", Categoria = Categoria.Nino }));

        Assert.Equal("Juan", socio.Nombre);
        Assert.Equal(Categoria.Adulto, socio.Categoria);
        Assert.Single(_store.Credentials);
        Assert.Equal(1, _store.AddCalls);
    }

    [Fact]
    public async Task HandleAsync_SiOtraAltaGanaLaCarreraDelDni_ReintentaConLosDatosDelGanador()
    {
        var ganador = new Socio(
            Guid.NewGuid(), "did:example:ganador", 50, "30123456", "Ganador", "Otro", Categoria.Adulto, "https://x.test/f.jpg", _now, _now);
        _store.OnAdd = (_, _) =>
        {
            _store.OnAdd = null;
            _store.Seed(ganador);
            throw new DuplicateDniException("30123456");
        };

        var result = await _useCase.HandleAsync(Command());

        Assert.False(result.IsNewSocio);
        Assert.Equal("000050", result.NumeroSocio);
        Assert.Equal(2, _issuer.IssueCalls);
        Assert.Equal("did:example:ganador", _issuer.LastClaims!["id"]);
        Assert.Same(ganador, Assert.Single(_store.Socios));
        Assert.Single(_store.Credentials);
    }

    [Fact]
    public async Task HandleAsync_SiLaCarreraDelDniSeRepite_PropagaLaExcepcionSinReintentarDeNuevo()
    {
        _store.OnAdd = (_, _) => throw new DuplicateDniException("30123456");

        await Assert.ThrowsAsync<DuplicateDniException>(() => _useCase.HandleAsync(Command()));

        Assert.Equal(2, _store.AddCalls);
        Assert.Empty(_store.Credentials);
    }

    [Fact]
    public async Task HandleAsync_ConUnaAltaExitosa_ElSnapshotYElDocumentoSonLosDeLaVcFirmada()
    {
        var result = await _useCase.HandleAsync(Command());

        var credential = Assert.Single(_store.Credentials);
        Assert.Equal(result.Vc.Id, credential.VcId);
        Assert.Equal(result.Vc.ToJson(), credential.Document);
        Assert.Equal(result.Document, credential.Document);
        Assert.Equal(result.Vc.ValidFrom, credential.ValidFrom);
        Assert.Equal(result.Vc.ValidUntil, credential.ValidUntil);
        Assert.Equal(CredentialStatus.Active, credential.Status);
    }

    [Fact]
    public async Task HandleAsync_ConUnCancellationToken_LoPropagaAlIssuerYALosRepositorios()
    {
        using var cts = new CancellationTokenSource();

        await _useCase.HandleAsync(Command(), cts.Token);

        Assert.Equal(cts.Token, _issuer.LastToken);
        Assert.Equal(cts.Token, _store.LastToken);
    }

    private static IssueCredentialCommand Command() =>
        new("Juan", "Pérez", "30123456", Categoria.Adulto, "https://cdn.futbol.com.ar/socios/8f14e45f.jpg");
}
