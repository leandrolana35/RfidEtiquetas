using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RfidEtiquetas.Local.Services;
using RfidEtiquetas.Local.Services.Printing;
using RfidEtiquetas.Shared.Data;
using RfidEtiquetas.Shared.Data.Models;
using RfidEtiquetas.Shared.Services;
using RfidEtiquetas.Shared.Services.Auth;
using RfidEtiquetas.Shared.Services.Printing;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<RfidEncoderService>();
builder.Services.AddSingleton<BarcodeService>();
builder.Services.AddSingleton<ZplBuilder>();
builder.Services.AddSingleton<IPrinterService, ZplPrinterService>();
builder.Services.AddSingleton<LoteService>();
builder.Services.AddHostedService<PrintJobProcessor>();

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
app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.Run();
