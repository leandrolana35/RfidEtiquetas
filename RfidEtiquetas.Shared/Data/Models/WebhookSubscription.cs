namespace RfidEtiquetas.Shared.Data.Models;

// Nomes de evento em um só lugar, pra evitar strings soltas divergindo entre
// o dispatcher, os pontos de disparo e a tela de cadastro.
public static class WebhookEventos
{
    public const string JobCreated = "job.created";
    public const string JobCompleted = "job.completed";
    public const string JobFailed = "job.failed";

    public static readonly string[] Todos = { JobCreated, JobCompleted, JobFailed };
}

// O Secret fica em texto puro (diferente de ApiKey) porque o servidor precisa
// dele toda vez que envia um evento, para calcular a assinatura HMAC — não dá
// pra guardar só um hash como fazemos com a chave de API.
public class WebhookSubscription
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Secret { get; set; } = "";
    public string[] Eventos { get; set; } = Array.Empty<string>();

    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
