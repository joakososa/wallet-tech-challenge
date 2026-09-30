using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Wallet.Issuer;

public static class IssuerServiceCollectionExtensions
{
    private const string _sectionName = "Issuer";

    /// <summary>
    /// Registra el Issuer con la configuración de la sección <c>Issuer</c> (variables <c>Issuer__*</c>),
    /// validada al arrancar.
    /// </summary>
    public static IServiceCollection AddIssuer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IssuerOptions>()
            .Bind(configuration.GetSection(_sectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<IssuerOptions>, IssuerOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICredentialIssuer>(provider => new CredentialIssuer(
            provider.GetRequiredService<IOptions<IssuerOptions>>().Value,
            provider.GetRequiredService<TimeProvider>()));

        return services;
    }
}
