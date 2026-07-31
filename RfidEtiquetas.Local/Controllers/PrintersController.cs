using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Services.Auth;
using RfidEtiquetas.Shared.Services.Printing;

namespace RfidEtiquetas.Local.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)]
public class PrintersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPrinterService _printer;

    public PrintersController(AppDbContext db, IPrinterService printer)
    {
        _db = db;
        _printer = printer;
    }

    /// <summary>Lista as estações da empresa da chave de API, testando a conexão de cada uma (em paralelo).</summary>
    [HttpGet("printers")]
    public async Task<IActionResult> ListPrinters()
    {
        var empresaId = int.Parse(User.FindFirst("EmpresaId")!.Value);
        var estacoes = _db.GetEstacoes().Where(e => e.EmpresaId == empresaId).ToList();

        var resultados = await Task.WhenAll(estacoes.Select(async e =>
        {
            var conexao = await Task.Run(() => _printer.TestarConexao(e));
            return new
            {
                id = e.Id,
                nome = e.NomeEstacao,
                modelo = e.ModeloImpressora.ToString(),
                online = conexao.Sucesso,
                erro = conexao.Erro
            };
        }));

        return Ok(resultados);
    }
}
