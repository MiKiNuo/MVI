using MVI;

namespace Winform;

public record MviResult : IMviResult
{
    public string Message { get; set; }
    public int Code { set; get; }
    public object Data { get; set; }
}