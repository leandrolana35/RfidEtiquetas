using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Services.Auth;

namespace RfidEtiquetas.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada no appsettings.json");

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        // Em modo desktop (offline, usuário local) não há login por cookie:
        // usamos a versão "sempre administrador" que o AppDbContext espera.
        services.AddSingleton<ICurrentUserService, SystemCurrentUserService>();

        // Fábrica de DbContext: cada operação cria um contexto curto e fresco
        // (sem cache preso de sessão — some o problema de "status não atualiza").
        services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(cs));

        Services = services.BuildServiceProvider();
    }
}
