using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace MVI
{
    public abstract class Store<TState, TIntent> : IDisposable
        where TState : IState
        where TIntent : IIntent
    {
        private readonly BehaviorSubject<TState> _stateSubject;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected Store(TState initialState)
        {
            _stateSubject = new BehaviorSubject<TState>(initialState);
        }

        public Observable<TState> State => _stateSubject;

        public void Bind(IView<TState, TIntent> view)
        {
            Process(view.IntentSubject);
            _stateSubject
                .Subscribe(view.Render)
                .AddTo(_disposables);
        }

        protected async ValueTask<Observable<IMviResult>> ProcessIntentAsync(TIntent intent,
            CancellationToken ct = default)
        {
            var result = await intent.HandleIntentAsync(_stateSubject.Value);
            return Observable.Return(result);
        }

        public void Process(Observable<TIntent> intents)
        {
            intents
                .SelectAwait(ProcessIntentAsync)
                .Switch()
                .Subscribe(Reducer)
                .AddTo(_disposables);
        }

        private void Reducer(IMviResult result)
        {
            var newState = Reducer(_stateSubject.Value, result);
            UpdateState(newState);
        }


        public void UpdateState(TState state)
        {
            _stateSubject.OnNext(state);
        }

        protected abstract TState Reducer(TState currentState, IMviResult result);

        public void Dispose() => _disposables.Dispose();
    }
}