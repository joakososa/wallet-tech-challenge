namespace Wallet.Issuer;

internal sealed class CredentialIssuer : ICredentialIssuer
{
    private const string _proofType = "HMAC-SHA256";

    private readonly IssuerOptions _options;
    private readonly TimeProvider _time;
    private readonly HmacSha256Signer _signer;
    private readonly string _baseUri;

    public CredentialIssuer(IssuerOptions options, TimeProvider time)
    {
        _options = options;
        _time = time;
        _signer = new HmacSha256Signer(options.SigningKey);
        _baseUri = options.CredentialBaseUri.TrimEnd('/') + "/";
    }

    public Task<VerifiableCredential> IssueAsync(
        IReadOnlyDictionary<string, string> credentialSubject,
        IReadOnlyList<string> types,
        CredentialStatus status = CredentialStatus.Active,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Validate(credentialSubject, types, status);

        var validFrom = TruncateToSeconds(_time.GetUtcNow());

        // Se copian las colecciones: lo firmado tiene que ser lo devuelto aunque el llamador las modifique después.
        var payload = new CredentialPayload(
            Id: _baseUri + Guid.NewGuid(),
            Types: [.. types],
            Issuer: _options.Did,
            ValidFrom: validFrom,
            ValidUntil: validFrom.AddYears(1),
            CredentialStatus: status,
            CredentialSubject: new Dictionary<string, string>(credentialSubject));

        var proof = new Proof(
            Type: _proofType,
            Created: validFrom,
            VerificationMethod: $"{_options.Did}#{_options.KeyId}",
            ProofValue: _signer.Sign(payload.ToCanonicalBytes()));

        return Task.FromResult(new VerifiableCredential(payload, proof));
    }

    private static void Validate(
        IReadOnlyDictionary<string, string> credentialSubject,
        IReadOnlyList<string> types,
        CredentialStatus status)
    {
        ArgumentNullException.ThrowIfNull(credentialSubject);
        ArgumentNullException.ThrowIfNull(types);

        if (credentialSubject.Any(claim => claim.Value is null))
        {
            throw new ArgumentException("Los claims no pueden tener valores nulos.", nameof(credentialSubject));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Estado de credencial inválido.");
        }
    }

    private static DateTimeOffset TruncateToSeconds(DateTimeOffset date)
    {
        var ticks = date.UtcTicks;
        return new DateTimeOffset(ticks - (ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);
    }
}
