using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Wallet.Issuer;

/// <summary>
/// Escribe el JSON de la credencial (ADR 014): sin <c>proof</c> es el canónico que se firma (ADR 004 y 012);
/// con <c>proof</c> es el documento completo (ADR 013).
/// </summary>
internal sealed class CredentialJsonBuilder(CredentialPayload payload)
{
    private const string _dateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private Proof? _proof;

    /// <summary>Agrega <c>proof</c> como último campo.</summary>
    public CredentialJsonBuilder WithProof(Proof proof)
    {
        _proof = proof;
        return this;
    }

    public byte[] Build()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, _writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("credentialStatus", (int)payload.CredentialStatus);

            writer.WriteStartObject("credentialSubject");
            foreach (var claim in payload.CredentialSubject.OrderBy(c => c.Key, StringComparer.Ordinal))
            {
                writer.WriteString(claim.Key, claim.Value);
            }

            writer.WriteEndObject();

            writer.WriteString("id", payload.Id);
            writer.WriteString("issuer", payload.Issuer);

            writer.WriteStartArray("type");
            foreach (var type in payload.Types)
            {
                writer.WriteStringValue(type);
            }

            writer.WriteEndArray();

            writer.WriteString("validFrom", Format(payload.ValidFrom));
            writer.WriteString("validUntil", Format(payload.ValidUntil));

            if (_proof is not null)
            {
                WriteProof(writer, _proof);
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static void WriteProof(Utf8JsonWriter writer, Proof proof)
    {
        writer.WriteStartObject("proof");
        writer.WriteString("type", proof.Type);
        writer.WriteString("created", Format(proof.Created));
        writer.WriteString("verificationMethod", proof.VerificationMethod);
        writer.WriteString("proofValue", proof.ProofValue);
        writer.WriteEndObject();
    }

    private static string Format(DateTimeOffset date) =>
        date.UtcDateTime.ToString(_dateFormat, CultureInfo.InvariantCulture);
}
