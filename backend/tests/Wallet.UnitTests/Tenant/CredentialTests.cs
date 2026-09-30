using Wallet.Issuer;
using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant;

public class CredentialTests
{
    private static readonly DateTimeOffset _validFrom = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);
    private static readonly Guid _vcGuid = Guid.Parse("8f14e45f-ceea-467e-9de1-93f5a5f4bfae");

    [Fact]
    public void FromIssued_ConUnaVcYUnSocio_ArmaElSnapshotConSusDatos()
    {
        var socio = CrearSocio();
        var vc = CrearVc();
        var createdAt = _validFrom.AddSeconds(1);

        var credential = Credential.FromIssued(vc, socio, "club-futbol", createdAt);

        Assert.Equal(_vcGuid, credential.Id);
        Assert.Equal(vc.Id, credential.VcId);
        Assert.Equal(socio.Id, credential.SocioId);
        Assert.Equal("club-futbol", credential.TenantId);
        Assert.Equal("Juan", credential.Nombre);
        Assert.Equal("Pérez", credential.Apellido);
        Assert.Equal("30123456", credential.Dni);
        Assert.Equal(123, credential.NumeroSocio);
        Assert.Equal(Categoria.Adulto, credential.Categoria);
        Assert.Equal("https://cdn.futbol.com.ar/socios/8f14e45f.jpg", credential.Foto);
        Assert.Equal(_validFrom, credential.ValidFrom);
        Assert.Equal(_validFrom.AddYears(1), credential.ValidUntil);
        Assert.Equal(CredentialStatus.Active, credential.Status);
        Assert.Equal(createdAt, credential.CreatedAt);
    }

    [Fact]
    public void FromIssued_ConUnaVc_GuardaElDocumentoTalComoLoSerializaElIssuer()
    {
        var vc = CrearVc();

        var credential = Credential.FromIssued(vc, CrearSocio(), "club-futbol", _validFrom);

        Assert.Equal(vc.ToJson(), credential.Document);
    }

    private static Socio CrearSocio() =>
        new(
            id: Guid.NewGuid(),
            subjectDid: "did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6",
            numeroSocio: 123,
            dni: "30123456",
            nombre: "Juan",
            apellido: "Pérez",
            categoria: Categoria.Adulto,
            foto: "https://cdn.futbol.com.ar/socios/8f14e45f.jpg",
            createdAt: _validFrom,
            updatedAt: _validFrom);

    private static VerifiableCredential CrearVc() =>
        new(
            Id: $"https://credenciales.futbol.com.ar/{_vcGuid}",
            Type: ["VerifiableCredential", "SocioCredential"],
            Issuer: "did:example:futbol",
            ValidFrom: _validFrom,
            ValidUntil: _validFrom.AddYears(1),
            CredentialStatus: CredentialStatus.Active,
            CredentialSubject: new Dictionary<string, string> { ["nombre"] = "Juan" },
            Proof: new Proof("HMAC-SHA256", _validFrom, "did:example:futbol#key-1", "firma"));
}
