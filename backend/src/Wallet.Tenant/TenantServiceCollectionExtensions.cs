using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wallet.Tenant;

public static class TenantServiceCollectionExtensions
{
    private const string _sectionName = "Tenant";

    /// <summary>
    /// Registra los casos de uso y la configuración de la sección <c>Tenant</c> (<c>Tenant__Id</c>).
    /// El <see cref="TimeProvider"/> lo registra <c>AddIssuer()</c>.
    /// </summary>
    public static IServiceCollection AddTenant(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TenantOptions>().Bind(configuration.GetSection(_sectionName));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<TenantOptions>>().Value);

        services.AddScoped<IssueCredentialUseCase>();
        services.AddScoped<ListCredentialsUseCase>();

        return services;
    }
}
