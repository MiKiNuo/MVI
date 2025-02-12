using R3;
using UnityEngine;
using UnityEngine.UI;


namespace MVI.Demo
{
    public class LoginView : BaseView<LoginState, LoginIntent>
    {
        [SerializeField] private InputField _userNameInput;

        [SerializeField] private InputField _passwordInput;

        [SerializeField] private Text _infoText;

        [SerializeField] private Button _sureBtn;

        [SerializeField] private Button _cancleBtn;

        private readonly Subject<LoginIntent> _loginSubject = new();

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
}