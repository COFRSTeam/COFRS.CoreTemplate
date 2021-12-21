using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.ServiceUtilities
{
    internal static class WinNative
    {
        public const int WM_PAINT = 15;
        public const int WM_ERASEBKGND = 20;
        public const int WM_NCCREATE = 129;
        public const int WM_NCDESTROY = 130;
        public const int WM_NCCALCSIZE = 131;
        public const int WM_NCHITTEST = 132;
        public const int WM_NCPAINT = 133;
        public const int WM_NCACTIVATE = 134;

        //GetDCEx Flags
        public const int DCX_WINDOW = 0x00000001;
        public const int DCX_CACHE = 0x00000002;
        public const int DCX_PARENTCLIP = 0x00000020;
        public const int DCX_CLIPSIBLINGS = 0x00000010;
        public const int DCX_CLIPCHILDREN = 0x00000008;
        public const int DCX_NORESETATTRS = 0x00000004;
        public const int DCX_LOCKWINDOWUPDATE = 0x00000400;
        public const int DCX_EXCLUDERGN = 0x00000040;
        public const int DCX_INTERSECTRGN = 0x00000080;
        public const int DCX_INTERSECTUPDATE = 0x00000200;
        public const int DCX_VALIDATE = 0x00200000;

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            readonly long x;
            readonly long y;

            public override string ToString()
            {
                return $"({x},{y})";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
            public uint lPrivate;

            public override string ToString()
            {
                return $"{handle}, {msg}, {wParam}, {lParam}";
            }
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr window, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        //RECT Structure
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        //WINDOWPOS Structure
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndinsertafter;
            public int x, y, cx, cy;
            public int flags;
        }

        //NCCALCSIZE_PARAMS Structure
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            public RECT rgrc0, rgrc1, rgrc2;
            public WINDOWPOS lppos;
        }

        //SetWindowTheme UXtheme Function
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int SetWindowTheme(
            IntPtr hWnd,
            String pszSubAppName,
            String pszSubIdList);

        //GetWindowRect User32 Function
        [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetWindowRect(
            IntPtr hwnd,
            out RECT lpRect
            );

        //GetWindowDC User32 Function
        [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetWindowDC(
            IntPtr hWnd
            );

        //GetDCEx User32 Function
        [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetDCEx(
            IntPtr hWnd,
            IntPtr hrgnClip,
            int flags
            );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);
    }
}
