using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Printing;

namespace RfidEtiquetas.Local.Services;

/// <summary>
/// Consome a fila de PrintJob (criados via POST /api/v1/print) um de cada vez,
/// na ordem de chegada. Roda só no app Local porque é o único com acesso real
/// à impressora (IPrinterService concreto fala COM/TCP com hardware físico).
/// </summary>
public class PrintJobProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrintJobProcessor> _logger;

    public PrintJobProcessor(IServiceScopeFactory scopeFactory, ILogger<PrintJobProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processou = await ProcessarProximoAsync(stoppingToken);
                if (!processou)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // encerramento normal do app
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no PrintJobProcessor");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessarProximoAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var printer = scope.ServiceProvider.GetRequiredService<IPrinterService>();

        var job = await db.PrintJobs
            .Include(j => j.Estacao)
            .Include(j => j.EtiquetaTemplate)
            .Where(j => j.Status == StatusPrintJob.Pendente)
            .OrderBy(j => j.CriadoEm)
            .FirstOrDefaultAsync(ct);

        if (job == null) return false;

        job.Status = StatusPrintJob.Processando;
        await db.SaveChangesAsync(ct);

        try
        {
            if (job.Estacao == null || job.EtiquetaTemplate == null)
                throw new InvalidOperationException("Estação ou layout do job não encontrado.");

            var codigo = job.Sku ?? job.Epc;
            var resultado = printer.Imprimir(job.EtiquetaTemplate, codigo, job.Epc, job.Estacao);

            job.Status = resultado.Sucesso ? StatusPrintJob.Concluido : StatusPrintJob.Falhou;
            job.MensagemErro = resultado.Erro;
        }
        catch (Exception ex)
        {
            job.Status = StatusPrintJob.Falhou;
            job.MensagemErro = ex.Message;
        }

        job.ProcessadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Se o job veio ligado a um Projeto, reflete o resultado no mesmo
        // TagRegistro que o Lote.razor usa — o dashboard de Projetos passa a
        // contar também as impressões feitas via API.
        if (job.TagRegistroId is long tagRegistroId)
        {
            var statusTag = job.Status == StatusPrintJob.Concluido ? StatusTag.Impressa : StatusTag.Erro;
            db.MarcarTag(tagRegistroId, statusTag, job.EstacaoId, job.MensagemErro);
        }

        return true;
    }
}
