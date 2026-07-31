namespace RfidEtiquetas.Shared.Data.Models;

// A chave real só existe em texto puro no momento da criação (mostrada uma
// única vez ao usuário) — aqui só guardamos o hash (SHA-256) e um prefixo
// curto para identificação visual nas listagens ("afx_a1b2c3...").
public class ApiKey
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string ChaveHash { get; set; } = "";
    public string ChavePrefixo { get; set; } = "";

    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoUsoEm { get; set; }
}
