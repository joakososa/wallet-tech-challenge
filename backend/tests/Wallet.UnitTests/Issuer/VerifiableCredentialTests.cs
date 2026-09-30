using System.Text;
using System.Text.Json;
using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class VerifiableCredentialTests
{
    // Canónico del enunciado (sección 4.1.2) sin la llave de cierre, más el proof de ejemplo.
    private const string _enunciadoCanonicalWithoutClosingBrace =
        """
        {"credentialStatus":0,"credentialSubject":{"apellido":"Pérez","categoria":"adulto","dni":"30123456","foto":"https://cdn.futbol.com.ar/socios/8f14e45f.jpg","id":"did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6","nombre":"Juan","numeroSocio":"000123"},"id":"https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae","issuer":"did:example:futbol","type":["VerifiableCredential","SocioCredential"],"validFrom":"2026-08-09T14:32:10Z","validUntil":"2027-08-09T14:32:10Z"
        """;

    private const string _enunciadoProofJson =
        """
        ,"proof":{"type":"HMAC-SHA256","created":"2026-08-09T14:32:10Z","verificationMethod":"did:example:futbol#key-1","proofValue":"b8f3a1e9c02d4f7a6e1b9c3d8f2a5e7b1c4d6f9a0e3b7c1d5f8a2e6b9c0d3f7a"}}
        """;

    private static readonly DateTimeOffset _validFrom = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);

    [Fact]
    public void ToJson_ConLaCredencialDelEnunciado_ProduceElCanonicoDelEnunciadoMasElProof()
    {
        var credential = CrearCredencial();

        var json = credential.ToJson();

        Assert.Equal(_enunciadoCanonicalWithoutClosingBrace + _enunciadoProofJson, json);
    }

    [Fact]
    public void ToJson_SinElProof_EsExactamenteElTextoFirmado()
    {
        var credential = CrearCredencial();
        var payload = new CredentialPayload(
            credential.Id,
            credential.Type,
            credential.Issuer,
            credential.ValidFrom,
            credential.ValidUntil,
            credential.CredentialStatus,
            credential.CredentialSubject);

        var json = credential.ToJson();
        var withoutProof = json[..json.IndexOf(",\"proof\":", StringComparison.Ordinal)] + "}";

        Assert.Equal(Encoding.UTF8.GetString(payload.ToCanonicalBytes()), withoutProof);
    }

    [Fact]
    public void ToJson_ConFechasEnOtroOffset_LasEscribeEnUtcConSufijoZ()
    {
        var conOffset = new DateTimeOffset(2026, 8, 9, 11, 32, 10, TimeSpan.FromHours(-3));
        var credential = CrearCredencial() with
        {
            ValidFrom = conOffset,
            ValidUntil = conOffset.AddYears(1),
            Proof = CrearCredencial().Proof with { Created = conOffset }
        };

        var json = credential.ToJson();

        Assert.Contains("\"validFrom\":\"2026-08-09T14:32:10Z\"", json);
        Assert.Contains("\"created\":\"2026-08-09T14:32:10Z\"", json);
    }

    [Fact]
    public void ToJson_ConNoAscii_NoLoEscapa()
    {
        var credential = CrearCredencial() with
        {
            CredentialSubject = new Dictionary<string, string> { ["nombre"] = "niño" }
        };

        var json = credential.ToJson();

        Assert.Contains("\"nombre\":\"niño\"", json);
    }

    [Fact]
    public void ToJson_ConProof_EsUnJsonValidoConProofComoUltimoCampo()
    {
        var json = CrearCredencial().ToJson();

        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Equal(
            ["credentialStatus", "credentialSubject", "id", "issuer", "type", "validFrom", "validUntil", "proof"],
            names);
    }

    private static VerifiableCredential CrearCredencial() =>
        new(
            Id: "https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae",
            Type: ["VerifiableCredential", "SocioCredential"],
            Issuer: "did:example:futbol",
            ValidFrom: _validFrom,
            ValidUntil: _validFrom.AddYears(1),
            CredentialStatus: CredentialStatus.Active,
            CredentialSubject: new Dictionary<string, string>
            {
                ["numeroSocio"] = "000123",
                ["nombre"] = "Juan",
                ["id"] = "did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6",
                ["foto"] = "https://cdn.futbol.com.ar/socios/8f14e45f.jpg",
                ["dni"] = "30123456",
                ["categoria"] = "adulto",
                ["apellido"] = "Pérez"
            },
            Proof: new Proof(
                Type: "HMAC-SHA256",
                Created: _validFrom,
                VerificationMethod: "did:example:futbol#key-1",
                ProofValue: "b8f3a1e9c02d4f7a6e1b9c3d8f2a5e7b1c4d6f9a0e3b7c1d5f8a2e6b9c0d3f7a"));
}
