using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache
{
    public static class LoginMixin
    {
        /// <summary>
        /// Save a user/password combination in a secure blob cache. Note that
        /// this method only allows exactly *one* user/pass combo to be saved,
        /// calling this more than once will overwrite the previous entry.
        /// </summary>
        /// <param name="user">The user name to save.</param>
        /// <param name="password">The associated password</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        public static IObservable<Unit> SaveLogin(this ISecureBlobCache This, string user, string password, string host = "default", DateTimeOffset? absoluteExpiration = null)
        {
            return This.InsertObject("login:" + host, new Tuple<string, string>(user, password), absoluteExpiration);
        }

        /// <summary>
        /// Returns the currently cached user/password. If the cache does not
        /// contain a user/password, this returns an Observable which
        /// OnError's with KeyNotFoundException.
        /// </summary>
        /// <returns>A Future result representing the user/password Tuple.</returns>
        public static IObservable<LoginInfo> GetLoginAsync(this ISecureBlobCache This, string host = "default")
        {
            return This.GetObject<Tuple<string, string>>("login:" + host).Select(x => new LoginInfo(x));
        }

        /// <summary>
        /// Erases the login associated with the specified host
        /// </summary>
        public static IObservable<Unit> EraseLogin(this ISecureBlobCache This, string host = "default")
        {
            return This.InvalidateObject<Tuple<string, string>>("login:" + host);
        }
    }
}