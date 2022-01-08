using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Interaction logic for AddCollection.xaml
    /// </summary>
    public partial class AddCollection : DialogWindow
    {
		#region Variables
		public string ConnectionString { get; set; }
		public ResourceClass ResourceModel { get; set; }
		public IServiceProvider ServiceProvider { get; set; }
        public List<string> SelectedItems { get; set; }
		#endregion

		public AddCollection()
        {
            InitializeComponent();
        }
		private void OnLoad(object sender, RoutedEventArgs e)
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
			Label_ResourceName.Content = ResourceModel.ClassName;
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell2;

            //	Now scan all the resource models in the resource map
            foreach (var resourceModel in codeService.ResourceClassList)
            {
                //	SKip the parent resource, it can't reference itself.
                if (!resourceModel.ClassName.Equals(ResourceModel.ClassName, StringComparison.OrdinalIgnoreCase) &&
                                                    resourceModel.Entity != null)
                {
                    //	See if there is a member in this resource that references the parent resource
                    var referenceColumn = resourceModel.Columns.Where(c => c.IsForeignKey && c.ForeignTableName.Equals(ResourceModel.Entity.TableName, StringComparison.OrdinalIgnoreCase));

                    if (referenceColumn != null && referenceColumn.Count() == 1)
                    {
                        shell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out uint win32Color);
                        System.Drawing.Color clr = ColorTranslator.FromWin32((int)win32Color);
                            
                        CheckBox theItem = new CheckBox
                        {
                            IsChecked = false,
                            Content = resourceModel.ClassName,
                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B)),
                        };

                        theItem.Click += TheItem_Click;

                        //	Ther is, so add it to the child resource list
                        Listbox_ChildResources.Items.Add(theItem);
                        Listbox_ChildResources.MouseDoubleClick += OnResourceItemDoubleClicked;
                        Listbox_ChildResources.MouseUp += OnResourceItemClicked;
                    }
                }
            }

            Button_OK.IsEnabled = false;
			Button_OK.IsDefault = false;
			Button_Cancel.IsDefault = true;
		}

        private void TheItem_Click(object sender, RoutedEventArgs e)
        {
            Button_OK.IsEnabled = false;

            foreach (CheckBox item in Listbox_ChildResources.Items)
                if (item.IsChecked.HasValue && item.IsChecked.Value == true)
                    Button_OK.IsEnabled = true;
        }

        private void OnResourceItemClicked(object sender, MouseButtonEventArgs e)
        {
            Button_OK.IsEnabled = false;

            foreach (CheckBox item in Listbox_ChildResources.Items)
                if (item.IsChecked.HasValue && item.IsChecked.Value == true)
                    Button_OK.IsEnabled = true;
        }

        private void OnResourceItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            Button_OK.IsEnabled = false;

            foreach ( CheckBox item in Listbox_ChildResources.Items)
                if ( item.IsChecked.HasValue && item.IsChecked.Value == true)
                    Button_OK.IsEnabled =true;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
		{
            SelectedItems = new List<string>();

            foreach ( CheckBox item in Listbox_ChildResources.Items)
            {
                if  (item.IsChecked.HasValue && item.IsChecked.Value == true)
                {
                    SelectedItems.Add(item.Content.ToString());
                }
            }

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
