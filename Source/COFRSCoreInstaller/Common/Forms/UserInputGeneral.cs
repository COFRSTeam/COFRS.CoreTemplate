using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
	public partial class UserInputGeneral : Form
	{
		#region variables
		private ServerConfig _serverConfig;
		public int InstallType { get; set; }
		private bool Populating = false;
		public DBTable DatabaseTable { get; set; }
		public JObject Examples { get; set; }
		public string DefaultConnectionString { get; set; }
		public string ConnectionString { get; set; }
		public List<ClassFile> ClassList { get; set; }
		public DBServerType ServerType { get; set; }
		public List<string> Policies { get; set; }
		#endregion

		#region Utility functions
		public UserInputGeneral()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			_portNumber.Location = new Point(103, 60);

			if (InstallType == 1)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to translate between. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate the AutoMapper Profile. Then press OK to generate the AutoMapper Profile class.";
				_titleLabel.Text = "COFRS AutoMapper Profile Generator";
				PolicyLabel.Hide();
				policyCombo.Hide();
			}
			else if (InstallType == 3)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to create a validator for. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate the validator class. Then press OK to generate the validator class.";
				_titleLabel.Text = "COFRS Validator Class Generator";
				PolicyLabel.Hide();
				policyCombo.Hide();
			}
			else if (InstallType == 5)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to create a controller for. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate a controller. Then press OK to generate the controller.";
				_titleLabel.Text = "COFRS Controller Class Generator";

				if (Policies == null || Policies.Count == 0)
				{
					PolicyLabel.Hide();
					policyCombo.Hide();
				}
				else
				{
					policyCombo.Items.AddRange(Policies.ToArray());
					policyCombo.SelectedIndex = 0;
					PolicyLabel.Show();
					policyCombo.Show();
				}
			}

			ReadServerList();

			_entityModelList.Items.Clear();
			_resourceModelList.Items.Clear();

			foreach (var classFile in ClassList)
			{
				if (classFile.ElementType == ElementType.Table)
					_entityModelList.Items.Add((EntityClassFile)classFile);
				else if ( classFile.ElementType == ElementType.Resource)
					_resourceModelList.Items.Add((ResourceClassFile)classFile);
			}

			if (_entityModelList.Items.Count == 0)
			{
				if (InstallType == 1)
				{
					MessageBox.Show("No entity models were found in the project. Please create a corresponding entity model before attempting to create a mapping model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else if (InstallType == 3)
				{
					MessageBox.Show("No entity models were found in the project. Please create a corresponding entity model before attempting to create an example model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else if (InstallType == 5)
				{
					MessageBox.Show("No entity models were found in the project. Please create a corresponding entity model before attempting to create a controller class.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				DialogResult = DialogResult.Cancel;
				Close();
			}

			if (_resourceModelList.Items.Count == 0)
			{
				if (InstallType == 1)
				{
					MessageBox.Show("No resource models were found in the project. To create a mapping, you must first create the entity model and its corresponding resource model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else if (InstallType == 4)
				{
					MessageBox.Show("No resource models were found in the project. Please create a corresponding resource model before attempting to create an example model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else if (InstallType == 5)
				{
					MessageBox.Show("No resource models were found in the project. Please create a corresponding resource model before attempting to create a controller class.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				DialogResult = DialogResult.Cancel;
				Close();
			}

			OnServerChanged(this, new EventArgs());
		}
		#endregion

		#region User Interactions
		private void OnServerTypeChanged(object sender, EventArgs e)
		{
			try
			{
				if (_serverTypeList.SelectedIndex == 0 || _serverTypeList.SelectedIndex == 1)
				{
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
					_authenticationLabel.Text = "Port Number";
					_portNumber.Show();
					_portNumber.Enabled = true;
					_userName.Enabled = true;
					_userNameLabel.Enabled = true;
					_password.Enabled = true;
					_passwordLabel.Enabled = true;
					_rememberPassword.Enabled = true;
				}
				else
				{
					_authenticationList.Enabled = true;
					_authenticationList.Show();
					_authenticationLabel.Text = "Authentication";
					_portNumber.Hide();
					_portNumber.Enabled = false;
				}

				if (!Populating)
				{
					PopulateServers();
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnServerChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;
					ServerType = server.DBType;

					if (server != null)
					{
						if (server.DBType == DBServerType.SQLSERVER)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Authentication";
							_authenticationList.Enabled = true;
							_authenticationList.Show();

							_authenticationList.SelectedIndex = (server.DBAuth == DBAuthentication.SQLSERVERAUTH) ? 0 : 1;

							if (server.DBAuth == DBAuthentication.SQLSERVERAUTH)
							{
								_userNameLabel.Enabled = true;
								_userName.Enabled = true;
								_userName.Text = server.Username;

								_passwordLabel.Enabled = true;
								_password.Enabled = true;
								_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

								_rememberPassword.Enabled = true;
								_rememberPassword.Checked = server.RememberPassword;
							}
							else
							{
								_userNameLabel.Enabled = false;
								_userName.Enabled = false;
								_userName.Text = string.Empty;

								_passwordLabel.Enabled = false;
								_password.Enabled = false;
								_password.Text = string.Empty;

								_rememberPassword.Enabled = false;
								_rememberPassword.Checked = false;
							}
						}
						else if (server.DBType == DBServerType.POSTGRESQL)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Port Number";
							_authenticationList.Enabled = false;
							_authenticationList.Hide();

							_portNumber.Enabled = true;
							_portNumber.Value = server.PortNumber;

							_userNameLabel.Enabled = true;
							_userName.Enabled = true;
							_userName.Text = server.Username;

							_passwordLabel.Enabled = true;
							_password.Enabled = true;
							_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

							_rememberPassword.Enabled = true;
							_rememberPassword.Checked = server.RememberPassword;
						}
						else if (server.DBType == DBServerType.MYSQL)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Port Number";
							_authenticationList.Enabled = false;
							_authenticationList.Hide();

							_portNumber.Enabled = true;
							_portNumber.Value = server.PortNumber;

							_userNameLabel.Enabled = true;
							_userName.Enabled = true;
							_userName.Text = server.Username;

							_passwordLabel.Enabled = true;
							_password.Enabled = true;
							_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

							_rememberPassword.Enabled = true;
							_rememberPassword.Checked = server.RememberPassword;
						}

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSelectedDatabaseChanged(object sender, EventArgs e)
		{
			try
			{
				var server = (DBServer)_serverList.SelectedItem;
				var db = (string)_dbList.SelectedItem;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					ConnectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";
					_tableList.Items.Clear();

					using (var connection = new NpgsqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"
SELECT schemaname, tablename
  FROM pg_catalog.pg_tables
 WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema';
";

						using (var command = new NpgsqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};

									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}
				else if (server.DBType == DBServerType.MYSQL)
				{
					ConnectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={_password.Text};";
					_tableList.Items.Clear();

					using (var connection = new MySqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"

SELECT TABLE_SCHEMA, TABLE_NAME FROM information_schema.tables 
 where table_type = 'BASE TABLE'
   and TABLE_SCHEMA = @databaseName;
";

						using (var command = new MySqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@databaseName", db);

							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};

									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}
				else
				{
					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						ConnectionString = $"Server ={server.ServerName};Database={db};Trusted_Connection=True;";
					else
						ConnectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={_password.Text};";

					_tableList.Items.Clear();

					using (var connection = new SqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"
select s.name, t.name
  from sys.tables as t with(nolock)
 inner join sys.schemas as s with(nolock) on s.schema_id = t.schema_id
  order by s.name, t.name";

						using (var command = new SqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};
									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}

				_tableList.SelectedIndex = -1;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSelectedTableChanged(object sender, EventArgs e)
		{
			try
			{
				var server = (DBServer)_serverList.SelectedItem;

				if (server == null)
				{
					MessageBox.Show("You must select a Database server to create a new resource model. Please select a database server and try again.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				var db = (string)_dbList.SelectedItem;
				if (string.IsNullOrWhiteSpace(db))
				{
					MessageBox.Show("You must select a Database to create a new resource model. Please select a database and try again.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				var table = (DBTable)_tableList.SelectedItem;

				if (table == null)
				{
					MessageBox.Show("You must select a Database table to create a new resource model. Please select a database table and try again.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				Populating = true;

				bool foundit = false;

				for (int i = 0; i < _entityModelList.Items.Count; i++)
				{
					var entity = (EntityClassFile)_entityModelList.Items[i];

					if (entity.TableName == table.Table)
					{
						_entityModelList.SelectedIndex = i;

						for (int j = 0; j < _resourceModelList.Items.Count; j++)
						{
							var resource = (ResourceClassFile)_resourceModelList.Items[j];

							if (string.Equals(resource.EntityClass, entity.ClassName, StringComparison.OrdinalIgnoreCase))
							{
								_resourceModelList.SelectedIndex = j;
								foundit = true;
								break;
							}
						}
						break;
					}
				}

				if (!foundit)
				{
					_entityModelList.SelectedIndex = -1;
					_resourceModelList.SelectedIndex = -1;
					_tableList.SelectedIndex = -1;

					if (InstallType == 1)
					{
						MessageBox.Show("No matching entity and resource class found. You will not be able to create a mapping model without a matching entity and resource model.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else if (InstallType == 4)
					{
						MessageBox.Show("No matching entity and resource class found. You will not be able to create a example model without a matching entity and resource model.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else if (InstallType == 5)
					{
						MessageBox.Show("No matching entity and resource class found. You will not be able to create a Controller without a matching entity and resource model.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}

				Populating = false;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnUserNameChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						server.Username = _userName.Text;

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnPasswordChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						if (server.RememberPassword)
							server.Password = _password.Text;
						else
							server.Password = string.Empty;

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSavePasswordChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						server.RememberPassword = _rememberPassword.Checked;

						if (!server.RememberPassword)
							server.Password = string.Empty;
						else
							server.Password = _password.Text;

						var savedServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnAuthenticationChanged(object sender, EventArgs e)
		{
			if (!Populating)
			{
				_dbList.Items.Clear();
				_tableList.Items.Clear();
				var server = (DBServer)_serverList.SelectedItem;

				if (server != null)
				{
					server.DBAuth = _authenticationList.SelectedIndex == 0 ? DBAuthentication.SQLSERVERAUTH : DBAuthentication.WINDOWSAUTH;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					{
						_userName.Text = string.Empty;
						_userName.Enabled = false;
						_userNameLabel.Enabled = false;

						_password.Text = string.Empty;
						_password.Enabled = false;
						_passwordLabel.Enabled = false;

						_rememberPassword.Checked = false;
						_rememberPassword.Enabled = false;
					}
					else
					{
						_userName.Enabled = true;
						_userNameLabel.Enabled = true;

						_password.Enabled = true;
						_passwordLabel.Enabled = true;

						_rememberPassword.Checked = server.RememberPassword;
						_rememberPassword.Enabled = true;
					}

					Save();

					if (TestConnection(server))
						PopulateDatabases();
				}
			}
		}

		private void OnAddServer(object sender, EventArgs e)
		{
			try
			{
				var dialog = new AddConnection
				{
					LastServerUsed = (DBServer)_serverList.SelectedItem
				};

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					_serverConfig.Servers.Add(dialog.Server);
					Save();

					switch (dialog.Server.DBType)
					{
						case DBServerType.MYSQL: _serverTypeList.SelectedIndex = 0; break;
						case DBServerType.POSTGRESQL: _serverTypeList.SelectedIndex = 1; break;
						case DBServerType.SQLSERVER: _serverTypeList.SelectedIndex = 2; break;
					}

					OnServerTypeChanged(this, new EventArgs());

					for (int index = 0; index < _serverList.Items.Count; index++)
					{
						if (string.Equals((_serverList.Items[index] as DBServer).ServerName, dialog.Server.ServerName, StringComparison.OrdinalIgnoreCase))
						{
							_serverList.SelectedIndex = index;
							break;
						}
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnRemoveServer(object sender, EventArgs e)
		{
			try
			{
				var deprecatedServer = (DBServer)_serverList.SelectedItem;
				var newList = new List<DBServer>();

				foreach (var server in _serverConfig.Servers)
				{
					if (!string.Equals(server.ServerName, deprecatedServer.ServerName, StringComparison.OrdinalIgnoreCase))
					{
						newList.Add(server);
					}
				}

				_serverConfig.Servers = newList;

				if (_serverConfig.LastServerUsed >= _serverConfig.Servers.Count())
				{
					_serverConfig.LastServerUsed = 0;
				}

				Save();

				OnServerTypeChanged(this, new EventArgs());
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnResourceModelChanged(object sender, EventArgs e)
		{
			try
			{
				if (_resourceModelList.SelectedIndex != -1 && !Populating)
				{
					var resourceClassFile = (ResourceClassFile)_resourceModelList.SelectedItem;
					var foundIt = false;

					for (int index = 0; index < _entityModelList.Items.Count; index++)
					{
						var entityClassFile = (EntityClassFile)_entityModelList.Items[index];

						if (string.Equals(resourceClassFile.EntityClass, entityClassFile.ClassName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_entityModelList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding entity class for this model was found. Please select another model, or create the entity model for this model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}

					var entity = _entityModelList.SelectedItem as EntityClassFile;
					foundIt = false;

					for (int index = 0; index < _tableList.Items.Count; index++)
					{
						var table = (DBTable)_tableList.Items[index];

						if (string.Equals(table.Table, entity.TableName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_tableList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding table for this entity was found in the selected database. Please select the server and database that contains this table.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnEntityModelChanged(object sender, EventArgs e)
		{
			try
			{
				if (_entityModelList.SelectedIndex != -1 && !Populating)
				{
					var classFile = (EntityClassFile)_entityModelList.SelectedItem;
					var foundIt = false;

					for (int index = 0; index < _tableList.Items.Count; index++)
					{
						var table = (DBTable)_tableList.Items[index];

						if (string.Equals(table.Table, classFile.TableName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_tableList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding table for this entity was found in the selected database. Please select the server and database that contains this table.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}

					foundIt = false;

					for (int index = 0; index < _resourceModelList.Items.Count; index++)
					{
						var resourceClassFile = (ResourceClassFile)_resourceModelList.Items[index];

						if (string.Equals(resourceClassFile.EntityClass, classFile.ClassName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_resourceModelList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding resource class for this entity was found. Please select another entity, or create the resource model for this entity.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnOK(object sender, EventArgs e)
		{
			Save();
			DatabaseTable = (DBTable)_tableList.SelectedItem;

			if (_entityModelList.SelectedIndex == -1)
			{
				MessageBox.Show("You must select an entity model in order to create this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_resourceModelList.SelectedIndex == -1)
			{
				MessageBox.Show("You must select a resource model in order to create this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void OnPortNumberChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						server.PortNumber = Convert.ToInt32(_portNumber.Value);

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion

		#region Helper Functions
		/// <summary>
		/// Reads the list of SQL Servers from the server configuration list
		/// </summary>
		private void PopulateServers()
		{
			DBServerType serverType;

			switch (_serverTypeList.SelectedIndex)
			{
				case 0: serverType = DBServerType.MYSQL; break;
				case 1: serverType = DBServerType.POSTGRESQL; break;
				case 2: serverType = DBServerType.SQLSERVER; break;
				default: serverType = DBServerType.SQLSERVER; break;
			}

			var serverList = _serverConfig.Servers.Where(s => s.DBType == serverType);

			_serverList.Items.Clear();
			_dbList.Items.Clear();
			_tableList.Items.Clear();

			if (serverList.Count() == 0)
			{
				_serverList.Enabled = false;
				_serverList.SelectedIndex = -1;

				if (serverType == DBServerType.SQLSERVER)
				{
					_authenticationList.SelectedIndex = -1;
					_authenticationList.Enabled = false;
					_authenticationList.Show();

					_portNumber.Enabled = false;
					_portNumber.Hide();
				}

				else if (serverType == DBServerType.POSTGRESQL)
				{
					_portNumber.Enabled = false;
					_portNumber.Value = 1024;
					_portNumber.Show();

					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.MYSQL)
				{
					_portNumber.Enabled = false;
					_portNumber.Value = 3306;
					_portNumber.Show();

					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				_userName.Enabled = false;
				_userName.Text = string.Empty;

				_password.Enabled = false;
				_password.Text = string.Empty;

				_rememberPassword.Enabled = false;
				_rememberPassword.Checked = false;
			}
			else
			{
				_serverList.Enabled = true;

				if (serverType == DBServerType.POSTGRESQL)
				{
					_portNumber.Enabled = true;
					_portNumber.Show();
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.MYSQL)
				{
					_portNumber.Enabled = true;
					_portNumber.Show();
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.SQLSERVER)
				{
					_portNumber.Enabled = false;
					_portNumber.Hide();
					_authenticationList.Enabled = true;
					_authenticationList.Show();
				}

				foreach (var server in serverList)
				{
					_serverList.Items.Add(server);
				}

				if (_serverList.Items.Count > 0)
					_serverList.SelectedIndex = 0;
			}
		}

		private void Save()
		{
			int index = 0;
			var server = (DBServer)_serverList.SelectedItem;

			if (server != null)
			{
				foreach (var dbServer in _serverConfig.Servers)
				{
					if (string.Equals(dbServer.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase) &&
						dbServer.DBType == server.DBType)
					{
						_serverConfig.LastServerUsed = index;
						break;
					}

					index++;
				}
			}

			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");
			File.Delete(filePath);

			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var streamWriter = new StreamWriter(stream))
				{
					using (var writer = new JsonTextWriter(streamWriter))
					{
						var serializer = new JsonSerializer();
						serializer.Serialize(writer, _serverConfig);
					}
				}
			}
		}

		private void PopulateDatabases()
		{
			var server = (DBServer)_serverList.SelectedItem;

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				if (string.IsNullOrWhiteSpace(_password.Text))
					return;

				ConnectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new NpgsqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"
SELECT datname 
  FROM pg_database
 WHERE datistemplate = false
   AND datname != 'postgres'
 ORDER BY datname";

						using (var command = new NpgsqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								int itemindex = 0;

								while (reader.Read())
								{
									var databaseName = reader.GetString(0);

									_dbList.Items.Add(databaseName);

									string cs = $"Server={server.ServerName};Port={server.PortNumber};Database={databaseName};User ID={server.Username};Password={_password.Text};";

									if (string.Equals(cs, DefaultConnectionString, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (_dbList.Items.Count > 0)
						_dbList.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				if (string.IsNullOrWhiteSpace(_password.Text))
					return;

				ConnectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new MySqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"
select SCHEMA_NAME from information_schema.SCHEMATA
 where SCHEMA_NAME not in ( 'information_schema', 'performance_schema', 'sys', 'mysql');";

						using (var command = new MySqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								int itemindex = 0;

								while (reader.Read())
								{
									var databaseName = reader.GetString(0);

									_dbList.Items.Add(databaseName);

									string cs = $"Server={server.ServerName};Port={server.PortNumber};Database={databaseName};UID={server.Username};PWD={_password.Text};";

									if (string.Equals(cs, DefaultConnectionString, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (_dbList.Items.Count > 0)
						_dbList.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				if (server.DBAuth == DBAuthentication.SQLSERVERAUTH && string.IsNullOrWhiteSpace(_password.Text))
					return;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					ConnectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
				else
					ConnectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new SqlConnection(ConnectionString))
					{
						connection.Open();

						var query = @"
select name
  from sys.databases with(nolock)
 where name not in ( 'master', 'model', 'msdb', 'tempdb' )
 order by name";

						using (var command = new SqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								int itemindex = 0;

								while (reader.Read())
								{
									var databaseName = reader.GetString(0);

									_dbList.Items.Add(databaseName);
									string cs;

									if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
										cs = $"Server={server.ServerName};Database={databaseName};Trusted_Connection=True;";
									else
										cs = $"Server={server.ServerName};Database={databaseName};uid={server.Username};pwd={_password.Text};";

									if (string.Equals(cs, DefaultConnectionString, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (_dbList.Items.Count > 0)
						_dbList.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>
		/// Tests to see if the server credentials are sufficient to establish a connection
		/// to the server
		/// </summary>
		/// <param name="server">The Database Server we are trying to connect to.</param>
		/// <returns></returns>
		private bool TestConnection(DBServer server)
		{
			_tableList.Items.Clear();
			_dbList.Items.Clear();

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				string connectionString;
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

				//	Attempt to connect to the database.
				try
				{
					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				string connectionString;
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

				//	Attempt to connect to the database.
				try
				{
					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}
			else
			{
				//	Construct the connection string
				string connectionString;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
				else
					connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";


				//	Attempt to connect to the database.
				try
				{
					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}

			//	If we got here, it worked. We were able to establish and close
			//	the connection.
			return true;
		}

		/// <summary>
		/// Reads the list of SQL Servers from the server configuration list
		/// </summary>
		private void ReadServerList()
		{
			//	Indicate that we are merely populating windows at this point. There are certain
			//	actions that occur during the loading of windows that mimic user interaction.
			//	There is no user interaction at this point, so there are certain actions we 
			//	do not want to run while populating.
			Populating = true;

			//	Get the location of the server configuration on disk
			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");

			//	Read the ServerConfig into memory. If one does not exist
			//	create an empty one.
			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var streamReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(streamReader))
					{
						var serializer = new JsonSerializer();

						_serverConfig = serializer.Deserialize<ServerConfig>(reader);

						if (_serverConfig == null)
							_serverConfig = new ServerConfig();
					}
				}
			}

			//	If there are any servers in the list, we need to populate
			//	the windows controls.
			if (_serverConfig.Servers.Count() > 0)
			{
				int LastServerUsed = _serverConfig.LastServerUsed;
				//	When we populate the windows controls, ensure that the last server that
				//	the user used is in the visible list, and make sure it is the one
				//	selected.
				for (int candidate = 0; candidate < _serverConfig.Servers.ToList().Count(); candidate++)
				{
					var candidateServer = _serverConfig.Servers.ToList()[candidate];
					var candidateConnectionString = string.Empty;

					switch (candidateServer.DBType)
					{
						case DBServerType.MYSQL:
							candidateConnectionString = $"Server={candidateServer.ServerName};Port={candidateServer.PortNumber}";
							break;

						case DBServerType.POSTGRESQL:
							candidateConnectionString = $"Server={candidateServer.ServerName};Port={candidateServer.PortNumber}";
							break;

						case DBServerType.SQLSERVER:
							candidateConnectionString = $"Server={candidateServer.ServerName}";
							break;
					}

					if (DefaultConnectionString.StartsWith(candidateConnectionString))
					{
						LastServerUsed = candidate;
						break;
					}
				}

				var dbServer = _serverConfig.Servers.ToList()[LastServerUsed];
				DBServerType selectedType = dbServer.DBType;

				switch (dbServer.DBType)
				{
					case DBServerType.MYSQL: _serverTypeList.SelectedIndex = 0; break;
					case DBServerType.POSTGRESQL: _serverTypeList.SelectedIndex = 1; break;
					case DBServerType.SQLSERVER: _serverTypeList.SelectedIndex = 2; break;
				}

				var serverList = _serverConfig.Servers.Where(s => s.DBType == selectedType);
				int index = 0;
				int selectedIndex = -1;

				foreach (var server in serverList)
				{
					_serverList.Items.Add(server);

					if (string.Equals(server.ServerName, dbServer.ServerName, StringComparison.OrdinalIgnoreCase))
						selectedIndex = index;

					index++;
				}

				if (_serverList.Items.Count > 0)
				{
					_serverList.SelectedIndex = selectedIndex;

					if (dbServer.DBType == DBServerType.SQLSERVER)
					{
						_authenticationList.SelectedIndex = dbServer.DBAuth == DBAuthentication.WINDOWSAUTH ? 0 : 1;

						if (_authenticationList.SelectedIndex == 0)
						{
							_userName.Text = string.Empty;
							_userName.Enabled = false;

							_password.Text = string.Empty;
							_password.Enabled = false;

							_rememberPassword.Checked = false;
							_rememberPassword.Enabled = false;
						}
						else
						{
							_userName.Text = dbServer.Username;
							_userName.Enabled = true;

							_rememberPassword.Checked = dbServer.RememberPassword;
							_rememberPassword.Enabled = true;

							if (dbServer.RememberPassword)
							{
								_password.Text = dbServer.Password;
								_password.Enabled = true;
							}
							else
							{
								_password.Text = string.Empty;
								_password.Enabled = true;
							}
						}
					}
					else if (dbServer.DBType == DBServerType.POSTGRESQL)
					{
						_portNumber.Value = dbServer.PortNumber;
						_userName.Text = dbServer.Username;
						_userName.Enabled = true;

						_rememberPassword.Checked = dbServer.RememberPassword;
						_rememberPassword.Enabled = true;
						_password.Enabled = true;

						if (dbServer.RememberPassword)
						{
							_password.Text = dbServer.Password;
						}
						else
						{
							_password.Text = string.Empty;
						}
					}
					else if (dbServer.DBType == DBServerType.MYSQL)
					{
						_portNumber.Value = dbServer.PortNumber;
						_userName.Text = dbServer.Username;
						_userName.Enabled = true;

						_rememberPassword.Checked = dbServer.RememberPassword;
						_rememberPassword.Enabled = true;
						_password.Enabled = true;

						if (dbServer.RememberPassword)
						{
							_password.Text = dbServer.Password;
						}
						else
						{
							_password.Text = string.Empty;
						}
					}

					PopulateDatabases();
				}
			}
			else
			{
				//	There were no servers in the list, make sure everything is empty
				_serverTypeList.SelectedIndex = 1;

				_authenticationList.Enabled = false;
				_authenticationList.SelectedIndex = -1;

				_serverList.Enabled = false;
				_serverList.Items.Clear();

				_userName.Enabled = false;
				_userName.Text = string.Empty;

				_password.Enabled = false;
				_password.Text = string.Empty;

				_rememberPassword.Enabled = false;
				_rememberPassword.Checked = false;
			}

			//	We're done. Turn off the populating flag.
			Populating = false;
		}

		private int ReadComposite(NpgsqlDataReader reader, DBColumn column, int ordinal, List<EntityClassFile> classList, JObject values)
		{
			var cl = classList.FirstOrDefault(c => string.Equals(c.ClassName, column.dbDataType, StringComparison.OrdinalIgnoreCase));

			foreach (var member in cl.Columns)
			{
				if ((NpgsqlDbType)member.DataType == NpgsqlDbType.Unknown)
				{
					var ch = classList.FirstOrDefault(c => string.Equals(c.ClassName, member.dbDataType, StringComparison.OrdinalIgnoreCase));

					if (ch.ElementType == ElementType.Enum)
					{
						var value = reader.GetValue(ordinal);
						values.Add(member.ColumnName, JToken.FromObject(value));
						ordinal++;
					}
					else
					{
						var jObject = new JObject();

						ordinal = ReadComposite(reader, member, ordinal, classList, jObject);

						values.Add(member.ColumnName, jObject);
					}
				}
				else
				{
					var value = reader.GetValue(ordinal);

					if (value.GetType() == typeof(IPAddress))
					{
						var ipAddress = (IPAddress)value;
						values.Add(member.ColumnName, ipAddress.ToString());
					}
					else if (value.GetType() == typeof(IPAddress[]))
					{
						var theValue = (IPAddress[])value;
						var json = new JArray();

						foreach (var val in theValue)
						{
							json.Add(val.ToString());
						}
						values.Add(member.ColumnName, json);
					}
					else if (value.GetType() == typeof(ValueTuple<IPAddress, int>))
					{
						var ipAddress = ((ValueTuple<IPAddress, int>)value).Item1;
						int filter = ((ValueTuple<IPAddress, int>)value).Item2;

						var theValue = new JObject
											{
												{ "IPAddress", ipAddress.ToString() },
												{ "Filter", filter }
											};

						values.Add(member.ColumnName, theValue);
					}
					else if (value.GetType() == typeof(ValueTuple<IPAddress, int>[]))
					{
						var theValue = (ValueTuple<IPAddress, int>[])value;
						var json = new JArray();

						foreach (var val in theValue)
						{
							var ipAddress = val.Item1;
							int filter = val.Item2;

							var aValue = new JObject
												{
													{ "IPAddress", ipAddress.ToString() },
													{ "Filter", filter }
												};

							json.Add(aValue);
						}

						values.Add(member.ColumnName, json);
					}
					else if (value.GetType() == typeof(PhysicalAddress))
					{
						var physicalAddress = (PhysicalAddress)value;
						values.Add(member.ColumnName, physicalAddress.ToString());
					}
					else if (value.GetType() == typeof(PhysicalAddress[]))
					{
						var result = new JArray();
						var theValue = (PhysicalAddress[])value;

						foreach (var addr in theValue)
						{
							result.Add(addr.ToString());
						}

						values.Add(member.ColumnName, result);
					}
					else if (value.GetType() == typeof(BitArray))
					{
						var answer = new StringBuilder();

						foreach (bool val in (BitArray)value)
						{
							var strVal = val ? "1" : "0";
							answer.Append(strVal);
						}
						values.Add(member.ColumnName, JToken.FromObject(answer.ToString()));
					}
					else
					{
						values.Add(member.ColumnName, JToken.FromObject(value));
					}
					ordinal++;
				}
			}

			return ordinal;
		}

		private bool DeconstructComposite(string parent, DBColumn column, List<EntityClassFile> classList, StringBuilder query, bool firstColumn)
		{
			var cl = classList.FirstOrDefault(c => string.Equals(c.ClassName, column.dbDataType, StringComparison.OrdinalIgnoreCase));

			if (cl != null)
			{
				foreach (var childmember in cl.Columns)
				{
					if ((NpgsqlDbType)childmember.DataType == NpgsqlDbType.Unknown)
					{
						var ch = classList.FirstOrDefault(c => string.Equals(c.ClassName, childmember.dbDataType, StringComparison.OrdinalIgnoreCase));

						if (ch.ElementType == ElementType.Enum)
						{
							firstColumn = AppendComma(query, firstColumn);
							query.Append($"({parent}).\"{childmember.EntityName}\"");
						}
						else
						{
							var newparent = $"({parent}).\"{childmember.EntityName}\"";
							firstColumn = DeconstructComposite(newparent, childmember, classList, query, firstColumn);
						}
					}
					else
					{
						firstColumn = AppendComma(query, firstColumn);
						query.Append($"({parent}).\"{childmember.EntityName}\"");
					}
				}
			}

			return firstColumn;
		}

		private static bool AppendComma(StringBuilder query, bool firstColumn)
		{
			if (firstColumn)
				firstColumn = false;
			else
				query.Append(", ");

			return firstColumn;
		}

		#endregion
	}
}
