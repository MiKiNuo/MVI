using System.Threading.Tasks;

namespace MVI
{
    public interface IReducer<TState, TIntent> where TState : IState where TIntent : IIntent
    {
        ValueTask<TState>  ReduceAsync(TState currentState, TIntent intent);
    }
}