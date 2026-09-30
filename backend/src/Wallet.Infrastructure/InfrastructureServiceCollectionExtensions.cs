using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Tenant;

namespace Wallet.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    private const string _connectionName = "Wallet";
    private const int _commandTimeoutSeconds = 10;

    /// <summary>
    /// Registra el <see cref="WalletDbContext"/> y los repositorios. La cadena de conexión sale de
    /// <c>ConnectionStrings__Wallet</c>; el timeout de conexión se fija ahí (<c>Timeout=5</c>) y el de comando acá.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(_connectionName)
            ?? throw new InvalidOperationException("Falta la cadena de conexión ConnectionStrings__Wallet.");

        services.AddDbContext<WalletDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(_commandTimeoutSeconds)));

        services.AddScoped<ISocioRepository, SocioRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();

        return services;
    }
}
