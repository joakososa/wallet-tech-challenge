using Wallet.Tenant;
using Wallet.UnitTests.Tenant.Fakes;

namespace Wallet.UnitTests.Tenant;

public class ListCredentialsUseCaseTests
{
    private static readonly DateTimeOffset _now = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);

    private readonly InMemoryStore _store = new();
    private readonly ListCredentialsUseCase _list;
    private readonly IssueCredentialUseCase _issue;

    public ListCredentialsUseCaseTests()
    {
        var options = new TenantOptions();
        _list = new ListCredentialsUseCase(_store, options);
        _issue = new IssueCredentialUseCase(_store, _store, new FakeCredentialIssuer(_now), new FixedTimeProvider(_now), options);
    }

    [Fact]
    public async Task HandleAsync_SinCredenciales_DevuelveUnaListaVacia()
    {
        var result = await _list.HandleAsync(new ListCredentialsQuery());

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_SinFiltros_DevuelveTodasLasCredencialesDelTenant()
    {
        await Emitir("30123456");
        await Emitir("31123456");

        var result = await _list.HandleAsync(new ListCredentialsQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_ConFiltroPorDniConPuntos_LoNormalizaYFiltra()
    {
        await Emitir("30123456");
        await Emitir("31123456");

        var result = await _list.HandleAsync(new ListCredentialsQuery(Dni: "30.123.456"));

        Assert.Equal("30123456", Assert.Single(result).Dni);
    }

    [Fact]
    public async Task HandleAsync_ConFiltroPorNumeroDeSocioConCeros_LoInterpretaYFiltra()
    {
        await Emitir("30123456");
        await Emitir("31123456");

        var result = await _list.HandleAsync(new ListCredentialsQuery(NumeroSocio: "000002"));

        Assert.Equal("31123456", Assert.Single(result).Dni);
    }

    [Fact]
    public async Task HandleAsync_ConFiltrosEnBlanco_LosIgnora()
    {
        await Emitir("30123456");

        var result = await _list.HandleAsync(new ListCredentialsQuery(Dni: " ", NumeroSocio: ""));

        Assert.Single(result);
    }

    [Theory]
    [InlineData("abc", null)]
    [InlineData(null, "abc")]
    public async Task HandleAsync_ConUnFiltroInvalido_LanzaArgumentException(string? dni, string? numeroSocio)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _list.HandleAsync(new ListCredentialsQuery(dni, numeroSocio)));
    }

    [Fact]
    public async Task HandleAsync_ConUnCancellationToken_LoPropagaAlRepositorio()
    {
        using var cts = new CancellationTokenSource();

        await _list.HandleAsync(new ListCredentialsQuery(), cts.Token);

        Assert.Equal(cts.Token, _store.LastToken);
    }

    private async Task Emitir(string dni)
    {
        await _issue.HandleAsync(new IssueCredentialCommand("Juan", "Pérez", dni, Categoria.Adulto, "https://x.test/f.jpg"));
    }
}
