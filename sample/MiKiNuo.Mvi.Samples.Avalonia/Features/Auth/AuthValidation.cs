using System.Text.RegularExpressions;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

internal static class AuthValidation
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string? Register(string userName, string email, string password, string confirmation)
    {
        if (userName.Trim().Length < 3) return "用户名至少需要 3 个字符。";
        if (!EmailPattern.IsMatch(email)) return "邮箱格式不正确。";
        if (password.Length < 6) return "密码长度至少为 6 位。";
        if (password != confirmation) return "两次输入的密码不一致。";
        return null;
    }

    internal static string? ResetPassword(string userName, string password, string confirmation)
    {
        if (userName.Trim().Length < 3) return "用户名至少需要 3 个字符。";
        if (password.Length < 6) return "新密码长度至少为 6 位。";
        if (password != confirmation) return "两次输入的密码不一致。";
        return null;
    }
}
