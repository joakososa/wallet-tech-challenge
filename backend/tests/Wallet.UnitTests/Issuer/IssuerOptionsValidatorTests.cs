using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class IssuerOptionsValidatorTests
{
    private readonly IssuerOptionsValidator _validator = new();

    [Fact]
    public void Validate_ConLaConfiguracionCompleta_EsValida()
    {
        var result = _validator.Validate(null, CrearOpciones());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("demasiado-corta")]
    [InlineData("1234567890123456789012345678901")] // 31 bytes
    public void Validate_ConUnaClaveAusenteOCorta_FallaNombrandoLaVariable(string clave)
    {
        var result = _validator.Validate(null, CrearOpciones(signingKey: clave));

        Assert.True(result.Failed);
        Assert.Contains("Issuer__SigningKey", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ConUnaClaveDe32Bytes_EsValida()
    {
        var result = _validator.Validate(null, CrearOpciones(signingKey: new string('a', 32)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ConUnaClaveCuyosCaracteresSuperan32PeroNoLosBytes_CuentaBytes()
    {
        // 16 caracteres, 'ñ' ocupa 2 bytes en UTF-8: 32 bytes en total.
        var result = _validator.Validate(null, CrearOpciones(signingKey: new string('ñ', 16)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ConUnaClaveDe31BytesYMenosDe32Caracteres_Falla()
    {
        var result = _validator.Validate(null, CrearOpciones(signingKey: new string('ñ', 15) + "a"));

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("key#1")]
    [InlineData("key 1")]
    [InlineData("key\t1")]
    public void Validate_ConUnKeyIdInvalido_FallaNombrandoLaVariable(string keyId)
    {
        var result = _validator.Validate(null, CrearOpciones(keyId: keyId));

        Assert.True(result.Failed);
        Assert.Contains("Issuer__KeyId", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("example:futbol")]
    [InlineData("did:")]
    public void Validate_ConUnDidInvalido_FallaNombrandoLaVariable(string did)
    {
        var result = _validator.Validate(null, CrearOpciones(did: did));

        Assert.True(result.Failed);
        Assert.Contains("Issuer__Did", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("credenciales.futbol.com.ar")]
    [InlineData("/relativa/")]
    [InlineData("ftp://credenciales.futbol.com.ar/")]
    public void Validate_ConUnaBaseUriInvalida_FallaNombrandoLaVariable(string baseUri)
    {
        var result = _validator.Validate(null, CrearOpciones(baseUri: baseUri));

        Assert.True(result.Failed);
        Assert.Contains("Issuer__CredentialBaseUri", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ConVariosErrores_LosInformaTodosJuntos()
    {
        var result = _validator.Validate(null, CrearOpciones(signingKey: "corta", keyId: "a#b", did: "x"));

        Assert.Contains("Issuer__SigningKey", result.FailureMessage, StringComparison.Ordinal);
        Assert.Contains("Issuer__KeyId", result.FailureMessage, StringComparison.Ordinal);
        Assert.Contains("Issuer__Did", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ConUnaClaveInvalida_NoIncluyeElSecretoEnElMensaje()
    {
        var result = _validator.Validate(null, CrearOpciones(signingKey: "secreto-corto"));

        Assert.DoesNotContain("secreto-corto", result.FailureMessage, StringComparison.Ordinal);
    }

    private static IssuerOptions CrearOpciones(
        string signingKey = "clave-de-prueba-de-al-menos-32-bytes!!",
        string keyId = "key-1",
        string did = "did:example:futbol",
        string baseUri = "https://credenciales.futbol.com.ar/") =>
        new()
        {
            Did = did,
            SigningKey = signingKey,
            KeyId = keyId,
            CredentialBaseUri = baseUri
        };
}
