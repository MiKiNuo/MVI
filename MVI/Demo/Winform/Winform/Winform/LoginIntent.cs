using MVI;

namespace Winform;

public record LoginIntent : IIntent
{
    public virtual ValueTask<IMviResult> HandleIntentAsync(IState state, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public record Submit : LoginIntent
{
    public override async ValueTask<IMviResult> HandleIntentAsync(IState state, CancellationToken ct = default)
    {
        await ValueTask.CompletedTask;
        var curState = state as LoginState;
        var mviResult = new MviResult();
        if (curState is null)
        {
            mviResult.Code = -1;
            mviResult.Message = "状态类型不对";
            return mviResult;
        }
        if (string.IsNullOrWhiteSpace(curState.Password) || string.IsNullOrWhiteSpace(curState.Username))
        {
            mviResult.Code = -1;
            mviResult.Message = "用户名或密码不能为空";
        }
        else if (curState.Username != "admin")
        {
            mviResult.Code = -1;
            mviResult.Message = "用户名错误";
        }
        else if (curState.Password != "888888")
        {
            mviResult.Code = -1;
            mviResult.Message = "密码错误";
        }
        else if (curState.Password == "888888" && curState.Username == "admin")
        {
            mviResult.Code = 0;
            mviResult.Message = "登录成功";
            mviResult.Data = new LoginState(curState.Username, curState.Password, false, "");
        }

        return mviResult;
    }
}

public record Cancel : LoginIntent
{

}