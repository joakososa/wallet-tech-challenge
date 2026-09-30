using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Wallet.Issuer;
using Wallet.Tenant;

namespace Wallet.Api.Errors;

/// <summary>Traduce las excepciones del dominio y de la infraestructura a ProblemDetails con un <c>code</c> estable (ADR 007 y 016).</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            IssuerSigningException => (StatusCodes.Status500InternalServerError, "issuer_signing_failed", "No se pudo firmar la credencial."),
            DuplicateDniException => (StatusCodes.Status409Conflict, "duplicate_dni", "Otra alta del mismo DNI se procesó al mismo tiempo. Reintentá."),
            _ when IsDatabaseUnavailable(exception) => (StatusCodes.Status503ServiceUnavailable, "database_unavailable", "La base de datos no está disponible."),
            _ => (StatusCodes.Status500InternalServerError, string.Empty, "Ocurrió un error inesperado.")
        };

        var problem = new ProblemDetails { Status = status, Title = title };
        if (code.Length > 0)
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }
}
