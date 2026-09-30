using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wallet.Infrastructure;

namespace Wallet.IntegrationTests;

/// <summary>Arranca la API real contra un PostgreSQL de verdad (Testcontainers). Las migraciones corren al iniciar (ADR 017).</summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string SigningKey = "clave-de-integracion-de-al-menos-32-bytes!!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // La configuración va por variables de entorno, como en producción; la API las lee al construir el host.
        Environment.SetEnvironmentVariable("ConnectionStrings__Wallet", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Issuer__Did", "did:example:futbol");
        Environment.SetEnvironmentVariable("Issuer__KeyId", "key-1");
        Environment.SetEnvironmentVariable("Issuer__CredentialBaseUri", "https://credenciales.futbol.com.ar/");
        Environment.SetEnvironmentVariable("Issuer__SigningKey", SigningKey);

        Factory = new WebApplicationFactory<Program>();
    }

    /// <summary>Deja las tablas vacías y la secuencia en 1, para que cada test parta de cero.</summary>
    public async Task ResetAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE credentials, socios; ALTER SEQUENCE numero_socio_seq RESTART WITH 1;");
    }

    public async Task<(int Socios, int Credentials)> CountRowsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        return (await db.Socios.CountAsync(), await db.Credentials.CountAsync());
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
