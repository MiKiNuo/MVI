namespace MVI.Demo
{
    public class LoginStore : Store<LoginState, LoginIntent>
    {
        public LoginStore(LoginState initialState) : base(initialState)
        {
            
        }
        protected override LoginState Reducer(LoginState currentState, IMviResult result)
        {
            if (result is not MviResult mviResult) return currentState;
            if (mviResult.Code == 0)
            {
                var data = mviResult.Data as LoginState;
                return data != null ? new LoginState(data.Username, data.Password, false, mviResult.Message) : currentState.SetError(mviResult.Message);
            }
            else
            {
                return currentState.SetError(mviResult.Message);
            }
        }
    }
}