using Wallet.Tenant;

namespace Wallet.UnitTests.Tenant;

public class DniTests
{
    [Theory]
    [InlineData("30123456", "30123456")]
    [InlineData("30.123.456", "30123456")]
    [InlineData(" 30 123 456 ", "30123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("123456789", "123456789")]
    public void TryNormalize_ConUnDniValido_DevuelveSoloLosDigitos(string input, string esperado)
    {
        var ok = Dni.TryNormalize(input, out var dni);

        Assert.True(ok);
        Assert.Equal(esperado, dni);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456")]
    [InlineData("1234567890")]
    [InlineData("3012345a")]
    [InlineData("-30123456")]
    [InlineData("30,123,456")]
    public void TryNormalize_ConUnDniInvalido_DevuelveFalse(string? input)
    {
        var ok = Dni.TryNormalize(input, out _);

        Assert.False(ok);
    }
}
