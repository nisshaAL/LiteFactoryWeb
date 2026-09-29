using LiteFactoryWeb.Models;
using LiteFactoryWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace LiteFactoryWeb.Pages;

[Authorize]
public class AccountModel : PageModel
{
    private readonly LiteFactoryApiClient _apiClient;

    public AccountModel(LiteFactoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public LiteFactoryAccount? Account { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Login");
        }

        var result = await _apiClient.GetCurrentAccountAsync(token, cancellationToken);
        if (!result.Success || result.Value == null)
        {
            ErrorMessage = result.ErrorMessage ?? "Не удалось загрузить аккаунт.";
            return Page();
        }

        Account = result.Value;
        if (string.IsNullOrWhiteSpace(Account.Role))
        {
            Account.Role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        }

        return Page();
    }
}
