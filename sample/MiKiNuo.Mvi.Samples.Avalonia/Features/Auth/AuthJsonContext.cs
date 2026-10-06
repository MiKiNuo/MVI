using System.Text.Json.Serialization;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>第三方登录请求；Username 必须序列化为接口要求的 username，而不是 userName。</summary>
/// <param name="Username">测试用户名。</param>
/// <param name="Password">测试密码。</param>
/// <param name="ExpiresInMins">测试令牌的有效分钟数。</param>
internal sealed record LoginRequest(string Username, string Password, int ExpiresInMins = 30)
{
    /// <summary>诊断不输出敏感字段。</summary>
    /// <returns>请求类别。</returns>
    public override string ToString() => nameof(LoginRequest);
}

/// <summary>第三方用户新增请求，仅用于测试，不会创建可登录的新账号。</summary>
/// <param name="Username">演示用户名。</param>
/// <param name="Email">演示邮箱。</param>
/// <param name="Password">演示密码。</param>
internal sealed record RegisterRequest(string Username, string Email, string Password)
{
    /// <summary>诊断不输出敏感字段。</summary>
    /// <returns>请求类别。</returns>
    public override string ToString() => nameof(RegisterRequest);
}

/// <summary>用户更新接口的演示请求，不包含恢复码或私有服务协议字段。</summary>
/// <param name="Password">演示新密码。</param>
internal sealed record ResetRequest(string Password)
{
    /// <summary>诊断不输出敏感字段。</summary>
    /// <returns>请求类别。</returns>
    public override string ToString() => nameof(ResetRequest);
}

/// <summary>客户端内部的编译期 JSON 元数据，不再有客户端与本地服务器共享的协议工程。</summary>
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(ResetRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
internal partial class AuthJsonContext : JsonSerializerContext { }
