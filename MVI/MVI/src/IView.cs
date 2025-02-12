using System;
using R3;

namespace MVI
{
    public interface IView<TState, TIntent> : IDisposable
        where TState : IState
        where TIntent : IIntent
    {
        void Bind();
        void Render(TState state);
        Subject<TIntent> IntentSubject { get; }
        Store<TState, TIntent> Store { get; }
        TState GetCurrentState();
        void EmitIntent(TIntent intent);
    }
}