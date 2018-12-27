using System;
using System.Collections.Generic;
using System.Text;

namespace Akavache.Core
{
    [AttributeUsage(AttributeTargets.All)]
    internal class PreserveAttribute : Attribute
    {
        public bool AllMembers;
        public bool Conditional;

        public PreserveAttribute(bool allMembers, bool conditional)
        {
            AllMembers = allMembers;
            Conditional = conditional;
        }

        public PreserveAttribute()
        {
        }
    }
}
