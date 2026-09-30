using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wallet.Tenant;

namespace Wallet.Infrastructure;

/// <summary>Mapeo de <c>socios</c> y <c>credentials</c> (ADR 002). Los nombres de tablas y columnas se fijan a mano, en snake_case.</summary>
public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    internal const string NumeroSocioSequence = "numero_socio_seq";
    internal const string SocioDniIndex = "ux_socios_dni";

    public DbSet<Socio> Socios => Set<Socio>();

    public DbSet<Credential> Credentials => Set<Credential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(NumeroSocioSequence).StartsAt(1).IncrementsBy(1);

        modelBuilder.Entity<Socio>(socio =>
        {
            socio.ToTable("socios");
            socio.HasKey(s => s.Id);
            socio.Property(s => s.Id).HasColumnName("id");
            socio.Property(s => s.SubjectDid).HasColumnName("subject_did").IsRequired();
            socio.Property(s => s.NumeroSocio).HasColumnName("numero_socio");
            socio.Property(s => s.Dni).HasColumnName("dni").IsRequired();
            socio.Property(s => s.Nombre).HasColumnName("nombre").IsRequired();
            socio.Property(s => s.Apellido).HasColumnName("apellido").IsRequired();
            socio.Property(s => s.Categoria).HasColumnName("categoria").HasConversion(CategoriaConverter());
            socio.Property(s => s.Foto).HasColumnName("foto").IsRequired();
            socio.Property(s => s.CreatedAt).HasColumnName("created_at");
            socio.Property(s => s.UpdatedAt).HasColumnName("updated_at");

            socio.HasIndex(s => s.Dni).IsUnique().HasDatabaseName(SocioDniIndex);
            socio.HasIndex(s => s.SubjectDid).IsUnique().HasDatabaseName("ux_socios_subject_did");
            socio.HasIndex(s => s.NumeroSocio).IsUnique().HasDatabaseName("ux_socios_numero_socio");
        });

        modelBuilder.Entity<Credential>(credential =>
        {
            credential.ToTable("credentials");
            credential.HasKey(c => c.Id);
            credential.Property(c => c.Id).HasColumnName("id");
            credential.Property(c => c.VcId).HasColumnName("vc_id").IsRequired();
            credential.Property(c => c.SocioId).HasColumnName("socio_id");
            credential.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
            credential.Property(c => c.Nombre).HasColumnName("nombre").IsRequired();
            credential.Property(c => c.Apellido).HasColumnName("apellido").IsRequired();
            credential.Property(c => c.Dni).HasColumnName("dni").IsRequired();
            credential.Property(c => c.NumeroSocio).HasColumnName("numero_socio");
            credential.Property(c => c.Categoria).HasColumnName("categoria").HasConversion(CategoriaConverter());
            credential.Property(c => c.Foto).HasColumnName("foto").IsRequired();
            credential.Property(c => c.ValidFrom).HasColumnName("valid_from");
            credential.Property(c => c.ValidUntil).HasColumnName("valid_until");
            credential.Property(c => c.Status).HasColumnName("status").HasConversion<short>();
            // json y no jsonb: conserva el texto exacto del documento firmado (ADR 002).
            credential.Property(c => c.Document).HasColumnName("document").HasColumnType("json").IsRequired();
            credential.Property(c => c.CreatedAt).HasColumnName("created_at");

            credential.HasOne<Socio>().WithMany().HasForeignKey(c => c.SocioId).OnDelete(DeleteBehavior.Restrict);
            credential.HasIndex(c => c.VcId).IsUnique().HasDatabaseName("ux_credentials_vc_id");
            credential.HasIndex(c => c.SocioId).HasDatabaseName("ix_credentials_socio_id");
            credential.HasIndex(c => c.ValidFrom).IsDescending().HasDatabaseName("ix_credentials_valid_from");
        });
    }

    private static ValueConverter<Categoria, string> CategoriaConverter() =>
        new(
            categoria => categoria.ToText(),
            text => ParseCategoria(text));

    private static Categoria ParseCategoria(string text) =>
        CategoriaText.TryParse(text, out var categoria)
            ? categoria
            : throw new InvalidOperationException($"Categoría desconocida en la base: '{text}'.");
}
