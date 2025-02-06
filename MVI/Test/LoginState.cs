using MVI;

namespace Test;

// LoginState.cs
public class LoginState : IState
{
    public string Username { get; }
    public string Password { get; }
    public bool IsLoading { get; }
    public string ErrorMessage { get; }

    public LoginState(string username, string password, bool isLoading, string errorMessage)
    {
        Username = username;
        Password = password;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
    }

    public LoginState UpdateUsername(string username) =>
        new LoginState(username, Password, IsLoading, ErrorMessage);

    public LoginState UpdatePassword(string password) =>
        new LoginState(Username, password, IsLoading, ErrorMessage);

    public LoginState StartLoading() =>
        new LoginState(Username, Password, true, null);

    public LoginState StopLoading() =>
        new LoginState(Username, Password, false, ErrorMessage);

    public LoginState SetError(string errorMessage) =>
        new LoginState(Username, Password, IsLoading, errorMessage);
}

