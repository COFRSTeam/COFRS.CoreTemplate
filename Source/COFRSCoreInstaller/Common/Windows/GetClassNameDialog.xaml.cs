using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace COFRS.Template.Common.Windows
{
    /// <summary>
    /// Interaction logic for GetClassNameDialog.xaml
    /// </summary>
    public partial class GetClassNameDialog : DialogWindow
    {
        public string ClassName { get; set; }
        public GetClassNameDialog()
        {
            InitializeComponent();
        }

        public GetClassNameDialog(string title, string hint)
        {
            InitializeComponent();
            Label_Generator.Content = title;
            Textbox_ClassName.Text = hint;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {

        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ClassName = Textbox_ClassName.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
