using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Auth;

namespace RfidEtiquetas.Local.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.SchemeName)]
public class PrintController : ControllerBase
{
    private readonly AppDbContext _db;

    public PrintController(AppDbContext db)
    {
        _db = db;
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
        if (string.IsNullOrWhiteSpace(req.Epc))
            return BadRequest(new { erro = "O campo 'epc' é obrigatório." });
        if (req.EstacaoId <= 0)
            return BadRequest(new { erro = "O campo 'estacaoId' é obrigatório." });
        if (req.LayoutId <= 0)
            return BadRequest(new { erro = "O campo 'layoutId' é obrigatório." });
        if (req.ProjetoId.HasValue != req.NumeroSequencia.HasValue)
            return BadRequest(new { erro = "'projetoId' e 'numeroSequencia' devem ser informados juntos." });

        var apiKeyId = int.Parse(User.FindFirst("ApiKeyId")!.Value);
        var empresaId = int.Parse(User.FindFirst("EmpresaId")!.Value);

        var estacao = await _db.EstacoesImpressoras.FindAsync(req.EstacaoId);
        if (estacao == null)
            return BadRequest(new { erro = $"Estação {req.EstacaoId} não encontrada." });
        if (estacao.EmpresaId != empresaId)
            return StatusCode(403, new { erro = "Esta estação não pertence à empresa desta chave de API." });

        var layout = await _db.Etiquetas.FindAsync(req.LayoutId);
        if (layout == null)
            return BadRequest(new { erro = $"Layout {req.LayoutId} não encontrado." });

        long? tagRegistroId = null;
        if (req.ProjetoId is int projetoId)
        {
            // IgnoreQueryFilters: o filtro de Projeto usa ICurrentUserService (cookie),
            // que não existe numa requisição autenticada por API Key — o escopo por
            // empresa aqui é feito manualmente, comparando com a empresa da própria chave.
            var projeto = await _db.Projetos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == projetoId);
            if (projeto == null)
                return BadRequest(new { erro = $"Projeto {projetoId} não encontrado." });
            if (projeto.EmpresaId != empresaId)
                return StatusCode(403, new { erro = "Este projeto não pertence à empresa desta chave de API." });

            var tag = await _db.TagRegistros.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.ProjetoId == projetoId && t.NumeroSequencia == req.NumeroSequencia);
            if (tag == null)
                return BadRequest(new { erro = $"Sequência {req.NumeroSequencia} não existe na faixa do projeto {projetoId}." });

            tagRegistroId = tag.Id;
        }

        var job = new PrintJob
        {
            ApiKeyId = apiKeyId,
            Epc = req.Epc.Trim(),
            Sku = req.Sku,
            Lote = req.Lote,
            EstacaoId = req.EstacaoId,
            EtiquetaTemplateId = req.LayoutId,
            ProjetoId = req.ProjetoId,
            TagRegistroId = tagRegistroId
        };
        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync();

        return Accepted(new
        {
            jobId = job.Id,
            status = job.Status.ToString().ToLowerInvariant(),
            criadoEm = job.CriadoEm
        });
    }
}
