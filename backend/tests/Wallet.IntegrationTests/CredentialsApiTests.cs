using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Api.Simulation;
using Wallet.Issuer;

namespace Wallet.IntegrationTests;

public sealed class CredentialsApiTests : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private const string Url = "/api/credentials";

    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public CredentialsApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Post_ConDatosValidos_DevuelveCreatedConNumeroDeSocioYLaVcFirmada()
    {
        var response = await _client.PostAsJsonAsync(Url, Alta("30.123.456"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("000001", body.GetProperty("numeroSocio").GetString());
        Assert.True(body.GetProperty("isNewSocio").GetBoolean());
        Assert.Matches(@"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$", body.GetProperty("validFrom").GetString()!);
        var credential = body.GetProperty("credential");
        Assert.Equal("Pérez", credential.GetProperty("credentialSubject").GetProperty("apellido").GetString());
        Assert.Equal("30123456", credential.GetProperty("credentialSubject").GetProperty("dni").GetString());
        Assert.Equal("HMAC-SHA256", credential.GetProperty("proof").GetProperty("type").GetString());
        Assert.Equal((1, 1), await _fixture.CountRowsAsync());
    }

    [Fact]
    public async Task Post_ConDatosValidos_LaFirmaDelDocumentoDevueltoSeVerificaConLaClave()
    {
        var response = await _client.PostAsJsonAsync(Url, Alta("30123456"));

        var credential = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("credential");
        // Se recalcula el HMAC sobre el documento sin proof, tal como se devolvió (con tildes sin escapar).
        var canonical = WriteWithoutProof(credential);
        var expected = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(ApiFixture.SigningKey), canonical));
        Assert.Equal(expected, credential.GetProperty("proof").GetProperty("proofValue").GetString());
    }

    [Fact]
    public async Task Post_ConElDniDeUnSocioExistente_ReutilizaNumeroYDidYActualizaLosDatos()
    {
        var first = await (await _client.PostAsJsonAsync(Url, Alta("30123456"))).Content.ReadFromJsonAsync<JsonElement>();

        var response = await _client.PostAsJsonAsync(Url, Alta("30123456") with { Nombre = "Juan Carlos", Categoria = "adulto" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var second = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(second.GetProperty("isNewSocio").GetBoolean());
        Assert.Equal("000001", second.GetProperty("numeroSocio").GetString());
        Assert.Equal(SubjectId(first), SubjectId(second));
        Assert.Equal("Juan Carlos", second.GetProperty("credential").GetProperty("credentialSubject").GetProperty("nombre").GetString());
        Assert.Equal((1, 2), await _fixture.CountRowsAsync());
    }

    [Fact]
    public async Task Post_ConDatosInvalidos_DevuelveBadRequestConLosErroresPorCampoYNoPersisteNada()
    {
        var invalid = new AltaRequest(" ", "Pérez", "12", "senior", "ftp://x");

        var response = await _client.PostAsJsonAsync(Url, invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("nombre", out _));
        Assert.True(errors.TryGetProperty("dni", out _));
        Assert.True(errors.TryGetProperty("categoria", out _));
        Assert.True(errors.TryGetProperty("foto", out _));
        Assert.False(errors.TryGetProperty("apellido", out _));
        Assert.Equal((0, 0), await _fixture.CountRowsAsync());
    }

    [Fact]
    public async Task Post_SiFallaLaFirma_DevuelveErrorConCodigoYNoPersisteNada()
    {
        // Se usa el decorator real de simulación, el mismo que activa Issuer__SimulateFailure (ADR 012).
        using var failing = _fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<ICredentialIssuer, SimulatedFailureCredentialIssuer>()));
        using var client = failing.CreateClient();

        var response = await client.PostAsJsonAsync(Url, Alta("30123456"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("issuer_signing_failed", problem.GetProperty("code").GetString());
        Assert.Equal((0, 0), await _fixture.CountRowsAsync());
    }

    [Fact]
    public async Task Post_ConAltasConcurrentesDelMismoDniNuevo_CreaUnSoloSocioConUnSoloNumero()
    {
        var requests = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync(Url, Alta("30123456")));

        var responses = await Task.WhenAll(requests);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Single(bodies.Select(b => b.GetProperty("numeroSocio").GetString()).Distinct());
        Assert.Single(bodies.Select(SubjectId).Distinct());
        Assert.Equal((1, 6), await _fixture.CountRowsAsync());
    }

    [Fact]
    public async Task Get_SinCredenciales_DevuelveUnaListaVacia()
    {
        var response = await _client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Get_ConCredencialesEmitidas_LasDevuelveDeLaMasNuevaALaMasVieja()
    {
        await _client.PostAsJsonAsync(Url, Alta("30123456"));
        await Task.Delay(1100);
        await _client.PostAsJsonAsync(Url, Alta("31123456") with { Nombre = "Ana" });

        var list = await _client.GetFromJsonAsync<JsonElement>(Url);

        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("Ana", list[0].GetProperty("nombre").GetString());
        Assert.Equal("000002", list[0].GetProperty("numeroSocio").GetString());
        Assert.Equal("niño", list[1].GetProperty("categoria").GetString());
        Assert.Equal(0, list[1].GetProperty("status").GetInt32());
        Assert.Equal("Pérez", list[1].GetProperty("credential").GetProperty("credentialSubject").GetProperty("apellido").GetString());
    }

    [Fact]
    public async Task Get_ConFiltros_DevuelveSoloLasCredencialesDelSocio()
    {
        await _client.PostAsJsonAsync(Url, Alta("30123456"));
        await _client.PostAsJsonAsync(Url, Alta("31123456"));

        var byDni = await _client.GetFromJsonAsync<JsonElement>($"{Url}?dni=31.123.456");
        var byNumero = await _client.GetFromJsonAsync<JsonElement>($"{Url}?numeroSocio=000001");

        Assert.Equal("31123456", Assert.Single(byDni.EnumerateArray()).GetProperty("dni").GetString());
        Assert.Equal("30123456", Assert.Single(byNumero.EnumerateArray()).GetProperty("dni").GetString());
    }

    [Fact]
    public async Task Get_ConUnFiltroInvalido_DevuelveBadRequest()
    {
        var response = await _client.GetAsync($"{Url}?dni=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_ConLaBaseDisponible_DevuelveOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static AltaRequest Alta(string dni) =>
        new("Juan", "Pérez", dni, "niño", "https://cdn.futbol.com.ar/socios/8f14e45f.jpg");

    private static string? SubjectId(JsonElement body) =>
        body.GetProperty("credential").GetProperty("credentialSubject").GetProperty("id").GetString();

    private static byte[] WriteWithoutProof(JsonElement credential)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            foreach (var property in credential.EnumerateObject().Where(p => p.Name != "proof"))
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private sealed record AltaRequest(string Nombre, string Apellido, string Dni, string Categoria, string Foto);
}
