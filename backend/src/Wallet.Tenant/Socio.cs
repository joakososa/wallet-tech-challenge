namespace Wallet.Tenant;

/// <summary>El estado actual del socio. Su identidad es el DNI, que no cambia (ADR 006).</summary>
public sealed class Socio(
    Guid id,
    string subjectDid,
    long numeroSocio,
    string dni,
    string nombre,
    string apellido,
    Categoria categoria,
    string foto,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt)
{
    public Guid Id { get; } = id;

    /// <summary><c>credentialSubject.id</c>: se genera una sola vez.</summary>
    public string SubjectDid { get; } = subjectDid;

    public long NumeroSocio { get; } = numeroSocio;

    public string Dni { get; } = dni;

    public string Nombre { get; private set; } = nombre;

    public string Apellido { get; private set; } = apellido;

    public Categoria Categoria { get; private set; } = categoria;

    public string Foto { get; private set; } = foto;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;

    /// <summary>Actualiza los datos que pueden cambiar; gana la última escritura (ADR 006).</summary>
    public void Update(string nombre, string apellido, Categoria categoria, string foto, DateTimeOffset now)
    {
        Nombre = nombre;
        Apellido = apellido;
        Categoria = categoria;
        Foto = foto;
        UpdatedAt = now;
    }
}
