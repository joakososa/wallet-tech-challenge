using System.Text;
using Microsoft.Extensions.Options;

namespace Wallet.Issuer;

/// <summary>Reglas de configuración que se verifican al arrancar (ADR 004, 011 y 012).</summary>
internal sealed class IssuerOptionsValidator : IValidateOptions<IssuerOptions>
{
    private const int _minSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, IssuerOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SigningKey)
            || Encoding.UTF8.GetByteCount(options.SigningKey) < _minSigningKeyBytes)
        {
            failures.Add($"Issuer__SigningKey es obligatoria y debe tener al menos {_minSigningKeyBytes} bytes.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyId)
            || options.KeyId.Contains('#', StringComparison.Ordinal)
            || options.KeyId.Any(char.IsWhiteSpace))
        {
            failures.Add("Issuer__KeyId es obligatorio y no puede contener '#' ni espacios.");
        }

        if (string.IsNullOrWhiteSpace(options.Did)
            || !options.Did.StartsWith("did:", StringComparison.Ordinal)
            || options.Did.Length == "did:".Length)
        {
            failures.Add("Issuer__Did es obligatorio y debe empezar con 'did:'.");
        }

        if (!Uri.TryCreate(options.CredentialBaseUri, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add("Issuer__CredentialBaseUri es obligatoria y debe ser una URL absoluta http o https.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
