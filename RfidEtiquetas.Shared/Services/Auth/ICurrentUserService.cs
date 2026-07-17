namespace RfidEtiquetas.Shared.Services.Auth;

// Abstrai "quem está logado agora" para o filtro de empresa no AppDbContext.
// Implementado com HttpContext nos apps Web/Local, e por uma versão fixa
// (sempre administrador) em ferramentas offline como o importador do SQLite.
public interface ICurrentUserService
{
    int? EmpresaId { get; }
    bool IsAdministrador { get; }
}
