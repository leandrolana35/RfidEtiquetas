using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RfidEtiquetas.Shared.Data;

namespace RfidEtiquetas.Shared.Services.Auth;

public class ApiKeyAuthOptions : AuthenticationSchemeOptions
{
}

// Autenticação por cabeçalho X-API-Key, separada do login por cookie usado
// no restante do app — as rotas /api/v1/* usam
// [Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)] em vez
// do esquema padrão de cookie do Identity.
public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";

    private readonly AppDbContext _db;

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var valores) || string.IsNullOrWhiteSpace(valores.ToString()))
            return AuthenticateResult.Fail($"Cabeçalho {HeaderName} ausente.");

        var chave = valores.ToString();
        var hash = ApiKeyService.Hash(chave);

        var apiKey = await _db.ApiKeys.Include(k => k.Empresa)
            .FirstOrDefaultAsync(k => k.ChaveHash == hash && k.Ativo);

        if (apiKey == null)
            return AuthenticateResult.Fail("Chave de API inválida ou revogada.");

        apiKey.UltimoUsoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim("ApiKeyId", apiKey.Id.ToString()),
            new Claim("EmpresaId", apiKey.EmpresaId.ToString()),
            new Claim(ClaimTypes.Name, apiKey.Nome)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
