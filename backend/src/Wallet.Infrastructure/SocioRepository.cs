using Microsoft.EntityFrameworkCore;
using Wallet.Tenant;

namespace Wallet.Infrastructure;

internal sealed class SocioRepository(WalletDbContext db) : ISocioRepository
{
    /// <remarks>El socio queda rastreado por el contexto: <see cref="CredentialRepository.AddAsync"/> lo actualiza en lugar de insertarlo.</remarks>
    public Task<Socio?> FindByDniAsync(string dni, CancellationToken ct = default) =>
        db.Socios.FirstOrDefaultAsync(s => s.Dni == dni, ct);

    /// <remarks><c>nextval</c> no participa de ninguna transacción: un rollback no devuelve el número (ADR 003).</remarks>
    public Task<long> NextNumeroSocioAsync(CancellationToken ct = default) =>
        db.Database
            .SqlQuery<long>($"SELECT nextval('numero_socio_seq') AS \"Value\"")
            .SingleAsync(ct);
}
