namespace RfidEtiquetas.Shared.Services.Auth;

// Usado fora de uma requisição HTTP (migrações do EF Core, ferramenta de
// importação do SQLite): sempre "administrador", para não aplicar o filtro
// de empresa em contexto offline/administrativo.
public class SystemCurrentUserService : ICurrentUserService
{
    public int? EmpresaId => null;
    public bool IsAdministrador => true;
}
