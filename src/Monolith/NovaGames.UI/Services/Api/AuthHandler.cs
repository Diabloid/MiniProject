using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NovaGames.Core.Services;

namespace NovaGames.UI.Services.Api;

public class AuthHandler : DelegatingHandler
{
    private readonly UserSession _userSession;
    private readonly TokenStorageService _tokenStorage;
    private readonly GatewayConfig _config;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event Action? OnSessionExpired;
    public event Action<int>? OnRateLimitExceeded;

    public AuthHandler(
        UserSession userSession,
        TokenStorageService tokenStorage,
        GatewayConfig config)
    {
        _userSession = userSession;
        _tokenStorage = tokenStorage;
        _config = config;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // 1. Автоматичне додавання Bearer-токена, якщо він є
        var token = _userSession.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            var saved = _tokenStorage.GetTokens();
            token = saved.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _userSession.Token = token;
                _userSession.RefreshToken = saved.RefreshToken ?? string.Empty;
            }
        }

        if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization == null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(503, "Сервер тимчасово недоступний. Перевірте підключення до мережі або роботу шлюзу.", null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(504, "Час очікування відповіді від сервера вичерпано. Спробуйте пізніше.", null);
        }

        // 2. Обробка 429 Too Many Requests (Rate Limiting)
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            int retryAfter = 60;
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var valStr = values.FirstOrDefault();
                if (int.TryParse(valStr, out var parsedSec))
                {
                    retryAfter = parsedSec;
                }
            }

            ClientProblemDetails? problem = null;
            try
            {
                problem = await response.Content.ReadFromJsonAsync<ClientProblemDetails>(cancellationToken: ct);
            }
            catch { }

            OnRateLimitExceeded?.Invoke(retryAfter);
            throw new RateLimitExceededException(retryAfter, problem);
        }

        // 3. Обробка 401 Unauthorized — ОДНА спроба оновити токен через RefreshToken
        if (response.StatusCode == HttpStatusCode.Unauthorized && !request.RequestUri!.AbsolutePath.Contains("/auth/"))
        {
            var refreshed = await TryRefreshTokenAsync(ct);
            if (refreshed)
            {
                // Повторюємо оригінальний запит із новим токеном
                var clonedRequest = await CloneHttpRequestMessageAsync(request);
                clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userSession.Token);
                return await base.SendAsync(clonedRequest, ct);
            }
            else
            {
                _userSession.Token = string.Empty;
                _userSession.RefreshToken = string.Empty;
                _userSession.CurrentUser = null;
                _tokenStorage.ClearToken();
                OnSessionExpired?.Invoke();
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var refreshToken = _userSession.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                var saved = _tokenStorage.GetTokens();
                refreshToken = saved.RefreshToken;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            using var refreshClient = new HttpClient();
            refreshClient.BaseAddress = new Uri(_config.BaseUrl);

            var refreshReq = new { refreshToken };
            var refreshResp = await refreshClient.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshReq, ct);

            if (!refreshResp.IsSuccessStatusCode)
            {
                return false;
            }

            var authDto = await refreshResp.Content.ReadFromJsonAsync<ClientAuthResponse>(cancellationToken: ct);
            if (authDto == null || string.IsNullOrWhiteSpace(authDto.AccessToken))
            {
                return false;
            }

            _userSession.Token = authDto.AccessToken;
            _userSession.RefreshToken = authDto.RefreshToken;
            _tokenStorage.SaveTokens(authDto.AccessToken, authDto.RefreshToken);

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var h in req.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
        }

        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
