using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wallet.Infrastructure;

/// <summary>Solo lo usa <c>dotnet ef</c> para generar migraciones; no se conecta a ninguna base.</summary>
internal sealed class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WalletDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time")
            .Options);
}
