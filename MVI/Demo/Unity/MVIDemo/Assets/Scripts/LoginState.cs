namespace MVI.Demo
{
    public record LoginState(
        string Username,
        string Password,
        bool IsLoading,
        string ErrorMessage)
        : IState

    {
        public LoginState SetError(string errorMessage) => new LoginState(Username, Password, IsLoading, errorMessage);
        public string Username { get; } = Username;
        public string Password { get; } = Password;
        public bool IsLoading { get; } = IsLoading;
        
        public string ErrorMessage { get; } = ErrorMessage;
    }

}