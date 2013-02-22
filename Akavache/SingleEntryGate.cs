using System.Threading;

namespace Akavache
{
    public class SingleEntryGate
    {
        int enterCount;

        /// <summary>
        /// Returns true for the first and only entry. After that, it returns false.
        /// </summary>
        /// <returns></returns>
        public bool Enter()
        {
            IsClosed = true;
            return Interlocked.Increment(ref enterCount) == 1;
        }

        public bool IsClosed  { get; private set; }
    }
}
