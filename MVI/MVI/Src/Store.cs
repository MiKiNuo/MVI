using System.Threading.Tasks;
using R3;

namespace MVI.Src
{
    public abstract class Store<TState, TIntent> where TState : class, IState where TIntent : IIntent
    {
        private readonly ReactiveProperty<TState> _state;
        private readonly IReducer<TState, TIntent> _reducer;

        public Observable<TState> State => _state;
        public TState CurrentState => _state.Value;

        public Store(TState initialState, IReducer<TState, TIntent> reducer)
        {
            _reducer = reducer;
            _state = new ReactiveProperty<TState>(initialState);
        }

        public virtual async Task ProcessIntentAsync(TIntent intent)
        {
            var newState = await _reducer.ReduceAsync(CurrentState, intent);
            _state.Value = newState;
        }
    }
}