using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services.Auth;

namespace RfidEtiquetas.Shared.Data;

// IdentityUserContext (não IdentityDbContext) porque não usamos Identity Roles:
// o perfil Administrador/Usuario é uma coluna simples em Usuario, não uma
// tabela AspNetRoles à parte.
public class AppDbContext : IdentityUserContext<Usuario, int>
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();
    public DbSet<EstacaoImpressora> EstacoesImpressoras => Set<EstacaoImpressora>();
    public DbSet<Projeto> Projetos => Set<Projeto>();
    public DbSet<TagRegistro> TagRegistros => Set<TagRegistro>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>()
            .HasOne(u => u.Empresa).WithMany()
            .HasForeignKey(u => u.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Projeto>(e =>
        {
            e.HasOne(p => p.EtiquetaTemplate).WithMany()
                .HasForeignKey(p => p.EtiquetaTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Empresa).WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuário só enxerga projetos da própria empresa; Administrador vê todas.
            e.HasQueryFilter(p => _currentUser.IsAdministrador || p.EmpresaId == _currentUser.EmpresaId);
        });

        builder.Entity<TagRegistro>(e =>
        {
            e.HasOne(t => t.Projeto).WithMany()
                .HasForeignKey(t => t.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Estacao).WithMany()
                .HasForeignKey(t => t.EstacaoId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => new { t.ProjetoId, t.NumeroSequencia }).IsUnique();
            e.HasIndex(t => new { t.ProjetoId, t.Status });

            e.HasQueryFilter(t => _currentUser.IsAdministrador || t.Projeto!.EmpresaId == _currentUser.EmpresaId);
        });

        // ApiKey/PrintJob não usam o filtro de empresa baseado em cookie (ICurrentUserService)
        // porque a autenticação por API Key é um mecanismo totalmente separado do login por
        // cookie — o escopo por empresa aqui é aplicado explicitamente nos controllers, a
        // partir da EmpresaId da própria chave autenticada.
        builder.Entity<ApiKey>(e =>
        {
            e.HasOne(k => k.Empresa).WithMany()
                .HasForeignKey(k => k.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(k => k.ChaveHash).IsUnique();
        });

        builder.Entity<PrintJob>(e =>
        {
            e.HasOne(j => j.ApiKey).WithMany()
                .HasForeignKey(j => j.ApiKeyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(j => j.Estacao).WithMany()
                .HasForeignKey(j => j.EstacaoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(j => j.EtiquetaTemplate).WithMany()
                .HasForeignKey(j => j.EtiquetaTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(j => j.Projeto).WithMany()
                .HasForeignKey(j => j.ProjetoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(j => j.TagRegistro).WithMany()
                .HasForeignKey(j => j.TagRegistroId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(j => new { j.Status, j.CriadoEm }); // usado pelo PrintJobProcessor
        });

        // EstacaoImpressora NÃO usa query filter automático (diferente de Projeto/TagRegistro):
        // o PrintJobProcessor (background service, sem HttpContext/cookie) e os controllers
        // da API (autenticados por chave, não por cookie) também consultam esta tabela — um
        // filtro baseado em ICurrentUserService escondería os registros nesses dois contextos.
        // O escopo por empresa aqui é feito explicitamente em cada chamador (ver
        // GetEstacoesVisiveis abaixo e os controllers de API).
        builder.Entity<EstacaoImpressora>()
            .HasOne(e => e.Empresa).WithMany()
            .HasForeignKey(e => e.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mesma lógica de escopo manual (sem query filter) das outras entidades
        // usadas pelo processador de fila em background.
        builder.Entity<WebhookSubscription>(e =>
        {
            e.HasOne(w => w.Empresa).WithMany()
                .HasForeignKey(w => w.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WebhookDelivery>(e =>
        {
            e.HasOne(d => d.WebhookSubscription).WithMany()
                .HasForeignKey(d => d.WebhookSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => new { d.Status, d.ProximaTentativaEm }); // usado pelo WebhookDeliveryProcessor
        });
    }

    // Helpers simples de CRUD, na mesma linha do que a antiga classe AppDbContext
    // (ADO.NET) já expunha — mantém as páginas Razor enxutas sem precisar de
    // uma camada de repositório separada para um app deste tamanho.

    public List<Etiqueta> GetEtiquetas() => Etiquetas.OrderBy(e => e.Modelo).ToList();

    public Etiqueta? GetEtiqueta(int id) => Etiquetas.Find(id);

    public int SaveEtiqueta(Etiqueta e)
    {
        if (e.Id == 0) Etiquetas.Add(e);
        else Etiquetas.Update(e);
        SaveChanges();
        return e.Id;
    }

    public void DeleteEtiqueta(int id)
    {
        var e = Etiquetas.Find(id);
        if (e == null) return;
        Etiquetas.Remove(e);
        SaveChanges();
    }

    public List<EstacaoImpressora> GetEstacoes() => EstacoesImpressoras.Include(e => e.Empresa).OrderBy(e => e.NomeEstacao).ToList();

    /// <summary>Escopo explícito por empresa (ver comentário em OnModelCreating sobre por que não é um query filter automático).</summary>
    public List<EstacaoImpressora> GetEstacoesVisiveis(bool isAdministrador, int? empresaId)
        => GetEstacoes().Where(e => isAdministrador || e.EmpresaId == empresaId).ToList();

    public EstacaoImpressora? GetEstacao(int id) => EstacoesImpressoras.Include(e => e.Empresa).FirstOrDefault(e => e.Id == id);

    public int SaveEstacao(EstacaoImpressora e)
    {
        if (e.Id == 0) EstacoesImpressoras.Add(e);
        else EstacoesImpressoras.Update(e);
        SaveChanges();
        return e.Id;
    }

    public void DeleteEstacao(int id)
    {
        var e = EstacoesImpressoras.Find(id);
        if (e == null) return;
        EstacoesImpressoras.Remove(e);
        SaveChanges();
    }

    public List<Projeto> GetProjetos() => Projetos.Include(p => p.EtiquetaTemplate).OrderByDescending(p => p.CriadoEm).ToList();

    public Projeto? GetProjeto(int id) => Projetos.Include(p => p.EtiquetaTemplate).FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Cria o Projeto e já popula um TagRegistro (status NaoImpressa) para cada
    /// número entre SequenciaInicial e SequenciaFinal, para o dashboard poder
    /// mostrar contagem imediatamente, sem esperar a primeira impressão.
    /// Em edição, a faixa também pode mudar: números novos ganham TagRegistro
    /// na hora; números removidos só são apagados se ainda não tiverem
    /// histórico (nunca apaga uma tag já impressa ou com erro silenciosamente).
    /// </summary>
    /// <returns>Mensagem de erro, ou null se salvou com sucesso.</returns>
    public string? SalvarProjeto(Projeto p)
    {
        bool novo = p.Id == 0;
        if (novo)
        {
            Projetos.Add(p);
            SaveChanges();

            for (long n = p.SequenciaInicial; n <= p.SequenciaFinal; n++)
                TagRegistros.Add(new TagRegistro { ProjetoId = p.Id, NumeroSequencia = n });
            SaveChanges();
            return null;
        }

        var existente = Projetos.AsNoTracking().First(x => x.Id == p.Id);
        if (existente.SequenciaInicial != p.SequenciaInicial || existente.SequenciaFinal != p.SequenciaFinal)
        {
            var todasTags = TagRegistros.Where(t => t.ProjetoId == p.Id).ToList();
            var foraDaFaixa = todasTags.Where(t => t.NumeroSequencia < p.SequenciaInicial || t.NumeroSequencia > p.SequenciaFinal).ToList();
            var comHistorico = foraDaFaixa.Where(t => t.Status != StatusTag.NaoImpressa).ToList();
            if (comHistorico.Count > 0)
                return $"Não é possível reduzir a faixa: {comHistorico.Count} tag(s) fora da nova faixa já têm histórico (impressa ou erro). Ajuste o status delas primeiro na lista de tags abaixo.";

            TagRegistros.RemoveRange(foraDaFaixa);
            var numerosRestantes = todasTags.Select(t => t.NumeroSequencia).Except(foraDaFaixa.Select(t => t.NumeroSequencia)).ToHashSet();
            for (long n = p.SequenciaInicial; n <= p.SequenciaFinal; n++)
                if (!numerosRestantes.Contains(n))
                    TagRegistros.Add(new TagRegistro { ProjetoId = p.Id, NumeroSequencia = n });
        }

        Projetos.Update(p);
        SaveChanges();
        return null;
    }

    public List<TagRegistro> GetTagsDoProjeto(int projetoId)
        => TagRegistros.Where(t => t.ProjetoId == projetoId).OrderBy(t => t.NumeroSequencia).ToList();

    private IQueryable<TagRegistro> FiltrarTagsDoProjeto(int projetoId, StatusTag? status, string? numeroBusca)
    {
        var q = TagRegistros.Where(t => t.ProjetoId == projetoId);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(numeroBusca) && long.TryParse(numeroBusca, out var num))
            q = q.Where(t => t.NumeroSequencia == num);
        return q;
    }

    /// <summary>Usada pela lista editável de tags em Projetos.razor — carrega em páginas, não tudo de uma vez.</summary>
    public List<TagRegistro> GetTagsDoProjetoFiltradas(int projetoId, StatusTag? status, string? numeroBusca, int take)
        => FiltrarTagsDoProjeto(projetoId, status, numeroBusca).OrderBy(t => t.NumeroSequencia).Take(take).ToList();

    public int CountTagsDoProjetoFiltradas(int projetoId, StatusTag? status, string? numeroBusca)
        => FiltrarTagsDoProjeto(projetoId, status, numeroBusca).Count();

    /// <summary>Edição manual completa de uma tag — usada pela tela de Projetos (Administrador).</summary>
    public void AtualizarTagRegistro(long id, StatusTag status, string? codigoBarras, string? dadoRfid, string? mensagemErro, int? estacaoId)
    {
        var tag = TagRegistros.FirstOrDefault(t => t.Id == id);
        if (tag == null) return;
        tag.Status = status;
        tag.CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras;
        tag.DadoRfid = string.IsNullOrWhiteSpace(dadoRfid) ? null : dadoRfid;
        tag.MensagemErro = string.IsNullOrWhiteSpace(mensagemErro) ? null : mensagemErro;
        tag.EstacaoId = estacaoId;
        tag.DataImpressao = status == StatusTag.Impressa ? (tag.DataImpressao ?? DateTime.UtcNow) : null;
        SaveChanges();
    }

    /// <summary>Próximo item ainda não impresso (ou com erro, para permitir reimpressão) do projeto.</summary>
    public TagRegistro? ProximaTagPendente(int projetoId)
        => TagRegistros
            .Where(t => t.ProjetoId == projetoId && t.Status != StatusTag.Impressa)
            .OrderBy(t => t.NumeroSequencia)
            .FirstOrDefault();

    public void MarcarTag(long tagRegistroId, StatusTag status, int? estacaoId, string? erro = null)
    {
        // IgnoreQueryFilters: o id já foi validado pelo chamador (seja a página
        // Blazor com sessão de cookie, seja o PrintJobProcessor sem HttpContext
        // algum) — sem isso, chamadas vindas do processador de fila da API
        // (sem cookie, então _currentUser.EmpresaId é null) nunca encontrariam
        // a linha e o status ficaria preso em "NaoImpressa" silenciosamente.
        var tag = TagRegistros.IgnoreQueryFilters().FirstOrDefault(t => t.Id == tagRegistroId);
        if (tag == null) return;
        tag.Status = status;
        tag.DataImpressao = status == StatusTag.Impressa ? DateTime.UtcNow : tag.DataImpressao;
        tag.MensagemErro = erro;
        tag.EstacaoId = estacaoId;
        SaveChanges();
    }

    public (int Total, int Impressas, int NaoImpressas, int Erros) GetResumoTags(int projetoId)
    {
        var grupos = TagRegistros.Where(t => t.ProjetoId == projetoId)
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Qtd = g.Count() })
            .ToList();

        int Qtd(StatusTag s) => grupos.FirstOrDefault(g => g.Key == s)?.Qtd ?? 0;
        return (grupos.Sum(g => g.Qtd), Qtd(StatusTag.Impressa), Qtd(StatusTag.NaoImpressa), Qtd(StatusTag.Erro));
    }

    public List<Empresa> GetEmpresas() => Empresas.OrderBy(e => e.Nome).ToList();

    public Empresa? GetEmpresa(int id) => Empresas.Find(id);

    public int SaveEmpresa(Empresa e)
    {
        if (e.Id == 0) Empresas.Add(e);
        else Empresas.Update(e);
        SaveChanges();
        return e.Id;
    }

    public List<ApiKey> GetApiKeys() => ApiKeys.Include(k => k.Empresa).OrderByDescending(k => k.CriadoEm).ToList();

    /// <summary>Cria a chave e devolve o valor em texto puro — só existe neste retorno, nunca mais.</summary>
    public (ApiKey Registro, string ChaveTextoPuro) CriarApiKey(string nome, int empresaId)
    {
        var chave = Services.ApiKeyService.GerarChave();
        var registro = new ApiKey
        {
            Nome = nome,
            EmpresaId = empresaId,
            ChaveHash = Services.ApiKeyService.Hash(chave),
            ChavePrefixo = Services.ApiKeyService.Prefixo(chave)
        };
        ApiKeys.Add(registro);
        SaveChanges();
        return (registro, chave);
    }

    public void RevogarApiKey(int id)
    {
        var k = ApiKeys.Find(id);
        if (k == null) return;
        k.Ativo = false;
        SaveChanges();
    }

    public List<WebhookSubscription> GetWebhooks() => WebhookSubscriptions.Include(w => w.Empresa).OrderByDescending(w => w.CriadoEm).ToList();

    public WebhookSubscription CriarWebhook(string url, string secret, string[] eventos, int empresaId)
    {
        var w = new WebhookSubscription { Url = url, Secret = secret, Eventos = eventos, EmpresaId = empresaId };
        WebhookSubscriptions.Add(w);
        SaveChanges();
        return w;
    }

    public void RevogarWebhook(int id)
    {
        var w = WebhookSubscriptions.Find(id);
        if (w == null) return;
        w.Ativo = false;
        SaveChanges();
    }

    public List<WebhookDelivery> GetEntregasRecentes(int webhookSubscriptionId, int limite = 20)
        => WebhookDeliveries.Where(d => d.WebhookSubscriptionId == webhookSubscriptionId)
            .OrderByDescending(d => d.CriadoEm).Take(limite).ToList();
}
