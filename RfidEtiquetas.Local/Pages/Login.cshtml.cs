using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RfidEtiquetas.Shared.Data.Models;

namespace RfidEtiquetas.Local.Pages;

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

        return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
