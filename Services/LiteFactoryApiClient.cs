using LiteFactoryWeb.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LiteFactoryWeb.Services;

public sealed class LiteFactoryApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public LiteFactoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LiteFactoryApiResult<LiteFactoryAuthSession>> RegisterAsync(
        string email,
        string nickname,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var registerResponse = await PostJsonAsync("api/auth/register", new RegisterRequest(email, nickname, password), cancellationToken);
            if (!registerResponse.IsSuccessStatusCode)
            {
                return LiteFactoryApiResult<LiteFactoryAuthSession>.Error(await ReadApiErrorAsync(registerResponse, "Не удалось создать аккаунт.", cancellationToken));
            }

            return await LoginAsync(nickname, password, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Сервис авторизации временно недоступен.");
        }
        catch (JsonException)
        {
            return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Сервис авторизации вернул некорректный ответ.");
        }
    }

    public async Task<LiteFactoryApiResult<LiteFactoryAuthSession>> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await PostJsonAsync("api/auth/login", new LoginRequest(login, password), cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Неверный никнейм или пароль.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return LiteFactoryApiResult<LiteFactoryAuthSession>.Error(await ReadApiErrorAsync(response, "Не удалось войти.", cancellationToken));
            }

            var auth = await ReadJsonAsync<AuthResponse>(response, cancellationToken);
            if (auth == null || string.IsNullOrWhiteSpace(auth.AccessToken))
            {
                return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Сервис авторизации вернул некорректный ответ.");
            }

            var account = await GetCurrentAccountAsync(auth.AccessToken, cancellationToken);
            if (!account.Success || account.Value == null)
            {
                return LiteFactoryApiResult<LiteFactoryAuthSession>.Error(account.ErrorMessage ?? "Не удалось загрузить профиль аккаунта.");
            }

            return LiteFactoryApiResult<LiteFactoryAuthSession>.Ok(new LiteFactoryAuthSession
            {
                AccessToken = auth.AccessToken,
                ExpiresAtUtc = auth.ExpiresAtUtc,
                User = account.Value
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Сервис авторизации временно недоступен.");
        }
        catch (JsonException)
        {
            return LiteFactoryApiResult<LiteFactoryAuthSession>.Error("Сервис авторизации вернул некорректный ответ.");
        }
    }

    public async Task<LiteFactoryApiResult<LiteFactoryAccount>> GetCurrentAccountAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/account/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return LiteFactoryApiResult<LiteFactoryAccount>.Error("Не удалось загрузить данные аккаунта.");
            }

            var account = await ReadJsonAsync<LiteFactoryAccount>(response, cancellationToken);
            return account == null
                ? LiteFactoryApiResult<LiteFactoryAccount>.Error("Сервис авторизации вернул некорректный ответ.")
                : LiteFactoryApiResult<LiteFactoryAccount>.Ok(account);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return LiteFactoryApiResult<LiteFactoryAccount>.Error("Сервис авторизации временно недоступен.");
        }
        catch (JsonException)
        {
            return LiteFactoryApiResult<LiteFactoryAccount>.Error("Сервис авторизации вернул некорректный ответ.");
        }
    }

    private async Task<HttpResponseMessage> PostJsonAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(path, content, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response, string fallback, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var error = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(stream, JsonOptions, cancellationToken);
            return string.IsNullOrWhiteSpace(error?.Error) ? fallback : error.Error;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private sealed record RegisterRequest(string Email, string Nickname, string Password);

    private sealed record LoginRequest(string Login, string Password);

    private sealed class AuthResponse
    {
        public LiteFactoryAccount User { get; set; } = new();

        public string AccessToken { get; set; } = "";

        public DateTimeOffset ExpiresAtUtc { get; set; }
    }

    private sealed class ApiErrorResponse
    {
        public string Error { get; set; } = "";
    }
}
