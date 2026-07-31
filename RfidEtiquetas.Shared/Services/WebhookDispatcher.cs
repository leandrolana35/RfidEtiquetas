using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Shared.Services;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly AppDbContext _db;

    public WebhookDispatcher(AppDbContext db)
    {
        _db = db;
    }

    public void EnqueueEvent(string evento, int empresaId, object payload)
    {
        var assinantes = _db.WebhookSubscriptions
            .Where(w => w.Ativo && w.EmpresaId == empresaId && w.Eventos.Contains(evento))
            .ToList();

        if (assinantes.Count == 0) return;

        var json = JsonSerializer.Serialize(payload);
        foreach (var assinante in assinantes)
        {
            _db.WebhookDeliveries.Add(new WebhookDelivery
            {
                WebhookSubscriptionId = assinante.Id,
                Evento = evento,
                PayloadJson = json
            });
        }
        _db.SaveChanges();
    }
}
