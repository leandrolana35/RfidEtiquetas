using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace RfidEtiquetas.Services;

public class BarcodeService
{
    public string GerarBase64(string conteudo, int tipo, int largura = 300, int altura = 100)
    {
        if (string.IsNullOrWhiteSpace(conteudo)) return "";

        try
        {
            var format = MapFormato(tipo);
            var writer = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = largura,
                    Height = altura,
                    Margin = 5,
                    PureBarcode = false
                }
            };

            using var bitmap = writer.Write(conteudo);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);

            return "data:image/png;base64," + Convert.ToBase64String(data.ToArray());
        }
        catch
        {
            return "";
        }
    }

    private static BarcodeFormat MapFormato(int tipo) => tipo switch
    {
        1 => BarcodeFormat.CODE_128,
        2 => BarcodeFormat.EAN_13,
        3 => BarcodeFormat.CODE_39,
        4 => BarcodeFormat.QR_CODE,
        5 => BarcodeFormat.CODE_93,
        _ => BarcodeFormat.CODE_128
    };
}
