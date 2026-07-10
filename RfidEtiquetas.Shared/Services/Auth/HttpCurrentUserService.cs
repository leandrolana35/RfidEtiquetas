using Microsoft.AspNetCore.Http;

namespace RfidEtiquetas.Shared.Services.Auth;

// Lê EmpresaId/Perfil das claims do cookie de login (ver UsuarioClaimsPrincipalFactory),
// evitando ir ao banco a cada consulta só para saber quem está logado.
public class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int? EmpresaId
    {
        get
        {
            var valor = _accessor.HttpContext?.User?.FindFirst("EmpresaId")?.Value;
            return int.TryParse(valor, out var id) ? id : null;
        }
    }

    public bool IsAdministrador
        => _accessor.HttpContext?.User?.FindFirst("Perfil")?.Value == "Administrador";
}
