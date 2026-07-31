namespace RfidEtiquetas.Shared.Data.Models;

public enum StatusPrintJob
{
    Pendente = 0,
    Processando = 1,
    Concluido = 2,
    Falhou = 3
}

// Trabalho de impressão recebido via API externa (POST /api/v1/print).
// Fica na fila até o PrintJobProcessor (background service) processá-lo.
// Se ProjetoId/TagRegistroId forem informados, o job fica ligado ao mesmo
// rastreamento de status que o fluxo interno (Lote.razor) usa — o dashboard
// de Projetos passa a refletir também as impressões feitas pela API.
public class PrintJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int ApiKeyId { get; set; }
    public ApiKey? ApiKey { get; set; }

    public string Epc { get; set; } = "";
    public string? Sku { get; set; }
    public string? Lote { get; set; }

    public int EstacaoId { get; set; }
    public EstacaoImpressora? Estacao { get; set; }

    public int EtiquetaTemplateId { get; set; }
    public Etiqueta? EtiquetaTemplate { get; set; }

    // Opcionais: quando informados, o job está associado a um Projeto/tag
    // específicos já cadastrados internamente (validado contra a empresa da API Key).
    public int? ProjetoId { get; set; }
    public Projeto? Projeto { get; set; }
    public long? TagRegistroId { get; set; }
    public TagRegistro? TagRegistro { get; set; }

    public StatusPrintJob Status { get; set; } = StatusPrintJob.Pendente;
    public string? MensagemErro { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessadoEm { get; set; }
}
