namespace Wallet.Api.Contracts;

/// <summary>Cuerpo del alta. Todo es opcional a nivel de binding: la validación (ADR 007) informa cada campo por separado.</summary>
public sealed record IssueCredentialRequest(
    string? Nombre,
    string? Apellido,
    string? Dni,
    string? Categoria,
    string? Foto);
