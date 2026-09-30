using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wallet.Issuer;

namespace Wallet.UnitTests.Issuer;

public class IssuerServiceCollectionExtensionsTests
{
    private static readonly string[] _types = ["VerifiableCredential", "SocioCredential"];

    [Fact]
    public async Task AddIssuer_ConLaConfiguracionCompleta_PermiteEmitirUnaCredencial()
    {
        using var provider = CrearProvider(ConfiguracionValida());
        var issuer = provider.GetRequiredService<ICredentialIssuer>();

        var vc = await issuer.IssueAsync(new Dictionary<string, string> { ["nombre"] = "Juan" }, _types);

        Assert.Equal("did:example:futbol", vc.Issuer);
        Assert.Equal("did:example:futbol#key-1", vc.Proof.VerificationMethod);
        Assert.StartsWith("https://credenciales.futbol.com.ar/", vc.Id, StringComparison.Ordinal);
    }

    [Fact]
    public void AddIssuer_ConUnaClaveCorta_FallaAlResolverElIssuerConUnMensajeClaro()
    {
        var configuracion = ConfiguracionValida();
        configuracion["Issuer:SigningKey"] = "corta";
        using var provider = CrearProvider(configuracion);

        var error = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<ICredentialIssuer>());

        Assert.Contains("Issuer__SigningKey", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddIssuer_SinConfiguracion_FallaAlResolverElIssuer()
    {
        using var provider = CrearProvider([]);

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<ICredentialIssuer>());
    }

    [Fact]
    public void AddIssuer_RegistraElIssuerComoSingleton()
    {
        using var provider = CrearProvider(ConfiguracionValida());

        Assert.Same(
            provider.GetRequiredService<ICredentialIssuer>(),
            provider.GetRequiredService<ICredentialIssuer>());
    }

    private static Dictionary<string, string?> ConfiguracionValida() =>
        new()
        {
            ["Issuer:Did"] = "did:example:futbol",
            ["Issuer:SigningKey"] = "clave-de-prueba-de-al-menos-32-bytes!!",
            ["Issuer:KeyId"] = "key-1",
            ["Issuer:CredentialBaseUri"] = "https://credenciales.futbol.com.ar/"
        };

    private static ServiceProvider CrearProvider(Dictionary<string, string?> valores)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new ServiceCollection().AddIssuer(configuration).BuildServiceProvider();
    }
}
