using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Contracts;
using Wallet.Api.Errors;
using Wallet.Api.Health;
using Wallet.Api.Simulation;
using Wallet.Infrastructure;
using Wallet.Issuer;
using Wallet.Tenant;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // No escapar tildes ni la ñ: lo devuelto tiene que ser el documento que se firmó (ADR 004 y 016).
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddIssuer(builder.Configuration);
builder.Services.AddTenant(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Solo para demostrar UC01 5a a mano (ADR 012): en Development y con Issuer__SimulateFailure=true.
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Issuer:SimulateFailure"))
{
    builder.Services.AddSingleton<ICredentialIssuer, SimulatedFailureCredentialIssuer>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Aplica las migraciones pendientes al arrancar, para que "docker compose up" y "dotnet run" funcionen sin pasos extra (ADR 017).
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<WalletDbContext>().Database.MigrateAsync();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Permite que los tests de integración arranquen la aplicación con <c>WebApplicationFactory</c>.</summary>
public partial class Program;
