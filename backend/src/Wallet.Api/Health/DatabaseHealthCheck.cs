using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wallet.Infrastructure;

namespace Wallet.Api.Health;

public sealed class DatabaseHealthCheck(WalletDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("No hay conexión con la base de datos.");
}
