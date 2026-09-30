namespace Wallet.Tenant;

/// <summary>La única implementación de la regla del DNI (ADR 006): sin puntos ni espacios, solo dígitos, de 7 a 9.</summary>
public static class Dni
{
    private const int _minLength = 7;
    private const int _maxLength = 9;

    public static bool TryNormalize(string? input, out string dni)
    {
        dni = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Replace(".", string.Empty).Replace(" ", string.Empty);
        if (normalized.Length is < _minLength or > _maxLength || !normalized.All(char.IsAsciiDigit))
        {
            return false;
        }

        dni = normalized;
        return true;
    }
}
