namespace RfidEtiquetas.Data.Models;

public class Parametros
{
    public int Id { get; set; }

    // Tipo de conexão com a impressora: 0 = USB/COM, 1 = Rede (Ethernet)
    public int TipoConexao { get; set; }

    // Conexão USB/COM
    public string PortaCom { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;

    // Conexão por Rede (TCP — Sato usa porta 9100 por padrão)
    public string ImpressoraIp { get; set; } = "192.168.1.100";
    public int ImpressoraPorta { get; set; } = 9100;

    // Servidor AfixData (opcional)
    public string ServidorIp { get; set; } = "";
    public string ServidorPorta { get; set; } = "";
}
