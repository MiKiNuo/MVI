using System;
using R3;

namespace MVI
{
    public interface IView<TState, TIntent> : IDisposable
        where TState : IState
        where TIntent : IIntent
    {
        void Bind(Store<TState, TIntent> store);
        void Render(TState state);
        Observable<TIntent> IntentStream { get; }
    }
}