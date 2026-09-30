using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant;

public class CategoriaTextTests
{
    [Theory]
    [InlineData(Categoria.Adulto, "adulto")]
    [InlineData(Categoria.Juvenil, "juvenil")]
    [InlineData(Categoria.Nino, "niño")]
    public void ToText_ConUnaCategoria_DevuelveElTextoDelEnunciado(Categoria categoria, string esperado)
    {
        Assert.Equal(esperado, categoria.ToText());
    }

    [Theory]
    [InlineData("adulto", Categoria.Adulto)]
    [InlineData("juvenil", Categoria.Juvenil)]
    [InlineData("niño", Categoria.Nino)]
    public void TryParse_ConUnTextoValido_DevuelveLaCategoria(string texto, Categoria esperada)
    {
        var ok = CategoriaText.TryParse(texto, out var categoria);

        Assert.True(ok);
        Assert.Equal(esperada, categoria);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Adulto")]
    [InlineData("nino")]
    [InlineData("veterano")]
    public void TryParse_ConUnTextoInvalido_DevuelveFalse(string? texto)
    {
        Assert.False(CategoriaText.TryParse(texto, out _));
    }

    [Fact]
    public void ToText_ConUnValorFueraDelEnum_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Categoria)99).ToText());
    }
}
