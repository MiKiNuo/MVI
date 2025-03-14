﻿using MVI;

namespace Test;

public record LoginIntent : IIntent
{
    public virtual async ValueTask<IMviResult> HandleIntentAsync(IState request, CancellationToken ct = default)
    {
        await ValueTask.CompletedTask;
        return null;
    }
}

public record Submit : LoginIntent
{
    public override async ValueTask<IMviResult> HandleIntentAsync(IState request, CancellationToken ct = default)
    {
        await ValueTask.CompletedTask;
        var curState = request as LoginState;         
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
        }else if (curState.Password != "888888")
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