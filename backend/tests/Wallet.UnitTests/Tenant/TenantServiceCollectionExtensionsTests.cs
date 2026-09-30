using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Issuer;
using Wallet.Tenant;
using Wallet.UnitTests.Tenant.Fakes;

namespace Wallet.UnitTests.Tenant;

public class TenantServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTenant_SinConfiguracion_UsaElTenantPorDefecto()
    {
        using var provider = CrearProvider([]);

        Assert.Equal("club-futbol", provider.GetRequiredService<TenantOptions>().Id);
    }

    [Fact]
    public void AddTenant_ConTenantId_LoTomaDeLaConfiguracion()
    {
        using var provider = CrearProvider(new Dictionary<string, string?> { ["Tenant:Id"] = "otro-club" });

        Assert.Equal("otro-club", provider.GetRequiredService<TenantOptions>().Id);
    }

    [Fact]
    public void AddTenant_ConLosPuertosRegistrados_ResuelveLosCasosDeUso()
    {
        using var provider = CrearProvider([]);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IssueCredentialUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ListCredentialsUseCase>());
    }

    private static ServiceProvider CrearProvider(Dictionary<string, string?> valores)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var store = new InMemoryStore();

        var services = new ServiceCollection();
        services.AddSingleton<ISocioRepository>(store);
        services.AddSingleton<ICredentialRepository>(store);
        services.AddSingleton<ICredentialIssuer>(new FakeCredentialIssuer(DateTimeOffset.UnixEpoch));
        services.AddSingleton(TimeProvider.System);
        services.AddTenant(configuration);

        return services.BuildServiceProvider();
    }
}
