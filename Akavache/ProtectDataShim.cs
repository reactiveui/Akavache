using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Akavache
{
    public static class ProtectedData
    {
        public static byte[] Protect(byte[] originalData, byte[] entropy)
        {
            return originalData;
        }

        public static byte[] Unprotect(byte[] originalData, byte[] entropy)
        {
            return originalData;
        }
    }
}