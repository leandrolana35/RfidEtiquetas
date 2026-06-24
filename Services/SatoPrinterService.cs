using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using RfidEtiquetas.Data.Models;

namespace RfidEtiquetas.Services;

/// <summary>
/// Comunica com a impressora via ZPL (Zebra Programming Language).
/// A Sato C4LX usada aceita ZPL — template replicado do aplicativo original.
/// </summary>
public class SatoPrinterService
{
    private readonly RfidEncoderService _rfidEncoder;

    // Resolução da impressora em dots por mm. 203 dpi = 8 dots/mm (padrão).
    // Se sua impressora for 305 dpi, troque para 12.
    private const int DotsPorMm = 8;

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
            var zpl = MontarZpl(template, codigoBarras, dadoRfid, out avisoRfid);
            var bytes = Encoding.ASCII.GetBytes(zpl);

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

    public record ResultadoLote(bool Sucesso, int Impressos, int Total, string? Erro);

    /// <summary>
    /// Imprime/grava uma lista de itens em lote, reutilizando uma única conexão.
    /// Cada item: (codigoBarras, dadoRfid). onProgresso é chamado a cada etiqueta.
    /// </summary>
    public ResultadoLote ImprimirLote(
        Etiqueta template,
        List<(string CodigoBarras, string DadoRfid)> itens,
        Parametros conexao,
        int intervaloMs = 1000,
        Action<int>? onProgresso = null)
    {
        // Cada etiqueta do lote é impressa uma vez
        template.Quantidade = 1;
        int feitos = 0;

        try
        {
            if (conexao.TipoConexao == 1)
            {
                using var cliente = new TcpClient();
                if (!cliente.ConnectAsync(conexao.ImpressoraIp, conexao.ImpressoraPorta).Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException($"Não foi possível conectar em {conexao.ImpressoraIp}:{conexao.ImpressoraPorta}.");
                using var stream = cliente.GetStream();
                foreach (var item in itens)
                {
                    var zpl = MontarZpl(template, item.CodigoBarras, item.DadoRfid, out _);
                    var bytes = Encoding.ASCII.GetBytes(zpl);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                    feitos++;
                    onProgresso?.Invoke(feitos);
                    PausarEntreEtiquetas(intervaloMs);
                }
            }
            else
            {
                using var porta = new SerialPort(conexao.PortaCom, conexao.BaudRate, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    Handshake = Handshake.XOnXOff
                };
                porta.Open();
                foreach (var item in itens)
                {
                    var zpl = MontarZpl(template, item.CodigoBarras, item.DadoRfid, out _);
                    var bytes = Encoding.ASCII.GetBytes(zpl);
                    porta.Write(bytes, 0, bytes.Length);
                    feitos++;
                    onProgresso?.Invoke(feitos);
                    PausarEntreEtiquetas(intervaloMs);
                }
                porta.Close();
            }

            return new(true, feitos, itens.Count, null);
        }
        catch (Exception ex)
        {
            return new(false, feitos, itens.Count, ex.Message);
        }
    }

    private static void PausarEntreEtiquetas(int intervaloMs)
    {
        if (intervaloMs > 0)
            Thread.Sleep(Math.Min(intervaloMs, 5000));
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
        return MontarZpl(template, codigoBarras, dadoRfid, out _);
    }

    /// <summary>
    /// Monta o comando ZPL. Estrutura replicada do aplicativo original que funcionava.
    /// </summary>
    private string MontarZpl(Etiqueta t, string codBarras, string dadoRfid, out string? avisoRfid)
    {
        avisoRfid = null;
        var sb = new StringBuilder();

        // Início + acentuação (UTF-8)
        sb.Append("^XA^CI28");

        // IMPORTANTE: por padrão NÃO enviamos NENHUM comando que altere a
        // configuração da impressora (tamanho, passo, posição, velocidade,
        // densidade, orientação). Apenas gravamos o chip e imprimimos os campos.
        // Esses ajustes só são enviados se o usuário marcar "Definir tamanho"
        // explicitamente no modelo.
        if (t.DefinirTamanho)
        {
            sb.Append($"^PW{t.LarguraMm * DotsPorMm}");
            sb.Append($"^LL{t.AlturaMm * DotsPorMm}");
            sb.Append("^LH0,0^LS0,0^LT0^FWN^LRN");
            sb.Append($"^PR{Clamp(t.Velocidade, 1, 14)}");
            sb.Append($"^MD{Clamp(t.Densidade, 0, 30)}");
        }

        // Quantidade de cópias DESTE trabalho (não é configuração persistente).
        sb.Append($"^PQ{Math.Max(1, t.Quantidade)}");

        // === Gravação RFID (banco EPC) ===
        if (t.RfidAtivo && !string.IsNullOrWhiteSpace(dadoRfid))
        {
            var encoding = (RfidEncoderService.EncodingTipo)t.RfidEncodingTipo;
            var alinhamento = (RfidEncoderService.Alinhamento)t.RfidAlinhamento;
            var result = _rfidEncoder.Codificar(t.RfidPrefixo + dadoRfid, t.RfidTamanhoBits, encoding, alinhamento);
            avisoRfid = result.Aviso;

            // ^RFW,H,2,<words>,1 — Write em Hexadecimal no banco EPC.
            // words = número de palavras de 16 bits (96 bits = 6 words).
            int words = t.RfidTamanhoBits / 16;
            sb.Append($"^RFW,H,2,{words},1^FD{result.HexData}^FS");
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

            // QR Code usa prefixo de modo no ^FD
            sb.Append(t.BarTipo == 4 ? $"^FDLA,{dados}^FS" : $"^FD{dados}^FS");
        }

        // === Logo (imagem .GRF previamente carregada na impressora) ===
        if (t.LogoImprimir && !string.IsNullOrWhiteSpace(t.LogoArquivo))
        {
            sb.Append($"^FO{t.LogoX},{t.LogoY}^XG{t.LogoArquivo},1,1^FS");
        }

        // Ponto de registro (1 dot) — presente no template original.
        // Garante que a etiqueta seja processada/alimentada mesmo numa
        // etiqueta que só grava RFID (sem texto/código visível).
        sb.Append("^FO0,0^GB1,1,1^FS");

        // Fim
        sb.Append("^XZ");

        return sb.ToString();
    }

    private static void AppendTexto(StringBuilder sb, string texto, int x, int y, int tam)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        // ^FO<x>,<y>^A0N,<altura>,<largura>^FD<texto>^FS
        sb.Append($"^FO{x},{y}^A0N,{tam},{tam}^FD{EscaparZpl(texto)}^FS");
    }

    private static string EscaparZpl(string s)
        => s.Replace("^", " ").Replace("~", " ");

    private static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));

    private static string ApplyZeroPad(string cod, int zeros)
        => zeros > 0 ? cod.PadLeft(zeros, '0') : cod;
}
