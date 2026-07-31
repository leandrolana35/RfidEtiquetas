namespace RfidEtiquetas.Shared.Data.Models;

public enum StatusWebhookDelivery
{
    Pendente = 0,
    Sucesso = 1,
    Esgotado = 2 // todas as tentativas de retry falharam
}

// Uma linha por tentativa de entrega de UM evento pra UMA assinatura.
// PayloadJson é fixado no momento da criação (o que foi assinado/enviado
// não muda entre tentativas, mesmo que os dados de origem mudem depois).
public class WebhookDelivery
{
    public long Id { get; set; }

    public int WebhookSubscriptionId { get; set; }
    public WebhookSubscription? WebhookSubscription { get; set; }

    public string Evento { get; set; } = "";
    public string PayloadJson { get; set; } = "";

    public StatusWebhookDelivery Status { get; set; } = StatusWebhookDelivery.Pendente;
    public int Tentativas { get; set; }
    public DateTime ProximaTentativaEm { get; set; } = DateTime.UtcNow;
    public int? UltimoStatusCode { get; set; }
    public string? UltimoErro { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EntregueEm { get; set; }
}
