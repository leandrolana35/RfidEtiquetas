using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Local.Mcp;
using RfidEtiquetas.Local.Services;
using RfidEtiquetas.Local.Services.Printing;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services;
using RfidEtiquetas.Shared.Services.Auth;
using RfidEtiquetas.Shared.Services.Printing;

// ContentRootPath fixo na pasta do executável — garante que appsettings.json e wwwroot
// sejam encontrados mesmo quando o .exe é aberto com outra pasta de trabalho (modo janela).
// Como Serviço, o UseWindowsService já faz isso; aqui vale para os dois modos.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Permite rodar como Serviço do Windows de verdade (sc.exe create) — sem isso
// o processo não entende o protocolo de start/stop do Service Control Manager
// e o Windows reporta falha ao iniciar o serviço.
builder.Host.UseWindowsService();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAuthorizationCore();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
    })
    // Esquema separado para a API externa (cabeçalho X-API-Key) — não interfere
    // com o login por cookie do resto do app; cada endpoint escolhe qual usar.
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, null);

// Política usada pelo endpoint MCP — mesma chave de API da REST.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKey", policy => policy
        .AddAuthenticationSchemes(ApiKeyAuthHandler.SchemeName)
        .RequireAuthenticatedUser());
});

builder.Services.AddIdentityCore<Usuario>(o =>
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<UsuarioClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddScoped<PrintJobService>();
builder.Services.AddSingleton<RfidEncoderService>();
builder.Services.AddSingleton<BarcodeService>();
builder.Services.AddSingleton<ZplBuilder>();
builder.Services.AddSingleton<IPrinterService, ZplPrinterService>();
builder.Services.AddSingleton<LoteService>();
builder.Services.AddHostedService<PrintJobProcessor>();
builder.Services.AddHostedService<WebhookDeliveryProcessor>();
builder.Services.AddHttpClient("webhooks", c => c.Timeout = TimeSpan.FromSeconds(10));

// Servidor MCP — expõe as tools pra agentes de IA via HTTP, autenticado
// pela mesma X-API-Key da API REST (ver política "ApiKey" acima e o
// mapeamento de /mcp mais abaixo).
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<RfidTools>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    await SeedData.EnsureSeedAsync(db, userManager);
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMcp("/mcp").RequireAuthorization("ApiKey");
app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

// Como Serviço do Windows (ou sem interface interativa): roda headless, como sempre.
// Como .exe aberto pelo usuário: sobe o servidor e abre a JANELA do app (WebView2),
// sem console visível e sem navegador externo — vira um aplicativo cliente-servidor.
bool comoServico = Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService();

if (comoServico || !Environment.UserInteractive)
{
    app.Run();
}
else
{
    await app.StartAsync();

    var urlAbrir = (builder.Configuration["urls"] ?? "http://localhost:5090").Split(';', ',')[0].Trim();

    // A UI do WinForms/WebView2 precisa de um thread STA próprio.
    var uiThread = new Thread(() =>
    {
        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.SystemAware);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new RfidEtiquetas.Local.ShellForm(urlAbrir));
    });
    uiThread.SetApartmentState(ApartmentState.STA);
    uiThread.Start();
    uiThread.Join();   // bloqueia até o usuário fechar a janela

    await app.StopAsync();   // ao fechar a janela, derruba o servidor junto
}
