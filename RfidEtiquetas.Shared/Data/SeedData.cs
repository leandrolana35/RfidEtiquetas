using Microsoft.AspNetCore.Identity;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Data;

// Garante que existe pelo menos uma Empresa e um usuário Administrador para
// conseguir logar num banco novo — roda no startup dos apps, idempotente
// (não faz nada se já existir gente cadastrada).
public static class SeedData
{
    public const string AdminEmailPadrao = "admin@afixcode.com";
    public const string AdminSenhaPadrao = "admin123";

    public static async Task EnsureSeedAsync(AppDbContext db, UserManager<Usuario> userManager)
    {
        if (!db.Empresas.Any())
        {
            db.Empresas.Add(new Empresa { Nome = "Afixcode" });
            db.Empresas.Add(new Empresa { Nome = "Afixgraf" });
            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            var afixcode = db.Empresas.First(e => e.Nome == "Afixcode");
            var admin = new Usuario
            {
                UserName = AdminEmailPadrao,
                Email = AdminEmailPadrao,
                Nome = "Administrador",
                Perfil = Perfil.Administrador,
                EmpresaId = afixcode.Id
            };
            await userManager.CreateAsync(admin, AdminSenhaPadrao);
        }
    }
}
