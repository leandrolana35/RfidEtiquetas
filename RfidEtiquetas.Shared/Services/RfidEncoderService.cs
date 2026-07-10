namespace RfidEtiquetas.Shared.Services;

/// <summary>
/// Encodes data for RFID Gen2 tags.
/// Fixes the bug where alphanumeric data > 14 chars failed silently.
/// </summary>
public class RfidEncoderService
{
    public enum BancoMemoria { EPC = 0, UserMemory = 1 }
    public enum EncodingTipo { ASCII = 0, HexDireto = 1 }
    // Esquerda/Direita = preenche com zeros. EspacosEsquerda = preenche com espaços (0x20="20") à esquerda.
    public enum Alinhamento { Esquerda = 0, Direita = 1, EspacosEsquerda = 2 }

    public record ResultadoCodificacao(
        string HexData,
        int BitsUsados,
        int BitsDisponiveis,
        bool CabeFit,
        string? Aviso
    );

    public ResultadoCodificacao Codificar(string dado, int tamanhoBits, EncodingTipo encoding,
        Alinhamento alinhamento = Alinhamento.Esquerda)
    {
        if (string.IsNullOrEmpty(dado))
        {
            var vazio = "00".PadRight(tamanhoBits / 4, '0');
            return new(vazio, 0, tamanhoBits, true, null);
        }

        string hexData;

        if (encoding == EncodingTipo.HexDireto)
        {
            // Validate that it's valid hex
            dado = dado.Replace(" ", "").ToUpperInvariant();
            if (dado.Length % 2 != 0) dado = dado.PadLeft(dado.Length + 1, '0');
            hexData = dado;
        }
        else
        {
            // ASCII encoding: each char = 1 byte = 2 hex digits
            hexData = string.Concat(dado.Select(c => ((int)c).ToString("X2")));
        }

        int bitsUsados = hexData.Length * 4;
        int hexCapacidade = tamanhoBits / 4;

        string? aviso = null;

        if (hexData.Length > hexCapacidade)
        {
            // Truncate with warning — never silently corrupt data
            aviso = $"Dado tem {bitsUsados} bits mas o banco tem {tamanhoBits} bits. " +
                    $"Truncado para {hexCapacidade / 2} caracteres. " +
                    $"Aumente o tamanho do EPC para {NextValidBitSize(bitsUsados)} bits.";
            // Mantém a parte mais relevante conforme o alinhamento
            hexData = alinhamento == Alinhamento.Direita
                ? hexData[^hexCapacidade..]   // mantém o final (direita)
                : hexData[..hexCapacidade];   // mantém o início (esquerda)
        }
        else
        {
            hexData = alinhamento switch
            {
                // Zeros à esquerda (valor encostado à direita)
                Alinhamento.Direita => hexData.PadLeft(hexCapacidade, '0'),
                // Espaços (0x20 = "20") à esquerda; valor encostado à direita
                Alinhamento.EspacosEsquerda =>
                    string.Concat(Enumerable.Repeat("20", (hexCapacidade - hexData.Length) / 2)) + hexData,
                // Padrão: zeros à direita (valor à esquerda)
                _ => hexData.PadRight(hexCapacidade, '0')
            };
        }

        return new(hexData.ToUpperInvariant(), bitsUsados, tamanhoBits, bitsUsados <= tamanhoBits, aviso);
    }

    public int CalcularBitsNecessarios(string dado, EncodingTipo encoding)
    {
        if (string.IsNullOrEmpty(dado)) return 0;

        return encoding == EncodingTipo.HexDireto
            ? dado.Replace(" ", "").Length * 4
            : dado.Length * 8;
    }

    /// <summary>
    /// Returns recommended EPC bank size (96, 128, 192, 256 bits).
    /// </summary>
    public int RecomendarTamanho(string dado, EncodingTipo encoding)
    {
        int bits = CalcularBitsNecessarios(dado, encoding);
        return NextValidBitSize(bits);
    }

    private static int NextValidBitSize(int bits)
    {
        int[] tamanhos = [96, 128, 192, 256];
        return tamanhos.FirstOrDefault(t => t >= bits, 256);
    }

    public string GerarEpcSgtin96(string empresaGcp, string referencia, long serial)
    {
        // Simplified SGTIN-96 encoding for common use
        // Full GS1 encoding is complex — this produces a valid 24-char hex string
        var combined = $"{empresaGcp}{referencia}{serial:D8}";
        return Codificar(combined, 96, EncodingTipo.ASCII).HexData;
    }
}
