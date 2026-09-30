using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wallet.Api.Contracts;

/// <summary>Escribe las fechas como <c>yyyy-MM-ddTHH:mm:ssZ</c>, el mismo formato del documento firmado (ADR 005).</summary>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string _format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(_format, CultureInfo.InvariantCulture));
}
