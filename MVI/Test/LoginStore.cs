using MVI;

namespace Test;

public class LoginStore : Store<LoginState,LoginIntent>
{
    public LoginStore(LoginState initialState, IReducer<LoginState, LoginIntent> reducer) : base(initialState, reducer)
    {
    }
}