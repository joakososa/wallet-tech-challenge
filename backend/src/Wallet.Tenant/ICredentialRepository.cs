namespace Wallet.Tenant;

public interface ICredentialRepository
{
    /// <summary>
    /// Guarda el socio y la credencial de forma atómica, en una transacción corta (ADR 003).
    /// Un socio devuelto por <see cref="ISocioRepository.FindByDniAsync"/> se actualiza; uno nuevo se inserta.
    /// </summary>
    /// <exception cref="DuplicateDniException">Si otro alta insertó el mismo DNI mientras tanto.</exception>
    Task AddAsync(Socio socio, Credential credential, CancellationToken ct = default);

    /// <summary>Lista las credenciales del tenant, de la más nueva a la más vieja.</summary>
    Task<IReadOnlyList<CredentialSummary>> ListAsync(CredentialFilter filter, CancellationToken ct = default);
}
