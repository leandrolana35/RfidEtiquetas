namespace RfidEtiquetas.Shared.Data.Models;

public class Projeto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";

    public int EtiquetaTemplateId { get; set; }
    public Etiqueta? EtiquetaTemplate { get; set; }

    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    // Faixa planejada/cadastrada. O que já foi de fato impresso/gravado
    // é sempre apurado a partir de TagRegistro, não de um contador separado aqui,
    // para não haver risco de desalinhamento entre os dois.
    public long SequenciaInicial { get; set; }
    public long SequenciaFinal { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public int? CriadoPorUsuarioId { get; set; }
}
