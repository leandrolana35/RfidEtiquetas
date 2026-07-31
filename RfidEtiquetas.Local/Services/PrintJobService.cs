using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services;

namespace RfidEtiquetas.Local.Services;

public record CriarJobResultado(bool Sucesso, string? Erro, int CodigoErro, PrintJob? Job);

/// <summary>
/// Validação + criação de PrintJob, compartilhada entre a API REST
/// (PrintController) e a tool MCP (print_rfid_tag) — as duas superfícies
/// de integração externa devem se comportar exatamente igual.
/// </summary>
public class PrintJobService
{
    private readonly AppDbContext _db;
    private readonly IWebhookDispatcher _webhooks;

    public PrintJobService(AppDbContext db, IWebhookDispatcher webhooks)
    {
        _db = db;
        _webhooks = webhooks;
    }

    public async Task<CriarJobResultado> CriarJobAsync(
        int empresaId, int apiKeyId,
        string epc, string? sku, string? lote,
        int estacaoId, int layoutId,
        int? projetoId, long? numeroSequencia)
    {
        if (string.IsNullOrWhiteSpace(epc))
            return new(false, "O campo 'epc' é obrigatório.", 400, null);
        if (estacaoId <= 0)
            return new(false, "O campo 'estacaoId' é obrigatório.", 400, null);
        if (layoutId <= 0)
            return new(false, "O campo 'layoutId' é obrigatório.", 400, null);
        if (projetoId.HasValue != numeroSequencia.HasValue)
            return new(false, "'projetoId' e 'numeroSequencia' devem ser informados juntos.", 400, null);

        var estacao = await _db.EstacoesImpressoras.FindAsync(estacaoId);
        if (estacao == null)
            return new(false, $"Estação {estacaoId} não encontrada.", 400, null);
        if (estacao.EmpresaId != empresaId)
            return new(false, "Esta estação não pertence à empresa desta chave de API.", 403, null);

        var layout = await _db.Etiquetas.FindAsync(layoutId);
        if (layout == null)
            return new(false, $"Layout {layoutId} não encontrado.", 400, null);

        long? tagRegistroId = null;
        if (projetoId is int pid)
        {
            // IgnoreQueryFilters: o filtro de Projeto usa ICurrentUserService (cookie),
            // que não existe numa requisição autenticada por API Key/MCP — o escopo por
            // empresa aqui é feito manualmente, comparando com a empresa da própria chave.
            var projeto = await _db.Projetos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pid);
            if (projeto == null)
                return new(false, $"Projeto {pid} não encontrado.", 400, null);
            if (projeto.EmpresaId != empresaId)
                return new(false, "Este projeto não pertence à empresa desta chave de API.", 403, null);

            var tag = await _db.TagRegistros.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.ProjetoId == pid && t.NumeroSequencia == numeroSequencia);
            if (tag == null)
                return new(false, $"Sequência {numeroSequencia} não existe na faixa do projeto {pid}.", 400, null);

            tagRegistroId = tag.Id;
        }

        var job = new PrintJob
        {
            ApiKeyId = apiKeyId,
            Epc = epc.Trim(),
            Sku = sku,
            Lote = lote,
            EstacaoId = estacaoId,
            EtiquetaTemplateId = layoutId,
            ProjetoId = projetoId,
            TagRegistroId = tagRegistroId
        };
        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync();

        _webhooks.EnqueueEvent(WebhookEventos.JobCreated, empresaId, new
        {
            evento = WebhookEventos.JobCreated,
            jobId = job.Id,
            epc = job.Epc,
            sku = job.Sku,
            lote = job.Lote,
            estacaoId = job.EstacaoId,
            layoutId = job.EtiquetaTemplateId,
            projetoId = job.ProjetoId,
            criadoEm = job.CriadoEm
        });

        return new(true, null, 202, job);
    }
}
