using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

public record ResultadoImpressao(bool Sucesso, string? Erro, string? AvisoRfid);
public record ResultadoLote(bool Sucesso, int Impressos, int Total, string? Erro, bool Cancelado = false);

// Implementado apenas no app Local (só ele fala com hardware via COM/TCP).
// O app Web nunca instancia isso — só lê o TagRegistro que o Local grava.
public interface IPrinterService
{
    ResultadoImpressao Imprimir(Etiqueta template, string codigoBarras, string dadoRfid, EstacaoImpressora estacao);

    ResultadoLote ImprimirLote(
        Etiqueta template,
        List<(string CodigoBarras, string DadoRfid)> itens,
        EstacaoImpressora estacao,
        int intervaloMs,
        Action<int>? onProgresso,
        CancellationToken cancelamento = default);

    ResultadoImpressao TestarConexao(EstacaoImpressora estacao);

    // Envia o comando de cancelar/limpar a fila da impressora (~JA no ZPL),
    // para interromper imediatamente etiquetas já no buffer da impressora.
    void CancelarImpressora(EstacaoImpressora estacao);

    string[] ListarPortas();
}
