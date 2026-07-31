using System.Text;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services;

namespace RfidEtiquetas.Local.Services;

/// <summary>
/// Consome a fila de WebhookDelivery e envia o POST assinado (HMAC-SHA256) para
/// a URL cadastrada. Em caso de falha (erro de rede ou resposta não-2xx),
/// reagenda com backoff exponencial até esgotar as tentativas.
/// </summary>
public class WebhookDeliveryProcessor : BackgroundService
{
    // Backoff exponencial: 30s, 2min, 10min, 30min, 2h — depois disso, desiste (Esgotado).
    private static readonly TimeSpan[] Backoff =
    {
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2)
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryProcessor> _logger;

    public WebhookDeliveryProcessor(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processou = await ProcessarProximaAsync(stoppingToken);
                if (!processou)
                    await Task.Delay(2000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // encerramento normal do app
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no WebhookDeliveryProcessor");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessarProximaAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entrega = await db.WebhookDeliveries
            .Include(d => d.WebhookSubscription)
            .Where(d => d.Status == StatusWebhookDelivery.Pendente && d.ProximaTentativaEm <= DateTime.UtcNow)
            .OrderBy(d => d.ProximaTentativaEm)
            .FirstOrDefaultAsync(ct);

        if (entrega == null || entrega.WebhookSubscription == null) return false;

        var assinatura = WebhookSigner.Assinar(entrega.WebhookSubscription.Secret, entrega.PayloadJson);
        var http = _httpClientFactory.CreateClient("webhooks");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, entrega.WebhookSubscription.Url)
            {
                Content = new StringContent(entrega.PayloadJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Webhook-Event", entrega.Evento);
            request.Headers.Add("X-Webhook-Id", entrega.Id.ToString());
            request.Headers.Add("X-Webhook-Signature", $"sha256={assinatura}");

            using var resposta = await http.SendAsync(request, ct);
            entrega.Tentativas++;
            entrega.UltimoStatusCode = (int)resposta.StatusCode;

            if (resposta.IsSuccessStatusCode)
            {
                entrega.Status = StatusWebhookDelivery.Sucesso;
                entrega.EntregueEm = DateTime.UtcNow;
                entrega.UltimoErro = null;
            }
            else
            {
                Reagendar(entrega, $"Resposta HTTP {(int)resposta.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            entrega.Tentativas++;
            Reagendar(entrega, ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void Reagendar(WebhookDelivery entrega, string erro)
    {
        entrega.UltimoErro = erro;
        int indice = entrega.Tentativas - 1;
        if (indice >= Backoff.Length)
        {
            entrega.Status = StatusWebhookDelivery.Esgotado;
            return;
        }
        entrega.ProximaTentativaEm = DateTime.UtcNow.Add(Backoff[indice]);
    }
}
