using System.Text;
using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class HmacSha256SignerTests
{
    [Fact]
    public void Sign_ConElVectorDePruebaDeLaRfc4231_ProduceElHmacEsperado()
    {
        // RFC 4231, caso de prueba 2. La respuesta correcta viene de la especificación.
        var signer = new HmacSha256Signer("Jefe");
        var data = Encoding.UTF8.GetBytes("what do ya want for nothing?");
        var expected = Convert.FromHexString("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843");

        var proofValue = signer.Sign(data);

        Assert.Equal(expected, Convert.FromBase64String(proofValue));
    }

    [Fact]
    public void Sign_ConElJsonCanonicoDelEnunciado_ProduceElHmacCalculadoConOpenssl()
    {
        // Valor calculado afuera del código:
        //   printf %s "$JSON" | openssl dgst -sha256 -hmac "$CLAVE" -binary | openssl base64 -A
        const string json =
            """{"credentialStatus":0,"credentialSubject":{"apellido":"Pérez","categoria":"adulto","dni":"30123456","foto":"https://cdn.futbol.com.ar/socios/8f14e45f.jpg","id":"did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6","nombre":"Juan","numeroSocio":"000123"},"id":"https://credenciales.futbol.com.ar/8f14e45f-ceea-467e-9de1-93f5a5f4bfae","issuer":"did:example:futbol","type":["VerifiableCredential","SocioCredential"],"validFrom":"2026-08-09T14:32:10Z","validUntil":"2027-08-09T14:32:10Z"}""";
        var signer = new HmacSha256Signer("clave-de-prueba-de-al-menos-32-bytes!!");

        var proofValue = signer.Sign(Encoding.UTF8.GetBytes(json));

        Assert.Equal("AIBeuvJceW9EyHfaxV6hmAJwKrzNCljeOGDh532Md4M=", proofValue);
    }

    [Fact]
    public void Sign_ProduceBase64DeUnHmacDe32Bytes()
    {
        var signer = new HmacSha256Signer("clave-de-prueba-de-al-menos-32-bytes!!");

        var proofValue = signer.Sign(Encoding.UTF8.GetBytes("cualquier contenido"));

        Assert.Equal(44, proofValue.Length);
        Assert.EndsWith("=", proofValue, StringComparison.Ordinal);
        Assert.Equal(32, Convert.FromBase64String(proofValue).Length);
    }

    [Fact]
    public void Sign_ConLosMismosDatos_ProduceLaMismaFirma()
    {
        var signer = new HmacSha256Signer("clave-de-prueba-de-al-menos-32-bytes!!");
        var data = Encoding.UTF8.GetBytes("contenido");

        Assert.Equal(signer.Sign(data), signer.Sign(data));
    }

    [Fact]
    public void Sign_ConOtraClave_ProduceOtraFirma()
    {
        var data = Encoding.UTF8.GetBytes("contenido");

        var conUnaClave = new HmacSha256Signer("clave-de-prueba-de-al-menos-32-bytes!!").Sign(data);
        var conOtraClave = new HmacSha256Signer("otra-clave-de-prueba-de-al-menos-32-bytes").Sign(data);

        Assert.NotEqual(conUnaClave, conOtraClave);
    }

    [Fact]
    public void Sign_ConUnCaracterDistinto_ProduceOtraFirma()
    {
        var signer = new HmacSha256Signer("clave-de-prueba-de-al-menos-32-bytes!!");

        var original = signer.Sign(Encoding.UTF8.GetBytes("Perez"));
        var alterado = signer.Sign(Encoding.UTF8.GetBytes("Pérez"));

        Assert.NotEqual(original, alterado);
    }
}
