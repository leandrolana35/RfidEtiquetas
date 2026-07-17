namespace RfidEtiquetas.Shared.Data.Models;

public enum StatusTag
{
    NaoImpressa = 0,
    Impressa = 1,
    Erro = 2
}

// Uma linha por número de sequência dentro da faixa do Projeto.
public class TagRegistro
{
    public long Id { get; set; }

    public int ProjetoId { get; set; }
    public Projeto? Projeto { get; set; }

    public long NumeroSequencia { get; set; }
    public string? CodigoBarras { get; set; }
    public string? DadoRfid { get; set; }

    public StatusTag Status { get; set; } = StatusTag.NaoImpressa;
    public DateTime? DataImpressao { get; set; }
    public string? MensagemErro { get; set; }

    public int? EstacaoId { get; set; }
    public EstacaoImpressora? Estacao { get; set; }
}
