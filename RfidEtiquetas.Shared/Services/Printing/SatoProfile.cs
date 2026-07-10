using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

// Valores replicados do SatoPrinterService original (produção, testado com
// a SATO CL4NX Plus 203dpi via TCP 9100).
public class SatoProfile : PrinterProfile
{
    public override ModeloImpressora Modelo => ModeloImpressora.Sato_CL4NX;
    public override int DotsPorMm => 8; // 203dpi
    public override int VelocidadeMin => 1;
    public override int VelocidadeMax => 14;
    public override int DensidadeMin => 0;
    public override int DensidadeMax => 30;

    public override string MontarComandoRfidWrite(int words, string hexData)
        => $"^RFW,H,2,{words},1^FD{hexData}^FS";
}
