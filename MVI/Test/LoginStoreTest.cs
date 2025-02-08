namespace Test;
using Xunit;
using R3;
public class LoginStoreTest
{
    [Fact]
    public void Login_Intent_Submit_UserName_NotEmpty()
    {
        // Arrange
        var store = new LoginStore(new LoginState("admin","",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.NotNull( stateObserver[1].Username);
    }

    [Fact]
    public void Login_Intent_Submit_Password_NotEmpty()
    {
        // Arrange
        var store = new LoginStore(new LoginState("","888888",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.NotNull( stateObserver[1].Password);
    }
    
    [Fact]
    public void Login_Intent_Submit_UserName_NotCorrect()
    {
        // Arrange
        var store = new LoginStore(new LoginState("123","123",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.NotEqual("admin", stateObserver[1].Username);
    
    }
    
    [Fact]
    public void Login_Intent_Submit_Password_NotCorrect()
    {
        // Arrange
        var store = new LoginStore(new LoginState("123","123",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.NotEqual("888888", stateObserver[1].Password);
    }
    
    [Fact]
    public void Login_Intent_Submit_Password_Correct()
    {
        // Arrange
        var store = new LoginStore(new LoginState("admin","888888",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.Equal("888888", stateObserver[1].Password);
    }
    
        
    [Fact]
    public void Login_Intent_Submit_UserName_Correct()
    {
        // Arrange
        var store = new LoginStore(new LoginState("admin","888888",false,""));
        var stateObserver = store.State.ToLiveList();

        // Act
        store.Process(Observable.Return((LoginIntent )new Submit()));

        // Assert
        Assert.Equal("admin", stateObserver[1].Username);
    }
}