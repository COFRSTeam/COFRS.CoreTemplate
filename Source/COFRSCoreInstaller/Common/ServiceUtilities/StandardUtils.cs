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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using VSLangProj;

namespace COFRS.Template.Common.ServiceUtilities
{
	public static class StandardUtils
	{
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

		public static void EnsureFolder(Solution solution, ProjectFolder projectFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//var folderParts = folderName.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
			//var project = solution.Projects.Item(1);
			//string rootFolder = string.Empty;

			//foreach (Property property in project.Properties)
			//{
			//	if (string.Equals(property.Name, "FullPath", StringComparison.OrdinalIgnoreCase))
			//		rootFolder = property.Value.ToString();
			//}

			//var parentFolder = FindFolder(project, folderParts[0]);

			//if (parentFolder == null)
			//{
			//	try
			//	{
			//		parentFolder = project.ProjectItems.AddFolder(folderParts[0]);
			//	}
			//	catch (Exception)
			//	{
			//		rootFolder = Path.Combine(rootFolder, folderParts[0]);
			//		parentFolder = project.ProjectItems.AddFromDirectory(rootFolder);
			//	}
			//}

			//for (int i = 1; i < folderParts.Length; i++)
			//{
			//	var childFolder = FindFolder(parentFolder, folderParts[i]);

			//	if (childFolder == null)
			//	{
			//		try
			//		{
			//			childFolder = parentFolder.ProjectItems.AddFolder(folderParts[i]);
			//		}
			//		catch (Exception)
			//		{
			//			rootFolder = Path.Combine(rootFolder, folderParts[i]);
			//			childFolder = project.ProjectItems.AddFromDirectory(rootFolder);
			//		}
			//	}

			//	parentFolder = childFolder;
			//}
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

		public static string FindValidatorNamespace(Solution solution, ResourceClassFile resourceClass, EntityClassFile entityClass, out string validatorInterface)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			validatorInterface = string.Empty;

			var candidate = $"{resourceClass.ClassName}Validator.cs";
			var projectItem = solution.FindProjectItem(candidate);

			if (projectItem == null)
			{
				foreach (Project project in solution.Projects)
				{
					projectItem = FindValidator(project.ProjectItems, resourceClass, entityClass);
				}
			}

			if (projectItem != null)
			{
				var code = projectItem.FileCodeModel;

				foreach (CodeNamespace namespaceElement in code.CodeElements.GetTypes<CodeNamespace>())
				{
					foreach (CodeClass classElement in namespaceElement.Children.GetTypes<CodeClass>())
					{
						foreach (CodeClass baseClass in classElement.Bases.GetTypes<CodeClass>())
						{
							if (baseClass.FullName.Contains(resourceClass.ClassName))
							{
								foreach (CodeInterface childCandidate in classElement.ImplementedInterfaces.GetTypes<CodeInterface>())
								{
									validatorInterface = childCandidate.Name;
								}

								return namespaceElement.Name;
							}
						}
					}
				}
			}

			return "none";
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
						if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
						{
							if (UndefinedClassList.FirstOrDefault(c => string.Equals(c.TableName, column.EntityName, StringComparison.OrdinalIgnoreCase)) == null)
							{
								var aList = new List<EntityModel>();
								var bList = new List<EntityModel>();
								var className = $"E{StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.ColumnName))}";

								var elementType = DBHelper.GetElementType(classFile.SchemaName, column.dbDataType, entityMap, connectionString);

								var aClassFile = new EntityModel()
								{
									ClassName = className,
									TableName = column.dbDataType,
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

		public static void GenerateColumns(EntityModel classFile, string connectionString)
		{
			var columns = new List<DBColumn>();

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
					command.Parameters.AddWithValue("@schema", classFile.SchemaName);
					command.Parameters.AddWithValue("@tablename", classFile.TableName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var dbColumn = new DBColumn
							{
								EntityName = reader.GetString(0),
								ColumnName = CorrectForReservedNames(NormalizeClassName(reader.GetString(0))),
								DataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1)),
								dbDataType = reader.GetString(1),
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

			classFile.Columns = columns.ToArray();
		}

		public static bool EqualPaths(string patha, string pathb)
        {
			var a = Path.GetFullPath(patha).Replace('/', Path.DirectorySeparatorChar); 
			var b = Path.GetFullPath(pathb).Replace('/', Path.DirectorySeparatorChar); 

			if (a.EndsWith(Path.DirectorySeparatorChar.ToString()))
				a = a.Substring(0, a.Length - 1);

			if (b.EndsWith(Path.DirectorySeparatorChar.ToString()))
				b = b.Substring(0, b.Length - 1);

			return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
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


		/// <summary>
		/// Loads all the entity models in a solution
		/// </summary>
		/// <param name="solution">The open solution</param>
		/// <returns>The list of <see cref="MemberInfo"/> objects that describe all members in the solution</returns>
		public static Dictionary<string, MemberInfo> LoadProgramDetail(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>();

			foreach (Project project in solution.Projects)
			{
				if (project.Kind == PrjKind.prjKindCSharpProject)
				{
					foreach (var element in ScanProject(project.ProjectItems))
						members.Add(element.Key, element.Value);
				}
			}

			return members;
		}

		/// <summary>
		/// Loads all the entity models in a solution
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static List<EntityModel> LoadClassList(Dictionary<string, MemberInfo> members)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var entityMap = new EntityMap { Maps = new EntityModel[] { } };

			foreach (var item in members)
			{
				if (item.Value.ElementType == ElementType.Composite ||
					 item.Value.ElementType == ElementType.Enum)
				{
					var classList = entityMap.Maps.ToList();
					classList.AddRange(LoadClassFile(item.Value, entityMap, members));
					entityMap.Maps = classList.ToArray();
				}
			}

			foreach (var item in members)
			{
				if (item.Value.ElementType == ElementType.Table)
				{
					var classList = entityMap.Maps.ToList();
					classList.AddRange(LoadClassFile(item.Value, entityMap, members));
					entityMap.Maps = classList.ToArray();
				}
			}

			foreach (var item in members)
			{
				if (item.Value.ElementType == ElementType.Resource)
				{
					var classList = entityMap.Maps.ToList();
					classList.AddRange(LoadClassFile(item.Value, entityMap, members));
					entityMap.Maps = classList.ToArray();
				}
			}

			return entityMap.Maps.ToList();
		}

		/// <summary>
		/// Loads all the entity models in a project
		/// </summary>
		/// <param name="projectItems"></param>
		/// <returns></returns>
		private static Dictionary<string, MemberInfo> ScanProject(ProjectItems projectItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var results = new Dictionary<string, MemberInfo>();

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					foreach (var element in ScanProject(projectItem.ProjectItems))
						results.Add(element.Key, element.Value);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
						 projectItem.FileCodeModel != null &&
						 projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
						 Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (var element in ScanClassFile(projectItem))
						results.Add(element.Key, element.Value);
				}
			}

			return results;
		}

		private static Dictionary<string, MemberInfo> ScanClassFile(ProjectItem projectItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new Dictionary<string, MemberInfo>();

			foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.GetTypes<CodeNamespace>())
			{
				foreach (CodeElement childElement in namespaceElement.Members)
				{
					if (childElement.Kind == vsCMElement.vsCMElementClass)
					{
						CodeAttribute tableAttribute = null;
						CodeAttribute compositeAttribute = null;
						CodeAttribute entityAttribute = null;

						try { tableAttribute = (CodeAttribute)childElement.Children.Item("Table"); } catch (Exception) { }
						try { compositeAttribute = (CodeAttribute)childElement.Children.Item("PgComposite"); } catch (Exception) { }
						try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

						if (tableAttribute != null)
						{
							var entityName = string.Empty;

							var match = Regex.Match(tableAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

							if (match.Success)
							{
								entityName = match.Groups["tableName"].Value;
							}

							var item = new MemberInfo
							{
								ClassName = childElement.Name,
								EntityName = entityName,
								ElementType = ElementType.Table,
								Namespace = namespaceElement,
								Member = childElement
							};

							results.Add(item.ClassName, item);
						}
						else if (compositeAttribute != null)
						{
							var entityName = string.Empty;
							var match = Regex.Match(compositeAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

							if (match.Success)
							{
								entityName = match.Groups["tableName"].Value;
							}

							var item = new MemberInfo
							{
								ClassName = childElement.Name,
								EntityName = entityName,
								ElementType = ElementType.Composite,
								Namespace = namespaceElement,
								Member = childElement
							};

							results.Add(item.ClassName, item);
						}
						else if (entityAttribute != null)
						{
							var entityName = string.Empty;

							var match = Regex.Match(entityAttribute.Value, "\"(?<entityName>[A-Za-z][A-Za-z0-9_]*)\"");

							if (match.Success)
							{
								entityName = match.Groups["entityName"].Value;
							}

							var item = new MemberInfo
							{
								ClassName = childElement.Name,
								EntityName = entityName,
								ElementType = ElementType.Resource,
								Namespace = namespaceElement,
								Member = childElement
							};

							results.Add(item.ClassName, item);
						}
					}
					else if (childElement.Kind == vsCMElement.vsCMElementEnum)
					{
						CodeAttribute attributeElement = null;

						try { attributeElement = (CodeAttribute)childElement.Children.Item("PgEnum"); } catch (Exception) { }

						if (attributeElement != null)
						{
							var entityName = string.Empty;

							var match = Regex.Match(attributeElement.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

							if (match.Success)
							{
								entityName = match.Groups["tableName"].Value;
							}

							var item = new MemberInfo
							{
								ClassName = childElement.Name,
								EntityName = entityName,
								ElementType = ElementType.Enum,
								Namespace = namespaceElement,
								Member = childElement
							};

							results.Add(item.ClassName, item);
						}
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Load all entity models in a file
		/// </summary>
		/// <param name="projectItem"></param>
		/// <returns></returns>
		private static List<EntityModel> LoadClassFile(MemberInfo member, EntityMap entityMap, Dictionary<string, MemberInfo> members)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<EntityModel>();

			if (member.ElementType == ElementType.Composite)
			{
				try
				{
					var entity = new EntityModel
					{
						ClassName = member.ClassName,
						Namespace = member.Namespace.Name,
						ElementType = member.ElementType,
						Folder = member.Member.ProjectItem.FileNames[0]
					};

					CodeAttribute attribute = (CodeAttribute)member.Member.Children.Item("PgComposite");

					var match = Regex.Match(attribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

					if (match.Success)
					{
						var Columns = new List<DBColumn>();
						entity.ServerType = DBServerType.POSTGRESQL;
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;

						foreach (CodeElement element in member.Member.Children)
						{
							if (element.Kind == vsCMElement.vsCMElementProperty)
							{
								CodeProperty property = (CodeProperty)element;
								CodeAttribute memberAttribute = (CodeAttribute)property.Children.Item("Member");

								var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
								var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

								bool isPrimaryKey = false;
								bool isAutoField = false;
								bool isIdentity = false;
								bool isIndexed = false;
								bool isForeignKey = false;
								bool isNullable = false;
								bool isFixed = false;
								string nativeDataType = string.Empty;
								long dataLength = 0;
								int precision = 0;
								int scale = 0;
								string entityName = string.Empty;

								var match3 = Regex.Match(memberAttribute.Value, "IsPrimaryKey[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isPrimaryKey = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "AutoField[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isAutoField = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsIdentity[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isIdentity = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsIndexed[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isIndexed = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsForeignKey[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isForeignKey = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsNullable[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isNullable = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsFixed[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isFixed = bool.Parse(match3.Groups["boolValue"].Value);

								var whitespace = "[ \\t]*";
								var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
								var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
								var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
								var typedecl = $"{variableName}(({singletype})|({multitype}))*";

								match3 = Regex.Match(memberAttribute.Value, $"NativeDataType[ \t]*=[ \t]*\"(?<nativeType>{typedecl})\"");

								if (match3.Success)
									nativeDataType = match3.Groups["nativeType"].Value;

								match3 = Regex.Match(memberAttribute.Value, $"Length[ \t]*=[ \t]*(?<length>[0-9]+)");

								if (match3.Success)
									dataLength = Convert.ToInt64(match3.Groups["length"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"Precision[ \t]*=[ \t]*(?<precision>[0-9]+)");

								if (match3.Success)
									precision = Convert.ToInt32(match3.Groups["precision"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"Scale[ \t]*=[ \t]*(?<scale>[0-9]+)");

								if (match3.Success)
									scale = Convert.ToInt32(match3.Groups["scale"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"ColumnName[ \t]*=[ \t]*(?<entityName>[_a-zA-Z][_a-zA-Z0-9]*)");

								if (match3.Success)
									entityName = match3.Groups["entityName"].Value;

								var className = property.Name;

								if (string.IsNullOrWhiteSpace(entityName))
									entityName = className;

								try
								{
									CodeAttribute pgNameAttribute = (CodeAttribute)property.Children.Item("PgName");
									var match4 = Regex.Match(pgNameAttribute.Value, "\\\"(?<entityName>[A-Za-z][A-Za-z0-9_]*)\"");

									if (match4.Success)
										entityName = match4.Groups["entityName"].Value;

								}
								catch (Exception) { }

								var entityColumn = new DBColumn()
								{
									ColumnName = className,
									EntityName = entityName,
									EntityType = dataType,
									IsIdentity = isIdentity,
									IsPrimaryKey = isPrimaryKey,
									IsComputed = isAutoField,
									IsIndexed = isIndexed,
									IsForeignKey = isForeignKey,
									IsNullable = isNullable,
									IsFixed = isFixed,
									dbDataType = nativeDataType,
									Length = dataLength,
									NumericPrecision = precision,
									NumericScale = scale,
								};

								if (entity.ServerType == DBServerType.MYSQL)
									entityColumn.DataType = DBHelper.ConvertMySqlDataType(entityColumn.dbDataType);
								else if (entity.ServerType == DBServerType.POSTGRESQL)
									entityColumn.DataType = DBHelper.ConvertPostgresqlDataType(entityColumn.dbDataType);
								else if (entity.ServerType == DBServerType.SQLSERVER)
									entityColumn.DataType = DBHelper.ConvertSqlServerDataType(entityColumn.dbDataType);

								Columns.Add(entityColumn);
							}
						}

						entity.Columns = Columns.ToArray();
					}


					results.Add(entity);
				}
				catch (Exception) { }
			}
			else if (member.ElementType == ElementType.Table)
			{
				try
				{
					var entity = new EntityModel
					{
						ClassName = member.ClassName,
						Namespace = member.Namespace.Name,
						ElementType = member.ElementType,
						Folder = member.Member.ProjectItem.FileNames[0]
					};

					CodeAttribute attribute = (CodeAttribute)member.Member.Children.Item("Table");
					var match = Regex.Match(attribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

					if (match.Success)
					{
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;
						entity.ServerType = (DBServerType)Enum.Parse(typeof(DBServerType), match.Groups["dbtype"].Value);
						var Columns = new List<DBColumn>();

						foreach (CodeElement element in member.Member.Children)
						{
							if (element.Kind == vsCMElement.vsCMElementProperty)
							{
								CodeProperty property = (CodeProperty)element;
								CodeAttribute memberAttribute = (CodeAttribute)property.Children.Item("Member");

								var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
								var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

								bool isPrimaryKey = false;
								bool isAutoField = false;
								bool isIdentity = false;
								bool isIndexed = false;
								bool isForeignKey = false;
								bool isNullable = false;
								bool isFixed = false;
								string nativeDataType = string.Empty;
								long dataLength = 0;
								int precision = 0;
								int scale = 0;
								string entityName = string.Empty;

								var match3 = Regex.Match(memberAttribute.Value, "IsPrimaryKey[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isPrimaryKey = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "AutoField[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isAutoField = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsIdentity[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isIdentity = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsIndexed[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isIndexed = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsForeignKey[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isForeignKey = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsNullable[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isNullable = bool.Parse(match3.Groups["boolValue"].Value);

								match3 = Regex.Match(memberAttribute.Value, "IsFixed[ \t]*=[ \t]*(?<boolValue>true|false)");

								if (match3.Success)
									isFixed = bool.Parse(match3.Groups["boolValue"].Value);

								var whitespace = "[ \\t]*";
								var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
								var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
								var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
								var typedecl = $"{variableName}(({singletype})|({multitype}))*";

								match3 = Regex.Match(memberAttribute.Value, $"NativeDataType[ \t]*=[ \t]*\"(?<nativeType>{typedecl})\"");

								if (match3.Success)
									nativeDataType = match3.Groups["nativeType"].Value;

								match3 = Regex.Match(memberAttribute.Value, $"Length[ \t]*=[ \t]*(?<length>[0-9]+)");

								if (match3.Success)
									dataLength = Convert.ToInt64(match3.Groups["length"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"Precision[ \t]*=[ \t]*(?<precision>[0-9]+)");

								if (match3.Success)
									precision = Convert.ToInt32(match3.Groups["precision"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"Scale[ \t]*=[ \t]*(?<scale>[0-9]+)");

								if (match3.Success)
									scale = Convert.ToInt32(match3.Groups["scale"].Value);

								match3 = Regex.Match(memberAttribute.Value, $"ColumnName[ \t]*=[ \t]*(?<entityName>[_a-zA-Z][_a-zA-Z0-9]*)");

								if (match3.Success)
									entityName = match3.Groups["entityName"].Value;

								var className = property.Name;

								if (string.IsNullOrWhiteSpace(entityName))
									entityName = className;

								var entityColumn = new DBColumn()
								{
									ColumnName = className,
									EntityName = entityName,
									EntityType = dataType,
									IsIdentity = isIdentity,
									IsPrimaryKey = isPrimaryKey,
									IsComputed = isAutoField,
									IsIndexed = isIndexed,
									IsForeignKey = isForeignKey,
									IsNullable = isNullable,
									IsFixed = isFixed,
									dbDataType = nativeDataType,
									Length = dataLength,
									NumericPrecision = precision,
									NumericScale = scale,
								};

								if (entity.ServerType == DBServerType.MYSQL)
									entityColumn.DataType = DBHelper.ConvertMySqlDataType(entityColumn.dbDataType);
								else if (entity.ServerType == DBServerType.POSTGRESQL)
									entityColumn.DataType = DBHelper.ConvertPostgresqlDataType(entityColumn.dbDataType);
								else if (entity.ServerType == DBServerType.SQLSERVER)
									entityColumn.DataType = DBHelper.ConvertSqlServerDataType(entityColumn.dbDataType);

								Columns.Add(entityColumn);
							}
						}

						entity.Columns = Columns.ToArray();
					}

					results.Add(entity);
				}
				catch (Exception) { }
			}
			//else if (member.ElementType == ElementType.Resource)
			//{
			//	try
			//	{
			//		var entity = new EntityModel
			//		{
			//			ClassName = member.ClassName,
			//			Namespace = member.Namespace.Name,
			//			ElementType = member.ElementType,
			//			Folder = member.Member.ProjectItem.FileNames[0]
			//		};

			//		CodeAttribute attribute = (CodeAttribute)member.Member.Children.Item("Entity");
			//		var match = Regex.Match(attribute.Value, "typeof[ \t]*\\([ \t]*(?<entityName>[_A-Za-z][A-Za-z0-9_]*[ \t]*)\\)");

			//		if (match.Success)
			//		{
			//			entity.EntityClass = match.Groups["entityName"].Value;
			//			entity.Members = new List<ClassMember>();

			//			var entityClass = entityMap.Maps.FirstOrDefault(e =>
			//			   string.Equals(e.ClassName, entity.EntityClass, StringComparison.OrdinalIgnoreCase));

			//			if (entityClass != null)
			//			{
			//				foreach (CodeProperty property in member.Member.Children.GetTypes<CodeProperty>())
			//				{
			//					var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			//					var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

			//					var childMember = new ClassMember()
			//					{
			//						ResourceMemberName = property.Name,
			//						ResourceMemberType = dataType,
			//						EntityNames = new List<DBColumn>(),
			//						ChildMembers = new List<ClassMember>()
			//					};

			//					LoadChildMembers(DBServerType.POSTGRESQL, childMember, entityMap);

			//					entity.Members.Add(childMember);
			//				}

			//				var columnName = string.Empty;

			//				foreach (var column in entityClass.Columns)
			//				{
			//					if (column.IsPrimaryKey)
			//					{
			//						var theMember = entity.Members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "href", StringComparison.OrdinalIgnoreCase));

			//						if (theMember == null)
			//						{
			//							theMember = new ClassMember()
			//							{
			//								ResourceMemberName = string.Empty,
			//								ResourceMemberType = string.Empty,
			//								EntityNames = new List<DBColumn>(),
			//								ChildMembers = new List<ClassMember>()
			//							};

			//							entity.Members.Add(theMember);
			//						}

			//						var entityColumn = new DBColumn()
			//						{
			//							EntityName = column.EntityName,
			//							EntityType = column.EntityType,
			//							ColumnName = column.ColumnName,
			//							DataType = column.DataType,
			//							dbDataType = column.dbDataType,
			//							ForeignTableName = column.ForeignTableName,
			//							IsComputed = column.IsComputed,
			//							IsForeignKey = column.IsForeignKey,
			//							IsIdentity = column.IsIdentity,
			//							IsIndexed = column.IsIndexed,
			//							IsNullable = column.IsNullable,
			//							IsPrimaryKey = column.IsPrimaryKey,
			//							IsFixed = column.IsFixed,
			//							Length = column.Length
			//						};

			//						theMember.EntityNames.Add(entityColumn);
			//					}
			//					else if (column.IsForeignKey)
			//					{
			//						string shortColumnName;

			//						if (string.Equals(column.ForeignTableName, entityClass.TableName, StringComparison.OrdinalIgnoreCase))
			//						{
			//							shortColumnName = column.ColumnName;
			//							if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
			//								shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
			//						}
			//						else
			//							shortColumnName = column.ForeignTableName;

			//						var normalizer = new NameNormalizer(shortColumnName);
			//						var resourceName = normalizer.SingleForm;

			//						var theMember = entity.Members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, resourceName, StringComparison.OrdinalIgnoreCase));

			//						if (theMember == null)
			//						{
			//							theMember = new ClassMember()
			//							{
			//								ResourceMemberName = string.Empty,
			//								ResourceMemberType = string.Empty,
			//								EntityNames = new List<DBColumn>(),
			//								ChildMembers = new List<ClassMember>()
			//							};

			//							entity.Members.Add(theMember);
			//						}

			//						var entityColumn = new DBColumn()
			//						{
			//							EntityName = column.EntityName,
			//							EntityType = column.EntityType,
			//							ColumnName = column.ColumnName,
			//							DataType = column.DataType,
			//							dbDataType = column.dbDataType,
			//							ForeignTableName = column.ForeignTableName,
			//							IsComputed = column.IsComputed,
			//							IsForeignKey = column.IsForeignKey,
			//							IsIdentity = column.IsIdentity,
			//							IsIndexed = column.IsIndexed,
			//							IsNullable = column.IsNullable,
			//							IsPrimaryKey = column.IsPrimaryKey,
			//							IsFixed = column.IsFixed,
			//							Length = column.Length
			//						};

			//						theMember.EntityNames.Add(entityColumn);
			//					}
			//					else
			//					{
			//						var theMember = entity.Members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, column.EntityName, StringComparison.OrdinalIgnoreCase));

			//						if (theMember == null)
			//						{
			//							var potentialMember = entity.Members.FirstOrDefault(m => column.EntityName.Length > m.ResourceMemberName.Length && string.Equals(m.ResourceMemberName, column.EntityName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

			//							if (potentialMember != null)
			//							{
			//								var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, column.EntityName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

			//								if (childMember != null)
			//									theMember = childMember;
			//							}
			//						}

			//						if (theMember == null)
			//						{
			//							theMember = new ClassMember()
			//							{
			//								ResourceMemberName = string.Empty,
			//								ResourceMemberType = string.Empty,
			//								EntityNames = new List<DBColumn>(),
			//								ChildMembers = new List<ClassMember>()
			//							};

			//							entity.Members.Add(theMember);
			//						}

			//						var entityColumn = new DBColumn()
			//						{
			//							EntityName = column.EntityName,
			//							EntityType = column.EntityType,
			//							ColumnName = column.ColumnName,
			//							DataType = column.DataType,
			//							dbDataType = column.dbDataType,
			//							ForeignTableName = column.ForeignTableName,
			//							IsComputed = column.IsComputed,
			//							IsForeignKey = column.IsForeignKey,
			//							IsIdentity = column.IsIdentity,
			//							IsFixed = column.IsFixed,
			//							IsIndexed = column.IsIndexed,
			//							IsNullable = column.IsNullable,
			//							IsPrimaryKey = column.IsPrimaryKey,
			//							Length = column.Length
			//						};

			//						theMember.EntityNames.Add(entityColumn);
			//					}
			//				}

			//				results.Add(entity);
			//			}
			//		}
			//	}
			//	catch (Exception) { }
			//}
			else if (member.ElementType == ElementType.Enum)
			{
				try
				{
					var entity = new EntityModel
					{
						ClassName = member.ClassName,
						Namespace = member.Namespace.Name,
						ElementType = member.ElementType,
						Folder = member.Member.ProjectItem.FileNames[0]
					};
					var attribute = (CodeAttribute)member.Member.Children.Item("PgEnum");
					var match = Regex.Match(attribute.Value, "\\\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

					if (match.Success)
					{
						entity.ServerType = DBServerType.POSTGRESQL;
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;
						var Columns = new List<DBColumn>();

						foreach (CodeElement property in member.Member.Children)
						{
							if (property.Kind == vsCMElement.vsCMElementVariable)
							{
								CodeAttribute pgNameAttribute = (CodeAttribute)property.Children.Item("PgName");
								var match2 = Regex.Match(pgNameAttribute.Value, "\\\"(?<entityName>[A-Za-z][A-Za-z0-9_]*)\"");

								if (match2.Success)
								{
									var column = new DBColumn
									{
										ColumnName = property.Name,
										EntityName = match2.Groups["entityName"].Value
									};

									Columns.Add(column);
								}
							}
						}

						entity.Columns = Columns.ToArray();
						results.Add(entity);
					}
				}
				catch (Exception) { }
			}

			return results;
		}

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
					var dataType = "Unknown";

					if (dbType == DBServerType.MYSQL)
						dataType = DBHelper.GetMySqlDataType(column);
					else if (dbType == DBServerType.POSTGRESQL)
						dataType = DBHelper.GetPostgresDataType(column, entityMap);
					else if (dbType == DBServerType.SQLSERVER)
						dataType = DBHelper.GetSQLServerDataType(column);

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

		public static string LoadPolicy(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = solution.FindProjectItem("appSettings.json");

			var window = settingsFile.Open(Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			var lines = sel.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
				if (match.Success)
					return match.Groups["policy"].Value;
			}

			window.Close();

			return string.Empty;
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

		public static void RegisterValidationModel(Solution solution, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem serviceConfig = solution.FindProjectItem("ServicesConfig.cs");
			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);

			bool wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				serviceConfig.Open(Constants.vsViewKindCode);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;
			var activePoint = sel.ActivePoint;

			sel.StartOfDocument();
			var hasValidationUsing = sel.FindText($"using {validationNamespace}");

			if (!hasValidationUsing)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");
				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				sel.NewLine();
				sel.Insert($"using {validationNamespace};");
			}

			if (!sel.FindText($"services.AddScoped<I{validationClass}, {validationClass}>();", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("services.AddSingleton<IApiOptions>(ApiOptions)");
				sel.SelectLine();
				sel.LineDown();
				sel.SelectLine();

				if (sel.Text.Contains("Configure Translation options"))
				{
					sel.LineUp();
					sel.EndOfLine();
					sel.Insert($"\r\n\t\t\t//\tRegister Validators");
					sel.NewLine();
					sel.Insert($"services.AddScoped<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					sel.Insert($"\r\n\t\t\tservices.AddScoped<I{validationClass}, {validationClass}>();");
				}
			}

			doc.Save();

			if (wasOpen)
			{
				sel.MoveToPoint(activePoint);
			}
			else
				window.Close();
		}

		public static void RegisterComposite(Solution solution, EntityModel entityModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (entityModel.ElementType == ElementType.Undefined ||
				entityModel.ElementType == ElementType.Table ||
				entityModel.ElementType == ElementType.Resource)
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
						EntityType = column.EntityType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
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
						EntityType = column.dbDataType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
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
						EntityType = column.EntityType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
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
			if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
					  (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
					  (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char))
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.MediumText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.LongText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.TinyText))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
			{
				entityColumn.IsFixed = false;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
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
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
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
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="parent">A <see cref="ProjectItem"/> folder within the project.</param>
		/// <param name="projectName">The name of the project containing the <see cref="ProjectItem"/> folder.</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
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
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null )
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
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null )
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

		public static EntityMap OpenEntityMap(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\EntityMap.json");

			try
			{
				var jsonData = File.ReadAllText(mappingPath);

				var projectMapping = JsonConvert.DeserializeObject<EntityMap>(jsonData, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore,
					Formatting = Formatting.Indented,
					MissingMemberHandling = MissingMemberHandling.Ignore
				});

				return projectMapping;
			}
			catch (FileNotFoundException)
			{
                var entityMap = new EntityMap
                {
                    Maps = new EntityModel[] { }
                };
                return entityMap;
			}
			catch (DirectoryNotFoundException)
			{
                var entityMap = new EntityMap
                {
                    Maps = new EntityModel[] { }
                };
                return entityMap;
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

		public static void SaveEntityMap(Solution solution, EntityMap projectMapping)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			SaveEntityMap(solutionPath, projectMapping);
		}

		public static void SaveEntityMap(string solutionDirectory, EntityMap projectMapping)
		{
			var mappingPath = Path.Combine(solutionDirectory, ".cofrs\\EntityMap.json");

			var jsonData = JsonConvert.SerializeObject(projectMapping, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				MissingMemberHandling = MissingMemberHandling.Ignore
			});

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
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute tableAttribute = null;
								CodeAttribute compositeAttribute = null;

								try { tableAttribute = (CodeAttribute)childElement.Children.Item("Table"); } catch (Exception) { }
								try { compositeAttribute = (CodeAttribute)childElement.Children.Item("PgComposite"); } catch (Exception) { }

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
										ClassName = childElement.Name,
										ElementType = ElementType.Table,
										Namespace = namespaceElement.Name,
										ServerType = serverType,
										SchemaName = schemaName,
										TableName = entityName,
										ProjectName = entityModelsFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;

											var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
											var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

											CodeAttribute memberAttribute = null;
											try { memberAttribute = (CodeAttribute)memberElement.Children.Item("Member"); } catch (Exception) { }

											var dbColumn = new DBColumn
											{
												ColumnName = property.Name,
												EntityName = property.Name,
												EntityType = dataType
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

												matchit = Regex.Match(memberAttribute.Value, "NativeDataType[ \t]*=[ \t]*\"(?<NativeDataType>[_a-zA-Z][_a-zA-Z0-9]*)\"");

												if (matchit.Success)
													dbColumn.dbDataType = matchit.Groups["NativeDataType"].Value;

												matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*\"(?<Length>[0-9]+)\"");

												if (matchit.Success)
													dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);

												matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*\"(?<Length>[0-9]+)\"");

												if (matchit.Success)
													dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);
											}

											if (entityModel.ServerType == DBServerType.MYSQL)
												dbColumn.DataType = DBHelper.ConvertMySqlDataType(dbColumn.dbDataType);
											else if (entityModel.ServerType == DBServerType.POSTGRESQL)
												dbColumn.DataType = DBHelper.ConvertPostgresqlDataType(dbColumn.dbDataType);
											else if (entityModel.ServerType == DBServerType.SQLSERVER)
												dbColumn.DataType = DBHelper.ConvertSqlServerDataType(dbColumn.dbDataType);

											columns.Add(dbColumn);
										}
									}

									entityModel.Columns = columns.ToArray();
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
										ClassName = childElement.Name,
										ElementType = ElementType.Composite,
										Namespace = namespaceElement.Name,
										ServerType = serverType,
										SchemaName = schemaName,
										TableName = entityName,
										ProjectName = entityModelsFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									map.Add(entityModel);
								}
							}
							else if (childElement.Kind == vsCMElement.vsCMElementEnum)
							{
								CodeAttribute attributeElement = null;

								try { attributeElement = (CodeAttribute)childElement.Children.Item("PgEnum"); } catch (Exception) { }

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
										ClassName = childElement.Name,
										ElementType = ElementType.Enum,
										Namespace = namespaceElement.Name,
										ServerType = serverType,
										SchemaName = schemaName,
										TableName = entityName,
										ProjectName = entityModelsFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement enumElement in childElement.Children)
									{
										if (enumElement.Kind == vsCMElement.vsCMElementVariable)
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
			}

			return new EntityMap() { Maps = map.ToArray() };
		}
		public static ResourceMap LoadResourceModels(Solution solution, EntityMap entityMap, ProjectFolder resourceModelFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<ResourceModel>();

			var entityFolderContents = FindProjectFolderContents(solution, resourceModelFolder);

			foreach (ProjectItem projectItem in entityFolderContents)
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
									DBServerType serverType = DBServerType.SQLSERVER;

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = serverType,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;

											var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
											var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

											CodeAttribute memberAttribute = null;
											try { memberAttribute = (CodeAttribute)memberElement.Children.Item("Member"); } catch (Exception) { }

											var dbColumn = new DBColumn
											{
												ColumnName = property.Name,
												EntityName = property.Name,
												EntityType = dataType
											};

											if (memberAttribute != null)
											{
												var matchit = Regex.Match(memberAttribute.Value, "IsPrimary[ \t]*=[ \t]*(?<IsPrimary>(true|false))");

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

												matchit = Regex.Match(memberAttribute.Value, "NativeDataType[ \t]*=[ \t]*\"(?<NativeDataType>[_a-zA-Z][_a-zA-Z0-9]*)\"");

												if (matchit.Success)
													dbColumn.dbDataType = matchit.Groups["NativeDataType"].Value;

												matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*\"(?<Length>[0-9]+)\"");

												if (matchit.Success)
													dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);

												matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*\"(?<Length>[0-9]+)\"");

												if (matchit.Success)
													dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);
											}

											if (resourceModel.ServerType == DBServerType.MYSQL)
												dbColumn.DataType = DBHelper.ConvertMySqlDataType(dbColumn.dbDataType);
											else if (resourceModel.ServerType == DBServerType.POSTGRESQL)
												dbColumn.DataType = DBHelper.ConvertPostgresqlDataType(dbColumn.dbDataType);
											else if (resourceModel.ServerType == DBServerType.SQLSERVER)
												dbColumn.DataType = DBHelper.ConvertSqlServerDataType(dbColumn.dbDataType);

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();
									map.Add(resourceModel);
								}
							}
							else if (childElement.Kind == vsCMElement.vsCMElementEnum)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null)
								{
									DBServerType serverType = DBServerType.SQLSERVER;

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = serverType,
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
							}
						}
					}
				}
			}

			return new ResourceMap() { Maps = map.ToArray() };
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
		#endregion
	}
}
