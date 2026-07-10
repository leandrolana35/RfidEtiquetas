using RfidEtiquetas.Data;
using RfidEtiquetas.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSingleton<AppDbContext>();
builder.Services.AddSingleton<RfidEncoderService>();
builder.Services.AddSingleton<BarcodeService>();
builder.Services.AddSingleton<SatoPrinterService>();
builder.Services.AddSingleton<LoteService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
