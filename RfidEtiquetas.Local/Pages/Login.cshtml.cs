using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Local.Pages;

// Login precisa ser uma Razor Page (não um componente Blazor): gravar o
// cookie de autenticação exige escrever no HttpContext.Response, o que não
// é possível a partir de um componente Blazor Server já conectado via SignalR.
public class LoginModel : PageModel
{
    private readonly SignInManager<Usuario> _signInManager;

    public LoginModel(SignInManager<Usuario> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Senha { get; set; } = "";

    public string? Erro { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var resultado = await _signInManager.PasswordSignInAsync(Email, Senha, isPersistent: true, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            Erro = "E-mail ou senha inválidos.";
            return Page();
        }

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
