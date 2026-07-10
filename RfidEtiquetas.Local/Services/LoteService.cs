using NPOI.SS.UserModel;

namespace RfidEtiquetas.Services;

public class LoteService
{
    /// <summary>
    /// Gera uma sequência numérica: início, quantidade, incremento,
    /// com zeros à esquerda (dígitos), prefixo e sufixo.
    /// </summary>
    public List<string> GerarSequencia(long inicio, int quantidade, int incremento,
        int digitos, string prefixo, string sufixo)
    {
        var lista = new List<string>();
        if (quantidade < 1) return lista;
        if (incremento == 0) incremento = 1;

        long v = inicio;
        for (int i = 0; i < quantidade && i < 100000; i++)
        {
            string num = digitos > 0 ? v.ToString().PadLeft(digitos, '0') : v.ToString();
            lista.Add($"{prefixo}{num}{sufixo}");
            v += incremento;
        }
        return lista;
    }

    /// <summary>
    /// Lê uma coluna de um arquivo Excel (.xls/.xlsx) ou retorna erro.
    /// colunaIndex: 0 = coluna A, 1 = B, etc.
    /// </summary>
    public List<string> LerColunaExcel(Stream arquivo, int colunaIndex, bool temCabecalho)
    {
        var lista = new List<string>();
        var workbook = WorkbookFactory.Create(arquivo);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null) return lista;

        int inicio = temCabecalho ? 1 : 0;
        for (int i = inicio; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            var cell = row?.GetCell(colunaIndex);
            if (cell == null) continue;

            string valor = LerCelula(cell);
            if (!string.IsNullOrWhiteSpace(valor))
                lista.Add(valor.Trim());
        }
        return lista;
    }

    /// <summary>
    /// Lê uma coluna de um arquivo CSV (separado por vírgula ou ponto-e-vírgula).
    /// </summary>
    public List<string> LerColunaCsv(Stream arquivo, int colunaIndex, bool temCabecalho)
    {
        var lista = new List<string>();
        using var reader = new StreamReader(arquivo);
        string? linha;
        int n = 0;
        while ((linha = reader.ReadLine()) != null)
        {
            n++;
            if (temCabecalho && n == 1) continue;
            if (string.IsNullOrWhiteSpace(linha)) continue;

            var sep = linha.Contains(';') ? ';' : ',';
            var partes = linha.Split(sep);
            if (colunaIndex < partes.Length)
            {
                var valor = partes[colunaIndex].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(valor))
                    lista.Add(valor);
            }
        }
        return lista;
    }

    private static string LerCelula(ICell cell) => cell.CellType switch
    {
        CellType.Numeric => FormatarNumero(cell.NumericCellValue),
        CellType.String => cell.StringCellValue,
        CellType.Boolean => cell.BooleanCellValue.ToString(),
        CellType.Formula => cell.CachedFormulaResultType == CellType.Numeric
            ? FormatarNumero(cell.NumericCellValue)
            : cell.StringCellValue,
        _ => cell.ToString() ?? ""
    };

    // Evita que "123" vire "123.0"
    private static string FormatarNumero(double d)
        => d == Math.Floor(d) && !double.IsInfinity(d)
            ? ((long)d).ToString()
            : d.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
