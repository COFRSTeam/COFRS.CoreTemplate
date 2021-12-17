using COFRSCoreCommon.Utilities;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSCoreCommon.Forms
{
    public partial class ProgressForm : Form
    {
        private string MessageText { get; set; }
        private DTE2 dte;
        private IVsUIShell2 uiShell;

        public ProgressForm()
        {
            InitializeComponent();
        }
        public ProgressForm(DTE2 dte2, IVsUIShell2 shell, string message)
        {
            InitializeComponent();
            MessageText = message;
            dte = dte2;
            uiShell = shell;

            uint win32Color;
            uiShell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_ENVIRONMENT_BACKGROUND, out win32Color);

            //translate it to a managed Color structure
            BackColor = ColorTranslator.FromWin32((int)win32Color);

            uiShell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_PANEL_TEXT, out win32Color);
            ForeColor = ColorTranslator.FromWin32((int)win32Color);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Message.Text = MessageText; 
        }
        
        protected override void DefWndProc(ref Message m)
        {
            switch ( m.Msg )
            {
                case WinNative.WM_ERASEBKGND:
                    WmEraseBackground(ref m);
                    break;

                case WinNative.WM_NCCALCSIZE:
                    WmNCCalcSize(ref m); 
                    break; 

                case WinNative.WM_NCPAINT:
                    WmNCPaint(ref m); 
                    break;

                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        private void WmEraseBackground(ref Message m)
        {
            m.Result = new IntPtr(1);
        }

        //WM_NCCALCSIZE
        private void WmNCCalcSize(ref Message m)
        {
            //Get Window Rect
            WinNative.RECT formRect = new WinNative.RECT();
            WinNative.GetWindowRect(m.HWnd, out formRect);

            //Check WPARAM
            if (m.WParam != IntPtr.Zero)    //TRUE
            {
                //When TRUE, LPARAM Points to a NCCALCSIZE_PARAMS structure
                var nccsp = (WinNative.NCCALCSIZE_PARAMS) Marshal.PtrToStructure(m.LParam, typeof(WinNative.NCCALCSIZE_PARAMS));
                IntPtr returnValue = IntPtr.Zero;

                if (nccsp.rgrc2.left == nccsp.rgrc1.left + 1 &&
                     nccsp.rgrc2.right == nccsp.rgrc1.right - 1 &&
                     nccsp.rgrc2.top == nccsp.rgrc1.top + 21 &&
                     nccsp.rgrc2.bottom == nccsp.rgrc1.bottom - 1)
                {
                    returnValue = new IntPtr(0x300);
                }

                //  Calculate the Source
                nccsp.rgrc2.left = nccsp.rgrc1.left;
                nccsp.rgrc2.top = nccsp.rgrc1.top;
                nccsp.rgrc2.right = nccsp.rgrc1.right;
                nccsp.rgrc2.bottom = nccsp.rgrc1.bottom;

                //  Calculate the destination
                nccsp.rgrc1.left = nccsp.rgrc0.left;
                nccsp.rgrc1.top = nccsp.rgrc0.top;
                nccsp.rgrc1.right = nccsp.rgrc0.right;
                nccsp.rgrc1.bottom = nccsp.rgrc0.bottom;

                //We're adjusting the size of the client area here. Right now, the client area is the whole form.
                //Adding to the Top, Bottom, Left, and Right will size the client area.

                nccsp.rgrc0.top += 21;      //30-pixel top border
                nccsp.rgrc0.left += 1;      //4-pixel left (resize) border
                nccsp.rgrc0.bottom -= 1;    //4-pixel bottom (resize) border
                nccsp.rgrc0.right -= 1;     //4-pixel right (resize) border

                //Set the structure back into memory
                Marshal.StructureToPtr(nccsp, m.LParam, true);

                m.Result = returnValue;
            }
            else    //FALSE
            {
                //When FALSE, LPARAM Points to a RECT structure
                var clnRect = (WinNative.RECT)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(WinNative.RECT));

                //Like before, we're adjusting the rectangle...
                //Adding to the Top, Bottom, Left, and Right will size the client area.
                clnRect.top += 21;      //30-pixel top border
                clnRect.bottom -= 1;    //4-pixel bottom (resize) border
                clnRect.left += 1;      //4-pixel left (resize) border
                clnRect.right -= 1;     //4-pixel right (resize) border

                //Set the structure back into memory
                Marshal.StructureToPtr(clnRect, m.LParam, true);
                m.Result = IntPtr.Zero;
            }
        }

        //WM_NCPAINT
        private void WmNCPaint(ref Message m)
        {
            //Store HDC
            IntPtr hdc = IntPtr.Zero;
            Graphics gfx = null;


            hdc = WinNative.GetWindowDC(m.HWnd);
            gfx = Graphics.FromHdc(hdc);

            uiShell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_PANEL_BORDER, out uint win32Color);
            var borderColor = ColorTranslator.FromWin32((int)win32Color);
            Brush borderBrush = new SolidBrush(borderColor);

            uiShell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_TITLEBAR_INACTIVE, out win32Color);
            var captionColor = ColorTranslator.FromWin32((int)win32Color);
            Brush captionBrush = new SolidBrush(captionColor);

            //Exclude Client Area
            gfx.ExcludeClip(new Rectangle(1, 21, Width-2, Height-22));  //Exclude Client Area (GetWindowDC grabs the WHOLE window's graphics handle)

            Rectangle rcWindow = new Rectangle(0, 0, Width, Height); 
            gfx.FillRectangle(borderBrush, rcWindow);

            Rectangle rcCaption = new Rectangle(1, 1, Width - 2, 20);
            gfx.FillRectangle(captionBrush, rcCaption);

            WinNative.ReleaseDC(m.HWnd, hdc);


            //Return Zero
            m.Result = IntPtr.Zero;
        }

        private void MyForm_NCPaint(object sender, PaintEventArgs e)
        {
            Color border = Color.FromArgb((int)dte.GetThemeColor(vsThemeColors.vsThemeColorPanelBorder));
            Color caption = Color.FromArgb((int)dte.GetThemeColor(vsThemeColors.vsThemeColorTitlebarActive));
            Color captionText = Color.FromArgb((int)dte.GetThemeColor(vsThemeColors.vsThemeColorTitlebarActiveText));

            border = Color.Red;
            caption = Color.Blue;
            captionText = Color.White;

 //           caption = Color.FromArgb(0x1e1e1e);

            var borderBrush = new SolidBrush(border);
            var captionBrush = new SolidBrush(caption);
            var captionTextBrush = new SolidBrush(captionText);


            WinNative.GetWindowRect(this.Handle, out WinNative.RECT rc);
            Rectangle rcc = new Rectangle(0, 0, rc.right - rc.left, rc.bottom - rc.top);


            e.Graphics.FillRectangle(borderBrush, rcc);
            e.Graphics.Clear(border);
           
            var rcCaption = new Rectangle(rcc.Left+1, rcc.Top+1, 20, rcc.Width-2);
            e.Graphics.FillRectangle(captionBrush, rcCaption);

            var font = new Font(FontFamily.GenericSansSerif, (float) 10.0, FontStyle.Regular);

            e.Graphics.DrawString(this.Text, font, captionTextBrush, new PointF(4, 1));

        }
    }
}
