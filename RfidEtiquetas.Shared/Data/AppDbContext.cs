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

    public List<EstacaoImpressora> GetEstacoes() => EstacoesImpressoras.OrderBy(e => e.NomeEstacao).ToList();

    public EstacaoImpressora? GetEstacao(int id) => EstacoesImpressoras.Find(id);

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
    /// </summary>
    public int SalvarProjeto(Projeto p)
    {
        bool novo = p.Id == 0;
        if (novo) Projetos.Add(p);
        else Projetos.Update(p);
        SaveChanges();

        if (novo)
        {
            for (long n = p.SequenciaInicial; n <= p.SequenciaFinal; n++)
                TagRegistros.Add(new TagRegistro { ProjetoId = p.Id, NumeroSequencia = n });
            SaveChanges();
        }

        return p.Id;
    }

    public List<TagRegistro> GetTagsDoProjeto(int projetoId)
        => TagRegistros.Where(t => t.ProjetoId == projetoId).OrderBy(t => t.NumeroSequencia).ToList();

    /// <summary>Próximo item ainda não impresso (ou com erro, para permitir reimpressão) do projeto.</summary>
    public TagRegistro? ProximaTagPendente(int projetoId)
        => TagRegistros
            .Where(t => t.ProjetoId == projetoId && t.Status != StatusTag.Impressa)
            .OrderBy(t => t.NumeroSequencia)
            .FirstOrDefault();

    public void MarcarTag(long tagRegistroId, StatusTag status, int? estacaoId, string? erro = null)
    {
        var tag = TagRegistros.Find(tagRegistroId);
        if (tag == null) return;
        tag.Status = status;
        tag.DataImpressao = status == StatusTag.Impressa ? DateTime.UtcNow : tag.DataImpressao;
        tag.MensagemErro = erro;
        tag.EstacaoId = estacaoId;
        SaveChanges();
    }
}
