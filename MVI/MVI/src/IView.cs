using R3;

namespace MVI
{
    public interface IView<TState, TIntent> where TState : IState where TIntent : IIntent
    {
        void Render(TState state);
        Observable<TIntent> Intents { get; }
    }
}