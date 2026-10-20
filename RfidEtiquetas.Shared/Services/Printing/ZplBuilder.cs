using System;
using System.Linq;
using System.Text;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

// Núcleo de montagem do ZPL, comum aos dois modelos de impressora.
// Extraído do SatoPrinterService original: a estrutura replica exatamente
// o app antigo que funcionava (o bloco ^PW/^LL/^LH.../^PQ/^PR/^MD é
// essencial para o RFID posicionar a tag sob a antena — ver PrinterProfile
// para o que varia por modelo).
public class ZplBuilder
{
    private readonly RfidEncoderService _rfidEncoder;

    public ZplBuilder(RfidEncoderService rfidEncoder)
    {
        _rfidEncoder = rfidEncoder;
    }

    public string Montar(Etiqueta t, string codBarras, string dadoRfid, PrinterProfile perfil, out string? avisoRfid, OpcoesRfidLock? lockRfid = null)
    {
        avisoRfid = null;
        var sb = new StringBuilder();

        sb.Append("^XA^CI28");
        sb.Append($"^PW{t.LarguraMm * perfil.DotsPorMm}");
        sb.Append($"^LL{t.AlturaMm * perfil.DotsPorMm}");
        sb.Append("^LH0,0^LS0,0^LT0^FWN^LRN");
        sb.Append($"^PQ{Math.Max(1, t.Quantidade)}");
        sb.Append($"^PR{Clamp(t.Velocidade, perfil.VelocidadeMin, perfil.VelocidadeMax)}");
        sb.Append($"^MD{Clamp(t.Densidade, perfil.DensidadeMin, perfil.DensidadeMax)}");

        // === Gravação RFID (banco EPC) ===
        if (t.RfidAtivo && !string.IsNullOrWhiteSpace(dadoRfid))
        {
            var encoding = (RfidEncoderService.EncodingTipo)t.RfidEncodingTipo;
            var alinhamento = (RfidEncoderService.Alinhamento)t.RfidAlinhamento;
            var result = _rfidEncoder.Codificar(t.RfidPrefixo + dadoRfid, t.RfidTamanhoBits, encoding, alinhamento);
            avisoRfid = result.Aviso;

            int words = t.RfidTamanhoBits / 16;
            sb.Append(perfil.MontarComandoRfidWrite(words, result.HexData));

            // === Proteção (senha + lock) do chip — opcional, por job ===
            // Vem DEPOIS da gravação do EPC e no MESMO formato de etiqueta (a tag
            // só passa uma vez sob a antena). Se o chip não suportar senha, a
            // impressora emite VOID nessa etiqueta e segue para a próxima.
            if (lockRfid is not null && !string.IsNullOrWhiteSpace(lockRfid.SenhaAcesso))
                sb.Append(MontarBlocoLockRfid(lockRfid));
        }

        // === Valor do RFID impresso como texto ===
        if (t.RfidImprimirValor && !string.IsNullOrWhiteSpace(dadoRfid))
        {
            AppendTexto(sb, t.RfidPrefixo + dadoRfid, t.RfidValorX, t.RfidValorY, t.RfidValorTam);
        }

        // === Textos ===
        AppendTexto(sb, t.Texto1, t.Texto1X, t.Texto1Y, t.Texto1Tam);
        AppendTexto(sb, t.Texto2, t.Texto2X, t.Texto2Y, t.Texto2Tam);
        AppendTexto(sb, t.Texto3, t.Texto3X, t.Texto3Y, t.Texto3Tam);
        AppendTexto(sb, t.Texto4, t.Texto4X, t.Texto4Y, t.Texto4Tam);

        // === Código de barras ===
        if (t.BarImprimir && !string.IsNullOrWhiteSpace(codBarras))
        {
            string dados = t.BarPrefixo + ApplyZeroPad(codBarras, t.BarZerosEsquerda) + t.BarSufixo;
            string mostraTexto = t.BarImprimirCodigo ? "Y" : "N";
            int espessura = Math.Max(1, t.BarEspessura);

            sb.Append($"^FO{t.BarCodX},{t.BarCodY}");
            sb.Append($"^BY{espessura}");

            switch (t.BarTipo)
            {
                case 2: // EAN-13
                    sb.Append($"^BEN,{t.BarAltura},{mostraTexto},N");
                    break;
                case 3: // Code 39
                    sb.Append($"^B3N,N,{t.BarAltura},{mostraTexto},N");
                    break;
                case 4: // QR Code
                    sb.Append("^BQN,2,5");
                    break;
                case 5: // Code 93
                    sb.Append($"^BAN,{t.BarAltura},{mostraTexto},N");
                    break;
                default: // Code 128
                    sb.Append($"^BCN,{t.BarAltura},{mostraTexto},N,N");
                    break;
            }

            sb.Append(t.BarTipo == 4 ? $"^FDLA,{dados}^FS" : $"^FD{dados}^FS");
        }

        // === Logo (imagem .GRF previamente carregada na impressora) ===
        if (t.LogoImprimir && !string.IsNullOrWhiteSpace(t.LogoArquivo))
        {
            sb.Append($"^FO{t.LogoX},{t.LogoY}^XG{t.LogoArquivo},1,1^FS");
        }

        // Ponto de registro (1 dot) — garante que a etiqueta seja processada/alimentada
        // mesmo numa etiqueta que só grava RFID (sem texto/código visível).
        sb.Append("^FO0,0^GB1,1,1^FS");

        sb.Append("^XZ");

        return sb.ToString();
    }

    // Monta o bloco ZPL de senha + trava (EPC Gen2):
    //   ^RFW,H,P^FD<acesso>,<kill>^FS  -> grava a(s) senha(s) no banco Reserved
    //   ^RFW,H,,,A^FD<acesso>^FS       -> informa a senha de acesso para o lock
    //   ^RLM,k,a,e                     -> trava kill / acesso / EPC (L=destravável, P=permanente)
    private static string MontarBlocoLockRfid(OpcoesRfidLock o)
    {
        string acc = NormalizarSenha(o.SenhaAcesso);
        string? kill = string.IsNullOrWhiteSpace(o.SenhaKill) ? null : NormalizarSenha(o.SenhaKill!);

        var sb = new StringBuilder();
        sb.Append(kill is null ? $"^RFW,H,P^FD{acc}^FS" : $"^RFW,H,P^FD{acc},{kill}^FS");
        sb.Append($"^RFW,H,,,A^FD{acc}^FS");

        string trava = o.Permanente ? "P" : "L";
        string k = o.TravarKill ? trava : "U";       // U = deixa destravado (no-op em tag nova)
        string a = trava;                            // protege a própria senha de acesso
        string e = o.TravarAlteracao ? trava : "U";
        sb.Append($"^RLM,{k},{a},{e}");
        return sb.ToString();
    }

    // Garante 8 caracteres hexadecimais (32 bits). Remove o que não for hex,
    // completa com zeros à esquerda e corta no tamanho.
    private static string NormalizarSenha(string s)
    {
        var hex = new string((s ?? "").Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        if (hex.Length > 8) hex = hex[..8];
        return hex.PadLeft(8, '0');
    }

    private static void AppendTexto(StringBuilder sb, string texto, int x, int y, int tam)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        sb.Append($"^FO{x},{y}^A0N,{tam},{tam}^FD{EscaparZpl(texto)}^FS");
    }

    private static string EscaparZpl(string s)
        => s.Replace("^", " ").Replace("~", " ");

    private static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));

    private static string ApplyZeroPad(string cod, int zeros)
        => zeros > 0 ? cod.PadLeft(zeros, '0') : cod;
}
