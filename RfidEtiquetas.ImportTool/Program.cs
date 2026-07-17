using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Auth;

// Importa os dados do Banco.db (SQLite) antigo — modelos de etiqueta e a
// configuração de impressora — para o banco PostgreSQL novo.
// Uso: dotnet run --project RfidEtiquetas.ImportTool -- <caminho-do-Banco.db> <connection-string-postgres>

if (args.Length < 2)
{
    Console.WriteLine("Uso: dotnet run --project RfidEtiquetas.ImportTool -- <caminho-do-Banco.db> <connection-string-postgres>");
    Console.WriteLine("Exemplo: dotnet run --project RfidEtiquetas.ImportTool -- \"C:\\...\\Banco.db\" \"Host=...;Database=rfidetiquetas;Username=...;Password=...\"");
    return 1;
}

var sqlitePath = args[0];
var pgConnectionString = args[1];

if (!File.Exists(sqlitePath))
{
    Console.WriteLine($"Arquivo não encontrado: {sqlitePath}");
    return 1;
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(pgConnectionString)
    .Options;

// SystemCurrentUserService = sempre "administrador", para o import não ser
// bloqueado pelo filtro de empresa (não faz sentido fora de uma requisição HTTP).
using var db = new AppDbContext(options, new SystemCurrentUserService());

Console.WriteLine("Aplicando migrações pendentes no banco de destino...");
db.Database.Migrate();

using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
sqlite.Open();

var etiquetasImportadas = 0;
var etiquetasIgnoradas = 0;

using (var cmd = sqlite.CreateCommand())
{
    cmd.CommandText = "SELECT * FROM Etiquetas";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var nome = reader.GetString(reader.GetOrdinal("Modelo"));
        if (db.Etiquetas.Any(e => e.Modelo == nome))
        {
            Console.WriteLine($"  Ignorado (já existe no destino): {nome}");
            etiquetasIgnoradas++;
            continue;
        }

        db.Etiquetas.Add(MapEtiqueta(reader));
        etiquetasImportadas++;
    }
}
db.SaveChanges();
Console.WriteLine($"Modelos de etiqueta importados: {etiquetasImportadas} (ignorados por já existir: {etiquetasIgnoradas})");

using (var cmd = sqlite.CreateCommand())
{
    cmd.CommandText = "SELECT * FROM Parametros LIMIT 1";
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        var estacao = new EstacaoImpressora
        {
            NomeEstacao = "Estação importada",
            ModeloImpressora = ModeloImpressora.Sato_CL4NX, // era o único modelo suportado antes da migração
            TipoConexao = reader.GetInt32(reader.GetOrdinal("TipoConexao")) == 1 ? TipoConexaoImpressora.Rede : TipoConexaoImpressora.Com,
            PortaCom = reader.GetString(reader.GetOrdinal("PortaCom")),
            BaudRate = reader.GetInt32(reader.GetOrdinal("BaudRate")),
            ImpressoraIp = reader.GetString(reader.GetOrdinal("ImpressoraIp")),
            ImpressoraPorta = reader.GetInt32(reader.GetOrdinal("ImpressoraPorta"))
        };
        db.EstacoesImpressoras.Add(estacao);
        db.SaveChanges();
        Console.WriteLine($"Configuração de impressora importada como \"{estacao.NomeEstacao}\".");
    }
    else
    {
        Console.WriteLine("Nenhuma configuração de impressora encontrada no SQLite de origem.");
    }
}

Console.WriteLine("Importação concluída.");
return 0;

static Etiqueta MapEtiqueta(SqliteDataReader r) => new()
{
    Modelo = r.GetString(r.GetOrdinal("Modelo")),
    EtiquetaX = r.GetInt32(r.GetOrdinal("EtiquetaX")),
    EtiquetaY = r.GetInt32(r.GetOrdinal("EtiquetaY")),
    LarguraMm = r.GetInt32(r.GetOrdinal("LarguraMm")),
    AlturaMm = r.GetInt32(r.GetOrdinal("AlturaMm")),
    DefinirTamanho = r.GetInt32(r.GetOrdinal("DefinirTamanho")) == 1,
    Texto1 = r.GetString(r.GetOrdinal("Texto1")),
    Texto1X = r.GetInt32(r.GetOrdinal("Texto1X")),
    Texto1Y = r.GetInt32(r.GetOrdinal("Texto1Y")),
    Texto1Tam = r.GetInt32(r.GetOrdinal("Texto1Tam")),
    Texto2 = r.GetString(r.GetOrdinal("Texto2")),
    Texto2X = r.GetInt32(r.GetOrdinal("Texto2X")),
    Texto2Y = r.GetInt32(r.GetOrdinal("Texto2Y")),
    Texto2Tam = r.GetInt32(r.GetOrdinal("Texto2Tam")),
    Texto3 = r.GetString(r.GetOrdinal("Texto3")),
    Texto3X = r.GetInt32(r.GetOrdinal("Texto3X")),
    Texto3Y = r.GetInt32(r.GetOrdinal("Texto3Y")),
    Texto3Tam = r.GetInt32(r.GetOrdinal("Texto3Tam")),
    Texto4 = r.GetString(r.GetOrdinal("Texto4")),
    Texto4X = r.GetInt32(r.GetOrdinal("Texto4X")),
    Texto4Y = r.GetInt32(r.GetOrdinal("Texto4Y")),
    Texto4Tam = r.GetInt32(r.GetOrdinal("Texto4Tam")),
    BarTipo = r.GetInt32(r.GetOrdinal("BarTipo")),
    BarCodX = r.GetInt32(r.GetOrdinal("BarCodX")),
    BarCodY = r.GetInt32(r.GetOrdinal("BarCodY")),
    BarAltura = r.GetInt32(r.GetOrdinal("BarAltura")),
    BarEspessura = r.GetInt32(r.GetOrdinal("BarEspessura")),
    BarImprimir = r.GetInt32(r.GetOrdinal("BarImprimir")) == 1,
    BarImprimirCodigo = r.GetInt32(r.GetOrdinal("BarImprimirCodigo")) == 1,
    BarFonteTam = r.GetInt32(r.GetOrdinal("BarFonteTam")),
    BarPrefixo = r.GetString(r.GetOrdinal("BarPrefixo")),
    BarSufixo = r.GetString(r.GetOrdinal("BarSufixo")),
    BarZerosEsquerda = r.GetInt32(r.GetOrdinal("BarZerosEsquerda")),
    RfidAtivo = r.GetInt32(r.GetOrdinal("RfidAtivo")) == 1,
    RfidBanco = r.GetInt32(r.GetOrdinal("RfidBanco")),
    RfidTamanhoBits = r.GetInt32(r.GetOrdinal("RfidTamanhoBits")),
    RfidEncodingTipo = r.GetInt32(r.GetOrdinal("RfidEncodingTipo")),
    RfidAlinhamento = r.GetInt32(r.GetOrdinal("RfidAlinhamento")),
    RfidPrefixo = r.GetString(r.GetOrdinal("RfidPrefixo")),
    RfidImprimirValor = r.GetInt32(r.GetOrdinal("RfidImprimirValor")) == 1,
    RfidValorX = r.GetInt32(r.GetOrdinal("RfidValorX")),
    RfidValorY = r.GetInt32(r.GetOrdinal("RfidValorY")),
    RfidValorTam = r.GetInt32(r.GetOrdinal("RfidValorTam")),
    LogoImprimir = r.GetInt32(r.GetOrdinal("LogoImprimir")) == 1,
    LogoArquivo = r.GetString(r.GetOrdinal("LogoArquivo")),
    LogoX = r.GetInt32(r.GetOrdinal("LogoX")),
    LogoY = r.GetInt32(r.GetOrdinal("LogoY")),
    LogoLargura = r.GetInt32(r.GetOrdinal("LogoLargura")),
    LogoAltura = r.GetInt32(r.GetOrdinal("LogoAltura")),
    Intervalo = r.GetInt32(r.GetOrdinal("Intervalo")),
    Quantidade = r.GetInt32(r.GetOrdinal("Quantidade")),
    Velocidade = r.GetInt32(r.GetOrdinal("Velocidade")),
    Densidade = r.GetInt32(r.GetOrdinal("Densidade"))
};
