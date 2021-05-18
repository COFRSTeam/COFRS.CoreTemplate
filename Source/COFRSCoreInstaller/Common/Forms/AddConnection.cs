using COFRS.Template.Common.Models;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
	public partial class AddConnection : Form
	{
		public DBServer Server { get; set; }
		public DBServer LastServerUsed { get; set; }

		public AddConnection()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			if (LastServerUsed == null)
				_dbServerType.SelectedIndex = 0;
			else if (LastServerUsed.DBType == DBServerType.MYSQL)
				_dbServerType.SelectedIndex = 0;
			else if (LastServerUsed.DBType == DBServerType.POSTGRESQL)
				_dbServerType.SelectedIndex = 1;
			else
				_dbServerType.SelectedIndex = 2;

			_portNumber.Location = new Point(133, 63);

			if (_dbServerType.SelectedIndex == 0 || _dbServerType.SelectedIndex == 1)
			{
				_authentication.Enabled = false;
				_authentication.Hide();
				_authenticationLabel.Text = "Port Number";
				_portNumber.Show();
				_portNumber.Enabled = true;
			}
			else
			{
				_authentication.Enabled = true;
				_authentication.Show();
				_authenticationLabel.Text = "Authentication";
				_portNumber.Hide();
				_portNumber.Enabled = false;
			}

			_portNumber.Value = _dbServerType.SelectedIndex == 0 ? 3306 : 5432;
			_authentication.SelectedIndex = 1;
			_userNameLabel.Enabled = true;
			_userName.Enabled = true;
			_passwordLabel.Enabled = true;
			_password.Enabled = true;
			_rememberPassword.Checked = false;
			_rememberPassword.Enabled = true;
			_checkConnectionResult.Text = "Connection is not verified";
			_checkConnectionResult.ForeColor = Color.Red;

			if (_dbServerType.SelectedIndex == 0)
			{
				_userName.Text = "root";
				_portNumber.Value = 3306;
			}
			else if (_dbServerType.SelectedIndex == 1)
			{
				_userName.Text = "postgres";
				_portNumber.Value = 5432;
			}
		}

		private void OnAuthenticationChanged(object sender, EventArgs e)
		{
			if (_authentication.SelectedIndex == 1)
			{
				_userNameLabel.Enabled = false;
				_userName.Enabled = false;
				_passwordLabel.Enabled = false;
				_password.Enabled = false;
				_rememberPassword.Checked = false;
				_rememberPassword.Enabled = false;
			}
			else
			{
				_userNameLabel.Enabled = true;
				_userName.Enabled = true;
				_passwordLabel.Enabled = true;
				_password.Enabled = true;
				_rememberPassword.Checked = true;
				_rememberPassword.Enabled = true;
			}
		}

		private void OnCheckConnection(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_serverName.Text))
			{
				MessageBox.Show("You must provide a server name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (_dbServerType.SelectedIndex == 0)
			{
				if (string.IsNullOrWhiteSpace(_userName.Text))
				{
					MessageBox.Show("You must provide a user name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				if (string.IsNullOrWhiteSpace(_password.Text))
				{
					MessageBox.Show("You must provide a password.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
			}
			else if (_dbServerType.SelectedIndex == 1)
			{
				if (string.IsNullOrWhiteSpace(_userName.Text))
				{
					MessageBox.Show("You must provide a user name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
				if (string.IsNullOrWhiteSpace(_password.Text))
				{
					MessageBox.Show("You must provide a password.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
			}
			else
			{
				if (_authentication.SelectedIndex == 0)
				{
					if (string.IsNullOrWhiteSpace(_userName.Text))
					{
						MessageBox.Show("You must provide a user name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						return;
					}
					if (string.IsNullOrWhiteSpace(_password.Text))
					{
						MessageBox.Show("You must provide a password.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				DBType = _dbServerType.SelectedIndex == 0 ? DBServerType.MYSQL : _dbServerType.SelectedIndex == 1 ? DBServerType.POSTGRESQL : DBServerType.SQLSERVER,
				DBAuth = _authentication.SelectedIndex == 0 ? DBAuthentication.SQLSERVERAUTH : DBAuthentication.WINDOWSAUTH,
				ServerName = _serverName.Text,
				PortNumber = Convert.ToInt32(_portNumber.Value),
				Username = _userName.Text,
				Password = (_rememberPassword.Checked) ? _password.Text : string.Empty,
				RememberPassword = _rememberPassword.Checked
			};

			if (server.DBType == DBServerType.SQLSERVER)
			{
				try
				{
					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
					else
						connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";

					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
						_checkConnectionResult.Text = "Connection verified";
						_checkConnectionResult.ForeColor = Color.Green;
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					_checkConnectionResult.Text = "Connection is not verified";
					_checkConnectionResult.ForeColor = Color.Red;
					Server = null;
				}
			}
			else if (server.DBType == DBServerType.POSTGRESQL)
			{
				try
				{
					connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();
						_checkConnectionResult.Text = "Connection verified";
						_checkConnectionResult.ForeColor = Color.Green;
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					_checkConnectionResult.Text = "Connection is not verified";
					_checkConnectionResult.ForeColor = Color.Red;
					Server = null;
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				try
				{
					connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();
						_checkConnectionResult.Text = "Connection verified";
						_checkConnectionResult.ForeColor = Color.Green;
						Server = server;
						return true;
					}
				}
				catch (Exception)
				{
					_checkConnectionResult.Text = "Connection is not verified";
					_checkConnectionResult.ForeColor = Color.Red;
					Server = null;
				}
			}

			return false;
		}

		private void OnServerTypeChanged(object sender, EventArgs e)
		{
			if (_dbServerType.SelectedIndex == 0 || _dbServerType.SelectedIndex == 1)
			{
				_authenticationLabel.Text = "Port Number";
				_authentication.Enabled = false;
				_authentication.Hide();
				_portNumber.Enabled = true;
				_portNumber.Show();
				_userName.Enabled = true;
				_userNameLabel.Enabled = true;
				_password.Enabled = true;
				_passwordLabel.Enabled = true;
				_rememberPassword.Enabled = true;
				_userName.Text = _dbServerType.SelectedIndex == 0 ? "root" : "postgres";
				_portNumber.Value = _dbServerType.SelectedIndex == 0 ? 3306 : 5432;
			}
			else
			{
				_authenticationLabel.Text = "Authentication";
				_authentication.Show();
				_authentication.Enabled = true;
				_portNumber.Enabled = false;
				_portNumber.Hide();
				_userName.Text = string.Empty;

				if (_authentication.SelectedIndex == 1)
				{
					_userNameLabel.Enabled = false;
					_userName.Enabled = false;
					_passwordLabel.Enabled = false;
					_password.Enabled = false;
					_rememberPassword.Checked = false;
					_rememberPassword.Enabled = false;
				}
				else
				{
					_userNameLabel.Enabled = true;
					_userName.Enabled = true;
					_passwordLabel.Enabled = true;
					_password.Enabled = true;
					_rememberPassword.Checked = true;
					_rememberPassword.Enabled = true;
				}
			}
		}

		private void OnOKPressed(object sender, EventArgs e)
		{
			if (!CheckConnection())
			{
				MessageBox.Show("Could not establish a connection to the server. Check your settings and credentials.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
