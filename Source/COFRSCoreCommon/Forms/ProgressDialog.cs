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
        private string MessageText { get; set; }

        public ProgressDialog()
        {
            InitializeComponent();
        }
        public ProgressDialog(string message)
        {
            InitializeComponent();
            MessageText = message;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Message.Text = MessageText; 
        }
    }
}
