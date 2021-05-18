using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
