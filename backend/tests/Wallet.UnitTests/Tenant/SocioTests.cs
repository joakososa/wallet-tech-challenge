using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant;

public class SocioTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 8, 9, 14, 32, 10, TimeSpan.Zero);

    [Fact]
    public void Update_ConDatosNuevos_CambiaLosDatosMutablesYLaFechaDeActualizacion()
    {
        var socio = CrearSocio();
        var now = _createdAt.AddDays(3);

        socio.Update("Juana", "Gómez", Categoria.Juvenil, "https://cdn.futbol.com.ar/socios/nueva.jpg", now);

        Assert.Equal("Juana", socio.Nombre);
        Assert.Equal("Gómez", socio.Apellido);
        Assert.Equal(Categoria.Juvenil, socio.Categoria);
        Assert.Equal("https://cdn.futbol.com.ar/socios/nueva.jpg", socio.Foto);
        Assert.Equal(now, socio.UpdatedAt);
    }

    [Fact]
    public void Update_ConDatosNuevos_NoTocaLaIdentidadDelSocio()
    {
        var socio = CrearSocio();
        var id = socio.Id;

        socio.Update("Juana", "Gómez", Categoria.Juvenil, "https://cdn.futbol.com.ar/socios/nueva.jpg", _createdAt.AddDays(3));

        Assert.Equal(id, socio.Id);
        Assert.Equal("did:example:3fa85f64-5717-4562-b3fc-2c963f66afa6", socio.SubjectDid);
        Assert.Equal(123, socio.NumeroSocio);
        Assert.Equal("30123456", socio.Dni);
        Assert.Equal(_createdAt, socio.CreatedAt);
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
            createdAt: _createdAt,
            updatedAt: _createdAt);
}
