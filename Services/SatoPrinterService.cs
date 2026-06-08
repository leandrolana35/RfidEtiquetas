using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using RfidEtiquetas.Data.Models;

namespace RfidEtiquetas.Services;

/// <summary>
/// Communicates with Sato C4LX via SBPL (Sato Barcode Printer Language).
/// Reference: Sato C4LX SBPL Programming Manual.
/// </summary>
public class SatoPrinterService
{
    private readonly RfidEncoderService _rfidEncoder;
    private static readonly char ESC = '\x1B';

    public SatoPrinterService(RfidEncoderService rfidEncoder)
    {
        _rfidEncoder = rfidEncoder;
    }

    public string[] ListarPortas() => SerialPort.GetPortNames();

    public record ResultadoImpressao(bool Sucesso, string? Erro, string? AvisoRfid);

    /// <summary>
    /// Imprime usando os parâmetros de conexão salvos (COM ou Rede).
    /// </summary>
    public ResultadoImpressao Imprimir(
        Etiqueta template,
        string codigoBarras,
        string dadoRfid,
        Parametros conexao)
    {
        string? avisoRfid = null;
        try
        {
            var sbpl = MontarSbpl(template, codigoBarras, dadoRfid, out avisoRfid);
            var bytes = Encoding.ASCII.GetBytes(sbpl);

            if (conexao.TipoConexao == 1)
                EnviarPorRede(bytes, conexao.ImpressoraIp, conexao.ImpressoraPorta);
            else
                EnviarPorCom(bytes, conexao.PortaCom, conexao.BaudRate);

            return new(true, null, avisoRfid);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, avisoRfid);
        }
    }

    private static void EnviarPorCom(byte[] bytes, string portaCom, int baudRate)
    {
        using var porta = new SerialPort(portaCom, baudRate, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 5000,
            ReadTimeout = 2000,
            Handshake = Handshake.XOnXOff
        };
        porta.Open();
        porta.Write(bytes, 0, bytes.Length);
        porta.Close();
    }

    private static void EnviarPorRede(byte[] bytes, string ip, int porta)
    {
        using var cliente = new TcpClient();
        // Timeout de conexão de 5 segundos
        var conectar = cliente.ConnectAsync(ip, porta);
        if (!conectar.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException($"Não foi possível conectar na impressora {ip}:{porta} (tempo esgotado).");

        using var stream = cliente.GetStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    /// <summary>
    /// Testa a conexão com a impressora sem imprimir nada.
    /// </summary>
    public ResultadoImpressao TestarConexao(Parametros conexao)
    {
        try
        {
            if (conexao.TipoConexao == 1)
            {
                using var cliente = new TcpClient();
                var conectar = cliente.ConnectAsync(conexao.ImpressoraIp, conexao.ImpressoraPorta);
                if (!conectar.Wait(TimeSpan.FromSeconds(5)))
                    return new(false, $"Sem resposta de {conexao.ImpressoraIp}:{conexao.ImpressoraPorta}", null);
            }
            else
            {
                using var porta = new SerialPort(conexao.PortaCom, conexao.BaudRate);
                porta.Open();
                porta.Close();
            }
            return new(true, null, null);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, null);
        }
    }

    public string GerarSbplPreview(Etiqueta template, string codigoBarras, string dadoRfid)
    {
        return MontarSbpl(template, codigoBarras, dadoRfid, out _);
    }

    private string MontarSbpl(Etiqueta t, string codBarras, string dadoRfid, out string? avisoRfid)
    {
        avisoRfid = null;
        var sb = new StringBuilder();

        // ESC A = Start of label
        sb.Append($"{ESC}A");

        // ESC CS = print speed (1-10)
        sb.Append($"{ESC}CS{t.Velocidade:D2}");

        // ESC D = density (darkness 1-15)
        sb.Append($"{ESC}D{t.Densidade:D2}");

        // ESC L = label width in mm * 100 (e.g. 10000 = 100mm)
        sb.Append($"{ESC}L{t.LarguraMm * 100:D5}");

        // === RFID Write ===
        if (t.RfidAtivo && !string.IsNullOrWhiteSpace(dadoRfid))
        {
            var encoding = (RfidEncoderService.EncodingTipo)t.RfidEncodingTipo;
            var result = _rfidEncoder.Codificar(t.RfidPrefixo + dadoRfid, t.RfidTamanhoBits, encoding);
            avisoRfid = result.Aviso;

            // SBPL RFID write command for C4LX:
            // ESC XR <banco>,<bits>,<hex_data>
            // Banco: EPC=01, User=03
            string bancoCode = t.RfidBanco == 0 ? "01" : "03";
            sb.Append($"{ESC}XR{bancoCode},{t.RfidTamanhoBits:D3},{result.HexData}");
        }

        // === Texts ===
        AppendTexto(sb, t.Texto1, t.Texto1X, t.Texto1Y, t.Texto1Tam);
        AppendTexto(sb, t.Texto2, t.Texto2X, t.Texto2Y, t.Texto2Tam);
        AppendTexto(sb, t.Texto3, t.Texto3X, t.Texto3Y, t.Texto3Tam);
        AppendTexto(sb, t.Texto4, t.Texto4X, t.Texto4Y, t.Texto4Tam);

        // === Barcode ===
        if (t.BarImprimir && !string.IsNullOrWhiteSpace(codBarras))
        {
            string barWithAfixes = t.BarPrefixo + ApplyZeroPad(codBarras, t.BarZerosEsquerda) + t.BarSufixo;
            string barTipoCode = MapBarcodeTipo(t.BarTipo);

            // SBPL barcode: ESC BW <x>,<y>,<rotation>,<tipo>,<espessura>,<branco>,<altura>
            sb.Append($"{ESC}BW{t.BarCodX:D4},{t.BarCodY:D4},0,{barTipoCode},{t.BarEspessura},{t.BarEspessura * 2},{t.BarAltura:D3}");

            if (t.BarImprimirCodigo)
            {
                // Print human readable below barcode
                // ESC FT <x>,<y>,<size> + data
                sb.Append($"{ESC}FT{t.BarCodX:D4},{t.BarCodY + t.BarAltura + 2:D4},{t.BarFonteTam:D2}");
                sb.Append(barWithAfixes);
                sb.Append("\r\n");
            }

            // Write barcode data — must follow BW command
            sb.Append(barWithAfixes);
            sb.Append("\r\n");
        }

        // === Logo ===
        if (t.LogoImprimir && !string.IsNullOrWhiteSpace(t.LogoArquivo) && File.Exists(t.LogoArquivo))
        {
            // ESC XP <x>,<y>,<w>,<h>,arquivo
            sb.Append($"{ESC}XP{t.LogoX:D4},{t.LogoY:D4},{t.LogoLargura:D3},{t.LogoAltura:D3}");
        }

        // ESC Q = quantity
        sb.Append($"{ESC}Q{t.Quantidade:D4}");

        // ESC Z = end/print
        sb.Append($"{ESC}Z");

        return sb.ToString();
    }

    private static void AppendTexto(StringBuilder sb, string texto, int x, int y, int tam)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        char esc = '\x1B';
        // ESC FW <x>,<y>,0,A,<size>,<size>  = WinFont text
        sb.Append($"{esc}FW{x:D4},{y:D4},0,A,{tam:D2},{tam:D2}");
        sb.Append(texto);
        sb.Append("\r\n");
    }

    private static string MapBarcodeTipo(int tipo) => tipo switch
    {
        1 => "G", // Code128
        2 => "E", // EAN-13
        3 => "B", // Code39
        4 => "P", // QR Code
        5 => "A", // Code93
        _ => "G"
    };

    private static string ApplyZeroPad(string cod, int zeros)
    {
        return zeros > 0 ? cod.PadLeft(zeros, '0') : cod;
    }
}
