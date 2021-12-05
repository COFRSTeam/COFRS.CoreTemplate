using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSCoreCommon.Forms
{
    public partial class ProgressDialog : Form
    {
        string msg;

        public ProgressDialog(string message)
        {
            msg = message;
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            MessageText.Text = msg;
        }
    }
}
