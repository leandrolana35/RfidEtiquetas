using System.Windows;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using RfidEtiquetas.Desktop.Components.Pages;

namespace RfidEtiquetas.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var webview = new BlazorWebView
        {
            HostPage = "wwwroot/index.html",
            Services = App.Services
        };
        webview.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Home)
        });

        RootGrid.Children.Add(webview);
    }
}
