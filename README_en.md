# MVI
- zh [chinese](README.md)
This is an implementation of the Model-View-Intent (MVI) architecture using C#, with the following features:

- Uses R3 as the reactive library.
-Cross-platform support for any C#-compatible platform. Tested with Windows Forms and Unity.
-Simple and easy to understand, with core code under 100 lines.
-Supports unit testing.
## Tutorials
### I. Windows Forms Tutorial
We use a login interface as an example. The State should ideally be a record or sealed class and implement the IState interface. For a login screen, the state includes fields like username, password, loading status, and error messages.

#### 1. Define the State

      public record LoginState(  
      string Username,  
      string Password,  
      bool IsLoading,  
      string ErrorMessage)  
      : IState  
      {  
         public LoginState SetError(string errorMessage) => new LoginState(Username, Password, IsLoading, errorMessage);  
      }
#### 2. Define the Intent
   Intents (user actions) must implement IIntent. For a login screen, common intents include submitting credentials or canceling.


   public record LoginIntent : IIntent  
   {  
      public virtual ValueTask<IMviResult> HandleIntentAsync(IState state, CancellationToken ct = default)  
      {  
         throw new NotImplementedException();  
      }  
   }
#### 3. Define the MviResult
   A result class to encapsulate outcomes like error codes, messages, and data:


   public record MviResult : IMviResult  
   {  
      public string Message { get; set; }  
      public int Code { set; get; }  
      public object Data { get; set; }  
   }
#### 4. Define the Store
   The Store manages state updates based on results from intents:
   public class LoginStore : Store<LoginState, LoginIntent>  
   {  
   public LoginStore(LoginState initialState) : base(initialState) { }

    protected override LoginState Reducer(LoginState currentState, IMviResult result)  
    {  
        if (result is not MviResult mviResult) return currentState;  
        if (mviResult.Code == 0)  
        {  
            var data = mviResult.Data as LoginState;  
            return data != null ? new LoginState(data.Username, data.Password, false, mviResult.Message) : currentState.SetError(mviResult.Message);  
        }  
        else  
        {  
            return currentState.SetError(mviResult.Message);  
        }  
    }  
   }
####  5. Implement the View
   Create a Form or UserControl that implements IView<LoginState, LoginIntent>:

   public partial class LoginView : Form, IView<LoginState, LoginIntent>  
   {  
      private readonly LoginStore _loginStore = new(new LoginState("", "", false, ""));  
      private readonly Subject<LoginIntent> _intentSubject = new();
   
       public LoginView()  
       {  
           InitializeComponent();  
       }  
   
       protected override void OnLoad(EventArgs e)  
       {  
           base.OnLoad(e);  
           Bind();  
       }  
   
       public void Bind()  
       {  
           Store.Bind(this);  
           _sureBtn.Click += (sender, args) => EmitIntent(new Submit());  
           _cancleBtn.Click += (sender, args) => EmitIntent(new Cancel());  
       }  
   
       public void Render(LoginState state)  
       {  
           _nameTxt.Text = state.Username;  
           _pwdTxt.Text = state.Password;  
           _infoLbl.Text = state.ErrorMessage;  
       }  
   
       public Subject<LoginIntent> IntentSubject => _intentSubject;  
       public Store<LoginState, LoginIntent> Store => _loginStore;  
   
       public LoginState GetCurrentState()  
       {  
           return new LoginState(_nameTxt.Text, _pwdTxt.Text, false, "");  
       }  
   
       public void EmitIntent(LoginIntent intent)  
       {  
           var currentState = GetCurrentState();  
           Store.UpdateState(currentState);  
           _intentSubject.OnNext(intent);  
       }  
   }
#### 6. Example Code
   See the Demo folder for a complete Windows Forms example.

### II. Unity Tutorial
   The State and Intent definitions are identical to the Windows Forms example. The primary difference lies in the View implementation.

#### 1. Import the R3 Package
   Follow the R3 documentation to import the package into your Unity project.

#### 2. BaseView Abstraction
   A reusable BaseView class simplifies MonoBehaviour integration:

   public abstract class BaseView<TState, TIntent> : MonoBehaviour, IView<TState, TIntent>  
   where TState : IState  
   where TIntent : IIntent  
   {  
      private readonly Subject<TIntent> _intentSubject = new();  
      public abstract Store<TState, TIntent> Store { get; }
   
       public void Dispose() => _intentSubject.Dispose();  
   
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
#### 3. Concrete View Implementation
   Extend BaseView for a login screen:

   public class LoginView : BaseView<LoginState, LoginIntent>  
   {  
      [SerializeField] private InputField _userNameInput;  
      [SerializeField] private InputField _passwordInput;  
      [SerializeField] private Text _infoText;  
      [SerializeField] private Button _sureBtn;  
      [SerializeField] private Button _cancleBtn;
   
       private readonly LoginStore _loginStore = new(new LoginState("", "", false, ""));  
   
       private void Start()  
       {  
           _sureBtn.onClick.AddListener(() => EmitIntent(new Submit()));  
           _cancleBtn.onClick.AddListener(() => EmitIntent(new Cancel()));  
           Bind();  
       }  
   
       public override Store<LoginState, LoginIntent> Store => _loginStore;  
   
       public override void Render(LoginState state)  
       {  
           _userNameInput.text = state.Username;  
           _passwordInput.text = state.Password;  
           _infoText.text = state.ErrorMessage;  
       }  
   
       public override LoginState GetCurrentState()  
       {  
           return new LoginState(_userNameInput.text, _passwordInput.text, false, "");  
       }  
   }
#### 4. Run the Example
   Open Demo/Unity/MVIDemo/Assets/Scenes/SampleScene.unity to test the Unity implementation.