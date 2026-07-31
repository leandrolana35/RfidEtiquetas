using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using RfidEtiquetas.Local.Services;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Printing;

namespace RfidEtiquetas.Local.Mcp;

// Tools expostas para agentes de IA via MCP (mesma autenticação X-API-Key da
// API REST — ver ApiKeyAuthHandler e o mapeamento do endpoint /mcp em Program.cs).
[McpServerToolType]
public class RfidTools
{
    private readonly AppDbContext _db;
    private readonly IPrinterService _printer;
    private readonly PrintJobService _printJobs;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RfidTools(AppDbContext db, IPrinterService printer, PrintJobService printJobs, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _printer = printer;
        _printJobs = printJobs;
        _httpContextAccessor = httpContextAccessor;
    }

    private (int EmpresaId, int ApiKeyId) IdentidadeAtual()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var empresaClaim = user?.FindFirst("EmpresaId")?.Value;
        var apiKeyClaim = user?.FindFirst("ApiKeyId")?.Value;
        if (empresaClaim == null || apiKeyClaim == null)
            throw new InvalidOperationException("Requisição MCP sem autenticação por API Key válida.");
        return (int.Parse(empresaClaim), int.Parse(apiKeyClaim));
    }

    private class LabelData
    {
        public int? LayoutId { get; set; }
        public string? Sku { get; set; }
        public string? Lote { get; set; }
        public int? ProjetoId { get; set; }
        public long? NumeroSequencia { get; set; }
    }

    [McpServerTool(Name = "print_rfid_tag")]
    [Description("Coloca uma etiqueta RFID na fila de impressão. Retorna o jobId para consulta posterior.")]
    public async Task<object> PrintRfidTag(
        [Description("Código EPC a ser gravado na tag")] string epc,
        [Description("JSON com dados da etiqueta. Chaves aceitas: layoutId (obrigatório, id do modelo/layout), sku, lote, projetoId, numeroSequencia (os dois últimos juntos, para ligar ao rastreamento de um Projeto)")] string label_data,
        [Description("Id da estação/impressora de destino")] int printer_id)
    {
        var (empresaId, apiKeyId) = IdentidadeAtual();

        LabelData dados;
        try
        {
            dados = string.IsNullOrWhiteSpace(label_data)
                ? new LabelData()
                : JsonSerializer.Deserialize<LabelData>(label_data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new LabelData();
        }
        catch (JsonException ex)
        {
            return new { erro = $"label_data não é um JSON válido: {ex.Message}" };
        }

        if (dados.LayoutId is not int layoutId)
            return new { erro = "label_data precisa conter a chave 'layoutId' com o id do modelo de etiqueta." };

        var resultado = await _printJobs.CriarJobAsync(
            empresaId, apiKeyId, epc, dados.Sku, dados.Lote,
            printer_id, layoutId, dados.ProjetoId, dados.NumeroSequencia);

        if (!resultado.Sucesso)
            return new { erro = resultado.Erro };

        var job = resultado.Job!;
        return new { jobId = job.Id, status = job.Status.ToString().ToLowerInvariant(), criadoEm = job.CriadoEm };
    }

    [McpServerTool(Name = "check_printer_status")]
    [Description("Verifica se uma impressora (ZPL/TSC) está pronta ou em erro, testando a conexão de verdade.")]
    public async Task<object> CheckPrinterStatus(
        [Description("Id da estação/impressora")] int printer_id)
    {
        var (empresaId, _) = IdentidadeAtual();

        var estacao = _db.GetEstacao(printer_id);
        if (estacao == null)
            return new { erro = $"Estação {printer_id} não encontrada." };
        if (estacao.EmpresaId != empresaId)
            return new { erro = "Esta estação não pertence à empresa desta chave de API." };

        var conexao = await Task.Run(() => _printer.TestarConexao(estacao));
        return new
        {
            printerId = estacao.Id,
            nome = estacao.NomeEstacao,
            modelo = estacao.ModeloImpressora.ToString(),
            pronta = conexao.Sucesso,
            erro = conexao.Erro
        };
    }

    [McpServerTool(Name = "search_printed_tags")]
    [Description("Consulta o histórico de tags impressas/geradas via API (não inclui impressões feitas manualmente pela tela Imprimir/Lote).")]
    public object SearchPrintedTags(
        [Description("Filtrar por EPC — busca parcial (opcional)")] string? epc = null,
        [Description("Filtrar por SKU — busca parcial (opcional)")] string? sku = null,
        [Description("Filtrar por status: pendente, processando, concluido ou falhou (opcional)")] string? status = null,
        [Description("Máximo de resultados (padrão 50, máximo 200)")] int limit = 50)
    {
        var (empresaId, _) = IdentidadeAtual();

        var query = _db.PrintJobs.Include(j => j.ApiKey).Where(j => j.ApiKey!.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(epc)) query = query.Where(j => j.Epc.Contains(epc));
        if (!string.IsNullOrWhiteSpace(sku)) query = query.Where(j => j.Sku != null && j.Sku.Contains(sku));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusPrintJob>(status, ignoreCase: true, out var statusEnum))
            query = query.Where(j => j.Status == statusEnum);

        return query
            .OrderByDescending(j => j.CriadoEm)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(j => new
            {
                jobId = j.Id,
                epc = j.Epc,
                sku = j.Sku,
                lote = j.Lote,
                status = j.Status.ToString().ToLowerInvariant(),
                criadoEm = j.CriadoEm,
                processadoEm = j.ProcessadoEm
            })
            .ToList();
    }
}
