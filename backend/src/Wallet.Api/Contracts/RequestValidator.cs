using Wallet.Tenant;

namespace Wallet.Api.Contracts;

/// <summary>Validación de los datos de entrada (ADR 007). Los errores se agrupan por campo, con los nombres del JSON.</summary>
internal static class RequestValidator
{
    private const int _maxNombre = 100;
    private const int _maxFoto = 2048;

    public static bool TryValidate(
        IssueCredentialRequest request,
        out IssueCredentialCommand command,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        command = null!;

        var nombre = ValidateName(request.Nombre, "nombre", errors);
        var apellido = ValidateName(request.Apellido, "apellido", errors);

        if (!Dni.TryNormalize(request.Dni, out var dni))
        {
            errors["dni"] = ["El DNI debe tener entre 7 y 9 dígitos (se admiten puntos y espacios)."];
        }

        if (!CategoriaText.TryParse(request.Categoria?.Trim(), out var categoria))
        {
            errors["categoria"] = ["La categoría debe ser adulto, juvenil o niño."];
        }

        var foto = request.Foto?.Trim() ?? string.Empty;
        if (foto.Length > _maxFoto
            || !Uri.TryCreate(foto, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            errors["foto"] = ["La foto debe ser una URL absoluta http o https (hasta 2048 caracteres)."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new IssueCredentialCommand(nombre, apellido, dni, categoria, foto);
        return true;
    }

    public static bool TryValidate(string? dni, string? numeroSocio, out Dictionary<string, string[]> errors)
    {
        errors = [];

        if (!string.IsNullOrWhiteSpace(dni) && !Dni.TryNormalize(dni, out _))
        {
            errors["dni"] = ["El DNI debe tener entre 7 y 9 dígitos (se admiten puntos y espacios)."];
        }

        if (!string.IsNullOrWhiteSpace(numeroSocio) && !NumeroSocio.TryParse(numeroSocio, out _))
        {
            errors["numeroSocio"] = ["El número de socio debe contener solo dígitos."];
        }

        return errors.Count == 0;
    }

    private static string ValidateName(string? value, string field, Dictionary<string, string[]> errors)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > _maxNombre)
        {
            errors[field] = [$"El campo es obligatorio y admite hasta {_maxNombre} caracteres."];
        }

        return trimmed;
    }
}
