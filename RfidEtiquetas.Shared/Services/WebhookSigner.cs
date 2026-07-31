using System.Security.Cryptography;
using System.Text;

namespace RfidEtiquetas.Shared.Services;

public static class WebhookSigner
{
    public static string GerarSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>HMAC-SHA256 do payload exatamente como enviado, em hex minúsculo.</summary>
    public static string Assinar(string secret, string payloadJson)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
