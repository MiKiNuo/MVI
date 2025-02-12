using R3;
using UnityEngine;

namespace MVI.Demo
{
    public abstract class BaseView<TState,TIntent> :MonoBehaviour,IView<TState,TIntent>
        where TState : IState
        where TIntent : IIntent
    {
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void Bind(Store<TState, TIntent> store)
        {
            throw new System.NotImplementedException();
        }

        public void Render(TState state)
        {
            throw new System.NotImplementedException();
        }

        public Observable<TIntent> IntentStream { get; }
    }
}