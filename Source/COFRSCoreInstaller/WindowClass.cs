using System;
using System.Windows.Forms;

namespace COFRSCoreInstaller
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
