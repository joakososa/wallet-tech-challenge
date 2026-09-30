using System.Globalization;

namespace Wallet.Tenant;

/// <summary>El número de socio se guarda como <c>long</c> y se expone con relleno de ceros a 6 dígitos (ADR 006).</summary>
public static class NumeroSocio
{
    public static string Format(long numero) => numero.ToString("D6", CultureInfo.InvariantCulture);

    /// <summary>Interpreta el texto del filtro del listado (<c>000123</c> o <c>123</c>).</summary>
    public static bool TryParse(string? text, out long numero) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out numero);
}
