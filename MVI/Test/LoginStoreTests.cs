
namespace Test;
using Xunit;
using System.Threading.Tasks;
using Moq;

public class LoginStoreTests
{
    private readonly LoginStore _store;
    private readonly Mock<LoginReducer> _reducerMock;

    public LoginStoreTests()
    {
        _reducerMock = new Mock<LoginReducer>();
        _store = new LoginStore(new LoginState("", "", false, null), _reducerMock.Object);
    }

    [Fact]
    public async Task ProcessIntent_UsernameChanged_UpdatesState()
    {
        // Arrange
        var newState = new LoginState("newUser", "", false, null);
        _reducerMock
            .Setup(r => r.ReduceAsync(It.IsAny<LoginState>(), new UsernameChanged("newUser")))
            .ReturnsAsync(newState);

        // Act
        await _store.ProcessIntentAsync(new UsernameChanged("newUser") );

        // Assert
        Assert.Equal("newUser", _store.CurrentState.Username);
    }

    [Fact]
    public async Task ProcessIntent_Submit_WithInvalidCredentials_SetsError()
    {
        // Arrange
        var errorState = new LoginState("", "", false, "Username and password are required.");
        _reducerMock
            .Setup(r => r.ReduceAsync(It.IsAny<LoginState>(), new Submit()))
            .ReturnsAsync(errorState);

        // Act
        await _store.ProcessIntentAsync(new Submit());

        // Assert
        Assert.Equal("Username and password are required.", _store.CurrentState.ErrorMessage);
    }
}