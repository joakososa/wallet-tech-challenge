using Wallet.Issuer;

namespace Wallet.Tenant;

/// <summary>Snapshot inmutable de lo que se emitió, con la VC completa en <see cref="Document"/> (ADR 002).</summary>
public sealed class Credential(
    Guid id,
    string vcId,
    Guid socioId,
    string tenantId,
    string nombre,
    string apellido,
    string dni,
    long numeroSocio,
    Categoria categoria,
    string foto,
    DateTimeOffset validFrom,
    DateTimeOffset validUntil,
    CredentialStatus status,
    string document,
    DateTimeOffset createdAt)
{
    /// <summary>El <c>Guid</c> de la VC.</summary>
    public Guid Id { get; } = id;

    /// <summary>La URI de la VC (<c>id</c> del documento).</summary>
    public string VcId { get; } = vcId;

    public Guid SocioId { get; } = socioId;

    public string TenantId { get; } = tenantId;

    public string Nombre { get; } = nombre;

    public string Apellido { get; } = apellido;

    public string Dni { get; } = dni;

    public long NumeroSocio { get; } = numeroSocio;

    public Categoria Categoria { get; } = categoria;

    public string Foto { get; } = foto;

    public DateTimeOffset ValidFrom { get; } = validFrom;

    public DateTimeOffset ValidUntil { get; } = validUntil;

    /// <summary>Estado actual. El del documento firmado no se muta (ADR 002).</summary>
    public CredentialStatus Status { get; private set; } = status;

    /// <summary>La VC completa tal como se emitió (<see cref="VerifiableCredential.ToJson"/>).</summary>
    public string Document { get; } = document;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    /// <summary>Arma el snapshot de una VC recién emitida para <paramref name="socio"/>, ya actualizado.</summary>
    public static Credential FromIssued(
        VerifiableCredential vc,
        Socio socio,
        string tenantId,
        DateTimeOffset createdAt) =>
        new(
            id: Guid.Parse(vc.Id[(vc.Id.LastIndexOf('/') + 1)..]),
            vcId: vc.Id,
            socioId: socio.Id,
            tenantId: tenantId,
            nombre: socio.Nombre,
            apellido: socio.Apellido,
            dni: socio.Dni,
            numeroSocio: socio.NumeroSocio,
            categoria: socio.Categoria,
            foto: socio.Foto,
            validFrom: vc.ValidFrom,
            validUntil: vc.ValidUntil,
            status: vc.CredentialStatus,
            document: vc.ToJson(),
            createdAt: createdAt);
}
