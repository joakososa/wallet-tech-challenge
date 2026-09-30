using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant;

public class NumeroSocioTests
{
    [Theory]
    [InlineData(1, "000001")]
    [InlineData(123, "000123")]
    [InlineData(999999, "999999")]
    [InlineData(1234567, "1234567")]
    public void Format_ConUnNumero_RellenaConCerosA6Digitos(long numero, string esperado)
    {
        Assert.Equal(esperado, NumeroSocio.Format(numero));
    }

    [Theory]
    [InlineData("000123", 123)]
    [InlineData("123", 123)]
    public void TryParse_ConDigitos_DevuelveElNumero(string text, long esperado)
    {
        var ok = NumeroSocio.TryParse(text, out var numero);

        Assert.True(ok);
        Assert.Equal(esperado, numero);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("12.5")]
    public void TryParse_ConTextoInvalido_DevuelveFalse(string? text)
    {
        var ok = NumeroSocio.TryParse(text, out _);

        Assert.False(ok);
    }
}
