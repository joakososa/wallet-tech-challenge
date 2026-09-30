using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Wallet.Issuer;

/// <summary>Todo lo que se firma: la credencial sin su <c>proof</c>.</summary>
internal sealed record CredentialPayload(
    string Id,
    IReadOnlyList<string> Types,
    string Issuer,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    CredentialStatus CredentialStatus,
    IReadOnlyDictionary<string, string> CredentialSubject)
{
    private const string _dateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>JSON canónico sobre el que se calcula la firma (ADR 004 y 012).</summary>
    public byte[] ToCanonicalBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, _writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("credentialStatus", (int)CredentialStatus);

            writer.WriteStartObject("credentialSubject");
            foreach (var claim in CredentialSubject.OrderBy(c => c.Key, StringComparer.Ordinal))
            {
                writer.WriteString(claim.Key, claim.Value);
            }

            writer.WriteEndObject();

            writer.WriteString("id", Id);
            writer.WriteString("issuer", Issuer);

            writer.WriteStartArray("type");
            foreach (var type in Types)
            {
                writer.WriteStringValue(type);
            }

            writer.WriteEndArray();

            writer.WriteString("validFrom", Format(ValidFrom));
            writer.WriteString("validUntil", Format(ValidUntil));
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static string Format(DateTimeOffset date) =>
        date.UtcDateTime.ToString(_dateFormat, CultureInfo.InvariantCulture);
}
