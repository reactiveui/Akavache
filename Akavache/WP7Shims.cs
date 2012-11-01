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