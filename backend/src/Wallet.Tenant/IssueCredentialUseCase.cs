using Wallet.Issuer;

namespace Wallet.Tenant;

/// <summary>UC01: alta de la credencial de un socio (ADR 003 y 015).</summary>
public sealed class IssueCredentialUseCase(
    ISocioRepository socios,
    ICredentialRepository credentials,
    ICredentialIssuer issuer,
    TimeProvider time,
    TenantOptions options)
{
    private static readonly string[] _types = ["VerifiableCredential", "SocioCredential"];

    /// <exception cref="ArgumentException">Si el DNI no es válido (la API lo valida antes con <see cref="Dni.TryNormalize"/>).</exception>
    /// <exception cref="IssuerSigningException">Si el Issuer no puede firmar: no se persiste ni se modifica nada.</exception>
    public async Task<IssueCredentialResult> HandleAsync(IssueCredentialCommand command, CancellationToken ct = default)
    {
        if (!Dni.TryNormalize(command.Dni, out var dni))
        {
            throw new ArgumentException("El DNI no es válido.", nameof(command));
        }

        try
        {
            return await IssueAsync(command, dni, ct);
        }
        catch (DuplicateDniException)
        {
            // Otra alta insertó este DNI entre la búsqueda y el commit. La VC ya firmada llevaba un DID y un número
            // que no van a existir: se repite todo el flujo, una sola vez, con los datos del socio ganador.
            return await IssueAsync(command, dni, ct);
        }
    }

    private async Task<IssueCredentialResult> IssueAsync(IssueCredentialCommand command, string dni, CancellationToken ct)
    {
        var existing = await socios.FindByDniAsync(dni, ct);
        var socioId = existing?.Id ?? Guid.NewGuid();
        var subjectDid = existing?.SubjectDid ?? $"did:example:{socioId}";
        var numeroSocio = existing?.NumeroSocio ?? await socios.NextNumeroSocioAsync(ct);

        var claims = new Dictionary<string, string>
        {
            ["id"] = subjectDid,
            ["nombre"] = command.Nombre,
            ["apellido"] = command.Apellido,
            ["dni"] = dni,
            ["numeroSocio"] = NumeroSocio.Format(numeroSocio),
            ["categoria"] = command.Categoria.ToText(),
            ["foto"] = command.Foto
        };

        // Si la firma falla, la excepción sube y todavía no se tocó ni el socio ni la base (UC01 5a).
        var vc = await issuer.IssueAsync(claims, _types, ct: ct);

        var now = time.GetUtcNow();
        Socio socio;
        if (existing is null)
        {
            socio = new Socio(socioId, subjectDid, numeroSocio, dni, command.Nombre, command.Apellido, command.Categoria, command.Foto, now, now);
        }
        else
        {
            existing.Update(command.Nombre, command.Apellido, command.Categoria, command.Foto, now);
            socio = existing;
        }

        var credential = Credential.FromIssued(vc, socio, options.Id, now);
        await credentials.AddAsync(socio, credential, ct);

        return new IssueCredentialResult(vc, credential.Document, NumeroSocio.Format(numeroSocio), existing is null);
    }
}
