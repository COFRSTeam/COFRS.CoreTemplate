using COFRS.Template.Common.Models;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

namespace COFRS.Template.Common.ServiceUtilities
{
    public static class DBHelper
	{
		public static List<EntityModel> GenerateEntityClassList(List<EntityModel> UndefinedClassList, string baseFolder, string connectionString)
		{
			DTE2 mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			var resultList = new List<EntityModel>();	//	This will hold all the new undefined classes

			foreach (var classFile in UndefinedClassList)
			{
				//	Get the list of column for this undefined class
				var columns = GenerateColumns(classFile.SchemaName, classFile.TableName, classFile.ServerType, connectionString);

				foreach (var column in columns)
				{
					if (string.IsNullOrWhiteSpace(column.ModelDataType))
					{
						//	Is is already defined?
						var elementType = GetElementType(mDte, classFile.SchemaName, column.DBDataType, connectionString);

						if (elementType == ElementType.Enum)
						{
							//	Is is already defined?
							if ( codeService.GetEntityClassBySchema(classFile.SchemaName, column.DBDataType) == null)
							{
								//	Is is already in the list?
								if (UndefinedClassList.FirstOrDefault(c => c.SchemaName.Equals(classFile.SchemaName) &&
																		   c.TableName.Equals(column.DBDataType)) == null)
								{
									var className = $"E{codeService.CorrectForReservedNames(codeService.NormalizeClassName(column.ColumnName))}";

									var aClassFile = new EntityModel()
									{
										ClassName = className,
										TableName = column.DBDataType,
										SchemaName = classFile.SchemaName,
										ProjectName = classFile.ProjectName,
										Folder = Path.Combine(baseFolder, $"{className}.cs"),
										Namespace = classFile.Namespace,
										ElementType = elementType,
										ServerType = DBServerType.POSTGRESQL
									};

									resultList.Add(aClassFile);
								}
							}
						}
						else
						{
							//	Is it already defined?
							if ( codeService.GetEntityClassBySchema(classFile.SchemaName, column.DBDataType) == null )
							{
								//	Is it already in the list?
								if (UndefinedClassList.FirstOrDefault(c => c.SchemaName.Equals(classFile.SchemaName) &&
																		   c.TableName.Equals(column.DBDataType)) == null)
								{
									var className = $"E{codeService.CorrectForReservedNames(codeService.NormalizeClassName(column.ColumnName))}";

									var aClassFile = new EntityModel()
									{
										ClassName = className,
										TableName = column.DBDataType,
										SchemaName = classFile.SchemaName,
										ProjectName = classFile.ProjectName,
										Folder = Path.Combine(baseFolder, $"{className}.cs"),
										Namespace = classFile.Namespace,
										ElementType = elementType,
										ServerType = DBServerType.POSTGRESQL
									};

									resultList.AddRange(GenerateEntityClassList(new List<EntityModel>() { aClassFile }, baseFolder, connectionString));
								}
							}
						}
					}
				}
			}

			resultList.AddRange(UndefinedClassList);
			return resultList;
		}


		public static DBColumn[] GenerateEnumColumns(string schema, string tableName, string connectionString)
		{
			var columns = new List<DBColumn>();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			string query = @"
select e.enumlabel as enum_value
from pg_type t 
   join pg_enum e on t.oid = e.enumtypid  
   join pg_catalog.pg_namespace n ON n.oid = t.typnamespace
where t.typname = @dataType
  and n.nspname = @schema";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", tableName);
					command.Parameters.AddWithValue("@schema", schema);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var element = reader.GetString(0);
							var elementName = codeService.NormalizeClassName(element);

							var column = new DBColumn()
							{
								ColumnName = elementName,
								EntityName = element
							};

							columns.Add(column);
						}

					}
				}
			}

			return columns.ToArray();
		}

		public static DBColumn[] GenerateColumns(string schemaName, string tableName, DBServerType serverType, string connectionString)
		{
			var codeService = COFRSServiceFactory.GetService<ICodeService> ();
			var columns = new List<DBColumn>();

			if (serverType == DBServerType.POSTGRESQL)
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
						command.Parameters.AddWithValue("@schema", schemaName);
						command.Parameters.AddWithValue("@tablename", tableName);

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var dbColumn = new DBColumn
								{
									EntityName = reader.GetString(0),
									ColumnName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(reader.GetString(0))),
									ModelDataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1)),
									DBDataType = reader.GetString(1),
									Length = Convert.ToInt64(reader.GetValue(2)),
									IsNullable = Convert.ToBoolean(reader.GetValue(3)),
									IsComputed = Convert.ToBoolean(reader.GetValue(4)),
									IsIdentity = Convert.ToBoolean(reader.GetValue(5)),
									IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6)),
									IsIndexed = Convert.ToBoolean(reader.GetValue(7)),
									IsForeignKey = Convert.ToBoolean(reader.GetValue(8)),
									ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
								};

								columns.Add(dbColumn);
							}
						}
					}
				}
			}
			else if (serverType == DBServerType.MYSQL)
			{

			}
			else if (serverType == DBServerType.SQLSERVER)
			{
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
						command.Parameters.AddWithValue("@schema", schemaName);
						command.Parameters.AddWithValue("@tablename", tableName);

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
									throw new Exception(".NET Core does not support the SQL Server geometry data type. You cannot create an entity model from this table.");
								}

								if (string.Equals(dbColumn.DBDataType, "geography", StringComparison.OrdinalIgnoreCase))
								{
									throw new Exception(".NET Core does not support the SQL Server geometry data type. You cannot create an entity model from this table.");
								}

								if (string.Equals(dbColumn.DBDataType, "variant", StringComparison.OrdinalIgnoreCase))
								{
									throw new Exception("COFRS does not support the SQL Server sql_variant data type. You cannot create an entity model from this table.");
								}

								dbColumn.ModelDataType = DBHelper.GetSQLServerDataType(dbColumn);
								columns.Add(dbColumn);
							}
						}
					}
				}
			}

			return columns.ToArray();
		}

		public static MemoryCache _cache = new MemoryCache("ClassCache");

		public static string GetPostgresqlExampleValue(DBColumn column)
        {
			if ( string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
            {
				if (column.Length == 1)
					return "a";
				else
					return "string";
            }
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "int2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "oid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "xid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "cid", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "name", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "citext", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			else if (string.Equals(column.DBDataType, "bool", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "timestamp", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("s");
			}
			else if (string.Equals(column.DBDataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "float4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "money", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "bytea", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbit", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.DBDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}

		public static string GetMySqlExampleValue(DBColumn column)
		{
			if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "sysname", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "nvarchar", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			if (string.Equals(column.DBDataType, "year", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy");
			}
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase) ||
			         string.Equals(column.DBDataType, "nchar", StringComparison.OrdinalIgnoreCase))
					{
						if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyint(1)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint unsigned", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "datetime", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbinary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "blob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "longblob", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.DBDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}
		public static string GetSqlServerExampleValue(DBColumn column)
		{
			if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "ntext", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "nvarchar", StringComparison.OrdinalIgnoreCase) )
			{
				return "string";
			}
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "datetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "datetime2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smalldatetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "real", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "money", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallmoney", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbinary", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}

			return "example";
		}


		/// <summary>
		/// Convers a Postgresql data type into its corresponding standard SQL data type
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public static string ConvertPostgresqlDataType(string dataType)
		{
			if (string.Equals(dataType, "bpchar", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "_char", StringComparison.OrdinalIgnoreCase))
				return "char[]";
			else if (string.Equals(dataType, "char", StringComparison.OrdinalIgnoreCase))
				return "char";
			else if (string.Equals(dataType, "int2", StringComparison.OrdinalIgnoreCase))
				return "short";
			else if (string.Equals(dataType, "_int2", StringComparison.OrdinalIgnoreCase))
				return "short[]";
			else if (string.Equals(dataType, "int4", StringComparison.OrdinalIgnoreCase))
				return "int";
			else if (string.Equals(dataType, "_int4", StringComparison.OrdinalIgnoreCase))
				return "int[]";
			else if (string.Equals(dataType, "oid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_oid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "xid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_xid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "cid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_cid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "point", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPoint";
			else if (string.Equals(dataType, "_point", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPoint[]";
			else if (string.Equals(dataType, "lseg", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLSeg";
			else if (string.Equals(dataType, "_lseg", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLSeg[]";
			else if (string.Equals(dataType, "line", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLine";
			else if (string.Equals(dataType, "_line", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLine[]";
			else if (string.Equals(dataType, "circle", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlCircle";
			else if (string.Equals(dataType, "_circle", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlCircle[]";
			else if (string.Equals(dataType, "path", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPath";
			else if (string.Equals(dataType, "_path", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPath[]";
			else if (string.Equals(dataType, "polygon", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPolygon";
			else if (string.Equals(dataType, "_polygon", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPolygon[]";
			else if (string.Equals(dataType, "box", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlBox";
			else if (string.Equals(dataType, "_box", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlBox[]";
			else if (string.Equals(dataType, "int8", StringComparison.OrdinalIgnoreCase))
				return "long";
			else if (string.Equals(dataType, "_int8", StringComparison.OrdinalIgnoreCase))
				return "long[]";
			else if (string.Equals(dataType, "varchar", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_varchar", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_text", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "citext", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_citext", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "name", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_name", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "_bit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "varbit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "_varbit", StringComparison.OrdinalIgnoreCase))
				return "BitArray[][]";
			else if (string.Equals(dataType, "bytea", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_bytea", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";
			else if (string.Equals(dataType, "bool", StringComparison.OrdinalIgnoreCase))
				return "bool";
			else if (string.Equals(dataType, "_bool", StringComparison.OrdinalIgnoreCase))
				return "bool[]";
			else if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase))
				return "DateTime";
			else if (string.Equals(dataType, "_date", StringComparison.OrdinalIgnoreCase))
				return "DateTime[]";
			else if (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase))
				return "DateTime";
			else if (string.Equals(dataType, "_timestamp", StringComparison.OrdinalIgnoreCase))
				return "DateTime[]";
			else if (string.Equals(dataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset";
			else if (string.Equals(dataType, "_timestamptz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset[]";
			else if (string.Equals(dataType, "timetz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset";
			else if (string.Equals(dataType, "_timetz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset[]";
			else if (string.Equals(dataType, "time", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan";
			else if (string.Equals(dataType, "_time", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan[]";
			else if (string.Equals(dataType, "interval", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan";
			else if (string.Equals(dataType, "_interval", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan[]";
			else if (string.Equals(dataType, "float8", StringComparison.OrdinalIgnoreCase))
				return "double";
			else if (string.Equals(dataType, "_float8", StringComparison.OrdinalIgnoreCase))
				return "double[]";
			else if (string.Equals(dataType, "float4", StringComparison.OrdinalIgnoreCase))
				return "single";
			else if (string.Equals(dataType, "_float4", StringComparison.OrdinalIgnoreCase))
				return "single[]";
			else if (string.Equals(dataType, "money", StringComparison.OrdinalIgnoreCase))
				return "decimal";
			else if (string.Equals(dataType, "_money", StringComparison.OrdinalIgnoreCase))
				return "decimal[]";
			else if (string.Equals(dataType, "numeric", StringComparison.OrdinalIgnoreCase))
				return "decimal";
			else if (string.Equals(dataType, "_numeric", StringComparison.OrdinalIgnoreCase))
				return "decimal[]";
			else if (string.Equals(dataType, "uuid", StringComparison.OrdinalIgnoreCase))
				return "Guid";
			else if (string.Equals(dataType, "_uuid", StringComparison.OrdinalIgnoreCase))
				return "Guid[]";
			else if (string.Equals(dataType, "json", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_json", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "jsonb", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_jsonb", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "jsonpath", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_jsonpath", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "xml", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_xml", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "inet", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet";
			else if (string.Equals(dataType, "_inet", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet[]";
			else if (string.Equals(dataType, "cidr", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet";
			else if (string.Equals(dataType, "_cidr", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet[]"; 
			else if (string.Equals(dataType, "macaddr", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_macaddr", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";
			else if (string.Equals(dataType, "macaddr8", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_macaddr8", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";

			return "";
		}

		/// <summary>
		/// Returns a model type based upon the SQL Server database metadata
		/// </summary>
		/// <param name="column">The <see cref="DBColumn"/> that contains the database metadata</param>
		/// <returns>The corresponding C# model type</returns>
		public static string GetSQLServerDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "int":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "tinyint":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "float":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
				case "numeric":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "smalldatetime":
				case "datetime2":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "text":
				case "varchar":
				case "ntext":
				case "nvarchar":
					return "string";

				case "char":
				case "nchar":
					if (column.Length == 1)
						return "char";

					return "string";

				case "binary":
				case "varbinary":
				case "timestamp":
					return $"IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "datetimeoffset":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "money":
				case "smallmoney":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "image":
					return "Image";

				case "uniqueidentifier":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "";
		}

		public static string GetPostgresDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "boolean":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "_boolean":
					return "BitArray";

				case "bit":
				case "varbit":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case "_varbit":
				case "_bit":
					if (column.Length == 1)
						return "BitArray";
					else
						return "BitArray[]";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "_smallint":
					return "short[]";

				case "integer":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "_integer":
					return "int[]";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "_bigint":
					return "long[]";

				case "oid":
				case "xid":
				case "cid":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "_oid":
				case "_xid":
				case "_cid":
					return "uint[]";

				case "point":
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case "_point":
					return "NpgsqlPoint[]";

				case "lseg":
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case "_lseg":
					return "NpgsqlLSeg[]";

				case "line":
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case "_line":
					return "NpgsqlLine[]";

				case "circle":
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case "_circle":
					return "NpgsqlCircle[]";

				case "box":
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case "_box":
					return "NpgsqlBox[]";

				case "path":
					return "NpgsqlPoint[]";

				case "_path":
					return "NpgsqlPoint[][]";

				case "polygon":
					return "NpgsqlPoint[]";

				case "_polygon":
					return "NpgsqlPoint[][]";

				case "bytea":
					return "byte[]";

				case "_bytea":
					return "byte[][]";

				case "text":
				case "citext":
					return "string";

				case "name":
					return "string";

				case "_text":
				case "_name":
				case "_citext":
					return "string[]";

				case "varchar":
				case "json":
					return "string";

				case "_varchar":
				case "_json":
					return "string[]";

				case "char":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else
						return "char[]";

				case "bpchar":
					return "string";

				case "_char":
					return "string[]";

				case "uuid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "_uuid":
					return "Guid[]";

				case "date":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_date":
					return "DateTime[]";

				case "timetz":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "_timetz":
					return "DateTimeOffset[]";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_time":
					return "TimeSpan[]";

				case "interval":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_interval":
					return "TimeSpan[]";

				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamp":
					return "DateTime[]";

				case "timestamptz":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamptz":
					return "DateTime[]";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "_double":
					return "double[]";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "_real":
					return "float[]";

				case "numeric":
				case "money":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "_numeric":
				case "_money":
					return "decimal[]";

				case "xml":
					return "string";

				case "_xml":
					return "string[]";

				case "jsonb":
					return "string";

				case "_jsonb":
					return "string[]";

				case "jsonpath":
					return "string";

				case "_jsonpath":
					return "string[]";

				case "inet":
					return "IPAddress";

				case "cidr":
					return "ValueTuple<IPAddress, int>";

				case "_inet":
					return "IPAddress[]";

				case "_cidr":
					return "ValueTuple<IPAddress, int>[]";

				case "macaddr":
				case "macaddr8":
					return "PhysicalAddress";

				case "_macaddr":
				case "_macaddr8":
					return "PhysicalAddress[]";
			}

			return "";
		}

		public static string GetMySqlDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit(1)":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "bit":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "byte":
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case "ubyte":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "int16":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "uint16":
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case "int24":
				case "int32":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "uint24":
				case "uint32":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "int64":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "uint64":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "float":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "year":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "text":
				case "mediumtext":
				case "longtext":
				case "varchar":
				case "varstring":
				case "tinytext":
					return "string";

				case "string":
					if (column.Length == 1)
						return "char";
					return "string";

				case "binary":
				case "varbinary":
				case "tinyblob":
				case "blob":
				case "mediumblob":
				case "longblob":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "guid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "enum":
				case "set":
				case "json":
					return "string";
			}

			return "";
		}

		public static string GetPostgresqlResourceDataType(DBColumn column, List<ResourceModel> resourceModels)
		{
			switch (column.DBDataType.ToLower())
			{
				case "boolean":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "_boolean":
					return "BitArray";

				case "bit":
				case "varbit":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case "_varbit":
					if (column.Length == 1)
						return "BitArray";
					else
						return "BitArray[]";

				case "_bit":
						if (column.Length == 1)
						{
							if (column.IsNullable)
								return "bool?";
							else
								return "bool";
						}
						else
							return "BitArray";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "_smallint":
					return "short[]";

				case "integer":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "_integer":
					return "int[]";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "_bigint":
					return "long[]";

				case "oid":
				case "xid":
				case "cid":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "_oid":
				case "_xid":
				case "_cid":
					return "uint[]";

				case "point":
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case "_point":
					return "NpgsqlPoint[]";

				case "lseg":
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case "_lseg":
					return "NpgsqlLSeg[]";

				case "line":
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case "_line":
					return "NpgsqlLine[]";

				case "circle":
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case "_circle":
					return "NpgsqlCircle[]";

				case "box":
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case "_box":
					return "NpgsqlBox[]";

				case "path":
					return "NpgsqlPoint[]";

				case "_path":
					return "NpgsqlPoint[][]";

				case "polygon":
					return "NpgsqlPoint[]";

				case "_polygon":
					return "NpgsqlPoint[][]";

				case "bytea":
					return "byte[]";

				case "_bytea":
					return "byte[][]";

				case "text":
				case "citext":
					return "string";

				case "name":
					if (string.Equals(column.DBDataType, "_name", StringComparison.OrdinalIgnoreCase))
						return "string[]";
					else
						return "string";

				case "_text":
				case "_name":
				case "_citext":
					return "string[]";

				case "varchar":
				case "json":
					return "string";

				case "_varchar":
				case "_json":
					return "string[]";

				case "char":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else
						return "char[]";

				case "bpchar":
					return "string";

				case "_char":
					return "string[]";

				case "uuid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "_uuid":
					return "Guid[]";

				case "date":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_date":
					return "DateTime[]";

				case "timetz":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "_timetz":
					return "DateTimeOffset[]";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_time":
					return "TimeSpan[]";

				case "interval":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_interval":
					return "TimeSpan[]";

				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamp":
					return "DateTime[]";

				case "timestamptz":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamptz":
					return "DateTime[]";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "_double":
					return "double[]";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "_real":
					return "float[]";

				case "numeric":
				case "money":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "_numeric":
				case "_money":
					return "decimal[]";

				case "xml":
					return "string";

				case "_xml":
					return "string[]";

				case "jsonb":
					return "string";

				case "_jsonb":
					return "string[]";

				case "jsonpath":
					return "string";

				case "_jsonpath":
					return "string[]";

				case "inet":
					return "IPAddress";

				case "cidr":
					return "ValueTuple<IPAddress, int>";

				case "_inet":
					return "IPAddress[]";

				case "_cidr":
					return "ValueTuple<IPAddress, int>[]";

				case "macaddr":
				case "macaddr8":
					return "PhysicalAddress";

				case "_macaddr":
				case "_macaddr8":
					return "PhysicalAddress[]";

				default:
					{
						var resourceModel = resourceModels.FirstOrDefault(e =>
							string.Equals(e.EntityModel.TableName, column.DBDataType, StringComparison.OrdinalIgnoreCase));

						if (resourceModel != null)
							return resourceModel.ClassName;
					}
					break;
			}

			return "";
		}

		public static string GetMySqlResourceDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit(1)":
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
				case "bit":
						if (column.IsNullable)
							return "ulong?";
						else
							return "ulong";

				case "byte":
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case "ubyte":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "int16":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "uint16":
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case "int24":
				case "int32":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "uint24":
				case "uint32":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "int64":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "uint64":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "float":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "year":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "text":
				case "mediumtext":
				case "longtext":
				case "varchar":
				case "varstring":
				case "tinytext":
					return "string";

				case "string":
					if (column.Length == 1)
						return "char";
					return "string";

				case "binary":
				case "varbinary":
				case "tinyblob":
				case "blob":
				case "mediumblob":
				case "longblob":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "guid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "enum":
				case "set":
				case "json":
					return "string";
			}

			return "";
		}

		public static string GetSqlServerResourceDataType(DBColumn column)
		{
			switch ( column.DBDataType.ToLower())
			{
				case "bit":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "int":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "tinyint":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "float":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "smalldatetime":
				case "datetime2":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "text":
				case "varchar":
				case "ntext":
				case "nvarchar":
					return "string";

				case "char":
				case "nchar":
					if (column.Length == 1)
						return "char";

					return "string";

				case "binary":
				case "varbinary":
				case "timestamp":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "datetimeoffset":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "money":
				case "smallmoney":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "image":
					return "Image";

				case "uniqueidentifier":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "Unknown";
		}

		#region Postgrsql Helper Functions
		public static ElementType GetElementType(DTE2 dte2, string schema, string tableName, string connectionString)
		{
			var codeService = (ICodeService)COFRSServiceFactory.GetService<ICodeService>();
			var entityClass = codeService.GetEntityClassBySchema(schema, tableName);

			if (entityClass != null)
					return entityClass.ElementType;

			string query = @"
select t.typtype
  from pg_type as t 
 inner join pg_catalog.pg_namespace n on n.oid = t.typnamespace
 WHERE ( t.typrelid = 0 OR ( SELECT c.relkind = 'c' FROM pg_catalog.pg_class c WHERE c.oid = t.typrelid ) )
   AND NOT EXISTS ( SELECT 1 FROM pg_catalog.pg_type el WHERE el.oid = t.typelem AND el.typarray = t.oid )
   and ( t.typcategory = 'C' or t.typcategory = 'E' ) 
   and n.nspname = @schema
   and t.typname = @element
";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@schema", schema);
					command.Parameters.AddWithValue("@element", tableName);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							var theType = reader.GetChar(0);

							if (theType == 'c')
								return ElementType.Composite;

							else if (theType == 'e')
								return ElementType.Enum;
						}
					}
				}
			}

			return ElementType.Table;
		}

		#endregion
	}
}
