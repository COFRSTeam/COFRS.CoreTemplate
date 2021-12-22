using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace COFRS.Template.Common.Windows
{
    /// <summary>
    /// Interaction logic for FullStackDialog.xaml
    /// </summary>
    public partial class FullStackDialog : DialogWindow
    {
		#region Variables
		public string SingularResourceName { get; set; }
		public string PluralResourceName { get; set; }
		public List<string> Policies;
		public string Policy { get; set; }

		public bool GenerateEntityModel { get; set; }
		public bool GenerateResourceModel { get; set; }
		public bool GenerateMappingModel { get; set; }
		public bool GenerateValidator { get; set; }
		public bool GenerateExampleData { get; set; }
		public bool GenerateController { get; set; }



		private ServerConfig _serverConfig;
		private bool Populating = true;
		public DBTable DatabaseTable { get; set; }
		public List<DBColumn> DatabaseColumns { get; set; }
		public string ConnectionString { get; set; }
		public ProjectFolder EntityModelsFolder { get; set; }
		public string DefaultConnectionString { get; set; }
		public Dictionary<string, string> ReplacementsDictionary { get; set; }
		public List<EntityModel> UndefinedEntityModels { get; set; }
		public DBServerType ServerType { get; set; }
		public ElementType EType { get; set; }
		#endregion

		public FullStackDialog()
		{
			InitializeComponent();
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

			Textbox_Singular.Text = SingularResourceName;
			Textbox_Plural.Text = PluralResourceName;


			if (Policies != null && Policies.Count > 0)
			{
				Combobox_Policy.Visibility = Visibility.Visible;
				Label_Policy.Visibility = Visibility.Visible;

				Combobox_Policy.Items.Clear();

				Combobox_Policy.Items.Add("Anonymous");

				foreach (var policy in Policies)
					Combobox_Policy.Items.Add(policy);

				Combobox_Policy.SelectedIndex = 0;
			}
			else
			{
				Combobox_Policy.Visibility = Visibility.Hidden;
				Label_Policy.Visibility = Visibility.Hidden;
			}

			DatabaseColumns = new List<DBColumn>();
			UndefinedEntityModels = new List<EntityModel>();
			ReadServerList();

			Button_OK.IsEnabled = false;
			Button_OK.IsDefault = false;
			Button_Cancel.IsDefault = true;
		}

		private void Databases_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				Listbox_Tables.SelectedIndex = -1;

				var server = (DBServer)Combobox_Server.SelectedItem;
				var db = (string)Listbox_Databases.SelectedItem;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={Textbox_Password.Password};";
					Listbox_Tables.Items.Clear();

					ConnectionString = connectionString;
					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
select schemaname, elementname
  from (
SELECT schemaname as schemaName, 
       tablename as elementName
  FROM pg_catalog.pg_tables
 WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema'

union all

select n.nspname as schemaName, 
       t.typname as elementName
  from pg_type as t 
 inner join pg_catalog.pg_namespace n on n.oid = t.typnamespace
 WHERE ( t.typrelid = 0
                OR ( SELECT c.relkind = 'c'
                        FROM pg_catalog.pg_class c
                        WHERE c.oid = t.typrelid ) )
            AND NOT EXISTS (
                    SELECT 1
                        FROM pg_catalog.pg_type el
                        WHERE el.oid = t.typelem
                        AND el.typarray = t.oid )
            AND n.nspname <> 'pg_catalog'
            AND n.nspname <> 'information_schema'
            AND n.nspname !~ '^pg_toast'
			and ( t.typcategory = 'C' or t.typcategory = 'E' ) ) as X
order by schemaname, elementname";

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

									Listbox_Tables.Items.Add(dbTable);
								}
							}
						}
					}
				}
				else if (server.DBType == DBServerType.MYSQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={Textbox_Password.Password};";
					Listbox_Tables.Items.Clear();

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

									Listbox_Tables.Items.Add(dbTable);
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
						connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={Textbox_Password.Password};";

					ConnectionString = connectionString;

					Listbox_Tables.Items.Clear();

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
									Listbox_Tables.Items.Add(dbTable);
								}
							}
						}
					}
				}

				Listbox_Tables.SelectedIndex = -1;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Tables_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			Button_OK.IsEnabled = false;
			Button_OK.IsDefault = false;
			Button_Cancel.IsDefault = true;

			try
			{
				var server = (DBServer)Combobox_Server.SelectedItem;
				ServerType = server.DBType;
				var db = (string)Listbox_Databases.SelectedItem;
				var table = (DBTable)Listbox_Tables.SelectedItem;
				DatabaseColumns.Clear();

				if (server == null)
					return;

				if (string.IsNullOrWhiteSpace(db))
					return;

				if (table == null)
					return;

				Button_Cancel.IsDefault = false;
				Button_OK.IsEnabled = true;
				Button_OK.IsDefault = true;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={Textbox_Password.Password};";

					UndefinedEntityModels.Clear();
					EType = DBHelper.GetElementType(mDte, table.Schema, table.Table, connectionString);

					switch (EType)
					{
						case ElementType.Enum:
							break;

						case ElementType.Composite:
							{
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
												ConstructPostgresqlColumn(table, reader);
											}
										}
									}
								}

								if (UndefinedEntityModels.Count > 0)
								{
									WarnUndefinedContent(table, connectionString);
								}
							}
							break;

						case ElementType.Table:
							{
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
												ConstructPostgresqlColumn(table, reader);
											}
										}
									}

									if (UndefinedEntityModels.Count > 0)
									{
										WarnUndefinedContent(table, connectionString);
									}
								}
							}
							break;
					}
				}
				else if (server.DBType == DBServerType.MYSQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={Textbox_Password.Password};";

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
										ColumnName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(reader.GetString(0))),
										EntityName = reader.GetString(0),
										DBDataType = reader.GetString(1),
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

									dbColumn.ModelDataType = DBHelper.GetMySqlDataType(dbColumn);
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
						connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={Textbox_Password.Password};";

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
										ColumnName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(reader.GetString(0))),
										EntityName = reader.GetString(0),
										DBDataType = reader.GetString(1),
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


									if (string.Equals(dbColumn.DBDataType, "geometry", StringComparison.OrdinalIgnoreCase))
									{
										Listbox_Tables.SelectedIndex = -1;
										MessageBox.Show(".NET Core does not support the SQL Server geometry data type. You cannot create an entity model from this table.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
										return;
									}

									if (string.Equals(dbColumn.DBDataType, "geography", StringComparison.OrdinalIgnoreCase))
									{
										Listbox_Tables.SelectedIndex = -1;
										MessageBox.Show(".NET Core does not support the SQL Server geography data type. You cannot create an entity model from this table.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
										return;
									}

									if (string.Equals(dbColumn.DBDataType, "variant", StringComparison.OrdinalIgnoreCase))
									{
										Listbox_Tables.SelectedIndex = -1;
										MessageBox.Show("COFRS does not support the SQL Server sql_variant data type. You cannot create an entity model from this table.", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
										return;
									}

									dbColumn.ModelDataType = DBHelper.GetSQLServerDataType(dbColumn);
									DatabaseColumns.Add(dbColumn);
								}
							}
						}
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}



		private void OK_Click(object sender, RoutedEventArgs e)
		{
			if (Listbox_Tables.SelectedIndex == -1)
			{
				MessageBox.Show("You must select a database table in order to create a controller", "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Save();

			SingularResourceName = Textbox_Singular.Text;
			PluralResourceName = Textbox_Plural.Text;

			var server = (DBServer)Combobox_Server.SelectedItem;
			DatabaseTable = (DBTable)Listbox_Tables.SelectedItem;

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				UndefinedEntityModels = DBHelper.GenerateEntityClassList(UndefinedEntityModels,
																		 EntityModelsFolder.Folder,
																		 ConnectionString);
			}

			Policy = Combobox_Policy.SelectedItem.ToString();

			GenerateEntityModel = Checkbox_EntityModel.IsChecked.HasValue ? Checkbox_EntityModel.IsChecked.Value : false;
			GenerateResourceModel = Checkbox_ResourceModel.IsChecked.HasValue ? Checkbox_ResourceModel.IsChecked.Value : false;
			GenerateMappingModel = Checkbox_MappingModel.IsChecked.HasValue ? Checkbox_MappingModel.IsChecked.Value : false;
			GenerateValidator = Checkbox_Validator.IsChecked.HasValue ? Checkbox_Validator.IsChecked.Value : false;
			GenerateExampleData = Checkbox_ExampleData.IsChecked.HasValue ? Checkbox_ExampleData.IsChecked.Value : false;
			GenerateController = Checkbox_Controller.IsChecked.HasValue ? Checkbox_Controller.IsChecked.Value : false;

			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#region Database Functions
		private void ServerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (Combobox_ServerType.SelectedIndex == 0 || Combobox_ServerType.SelectedIndex == 1)
				{
					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
					Label_Authentication.Content = "Port Number";
					Textbox_PortNumber.Visibility = Visibility.Visible;
					Textbox_PortNumber.IsEnabled = true;
					Textbox_UserName.IsEnabled = true;
					Label_UserName.IsEnabled = true;
					Textbox_Password.IsEnabled = true;
					Label_Password.IsEnabled = true;
					Checkbox_RememberPassword.IsEnabled = true;
				}
				else
				{
					Combobox_Authentication.IsEnabled = true;
					Combobox_Authentication.Visibility = Visibility.Visible;
					Label_Authentication.Content = "Authentication";
					Textbox_PortNumber.Visibility = Visibility.Hidden;
					Textbox_PortNumber.IsEnabled = false;
				}

				if (!Populating)
				{
					PopulateServers();
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Server_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (!Populating)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					var server = (DBServer)Combobox_Server.SelectedItem;
					ServerType = server.DBType;

					if (server != null)
					{
						if (server.DBType == DBServerType.SQLSERVER)
						{
							Label_Authentication.IsEnabled = true;
							Label_Authentication.Content = "Authentication";
							Combobox_Authentication.IsEnabled = true;
							Combobox_Authentication.Visibility = Visibility.Visible;
							Textbox_PortNumber.Visibility = Visibility.Hidden;
							Textbox_PortNumber.IsEnabled = false;

							Combobox_Authentication.SelectedIndex = (server.DBAuth == DBAuthentication.SQLSERVERAUTH) ? 0 : 1;

							if (server.DBAuth == DBAuthentication.SQLSERVERAUTH)
							{
								Label_UserName.IsEnabled = true;
								Textbox_UserName.IsEnabled = true;
								Textbox_UserName.Text = server.Username;

								Label_Password.IsEnabled = true;
								Textbox_Password.IsEnabled = true;
								Textbox_Password.Password = (server.RememberPassword) ? server.Password : string.Empty;

								Checkbox_RememberPassword.IsEnabled = true;
								Checkbox_RememberPassword.IsChecked = server.RememberPassword;
							}
							else
							{
								Label_UserName.IsEnabled = false;
								Textbox_UserName.IsEnabled = false;
								Textbox_UserName.Text = string.Empty;

								Label_Password.IsEnabled = false;
								Textbox_Password.IsEnabled = false;
								Textbox_Password.Password = string.Empty;

								Checkbox_RememberPassword.IsEnabled = false;
								Checkbox_RememberPassword.IsChecked = false;
							}
						}
						else if (server.DBType == DBServerType.POSTGRESQL)
						{
							Label_Authentication.IsEnabled = true;
							Label_Authentication.Content = "Port Number";
							Combobox_Authentication.IsEnabled = false;
							Combobox_Authentication.Visibility = Visibility.Hidden;

							Textbox_PortNumber.IsEnabled = true;
							Textbox_PortNumber.Text = server.PortNumber.ToString();
							Textbox_PortNumber.Visibility = Visibility.Visible;

							Label_UserName.IsEnabled = true;
							Textbox_UserName.IsEnabled = true;
							Textbox_UserName.Text = server.Username;

							Label_Password.IsEnabled = true;
							Textbox_Password.IsEnabled = true;
							Textbox_Password.Password = (server.RememberPassword) ? server.Password : string.Empty;

							Checkbox_RememberPassword.IsEnabled = true;
							Checkbox_RememberPassword.IsChecked = server.RememberPassword;
						}
						else if (server.DBType == DBServerType.MYSQL)
						{
							Label_Authentication.IsEnabled = true;
							Label_Authentication.Content = "Port Number";
							Combobox_Authentication.IsEnabled = false;
							Combobox_Authentication.Visibility = Visibility.Hidden;

							Textbox_PortNumber.IsEnabled = true;
							Textbox_PortNumber.Text = server.PortNumber.ToString();
							Textbox_PortNumber.Visibility = Visibility.Visible;

							Label_UserName.IsEnabled = true;
							Textbox_UserName.IsEnabled = true;
							Textbox_UserName.Text = server.Username;

							Label_Password.IsEnabled = true;
							Textbox_Password.IsEnabled = true;
							Textbox_Password.Password = (server.RememberPassword) ? server.Password : string.Empty;

							Checkbox_RememberPassword.IsEnabled = true;
							Checkbox_RememberPassword.IsChecked = server.RememberPassword;
						}

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void UserName_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				if (!Populating)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					var server = (DBServer)Combobox_Server.SelectedItem;

					if (server != null)
					{
						server.Username = Textbox_UserName.Text;

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Password_PasswordChanged(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!Populating)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					var server = (DBServer)Combobox_Server.SelectedItem;

					if (server != null)
					{
						if (server.RememberPassword)
							server.Password = Textbox_Password.Password;
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
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void PortNumber_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				if (!Populating)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					var server = (DBServer)Combobox_Server.SelectedItem;

					if (server != null)
					{
						server.PortNumber = Convert.ToInt32(Textbox_PortNumber.Text);

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void RememberPassword_Checked(object sender, RoutedEventArgs e)
		{
			RememberPassword_Changed(true);
		}

		private void RememberPassword_Unchecked(object sender, RoutedEventArgs e)
		{
			RememberPassword_Changed(false);
		}

		private void RememberPassword_Changed(bool isChecked)
		{
			try
			{
				if (!Populating)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					var server = (DBServer)Combobox_Server.SelectedItem;

					if (server != null)
					{
						server.RememberPassword = isChecked;

						if (!server.RememberPassword)
							server.Password = string.Empty;
						else
							server.Password = Textbox_Password.Password;

						var savedServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
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
			if (!Populating)
			{
				Listbox_Databases.Items.Clear();
				Listbox_Tables.Items.Clear();
				var server = (DBServer)Combobox_Server.SelectedItem;

				if (server != null)
				{
					server.DBAuth = Combobox_Authentication.SelectedIndex == 0 ? DBAuthentication.SQLSERVERAUTH : DBAuthentication.WINDOWSAUTH;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					{
						Textbox_UserName.Text = string.Empty;
						Textbox_UserName.IsEnabled = false;
						Label_UserName.IsEnabled = false;

						Textbox_Password.Password = string.Empty;
						Textbox_Password.IsEnabled = false;
						Label_Password.IsEnabled = false;

						Checkbox_RememberPassword.IsChecked = false;
						Checkbox_RememberPassword.IsEnabled = false;
					}
					else
					{
						Textbox_UserName.IsEnabled = true;
						Label_UserName.IsEnabled = true;

						Textbox_Password.IsEnabled = true;
						Label_Password.IsEnabled = true;

						Checkbox_RememberPassword.IsChecked = server.RememberPassword;
						Checkbox_RememberPassword.IsEnabled = true;
					}

					Save();

					if (TestConnection(server))
						PopulateDatabases();
				}
			}
		}

		private void AddNewServer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dialog = new AddConnectionDialog
				{
					LastServerUsed = (DBServer)Combobox_Server.SelectedItem
				};

				var result = dialog.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					Listbox_Databases.Items.Clear();
					Listbox_Tables.Items.Clear();
					_serverConfig.Servers.Add(dialog.Server);
					Save();

					switch (dialog.Server.DBType)
					{
						case DBServerType.MYSQL: Combobox_ServerType.SelectedIndex = 0; break;
						case DBServerType.POSTGRESQL: Combobox_ServerType.SelectedIndex = 1; break;
						case DBServerType.SQLSERVER: Combobox_ServerType.SelectedIndex = 2; break;
					}

					ServerType_SelectionChanged(this, new SelectionChangedEventArgs(e.RoutedEvent, null, null));

					for (int index = 0; index < Combobox_Server.Items.Count; index++)
					{
						if (string.Equals((Combobox_Server.Items[index] as DBServer).ServerName, dialog.Server.ServerName, StringComparison.OrdinalIgnoreCase))
						{
							Combobox_Server.SelectedIndex = index;
							break;
						}
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void RemoveServer_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var deprecatedServer = (DBServer)Combobox_Server.SelectedItem;
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

				ServerType_SelectionChanged(this, new SelectionChangedEventArgs(e.RoutedEvent, null, null));
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void PopulateServers()
		{
			DBServerType serverType;

			switch (Combobox_ServerType.SelectedIndex)
			{
				case 0: serverType = DBServerType.MYSQL; break;
				case 1: serverType = DBServerType.POSTGRESQL; break;
				case 2: serverType = DBServerType.SQLSERVER; break;
				default: serverType = DBServerType.SQLSERVER; break;
			}

			var serverList = _serverConfig.Servers.Where(s => s.DBType == serverType);

			Combobox_Server.Items.Clear();
			Listbox_Databases.Items.Clear();
			Listbox_Tables.Items.Clear();

			if (serverList.Count() == 0)
			{
				Combobox_Server.IsEnabled = false;
				Combobox_Server.SelectedIndex = -1;

				if (serverType == DBServerType.SQLSERVER)
				{
					Combobox_Authentication.SelectedIndex = -1;
					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Visible;

					Textbox_PortNumber.IsEnabled = false;
					Textbox_PortNumber.Visibility = Visibility.Hidden;
				}

				else if (serverType == DBServerType.POSTGRESQL)
				{
					Textbox_PortNumber.IsEnabled = false;
					Textbox_PortNumber.Text = "1024";
					Textbox_PortNumber.Visibility = Visibility.Visible;

					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
				}

				else if (serverType == DBServerType.MYSQL)
				{
					Textbox_PortNumber.IsEnabled = false;
					Textbox_PortNumber.Text = "3306";
					Textbox_PortNumber.Visibility = Visibility.Visible;

					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
				}

				Textbox_UserName.IsEnabled = false;
				Textbox_UserName.Text = string.Empty;

				Textbox_Password.IsEnabled = false;
				Textbox_Password.Password = string.Empty;

				Checkbox_RememberPassword.IsEnabled = false;
				Checkbox_RememberPassword.IsChecked = false;
			}
			else
			{
				Combobox_Server.IsEnabled = true;

				if (serverType == DBServerType.POSTGRESQL)
				{
					Textbox_PortNumber.IsEnabled = true;
					Textbox_PortNumber.Visibility = Visibility.Visible;
					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
				}

				else if (serverType == DBServerType.MYSQL)
				{
					Textbox_PortNumber.IsEnabled = true;
					Textbox_PortNumber.Visibility = Visibility.Visible;
					Combobox_Authentication.IsEnabled = false;
					Combobox_Authentication.Visibility = Visibility.Hidden;
				}

				else if (serverType == DBServerType.SQLSERVER)
				{
					Textbox_PortNumber.IsEnabled = false;
					Textbox_PortNumber.Visibility = Visibility.Hidden;
					Combobox_Authentication.IsEnabled = true;
					Combobox_Authentication.Visibility = Visibility.Visible;
				}

				foreach (var server in serverList)
				{
					Combobox_Server.Items.Add(server);
				}

				if (Combobox_Server.Items.Count > 0)
					Combobox_Server.SelectedIndex = 0;
			}
		}

		private void Save()
		{
			int index = 0;
			var server = (DBServer)Combobox_Server.SelectedItem;

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
			var server = (DBServer)Combobox_Server.SelectedItem;

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				if (string.IsNullOrWhiteSpace(Textbox_Password.Password))
					return;

				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={Textbox_Password.Password};";

				Listbox_Databases.Items.Clear();
				Listbox_Tables.Items.Clear();
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

									Listbox_Databases.Items.Add(databaseName);

									string cs = $"Server={server.ServerName};Port={server.PortNumber};Database={databaseName};User ID={server.Username};Password={Textbox_Password.Password};";

									if (DefaultConnectionString.StartsWith(cs, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (Listbox_Databases.Items.Count > 0)
						Listbox_Databases.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				if (string.IsNullOrWhiteSpace(Textbox_Password.Password))
					return;

				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={Textbox_Password.Password};";

				Listbox_Databases.Items.Clear();
				Listbox_Tables.Items.Clear();
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

									Listbox_Databases.Items.Add(databaseName);

									string cs = $"Server={server.ServerName};Port={server.PortNumber};Database={databaseName};UID={server.Username};PWD={Textbox_Password.Password};";

									if (DefaultConnectionString.StartsWith(cs, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (Listbox_Databases.Items.Count > 0)
						Listbox_Databases.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else
			{
				string connectionString;

				if (server.DBAuth == DBAuthentication.SQLSERVERAUTH && string.IsNullOrWhiteSpace(Textbox_Password.Password))
					return;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
				else
					connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={Textbox_Password.Password};";

				Listbox_Databases.Items.Clear();
				Listbox_Tables.Items.Clear();
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

									Listbox_Databases.Items.Add(databaseName);
									string cs;

									if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
										cs = $"Server={server.ServerName};Database={databaseName};Trusted_Connection=True;";
									else
										cs = $"Server={server.ServerName};Database={databaseName};uid={server.Username};pwd={Textbox_Password.Password};";

									if (DefaultConnectionString.StartsWith(cs, StringComparison.OrdinalIgnoreCase))
										selectedItem = itemindex;

									itemindex++;
								}
							}
						}
					}

					if (Listbox_Databases.Items.Count > 0)
						Listbox_Databases.SelectedIndex = selectedItem;
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Microsoft Visual Studio", MessageBoxButton.OK, MessageBoxImage.Error);
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
			Listbox_Tables.Items.Clear();
			Listbox_Databases.Items.Clear();

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				string connectionString;
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={Textbox_Password.Password};";

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
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={Textbox_Password.Password};";

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
					connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={Textbox_Password.Password};";


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
			Combobox_Server.Items.Clear();

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
					case DBServerType.MYSQL: Combobox_ServerType.SelectedIndex = 0; break;
					case DBServerType.POSTGRESQL: Combobox_ServerType.SelectedIndex = 1; break;
					case DBServerType.SQLSERVER: Combobox_ServerType.SelectedIndex = 2; break;
				}

				var serverList = _serverConfig.Servers.Where(s => s.DBType == selectedType);
				int index = 0;
				int selectedIndex = -1;

				foreach (var server in serverList)
				{
					Combobox_Server.Items.Add(server);

					if (string.Equals(server.ServerName, dbServer.ServerName, StringComparison.OrdinalIgnoreCase))
						selectedIndex = index;

					index++;
				}

				if (Combobox_Server.Items.Count > 0)
				{
					Combobox_Server.SelectedIndex = selectedIndex;

					if (dbServer.DBType == DBServerType.SQLSERVER)
					{
						Combobox_Authentication.SelectedIndex = dbServer.DBAuth == DBAuthentication.WINDOWSAUTH ? 0 : 1;

						if (Combobox_Authentication.SelectedIndex == 0)
						{
							Textbox_UserName.Text = string.Empty;
							Textbox_UserName.IsEnabled = false;

							Textbox_Password.Password = string.Empty;
							Textbox_Password.IsEnabled = false;

							Checkbox_RememberPassword.IsChecked = false;
							Checkbox_RememberPassword.IsEnabled = false;
						}
						else
						{
							Textbox_UserName.Text = dbServer.Username;
							Textbox_UserName.IsEnabled = true;

							Checkbox_RememberPassword.IsChecked = dbServer.RememberPassword;
							Checkbox_RememberPassword.IsEnabled = true;

							if (dbServer.RememberPassword)
							{
								Textbox_Password.Password = dbServer.Password;
								Textbox_Password.IsEnabled = true;
							}
							else
							{
								Textbox_Password.Password = string.Empty;
								Textbox_Password.IsEnabled = true;
							}
						}
					}
					else if (dbServer.DBType == DBServerType.POSTGRESQL)
					{
						Textbox_PortNumber.Text = dbServer.PortNumber.ToString();
						Textbox_UserName.Text = dbServer.Username;
						Textbox_UserName.IsEnabled = true;

						Checkbox_RememberPassword.IsChecked = dbServer.RememberPassword;
						Checkbox_RememberPassword.IsEnabled = true;
						Textbox_Password.IsEnabled = true;

						if (dbServer.RememberPassword)
						{
							Textbox_Password.Password = dbServer.Password;
						}
						else
						{
							Textbox_Password.Password = string.Empty;
						}
					}
					else if (dbServer.DBType == DBServerType.MYSQL)
					{
						Textbox_PortNumber.Text = dbServer.PortNumber.ToString();
						Textbox_UserName.Text = dbServer.Username;
						Textbox_UserName.IsEnabled = true;

						Checkbox_RememberPassword.IsChecked = dbServer.RememberPassword;
						Checkbox_RememberPassword.IsEnabled = true;
						Textbox_Password.IsEnabled = true;

						if (dbServer.RememberPassword)
						{
							Textbox_Password.Password = dbServer.Password;
						}
						else
						{
							Textbox_Password.Password = string.Empty;
						}
					}

					PopulateDatabases();
				}
			}
			else
			{
				//	There were no servers in the list, make sure everything is empty
				Combobox_ServerType.SelectedIndex = 1;

				Combobox_Authentication.IsEnabled = false;
				Combobox_Authentication.SelectedIndex = -1;

				Combobox_Server.IsEnabled = false;
				Combobox_Server.Items.Clear();

				Textbox_UserName.IsEnabled = false;
				Textbox_UserName.Text = string.Empty;

				Textbox_Password.IsEnabled = false;
				Textbox_Password.Password = string.Empty;

				Checkbox_RememberPassword.IsEnabled = false;
				Checkbox_RememberPassword.IsChecked = false;
			}

			//	We're done. Turn off the populating flag.
			Populating = false;
		}
		#endregion

		#region Utility Functions
		private void ConstructPostgresqlColumn(DBTable table, NpgsqlDataReader reader)
		{
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var entityName = reader.GetString(0);
			var columnName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(reader.GetString(0)));

			var dbColumn = new DBColumn
			{
				ColumnName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(reader.GetString(0))),
				EntityName = entityName,
				DBDataType = reader.GetString(1),
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

			dbColumn.ModelDataType = DBHelper.GetPostgresDataType(dbColumn);

			if (string.IsNullOrWhiteSpace(dbColumn.ModelDataType))
			{
				var theChildEntityClass = codeService.GetEntityClassBySchema(table.Schema, dbColumn.DBDataType);

				//	See if this column type is already defined...
				if (theChildEntityClass == null)
				{
					//	It's not defined. See if it is already included in the undefined list...
					if (UndefinedEntityModels.FirstOrDefault(ent =>
					   string.Equals(ent.SchemaName, table.Schema, StringComparison.OrdinalIgnoreCase) &&
					   string.Equals(ent.TableName, dbColumn.DBDataType, StringComparison.OrdinalIgnoreCase)) == null)
					{
						//	It's not defined, and it's not in the undefined list, so it is unknown. Let's make it known
						//	by constructing it and including it in the undefined list.
						entityName = dbColumn.DBDataType;
						var className = $"E{codeService.CorrectForReservedNames(codeService.NormalizeClassName(entityName))}";

						var entity = new EntityModel()
						{
							SchemaName = table.Schema,
							ClassName = className,
							TableName = entityName,
							Folder = Path.Combine(EntityModelsFolder.Folder, $"{className}.cs"),
							Namespace = EntityModelsFolder.Namespace,
							ServerType = DBServerType.POSTGRESQL,
							ProjectName = EntityModelsFolder.ProjectName
						};

						UndefinedEntityModels.Add(entity);
					}
				}

				dbColumn.ModelDataType = dbColumn.DBDataType;
			}

			DatabaseColumns.Add(dbColumn);
		}

		private void WarnUndefinedContent(DBTable table, string connectionString)
		{
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var message = new StringBuilder();
			message.Append($"The entity model {table.Table} uses ");

			var unknownEnums = new List<string>();
			var unknownComposits = new List<string>();
			var unknownTables = new List<string>();

			foreach (var unknownClass in UndefinedEntityModels)
			{
				unknownClass.ElementType = DBHelper.GetElementType(mDte, unknownClass.SchemaName, unknownClass.TableName, connectionString);

				if (unknownClass.ElementType == ElementType.Enum)
					unknownEnums.Add(unknownClass.TableName);
				else if (unknownClass.ElementType == ElementType.Composite)
					unknownComposits.Add(unknownClass.TableName);
				else if (unknownClass.ElementType == ElementType.Table)
					unknownTables.Add(unknownClass.TableName);
			}

			if (unknownEnums.Count > 0)
			{
				if (unknownEnums.Count > 1)
					message.Append("enum types of ");
				else
					message.Append("an enum type of ");

				for (int index = 0; index < unknownEnums.Count(); index++)
				{
					if (index == unknownEnums.Count() - 1 && unknownEnums.Count > 1)
						message.Append($" and {unknownEnums[index]}");
					else if (index > 0)
						message.Append($", {unknownEnums[index]}");
					else if (index == 0)
						message.Append(unknownEnums[index]);
				}
			}

			if (unknownComposits.Count > 0)
			{
				if (unknownEnums.Count > 0)
					message.Append("and ");

				if (unknownComposits.Count > 1)
					message.Append("composite types of ");
				else
					message.Append("a composite type of ");

				for (int index = 0; index < unknownComposits.Count(); index++)
				{
					if (index == unknownComposits.Count() - 1 && unknownComposits.Count > 1)
						message.Append($" and {unknownComposits[index]}");
					else if (index > 0)
						message.Append($", {unknownComposits[index]}");
					else if (index == 0)
						message.Append(unknownComposits[index]);
				}
			}

			if (unknownTables.Count > 0)
			{
				if (unknownEnums.Count > 0 || unknownComposits.Count > 0)
					message.Append("and ");

				if (unknownTables.Count > 1)
					message.Append("table types of ");
				else
					message.Append("a table type of ");

				for (int index = 0; index < unknownTables.Count(); index++)
				{
					if (index == unknownTables.Count() - 1 && unknownTables.Count > 1)
						message.Append($" and {unknownTables[index]}");
					else if (index > 0)
						message.Append($", {unknownTables[index]}");
					else if (index == 0)
						message.Append(unknownTables[index]);
				}
			}

			message.Append(".\r\n\r\nYou cannot generate this class until all the dependencies have been generated. Would you like to generate the undefined entities as part of generating this class?");

			var answer = MessageBox.Show(message.ToString(), "Microsoft Visual Studio", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (answer == MessageBoxResult.No)
				Button_OK.IsEnabled = false;
		}

		#endregion
	}
}
