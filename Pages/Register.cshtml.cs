using LiteFactoryWeb.Services;
using LiteFactoryWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LiteFactoryWeb.Pages;

public class RegisterModel : PageModel
{
    private readonly LiteFactoryApiClient _apiClient;

    public RegisterModel(LiteFactoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _apiClient.RegisterAsync(Input.Email, Input.Nickname, Input.Password, cancellationToken);
        if (!result.Success || result.Value == null)
        {
            ErrorMessage = result.ErrorMessage ?? "Не удалось создать аккаунт.";
            return Page();
        }

        await SignInAsync(result.Value);
        return RedirectToPage("/Account");
    }

    private async Task SignInAsync(Models.LiteFactoryAuthSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
            new(ClaimTypes.Name, session.User.Nickname),
            new("nickname", session.User.Nickname)
        };
        if (LiteFactoryRoles.IsKnown(session.User.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, session.User.Role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = session.ExpiresAtUtc,
            IsPersistent = false
        };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = session.AccessToken }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "Введите никнейм.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Никнейм должен быть от 3 до 20 символов.")]
        public string Nickname { get; set; } = "";

        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Введите корректный email.")]
        [StringLength(254, ErrorMessage = "Email слишком длинный.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Введите пароль.")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не короче 8 символов.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Повторите пароль.")]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
