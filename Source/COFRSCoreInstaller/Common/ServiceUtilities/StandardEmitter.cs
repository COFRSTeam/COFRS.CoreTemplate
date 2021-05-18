using COFRS.Template.Common.Models;
using MySql.Data.MySqlClient;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace COFRS.Template.Common.ServiceUtilities
{
    public class StandardEmitter
    {
		/// <summary>
		/// Emits an entity data model based upon the fields contained within the database table
		/// </summary>
		/// <param name="serverType">The type of server used to house the table</param>
		/// <param name="table">The name of the database table</param>
		/// <param name="entityClassName">The class name for the model?</param>
		/// <param name="columns">The list of columns contained in the database</param>
		/// <param name="replacementsDictionary">List of replacements key/value pairs for the solution</param>
		/// <param name="connectionString">The connection string to connect to the database, if necessary</param>
		/// <returns>A model of the entity data table</returns>
		public string EmitEntityModel(DBServerType serverType, DBTable table, string entityClassName, List<ClassFile> classList, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary, string connectionString)
		{
			var result = new StringBuilder();
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");
			replacementsDictionary.Add("$barray$", "false");

			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{entityClassName}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(table.Schema))
				result.AppendLine($"\t[Table(\"{table.Table}\", DBType = \"{serverType}\")]");
			else
				result.AppendLine($"\t[Table(\"{table.Table}\", Schema = \"{table.Schema}\", DBType = \"{serverType}\")]");

			result.AppendLine($"\tpublic class {entityClassName}");
			result.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var column in columns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
				}

				AppendNullable(result, column.IsNullable, ref first);

				if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
				{
					if (column.Length > 1)
						AppendFixed(result, column.Length, true, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Name) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)) ||
						 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
				{
					if (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bit) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if ((serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varbit)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Citext) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Text)) ||
						 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Char)) ||
						 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String))
				{
					//	Insert the column definition
					if (serverType == DBServerType.POSTGRESQL)
					{
						if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
						else if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
					}
					else if (serverType == DBServerType.MYSQL)
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
					else
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
				}

				else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
						 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
						 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
						 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
				{
					AppendFixed(result, column.Length, true, ref first);
					AppendAutofield(result, ref first);
				}

				if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Decimal) ||
					(serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal) ||
					(serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Numeric))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, serverType, column, ref first);
				AppendEntityName(result, column, ref first);

				if (serverType == DBServerType.POSTGRESQL)
				{
					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
						replacementsDictionary["$net$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
						replacementsDictionary["$net$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
						replacementsDictionary["$netinfo$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
						replacementsDictionary["$netinfo$"] = "true";

					if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
						replacementsDictionary["$barray$"] = "true";

					if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
						replacementsDictionary["$barray$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Bit && column.Length > 1)
						replacementsDictionary["$barray$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
						replacementsDictionary["$barray$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Point)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Circle)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Box)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Line)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Path)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
						replacementsDictionary["$npgsqltypes$"] = "true";

					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Polygon)
						replacementsDictionary["$npgsqltypes$"] = "true";
				}
				else if (serverType == DBServerType.SQLSERVER)
				{
					if ((SqlDbType)column.DataType == SqlDbType.Image)
						replacementsDictionary["$image$"] = "true";
				}

				result.AppendLine(")]");

				//	Insert the column definition
				if (serverType == DBServerType.POSTGRESQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column, classList)} {column.ColumnName} {{ get; set; }}");
				else if (serverType == DBServerType.MYSQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetMySqlDataType(column)} {column.ColumnName} {{ get; set; }}");
				else if (serverType == DBServerType.SQLSERVER)
					result.AppendLine($"\t\tpublic {DBHelper.GetSQLServerDataType(column)} {column.ColumnName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		public string EmitResourceModel(EntityClassFile entityClassFile, List<ClassFile> classList, List<ClassMember> entityClassMembers, string resourceClassName, Dictionary<string, string> replacementsDictionary)
		{
			replacementsDictionary.Add("$resourceimage$", "false");
			replacementsDictionary.Add("$resourcenet$", "false");
			replacementsDictionary.Add("$resourcenetinfo$", "false");
			replacementsDictionary.Add("$resourcebarray$", "false");
			replacementsDictionary.Add("$usenpgtypes$", "false");

			var results = new StringBuilder();
			bool hasPrimary = false;

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({entityClassFile.ClassName}))]");
			results.AppendLine($"\tpublic class {resourceClassName}");
			results.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var member in entityClassMembers)
			{
				if (firstColumn)
					firstColumn = false;
				else
					results.AppendLine();

				if (member.EntityNames[0].IsPrimaryKey)
				{
					if (!hasPrimary)
					{
						results.AppendLine("\t\t///\t<summary>");
						results.AppendLine($"\t\t///\tThe hypertext reference that identifies the resource.");
						results.AppendLine("\t\t///\t</summary>");
						results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
						hasPrimary = true;
					}
				}
				else if (member.EntityNames[0].IsForeignKey)
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\tA hypertext reference that identifies the associated {member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");
					results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
				}
				else
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\t{member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");

					if ( entityClassFile.ServerType == DBServerType.SQLSERVER)
                    {
						if ( (SqlDbType)member.EntityNames[0].DataType == SqlDbType.Image)
                        {
							replacementsDictionary["$resourceimage$"] = "true";
						}
					}
					else if ( entityClassFile.ServerType == DBServerType.POSTGRESQL)
                    {
						if ((NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Inet ||
							(NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Cidr ||
							(NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr )
						{
							replacementsDictionary["$resourcenet$"] = "true";
						}

						else if ((NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr8)
							replacementsDictionary["$resourcenetinfo$"] = "true";

						else if ((NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean) ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit) ||
								 ((NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Bit && member.EntityNames[0].Length > 1) ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Varbit)
						{
							replacementsDictionary["$resourcebarray$"] = "true";
						}
						else if ((NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Unknown ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Point ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.LSeg ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Path ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Circle ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Polygon ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Line ||
								 (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Box)
						{
							replacementsDictionary["$usenpgtypes$"] = "true";
						}
                    }

					if (entityClassFile.ServerType == DBServerType.POSTGRESQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetPostgresqlResourceDataType(member.EntityNames[0], classList)} {member.ResourceMemberName} {{ get; set; }}");
					else if (entityClassFile.ServerType == DBServerType.MYSQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetMySqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (entityClassFile.ServerType == DBServerType.SQLSERVER)
						results.AppendLine($"\t\tpublic {DBHelper.GetSqlServerResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
				}
			}

			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitEnum(string schema, string dataType, string className, string connectionString)
		{
			var nn = new NameNormalizer(className);
			var builder = new StringBuilder();

			builder.Clear();
			builder.AppendLine("\t///\t<summary>");
			builder.AppendLine($"\t///\tEnumerates a list of {nn.PluralForm}");
			builder.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				builder.AppendLine($"\t[PgEnum(\"{dataType}\")]");
			else
				builder.AppendLine($"\t[PgEnum(\"{dataType}\", Schema = \"{schema}\")]");

			builder.AppendLine($"\tpublic enum {className}");
			builder.AppendLine("\t{");

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
					command.Parameters.AddWithValue("@dataType", dataType);
					command.Parameters.AddWithValue("@schema", schema);

					bool firstUse = true;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							if (firstUse)
								firstUse = false;
							else
							{
								builder.AppendLine(",");
								builder.AppendLine();
							}

							var element = reader.GetString(0);

							builder.AppendLine("\t\t///\t<summary>");
							builder.AppendLine($"\t\t///\t{element}");
							builder.AppendLine("\t\t///\t</summary>");
							builder.AppendLine($"\t\t[PgName(\"{element}\")]");

							var elementName = StandardUtils.NormalizeClassName(element);
							builder.Append($"\t\t{elementName}");
						}
					}
				}
			}

			builder.AppendLine();
			builder.AppendLine("\t}");

			return builder.ToString();
		}

		public string EmitComposite(string schema, string dataType, string className, string connectionString, Dictionary<string, string> replacementsDictionary, List<ClassFile> definedElements, List<ClassFile> undefinedElements)
		{
			var nn = new NameNormalizer(className);
			var result = new StringBuilder();

			result.Clear();
			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{className}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				result.AppendLine($"\t[PgComposite(\"{dataType}\")]");
			else
				result.AppendLine($"\t[PgComposite(\"{dataType}\", Schema = \"{schema}\")]");

			result.AppendLine($"\tpublic class {className}");
			result.AppendLine("\t{");

			string query = @"
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
    and c.relname = @dataType
 order by a.attnum";

			var columns = new List<DBColumn>();
			var candidates = new List<EntityClassFile>();

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", dataType);
					command.Parameters.AddWithValue("@schema", schema);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							NpgsqlDbType theDataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1));

							if ( theDataType == NpgsqlDbType.Unknown)
							{
								var cname = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(reader.GetString(1)));

								var classFile = new EntityClassFile()
								{
									ClassName = cname,
									SchemaName = schema,
									TableName = reader.GetString(1),
									ClassNameSpace = replacementsDictionary["$rootnamespace$"] + ".Models.EntityModels",
									FileName = Path.Combine(Utilities.LoadBaseFolder(replacementsDictionary["$solutiondirectory$"]), $"Models\\EntityModels\\{cname}.cs")
								};

								candidates.Add(classFile);
							}

							var column = new DBColumn
							{
								ColumnName = reader.GetString(0),
								DataType = theDataType,
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

							columns.Add(column);
						}
					}
				}
			}

			foreach (var candidate in candidates)
			{
				if (definedElements.FirstOrDefault(c => c.GetType() == typeof(EntityClassFile) &&
														 string.Equals(((EntityClassFile)c).SchemaName, candidate.SchemaName, StringComparison.OrdinalIgnoreCase) &&
														 string.Equals(((EntityClassFile)c).TableName, candidate.TableName, StringComparison.OrdinalIgnoreCase)) == null)
				{
					candidate.ElementType = DBHelper.GetElementType(candidate.SchemaName, candidate.TableName, definedElements, connectionString);
					undefinedElements.Add(candidate);
				}
			}

			if (undefinedElements.Count > 0)
				return string.Empty;

			bool firstColumn = true;

			foreach (var column in columns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
				}

				AppendNullable(result, column.IsNullable, ref first);


				if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					((NpgsqlDbType)column.DataType == NpgsqlDbType.Name) ||
					((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)))
				{
					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Bit) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varbit)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
						 ((NpgsqlDbType)column.DataType == NpgsqlDbType.Citext) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Text)))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Char) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Char)))
				{
					//	Insert the column definition
					if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
					{
						AppendFixed(result, column.Length, true, ref first);
					}
					else if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
					{
						AppendFixed(result, column.Length, true, ref first);
					}
				}

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea)
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Numeric)
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, DBServerType.POSTGRESQL, column, ref first);
				AppendEntityName(result, column, ref first);

				if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
					replacementsDictionary["$net$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
					replacementsDictionary["$net$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
					replacementsDictionary["$netinfo$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
					replacementsDictionary["$netinfo$"] = "true";

				else if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Bit && column.Length > 1)
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Point)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Circle)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Box)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Line)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Path)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Polygon)
					replacementsDictionary["$npgsqltypes$"] = "true";

				result.AppendLine(")]");

				var memberName = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.ColumnName));
				result.AppendLine($"\t\t[PgName(\"{column.ColumnName}\")]");

				//	Insert the column definition
				result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column, definedElements)} {memberName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		/// <summary>
		/// Generate undefined elements
		/// </summary>
		/// <param name="composites">The list of elements to be defined"/></param>
		/// <param name="connectionString">The connection string to the database server</param>
		/// <param name="rootnamespace">The root namespace for the newly defined elements</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		/// <param name="definedElements">The lise of elements that are defined</param>
		public void GenerateComposites(List<ClassFile> composites, string connectionString, Dictionary<string, string> replacementsDictionary, List<ClassFile> definedElements)
		{
			foreach (var composite in composites)
			{
				if (composite.ElementType == ElementType.Enum)
				{
					var result = new StringBuilder();

					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");
					result.AppendLine();
					result.AppendLine($"namespace {composite.ClassNameSpace}");
					result.AppendLine("{");
					result.Append(EmitEnum(((EntityClassFile)composite).SchemaName, ((EntityClassFile)composite).TableName, composite.ClassName, connectionString));
					result.AppendLine("}");

					if (!Directory.Exists(Path.GetDirectoryName(composite.FileName)))
						Directory.CreateDirectory(Path.GetDirectoryName(composite.FileName));

					File.WriteAllText(composite.FileName, result.ToString());
				}
				else if (composite.ElementType == ElementType.Composite)
				{
					var result = new StringBuilder();
					var allElementsDefined = false;
					string body = string.Empty;

					while (!allElementsDefined)
					{
						var undefinedElements = new List<ClassFile>();
						body = EmitComposite(((EntityClassFile)composite).SchemaName, 
							                 ((EntityClassFile)composite).TableName, 
											 composite.ClassName, 
											 connectionString, 
											 replacementsDictionary, 
											 definedElements, 
											 undefinedElements);

						if (undefinedElements.Count > 0)
						{
							GenerateComposites(undefinedElements, connectionString, replacementsDictionary, definedElements);
							definedElements.AddRange(undefinedElements);
						}
						else
							allElementsDefined = true;
					}

					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");

					if (replacementsDictionary.ContainsKey("$net$"))
					{
						if (string.Equals(replacementsDictionary["$net$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Net;");
					}

					if (replacementsDictionary.ContainsKey("$barray$"))
					{
						if (string.Equals(replacementsDictionary["$barray$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Collections;");
					}

					if (replacementsDictionary.ContainsKey("$image$"))
					{
						if (string.Equals(replacementsDictionary["$image$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Drawing;");
					}

					if (replacementsDictionary.ContainsKey("$netinfo$"))
					{
						if (string.Equals(replacementsDictionary["$netinfo$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Net.NetworkInformation;");
					}

					result.AppendLine();
					result.AppendLine($"namespace {composite.ClassNameSpace}");
					result.AppendLine("{");
					result.Append(body);
					result.AppendLine("}");

					File.WriteAllText(composite.FileName, result.ToString());
				}
			}
		}


		#region Helper Functions
		private void AppendComma(StringBuilder result, ref bool first)
		{
			if (first)
				first = false;
			else
				result.Append(", ");
		}
		private void AppendPrimaryKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsPrimaryKey = true");
		}

		private void AppendIdentity(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIdentity = true, AutoField = true");
		}

		private void AppendIndexed(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIndexed = true");
		}

		private void AppendForeignKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsForeignKey = true");
		}

		private void AppendNullable(StringBuilder result, bool isNullable, ref bool first)
		{
			AppendComma(result, ref first);

			if (isNullable)
				result.Append("IsNullable = true");
			else
				result.Append("IsNullable = false");
		}

		private void AppendDatabaseType(StringBuilder result, DBServerType serverType, DBColumn column, ref bool first)
		{
			AppendComma(result, ref first);

			if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar)
				result.Append("NativeDataType=\"VarChar\"");
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary)
				result.Append("NativeDataType=\"VarBinary\"");
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
				result.Append("NativeDataType=\"char\"");
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal)
				result.Append("NativeDataType=\"Decimal\"");
			else
				result.Append($"NativeDataType=\"{column.dbDataType}\"");
		}

		private void AppendFixed(StringBuilder result, long length, bool isFixed, ref bool first)
		{
			AppendComma(result, ref first);

			if (length == -1)
			{
				if (isFixed)
					result.Append($"IsFixed = true");
				else
					result.Append($"IsFixed = false");
			}
			else
			{
				if (isFixed)
					result.Append($"Length = {length}, IsFixed = true");
				else
					result.Append($"Length = {length}, IsFixed = false");
			}
		}

		private void AppendAutofield(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("AutoField = true");
		}

		private void AppendEntityName(StringBuilder result, DBColumn column, ref bool first)
        {
			if (!string.IsNullOrWhiteSpace(column.EntityName) && !string.Equals(column.ColumnName, column.EntityName, StringComparison.Ordinal))
			{
				AppendComma(result, ref first);
				result.Append($"ColumnName = \"{column.EntityName}\"");
			}
		}


		private void AppendPrecision(StringBuilder result, int NumericPrecision, int NumericScale, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"Precision={NumericPrecision}, Scale={NumericScale}");
		}
		#endregion
	}
}
