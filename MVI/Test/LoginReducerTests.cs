namespace Test;
using Xunit;
using System.Threading.Tasks;

public class LoginReducerTests
{
    private readonly LoginReducer _reducer;

    public LoginReducerTests()
    {
        _reducer = new LoginReducer();
    }

    [Fact]
    public async Task Reduce_UsernameChanged_UpdatesUsername()
    {
        var initialState = new LoginState("", "", false, null);
        var newState = await _reducer.ReduceAsync(initialState, new LoginIntent.UsernameChanged("newUser"));

        Assert.Equal("newUser", newState.Username);
    }

    [Fact]
    public async Task Reduce_PasswordChanged_UpdatesPassword()
    {
        var initialState = new LoginState("", "", false, null);
        var newState = await _reducer.ReduceAsync(initialState, new LoginIntent.PasswordChanged("newPass"));

        Assert.Equal("newPass", newState.Password);
    }

    [Fact]
    public async Task Reduce_Submit_WithEmptyCredentials_SetsError()
    {
        var initialState = new LoginState("", "", false, null);
        var newState = await _reducer.ReduceAsync(initialState, new LoginIntent.Submit());

        Assert.Equal("用户名和密码不能为空", newState.ErrorMessage);
    }

    [Fact]
    public async Task Reduce_Submit_WithValidCredentials_ClearsError()
    {
        var initialState = new LoginState("admin", "password", false, null);
        var newState = await _reducer.ReduceAsync(initialState, new LoginIntent.Submit());

        Assert.Null(newState.ErrorMessage);
    }
}