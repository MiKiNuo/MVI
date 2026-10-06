using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>沿用 main 的 DummyJSON 公共测试 API，不需要启动本地服务器。</summary>
/// <remarks>注册和改密只模拟 HTTP 写入，不会保存账号或改变测试账号密码。只能使用演示数据。</remarks>
[DiService(ServiceLifetime.Scoped, ServiceType = typeof(IAuthService))]
public sealed class HttpAuthService : IAuthService, IDisposable
{
    /// <summary>服务拥有的 HTTP 客户端，随 Feature 作用域释放。</summary>
    private readonly HttpClient _client;

    /// <summary>创建访问公共测试接口的客户端；生成式 DI 使用这个无参构造函数。</summary>
    public HttpAuthService()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://dummyjson.com/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <summary>注入受控传输以验证 HTTP 契约；本服务拥有并释放该传输。</summary>
    /// <param name="handler">测试传输，不会作为生成式 DI 的服务依赖。</param>
    internal HttpAuthService(HttpMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri("https://dummyjson.com/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <inheritdoc/>
    public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(password);
        return SendAsync(HttpMethod.Post, "auth/login", new LoginRequest(userName, password),
            AuthJsonContext.Default.LoginRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);
        return SendAsync(HttpMethod.Post, "users/add", new RegisterRequest(userName, email, password),
            AuthJsonContext.Default.RegisterRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(newPassword);
        // 与原开源示例一致：固定对测试用户 1 做模拟更新，不冒充真实账号恢复接口。
        return SendAsync(HttpMethod.Put, "users/1", new ResetRequest(newPassword),
            AuthJsonContext.Default.ResetRequest, cancellationToken);
    }

    /// <summary>使用编译期 JSON 元数据发送请求；只把有效用户响应视为演示成功。</summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="path">测试接口路径。</param>
    /// <param name="payload">演示请求快照。</param>
    /// <param name="info">编译期序列化元数据。</param>
    /// <param name="token">调用方取消标记。</param>
    /// <returns>演示结果；调用方取消仍向上传播。</returns>
    private async Task<AuthResult> SendAsync<TRequest>(HttpMethod method, string path, TRequest payload,
        JsonTypeInfo<TRequest> info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            using HttpRequestMessage request = new(method, path)
            {
                Content = JsonContent.Create(payload, info)
            };
            using HttpResponseMessage response = await _client.SendAsync(request, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                // 不把可能回显密码或令牌的响应原文写入 State 或诊断。
                return AuthResult.Failure($"测试接口拒绝请求（HTTP {(int)response.StatusCode}），请检查测试账号和输入。");
            }

            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.Number ||
                !id.TryGetInt64(out long userId) || userId <= 0)
            {
                return AuthResult.Failure("测试接口未返回有效的用户结果。");
            }

            string username = ReadString(root, "username");
            if (string.IsNullOrWhiteSpace(username)) return AuthResult.Failure("测试接口未返回有效的用户名。");
            string displayName = $"{ReadString(root, "firstName")} {ReadString(root, "lastName")}".Trim();
            return AuthResult.Success(displayName.Length > 0 ? displayName : username);
        }
        catch (HttpRequestException)
        {
            return AuthResult.Failure("无法访问 DummyJSON，请检查网络或系统代理。");
        }
        catch (JsonException)
        {
            return AuthResult.Failure("测试接口响应不是有效的 JSON 用户结果。");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return AuthResult.Failure("DummyJSON 测试接口响应超时，请稍后重试。");
        }
    }

    /// <summary>只读取预期的字符串字段，忽略接口额外返回的密码、令牌和其他用户数据。</summary>
    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty : string.Empty;

    /// <summary>释放拥有的 HTTP 客户端及其传输。</summary>
    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
