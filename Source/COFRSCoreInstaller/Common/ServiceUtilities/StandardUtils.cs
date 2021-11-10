using COFRS.Template.Common.Extensions;
using COFRS.Template.Common.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace COFRS.Template.Common.ServiceUtilities
{
    public static class StandardUtils
	{
		public static void SaveProfileMap(Solution solution, ProfileMap theMap)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), $".cofrs\\{theMap.ResourceClassName}.{theMap.EntityClassName}.json");

			var json = JsonConvert.SerializeObject(theMap);

			using (var stream = new FileStream(mappingPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
				using (var writer = new StreamWriter(stream))
                {
					writer.Write(json);
                }
            }
		}

		public static string GetRelativeFolder(Solution solution, ProjectFolder folder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Project project = FindProject(solution, folder);
			var answer = "\\";

			if (project != null)
			{
				var projectFolder = project.Properties.Item("FullPath").Value.ToString();

				var solutionParts = projectFolder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
				var folderParts = folder.Folder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

				for (int i = solutionParts.Length; i < folderParts.Length; i++)
				{
					answer = Path.Combine(answer, folderParts[i]);
				}

				if (answer == "\\")
					answer = $"the root folder of {project.Name}";
				else
					answer = $"the {answer} folder of {project.Name}";

			}
			else
				answer = "unknown folder";

			return answer;
		}

		public static string GetProjectName(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				return project.Name;
			}

			return string.Empty;
		}

		public static Project GetProject(Solution solution, string projectName)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach ( Project project in solution.Projects)
            {
				if (string.Equals(project.Name, projectName, StringComparison.OrdinalIgnoreCase))
					return project;
            }

			return null;
        }

		private static ProjectItem FindValidator(ProjectItems projectItems, ResourceClassFile resourceClass, EntityClassFile entityClass)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					var validator = FindValidator(projectItem.ProjectItems, resourceClass, entityClass);

					if (validator != null)
						return validator;
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
						 projectItem.FileCodeModel != null &&
						 projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
						 Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					FileCodeModel code = projectItem.FileCodeModel;

					foreach (CodeNamespace namespaceElement in code.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeInterface candidateInterface in namespaceElement.Children.GetTypes<CodeInterface>())
						{
							var theName = $"IValidator<{resourceClass.ClassName}>";

							foreach (CodeInterface childCandidate in candidateInterface.Bases.GetTypes<CodeInterface>())
							{
								if (string.Equals(childCandidate.Name, "IValidator", StringComparison.OrdinalIgnoreCase))
								{
									if (childCandidate.FullName.Contains(resourceClass.ClassName))
										return projectItem;
								}
							}
						}
					}
				}
			}

			return null;
		}

		public static string CorrectForReservedNames(string columnName)
		{
			if (string.Equals(columnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "as", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "base", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "bool", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "break", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "byte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "case", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "catch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "char", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "checked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "class", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "const", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "continue", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "default", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "do", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "double", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "else", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "enum", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "event", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "extern", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "false", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "finally", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "float", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "for", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "goto", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "if", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "in", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "int", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "interface", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "internal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "is", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "lock", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "long", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "new", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "null", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "object", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "operator", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "out", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "override", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "params", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "private", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "protected", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "public", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ref", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "return", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "short", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "static", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "string", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "struct", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "switch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "this", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "throw", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "true", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "try", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "uint", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "using", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "void", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "while", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "add", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "alias", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "async", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "await", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "by", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "descending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "equals", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "from", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "get", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "global", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "group", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "into", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "join", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "let", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "on", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "partial", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "remove", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "select", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "set", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "var", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "when", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "where", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "yield", StringComparison.OrdinalIgnoreCase))
			{
				return $"{columnName}_Value";
			}

			return columnName;
		}

		public static string NormalizeClassName(string className)
		{
			var normalizedName = new StringBuilder();
			var indexStart = 1;

			while (className.EndsWith("_") && className.Length > 1)
				className = className.Substring(0, className.Length - 1);

			while (className.StartsWith("_") && className.Length > 1)
				className = className.Substring(1);

			if (className == "_")
				return className;

			normalizedName.Append(className.Substring(0, 1).ToUpper());

			int index = className.IndexOf("_");

			while (index != -1)
			{
				//	0----*----1----*----2
				//	street_address_1

				normalizedName.Append(className.Substring(indexStart, index - indexStart));
				normalizedName.Append(className.Substring(index + 1, 1).ToUpper());
				indexStart = index + 2;

				if (indexStart >= className.Length)
					index = -1;
				else
					index = className.IndexOf("_", indexStart);
			}

			if (indexStart < className.Length)
				normalizedName.Append(className.Substring(indexStart));

			return normalizedName.ToString();
		}

		public static List<EntityModel> GenerateEntityClassList(List<EntityModel> UndefinedClassList, EntityMap entityMap, string baseFolder, string connectionString)
		{
			List<EntityModel> resultList = new List<EntityModel>();

			foreach (var classFile in UndefinedClassList)
			{
				var newClassFile = GenerateEntityClass(classFile, entityMap, connectionString);
				resultList.Add(newClassFile);

				if (newClassFile.ElementType != ElementType.Enum)
				{
					foreach (var column in newClassFile.Columns)
					{
						if (string.IsNullOrWhiteSpace(column.ModelDataType))
						{
							if (UndefinedClassList.FirstOrDefault(c => string.Equals(c.TableName, column.EntityName, StringComparison.OrdinalIgnoreCase)) == null)
							{
								var aList = new List<EntityModel>();
								var bList = new List<EntityModel>();
								var className = $"E{StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.ColumnName))}";

								var elementType = DBHelper.GetElementType(classFile.SchemaName, column.DBDataType, entityMap, connectionString);

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

								aList.Add(aClassFile);
								bList.AddRange(entityMap.Maps);
								bList.AddRange(UndefinedClassList);

								var theMap = new EntityMap() { Maps = bList.ToArray() };

								resultList.AddRange(GenerateEntityClassList(aList, theMap, baseFolder, connectionString));
							}
						}
					}
				}
				else
					GenerateEnumColumns(newClassFile, connectionString);
			}

			return resultList;
		}

		private static EntityModel GenerateEntityClass(EntityModel classFile, EntityMap entityMap, string connectionString)
		{
			if (classFile.ElementType == ElementType.Enum)
				GenerateEnumColumns(classFile, connectionString);
			else
				GenerateColumns(classFile, connectionString);

			return classFile;
		}

		public static void GenerateEnumColumns(EntityModel entityModel, string connectionString)
		{
			var columns = new List<DBColumn>();

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
					command.Parameters.AddWithValue("@dataType", entityModel.TableName);
					command.Parameters.AddWithValue("@schema", entityModel.SchemaName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var element = reader.GetString(0);
							var elementName = NormalizeClassName(element);

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

			entityModel.Columns = columns.ToArray();
		}

		public static void GenerateColumns(EntityModel entityModel, string connectionString)
		{
			var columns = new List<DBColumn>();

			if (entityModel.ServerType == DBServerType.POSTGRESQL)
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
						command.Parameters.AddWithValue("@schema", entityModel.SchemaName);
						command.Parameters.AddWithValue("@tablename", entityModel.TableName);

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var dbColumn = new DBColumn
								{
									EntityName = reader.GetString(0),
									ColumnName = CorrectForReservedNames(NormalizeClassName(reader.GetString(0))),
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
			else if ( entityModel.ServerType == DBServerType.MYSQL )
            {

            }
			else if ( entityModel.ServerType == DBServerType.SQLSERVER)
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
						command.Parameters.AddWithValue("@schema", entityModel.SchemaName);
						command.Parameters.AddWithValue("@tablename", entityModel.TableName);

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var dbColumn = new DBColumn
								{
									ColumnName = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(reader.GetString(0))),
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

			entityModel.Columns = columns.ToArray();
		}

		public static bool IsChildOf(string parentPath, string candidateChildPath)
        {
			var a = Path.GetFullPath(parentPath).Replace('/', Path.DirectorySeparatorChar);
			var b = Path.GetFullPath(candidateChildPath).Replace('/', Path.DirectorySeparatorChar);

			if (a.EndsWith(Path.DirectorySeparatorChar.ToString()))
				a = a.Substring(0, a.Length - 1);

			if (b.EndsWith(Path.DirectorySeparatorChar.ToString()))
				b = b.Substring(0, b.Length - 1);

			a = a.ToLower();
			b = b.ToLower();

			if (b.Contains(a))
				return true;

			return false;
		}

		#region Solution functions



		private static void LoadChildMembers(DBServerType dbType, ClassMember member, EntityMap entityMap)
		{
			string memberProperName = string.Empty;

			if (member.ResourceMemberType.Contains("<"))
				return;

			if (member.ResourceMemberType.Contains(">"))
				return;

			if (member.ResourceMemberType.EndsWith("?"))
				memberProperName = member.ResourceMemberType.Substring(0, member.ResourceMemberType.Length - 1);
			else
				memberProperName = member.ResourceMemberType;

			var childClass = entityMap.Maps.FirstOrDefault(c => string.Equals(c.ClassName, memberProperName, StringComparison.OrdinalIgnoreCase));

			if (childClass != null && childClass.ElementType != ElementType.Enum)
			{
				var entityClass = childClass as EntityModel;

				foreach (var column in entityClass.Columns)
				{
					var memberName = column.ColumnName;
					var dataType = column.ModelDataType;

					var childMember = new ClassMember()
					{
						ResourceMemberName = memberName,
						ResourceMemberType = dataType,
						EntityNames = new List<DBColumn>(),
						ChildMembers = new List<ClassMember>()
					};

					LoadChildMembers(dbType, childMember, entityMap);

					member.ChildMembers.Add(childMember);
				}
			}
		}

		public static List<string> LoadPolicies(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<string>();
			var appSettings = solution.FindProjectItem("appSettings.json");

			var wasOpen = appSettings.IsOpen[EnvDTE.Constants.vsViewKindAny];

			if (!wasOpen)
				appSettings.Open(EnvDTE.Constants.vsViewKindTextView);

			var doc = appSettings.Document;
			var sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sel.Text)))
			{
				using (var textReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(textReader))
					{
						var jsonConfig = JObject.Load(reader, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });

						if (jsonConfig["OAuth2"] == null)
							return null;

						var oAuth2Settings = jsonConfig["OAuth2"].Value<JObject>();

						if (oAuth2Settings["Policies"] == null)
							return null;

						var policyArray = oAuth2Settings["Policies"].Value<JArray>();

						foreach (var policy in policyArray)
							results.Add(policy["Policy"].Value<string>());
					}
				}
			}

			if (!wasOpen)
				doc.Close();

			return results;
		}

		/// <summary>
		/// Checks to see if the candidate namespace is the root namespace of the startup project
		/// </summary>
		/// <param name="solution">The solution</param>
		/// <param name="candidateNamespace">The candidate namesapce</param>
		/// <returns><see langword="true"/> if the candidate namespace is the root namespace of the startup project; <see langword="false"/> otherwise</returns>
		public static bool IsRootNamespace(Solution solution, string candidateNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				try
				{
					var projectNamespace = project.Properties.Item("RootNamespace").Value.ToString();

					if (string.Equals(candidateNamespace, projectNamespace, StringComparison.OrdinalIgnoreCase))
						return true;
				}
				catch (ArgumentException) { }
			}

			return false;
		}

		public static string FindOrchestrationNamespace(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var projectItem = solution.FindProjectItem("ServiceOrchestrator.cs");
			var code = projectItem.FileCodeModel;

			foreach (CodeElement c in code.CodeElements)
			{
				if (c.Kind == vsCMElement.vsCMElementNamespace)
					return c.Name;
			}

			return string.Empty;
		}

		/// <summary>
		/// Find the project folder associated with the namespace
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <param name="destinationNamespace">The <see langword="namespace"/> to search for.</param>
		/// <returns>The collection of <see cref="ProjectFolder"/>s that contains the namespace</returns>
		public static List<ProjectFolder> FindProjectFolder(Solution solution, string destinationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var projectFolderCollection = new List<ProjectFolder>();

			foreach (Project project in solution.Projects)
			{
				try
				{
					var projectNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();
					string targetNamespace = destinationNamespace;

					
					var searchTemplate = targetNamespace.Replace(".","\\.").Replace("*", "[a-zA-Z_0-9]+");

					var match = Regex.Match(projectNamespace, searchTemplate);

					if ( match.Success )
                    {
						targetNamespace = match.Value;
                    }

					if (string.Equals(targetNamespace, projectNamespace, StringComparison.OrdinalIgnoreCase))
					{
						var result = new ProjectFolder()
						{
							Folder = project.Properties.Item("FullPath").Value.ToString(),
							Namespace = project.Properties.Item("DefaultNamespace").Value.ToString(),
							ProjectName = project.Name,
							Name = project.Name
						};

						projectFolderCollection.Add(result);
					}
					else if (targetNamespace.StartsWith(projectNamespace, StringComparison.OrdinalIgnoreCase))
					{
						ProjectItems projectItems = project.ProjectItems;
						bool continueLoop = true;

						while (continueLoop)
						{
							continueLoop = false;

							foreach (ProjectItem candidate in projectItems)
							{
								if (candidate.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
									candidate.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
								{
									var folderNamespace = candidate.Properties.Item("DefaultNamespace").Value.ToString();

									if (string.Equals(targetNamespace, folderNamespace, StringComparison.OrdinalIgnoreCase))
									{
										var result = new ProjectFolder()
										{
											Folder = candidate.Properties.Item("FullPath").Value.ToString(),
											Namespace = candidate.Properties.Item("DefaultNamespace").Value.ToString(),
											ProjectName = project.Name,
											Name = candidate.Name
										};

										projectFolderCollection.Add(result);
									}
									else if (targetNamespace.StartsWith(folderNamespace, StringComparison.OrdinalIgnoreCase))
									{
										projectItems = candidate.ProjectItems;
										continueLoop = true;
										break;
									}
								}
							}
						}
					}
				}
				catch ( Exception error )
                {
					Console.WriteLine(error.Message);
                }
			}

			return projectFolderCollection;
		}

		public static string LoadMoniker(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = solution.FindProjectItem("appSettings.json");

			var window = settingsFile.Open(Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;
			string moniker = string.Empty;

			sel.StartOfDocument();
			if (sel.FindText("CompanyName"))
			{
				sel.SelectLine();

				var match = Regex.Match(sel.Text, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");

				if (match.Success)
					moniker = match.Groups["moniker"].Value;
			}

			window.Close();

			return moniker;
		}

		public static string GetConnectionString(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem settingsFile = solution.FindProjectItem("appsettings.Local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];
			Window window = settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = doc.Selection as TextSelection;

			VirtualPoint activePoint = sel.ActivePoint;
			VirtualPoint anchorPoint = sel.AnchorPoint;

			sel.SelectAll();
			var settings = JObject.Parse(sel.Text);
			var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
			string connectionString = connectionStrings["DefaultConnection"].Value<string>();

			if (!wasOpen)
				window.Close();
			else
			{
				sel.Mode = vsSelectionMode.vsSelectionModeStream;
				sel.MoveToPoint(anchorPoint);
				sel.SwapAnchor();
				sel.MoveToPoint(activePoint);
			}

			return connectionString;
		}

		public static void ReplaceConnectionString(Solution solution, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = solution.FindProjectItem("appsettings.Local.json");

			var window = settingsFile.Open(Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();

			if (sel.FindText("Server=localdb;Database=master;Trusted_Connection=True;"))
			{
				sel.SelectLine();
				sel.Text = $"\t\t\"DefaultConnection\": \"{connectionString}\"\r\n";
				doc.Save();
			}

			window.Close();
		}

		/// <summary>
		/// This function will add the appropriate code to regster the validation model in the ServicesConfig.cs file.
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> containing the new validation class.</param>
		/// <param name="validationClass">The name of the validation class.</param>
		/// <param name="validationNamespace">The namespace where the validation class resides.</param>
		public static void RegisterValidationModel(Solution solution, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Get the ServicesConfig.cs project item.
			ProjectItem serviceConfig = solution.FindProjectItem("ServicesConfig.cs");
			bool wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];				//	Record if it was already open
			bool wasModified = false;													//	We haven't modified it yet

			if (!wasOpen)																//	If it wasn't open, open it.
				serviceConfig.Open(Constants.vsViewKindCode);

			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);		//	Get the window (so we can close it later)
			Document doc = serviceConfig.Document;										//	Get the doc 
			TextSelection sel = (TextSelection)doc.Selection;							//	Get the current selection
			var activePoint = sel.ActivePoint;											//	Get the active point

			//	The code will need to reference the validation namespace. Look
			//	to see if we have a using statement for that namespace.
			bool hasValidationUsing = false;											
			TextPoint endOfImports = sel.ActivePoint;

			foreach (CodeElement candidateUsing in serviceConfig.FileCodeModel.CodeElements)
			{
				if (candidateUsing.Kind == vsCMElement.vsCMElementImportStmt)
                {
					CodeImport codeImport = (CodeImport)candidateUsing;
					var theNamespace = codeImport.Namespace;
					endOfImports = codeImport.EndPoint;

					if ( string.Equals(theNamespace, validationNamespace, StringComparison.OrdinalIgnoreCase))
                    {
						hasValidationUsing = true;
					}
                }
			}

			//	If we don't have the using validation statement, we need to add it.
			//	We've kept track of the end of statement point on the last using
			//	statement, so insert the new one there.

			if ( !hasValidationUsing )
            {
				EditPoint2 editPoint = (EditPoint2)endOfImports.CreateEditPoint();
				editPoint.InsertNewLine();
				editPoint.Insert($"using {validationNamespace};");
				wasModified = true;
			}

			//	Now, we need to ensure that the code contains the AddScoped registration line. Find the namespace in this
			//	code. Inside of the namespace, find the class "ServiceCollectionExtension". Inside that class, find the
			//	function "ConfigureServices"
			//
			//	Once you have the "ConfigureServices" function, check to see if it already contains the AddScoped registration
			//	function (we don't want to insert it more than once). If it is not there, insert it as the last line of the
			//	function.
			foreach (CodeNamespace namespaceElement in serviceConfig.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
			{
				foreach (CodeClass candidateClass in namespaceElement.Members.GetTypes<CodeClass>())
				{
					if (string.Equals(candidateClass.Name, "ServiceCollectionExtension", StringComparison.OrdinalIgnoreCase))
					{
						foreach (CodeFunction candidateFunction in candidateClass.Members.GetTypes<CodeFunction>())
						{
							if (string.Equals(candidateFunction.Name, "ConfigureServices", StringComparison.OrdinalIgnoreCase))
							{
								sel.MoveToPoint(candidateFunction.StartPoint);
								var lineOfCode = $"services.AddScoped<I{validationClass}, {validationClass}>();";

								if (!sel.FindText(lineOfCode))
								{
									sel.MoveToPoint(candidateFunction.EndPoint);
									sel.LineUp();
									sel.EndOfLine();

									EditPoint2 editPoint = (EditPoint2)sel.ActivePoint.CreateEditPoint();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									sel.Insert(lineOfCode);
									wasModified = true;
								}
							}
						}
					}
				}
			}

			//	If we modified the document, save it now.
			if ( wasModified )
				doc.Save();

			//	If we were previously open, restore the active point to what it was before we changed it.
			//	Otherwise, if we were not previously open, the close the window.
			if (wasOpen)
				sel.MoveToPoint(activePoint);
            else
                window.Close();
        }

		public static void RegisterComposite(Solution solution, EntityModel entityModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (entityModel.ElementType == ElementType.Undefined ||
				entityModel.ElementType == ElementType.Table )
				return;

			ProjectItem serviceConfig = solution.FindProjectItem("ServicesConfig.cs");

			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);
			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasNpgsql = sel.FindText($"using Npgsql;");

			sel.StartOfDocument();
			var hasClassNamespace = sel.FindText($"using {entityModel.Namespace};");

			if (!hasNpgsql || !hasClassNamespace)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");

				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				if (!hasNpgsql)
				{
					sel.NewLine();
					sel.Insert($"using Npgsql;");
				}

				if (!hasClassNamespace)
				{
					sel.NewLine();
					sel.Insert($"using {entityModel.Namespace};");
				}
			}

            string searchText = (entityModel.ElementType == ElementType.Composite) ? 
				$"NpgsqlConnection.GlobalTypeMapper.MapComposite<{entityModel.ClassName}>(\"{entityModel.TableName}\");" :
				$"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityModel.ClassName}>(\"{entityModel.TableName}\");";

			if (!sel.FindText(searchText))
			{
				sel.StartOfDocument();
				sel.FindText("var myAssembly = Assembly.GetExecutingAssembly();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddSingleton<IRepositoryOptions>(RepositoryOptions);"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Postgresql Composits and Enums");
					sel.NewLine();
					if (entityModel.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{entityModel.ClassName}>(\"{entityModel.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityModel.ClassName}>(\"{entityModel.TableName}\");");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					if (entityModel.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{entityModel.ClassName}>(\"{entityModel.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityModel.ClassName}>(\"{entityModel.TableName}\");");
					sel.NewLine();
				}
			}

			doc.Save();
			window.Close();
		}

		public static List<ClassMember> LoadEntityClassMembers(EntityModel entityClass)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			foreach (var column in entityClass.Columns)
			{
				if (column.IsPrimaryKey)
				{
					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = "Href",
							ResourceMemberType = "Uri",
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						ColumnName = column.ColumnName,
						DBDataType = column.DBDataType,
						ModelDataType = column.ModelDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
				}
				else if (column.IsForeignKey)
				{
					string shortColumnName;

					if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
					{
						shortColumnName = CorrectForReservedNames(NormalizeClassName(column.ColumnName));
						if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
							shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
					}
					else
						shortColumnName = column.ForeignTableName;

					var normalizer = new NameNormalizer(shortColumnName);
					var domainName = normalizer.SingleForm;

					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = domainName,
							ResourceMemberType = "Uri",
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						ColumnName = column.ColumnName,
						ModelDataType = column.ModelDataType,
						DBDataType = column.DBDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
				}
				else
				{
					var normalizer = new NameNormalizer(column.EntityName);
					var resourceName = normalizer.PluralForm;

					if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
						resourceName = normalizer.SingleForm;

					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, resourceName, StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						ClassMember potentialMember = null;

						potentialMember = members.FirstOrDefault(m => resourceName.Length > m.ResourceMemberName.Length && string.Equals(m.ResourceMemberName, resourceName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

						if (potentialMember != null)
						{
							var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, resourceName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

							if (childMember != null)
								member = childMember;
						}
					}

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = resourceName,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						ColumnName = column.ColumnName,
						ModelDataType = column.ModelDataType,
						DBDataType = column.DBDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
					member.ResourceMemberName = entityColumn.ColumnName;
				}
			}

			return members;
		}

		private static void SetFixed(DBServerType serverType, DBColumn column, DBColumn entityColumn)
		{
			if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "nvarchar", StringComparison.OrdinalIgnoreCase))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)) ||
					  (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)))
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)) ||
					  (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)))
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NChar", StringComparison.OrdinalIgnoreCase))
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "String", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Image", StringComparison.OrdinalIgnoreCase))
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NText", StringComparison.OrdinalIgnoreCase))
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "MediumText", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "LongText", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "TinyText", StringComparison.OrdinalIgnoreCase)))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "ByteA", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)) ||
					 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)))
			{
				entityColumn.IsFixed = false;
			}
			else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Timestamp", StringComparison.OrdinalIgnoreCase))
			{
				entityColumn.IsFixed = true;
			}
		}

		/// <summary>
		/// Returns the <see cref="ProjectFolder"/> where the new item is being installed.
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <param name="replacementsDictionary">The dictionary of replacement values</param>
		/// <returns>The <see cref="ProjectFolder"/> where the new item is being installed.</returns>
		public static ProjectFolder GetInstallationFolder(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var selectedItem = dte.SelectedItems.Item(1);

			if (selectedItem.Project != null)
			{
				var projectFolder = new ProjectFolder
				{
					ProjectName = selectedItem.Project.Name,
					Folder = selectedItem.Project.Properties.Item("FullPath").Value.ToString(),
					Namespace = selectedItem.Project.Properties.Item("DefaultNamespace").Value.ToString(),
					Name = selectedItem.Project.Name
				};

				return projectFolder;
			}
			else
			{
				ProjectItem projectItem = selectedItem.ProjectItem;
				var project = projectItem.ContainingProject;

				var projectFolder = new ProjectFolder
				{
					ProjectName = project.Name,
					Folder = projectItem.Properties.Item("FullPath").Value.ToString(),
					Namespace = projectItem.Properties.Item("DefaultNamespace").Value.ToString(),
					Name = projectItem.Name
				};

				return projectFolder;
			}
		}


		#region Find the Entity Models Folder
		/// <summary>
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		public static ProjectFolder FindEntityModelsFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var entityFolder = ScanForEntity(project);

				if (entityFolder != null)
					return entityFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						entityFolder = FindEntityModelsFolder(candidateFolder, project.Name);

						if (entityFolder != null)
							return entityFolder;
					}
				}
			}

			//	We didn't find any entity models in the project. Search for the default entity models folder.
			var theCandidateNamespace = "*.Models.EntityModels";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static ProjectFolder FindEntityModelsFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var entityFolder = ScanForEntity(parent, projectName);

			if (entityFolder != null)
			{
				entityFolder.Name = parent.Name;
				return entityFolder;
			}

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					entityFolder = FindEntityModelsFolder(candidateFolder, projectName);

					if (entityFolder != null)
						return entityFolder;
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the project folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForEntity(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute tableAttribute = null;
								CodeAttribute compositeAttribute = null;

								try { tableAttribute = (CodeAttribute)childElement.Children.Item("Table"); } catch (Exception) { }
								try { compositeAttribute = (CodeAttribute)childElement.Children.Item("PgComposite"); } catch (Exception) { }

								if (tableAttribute != null || compositeAttribute != null)
								{
									return new ProjectFolder()
									{
										Folder = parent.Properties.Item("FullPath").Value.ToString(),
										Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
										ProjectName = projectName,
										Name = childElement.Name
									};
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForEntity(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute tableAttribute = null;
								CodeAttribute compositeAttribute = null;

								try { tableAttribute = (CodeAttribute)childElement.Children.Item("Table"); } catch (Exception) { }
								try { compositeAttribute = (CodeAttribute)childElement.Children.Item("PgComposite"); } catch (Exception) { }

								if (tableAttribute != null || compositeAttribute != null)
								{
									return new ProjectFolder()
									{
										Folder = parent.Properties.Item("FullPath").Value.ToString(),
										Namespace = namespaceElement.Name,
										ProjectName = parent.Name,
										Name = childElement.Name
									};
								}
							}
						}
					}
				}
			}

			return null;
		}
		#endregion

		#region Find the Resource Models Folder
		/// <summary>
		/// Locates and returns the resource models folder for the project
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an resource model, or null if none are found.</returns>
		public static ProjectFolder FindResourceModelsFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var resourceFolder = ScanForResource(project);

				if (resourceFolder != null)
					return resourceFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						resourceFolder = FindResourceModelsFolder(candidateFolder, project.Name);

						if (resourceFolder != null)
							return resourceFolder;
					}
				}
			}

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			var theCandidateNamespace = "*.Models.ResourceModels";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			theCandidateNamespace = "*.ResourceModels";

			candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the resource models folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an resource model, or null if none are found.</returns>
		private static ProjectFolder FindResourceModelsFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var resourceFolder = ScanForResource(parent, projectName);

			if (resourceFolder != null)
				return resourceFolder;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					resourceFolder = FindResourceModelsFolder(candidateFolder, projectName);

					if (resourceFolder != null)
						return resourceFolder;
				}
			}

			return null;
		}

		public static ProjectFolder FindMappingFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var mappingFolder = ScanForMapping(project);

				if (mappingFolder != null)
					return mappingFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						mappingFolder = FindMappingFolder(candidateFolder, project.Name);

						if (mappingFolder != null)
							return mappingFolder;
					}
				}
			}

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			var theCandidateNamespace = "*.Mapping";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the mapping folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static ProjectFolder FindMappingFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var mappingFolder = ScanForMapping(parent, projectName);

			if (mappingFolder != null)
				return mappingFolder;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					mappingFolder = FindMappingFolder(candidateFolder, projectName);

					if (mappingFolder != null)
						return mappingFolder;
				}
			}

			return null;
		}

		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindValidationFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var validadtionFolder = ScanForValidator(project);

				if (validadtionFolder != null)
					return validadtionFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						validadtionFolder = FindValidationFolder(candidateFolder, project.Name);

						if (validadtionFolder != null)
							return validadtionFolder;
					}
				}
			}

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			var theCandidateNamespace = "*.Validation";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the mapping folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static ProjectFolder FindValidationFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var validatorFolder = ScanForValidator(parent, projectName);

			if (validatorFolder != null)
				return validatorFolder;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					validatorFolder = FindValidationFolder(candidateFolder, projectName);

					if (validatorFolder != null)
						return validatorFolder;
				}
			}

			return null;
		}


		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindExampleFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var exampleFolder = ScanForExample(project);

				if (exampleFolder != null)
					return exampleFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						exampleFolder = FindExampleFolder(candidateFolder, project.Name);

						if (exampleFolder != null)
							return exampleFolder;
					}
				}
			}

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			var theCandidateNamespace = "*.Validation";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the mapping folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static ProjectFolder FindExampleFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var exampleFolder = ScanForExample(parent, projectName);

			if (exampleFolder != null)
				return exampleFolder;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					exampleFolder = FindExampleFolder(candidateFolder, projectName);

					if (exampleFolder != null)
						return exampleFolder;
				}
			}

			return null;
		}


		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static string FindValidatorInterface(Solution solution, string resourceClassName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var validationInterface = ScanForValidatorInterface(project.ProjectItems, resourceClassName);

				if (!string.IsNullOrWhiteSpace(validationInterface))
					return validationInterface;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						validationInterface = FindValidatorInterface(candidateFolder, resourceClassName);

						if (!string.IsNullOrWhiteSpace(validationInterface))
							return validationInterface;
					}
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Locates and returns the mapping folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static string FindValidatorInterface(ProjectItem parent, string resourceClassName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var validationInterface = ScanForValidatorInterface(parent.ProjectItems, resourceClassName);

			if (!string.IsNullOrWhiteSpace(validationInterface))
				return validationInterface;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					validationInterface = FindValidatorInterface(candidateFolder, resourceClassName);

					if (!string.IsNullOrWhiteSpace(validationInterface))
						return validationInterface;
				}
			}

			return null;
		}


		/// <summary>
		/// Find Controllers Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindControllersFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in solution.Projects)
			{
				var controllersFolder = ScanForControllers(project);

				if (controllersFolder != null)
					return controllersFolder;

				foreach (ProjectItem candidateFolder in project.ProjectItems)
				{
					if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
						candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
					{
						controllersFolder = FindControllersFolder(candidateFolder, project.Name);

						if (controllersFolder != null)
							return controllersFolder;
					}
				}
			}

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			var theCandidateNamespace = "*.Controllers";

			var candidates = FindProjectFolder(solution, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any folder matching the required namespace, so just return null.
			return null;
		}

		/// <summary>
		/// Locates and returns the mapping folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		private static ProjectFolder FindControllersFolder(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var controllersFolder = ScanForControllers(parent, projectName);

			if (controllersFolder != null)
				return controllersFolder;

			foreach (ProjectItem candidateFolder in parent.ProjectItems)
			{
				if (candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					candidateFolder.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					controllersFolder = FindControllersFolder(candidateFolder, projectName);

					if (controllersFolder != null)
						return controllersFolder;
				}
			}

			return null;
		}


		/// <summary>
		/// Scans the projects root folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForControllers(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass candidateClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isController = false;

							foreach (CodeElement parentClass in candidateClass.Bases)
							{
								if (string.Equals(parentClass.Name, "COFRSController", StringComparison.OrdinalIgnoreCase))
								{
									isController = true;
									break;
								}
							}

							if (isController)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = namespaceElement.Name,
									ProjectName = parent.Name,
									Name = candidateClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the project folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForControllers(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isController = false;

							foreach (CodeElement parentClass in codeClass.Bases)
							{
								if (string.Equals(parentClass.Name, "COFRSController", StringComparison.OrdinalIgnoreCase))
								{
									isController = true;
									break;
								}
							}

							if (isController)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
									ProjectName = projectName,
									Name = codeClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForValidator(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isValidator = false;

							foreach (CodeElement parentClass in codeClass.Bases)
							{
								if (string.Equals(parentClass.Name, "Validator", StringComparison.OrdinalIgnoreCase))
								{
									isValidator = true;
									break;
								}
							}

							if (isValidator)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = namespaceElement.Name,
									ProjectName = parent.Name,
									Name = codeClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the project folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForValidator(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeClass codeClass = (CodeClass)childElement;
								bool isValidator = false;

								foreach (CodeElement parentClass in codeClass.Bases)
								{
									if (string.Equals(parentClass.Name, "Validator", StringComparison.OrdinalIgnoreCase))
									{
										isValidator = true;
										break;
									}
								}

								if (isValidator)
								{
									return new ProjectFolder()
									{
										Folder = parent.Properties.Item("FullPath").Value.ToString(),
										Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
										ProjectName = projectName,
										Name = childElement.Name
									};
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForExample(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isExample = false;

							foreach ( CodeElement interfaceClass in codeClass.ImplementedInterfaces)
                            {
								if (string.Equals(interfaceClass.Name, "IExamplesProvider", StringComparison.OrdinalIgnoreCase))
								{
									isExample = true;
									break;
								}
							}

							if (isExample)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = namespaceElement.Name,
									ProjectName = parent.Name,
									Name = codeClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the project folder for an example class
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForExample(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeClass codeClass = (CodeClass)childElement;
								bool isExample = false;


								foreach (CodeElement interfaceClass in codeClass.ImplementedInterfaces)
								{
									if (string.Equals(interfaceClass.Name, "IExamplesProvider", StringComparison.OrdinalIgnoreCase))
									{
										isExample = true;
										break;
									}
								}

								if (isExample)
								{
									return new ProjectFolder()
									{
										Folder = parent.Properties.Item("FullPath").Value.ToString(),
										Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
										ProjectName = projectName,
										Name = childElement.Name
									};
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static string ScanForValidatorInterface(ProjectItems projectItems, string resourceClassName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in projectItems)
			{
				string validatorParentInterface = $"COFRS.IValidator<{resourceClassName}>";

				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeInterface candidateInterface in namespaceElement.Members.GetTypes<CodeInterface>())
						{
							foreach ( CodeInterface candidateParent in candidateInterface.Bases.GetTypes<CodeInterface>())
                            {
								var candidateParentName = candidateParent.FullName;

								if (string.Equals(candidateParentName, validatorParentInterface, StringComparison.OrdinalIgnoreCase))
									return candidateInterface.Name;
                            }
						}
					}
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Scans the project folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForMapping(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isProfile = false;

							foreach (CodeElement parentClass in codeClass.Bases)
							{
								if (string.Equals(parentClass.Name, "Profile", StringComparison.OrdinalIgnoreCase))
								{
									isProfile = true;
									break;
								}
							}

							if (isProfile)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
									ProjectName = projectName,
									Name = codeClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForMapping(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							bool isProfile = false;

							foreach (CodeElement parentClass in codeClass.Bases)
							{
								if (string.Equals(parentClass.Name, "Profile", StringComparison.OrdinalIgnoreCase))
								{
									isProfile = true;
									break;
								}
							}

							if (isProfile)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = namespaceElement.Name,
									ProjectName = parent.Name,
									Name = codeClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Scans the project folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private static ProjectFolder ScanForResource(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass candidateClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							CodeAttribute entityAttribute = null;

							try { entityAttribute = (CodeAttribute)candidateClass.Children.Item("Entity"); } catch (Exception) { }

							if (entityAttribute != null)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = parent.Properties.Item("DefaultNamespace").Value.ToString(),
									ProjectName = projectName,
									Name = candidateClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Scans the projects root folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private static ProjectFolder ScanForResource(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement candidateClass in namespaceElement.Members.GetTypes<CodeClass>())
						{
							CodeAttribute entityAttribute = null;

							try { entityAttribute = (CodeAttribute)candidateClass.Children.Item("Entity"); } catch (Exception) { }

							if (entityAttribute != null)
							{
								return new ProjectFolder()
								{
									Folder = parent.Properties.Item("FullPath").Value.ToString(),
									Namespace = namespaceElement.Name,
									ProjectName = parent.Name,
									Name = candidateClass.Name
								};
							}
						}
					}
				}
			}

			return null;
		}
		#endregion

		public static ProjectMapping OpenProjectMapping(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\ProjectMap.json");

			try
			{
				var jsonData = File.ReadAllText(mappingPath);

				var projectMapping = JsonConvert.DeserializeObject<ProjectMapping>(jsonData, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore,
					Formatting = Formatting.Indented,
					MissingMemberHandling = MissingMemberHandling.Ignore
				});

				return projectMapping;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (DirectoryNotFoundException)
			{
				return null;
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
				return null;
			}
		}

		public static void SaveProjectMapping(Solution solution, ProjectMapping projectMapping)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var jsonData = JsonConvert.SerializeObject(projectMapping, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				MissingMemberHandling = MissingMemberHandling.Ignore
			});

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\ProjectMap.json");

			File.WriteAllText(mappingPath, jsonData);
		}

		public static EntityMap LoadEntityModels(Solution solution, ProjectFolder entityModelsFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<EntityModel>();

			var entityFolderContents = FindProjectFolderContents(solution, entityModelsFolder);

			foreach (ProjectItem projectItem in entityFolderContents)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeClass classElement in namespaceElement.Members.GetTypes<CodeClass>())
						{
							CodeAttribute tableAttribute = null;
							CodeAttribute compositeAttribute = null;

							try { tableAttribute = (CodeAttribute)classElement.Children.Item("Table"); } catch (Exception) { }
							try { compositeAttribute = (CodeAttribute)classElement.Children.Item("PgComposite"); } catch (Exception) { }

							if (tableAttribute != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.SQLSERVER;

								var match = Regex.Match(tableAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
									serverType = (DBServerType)Enum.Parse(typeof(DBServerType), match.Groups["dbtype"].Value);
								}

								var entityModel = new EntityModel
								{
									ClassName = classElement.Name,
									ElementType = ElementType.Table,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement, entityModel);
								map.Add(entityModel);
							}
							else if (compositeAttribute != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.POSTGRESQL;
								var match = Regex.Match(compositeAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
								}

								var entityModel = new EntityModel
								{
									ClassName = classElement.Name,
									ElementType = ElementType.Composite,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement, entityModel);
								map.Add(entityModel);
							}
						}

						foreach (CodeEnum enumElement in namespaceElement.Members.GetTypes<CodeEnum>())
						{ 
							CodeAttribute attributeElement = null;

							try { attributeElement = (CodeAttribute)enumElement.Children.Item("PgEnum"); } catch (Exception) { }

							if (attributeElement != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.POSTGRESQL;

								var match = Regex.Match(attributeElement.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
								}

								var entityModel = new EntityModel
								{
									ClassName = enumElement.Name,
									ElementType = ElementType.Enum,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								var columns = new List<DBColumn>();

								foreach (CodeElement enumVariable in enumElement.Children)
								{
									if (enumVariable.Kind == vsCMElement.vsCMElementVariable)
									{
										CodeAttribute pgNameAttribute = null;
										try { pgNameAttribute = (CodeAttribute)enumElement.Children.Item("PgName"); } catch (Exception) { }

										var dbColumn = new DBColumn
										{
											ColumnName = enumElement.Name,
										};

										if (pgNameAttribute != null)
										{
											var matchit = Regex.Match(pgNameAttribute.Value, "\\\"(?<pgName>[_A-Za-z][A-Za-z0-9_]*)\\\"");

											if (matchit.Success)
												dbColumn.EntityName = matchit.Groups["pgName"].Value;
										}

										columns.Add(dbColumn);
									}
								}

								entityModel.Columns = columns.ToArray();

								map.Add(entityModel);
							}
						}
					}
				}
			}

			return new EntityMap() { Maps = map.ToArray() };
		}

        private static DBColumn[] LoadColumns(CodeClass codeClass, EntityModel entityModel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var columns = new List<DBColumn>();

            foreach (CodeElement memberElement in codeClass.Children)
            {
                if (memberElement.Kind == vsCMElement.vsCMElementProperty)
                {
                    CodeProperty property = (CodeProperty)memberElement;
					var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                    CodeAttribute memberAttribute = null;
                    try { memberAttribute = (CodeAttribute)memberElement.Children.Item("Member"); } catch (Exception) { }

					var dbColumn = new DBColumn
					{
						ColumnName = property.Name,
						EntityName = property.Name,
						ModelDataType = parts[parts.Count()-1]
                    };

                    if (memberAttribute != null)
                    {
                        var matchit = Regex.Match(memberAttribute.Value, "IsPrimaryKey[ \t]*=[ \t]*(?<IsPrimary>(true|false))");

                        if (matchit.Success)
                            if (string.Equals(matchit.Groups["IsPrimary"].Value, "true", StringComparison.OrdinalIgnoreCase))
                                dbColumn.IsPrimaryKey = true;

                        matchit = Regex.Match(memberAttribute.Value, "IsIdentity[ \t]*=[ \t]*(?<IsIdentity>(true|false))");

                        if (matchit.Success)
                            if (string.Equals(matchit.Groups["IsIdentity"].Value, "true", StringComparison.OrdinalIgnoreCase))
                                dbColumn.IsIdentity = true;

                        matchit = Regex.Match(memberAttribute.Value, "AutoField[ \t]*=[ \t]*(?<AutoField>(true|false))");

                        if (matchit.Success)
                            if (string.Equals(matchit.Groups["AutoField"].Value, "true", StringComparison.OrdinalIgnoreCase))
                                dbColumn.IsComputed = true;

                        matchit = Regex.Match(memberAttribute.Value, "IsIndexed[ \t]*=[ \t]*(?<IsIndexed>(true|false))");

                        if (matchit.Success)
                            if (string.Equals(matchit.Groups["IsIndexed"].Value, "true", StringComparison.OrdinalIgnoreCase))
                                dbColumn.IsIndexed = true;

                        matchit = Regex.Match(memberAttribute.Value, "IsNullable[ \t]*=[ \t]*(?<IsNullable>(true|false))");

                        if (matchit.Success)
                            if (string.Equals(matchit.Groups["IsNullable"].Value, "true", StringComparison.OrdinalIgnoreCase))
                                dbColumn.IsNullable = true;

						matchit = Regex.Match(memberAttribute.Value, "IsFixed[ \t]*=[ \t]*(?<IsFixed>(true|false))");

						if (matchit.Success)
							if (string.Equals(matchit.Groups["IsFixed"].Value, "true", StringComparison.OrdinalIgnoreCase))
								dbColumn.IsFixed = true;

						matchit = Regex.Match(memberAttribute.Value, "IsForeignKey[ \t]*=[ \t]*(?<IsForeignKey>(true|false))");

						if (matchit.Success)
							if (string.Equals(matchit.Groups["IsForeignKey"].Value, "true", StringComparison.OrdinalIgnoreCase))
								dbColumn.IsForeignKey = true;

						matchit = Regex.Match(memberAttribute.Value, "NativeDataType[ \t]*=[ \t]*\"(?<NativeDataType>[_a-zA-Z][_a-zA-Z0-9]*)\"");

                        if (matchit.Success)
                            dbColumn.DBDataType = matchit.Groups["NativeDataType"].Value;

						matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*(?<Length>[0-9]+)");

						if (matchit.Success)
							dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);

						matchit = Regex.Match(memberAttribute.Value, "ForeignTableName[ \t]*=[ \t]*\"(?<ForeignTableName>[_a-zA-Z][_a-zA-Z0-9]*)\"");

						if (matchit.Success)
							dbColumn.ForeignTableName = matchit.Groups["ForeignTableName"].Value;
					}

                    columns.Add(dbColumn);
                }
            }

            return columns.ToArray();
        }

        public static ResourceMap LoadResourceModels(Solution solution, EntityMap entityMap, ProjectFolder resourceModelFolder, DBServerType defaultServerType)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<ResourceModel>();

			var resourceFolderContents = FindProjectFolderContents(solution, resourceModelFolder);

			foreach (ProjectItem projectItem in resourceFolderContents)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null)
								{
									var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

									var entityName = "Unknown";
									if (match.Success)
										entityName = match.Groups["entityType"].Value.ToString();

									var entityModel = entityMap.Maps.FirstOrDefault(e =>
										string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = entityModel.ServerType,
										EntityModel = entityModel,
										ResourceType = ResourceType.Class,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();
									var foreignKeyColumns = entityModel.Columns.Where(c => c.IsForeignKey);

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;
											var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

											var dbColumn = new DBColumn
											{
												ColumnName = property.Name,
												ModelDataType = parts[parts.Count()-1],
												IsPrimaryKey = string.Equals(property.Name, "href", StringComparison.OrdinalIgnoreCase)
											};

											var fk = foreignKeyColumns.FirstOrDefault(c => 
											{
												var nn = new NameNormalizer(c.ForeignTableName);
												return string.Equals(nn.SingleForm, dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase);
											});

											if ( fk != null )
                                            {
												dbColumn.IsForeignKey = true;
												dbColumn.ForeignTableName = fk.ForeignTableName;
                                            }

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();
									map.Add(resourceModel);
								}
                                else
                                {
									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = defaultServerType,
										EntityModel = null,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();
									var functions = new List<CodeFunction2>();

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;
											var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

											if (property.Access == vsCMAccess.vsCMAccessPublic || property.Access == vsCMAccess.vsCMAccessProtected)
											{
												var dbColumn = new DBColumn
												{
													ColumnName = property.Name,
													ModelDataType = parts[parts.Count() - 1],
												};

												columns.Add(dbColumn);
											}
										}
										else if ( memberElement.Kind == vsCMElement.vsCMElementFunction)
                                        {
											CodeFunction2 function = (CodeFunction2)memberElement;
											functions.Add(function);
										}
									}

									resourceModel.Columns = columns.ToArray();
									resourceModel.Functions = functions.ToArray();
									map.Add(resourceModel);
								}
							}
							else if (childElement.Kind == vsCMElement.vsCMElementEnum)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null)
								{
									var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

									var entityName = "Unknown";
									if (match.Success)
										entityName = match.Groups["entityType"].Value.ToString();

									var entityModel = entityMap.Maps.FirstOrDefault(e =>
										string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = entityModel.ServerType,
										EntityModel = entityModel,
										ResourceType = ResourceType.Enum,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement enumElement in childElement.Children)
									{
										if (enumElement.Kind == vsCMElement.vsCMElementVariable)
										{
											var dbColumn = new DBColumn
											{
												ColumnName = enumElement.Name,
											};

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();

									map.Add(resourceModel);
								}
								else
                                {
									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = defaultServerType,
										EntityModel = null,
										ProjectName = resourceModelFolder.ProjectName,
										ResourceType = ResourceType.Enum,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement enumElement in childElement.Children)
									{
										if (enumElement.Kind == vsCMElement.vsCMElementVariable)
										{
											var dbColumn = new DBColumn
											{
												ColumnName = enumElement.Name,
											};

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();

									map.Add(resourceModel);
								}
							}
						}
					}
				}
			}

			return new ResourceMap() { Maps = map.ToArray() };
		}

		public static ProjectMapping LoadProjectMapping(DTE2 _appObject, ProjectMapping projectMapping, ProjectFolder installationFolder, out ProjectFolder entityModelsFolder, out ProjectFolder resourceModelsFolder, out ProjectFolder mappingFolder, out ProjectFolder validationFolder, out ProjectFolder exampleFolder, out ProjectFolder controllersFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (projectMapping == null)
			{
				entityModelsFolder = FindEntityModelsFolder(_appObject.Solution);

				if (entityModelsFolder == null)
					entityModelsFolder = installationFolder;

				resourceModelsFolder = FindResourceModelsFolder(_appObject.Solution);

				if (resourceModelsFolder == null)
					resourceModelsFolder = installationFolder;

				mappingFolder = FindMappingFolder(_appObject.Solution);

				if (mappingFolder == null)
					mappingFolder = installationFolder;

				validationFolder = FindValidationFolder(_appObject.Solution);

				if (validationFolder == null)
					validationFolder = installationFolder;

				exampleFolder = FindExampleFolder(_appObject.Solution);

				if (exampleFolder == null)
					exampleFolder = installationFolder;

				controllersFolder = FindControllersFolder(_appObject.Solution);

				if (controllersFolder == null)
					controllersFolder = installationFolder;

				projectMapping = new ProjectMapping
				{
					EntityFolder = entityModelsFolder.Folder,
					EntityNamespace = entityModelsFolder.Namespace,
					EntityProject = entityModelsFolder.ProjectName,
					ResourceFolder = resourceModelsFolder.Folder,
					ResourceNamespace = resourceModelsFolder.Namespace,
					ResourceProject = resourceModelsFolder.ProjectName,
					MappingFolder = mappingFolder.Folder,
					MappingNamespace = mappingFolder.Namespace,
					MappingProject = mappingFolder.ProjectName,
					ValidationFolder = validationFolder.Folder,
					ValidationNamespace = validationFolder.Namespace,
					ValidationProject = validationFolder.ProjectName,
					ExampleFolder = exampleFolder.Folder,
					ExampleNamespace = exampleFolder.Namespace,
					ExampleProject = exampleFolder.ProjectName,
					ControllersProject = controllersFolder.ProjectName,
					ControllersNamespace = controllersFolder.Namespace,
					ControllersFolder = controllersFolder.Folder,
					IncludeSDK = !string.Equals(entityModelsFolder.ProjectName, resourceModelsFolder.ProjectName, StringComparison.Ordinal)
				};

				SaveProjectMapping(_appObject.Solution, projectMapping);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(projectMapping.EntityProject) ||
					string.IsNullOrWhiteSpace(projectMapping.EntityNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.EntityFolder))
				{
					entityModelsFolder = FindEntityModelsFolder(_appObject.Solution);

					if (entityModelsFolder == null)
						entityModelsFolder = installationFolder;

					projectMapping.EntityFolder = entityModelsFolder.Folder;
					projectMapping.EntityNamespace = entityModelsFolder.Namespace;
					projectMapping.EntityProject = entityModelsFolder.ProjectName;

					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					entityModelsFolder = new ProjectFolder
					{
						Folder = projectMapping.EntityFolder,
						Namespace = projectMapping.EntityNamespace,
						ProjectName = projectMapping.EntityProject,
						Name = Path.GetFileName(projectMapping.EntityFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.ResourceProject) ||
					string.IsNullOrWhiteSpace(projectMapping.ResourceNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.ResourceFolder))
				{
					resourceModelsFolder = StandardUtils.FindResourceModelsFolder(_appObject.Solution);

					if (resourceModelsFolder == null)
						resourceModelsFolder = installationFolder;

					projectMapping.ResourceFolder = resourceModelsFolder.Folder;
					projectMapping.ResourceNamespace = resourceModelsFolder.Namespace;
					projectMapping.ResourceProject = resourceModelsFolder.ProjectName;

					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					resourceModelsFolder = new ProjectFolder
					{
						Folder = projectMapping.ResourceFolder,
						Namespace = projectMapping.ResourceNamespace,
						ProjectName = projectMapping.ResourceProject,
						Name = Path.GetFileName(projectMapping.ResourceFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.MappingProject) ||
					string.IsNullOrWhiteSpace(projectMapping.MappingNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.MappingFolder))
				{
					mappingFolder = StandardUtils.FindMappingFolder(_appObject.Solution);

					if (mappingFolder == null)
						mappingFolder = installationFolder;

					projectMapping.MappingProject = mappingFolder.ProjectName;
					projectMapping.MappingNamespace = mappingFolder.Namespace;
					projectMapping.MappingFolder = mappingFolder.Folder;
					
					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					mappingFolder = new ProjectFolder
					{
						Folder = projectMapping.MappingFolder,
						Namespace = projectMapping.MappingNamespace,
						ProjectName = projectMapping.MappingProject,
						Name = Path.GetFileName(projectMapping.MappingFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.ValidationProject) ||
					string.IsNullOrWhiteSpace(projectMapping.ValidationNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.ValidationFolder))
				{
					validationFolder = FindValidationFolder(_appObject.Solution);

					if (validationFolder == null)
						validationFolder = installationFolder;

					projectMapping.ValidationProject = validationFolder.ProjectName;
					projectMapping.ValidationNamespace = validationFolder.Namespace;
					projectMapping.ValidationFolder = validationFolder.Folder;

					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					validationFolder = new ProjectFolder
					{
						Folder = projectMapping.ValidationFolder,
						Namespace = projectMapping.ValidationNamespace,
						ProjectName = projectMapping.ValidationProject,
						Name = Path.GetFileName(projectMapping.MappingFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.ExampleProject) ||
					string.IsNullOrWhiteSpace(projectMapping.ExampleNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.ExampleFolder))
				{
					exampleFolder = FindExampleFolder(_appObject.Solution);

					if (exampleFolder == null)
						exampleFolder = installationFolder;

					projectMapping.ExampleProject = exampleFolder.ProjectName;
					projectMapping.ExampleNamespace = exampleFolder.Namespace;
					projectMapping.ExampleFolder = exampleFolder.Folder;

					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					exampleFolder = new ProjectFolder
					{
						Folder = projectMapping.ExampleFolder,
						Namespace = projectMapping.ExampleNamespace,
						ProjectName = projectMapping.ExampleProject,
						Name = Path.GetFileName(projectMapping.ExampleFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.ControllersProject) ||
					string.IsNullOrWhiteSpace(projectMapping.ControllersNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.ControllersFolder))
				{
					controllersFolder = FindControllersFolder(_appObject.Solution);

					if (controllersFolder == null)
						controllersFolder = installationFolder;

					projectMapping.ControllersProject = controllersFolder.ProjectName;
					projectMapping.ControllersNamespace = controllersFolder.Namespace;
					projectMapping.ControllersFolder = controllersFolder.Folder;

					SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					controllersFolder = new ProjectFolder
					{
						Folder = projectMapping.ControllersFolder,
						Namespace = projectMapping.ControllersNamespace,
						ProjectName = projectMapping.ControllersProject,
						Name = Path.GetFileName(projectMapping.MappingFolder)
					};
				}
			}

			return projectMapping;
		}

		#endregion

		#region Helper Functions
		private static ProjectItems FindProjectFolderContents(Solution solution, ProjectFolder projectFolder)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			Project project = FindProject(solution, projectFolder);

			if (project != null)
			{
				var rootFolder = project.Properties.Item("FullPath").Value.ToString();

				var solutionParts = rootFolder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
				var folderParts = projectFolder.Folder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

				if (solutionParts.Length == folderParts.Length)
					return project.ProjectItems;

				var projectItems = project.ProjectItems;
				ProjectItem folder = null;

				for (int i = solutionParts.Length; i < folderParts.Length; i++)
				{
					foreach (ProjectItem item in projectItems)
					{
						if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
							item.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
						{
							if (string.Equals(item.Name, folderParts[i], StringComparison.OrdinalIgnoreCase))
							{
								folder = item;
								projectItems = item.ProjectItems;
								break;
							}
						}
					}
				}

				return folder.ProjectItems;
			}

			return null;
		}


		/// <summary>
		/// Returns the <see cref="Project"/> that the <see cref="ProjectFolder"/> resides in.
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> to search</param>
		/// <param name="projectFolder">The <see cref="ProjectFolder"/> contained within the <see cref="Project"/></param>
		/// <returns></returns>
		private static Project FindProject(Solution solution, ProjectFolder projectFolder)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach ( Project project in solution.Projects)
            {
				if (string.Equals(project.Name, projectFolder.ProjectName, StringComparison.OrdinalIgnoreCase))
					return project;
            }

			return null;
		}

		/// <summary>
		/// Returns the default server type
		/// </summary>
		/// <returns></returns>
		public static DBServerType GetDefaultServerType(string DefaultConnectionString)
        {
			//	Get the location of the server configuration on disk
			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");

			ServerConfig _serverConfig;

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
				return dbServer.DBType;
			}

			return DBServerType.SQLSERVER;
		}
		#endregion
	}
}
