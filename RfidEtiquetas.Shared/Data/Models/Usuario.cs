using Microsoft.AspNetCore.Identity;

namespace RfidEtiquetas.Shared.Data.Models;

public enum Perfil
{
    Administrador = 0,
    Usuario = 1
}

// Email/UserName/PasswordHash vêm do IdentityUser (hash e regras de senha
// já resolvidos pelo Identity — não reinventar isso aqui).
public class Usuario : IdentityUser<int>
{
    public string Nome { get; set; } = "";
    public Perfil Perfil { get; set; } = Perfil.Usuario;
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
