# MVI
- en [English](README_en.md)
这是使用C#实现的（Model-View-Inten）MVI架构，具有以下特点：
- 使用了https://github.com/Cysharp/R3作为响应式库。
- 跨平台特点，理论上支持C#语言的任何平台，目前测试了Winform、Unity两个平台。
- 简单易懂，核心代码不到100行。
- 支持单元测试
## 教程
### 一、 winform使用教程
我们以登录界面作为示例进行讲解，定义的State最好是record类型或者seal类都可以，需要实现IState接口，登录界面需要用户名、密码、是否正在加载等状态，可以自己后面增加，如下面代码所示：
#### 1、定义State
    public record LoginState(
    string Username,
    string Password,
    bool IsLoading,
    string ErrorMessage)
    : IState
    
    {
        public LoginState SetError(string errorMessage) => new LoginState(Username, Password, IsLoading, errorMessage);
    
    }
定义完状态后我们在定义意图类，需要实现IIntent接口，然后给个虚方法，因为对于一个界面有很多的意图，所以以登录界面为例，登录可以点击登录、取消登录、加载数据等意图，每个意图都可以需要继承IIntent，如下图所示代码
#### 2、定义Intent

    public record LoginIntent : IIntent
    {
        public virtual ValueTask<IMviResult> HandleIntentAsync(IState state, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
可以创建一个结果类，包含错误码、消息对象以及相关数据可以自行扩展
#### 3、定义MviResult
    public record MviResult : IMviResult
    {
        public string Message { get; set; }
        public int Code { set; get; }
        public object Data { get; set; }
    }
Store需要获取State和Intent所以我们要定定义好上面的State和Intent，需要重写Reducer方法根据结果生成新的State，如下所示代码
#### 4、定义Store
    public class LoginStore : Store<LoginState, LoginIntent>
    {
        public LoginStore(LoginState initialState) : base(initialState)
        {
        }

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
新建一个Form或者 UserControl，需要实现IView接口，Iview接口需要绑定State和Intent类型，剩下的就是进行绑定和处理渲染的事情，如下代码所示
#### 5、View实现
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
    ｝

#### 6、示例代码
具体的代码教程请查看Demo文件夹下的Winform程序，直接打开运行即可

### 二、Unity使用教程
    State和Intent的教程是一样的，所以直接复制上面Winform的可以用，却别在于View层的实现不一样，还需要将我们的MVI相关的代码或者DLL导入到项目或者Plugins文件夹中，居来可以参照demo

#### 1、R3包的导入
    具体的导入教程可以参考R3官方文档不再叙述

#### 2、View实现
    抽象出来了BaseView跟Winform实现的一样，只不过进一步进行抽象而已，具体代码如如下：
#### 3、BaseView
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
#### 4、具体View实现
进一步抽象了BaseView后，我们对应的View只需要继承BaseView即可，举例登陆界面，如下代码所示：

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
    
#### 5、运行示例
    运行示例，直接打开Demo\Unity\MVIDemo\Assets\Scenes\SampleScene.unity即可
