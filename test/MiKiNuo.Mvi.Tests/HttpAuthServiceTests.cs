using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>通过内存 HTTP 传输验证真实客户端，不启动服务器，也不依赖公网测试站可用性。</summary>
public sealed class HttpAuthServiceTests
{
    /// <summary>登录使用第三方约定的路径、字段大小写和显示名；不解析旧的本机 AuthReply。</summary>
    [Test]
    public async Task LoginUsesDummyJsonContractAsync()
    {
        bool correctRequest = false;
        using HttpAuthService service = CreateService(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            JsonElement body = json.RootElement;
            correctRequest = request.Method == HttpMethod.Post
                && request.RequestUri!.AbsoluteUri == "https://dummyjson.com/auth/login"
                && request.Content.Headers.ContentType?.MediaType == "application/json"
                && body.GetProperty("username").GetString() == "emilys"
                && body.GetProperty("password").GetString() == "emilyspass"
                && body.GetProperty("expiresInMins").GetInt32() == 30
                && !body.TryGetProperty("userName", out _);
            return Response(HttpStatusCode.OK, """{"id":1,"username":"emilys","firstName":"Emily","lastName":"Johnson","accessToken":"not-for-state"}""");
        });
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(correctRequest).IsTrue();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.DisplayName).IsEqualTo("Emily Johnson");
        await Assert.That(result.ToString().Contains("not-for-state", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>没有显示名时使用接口返回的用户名，而不是伪造用户身份。</summary>
    [Test]
    public async Task LoginFallsBackToReturnedUsernameAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.OK, """{"id":1,"username":"emilys"}""");
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(result.DisplayName).IsEqualTo("emilys");
    }

    /// <summary>注册使用 users/add，只发送第三方字段。</summary>
    [Test]
    public async Task RegistrationUsesUsersAddAsync()
    {
        bool correctRequest = false;
        using HttpAuthService service = CreateService(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            JsonElement body = json.RootElement;
            correctRequest = request.Method == HttpMethod.Post
                && request.RequestUri!.AbsoluteUri == "https://dummyjson.com/users/add"
                && body.GetProperty("username").GetString() == "demo-input"
                && body.GetProperty("email").GetString() == "demo@example.test"
                && body.GetProperty("password").GetString() == "test-password";
            return Response(HttpStatusCode.Created, """{"id":209,"username":"demo-input"}""");
        });
        AuthResult result = await service.RegisterAsync("demo-input", "demo@example.test", "test-password", default);
        await Assert.That(correctRequest).IsTrue();
        await Assert.That(result.IsSuccess).IsTrue();
    }

    /// <summary>与 main 一致，用固定的 users/1 模拟更新，不发送不存在的验证码字段。</summary>
    [Test]
    public async Task ResetUsesDocumentedUserOneSimulationAsync()
    {
        bool correctRequest = false;
        using HttpAuthService service = CreateService(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            JsonElement body = json.RootElement;
            correctRequest = request.Method == HttpMethod.Put
                && request.RequestUri!.AbsoluteUri == "https://dummyjson.com/users/1"
                && body.GetProperty("password").GetString() == "test-new-password"
                && body.EnumerateObject().Count() == 1;
            return Response(HttpStatusCode.OK, """{"id":1,"username":"emilys","password":"test-new-password"}""");
        });
        AuthResult result = await service.ResetPasswordAsync("emilys", "test-new-password", default);
        await Assert.That(correctRequest).IsTrue();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.ToString().Contains("test-new-password", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>接口拒绝不能被转换成成功，错误原文中的敏感内容也不进入结果。</summary>
    [Test]
    public async Task HttpFailureDoesNotEchoSensitiveBodyAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.BadRequest, """{"message":"rejected secret-password"}""");
        AuthResult result = await service.LoginAsync("emilys", "secret-password", default);
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage!.Contains("400", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.ErrorMessage.Contains("secret-password", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>成功状态码配合 HTML 或畸形 JSON 仍是失败。</summary>
    [Test]
    public async Task MalformedSuccessResponseIsRejectedAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.OK, "<html>proxy error</html>");
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(result.IsSuccess).IsFalse();
    }

    /// <summary>旧的本地回复格式或缺少用户身份的成功响应不会误进入大厅。</summary>
    [Test]
    public async Task ResponseWithoutUserIdentityIsRejectedAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.OK, """{"isSuccess":true,"displayName":"fake"}""");
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(result.IsSuccess).IsFalse();
    }

    /// <summary>网络故障显示访问第三方站点的提示，不提示启动本地服务器。</summary>
    [Test]
    public async Task NetworkFailureHasThirdPartyMessageAsync()
    {
        using HttpAuthService service = CreateService((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage!.Contains("DummyJSON", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>传输超时映射成可重试错误。</summary>
    [Test]
    public async Task TransportTimeoutIsReportedAsync()
    {
        using HttpAuthService service = CreateService((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout")));
        AuthResult result = await service.LoginAsync("emilys", "emilyspass", default);
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage!.Contains("超时", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>调用方主动取消应继续抛取消异常，而不是被当成网络失败。</summary>
    [Test]
    public async Task CallerCancellationRemainsCancellationAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.OK, """{"id":1,"username":"emilys"}""");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await TestCheck.ThrowsAsync<OperationCanceledException>(() => service.LoginAsync("emilys", "emilyspass", cancellation.Token));
    }

    /// <summary>传输忽略取消并返回成功时，客户端仍检查调用方取消。</summary>
    [Test]
    public async Task LateSuccessAfterCancellationIsNotAcceptedAsync()
    {
        using CancellationTokenSource cancellation = new();
        using HttpAuthService service = CreateService((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(Response(HttpStatusCode.OK, """{"id":1,"username":"emilys"}"""));
        });
        await TestCheck.ThrowsAsync<OperationCanceledException>(() => service.LoginAsync("emilys", "emilyspass", cancellation.Token));
    }

    /// <summary>公开服务入口先验证引用参数，不通过禁用 CA1062 隐藏错误。</summary>
    [Test]
    public async Task PublicInputsRejectNullAsync()
    {
        using HttpAuthService service = CreateService(HttpStatusCode.OK, "{}");
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.LoginAsync(null!, "test", default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.LoginAsync("emilys", null!, default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync(null!, "demo@example.test", "test", default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync("demo", null!, "test", default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync("demo", "demo@example.test", null!, default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.ResetPasswordAsync(null!, "test", default));
        await TestCheck.ThrowsAsync<ArgumentNullException>(() => service.ResetPasswordAsync("emilys", null!, default));
    }

    /// <summary>Feature 释放服务后，底层传输也被释放。</summary>
    [Test]
    public async Task ServiceOwnsTransportAsync()
    {
        bool disposed = false;
        HttpAuthService service = CreateService(
            (_, _) => Task.FromResult(Response(HttpStatusCode.OK, "{}")),
            () => disposed = true);

        service.Dispose();

        await Assert.That(disposed).IsTrue();
    }

    /// <summary>使用固定响应创建拥有传输的真实客户端。</summary>
    private static HttpAuthService CreateService(HttpStatusCode code, string body)
        => CreateService((_, _) => Task.FromResult(Response(code, body)));

    /// <summary>创建测试客户端，并显式把传输所有权移交给服务。</summary>
    private static HttpAuthService CreateService(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        Action? disposed = null)
    {
        ArgumentNullException.ThrowIfNull(send);

        ProbeHttpHandler? handler = null;
        try
        {
            handler = new ProbeHttpHandler(send, disposed);
            HttpAuthService service = new(handler);
            handler = null;
            return service;
        }
        finally
        {
            handler?.Dispose();
        }
    }

    /// <summary>构造由客户端负责释放的测试响应。</summary>
    private static HttpResponseMessage Response(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>只替换 HTTP 传输，不替换被测客户端的序列化、协议或错误处理。</summary>
    private sealed class ProbeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        private readonly Action? _disposed;

        /// <summary>初始化受控 HTTP 传输探针。</summary>
        /// <param name="send">用于生成受控 HTTP 响应的发送委托。</param>
        /// <param name="disposed">传输被释放时执行的可选通知。</param>
        public ProbeHttpHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
            Action? disposed)
        {
            _send = send;
            _disposed = disposed;
        }

        /// <summary>获取是否已释放。</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>执行受控传输。</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _send(request, cancellationToken);

        /// <summary>记录所属客户端释放。</summary>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _disposed?.Invoke();
            }

            base.Dispose(disposing);
        }
    }
}
