using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace System
{
    public class Lazy<T>
    {
        public Lazy(Func<T> ValueFetcher)
        {
            _Value = ValueFetcher();
        }

        T _Value;
        public T Value
        {
            get { return _Value; }
        }
    }
}

namespace System
{
    internal class Tuple<T1, T2>
    {
        public Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
            var hash1 = (item1 != null) ? item1.GetHashCode() : 0;
            var hash2 = (item2 != null) ? item2.GetHashCode() : 0;
            hash = hash1 ^ hash2;
        }
        public Tuple() { }
    
        private int hash;
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
    
    
        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2>;
            if (other == null)
                return false;
    
            bool equals1 = (Item1 != null) ? Item1.Equals(other.Item1) : other.Item1 == null;
            bool equals2 = (Item2 != null) ? Item2.Equals(other.Item2) : other.Item2 == null;
            return equals1 && equals2;
        }
    
        public override int GetHashCode()
        {
            return hash;
        }
    }
}