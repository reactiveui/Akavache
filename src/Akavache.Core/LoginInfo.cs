using System;

namespace Akavache
{
    public class LoginInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginInfo"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public LoginInfo(string username, string password)
        {
            UserName = username;
            Password = password;
        }

        internal LoginInfo(Tuple<string, string> usernameAndLogin) : this(usernameAndLogin.Item1, usernameAndLogin.Item2)
        {
        }

        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; private set; }
    }
}
