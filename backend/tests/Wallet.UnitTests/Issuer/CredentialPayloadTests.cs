using System.Text;
using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class CredentialPayloadTests
{
    // JSON canónico de ejemplo del enunciado (sección 4.1.2). La respuesta correcta viene de afuera.
    private const string _enunciadoCanonicalJson =
        """{"credentialStatus":0,"credentialSubject":{"apellido":"Pérez","categoria":"adulto","dni":"30123456","foto":"https://cdn.futbol.com.ar/socios/8f14e45f.jpg","id":"did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6","nombre":"Juan","numeroSocio":"000123"},"id":"https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae","issuer":"did:example:futbol","type":["VerifiableCredential","SocioCredential"],"validFrom":"2026-08-09T14:32:10Z","validUntil":"2027-08-09T14:32:10Z"}""";

    private static readonly DateTimeOffset _validFrom = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);

    [Fact]
    public void ToCanonicalBytes_ConLaCredencialDelEnunciado_ProduceElJsonCanonicoDelEnunciado()
    {
        // Los claims se cargan desordenados a propósito: el orden lo pone el canonicalizador.
        var subject = new Dictionary<string, string>
        {
            ["numeroSocio"] = "000123",
            ["nombre"] = "Juan",
            ["id"] = "did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6",
            ["foto"] = "https://cdn.futbol.com.ar/socios/8f14e45f.jpg",
            ["dni"] = "30123456",
            ["categoria"] = "adulto",
            ["apellido"] = "Pérez"
        };
        var payload = CrearPayload(subject);

        var canonical = payload.ToCanonicalBytes();

        Assert.Equal(_enunciadoCanonicalJson, Encoding.UTF8.GetString(canonical));
    }

    [Fact]
    public void ToCanonicalBytes_ConClaimsEnOtroOrden_ProduceLosMismosBytes()
    {
        var enUnOrden = CrearPayload(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" });
        var enOtroOrden = CrearPayload(new Dictionary<string, string> { ["c"] = "3", ["a"] = "1", ["b"] = "2" });

        Assert.Equal(enUnOrden.ToCanonicalBytes(), enOtroOrden.ToCanonicalBytes());
    }

    [Fact]
    public void ToCanonicalBytes_ConMayusculasEnLasClaves_LasOrdenaDeFormaOrdinal()
    {
        var payload = CrearPayload(new Dictionary<string, string> { ["a"] = "1", ["Z"] = "2" });

        var json = Encoding.UTF8.GetString(payload.ToCanonicalBytes());

        Assert.Contains("""{"Z":"2","a":"1"}""", json);
    }

    [Theory]
    [InlineData("niño")]
    [InlineData("Ñandú")]
    [InlineData("O'Connor")]
    [InlineData("a&b")]
    [InlineData("a+b")]
    [InlineData("<b>")]
    public void ToCanonicalBytes_ConCaracteresQueElEncoderPorDefectoEscapa_LosEscribeSinEscapar(string valor)
    {
        var payload = CrearPayload(new Dictionary<string, string> { ["nombre"] = valor });

        var json = Encoding.UTF8.GetString(payload.ToCanonicalBytes());

        Assert.Contains($"\"nombre\":\"{valor}\"", json);
    }

    [Fact]
    public void ToCanonicalBytes_ConComillasEnUnValor_LasEscapaParaQueElJsonSeaValido()
    {
        var payload = CrearPayload(new Dictionary<string, string> { ["nombre"] = "a\"b" });

        var json = Encoding.UTF8.GetString(payload.ToCanonicalBytes());

        Assert.Contains("""{"nombre":"a\"b"}""", json);
    }

    [Theory]
    [InlineData(CredentialStatus.Active, 0)]
    [InlineData(CredentialStatus.Revoked, 1)]
    [InlineData(CredentialStatus.Suspended, 2)]
    public void ToCanonicalBytes_ConUnEstado_LoEscribeComoNumero(CredentialStatus status, int esperado)
    {
        var payload = CrearPayload(new Dictionary<string, string>()) with { CredentialStatus = status };

        var json = Encoding.UTF8.GetString(payload.ToCanonicalBytes());

        Assert.StartsWith($"{{\"credentialStatus\":{esperado},", json);
    }

    [Fact]
    public void ToCanonicalBytes_ConFechasEnOtroOffset_LasEscribeEnUtcConSufijoZ()
    {
        var conOffset = new DateTimeOffset(2026, 8, 9, 11, 32, 10, TimeSpan.FromHours(-3));
        var payload = CrearPayload(new Dictionary<string, string>()) with
        {
            ValidFrom = conOffset,
            ValidUntil = conOffset.AddYears(1)
        };

        var json = Encoding.UTF8.GetString(payload.ToCanonicalBytes());

        Assert.Contains("""
            "validFrom":"2026-08-09T14:32:10Z","validUntil":"2027-08-09T14:32:10Z"
            """.Trim(), json);
    }

    private static CredentialPayload CrearPayload(IReadOnlyDictionary<string, string> subject) =>
        new(
            Id: "https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae",
            Types: ["VerifiableCredential", "SocioCredential"],
            Issuer: "did:example:futbol",
            ValidFrom: _validFrom,
            ValidUntil: _validFrom.AddYears(1),
            CredentialStatus: CredentialStatus.Active,
            CredentialSubject: subject);
}
