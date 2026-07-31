namespace RfidEtiquetas.Shared.Services;

public interface IWebhookDispatcher
{
    /// <summary>
    /// Cria uma WebhookDelivery (status Pendente) para cada assinatura ativa da
    /// empresa que esteja inscrita nesse evento. Não faz nenhuma chamada HTTP —
    /// isso é feito pelo WebhookDeliveryProcessor em segundo plano.
    /// </summary>
    void EnqueueEvent(string evento, int empresaId, object payload);
}
