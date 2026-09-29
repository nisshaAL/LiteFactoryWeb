using LiteFactoryWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LiteFactoryWeb.Pages;

public class LoginModel : PageModel
{
    private readonly LiteFactoryApiClient _apiClient;

    public LoginModel(LiteFactoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

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

        var result = await _apiClient.LoginAsync(Input.Nickname, Input.Password, cancellationToken);
        if (!result.Success || result.Value == null)
        {
            ErrorMessage = result.ErrorMessage ?? "Неверный никнейм или пароль.";
            return Page();
        }

        await SignInAsync(result.Value, cancellationToken);
        return RedirectToPage("/Account");
    }

    private async Task SignInAsync(Models.LiteFactoryAuthSession session, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
            new(ClaimTypes.Name, session.User.Nickname),
            new("nickname", session.User.Nickname)
        };

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

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Введите никнейм.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Никнейм должен быть от 3 до 20 символов.")]
        public string Nickname { get; set; } = "";

        [Required(ErrorMessage = "Введите пароль.")]
        public string Password { get; set; } = "";
    }
}
