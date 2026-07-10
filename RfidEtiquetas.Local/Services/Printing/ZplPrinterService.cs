using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Printing;

namespace RfidEtiquetas.Local.Services.Printing;

/// <summary>
/// Único ponto do sistema que fala de verdade com uma impressora (COM ou TCP).
/// A montagem do ZPL em si vive em RfidEtiquetas.Shared.Services.Printing.ZplBuilder,
/// compartilhada entre os modelos Sato e Printronix — aqui só escolhemos o
/// PrinterProfile certo (por EstacaoImpressora.ModeloImpressora) e enviamos os bytes.
/// </summary>
public class ZplPrinterService : IPrinterService
{
    private readonly ZplBuilder _zplBuilder;

    public ZplPrinterService(ZplBuilder zplBuilder)
    {
        _zplBuilder = zplBuilder;
    }

    public string[] ListarPortas() => SerialPort.GetPortNames();

    public ResultadoImpressao Imprimir(
        Etiqueta template,
        string codigoBarras,
        string dadoRfid,
        EstacaoImpressora estacao)
    {
        string? avisoRfid = null;
        try
        {
            var perfil = PrinterProfile.Para(estacao.ModeloImpressora);
            var zpl = _zplBuilder.Montar(template, codigoBarras, dadoRfid, perfil, out avisoRfid);
            var bytes = Encoding.ASCII.GetBytes(zpl);

            if (estacao.TipoConexao == TipoConexaoImpressora.Rede)
                EnviarPorRede(bytes, estacao.ImpressoraIp, estacao.ImpressoraPorta);
            else
                EnviarPorCom(bytes, estacao.PortaCom, estacao.BaudRate);

            return new(true, null, avisoRfid);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message, avisoRfid);
        }
    }

    /// <summary>
    /// Imprime/grava uma lista de itens em lote, reutilizando uma única conexão.
    /// Cada item: (codigoBarras, dadoRfid). onProgresso é chamado a cada etiqueta.
    /// </summary>
    public ResultadoLote ImprimirLote(
        Etiqueta template,
        List<(string CodigoBarras, string DadoRfid)> itens,
        EstacaoImpressora estacao,
        int intervaloMs = 1000,
        Action<int>? onProgresso = null)
    {
        var perfil = PrinterProfile.Para(estacao.ModeloImpressora);

        // Cada etiqueta do lote é impressa uma vez
        template.Quantidade = 1;
        int feitos = 0;

        try
        {
            if (estacao.TipoConexao == TipoConexaoImpressora.Rede)
            {
                using var cliente = new TcpClient();
                if (!cliente.ConnectAsync(estacao.ImpressoraIp, estacao.ImpressoraPorta).Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException($"Não foi possível conectar em {estacao.ImpressoraIp}:{estacao.ImpressoraPorta}.");
                using var stream = cliente.GetStream();
                foreach (var item in itens)
                {
                    var zpl = _zplBuilder.Montar(template, item.CodigoBarras, item.DadoRfid, perfil, out _);
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
                using var porta = new SerialPort(estacao.PortaCom, estacao.BaudRate, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    Handshake = Handshake.XOnXOff
                };
                porta.Open();
                foreach (var item in itens)
                {
                    var zpl = _zplBuilder.Montar(template, item.CodigoBarras, item.DadoRfid, perfil, out _);
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
    public ResultadoImpressao TestarConexao(EstacaoImpressora estacao)
    {
        try
        {
            if (estacao.TipoConexao == TipoConexaoImpressora.Rede)
            {
                using var cliente = new TcpClient();
                var conectar = cliente.ConnectAsync(estacao.ImpressoraIp, estacao.ImpressoraPorta);
                if (!conectar.Wait(TimeSpan.FromSeconds(5)))
                    return new(false, $"Sem resposta de {estacao.ImpressoraIp}:{estacao.ImpressoraPorta}", null);
            }
            else
            {
                using var porta = new SerialPort(estacao.PortaCom, estacao.BaudRate);
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

    public string GerarZplPreview(Etiqueta template, string codigoBarras, string dadoRfid, ModeloImpressora modelo)
    {
        var perfil = PrinterProfile.Para(modelo);
        return _zplBuilder.Montar(template, codigoBarras, dadoRfid, perfil, out _);
    }
}
