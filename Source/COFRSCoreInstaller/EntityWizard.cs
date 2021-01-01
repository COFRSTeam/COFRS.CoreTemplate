using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class EntityWizard : IWizard
	{
		private bool Proceed = false;

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			try
			{
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];

				var namespaceParts = rootNamespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				var filePath = solutionDirectory;

				for (int i = 0; i < namespaceParts.Length; i++)
				{
					if (i == 0)
					{
						var candidate = Path.Combine(filePath, namespaceParts[i]);

						if (Directory.Exists(candidate))
							filePath = candidate;
					}
					else
						filePath = Path.Combine(filePath, namespaceParts[i]);
				}

				if (!Directory.Exists(filePath))
					Directory.CreateDirectory(filePath);

				var form = new UserInputEntity();

				if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					Proceed = true;
					var connectionString = form.ConnectionString;
					ReplaceConnectionString(connectionString, replacementsDictionary);
					var data = EmitObject(form.DatabaseTable, form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$model$", data);

				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}

		private string EmitObject(DBTable table, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			var result = new StringBuilder();
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");

			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{replacementsDictionary["$safeitemname$"]}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(table.Schema))
				result.AppendLine($"\t[Table(\"{table.Table}\")]");
			else
				result.AppendLine($"\t[Table(\"{table.Table}\", Schema = \"{table.Schema}\")]");

			result.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]}");
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

				AppendNullable(result, column.IsNullable, ref first);

				if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
				{
					if (column.Length > 1)
						AppendFixed(result, column.Length, true, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bit) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if ((column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varbit)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Text)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Char)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String))
				{
					//	Insert the column definition
					if (column.ServerType == DBServerType.POSTGRESQL)
					{
						if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}

						if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
					}
					else if (column.ServerType == DBServerType.MYSQL)
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

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
				{
					AppendFixed(result, column.Length, true, ref first);
					AppendAutofield(result, ref first);
				}

				if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Decimal) ||
					(column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal) ||
					(column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Numeric))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, column, ref first);

				if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
					replacementsDictionary["$image$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
					replacementsDictionary["$net$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
					replacementsDictionary["$net$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
					replacementsDictionary["$netinfo$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
					replacementsDictionary["$netinfo$"] = "true";

				//	Correct for reserved words
				CorrectForReservedNames(result, column, ref first);

				result.AppendLine(")]");

				//	Insert the column definition
				if (column.ServerType == DBServerType.POSTGRESQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column)} {column.ColumnName} {{ get; set; }}");
				else if (column.ServerType == DBServerType.MYSQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetMySqlDataType(column)} {column.ColumnName} {{ get; set; }}");
				else if (column.ServerType == DBServerType.SQLSERVER)
					result.AppendLine($"\t\tpublic {DBHelper.GetSQLServerDataType(column)} {column.ColumnName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		private void AppendPrecision(StringBuilder result, int NumericPrecision, int NumericScale, ref bool first)
        {
			AppendComma(result, ref first);

			result.Append($"Precision={NumericPrecision}, Scale={NumericScale}");
		}

		private void AppendDatabaseType(StringBuilder result, DBColumn column, ref bool first)
		{
			AppendComma(result, ref first);

			if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar)
				result.Append("NativeDataType=\"VarChar\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary)
				result.Append("NativeDataType=\"VarBinary\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
				result.Append("NativeDataType=\"char\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal)
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
				if ( isFixed )
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

		private void AppendPrimaryKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsPrimaryKey = true");
		}

		private void AppendIndexed(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIndexed = true");
		}

		private void AppendNullable(StringBuilder result, bool isNullable, ref bool first)
		{
			AppendComma(result, ref first);

			if (isNullable)
				result.Append("IsNullable = true");
			else
				result.Append("IsNullable = false");
		}

		private void AppendIdentity(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIdentity = true, AutoField = true");
		}

		private void AppendComma(StringBuilder result, ref bool first)
		{
			if (first)
				first = false;
			else
				result.Append(", ");
		}

		private void CorrectForReservedNames(StringBuilder result, DBColumn column, ref bool first)
		{
			if ( string.Equals(column.ColumnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "as", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "base", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "bool", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "break", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "byte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "case", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "catch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "char", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "checked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "class", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "const", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "continue", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "default", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "do", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "double", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "else", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "enum", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "event", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "extern", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "false", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "finally", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "float", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "for", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "goto", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "if", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "in", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "int", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "interface", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "internal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "is", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "lock", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "long", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "new", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "null", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "object", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "operator", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "out", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "override", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "params", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "private", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "protected", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "public", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ref", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "return", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "short", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "static", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "string", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "struct", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "switch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "this", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "throw", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "true", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "try", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "uint", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "using", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "void", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "while", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "add", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "alias", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "async", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "await", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "by", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "descending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "equals", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "from", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "get", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "global", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "group", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "into", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "join", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "let", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "on", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "partial", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "remove", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "select", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "set", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "value", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "var", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "when", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "where", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "yield", StringComparison.OrdinalIgnoreCase) )
			{
				AppendComma(result, ref first);
				result.Append($"ColumnName = \"{column.ColumnName}\"");
				column.ColumnName += "_Value";
			} 
		}


		private void ReplaceConnectionString(string connectionString, Dictionary<string, string> replacementsDictionary)
		{
			//	The first thing we need to do, is we need to load the appSettings.local.json file
			var fileName = GetLocalFileName(replacementsDictionary["$solutiondirectory$"]);
			string content;

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				using (var reader = new StreamReader(stream))
				{
					content = reader.ReadToEnd();
				}
			}

			var appSettings = JObject.Parse(content);
			var connectionStrings = appSettings.Value<JObject>("ConnectionStrings");

			if (string.Equals(connectionStrings.Value<string>("DefaultConnection"), "Server=developmentdb;Database=master;Trusted_Connection=True;", StringComparison.OrdinalIgnoreCase))
			{
				connectionStrings["DefaultConnection"] = connectionString;

				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				{
					using (var writer = new StreamWriter(stream))
					{
						writer.Write(appSettings.ToString());
						writer.Flush();
					}
				}
			}
		}

		private string GetLocalFileName(string rootFolder)
		{
			var files = Directory.GetFiles(rootFolder);

			foreach (var file in files)
			{
				if (file.ToLower().Contains("appsettings.local.json"))
					return file;
			}

			var childFolders = Directory.GetDirectories(rootFolder);

			foreach (var childFolder in childFolders)
			{
				var theFile = GetLocalFileName(childFolder);

				if (!string.IsNullOrWhiteSpace(theFile))
					return theFile;
			}


			return string.Empty;
		}
	}
}
