namespace Wallet.Tenant;

public sealed record IssueCredentialCommand(
    string Nombre,
    string Apellido,
    string Dni,
    Categoria Categoria,
    string Foto);
