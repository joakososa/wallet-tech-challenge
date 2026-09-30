using System.Security.Cryptography;
using System.Text;

namespace Wallet.Issuer;

/// <summary>Calcula el <c>proofValue</c>: HMAC-SHA256 del JSON canónico, en base64 (ADR 004).</summary>
internal sealed class HmacSha256Signer(string signingKey)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(signingKey);

    public string Sign(byte[] canonicalJson) =>
        Convert.ToBase64String(HMACSHA256.HashData(_key, canonicalJson));
}
