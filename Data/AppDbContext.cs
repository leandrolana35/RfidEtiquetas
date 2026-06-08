using Microsoft.Data.Sqlite;
using RfidEtiquetas.Data.Models;

namespace RfidEtiquetas.Data;

public class AppDbContext
{
    private readonly string _connectionString;

    public AppDbContext(IConfiguration config)
    {
        var dbPath = config["Database:Path"] ?? "Banco.db";
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private SqliteConnection Connect() => new(_connectionString);

    private static void AddColumnIfMissing(SqliteConnection conn, string tabela, string coluna, string definicao)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tabela})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), coluna, StringComparison.OrdinalIgnoreCase))
                return; // já existe
        }
        reader.Close();
        conn.ExecuteNonQuery($"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao}");
    }

    private void InitializeDatabase()
    {
        using var conn = Connect();
        conn.Open();
        conn.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS Etiquetas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Modelo VARCHAR(150) NOT NULL,
                EtiquetaX INT DEFAULT 0, EtiquetaY INT DEFAULT 0,
                LarguraMm INT DEFAULT 100, AlturaMm INT DEFAULT 50,
                Texto1 VARCHAR(150) DEFAULT '', Texto1X INT DEFAULT 10, Texto1Y INT DEFAULT 5,  Texto1Tam INT DEFAULT 12,
                Texto2 VARCHAR(150) DEFAULT '', Texto2X INT DEFAULT 10, Texto2Y INT DEFAULT 20, Texto2Tam INT DEFAULT 12,
                Texto3 VARCHAR(150) DEFAULT '', Texto3X INT DEFAULT 10, Texto3Y INT DEFAULT 35, Texto3Tam INT DEFAULT 12,
                Texto4 VARCHAR(150) DEFAULT '', Texto4X INT DEFAULT 10, Texto4Y INT DEFAULT 50, Texto4Tam INT DEFAULT 12,
                BarTipo INT DEFAULT 1, BarCodX INT DEFAULT 10, BarCodY INT DEFAULT 60,
                BarAltura INT DEFAULT 40, BarEspessura INT DEFAULT 2,
                BarImprimir INT DEFAULT 1, BarImprimirCodigo INT DEFAULT 1, BarFonteTam INT DEFAULT 10,
                BarPrefixo VARCHAR(10) DEFAULT '', BarSufixo VARCHAR(10) DEFAULT '', BarZerosEsquerda INT DEFAULT 0,
                RfidAtivo INT DEFAULT 1, RfidBanco INT DEFAULT 0,
                RfidTamanhoBits INT DEFAULT 96, RfidEncodingTipo INT DEFAULT 0, RfidPrefixo VARCHAR(20) DEFAULT '',
                LogoImprimir INT DEFAULT 0, LogoArquivo VARCHAR(600) DEFAULT '',
                LogoX INT DEFAULT 0, LogoY INT DEFAULT 0, LogoLargura INT DEFAULT 30, LogoAltura INT DEFAULT 30,
                Intervalo INT DEFAULT 30, Quantidade INT DEFAULT 1, Velocidade INT DEFAULT 3, Densidade INT DEFAULT 7
            );
            CREATE TABLE IF NOT EXISTS Parametros (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TipoConexao INT DEFAULT 0,
                PortaCom VARCHAR(20) DEFAULT 'COM1', BaudRate INT DEFAULT 9600,
                ImpressoraIp VARCHAR(50) DEFAULT '192.168.1.100', ImpressoraPorta INT DEFAULT 9100,
                ServidorIp VARCHAR(50) DEFAULT '', ServidorPorta VARCHAR(15) DEFAULT ''
            );
        ");

        // Migração: adiciona colunas novas se o banco já existia sem elas
        AddColumnIfMissing(conn, "Parametros", "TipoConexao", "INT DEFAULT 0");
        AddColumnIfMissing(conn, "Parametros", "ImpressoraIp", "VARCHAR(50) DEFAULT '192.168.1.100'");
        AddColumnIfMissing(conn, "Parametros", "ImpressoraPorta", "INT DEFAULT 9100");

        // Ensure at least one Parametros row
        var count = (long)(conn.ExecuteScalar("SELECT COUNT(*) FROM Parametros") ?? 0L);
        if (count == 0)
            conn.ExecuteNonQuery("INSERT INTO Parametros (PortaCom, BaudRate) VALUES ('COM1', 9600)");
    }

    public List<Etiqueta> GetEtiquetas()
    {
        using var conn = Connect();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Etiquetas ORDER BY Modelo";
        using var reader = cmd.ExecuteReader();
        var list = new List<Etiqueta>();
        while (reader.Read()) list.Add(MapEtiqueta(reader));
        return list;
    }

    public Etiqueta? GetEtiqueta(int id)
    {
        using var conn = Connect();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Etiquetas WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapEtiqueta(reader) : null;
    }

    public int SaveEtiqueta(Etiqueta e)
    {
        using var conn = Connect();
        conn.Open();
        using var cmd = conn.CreateCommand();

        if (e.Id == 0)
        {
            cmd.CommandText = @"
                INSERT INTO Etiquetas (Modelo,EtiquetaX,EtiquetaY,LarguraMm,AlturaMm,
                    Texto1,Texto1X,Texto1Y,Texto1Tam,Texto2,Texto2X,Texto2Y,Texto2Tam,
                    Texto3,Texto3X,Texto3Y,Texto3Tam,Texto4,Texto4X,Texto4Y,Texto4Tam,
                    BarTipo,BarCodX,BarCodY,BarAltura,BarEspessura,BarImprimir,BarImprimirCodigo,BarFonteTam,
                    BarPrefixo,BarSufixo,BarZerosEsquerda,
                    RfidAtivo,RfidBanco,RfidTamanhoBits,RfidEncodingTipo,RfidPrefixo,
                    LogoImprimir,LogoArquivo,LogoX,LogoY,LogoLargura,LogoAltura,
                    Intervalo,Quantidade,Velocidade,Densidade)
                VALUES ($modelo,$ex,$ey,$lmm,$amm,
                    $t1,$t1x,$t1y,$t1s,$t2,$t2x,$t2y,$t2s,$t3,$t3x,$t3y,$t3s,$t4,$t4x,$t4y,$t4s,
                    $bt,$bcx,$bcy,$ba,$be,$bi,$bic,$bft,$bp,$bs,$bz,
                    $ra,$rb,$rtb,$ret,$rp,$li,$la,$lx,$ly,$ll,$lal,
                    $int,$qtd,$vel,$den);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Etiquetas SET Modelo=$modelo,EtiquetaX=$ex,EtiquetaY=$ey,LarguraMm=$lmm,AlturaMm=$amm,
                    Texto1=$t1,Texto1X=$t1x,Texto1Y=$t1y,Texto1Tam=$t1s,
                    Texto2=$t2,Texto2X=$t2x,Texto2Y=$t2y,Texto2Tam=$t2s,
                    Texto3=$t3,Texto3X=$t3x,Texto3Y=$t3y,Texto3Tam=$t3s,
                    Texto4=$t4,Texto4X=$t4x,Texto4Y=$t4y,Texto4Tam=$t4s,
                    BarTipo=$bt,BarCodX=$bcx,BarCodY=$bcy,BarAltura=$ba,BarEspessura=$be,
                    BarImprimir=$bi,BarImprimirCodigo=$bic,BarFonteTam=$bft,
                    BarPrefixo=$bp,BarSufixo=$bs,BarZerosEsquerda=$bz,
                    RfidAtivo=$ra,RfidBanco=$rb,RfidTamanhoBits=$rtb,RfidEncodingTipo=$ret,RfidPrefixo=$rp,
                    LogoImprimir=$li,LogoArquivo=$la,LogoX=$lx,LogoY=$ly,LogoLargura=$ll,LogoAltura=$lal,
                    Intervalo=$int,Quantidade=$qtd,Velocidade=$vel,Densidade=$den
                WHERE Id=$id;
                SELECT $id;";
            cmd.Parameters.AddWithValue("$id", e.Id);
        }

        BindEtiqueta(cmd, e);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void DeleteEtiqueta(int id)
    {
        using var conn = Connect();
        conn.Open();
        conn.ExecuteNonQuery($"DELETE FROM Etiquetas WHERE Id = {id}");
    }

    public Parametros GetParametros()
    {
        using var conn = Connect();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Parametros LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new Parametros();
        return new Parametros
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            TipoConexao = reader.GetInt32(reader.GetOrdinal("TipoConexao")),
            PortaCom = reader.GetString(reader.GetOrdinal("PortaCom")),
            BaudRate = reader.GetInt32(reader.GetOrdinal("BaudRate")),
            ImpressoraIp = reader.GetString(reader.GetOrdinal("ImpressoraIp")),
            ImpressoraPorta = reader.GetInt32(reader.GetOrdinal("ImpressoraPorta")),
            ServidorIp = reader.GetString(reader.GetOrdinal("ServidorIp")),
            ServidorPorta = reader.GetString(reader.GetOrdinal("ServidorPorta"))
        };
    }

    public void SaveParametros(Parametros p)
    {
        using var conn = Connect();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Parametros SET
            TipoConexao=$tc, PortaCom=$pc, BaudRate=$br,
            ImpressoraIp=$ip, ImpressoraPorta=$ipp,
            ServidorIp=$sip, ServidorPorta=$sp WHERE Id=$id";
        cmd.Parameters.AddWithValue("$tc", p.TipoConexao);
        cmd.Parameters.AddWithValue("$pc", p.PortaCom);
        cmd.Parameters.AddWithValue("$br", p.BaudRate);
        cmd.Parameters.AddWithValue("$ip", p.ImpressoraIp);
        cmd.Parameters.AddWithValue("$ipp", p.ImpressoraPorta);
        cmd.Parameters.AddWithValue("$sip", p.ServidorIp);
        cmd.Parameters.AddWithValue("$sp", p.ServidorPorta);
        cmd.Parameters.AddWithValue("$id", p.Id);
        cmd.ExecuteNonQuery();
    }

    private static Etiqueta MapEtiqueta(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        Modelo = r.GetString(r.GetOrdinal("Modelo")),
        EtiquetaX = r.GetInt32(r.GetOrdinal("EtiquetaX")),
        EtiquetaY = r.GetInt32(r.GetOrdinal("EtiquetaY")),
        LarguraMm = r.GetInt32(r.GetOrdinal("LarguraMm")),
        AlturaMm = r.GetInt32(r.GetOrdinal("AlturaMm")),
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
        RfidPrefixo = r.GetString(r.GetOrdinal("RfidPrefixo")),
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

    private static void BindEtiqueta(SqliteCommand cmd, Etiqueta e)
    {
        cmd.Parameters.AddWithValue("$modelo", e.Modelo);
        cmd.Parameters.AddWithValue("$ex", e.EtiquetaX);
        cmd.Parameters.AddWithValue("$ey", e.EtiquetaY);
        cmd.Parameters.AddWithValue("$lmm", e.LarguraMm);
        cmd.Parameters.AddWithValue("$amm", e.AlturaMm);
        cmd.Parameters.AddWithValue("$t1", e.Texto1); cmd.Parameters.AddWithValue("$t1x", e.Texto1X); cmd.Parameters.AddWithValue("$t1y", e.Texto1Y); cmd.Parameters.AddWithValue("$t1s", e.Texto1Tam);
        cmd.Parameters.AddWithValue("$t2", e.Texto2); cmd.Parameters.AddWithValue("$t2x", e.Texto2X); cmd.Parameters.AddWithValue("$t2y", e.Texto2Y); cmd.Parameters.AddWithValue("$t2s", e.Texto2Tam);
        cmd.Parameters.AddWithValue("$t3", e.Texto3); cmd.Parameters.AddWithValue("$t3x", e.Texto3X); cmd.Parameters.AddWithValue("$t3y", e.Texto3Y); cmd.Parameters.AddWithValue("$t3s", e.Texto3Tam);
        cmd.Parameters.AddWithValue("$t4", e.Texto4); cmd.Parameters.AddWithValue("$t4x", e.Texto4X); cmd.Parameters.AddWithValue("$t4y", e.Texto4Y); cmd.Parameters.AddWithValue("$t4s", e.Texto4Tam);
        cmd.Parameters.AddWithValue("$bt", e.BarTipo); cmd.Parameters.AddWithValue("$bcx", e.BarCodX); cmd.Parameters.AddWithValue("$bcy", e.BarCodY);
        cmd.Parameters.AddWithValue("$ba", e.BarAltura); cmd.Parameters.AddWithValue("$be", e.BarEspessura);
        cmd.Parameters.AddWithValue("$bi", e.BarImprimir ? 1 : 0); cmd.Parameters.AddWithValue("$bic", e.BarImprimirCodigo ? 1 : 0);
        cmd.Parameters.AddWithValue("$bft", e.BarFonteTam);
        cmd.Parameters.AddWithValue("$bp", e.BarPrefixo); cmd.Parameters.AddWithValue("$bs", e.BarSufixo); cmd.Parameters.AddWithValue("$bz", e.BarZerosEsquerda);
        cmd.Parameters.AddWithValue("$ra", e.RfidAtivo ? 1 : 0); cmd.Parameters.AddWithValue("$rb", e.RfidBanco);
        cmd.Parameters.AddWithValue("$rtb", e.RfidTamanhoBits); cmd.Parameters.AddWithValue("$ret", e.RfidEncodingTipo); cmd.Parameters.AddWithValue("$rp", e.RfidPrefixo);
        cmd.Parameters.AddWithValue("$li", e.LogoImprimir ? 1 : 0); cmd.Parameters.AddWithValue("$la", e.LogoArquivo);
        cmd.Parameters.AddWithValue("$lx", e.LogoX); cmd.Parameters.AddWithValue("$ly", e.LogoY); cmd.Parameters.AddWithValue("$ll", e.LogoLargura); cmd.Parameters.AddWithValue("$lal", e.LogoAltura);
        cmd.Parameters.AddWithValue("$int", e.Intervalo); cmd.Parameters.AddWithValue("$qtd", e.Quantidade);
        cmd.Parameters.AddWithValue("$vel", e.Velocidade); cmd.Parameters.AddWithValue("$den", e.Densidade);
    }
}

internal static class SqliteExtensions
{
    public static void ExecuteNonQuery(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public static object? ExecuteScalar(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }
}
