using MVI;

namespace Test;
public class LoginReducer : IReducer<LoginState, LoginIntent>
{
    private async ValueTask<LoginState> HandleSubmit(LoginState currentState)
    {
        if (string.IsNullOrEmpty(currentState.Username) || string.IsNullOrEmpty(currentState.Password))
        {
            return currentState.SetError("Username and password are required.");
        }

        var loadingState = currentState.StartLoading();

        try
        {
            // 模拟登录请求
            await Task.Delay(1000); // 模拟网络请求
            bool isLoginSuccessful = await Authenticate(currentState.Username, currentState.Password);

            return isLoginSuccessful
                ? loadingState.StopLoading()
                : loadingState.SetError("Invalid credentials.");
        }
        catch (Exception ex)
        {
            return loadingState.SetError($"Login failed: {ex.Message}");
        }
    }

    private async Task<bool> Authenticate(string username, string password)
    {
        // 模拟认证逻辑
        await Task.Delay(500);
        return username == "admin" && password == "password";
    }

    public async ValueTask<LoginState> ReduceAsync(LoginState currentState, LoginIntent intent)
    {
        switch (intent)
        {
            case UsernameChanged:
                var usernameChanged = intent as UsernameChanged;
                currentState = currentState.UpdateUsername(usernameChanged.UserName);
                break;

            case PasswordChanged:
                var passwordChanged = intent as PasswordChanged;
                currentState = currentState.UpdatePassword(passwordChanged.Password);
                break;
            case Submit:
                var submit = intent as Submit;
                if (string.IsNullOrEmpty(currentState.Username) || string.IsNullOrEmpty(currentState.Password))
                {
                    currentState = currentState.SetError("用户名和密码不能为空");
                }
                

                //await HandleSubmit(currentState);
                break;
        }

        return currentState;
    }
}