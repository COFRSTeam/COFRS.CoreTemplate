using System.Windows.Forms;

namespace COFRSCoreInstaller
{
    public partial class ProgressDialog : Form
    {
        public string Message { get; set; }

        public ProgressDialog(string msg)
        {
            InitializeComponent();
            Message = msg;
            MessageText.Text = Message;
        }
    }
}
