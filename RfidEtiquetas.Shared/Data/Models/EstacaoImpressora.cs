namespace RfidEtiquetas.Shared.Data.Models;

public enum ModeloImpressora
{
    Sato_CL4NX = 0,
    Printronix_T6000 = 1
}

public enum TipoConexaoImpressora
{
    Com = 0,
    Rede = 1
}

// Substitui a antiga tabela Parametros (linha única) por um registro por
// posto físico de impressão, já que cada local pode ter um modelo diferente.
public class EstacaoImpressora
{
    public int Id { get; set; }
    public string NomeEstacao { get; set; } = "";
    public ModeloImpressora ModeloImpressora { get; set; } = ModeloImpressora.Sato_CL4NX;

    // A quem essa impressora pertence — usado para restringir o que a API
    // (por chave) e usuários não-administradores enxergam/podem usar.
    // Nullable: estações antigas (de antes desta coluna existir) ficam sem
    // dono até um Administrador editar e atribuir — ficam invisíveis pra API
    // e para usuários não-admin nesse meio tempo, o que é o comportamento seguro.
    public int? EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    public TipoConexaoImpressora TipoConexao { get; set; } = TipoConexaoImpressora.Rede;

    public string PortaCom { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;

    public string ImpressoraIp { get; set; } = "192.168.1.100";
    public int ImpressoraPorta { get; set; } = 9100;

    // Só usado se o modelo/impressora física tiver DPI diferente do padrão do perfil.
    public int? DpiOverride { get; set; }
}
