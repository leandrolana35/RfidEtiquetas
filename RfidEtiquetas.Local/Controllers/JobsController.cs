using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Services.Auth;

namespace RfidEtiquetas.Local.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var empresaId = int.Parse(User.FindFirst("EmpresaId")!.Value);

        var job = await _db.PrintJobs
            .Include(j => j.ApiKey)
            .Include(j => j.Estacao)
            .Include(j => j.EtiquetaTemplate)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        // 404 (não 403) se o job existe mas é de outra empresa — evita confirmar
        // pra quem não devia a existência do id.
        if (job == null || job.ApiKey?.EmpresaId != empresaId)
            return NotFound(new { erro = "Job não encontrado." });

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToString().ToLowerInvariant(),
            epc = job.Epc,
            sku = job.Sku,
            lote = job.Lote,
            estacao = job.Estacao?.NomeEstacao,
            layout = job.EtiquetaTemplate?.Modelo,
            projetoId = job.ProjetoId,
            erro = job.MensagemErro,
            criadoEm = job.CriadoEm,
            processadoEm = job.ProcessadoEm
        });
    }
}
