using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace RfidEtiquetas.Local;

// Janela nativa do app (quando roda como .exe): um WebView2 em tela cheia
// apontando para o servidor Blazor que está rodando no próprio processo.
// Assim o sistema abre como um aplicativo cliente-servidor, sem console e
// sem navegador externo.
public class ShellForm : Form
{
    public ShellForm(string url)
    {
        Text = "AFIXPrint";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1280;
        Height = 860;
        WindowState = FormWindowState.Maximized;

        // Pasta de dados do WebView2 em LocalAppData (sempre gravável, mesmo se o
        // .exe estiver numa pasta protegida).
        var userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AFIXPrint", "WebView2");

        var webview = new WebView2
        {
            Dock = DockStyle.Fill,
            CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = userData }
        };

        Controls.Add(webview);
        webview.Source = new Uri(url);
    }
}
