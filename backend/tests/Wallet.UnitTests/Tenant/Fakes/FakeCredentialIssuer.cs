using Wallet.Issuer;

namespace Wallet.UnitTests.Tenant.Fakes;

/// <summary>Arma una VC con lo que recibe, sin firmar de verdad. <see cref="OnIssue"/> permite simular una falla de firma.</summary>
public sealed class FakeCredentialIssuer(DateTimeOffset now) : ICredentialIssuer
{
    public int IssueCalls { get; private set; }

    public IReadOnlyDictionary<string, string>? LastClaims { get; private set; }

    public IReadOnlyList<string>? LastTypes { get; private set; }

    public CancellationToken LastToken { get; private set; }

    /// <summary>Se ejecuta antes de armar la VC. Puede lanzar <see cref="IssuerSigningException"/>.</summary>
    public Action? OnIssue { get; set; }

    public Task<VerifiableCredential> IssueAsync(
        IReadOnlyDictionary<string, string> credentialSubject,
        IReadOnlyList<string> types,
        CredentialStatus status = CredentialStatus.Active,
        CancellationToken ct = default)
    {
        IssueCalls++;
        LastClaims = credentialSubject;
        LastTypes = types;
        LastToken = ct;
        OnIssue?.Invoke();

        var vc = new VerifiableCredential(
            Id: $"https://credenciales.futbol.com.ar/{Guid.NewGuid()}",
            Type: types,
            Issuer: "did:example:futbol",
            ValidFrom: now,
            ValidUntil: now.AddYears(1),
            CredentialStatus: status,
            CredentialSubject: credentialSubject,
            Proof: new Proof("HMAC-SHA256", now, "did:example:futbol#key-1", "firma-de-prueba"));

        return Task.FromResult(vc);
    }
}
