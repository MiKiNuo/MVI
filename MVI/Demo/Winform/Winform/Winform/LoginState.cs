using MVI;

namespace Winform;

public record LoginState(
    string Username,
    string Password,
    bool IsLoading,
    string ErrorMessage)
    : IState

{
    public LoginState SetError(string errorMessage) => new LoginState(Username, Password, IsLoading, errorMessage);

}