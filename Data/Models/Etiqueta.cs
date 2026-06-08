namespace RfidEtiquetas.Data.Models;

public class Etiqueta
{
    public int Id { get; set; }
    public string Modelo { get; set; } = "";

    // Label position
    public int EtiquetaX { get; set; }
    public int EtiquetaY { get; set; }
    public int LarguraMm { get; set; } = 100;
    public int AlturaMm { get; set; } = 50;

    // Se false (padrão): NÃO envia ^PW/^LL — usa a calibração física da impressora.
    // Evita sobrescrever o passo (pitch) já calibrado no aparelho.
    public bool DefinirTamanho { get; set; }

    // Text fields (up to 4)
    public string Texto1 { get; set; } = "";
    public int Texto1X { get; set; }
    public int Texto1Y { get; set; }
    public int Texto1Tam { get; set; } = 12;

    public string Texto2 { get; set; } = "";
    public int Texto2X { get; set; }
    public int Texto2Y { get; set; }
    public int Texto2Tam { get; set; } = 12;

    public string Texto3 { get; set; } = "";
    public int Texto3X { get; set; }
    public int Texto3Y { get; set; }
    public int Texto3Tam { get; set; } = 12;

    public string Texto4 { get; set; } = "";
    public int Texto4X { get; set; }
    public int Texto4Y { get; set; }
    public int Texto4Tam { get; set; } = 12;

    // Barcode
    public int BarTipo { get; set; } = 1;     // 1=Code128 2=EAN13 3=Code39 4=QR
    public int BarCodX { get; set; }
    public int BarCodY { get; set; }
    public int BarAltura { get; set; } = 40;
    public int BarEspessura { get; set; } = 2;
    public bool BarImprimir { get; set; } = true;
    public bool BarImprimirCodigo { get; set; } = true;
    public int BarFonteTam { get; set; } = 10;
    public string BarPrefixo { get; set; } = "";
    public string BarSufixo { get; set; } = "";
    public int BarZerosEsquerda { get; set; }

    // RFID settings
    public bool RfidAtivo { get; set; } = true;
    public int RfidBanco { get; set; }         // 0=EPC 1=User Memory
    public int RfidTamanhoBits { get; set; } = 96;  // 96, 128 ou 256 bits
    public int RfidEncodingTipo { get; set; }  // 0=ASCII 1=Hex direto
    public string RfidPrefixo { get; set; } = "";

    // Imprimir o valor gravado no RFID como texto visível na etiqueta
    public bool RfidImprimirValor { get; set; }
    public int RfidValorX { get; set; } = 50;
    public int RfidValorY { get; set; } = 50;
    public int RfidValorTam { get; set; } = 40;

    // Logo
    public bool LogoImprimir { get; set; }
    public string LogoArquivo { get; set; } = "";
    public int LogoX { get; set; }
    public int LogoY { get; set; }
    public int LogoLargura { get; set; } = 30;
    public int LogoAltura { get; set; } = 30;

    // Print settings
    public int Intervalo { get; set; } = 30;
    public int Quantidade { get; set; } = 1;
    public int Velocidade { get; set; } = 3;   // 1-10
    public int Densidade { get; set; } = 7;    // 1-15
}
