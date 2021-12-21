using COFRS.Template.Common.ServiceUtilities;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Windows.Interop;
using System.Collections;
using System.Windows.Controls;

namespace COFRS.Template.Common.Windows
{
    /// <summary>
    /// Interaction logic for COFRSNewProjectDialog.xaml
    /// </summary>
    public partial class COFRSNewProjectDialog : DialogWindow, IDisposable
    {
        private bool disposedValue;

        public string Framework { get; set; }
		public string SecurityModel { get; set; }
		public string DatabaseTechnology { get; set; }
		public string CompanyMoniker { get; set; }

		public COFRSNewProjectDialog()
        {
            InitializeComponent();

			// Subscribe to theme changes events so we can refresh the colors
			VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
		}

		private void OnLoad(object sender, RoutedEventArgs e)
		{
			frameworkCombobox.Items.Clear();
			frameworkCombobox.Items.Add(".NET 5.0");
			frameworkCombobox.Items.Add(".NET 6.0");
			frameworkCombobox.SelectedIndex = 1;

			SecurityModelCombobox.Items.Clear();
			SecurityModelCombobox.Items.Add("None");
			SecurityModelCombobox.Items.Add("OAuth 2.0 / Open Id Connect");
			SecurityModelCombobox.SelectedIndex = 1;

			DatabaseTechnologyCombobox.Items.Clear();
			DatabaseTechnologyCombobox.Items.Add("My SQL");
			DatabaseTechnologyCombobox.Items.Add("Postgresql");
			DatabaseTechnologyCombobox.Items.Add("Microsoft SQL Server");
			DatabaseTechnologyCombobox.SelectedIndex = 2;
		}

		private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
		{
			RefreshColors();
		}

		private void RefreshColors()
        {
			MainGrid.Background = new SolidColorBrush(ConvertColor(VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUIBackgroundColorKey)));
		}

		private Color ConvertColor(System.Drawing.Color clr)
        {
			return Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
        }

		private void OnOK(object sender, RoutedEventArgs e)
        {
			if (string.IsNullOrWhiteSpace(CompanyMonikerTextBox.Text))
			{
				MessageBox.Show("Company moniker cannot be blank.\r\nThe company moniker is a short name for your company or organization, similiar to a stock market ticker symbol. Select a 6 to twelve character name that describes your company or organization.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			CompanyMoniker = CompanyMonikerTextBox.Text;

			switch (frameworkCombobox.SelectedIndex)
			{
				case 0:
					Framework = "net5.0";
					break;

				case 1:
					Framework = "net6.0";
					break;
			}

			switch (SecurityModelCombobox.SelectedIndex)
			{
				case 0:
					SecurityModel = "None";
					break;

				case 1:
					SecurityModel = "OAuth";
					break;
			}

			switch (DatabaseTechnologyCombobox.SelectedIndex)
			{
				case 0:
					DatabaseTechnology = "MySQL";
					break;

				case 1:
					DatabaseTechnology = "Postgresql";
					break;

				case 2:
					DatabaseTechnology = "SQLServer";
					break;
			}

			DialogResult = true;
			Close();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
					VSColorTheme.ThemeChanged -= this.VSColorTheme_ThemeChanged;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~COFRSNewProjectDialog()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
