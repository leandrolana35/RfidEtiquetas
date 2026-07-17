using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Printing;

// Isola as diferenças entre modelos de impressora (DPI, faixa de velocidade,
// dialeto do comando de gravação RFID) para o ZplBuilder poder gerar o
// mesmo tipo de etiqueta nos dois modelos sem duplicar a lógica inteira.
public abstract class PrinterProfile
{
    public abstract ModeloImpressora Modelo { get; }
    public abstract int DotsPorMm { get; }
    public abstract int VelocidadeMin { get; }
    public abstract int VelocidadeMax { get; }
    public abstract int DensidadeMin { get; }
    public abstract int DensidadeMax { get; }

    // words = tamanho do EPC em palavras de 16 bits (96 bits = 6 words).
    public abstract string MontarComandoRfidWrite(int words, string hexData);

    public static PrinterProfile Para(ModeloImpressora modelo) => modelo switch
    {
        ModeloImpressora.Printronix_T6000 => new PrintronixProfile(),
        _ => new SatoProfile()
    };
}
