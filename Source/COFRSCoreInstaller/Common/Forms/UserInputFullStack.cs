using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSLangProj;

namespace COFRS.Template.Common.Forms
{
	public partial class UserInputFullStack : Form
    {
		#region Variables
		private ServerConfig _serverConfig;
		private bool Populating = false;
		public DBTable DatabaseTable { get; set; }
		public List<DBColumn> DatabaseColumns { get; set; }
		public ProjectFolder EntityModelsFolder { get; set; }
		public string RootNamespace { get; set; }
		public string SingularResourceName { get; set; }
		public string PluralResourceName { get; set; }
		public string ConnectionString { get; set; }
		public string DefaultConnectionString { get; set; }
		public JObject Examples { get; set; }
		public Dictionary<string, string> ReplacementsDictionary { get; set; }
		public List<string> Policies;
		public List<ClassFile> ClassList { get; set; }
		public List<ClassFile> UndefinedClassList = new List<ClassFile>();
		public DBServerType ServerType { get; set; }
		public Dictionary<string, MemberInfo> Members { get; set; }

		public string Policy
		{
			get
			{
				if (policyCombo.Items.Count > 0)
				{
					return policyCombo.SelectedItem.ToString();
				}
				else
				{
					return string.Empty;
				}
			}
		}
		#endregion

		#region Utility functions
		public UserInputFullStack()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			_portNumber.Location = new Point(103, 70);
			DatabaseColumns = new List<DBColumn>();

			SingularName.Text = SingularResourceName;
			PluralName.Text = PluralResourceName;

			ReadServerList();

			if (Policies != null && Policies.Count > 0)
			{
				policyCombo.Visible = true;
				policyLabel.Visible = true;
				policyCombo.Items.Add("Anonymous");

				foreach (var policy in Policies)
					policyCombo.Items.Add(policy);

				policyCombo.SelectedIndex = 0;
			}
			else
			{
				policyLabel.Visible = false;
				policyCombo.Visible = false;
			}
		}
		#endregion

		#region user interactions
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

		private void OnRememberPasswordChanged(object sender, EventArgs e)
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

		private void OnAddNewServer(object sender, EventArgs e)
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

		private void OnDatabaseChanged(object sender, EventArgs e)
		{
			try
			{
				_tableList.SelectedIndex = -1;

				var server = (DBServer)_serverList.SelectedItem;
				var db = (string)_dbList.SelectedItem;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";
					_tableList.Items.Clear();

					ConnectionString = connectionString;
					using (var connection = new NpgsqlConnection(connectionString))
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
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={_password.Text};";
					_tableList.Items.Clear();

					ConnectionString = connectionString;
					using (var connection = new MySqlConnection(connectionString))
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
					string connectionString;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						connectionString = $"Server={server.ServerName};Database={db};Trusted_Connection=True;";
					else
						connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={_password.Text};";

					ConnectionString = connectionString;

					_tableList.Items.Clear();

					using (var connection = new SqlConnection(connectionString))
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

		private void OnTableChanged(object sender, EventArgs e)
		{
			try
			{
				var server = (DBServer)_serverList.SelectedItem;
				var db = (string)_dbList.SelectedItem;
				var table = (DBTable)_tableList.SelectedItem;
				DatabaseColumns.Clear();

				if (server == null)
					return;

				if (string.IsNullOrWhiteSpace(db))
					return;

				if (table == null)
					return;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					UndefinedClassList.Clear();

					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";

					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
select a.attname as columnname,
	   t.typname as datatype,
	   case when t.typname = 'varchar' then a.atttypmod-4
	        when t.typname = 'bpchar' then a.atttypmod-4
			when t.typname = '_varchar' then a.atttypmod-4
			when t.typname = '_bpchar' then a.atttypmod-4
	        when a.atttypmod > -1 then a.atttypmod
	        else a.attlen end as max_len,
	   case atttypid
            when 21 /*int2*/ then 16
            when 23 /*int4*/ then 32
            when 20 /*int8*/ then 64
         	when 1700 /*numeric*/ then
              	case when atttypmod = -1
                     then 0
                     else ((atttypmod - 4) >> 16) & 65535     -- calculate the precision
                     end
         	when 700 /*float4*/ then 24 /*FLT_MANT_DIG*/
         	when 701 /*float8*/ then 53 /*DBL_MANT_DIG*/
         	else 0
  			end as numeric_precision,
  		case when atttypid in (21, 23, 20) then 0
    		 when atttypid in (1700) then            
        		  case when atttypmod = -1 then 0       
            		   else (atttypmod - 4) & 65535            -- calculate the scale  
        			   end
       		else 0
  			end as numeric_scale,		
	   not a.attnotnull as is_nullable,
	   case when ( a.attgenerated = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_computed,

	   case when ( a.attidentity = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_identity,

	   case when (select indrelid from pg_index as px where px.indisprimary = true and px.indrelid = c.oid and a.attnum = ANY(px.indkey)) = c.oid then true else false end as is_primary,
	   case when (select indrelid from pg_index as ix where ix.indrelid = c.oid and a.attnum = ANY(ix.indkey)) = c.oid then true else false end as is_indexed,
	   case when (select conrelid from pg_constraint as cx where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) = c.oid then true else false end as is_foreignkey,
       (  select cc.relname from pg_constraint as cx inner join pg_class as cc on cc.oid = cx.confrelid where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) as foeigntablename
   from pg_class as c
  inner join pg_namespace as ns on ns.oid = c.relnamespace
  inner join pg_attribute as a on a.attrelid = c.oid and not a.attisdropped and attnum > 0
  inner join pg_type as t on t.oid = a.atttypid
  left outer join pg_attrdef as ad on ad.adrelid = a.attrelid and ad.adnum = a.attnum 
  where ns.nspname = @schema
    and c.relname = @tablename
 order by a.attnum
";

						using (var command = new NpgsqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@schema", table.Schema);
							command.Parameters.AddWithValue("@tablename", table.Table);

							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var entityName = reader.GetString(0);
									var columnName = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(reader.GetString(0)));

									NpgsqlDbType dataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1));

									if (dataType == NpgsqlDbType.Unknown)
									{
										var entity = ClassList.FirstOrDefault(ent =>
											ent.GetType() == typeof(EntityClassFile) &&
											string.Equals(((EntityClassFile)ent).SchemaName, table.Schema, StringComparison.OrdinalIgnoreCase) &&
											string.Equals(((EntityClassFile)ent).TableName, reader.GetString(1), StringComparison.OrdinalIgnoreCase));

										if (entity == null)
										{
											entityName = reader.GetString(1);
											var className = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(entityName));

											entity = new EntityClassFile()
											{
												SchemaName = table.Schema,
												ClassName = className,
												TableName = entityName,
												FileName = Path.Combine(EntityModelsFolder.Folder, $"{className}.cs"),
												ClassNameSpace = EntityModelsFolder.Namespace
											};

											UndefinedClassList.Add(entity);
										}
									}

									var dbColumn = new DBColumn
									{
										ColumnName = columnName,
										EntityName = entityName,
										DataType = dataType,
										dbDataType = reader.GetString(1),
										Length = Convert.ToInt64(reader.GetValue(2)),
										NumericPrecision = Convert.ToInt32(reader.GetValue(3)),
										NumericScale = Convert.ToInt32(reader.GetValue(4)),
										IsNullable = Convert.ToBoolean(reader.GetValue(5)),
										IsComputed = Convert.ToBoolean(reader.GetValue(6)),
										IsIdentity = Convert.ToBoolean(reader.GetValue(7)),
										IsPrimaryKey = Convert.ToBoolean(reader.GetValue(8)),
										IsIndexed = Convert.ToBoolean(reader.GetValue(9)),
										IsForeignKey = Convert.ToBoolean(reader.GetValue(10)),
										ForeignTableName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
									};

									DatabaseColumns.Add(dbColumn);
								}
							}
						}

						foreach (var unknownClass in UndefinedClassList)
						{
							unknownClass.ElementType = DBHelper.GetElementType(((EntityClassFile)unknownClass).SchemaName, ((EntityClassFile)unknownClass).TableName, ClassList, connectionString);
						}
					}
				}
				else if (server.DBType == DBServerType.MYSQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={_password.Text};";

					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
SELECT c.COLUMN_NAME as 'columnName',
       c.COLUMN_TYPE as 'datatype',
       case when c.CHARACTER_MAXIMUM_LENGTH is null then -1 else c.CHARACTER_MAXIMUM_LENGTH end as 'max_len',
       case when c.NUMERIC_PRECISION is null then 0 else c.NUMERIC_PRECISION end as 'precision',
        case when c.NUMERIC_SCALE is null then 0 else c.NUMERIC_SCALE end as 'scale',       
	   case when c.GENERATION_EXPRESSION != '' then 1 else 0 end as 'is_computed',
       case when c.EXTRA = 'auto_increment' then 1 else 0 end as 'is_identity',
       case when c.COLUMN_KEY = 'PRI' then 1 else 0 end as 'is_primary',
       case when c.COLUMN_KEY != '' then 1 else 0 end as 'is_indexed',
       case when c.IS_NULLABLE = 'no' then 0 else 1 end as 'is_nullable',
       case when cu.REFERENCED_TABLE_NAME is not null then 1 else 0 end as 'is_foreignkey',
       cu.REFERENCED_TABLE_NAME as 'foreigntablename'
  FROM `INFORMATION_SCHEMA`.`COLUMNS` as c
left outer join information_schema.KEY_COLUMN_USAGE as cu on cu.CONSTRAINT_SCHEMA = c.TABLE_SCHEMA
                                                         and cu.TABLE_NAME = c.TABLE_NAME
														 and cu.COLUMN_NAME = c.COLUMN_NAME
                                                         and cu.REFERENCED_TABLE_NAME is not null
 WHERE c.TABLE_SCHEMA=@schema
  AND c.TABLE_NAME=@tablename
ORDER BY c.ORDINAL_POSITION;
";

						using (var command = new MySqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@schema", db);
							command.Parameters.AddWithValue("@tablename", table.Table);
							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var x = reader.GetValue(8);

									var dbColumn = new DBColumn
									{
										ColumnName = reader.GetString(0),
										DataType = DBHelper.ConvertMySqlDataType(reader.GetString(1)),
										dbDataType = reader.GetString(1),
										Length = Convert.ToInt64(reader.GetValue(2)),
										NumericPrecision = Convert.ToInt32(reader.GetValue(3)),
										NumericScale = Convert.ToInt32(reader.GetValue(4)),
										IsComputed = Convert.ToBoolean(reader.GetValue(5)),
										IsIdentity = Convert.ToBoolean(reader.GetValue(6)),
										IsPrimaryKey = Convert.ToBoolean(reader.GetValue(7)),
										IsIndexed = Convert.ToBoolean(reader.GetValue(8)),
										IsNullable = Convert.ToBoolean(reader.GetValue(9)),
										IsForeignKey = Convert.ToBoolean(reader.GetValue(10)),
										ForeignTableName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
									};

									DatabaseColumns.Add(dbColumn);
								}
							}
						}
					}
				}
				else
				{
					string connectionString;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						connectionString = $"Server={server.ServerName};Database={db};Trusted_Connection=True;";
					else
						connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={_password.Text};";

					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
select c.name as column_name, 
       x.name as datatype, 
	   case when x.name = 'nchar' then c.max_length / 2
	        when x.name = 'nvarchar' then c.max_length / 2
			when x.name = 'text' then -1
			when x.name = 'ntext' then -1
			else c.max_length 
			end as max_length,
       case when c.precision is null then 0 else c.precision end as precision,
       case when c.scale is null then 0 else c.scale end as scale,
	   c.is_nullable, 
	   c.is_computed, 
	   c.is_identity,
	   case when ( select i.is_primary_key from sys.indexes as i inner join sys.index_columns as ic on ic.object_id = i.object_id and ic.index_id = i.index_id and i.is_primary_key = 1 where i.object_id = t.object_id and ic.column_id = c.column_id ) is not null  
	        then 1 
			else 0
			end as is_primary_key,
       case when ( select count(*) from sys.index_columns as ix where ix.object_id = c.object_id and ix.column_id = c.column_id ) > 0 then 1 else 0 end as is_indexed,
	   case when ( select count(*) from sys.foreign_key_columns as f where f.parent_object_id = c.object_id and f.parent_column_id = c.column_id ) > 0 then 1 else 0 end as is_foreignkey,
	   ( select t.name from sys.foreign_key_columns as f inner join sys.tables as t on t.object_id = f.referenced_object_id where f.parent_object_id = c.object_id and f.parent_column_id = c.column_id ) as foreigntablename
  from sys.columns as c
 inner join sys.tables as t on t.object_id = c.object_id
 inner join sys.schemas as s on s.schema_id = t.schema_id
 inner join sys.types as x on x.system_type_id = c.system_type_id and x.user_type_id = c.user_type_id
 where t.name = @tablename
   and s.name = @schema
   and x.name != 'sysname'
 order by t.name, c.column_id
";

						using (var command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@schema", table.Schema);
							command.Parameters.AddWithValue("@tablename", table.Table);

							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbColumn = new DBColumn
									{
										ColumnName = reader.GetString(0),
										EntityName = reader.GetString(0),
										dbDataType = reader.GetString(1),
										DataType = DBHelper.ConvertSqlServerDataType(reader.GetString(1)),
										Length = Convert.ToInt64(reader.GetValue(2)),
										NumericPrecision = Convert.ToInt32(reader.GetValue(3)),
										NumericScale = Convert.ToInt32(reader.GetValue(4)),
										IsNullable = Convert.ToBoolean(reader.GetValue(5)),
										IsComputed = Convert.ToBoolean(reader.GetValue(6)),
										IsIdentity = Convert.ToBoolean(reader.GetValue(7)),
										IsPrimaryKey = Convert.ToBoolean(reader.GetValue(8)),
										IsIndexed = Convert.ToBoolean(reader.GetValue(9)),
										IsForeignKey = Convert.ToBoolean(reader.GetValue(10)),
										ForeignTableName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
									};

									if (string.Equals(dbColumn.dbDataType, "geometry", StringComparison.OrdinalIgnoreCase))
									{
										_tableList.SelectedIndex = -1;
										MessageBox.Show(".NET Core does not support the SQL Server geometry data type. You cannot create an entity model from this table.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
										return;
									}

									if (string.Equals(dbColumn.dbDataType, "geography", StringComparison.OrdinalIgnoreCase))
									{
										_tableList.SelectedIndex = -1;
										MessageBox.Show(".NET Core does not support the SQL Server geography data type. You cannot create an entity model from this table.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
										return;
									}

									if (string.Equals(dbColumn.dbDataType, "variant", StringComparison.OrdinalIgnoreCase))
									{
										_tableList.SelectedIndex = -1;
										MessageBox.Show("COFRS does not support the SQL Server sql_variant data type. You cannot create an entity model from this table.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
										return;
									}

									DatabaseColumns.Add(dbColumn);
								}
							}
						}
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
			if (_tableList.SelectedIndex == -1)
			{
				MessageBox.Show("You must select a database table in order to create a controller", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SingularResourceName = SingularName.Text;
			PluralResourceName = PluralName.Text;

			Save();

			var server = (DBServer)_serverList.SelectedItem;
			DatabaseTable = (DBTable)_tableList.SelectedItem;

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				UndefinedClassList = StandardUtils.GenerateEntityClassList(UndefinedClassList, 
					                                                       ClassList, 
																		   Members, 
																		   EntityModelsFolder.Folder, 
																		   ConnectionString);
			}

			DialogResult = DialogResult.OK;
			Close();
		}
		#endregion

		#region Helper functions
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
			ServerType = server.DBType;

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				if (string.IsNullOrWhiteSpace(_password.Text))
					return;

				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new NpgsqlConnection(connectionString))
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

				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new MySqlConnection(connectionString))
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
				string connectionString;

				if (server.DBAuth == DBAuthentication.SQLSERVERAUTH && string.IsNullOrWhiteSpace(_password.Text))
					return;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
				else
					connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";

				_dbList.Items.Clear();
				_tableList.Items.Clear();
				int selectedItem = -1;

				try
				{
					using (var connection = new SqlConnection(connectionString))
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
				ServerType = dbServer.DBType;

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
					ServerType = dbServer.DBType;

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

		private bool LoadAppSettings(string folder)
		{
			var files = Directory.GetFiles(folder, "appSettings.Local.json");

			foreach (var file in files)
			{
				using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (var reader = new StreamReader(stream))
					{
						string content = reader.ReadToEnd();
						var settings = JObject.Parse(content);
						var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
						DefaultConnectionString = connectionStrings["DefaultConnection"].Value<string>();
						return true;
					}
				}
			}

			var childFolders = Directory.GetDirectories(folder);

			foreach (var childFolder in childFolders)
			{
				if (LoadAppSettings(childFolder))
					return true;
			}

			return false;
		}

		//private JObject ConstructExample(List<ClassFile> classList)
		//{
		//	var server = (DBServer)_serverList.SelectedItem;
		//	var table = (DBTable)_tableList.SelectedItem;
		//	var db = (string)_dbList.SelectedItem;
		//	var values = new JObject();

		//	//	-----------------------------------------------------------------
		//	//	Read data from MySQL
		//	//	-----------------------------------------------------------------
		//	if (server.DBType == DBServerType.MYSQL)
		//	{
		//		string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};uid={server.Username};pwd={_password.Text};";

		//		using (var connection = new MySqlConnection(connectionString))
		//		{
		//			connection.Open();

		//			var query = GenerateMySQLQuery(table);

		//			using (var command = new MySqlCommand(query, connection))
		//			{
		//				using (var reader = command.ExecuteReader())
		//				{
		//					if (reader.Read())
		//					{
		//						foreach (var column in DatabaseColumns)
		//						{
		//							var columnName = column.ColumnName;

		//							if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
		//							{
		//								values.Add(columnName, null);
		//							}
		//							else
		//							{
		//								var value = reader.GetValue(reader.GetOrdinal(columnName));
		//								values.Add(columnName, JToken.FromObject(value));
		//							}
		//						}

		//						return values;
		//					}
		//				}

		//				return GetMySqlValues(values);
		//			}
		//		}
		//	}

		//	//	-----------------------------------------------------------------
		//	//	Read data from Postgresql
		//	//	-----------------------------------------------------------------
		//	else if (server.DBType == DBServerType.POSTGRESQL)
		//	{
		//		ConnectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";

		//		using (var connection = new NpgsqlConnection(ConnectionString))
		//		{
		//			connection.Open();

		//			var query = GeneratePostgresqlQuery(table, classList);

		//			using (var command = new NpgsqlCommand(query, connection))
		//			{
		//				using (var reader = command.ExecuteReader())
		//				{
		//					if (reader.Read())
		//					{
		//						var ordinal = 0;

		//						foreach (var column in DatabaseColumns)
		//						{
		//							var columnName = column.ColumnName;

		//							if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
		//							{
		//								var cl = classList.FirstOrDefault(c => string.Equals(c.ClassName, column.dbDataType, StringComparison.OrdinalIgnoreCase));

		//								if (cl.ElementType == ElementType.Enum)
		//								{
		//									if (reader.IsDBNull(ordinal))
		//									{
		//										values.Add(columnName, null);
		//										ordinal++;
		//									}
		//									else
		//									{
		//										var value = reader.GetValue(ordinal);
		//										values.Add(columnName, JToken.FromObject(value));
		//										ordinal++;
		//									}
		//								}
		//								else
		//								{
		//									var jObject = new JObject();
		//									ordinal = ReadComposite(reader, column, ordinal, classList, jObject);
		//									values.Add(columnName, jObject);
		//								}
		//							}
		//							else if (reader.IsDBNull(ordinal))
		//							{
		//								values.Add(columnName, null);
		//								ordinal++;
		//							}
		//							else
		//							{
		//								var value = reader.GetValue(ordinal);

		//								if (value.GetType() == typeof(IPAddress))
		//								{
		//									var ipAddress = (IPAddress)value;
		//									values.Add(columnName, ipAddress.ToString());
		//								}
		//								else if (value.GetType() == typeof(IPAddress[]))
		//								{
		//									var theValue = (IPAddress[])value;
		//									var json = new JArray();

		//									foreach (var val in theValue)
		//									{
		//										json.Add(val.ToString());
		//									}
		//									values.Add(columnName, json);
		//								}
		//								else if (value.GetType() == typeof(ValueTuple<IPAddress, int>))
		//								{
		//									var ipAddress = ((ValueTuple<IPAddress, int>)value).Item1;
		//									int filter = ((ValueTuple<IPAddress, int>)value).Item2;

		//									var theValue = new JObject
		//									{
		//										{ "IPAddress", ipAddress.ToString() },
		//										{ "Filter", filter }
		//									};

		//									values.Add(columnName, theValue);
		//								}
		//								else if (value.GetType() == typeof(ValueTuple<IPAddress, int>[]))
		//								{
		//									var theValue = (ValueTuple<IPAddress, int>[])value;
		//									var json = new JArray();

		//									foreach (var val in theValue)
		//									{
		//										var ipAddress = val.Item1;
		//										int filter = val.Item2;

		//										var aValue = new JObject
		//										{
		//											{ "IPAddress", ipAddress.ToString() },
		//											{ "Filter", filter }
		//										};

		//										json.Add(aValue);
		//									}

		//									values.Add(columnName, json);
		//								}
		//								else if (value.GetType() == typeof(PhysicalAddress))
		//								{
		//									var physicalAddress = (PhysicalAddress)value;
		//									values.Add(columnName, physicalAddress.ToString());
		//								}
		//								else if (value.GetType() == typeof(PhysicalAddress[]))
		//								{
		//									var result = new JArray();
		//									var theValue = (PhysicalAddress[])value;

		//									foreach (var addr in theValue)
		//									{
		//										result.Add(addr.ToString());
		//									}

		//									values.Add(columnName, result);
		//								}
		//								else if (value.GetType() == typeof(BitArray))
		//								{
		//									var answer = new StringBuilder();

		//									foreach (bool val in (BitArray)value)
		//									{
		//										var strVal = val ? "1" : "0";
		//										answer.Append(strVal);
		//									}
		//									values.Add(columnName, JToken.FromObject(answer.ToString()));
		//								}
		//								else
		//								{
		//									values.Add(columnName, JToken.FromObject(value));
		//								}
		//								ordinal++;
		//							}
		//						}

		//						return values;
		//					}
		//				}
		//			}

		//			return GetPostgresqlValues(values);
		//		}
		//	}

		//	//	-----------------------------------------------------------------
		//	//	Read data from SQL Server
		//	//	-----------------------------------------------------------------
		//	else
		//	{
		//		string connectionString;

		//		if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
		//			connectionString = $"Server={server.ServerName};Database={(string)_dbList.SelectedItem};Trusted_Connection=True;";
		//		else
		//			connectionString = $"Server={server.ServerName};Database={(string)_dbList.SelectedItem};uid={server.Username};pwd={_password.Text};";

		//		using (var connection = new SqlConnection(connectionString))
		//		{
		//			connection.Open();

		//			var query = GenerateSQLServerQuery(table);

		//			using (var command = new SqlCommand(query, connection))
		//			{
		//				using (var reader = command.ExecuteReader())
		//				{
		//					if (reader.Read())
		//					{
		//						foreach (var column in DatabaseColumns)
		//						{
		//							var columnName = column.ColumnName;

		//							if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
		//							{
		//								values.Add(columnName, null);
		//							}
		//							else
		//							{
		//								var value = reader.GetValue(reader.GetOrdinal(columnName));
		//								values.Add(columnName, JToken.FromObject(value));
		//							}
		//						}
		//						return values;
		//					}
		//				}
		//			}

		//			return GetSqlServerValues(values);
		//		}
		//	}
		//}
		//private JObject GetSqlServerValues(JObject values)
		//{
		//	foreach (var column in DatabaseColumns)
		//	{
		//		var columnName = column.ColumnName;

		//		switch ((SqlDbType)column.DataType)
		//		{
		//			#region tinyint, smallint, int, bigint
		//			case SqlDbType.TinyInt:
		//				values.Add(columnName, JToken.FromObject((byte)1));
		//				break;

		//			case SqlDbType.SmallInt:
		//				values.Add(columnName, JToken.FromObject((short)1));
		//				break;

		//			case SqlDbType.Int:
		//				values.Add(columnName, JToken.FromObject((int)1));
		//				break;

		//			case SqlDbType.BigInt:
		//				values.Add(columnName, JToken.FromObject((long)1));
		//				break;
		//			#endregion

		//			#region varchar, nvarchar, text, ntext
		//			case SqlDbType.VarChar:
		//			case SqlDbType.NVarChar:
		//				{
		//					var answer = "The dog barked at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					values.Add(columnName, JToken.FromObject(answer));
		//				}
		//				break;

		//			case SqlDbType.Text:
		//			case SqlDbType.NText:
		//				values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
		//				break;
		//			#endregion

		//			#region binary, varbinary
		//			case SqlDbType.Binary:
		//				{
		//					if (column.Length == 1)
		//					{
		//						values.Add(columnName, JToken.FromObject((byte)32));
		//					}
		//					else if (column.Length == -1)
		//					{
		//						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//					}
		//					else
		//					{
		//						var answer = new byte[column.Length];
		//						for (int i = 0; i < column.Length; i++)
		//						{
		//							var byteValue = (byte)(i & 0x00FF);
		//							answer[i] = byteValue;
		//						}

		//						values.Add(columnName, JToken.FromObject(answer));
		//					}
		//				}
		//				break;

		//			case SqlDbType.VarBinary:
		//				values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;
		//			#endregion

		//			#region bit
		//			case SqlDbType.Bit:
		//				values.Add(columnName, JToken.FromObject(true));
		//				break;
		//			#endregion

		//			#region char, nchar
		//			case SqlDbType.NChar:
		//			case SqlDbType.Char:
		//				{
		//					if (column.Length == 1)
		//					{
		//						values.Add(columnName, JToken.FromObject('A'));
		//					}
		//					else if (column.Length == -1)
		//					{
		//						values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
		//					}
		//					else
		//					{
		//						const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
		//						var chars = new char[column.Length];
		//						for (int i = 0; i < chars.Length; i++)
		//						{
		//							int j = i % alphabet.Length;
		//							chars[i] = alphabet[j];
		//						}

		//						values.Add(columnName, JToken.FromObject(new string(chars)));
		//					}
		//				}
		//				break;
		//			#endregion

		//			#region image
		//			case SqlDbType.Image:
		//				{
		//					var image = (Image)new Bitmap(100, 100);
		//					var imageConverter = new ImageConverter();

		//					var bytes = imageConverter.ConvertTo(image, typeof(byte[]));
		//					values.Add(columnName, JToken.FromObject(bytes));
		//				}
		//				break;
		//			#endregion

		//			case SqlDbType.Date: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
		//			case SqlDbType.DateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
		//			case SqlDbType.DateTime2: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
		//			case SqlDbType.DateTimeOffset: values.Add(columnName, JToken.FromObject(DateTimeOffset.Now)); break;
		//			case SqlDbType.Decimal: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
		//			case SqlDbType.Float: values.Add(columnName, JToken.FromObject(Single.Parse("123.45"))); break;

		//			case SqlDbType.Money: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
		//			case SqlDbType.Real: values.Add(columnName, JToken.FromObject(Double.Parse("123.45"))); break;
		//			case SqlDbType.SmallDateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;


		//			case SqlDbType.SmallMoney: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;

		//			case SqlDbType.Time: values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(3))); break;
		//			case SqlDbType.Timestamp: values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })); break;
		//			case SqlDbType.UniqueIdentifier: values.Add(columnName, JToken.FromObject(Guid.NewGuid())); break;

		//			default:
		//				values.Add(columnName, JToken.FromObject("Unrecognized"));
		//				break;
		//		}
		//	}

		//	return values;
		//}

		//private JObject GetMySqlValues(JObject values)
		//{
		//	foreach (var column in DatabaseColumns)
		//	{
		//		var columnName = column.ColumnName;

		//		switch ((MySqlDbType)column.DataType)
		//		{
		//			#region tinyint, smallint, int, bigint
		//			case MySqlDbType.Byte:
		//				values.Add(columnName, JToken.FromObject((sbyte)1));
		//				break;

		//			case MySqlDbType.UByte:
		//				values.Add(columnName, JToken.FromObject((byte)1));
		//				break;

		//			case MySqlDbType.Int16:
		//				values.Add(columnName, JToken.FromObject((short)1));
		//				break;

		//			case MySqlDbType.UInt16:
		//				values.Add(columnName, JToken.FromObject((ushort)1));
		//				break;

		//			case MySqlDbType.Int24:
		//				values.Add(columnName, JToken.FromObject((int)1));
		//				break;

		//			case MySqlDbType.UInt24:
		//				values.Add(columnName, JToken.FromObject((uint)1));
		//				break;

		//			case MySqlDbType.Int32:
		//				values.Add(columnName, JToken.FromObject((int)1));
		//				break;

		//			case MySqlDbType.UInt32:
		//				values.Add(columnName, JToken.FromObject((uint)1));
		//				break;

		//			case MySqlDbType.Int64:
		//				values.Add(columnName, JToken.FromObject((long)1));
		//				break;

		//			case MySqlDbType.UInt64:
		//				values.Add(columnName, JToken.FromObject((ulong)1));
		//				break;
		//			#endregion

		//			#region decimal, double, float
		//			case MySqlDbType.Decimal:
		//				values.Add(columnName, JToken.FromObject((decimal)1.24m));
		//				break;
		//			case MySqlDbType.Double:
		//				values.Add(columnName, JToken.FromObject((double)1.24));
		//				break;
		//			case MySqlDbType.Float:
		//				values.Add(columnName, JToken.FromObject((float)1.24f));
		//				break;
		//			#endregion

		//			#region varchar, nvarchar, text, ntext
		//			case MySqlDbType.VarChar:
		//			case MySqlDbType.VarString:
		//				{
		//					var answer = "The dog barked at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					values.Add(columnName, JToken.FromObject(answer));
		//				}
		//				break;

		//			case MySqlDbType.Text:
		//			case MySqlDbType.TinyText:
		//			case MySqlDbType.MediumText:
		//			case MySqlDbType.LongText:
		//				values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
		//				break;

		//			case MySqlDbType.String:
		//				if (column.Length == 1)
		//					values.Add(columnName, JToken.FromObject('A'));
		//				else
		//				{
		//					const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
		//					var chars = new char[column.Length];
		//					for (int i = 0; i < chars.Length; i++)
		//					{
		//						int j = i % alphabet.Length;
		//						chars[i] = alphabet[j];
		//					}

		//					values.Add(columnName, JToken.FromObject(new string(chars)));
		//				}
		//				break;

		//			#endregion

		//			#region binary, varbinary
		//			case MySqlDbType.Binary:
		//				{
		//					if (column.Length == 1)
		//					{
		//						values.Add(columnName, JToken.FromObject((byte)32));
		//					}
		//					else if (column.Length == -1)
		//					{
		//						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//					}
		//					else
		//					{
		//						var answer = new byte[column.Length];
		//						for (int i = 0; i < column.Length; i++)
		//						{
		//							var byteValue = (byte)(i & 0x00FF);
		//							answer[i] = byteValue;
		//						}

		//						values.Add(columnName, JToken.FromObject(answer));
		//					}
		//				}
		//				break;

		//			case MySqlDbType.VarBinary:
		//			case MySqlDbType.TinyBlob:
		//			case MySqlDbType.Blob:
		//			case MySqlDbType.MediumBlob:
		//			case MySqlDbType.LongBlob:
		//				values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;
		//			#endregion

		//			#region bit
		//			case MySqlDbType.Bit:
		//				if (column.Length == 1)
		//					values.Add(columnName, JToken.FromObject(true));
		//				else
		//					values.Add(columnName, JToken.FromObject((ulong)1));
		//				break;
		//			#endregion

		//			#region enum, set
		//			case MySqlDbType.Enum:
		//				{
		//					var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
		//					values.Add(columnName, JToken.FromObject(theValues[1]));
		//				}
		//				break;

		//			case MySqlDbType.Set:
		//				{
		//					var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
		//					values.Add(columnName, JToken.FromObject(theValues[1]));
		//				}
		//				break;
		//			#endregion

		//			#region Datetime, timestamp, time, date, year
		//			case MySqlDbType.DateTime:
		//			case MySqlDbType.Date:
		//			case MySqlDbType.Timestamp:
		//				values.Add(columnName, JToken.FromObject(DateTime.Now));
		//				break;

		//			case MySqlDbType.Time:
		//				values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(2)));
		//				break;

		//			case MySqlDbType.Year:
		//				values.Add(columnName, JToken.FromObject(2020));
		//				break;
		//			#endregion

		//			default:
		//				values.Add(columnName, JToken.FromObject("Unrecognized"));
		//				break;
		//		}
		//	}

		//	return values;
		//}

		//private JObject GetPostgresqlValues(JObject values)
		//{
		//	foreach (var column in DatabaseColumns)
		//	{
		//		var columnName = column.ColumnName;

		//		switch ((NpgsqlDbType)column.DataType)
		//		{
		//			#region smallint, int, bigint
		//			case NpgsqlDbType.Smallint:
		//				values.Add(columnName, JToken.FromObject((short)1));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
		//				values.Add(columnName, JToken.FromObject(new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;

		//			case NpgsqlDbType.Integer:
		//				values.Add(columnName, JToken.FromObject((int)1));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Integer:
		//				values.Add(columnName, JToken.FromObject(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;

		//			case NpgsqlDbType.Bigint:
		//				values.Add(columnName, JToken.FromObject((long)1));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
		//				values.Add(columnName, JToken.FromObject(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;
		//			#endregion

		//			#region real, double, numeric
		//			case NpgsqlDbType.Real:
		//				values.Add(columnName, JToken.FromObject((float)1.3f));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Real:
		//				values.Add(columnName, JToken.FromObject(new float[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
		//				break;

		//			case NpgsqlDbType.Double:
		//				values.Add(columnName, JToken.FromObject((double)1.3f));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Double:
		//				values.Add(columnName, JToken.FromObject(new double[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
		//				break;

		//			case NpgsqlDbType.Numeric:
		//			case NpgsqlDbType.Money:
		//				values.Add(columnName, JToken.FromObject((decimal)1.3f));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
		//			case NpgsqlDbType.Array | NpgsqlDbType.Money:
		//				values.Add(columnName, JToken.FromObject(new decimal[] { 1.23m, 2.45m, 3.67m, 4.89m, 5.01m, 6.23m, 7.45m, 8.67m, 9.89m }));
		//				break;
		//			#endregion


		//			#region Guid
		//			case NpgsqlDbType.Uuid:
		//				values.Add(columnName, JToken.FromObject(Guid.NewGuid()));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
		//				values.Add(columnName, JToken.FromObject(new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }));
		//				break;
		//			#endregion

		//			#region json
		//			case NpgsqlDbType.Json:
		//				{
		//					var answer = "{ \"Name\": \"John\" }";
		//					values.Add(columnName, JToken.FromObject(answer));
		//				}
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Json:
		//				{
		//					var theList = new List<string>();

		//					var answer1 = "{ \"Name\": \"John\" }";
		//					theList.Add(answer1);

		//					var answer2 = "{ \"Name\": \"Jane\" }";
		//					theList.Add(answer2);

		//					var answer3 = "{ \"Name\": \"Bill\" }";
		//					theList.Add(answer3);

		//					values.Add(columnName, JToken.FromObject(theList.ToArray()));
		//				}
		//				break;

		//			#endregion

		//			#region varchar, text
		//			case NpgsqlDbType.Varchar:
		//				{
		//					var answer = "The dog barked at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					values.Add(columnName, JToken.FromObject(answer));
		//				}
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
		//				{
		//					var array = new JArray();
		//					var answer = "The dog barked at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					answer = "The cow mooed at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					answer = "The cat watched the dog and the cow.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					values.Add(columnName, array);
		//				}
		//				break;

		//			case NpgsqlDbType.Text:
		//				values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Text:
		//				{
		//					var array = new JArray
		//					{
		//						new JValue("The dog barked at the moon"),
		//						new JValue("The cow mooed at the moon"),
		//						new JValue("The cat watched the dog and the cow.")
		//					};
		//					values.Add(columnName, array);
		//				}
		//				break;
		//			#endregion

		//			#region bytea
		//			case NpgsqlDbType.Bytea:
		//				values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
		//				{
		//					var array = new JArray
		//					{
		//						JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
		//						JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
		//						JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
		//					};
		//					values.Add(columnName, array);
		//				}
		//				break;
		//			#endregion

		//			#region bit, varbit
		//			case NpgsqlDbType.Bit:
		//				{
		//					if (column.Length == 1)
		//						values.Add(columnName, JToken.FromObject(true));
		//					else
		//					{
		//						var str = new StringBuilder();
		//						for (int i = 0; i < column.Length; i++)
		//							str.Append("1");

		//						values.Add(columnName, JToken.FromObject(str.ToString()));
		//					}
		//				}
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Bit:
		//				{
		//					if (column.Length == 1)
		//					{
		//						var array = new JArray
		//						{
		//							JToken.FromObject(true),
		//							JToken.FromObject(true),
		//							JToken.FromObject(false),
		//							JToken.FromObject(true)
		//						};
		//						values.Add(columnName, array);
		//					}
		//					else
		//					{
		//						var array = new JArray();

		//						for (int i = 0; i < 3; i++)
		//						{
		//							var str = new StringBuilder();
		//							for (int j = 0; j < column.Length; j++)
		//								str.Append("1");
		//							array.Add(JToken.FromObject(str.ToString()));
		//						}

		//						values.Add(columnName, array);
		//					}
		//				}
		//				break;

		//			case NpgsqlDbType.Varbit:
		//				{
		//					var str = new StringBuilder();
		//					for (int i = 0; i < column.Length && i < 10; i++)
		//						str.Append("1");

		//					values.Add(columnName, JToken.FromObject(str.ToString()));
		//				}
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
		//				{
		//					if (column.Length == 1)
		//					{
		//						var array = new JArray
		//						{
		//							JToken.FromObject(true),
		//							JToken.FromObject(true),
		//							JToken.FromObject(false),
		//							JToken.FromObject(true)
		//						};
		//						values.Add(columnName, array);
		//					}
		//					else
		//					{
		//						var array = new JArray();

		//						for (int i = 0; i < 3; i++)
		//						{
		//							var str = new StringBuilder();
		//							for (int j = 0; j < column.Length && j < 10; j++)
		//								str.Append("1");
		//							array.Add(JToken.FromObject(str.ToString()));
		//						}

		//						values.Add(columnName, array);
		//					}
		//				}
		//				break;
		//			#endregion

		//			#region char
		//			case NpgsqlDbType.Char:
		//				{
		//					if (string.Equals(column.dbDataType, "_char", StringComparison.OrdinalIgnoreCase))
		//					{
		//						values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
		//					}
		//					else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase))
		//					{
		//						values.Add(columnName, JToken.FromObject('A'));
		//					}
		//					else
		//					{
		//						const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
		//						var chars = new char[column.Length];
		//						for (int i = 0; i < chars.Length; i++)
		//						{
		//							int j = i % alphabet.Length;
		//							chars[i] = alphabet[j];
		//						}

		//						values.Add(columnName, JToken.FromObject(new string(chars)));
		//					}
		//				}
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Char:
		//				{
		//					var array = new JArray();
		//					var answer = "The dog barked at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					answer = "The cow mooed at the moon.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					answer = "The cat watched the dog and the cow.";

		//					if (column.Length > -1)
		//						if (column.Length < answer.Length)
		//							answer = answer.Substring(0, (int)column.Length);

		//					array.Add(new JValue(answer));

		//					values.Add(columnName, array);
		//				}
		//				break;
		//			#endregion

		//			#region Boolean
		//			case NpgsqlDbType.Boolean:
		//				values.Add(columnName, JToken.FromObject((bool)true));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
		//				values.Add(columnName, JToken.FromObject(new bool[] { true, true, false }));
		//				break;
		//			#endregion

		//			#region DateTime
		//			case NpgsqlDbType.Date:
		//				values.Add(columnName, JToken.FromObject(DateTime.Now));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Date:
		//				values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
		//				break;

		//			case NpgsqlDbType.Timestamp:
		//				values.Add(columnName, JToken.FromObject(DateTime.Now));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
		//				values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
		//				break;

		//			case NpgsqlDbType.Time:
		//				values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(10)));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Time:
		//				values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
		//				break;

		//			case NpgsqlDbType.Interval:
		//				values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(15)));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.Interval:
		//				values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
		//				break;

		//			case NpgsqlDbType.TimeTz:
		//				values.Add(columnName, JToken.FromObject(DateTimeOffset.Now));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
		//				values.Add(columnName, JToken.FromObject(new DateTimeOffset[] { DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(2) }));
		//				break;

		//			case NpgsqlDbType.TimestampTz:
		//				values.Add(columnName, JToken.FromObject(DateTime.Now));
		//				break;

		//			case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
		//				values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
		//				break;
		//			#endregion

		//			default:
		//				values.Add(columnName, JToken.FromObject("Unrecognized"));
		//				break;
		//		}
		//	}

		//	return values;
		//}

		//private string GenerateSQLServerQuery(DBTable table)
		//{
		//	var query = new StringBuilder("SELECT TOP 1");

		//	bool firstColumn = true;

		//	foreach (var column in DatabaseColumns)
		//	{
		//		if (firstColumn)
		//			firstColumn = false;
		//		else
		//			query.Append(", ");

		//		var columnName = string.IsNullOrWhiteSpace(column.EntityName) ? column.ColumnName : column.EntityName;

		//		if (string.Equals(column.dbDataType, "hierarchyid", StringComparison.OrdinalIgnoreCase))
		//		{
		//			query.Append($"REPLACE(CAST({columnName} AS nvarchar({column.Length})), '/', '-' ) as [{columnName}]");
		//		}
		//		else
		//		{
		//			query.Append($"[{columnName}]");
		//		}
		//	}

		//	query.Append($" FROM [{table.Schema}].[{table.Table}] WITH(NOLOCK)");
		//	return query.ToString();
		//}

		//private string GenerateMySQLQuery(DBTable table)
		//{
		//	var query = new StringBuilder("SELECT ");

		//	bool firstColumn = true;

		//	foreach (var column in DatabaseColumns)
		//	{
		//		if (firstColumn)
		//			firstColumn = false;
		//		else
		//			query.Append(", "); 
				
		//		var columnName = string.IsNullOrWhiteSpace(column.EntityName) ? column.ColumnName : column.EntityName;

		//		query.Append($"`{columnName}`");
		//	}

		//	query.Append($" FROM `{table.Table}` LIMIT 1;");
		//	return query.ToString();
		//}

		//private string GeneratePostgresqlQuery(DBTable table, List<EntityClassFile> classList)
		//{
		//	var query = new StringBuilder("SELECT ");

		//	bool firstColumn = true;

		//	foreach (var column in DatabaseColumns)
		//	{
		//		var columnName = string.IsNullOrWhiteSpace(column.EntityName) ? column.ColumnName : column.EntityName;

		//		if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
		//		{
		//			var cl = classList.FirstOrDefault(c => string.Equals(c.ClassName, column.dbDataType, StringComparison.OrdinalIgnoreCase));

		//			if (cl == null || cl.ElementType == ElementType.Enum)
		//			{
		//				firstColumn = AppendComma(query, firstColumn);
		//				query.Append($"\"{columnName}\"");
		//			}
		//			else
		//			{
		//				var parent = $"\"{columnName}\"";
		//				firstColumn = DeconstructComposite(parent, column, classList, query, firstColumn);
		//			}
		//		}
		//		else
		//		{
		//			firstColumn = AppendComma(query, firstColumn);
		//			query.Append($"\"{columnName}\"");
		//		}
		//	}

		//	query.Append($" FROM \"{table.Schema}\".\"{table.Table}\" LIMIT 1");
		//	return query.ToString();
		//}

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

		#endregion
	}
}
