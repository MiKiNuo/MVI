using MVI;

namespace Test;

public class LoginIntent :IIntent
{
    public class UsernameChanged : LoginIntent
    {
        public UsernameChanged(string newUsername)
        {
            UserName = newUsername;
        }
        public string UserName { get; set; }
    }
    
    public class PasswordChanged : LoginIntent
    {
        public PasswordChanged(string newPassword)
        {
            Password = newPassword;
        }
        public string Password { get; set; }
    }
    
    public class Submit : LoginIntent
    {
        public void DOSubmit()
        {
            
        }
    }
}