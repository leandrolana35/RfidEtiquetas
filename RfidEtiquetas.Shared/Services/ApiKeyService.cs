using System.Security.Cryptography;
using System.Text;

namespace RfidEtiquetas.Shared.Services;

public static class ApiKeyService
{
    // Chave real, gerada só na criação: "afx_" + 32 bytes aleatórios em base64url.
    // Nunca é salva em texto puro — só o hash (comparação) e o prefixo (identificação visual).
    public static string GerarChave()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return $"afx_{token}";
    }

    public static string Hash(string chave)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(chave));
        return Convert.ToHexString(bytes);
    }

    public static string Prefixo(string chave) => chave.Length <= 12 ? chave : chave[..12] + "…";
}
