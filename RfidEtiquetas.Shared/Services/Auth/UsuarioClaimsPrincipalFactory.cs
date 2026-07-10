using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services.Auth;

// Acrescenta EmpresaId e Perfil como claims no cookie de login, para o
// HttpCurrentUserService (e o filtro de empresa do AppDbContext) não
// precisarem consultar o banco a cada requisição.
public class UsuarioClaimsPrincipalFactory : UserClaimsPrincipalFactory<Usuario>
{
    public UsuarioClaimsPrincipalFactory(
        UserManager<Usuario> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    public override async Task<ClaimsPrincipal> CreateAsync(Usuario user)
    {
        var principal = await base.CreateAsync(user);
        ((ClaimsIdentity)principal.Identity!).AddClaims(new[]
        {
            new Claim("Nome", user.Nome),
            new Claim("EmpresaId", user.EmpresaId.ToString()),
            new Claim("Perfil", user.Perfil.ToString())
        });
        return principal;
    }
}
