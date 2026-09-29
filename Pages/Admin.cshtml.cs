using LiteFactoryWeb.Models;
using LiteFactoryWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiteFactoryWeb.Pages;

[Authorize(Roles = "ADMIN")]
public class AdminModel : PageModel
{
    private static readonly string[] AllowedRoles = ["USER", "DEVELOPER", "ADMIN"];
    private readonly LiteFactoryApiClient _apiClient;

    public AdminModel(LiteFactoryApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IReadOnlyList<AdminUser> Users { get; private set; } = [];

    public IReadOnlyList<string> Roles => AllowedRoles;

    public string? ErrorMessage { get; private set; }

    public string? SuccessMessage { get; private set; }

    public int TotalCount => Users.Count;

    public int UserCount => Users.Count(user => user.Role == "USER");

    public int DeveloperCount => Users.Count(user => user.Role == "DEVELOPER");

    public int AdminCount => Users.Count(user => user.Role == "ADMIN");

    [BindProperty]
    public Guid UserId { get; set; }

    [BindProperty]
    public string Role { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["AdminSuccess"] as string;
        ErrorMessage = TempData["AdminError"] as string;
        await LoadUsersAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(CancellationToken cancellationToken)
    {
        var requestedRole = Role.Trim().ToUpperInvariant();
        if (!AllowedRoles.Contains(requestedRole))
        {
            TempData["AdminError"] = "Выберите корректную роль.";
            return RedirectToPage();
        }

        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AdminError"] = "Сессия администратора истекла. Войдите снова.";
            return RedirectToPage("/Login");
        }

        var result = await _apiClient.ChangeUserRoleAsync(token, UserId, requestedRole, cancellationToken);
        TempData[result.Success ? "AdminSuccess" : "AdminError"] =
            result.Success ? "Роль пользователя обновлена." : result.ErrorMessage ?? "Не удалось обновить роль пользователя.";

        return RedirectToPage();
    }

    private async Task LoadUsersAsync(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage ??= "Сессия администратора истекла. Войдите снова.";
            Users = [];
            return;
        }

        var result = await _apiClient.GetAdminUsersAsync(token, cancellationToken);
        if (!result.Success || result.Value == null)
        {
            ErrorMessage ??= result.ErrorMessage ?? "Не удалось загрузить список пользователей.";
            Users = [];
            return;
        }

        Users = result.Value;
    }
}
