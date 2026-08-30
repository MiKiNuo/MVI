using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>
/// 表示基于 dummyjson.com 公共测试 API 的联网认证服务。
/// </summary>
/// <remarks>
/// 登录走 POST /auth/login，注册走 POST /users/add，
/// 用于在真实网络条件下验证 MVI 异步链路（Intent → Effect → 回流）。
/// </remarks>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IAuthService))]
public sealed class HttpAuthService : IAuthService, IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://dummyjson.com/"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient
                .PostAsJsonAsync(
                    "auth/login",
                    new { username = userName, password = password, expiresInMins = 30 },
                    cancellationToken)
                .ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Failure($"服务器拒绝（{(int)response.StatusCode}）：{ExtractMessage(body)}");
            }

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            string firstName = root.TryGetProperty("firstName", out JsonElement first)
                ? first.GetString() ?? string.Empty
                : string.Empty;
            string lastName = root.TryGetProperty("lastName", out JsonElement last)
                ? last.GetString() ?? string.Empty
                : string.Empty;
            string displayName = $"{firstName} {lastName}".Trim();
            return AuthResult.Success(displayName.Length > 0 ? displayName : userName);
        }
        catch (HttpRequestException exception)
        {
            return AuthResult.Failure($"网络错误：{exception.Message}");
        }
        catch (TaskCanceledException)
        {
            return AuthResult.Failure("请求超时或已取消。");
        }
    }

    /// <inheritdoc />
    public async Task<AuthResult> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient
                .PostAsJsonAsync(
                    "users/add",
                    new { username = userName, email = email, password = password },
                    cancellationToken)
                .ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Failure($"服务器拒绝（{(int)response.StatusCode}）：{ExtractMessage(body)}");
            }

            return AuthResult.Success(userName);
        }
        catch (HttpRequestException exception)
        {
            return AuthResult.Failure($"网络错误：{exception.Message}");
        }
        catch (TaskCanceledException)
        {
            return AuthResult.Failure("请求超时或已取消。");
        }
    }

    /// <summary>
    /// 释放底层 HttpClient。
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static string ExtractMessage(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out JsonElement message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // 响应体不是 JSON 时直接透传原文。
        }

        return body;
    }
}
