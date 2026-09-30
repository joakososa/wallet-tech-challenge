using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Tenant;

namespace Wallet.Infrastructure;

internal sealed class CredentialRepository(WalletDbContext db) : ICredentialRepository
{
    public async Task AddAsync(Socio socio, Credential credential, CancellationToken ct = default)
    {
        // Un socio traído por SocioRepository ya está rastreado y se actualiza; uno nuevo se inserta.
        if (db.Entry(socio).State == EntityState.Detached)
        {
            db.Socios.Add(socio);
        }

        db.Credentials.Add(credential);

        try
        {
            // SaveChanges abre y cierra una única transacción: es la transacción corta del ADR 003.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Sin esto, el reintento por DuplicateDniException volvería a insertar las entidades del primer intento.
            db.ChangeTracker.Clear();

            if (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
                && pg.ConstraintName == WalletDbContext.SocioDniIndex)
            {
                throw new DuplicateDniException(socio.Dni, ex);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<CredentialSummary>> ListAsync(CredentialFilter filter, CancellationToken ct = default)
    {
        var query = db.Credentials.AsNoTracking().Where(c => c.TenantId == filter.TenantId);

        if (filter.Dni is not null)
        {
            query = query.Where(c => c.Dni == filter.Dni);
        }

        if (filter.NumeroSocio is not null)
        {
            query = query.Where(c => c.NumeroSocio == filter.NumeroSocio);
        }

        return await query
            .OrderByDescending(c => c.ValidFrom)
            .Select(c => new CredentialSummary(
                c.Id, c.VcId, c.Nombre, c.Apellido, c.Dni, c.NumeroSocio, c.Categoria, c.Foto,
                c.ValidFrom, c.ValidUntil, c.Status, c.Document))
            .ToListAsync(ct);
    }
}
