using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidEtiquetas.Local.Services;
using RfidEtiquetas.Shared.Services.Auth;

namespace RfidEtiquetas.Local.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)]
public class PrintController : ControllerBase
{
    private readonly PrintJobService _printJobs;

    public PrintController(PrintJobService printJobs)
    {
        _printJobs = printJobs;
    }

    // ProjetoId + NumeroSequencia são opcionais: quando informados, o job fica
    // ligado ao TagRegistro correspondente (mesmo rastreamento que o Lote.razor usa),
    // e é obrigatório que o Projeto pertença à mesma empresa da chave de API.
    public record PrintRequest(string Epc, string? Sku, string? Lote, int EstacaoId, int LayoutId, int? ProjetoId, long? NumeroSequencia);

    /// <summary>
    /// Recebe os dados de uma tag e coloca o trabalho na fila de impressão
    /// (processado de forma assíncrona pelo PrintJobProcessor). Consulte o
    /// resultado depois em GET /api/v1/jobs/{jobId}.
    /// </summary>
    [HttpPost("print")]
    public async Task<IActionResult> Print([FromBody] PrintRequest req)
    {
        var apiKeyId = int.Parse(User.FindFirst("ApiKeyId")!.Value);
        var empresaId = int.Parse(User.FindFirst("EmpresaId")!.Value);

        var resultado = await _printJobs.CriarJobAsync(
            empresaId, apiKeyId, req.Epc, req.Sku, req.Lote,
            req.EstacaoId, req.LayoutId, req.ProjetoId, req.NumeroSequencia);

        if (!resultado.Sucesso)
            return StatusCode(resultado.CodigoErro, new { erro = resultado.Erro });

        var job = resultado.Job!;
        return Accepted(new
        {
            jobId = job.Id,
            status = job.Status.ToString().ToLowerInvariant(),
            criadoEm = job.CriadoEm
        });
    }
}
