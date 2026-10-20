using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

public record ResultadoImpressao(bool Sucesso, string? Erro, string? AvisoRfid);
public record ResultadoLote(bool Sucesso, int Impressos, int Total, string? Erro, bool Cancelado = false);

// Opções de proteção (lock) do chip RFID, por job. Quando null, grava normal (sem travar).
// SenhaAcesso/SenhaKill = 8 caracteres hexadecimais (32 bits). A senha de acesso precisa
// ser diferente de "00000000" para o comando de lock funcionar (exigência do EPC Gen2).
public record OpcoesRfidLock(
    string SenhaAcesso,
    string? SenhaKill,
    bool TravarAlteracao,   // trava a regravação do EPC (só regrava com a senha)
    bool TravarKill,        // trava o comando de kill
    bool Permanente         // "P" (permalock irreversível) em vez de "L" (destravável com a senha)
);

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
        CancellationToken cancelamento = default,
        Func<bool>? pausado = null,
        OpcoesRfidLock? lockRfid = null);

    ResultadoImpressao TestarConexao(EstacaoImpressora estacao);

    // Envia o comando de cancelar/limpar a fila da impressora (~JA no ZPL),
    // para interromper imediatamente etiquetas já no buffer da impressora.
    void CancelarImpressora(EstacaoImpressora estacao);

    string[] ListarPortas();
}
