using COFRS.Template.Common.Models;
using Microsoft.VisualStudio.PlatformUI;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    /// Interaction logic for AddConnectionDialog.xaml
    /// </summary>
    public partial class AddConnectionDialog : DialogWindow, IDisposable
    {
        private bool disposedValue;
		public DBServer Server { get; set; }
		public DBServer LastServerUsed { get; set; }

		public AddConnectionDialog()
        {
            InitializeComponent();
			VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
		}

		private void OnLoad(object sender, RoutedEventArgs e)
		{
			Combobox_ServerType.Items.Clear();
			Combobox_ServerType.Items.Add("My SQL");
			Combobox_ServerType.Items.Add("Postgresql");
			Combobox_ServerType.Items.Add("SQL Server");

			Combobox_Authentication.Items.Clear();
			Combobox_Authentication.Items.Add("Windows Authority");
			Combobox_Authentication.Items.Add("SQL Server Authority");

			if (LastServerUsed == null)
				Combobox_ServerType.SelectedIndex = 0;
			else if (LastServerUsed.DBType == DBServerType.MYSQL)
				Combobox_ServerType.SelectedIndex = 0;
			else if (LastServerUsed.DBType == DBServerType.POSTGRESQL)
				Combobox_ServerType.SelectedIndex = 1;
			else
				Combobox_ServerType.SelectedIndex = 2;

			if (Combobox_ServerType.SelectedIndex == 0 || Combobox_ServerType.SelectedIndex == 1)
			{
				Combobox_Authentication.IsEnabled = false;
				Combobox_Authentication.Visibility = Visibility.Hidden;
				Label_Authentication.Content = "Port Number";
				Textbox_PortNumber.Visibility = Visibility.Visible;
				Textbox_PortNumber.IsEnabled = true;
			}
			else
			{
				Combobox_Authentication.IsEnabled = true;
				Combobox_Authentication.Visibility = Visibility.Visible;
				Label_Authentication.Content = "Authentication";
				Textbox_PortNumber.Visibility = Visibility.Hidden;
				Textbox_PortNumber.IsEnabled = false;
			}

			Textbox_PortNumber.Text = Combobox_ServerType.SelectedIndex == 0 ? "3306" : "5432";
			Combobox_Authentication.SelectedIndex = 1;
			Label_UserName.IsEnabled = true;
			Textbox_UserName.IsEnabled = true;
			Label_Password.IsEnabled = true;
			Textbox_Password.IsEnabled = true;
			Checkbox_RememberPassword.IsChecked = false;
			Checkbox_RememberPassword.IsEnabled = true;
			Label_CheckConnection.Content = "Connection is not verified";

			if (Combobox_ServerType.SelectedIndex == 0)
			{
				Textbox_UserName.Text = "root";
				Textbox_PortNumber.Text = "3306";
			}
			else if (Combobox_ServerType.SelectedIndex == 1)
			{
				Textbox_UserName.Text = "postgres";
				Textbox_PortNumber.Text = "5432";
			}
		}

		private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
		{
			RefreshColors();
		}

		private Color ConvertColor(System.Drawing.Color clr)
		{
			return Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
		}

		private void RefreshColors()
		{
			MainGrid.Background = new SolidColorBrush(ConvertColor(VSColorTheme.GetThemedColor(EnvironmentColors.BrandedUIBackgroundColorKey)));
		}

		private void ServerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (Combobox_ServerType.SelectedIndex == 0 || Combobox_ServerType.SelectedIndex == 1)
				{
					Label_Authentication.Content = "Port Number";
					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
					Textbox_PortNumber.IsEnabled = true;
					Textbox_PortNumber.Visibility = Visibility.Visible;
					Textbox_UserName.IsEnabled = true;
					Label_UserName.IsEnabled = true;
					Textbox_Password.IsEnabled = true;
					Label_Password.IsEnabled = true;
					Checkbox_RememberPassword.IsEnabled = true;
					Textbox_UserName.Text = Combobox_ServerType.SelectedIndex == 0 ? "root" : "postgres";
					Textbox_PortNumber.Text = Combobox_ServerType.SelectedIndex == 0 ? "3306" : "5432";
				}
				else
				{
					Label_Authentication.Content = "Authentication";
					Combobox_Authentication.Visibility = Visibility.Visible;
					Combobox_Authentication.IsEnabled = true;
					Textbox_PortNumber.IsEnabled = false;
					Textbox_PortNumber.Visibility = Visibility.Hidden;
					Textbox_UserName.Text = string.Empty;

					if (Combobox_Authentication.SelectedIndex == 1)
					{
						Label_UserName.IsEnabled = false;
						Textbox_UserName.IsEnabled = false;
						Label_Password.IsEnabled = false;
						Textbox_Password.IsEnabled = false;
						Checkbox_RememberPassword.IsChecked = false;
						Checkbox_RememberPassword.IsEnabled = false;
					}
					else
					{
						Label_UserName.IsEnabled = true;
						Textbox_UserName.IsEnabled = true;
						Label_Password.IsEnabled = true;
						Textbox_Password.IsEnabled = true;
						Checkbox_RememberPassword.IsChecked = true;
						Checkbox_RememberPassword.IsEnabled = true;
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Authentication_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Combobox_Authentication.SelectedIndex == 1)
			{
				Label_UserName.IsEnabled = false;
				Textbox_UserName.IsEnabled = false;
				Label_Password.IsEnabled = false;
				Textbox_Password.IsEnabled = false;
				Checkbox_RememberPassword.IsChecked = false;
				Checkbox_RememberPassword.IsEnabled = false;
			}
			else
			{
				Label_UserName.IsEnabled = true;
				Textbox_UserName.IsEnabled = true;
				Label_Password.IsEnabled = true;
				Textbox_Password.IsEnabled = true;
				Checkbox_RememberPassword.IsChecked = true;
				Checkbox_RememberPassword.IsEnabled = true;
			}
		}

        private void CheckConnection_Click(object sender, RoutedEventArgs e)
        {
			if (string.IsNullOrWhiteSpace(Textbox_ServerName.Text))
			{
				MessageBox.Show("You must provide a server name.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (Combobox_ServerType.SelectedIndex == 0)
			{
				if (string.IsNullOrWhiteSpace(Textbox_UserName.Text))
				{
					MessageBox.Show("You must provide a user name.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				if (string.IsNullOrWhiteSpace(Textbox_Password.Password))
				{
					MessageBox.Show("You must provide a password.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			else if (Combobox_ServerType.SelectedIndex == 1)
			{
				if (string.IsNullOrWhiteSpace(Textbox_UserName.Text))
				{
					MessageBox.Show("You must provide a user name.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				if (string.IsNullOrWhiteSpace(Textbox_Password.Password))
				{
					MessageBox.Show("You must provide a password.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			else
			{
				if (Combobox_Authentication.SelectedIndex == 0)
				{
					if (string.IsNullOrWhiteSpace(Textbox_UserName.Text))
					{
						MessageBox.Show("You must provide a user name.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
						return;
					}
					if (string.IsNullOrWhiteSpace(Textbox_Password.Password))
					{
						MessageBox.Show("You must provide a password.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
						return;
					}
				}
			}

			CheckConnection();
		}

		private bool CheckConnection()
		{
			string connectionString;

			var server = new DBServer
			{
				DBType = Combobox_ServerType.SelectedIndex == 0 ? DBServerType.MYSQL : Combobox_ServerType.SelectedIndex == 1 ? DBServerType.POSTGRESQL : DBServerType.SQLSERVER,
				DBAuth = Combobox_Authentication.SelectedIndex == 0 ? DBAuthentication.SQLSERVERAUTH : DBAuthentication.WINDOWSAUTH,
				ServerName = Textbox_ServerName.Text,
				PortNumber = Convert.ToInt32(Textbox_PortNumber.Text),
				Username = Textbox_UserName.Text,
				Password = (Checkbox_RememberPassword.IsChecked.HasValue && Checkbox_RememberPassword.IsChecked.Value) ? Textbox_Password.Password : string.Empty,
				RememberPassword = (Checkbox_RememberPassword.IsChecked.HasValue && Checkbox_RememberPassword.IsChecked.Value) ? Checkbox_RememberPassword.IsChecked.Value : false
			};

			if (server.DBType == DBServerType.SQLSERVER)
			{
				try
				{
					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
					else
						connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={Textbox_Password.Password};";

					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
						Label_CheckConnection.Content = "Connection verified";
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					Label_CheckConnection.Content = "Connection is not verified";
					Server = null;
				}
			}
			else if (server.DBType == DBServerType.POSTGRESQL)
			{
				try
				{
					connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={Textbox_Password.Password};";

					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();
						Label_CheckConnection.Content = "Connection verified";
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					Label_CheckConnection.Content = "Connection is not verified";
					Server = null;
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				try
				{
					connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={Textbox_Password.Password};";

					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();
						Label_CheckConnection.Content = "Connection verified";
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					Label_CheckConnection.Content = "Connection is not verified";
					Server = null;
				}
			}

			return false;
		}

        private void OK_Click(object sender, RoutedEventArgs e)
        {
			if (!CheckConnection())
			{
				MessageBox.Show("Could not establish a connection to the server. Check your settings and credentials.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DialogResult = true;
			Close();
		}
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
			DialogResult = false;
			Close();
        }

		#region Dispose
		protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
					VSColorTheme.ThemeChanged -= VSColorTheme_ThemeChanged;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AddConnectionDialog()
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
        #endregion
    }
}
