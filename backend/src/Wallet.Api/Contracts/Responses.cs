using System.Text.Json;
using Wallet.Issuer;
using Wallet.Tenant;

namespace Wallet.Api.Contracts;

/// <summary>Respuesta del alta (ADR 016). <paramref name="Credential"/> es la VC completa, incrustada como objeto JSON.</summary>
public sealed record IssueCredentialResponse(
    string NumeroSocio,
    bool IsNewSocio,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    JsonElement Credential)
{
    public static IssueCredentialResponse From(IssueCredentialResult result) =>
        new(result.NumeroSocio, result.IsNewSocio, result.Vc.ValidFrom, result.Vc.ValidUntil, ParseDocument(result.Document));

    internal static JsonElement ParseDocument(string document)
    {
        using var parsed = JsonDocument.Parse(document);
        return parsed.RootElement.Clone();
    }
}

/// <summary>Una fila del listado (ADR 016). <paramref name="Status"/> es el número del enunciado (0, 1 o 2).</summary>
public sealed record CredentialListItem(
    Guid Id,
    string Nombre,
    string Apellido,
    string Dni,
    string NumeroSocio,
    string Categoria,
    string Foto,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus Status,
    JsonElement Credential)
{
    public static CredentialListItem From(CredentialSummary summary) =>
        new(
            summary.Id,
            summary.Nombre,
            summary.Apellido,
            summary.Dni,
            Tenant.NumeroSocio.Format(summary.NumeroSocio),
            summary.Categoria.ToText(),
            summary.Foto,
            summary.ValidFrom,
            summary.ValidUntil,
            summary.Status,
            IssueCredentialResponse.ParseDocument(summary.Document));
}
