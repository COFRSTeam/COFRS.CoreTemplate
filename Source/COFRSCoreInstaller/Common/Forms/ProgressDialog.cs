using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
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
