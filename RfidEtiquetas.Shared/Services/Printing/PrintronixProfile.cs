using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

// ATENÇÃO — valores ainda NÃO validados numa T6000 real, só na documentação
// pública do fabricante. A Printronix confirma emulação ZPL/ZGL com comandos
// ^RF embutidos, mas o RFID Labeling Reference Manual (ex. P220002F_RM_RFID.pdf,
// PTX_RM_RFID_178424L.pdf) mostra um exemplo "^RFW,H,0^FD...^FS" — sem o
// parâmetro de "words" e com banco "0" em vez do "2" usado pela Sato.
// Confirmar contra o manual/impressora física antes de imprimir em produção.
public class PrintronixProfile : PrinterProfile
{
    public override ModeloImpressora Modelo => ModeloImpressora.Printronix_T6000;
    public override int DotsPorMm => 8; // 203dpi assumido — confirmar o modelo real em uso
    public override int VelocidadeMin => 1;
    public override int VelocidadeMax => 10;
    public override int DensidadeMin => 0;
    public override int DensidadeMax => 30;

    public override string MontarComandoRfidWrite(int words, string hexData)
        => $"^RFW,H,0^FD{hexData}^FS";
}
