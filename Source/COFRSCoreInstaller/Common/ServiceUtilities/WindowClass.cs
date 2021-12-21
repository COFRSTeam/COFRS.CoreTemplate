using System;
using System.Windows.Interop;

namespace COFRS.Template.Common.ServiceUtilities
{
    public class WindowClass : IWin32Window
    {
        public IntPtr Handle { get; set; }

        public WindowClass(IntPtr hwnd)
        {
            Handle = hwnd;
        }
    }
}
