using MVI;
using R3;

namespace Winform;

public partial class LoginView : Form,IView<LoginState,LoginIntent>
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
        _sureBtn.Click += (sender, args) =>
        {
            EmitIntent(new Submit());
        };

        _cancleBtn.Click += (sender, args) =>
        {
            EmitIntent(new Cancel());
        };
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
        return new LoginState(_nameTxt.Text,_pwdTxt.Text,false,"");
    }

    public void EmitIntent(LoginIntent intent)
    {
        var currentState = GetCurrentState();
        Store.UpdateState(currentState);
        _intentSubject.OnNext(intent);
    }
    
}