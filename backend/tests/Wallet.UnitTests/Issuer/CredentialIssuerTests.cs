using System.Security.Cryptography;
using System.Text;
using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class CredentialIssuerTests
{
    private const string _signingKey = "clave-de-prueba-de-al-menos-32-bytes!!";
    private const string _baseUri = "https://credenciales.futbol.com.ar/";

    private static readonly string[] _types = ["VerifiableCredential", "SocioCredential"];

    private static readonly Dictionary<string, string> _subject = new()
    {
        ["id"] = "did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ["nombre"] = "Juan",
        ["apellido"] = "Pérez"
    };

    [Fact]
    public async Task IssueAsync_ConUnaHoraConFracciones_TruncaValidFromAlSegundo()
    {
        var ahora = new DateTimeOffset(2026, 8, 9, 14, 32, 10, TimeSpan.Zero).AddTicks(4_567_891);
        var issuer = CrearIssuer(ahora);

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal(new DateTimeOffset(2026, 8, 9, 14, 32, 10, TimeSpan.Zero), vc.ValidFrom);
    }

    [Fact]
    public async Task IssueAsync_FijaValidUntilUnAnioDespuesDeValidFrom()
    {
        var issuer = CrearIssuer(new DateTimeOffset(2026, 8, 9, 14, 32, 10, TimeSpan.Zero));

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal(new DateTimeOffset(2027, 8, 9, 14, 32, 10, TimeSpan.Zero), vc.ValidUntil);
    }

    [Fact]
    public async Task IssueAsync_ConEmisionUnDiaBisiesto_VenceElUltimoDiaDeFebreroDelAnioSiguiente()
    {
        var issuer = CrearIssuer(new DateTimeOffset(2028, 2, 29, 10, 0, 0, TimeSpan.Zero));

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal(new DateTimeOffset(2029, 2, 28, 10, 0, 0, TimeSpan.Zero), vc.ValidUntil);
    }

    [Fact]
    public async Task IssueAsync_FijaProofCreatedIgualAValidFrom()
    {
        var issuer = CrearIssuer(new DateTimeOffset(2026, 8, 9, 14, 32, 10, TimeSpan.Zero));

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal(vc.ValidFrom, vc.Proof.Created);
    }

    [Theory]
    [InlineData("https://credenciales.futbol.com.ar/")]
    [InlineData("https://credenciales.futbol.com.ar")]
    public async Task IssueAsync_ConLaBaseConOSinBarraFinal_ArmaElIdBajoElDominioDelIssuer(string baseUri)
    {
        var issuer = CrearIssuer(baseUri: baseUri);

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.StartsWith(_baseUri, vc.Id, StringComparison.Ordinal);
        Assert.True(Guid.TryParse(vc.Id[_baseUri.Length..], out _));
    }

    [Fact]
    public async Task IssueAsync_ConDosEmisiones_GeneraIdsDistintos()
    {
        var issuer = CrearIssuer();

        var primera = await issuer.IssueAsync(_subject, _types);
        var segunda = await issuer.IssueAsync(_subject, _types);

        Assert.NotEqual(primera.Id, segunda.Id);
    }

    [Fact]
    public async Task IssueAsync_ArmaElProofConElAlgoritmoYElVerificationMethod()
    {
        var issuer = CrearIssuer();

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal("HMAC-SHA256", vc.Proof.Type);
        Assert.Equal("did:example:futbol#key-1", vc.Proof.VerificationMethod);
    }

    [Fact]
    public async Task IssueAsync_DevuelveElEmisorLosTiposYElSujetoRecibidos()
    {
        var issuer = CrearIssuer();

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal("did:example:futbol", vc.Issuer);
        Assert.Equal(_types, vc.Type);
        Assert.Equal(_subject, vc.CredentialSubject);
    }

    [Fact]
    public async Task IssueAsync_SinIndicarEstado_EmiteActiva()
    {
        var issuer = CrearIssuer();

        var vc = await issuer.IssueAsync(_subject, _types);

        Assert.Equal(CredentialStatus.Active, vc.CredentialStatus);
    }

    [Fact]
    public async Task IssueAsync_ConUnEstadoExplicito_LoRespeta()
    {
        var issuer = CrearIssuer();

        var vc = await issuer.IssueAsync(_subject, _types, CredentialStatus.Suspended);

        Assert.Equal(CredentialStatus.Suspended, vc.CredentialStatus);
    }

    [Fact]
    public async Task IssueAsync_ElProofValueSeVerificaRecalculandoElHmacSobreLaCredencialDevuelta()
    {
        var issuer = CrearIssuer();

        var vc = await issuer.IssueAsync(_subject, _types);

        // Lo que haría quien verifica: reconstruir el documento sin proof, canonicalizarlo y recalcular.
        var payload = new CredentialPayload(
            vc.Id, vc.Type, vc.Issuer, vc.ValidFrom, vc.ValidUntil, vc.CredentialStatus, vc.CredentialSubject);
        var esperado = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(_signingKey), payload.ToCanonicalBytes()));
        Assert.Equal(esperado, vc.Proof.ProofValue);
    }

    [Fact]
    public async Task IssueAsync_SiElLlamadorModificaElSujetoDespues_LaCredencialDevueltaNoCambia()
    {
        var issuer = CrearIssuer();
        var subject = new Dictionary<string, string>(_subject);

        var vc = await issuer.IssueAsync(subject, _types);
        subject["nombre"] = "Otro";

        Assert.Equal("Juan", vc.CredentialSubject["nombre"]);
    }

    [Fact]
    public async Task IssueAsync_ConUnEstadoInexistente_LanzaArgumentException()
    {
        var issuer = CrearIssuer();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => issuer.IssueAsync(_subject, _types, (CredentialStatus)99));
    }

    [Fact]
    public async Task IssueAsync_ConUnClaimSinValor_LanzaArgumentException()
    {
        var issuer = CrearIssuer();
        var subject = new Dictionary<string, string> { ["nombre"] = null! };

        await Assert.ThrowsAsync<ArgumentException>(() => issuer.IssueAsync(subject, _types));
    }

    [Fact]
    public async Task IssueAsync_ConUnSujetoNulo_LanzaArgumentNullException()
    {
        var issuer = CrearIssuer();

        await Assert.ThrowsAsync<ArgumentNullException>(() => issuer.IssueAsync(null!, _types));
    }

    [Fact]
    public async Task IssueAsync_ConUnTokenCancelado_LanzaOperationCanceledException()
    {
        var issuer = CrearIssuer();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => issuer.IssueAsync(_subject, _types, ct: cts.Token));
    }

    private static CredentialIssuer CrearIssuer(DateTimeOffset? ahora = null, string baseUri = _baseUri)
    {
        var options = new IssuerOptions
        {
            Did = "did:example:futbol",
            SigningKey = _signingKey,
            KeyId = "key-1",
            CredentialBaseUri = baseUri
        };
        var reloj = new RelojFijo(ahora ?? new DateTimeOffset(2026, 8, 9, 14, 32, 10, TimeSpan.Zero));
        return new CredentialIssuer(options, reloj);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
