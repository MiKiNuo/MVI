using R3;
using UnityEngine;

namespace MVI.Demo
{
    public abstract class BaseView<TState, TIntent> : MonoBehaviour, IView<TState, TIntent>
        where TState : IState
        where TIntent : IIntent
    {
        private readonly Subject<TIntent> _intentSubject = new();
        public abstract Store<TState, TIntent> Store { get; }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void Bind()
        {
            Store.Bind(this);
        }

        public abstract void Render(TState state);
        public abstract TState GetCurrentState();

        public void EmitIntent(TIntent intent)
        {
            var curState = GetCurrentState();
            Store.UpdateState(curState);
            IntentSubject.OnNext(intent);
        }

        public Subject<TIntent> IntentSubject => _intentSubject;
    }
}