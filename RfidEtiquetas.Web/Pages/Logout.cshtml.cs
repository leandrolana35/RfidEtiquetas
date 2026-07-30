using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Web.Pages;

// O botão "Sair" no menu é um <form> simples dentro de um componente Blazor
// (MainLayout.razor), fora do pipeline de TagHelpers do Razor Pages — por isso
// não tem token anti-CSRF automático. Risco baixo (só desloga), então ignoramos
// a validação aqui em vez de complicar o layout para gerar o token manualmente.
[IgnoreAntiforgeryToken]
public class LogoutModel : PageModel
{
    private readonly SignInManager<Usuario> _signInManager;

    public LogoutModel(SignInManager<Usuario> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("/login");
    }
}
