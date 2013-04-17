using System;

namespace Akavache
{
    public class LoginInfo
    {
        public LoginInfo(string username, string password)
        {
            UserName = username;
            Password = password;
        }

        internal LoginInfo(Tuple<string, string> usernameAndLogin) : this(usernameAndLogin.Item1, usernameAndLogin.Item2)
        {
        }

        public string UserName { get; private set; }
        public string Password { get; private set; }
    }
}
