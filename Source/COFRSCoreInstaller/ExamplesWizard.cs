using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ExamplesWizard : IWizard
	{
		private bool Proceed = false;

		// This method is called before opening any item that
		// has the OpenInEditor attribute.
		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		// This method is only called for item templates,
		// not for project templates.
		public void ProjectItemFinishedGenerating(ProjectItem
			projectItem)
		{
		}

		// This method is called after the project is created.
		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject,
			Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{
			try
			{
				var form = new UserInputGeneral()
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 4
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					var classFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var domainFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var model = EmitModel(replacementsDictionary["$targetframeworkversion$"], classFile, domainFile, form.DatabaseColumns, form.Examples, replacementsDictionary);
					var collectionmodel = EmitCollectionModel(replacementsDictionary["$targetframeworkversion$"], classFile, domainFile, form.DatabaseColumns, form.Examples);

					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$collectionmodel$", collectionmodel);
					replacementsDictionary.Add("$entitynamespace$", classFile.ClassNameSpace);
					replacementsDictionary.Add("$domainnamespace$", domainFile.ClassNamespace);

					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				Proceed = false;
			}
		}

		// This method is only called for item templates,
		// not for project templates.
		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}

		private string EmitModel(string version, EntityClassFile entityClassFile, ResourceClassFile domainClassFile, List<DBColumn> Columns, JObject Example, Dictionary<string, string> replacementsDictionary)
		{
			replacementsDictionary.Add("$usexml$", "false");

			var results = new StringBuilder();
			var classMembers = Utilities.LoadClassColumns(domainClassFile.FileName, entityClassFile.FileName, Columns);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{domainClassFile.ClassName} Example");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {domainClassFile.ClassName}Example : IExamplesProvider<{domainClassFile.ClassName}>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tGet Example");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<returns>An example of {domainClassFile.ClassName}</returns>");
			results.AppendLine($"\t\tpublic {domainClassFile.ClassName} GetExamples()");
			results.AppendLine("\t\t{");
			results.AppendLine($"\t\t\tvar item = new {entityClassFile.ClassName}");
			results.AppendLine("\t\t\t{");
			var first = true;

			foreach (var member in classMembers)
			{
				first = EmitEntiyMemeberSetting(Columns, Example, results, first, member);
			}

			results.AppendLine();
			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine($"\t\t\treturn AutoMapperFactory.Map<{entityClassFile.ClassName}, {domainClassFile.ClassName}>(item);");

			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private bool EmitEntiyMemeberSetting(List<DBColumn> Columns, JObject Example, StringBuilder results, bool first, ClassMember member)
		{
			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					first = EmitEntiyMemeberSetting(Columns, Example, results, first, childMember);
				}
			}
			else
			{
				foreach (var column in member.EntityNames)
				{
					if (first)
						first = false;
					else
						results.AppendLine(",");

					string value = "Unknown";

					if (column.ServerType == DBServerType.MYSQL)
						value = GetMySqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.POSTGRESQL)
						value = GetPostgresqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.SQLSERVER)
						value = GetSqlServerValue(column.ColumnName, Columns, Example);

					results.Append($"\t\t\t\t{column.EntityName} = {value}");
				}
			}

			return first;
		}

		private string EmitCollectionModel(string version, EntityClassFile entityClassFile, ResourceClassFile domainClassFile, List<DBColumn> Columns, JObject Example)
		{
			var results = new StringBuilder();
			var classMembers = Utilities.LoadClassColumns(domainClassFile.FileName, entityClassFile.FileName, Columns);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{domainClassFile.ClassName} Collection Example");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {domainClassFile.ClassName}CollectionExample : IExamplesProvider<RqlCollection<{domainClassFile.ClassName}>>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tGet Example");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<returns>An example of {domainClassFile.ClassName} collection</returns>");
			results.AppendLine($"\t\tpublic RqlCollection<{domainClassFile.ClassName}> GetExamples()");
			results.AppendLine("\t\t{");
			results.AppendLine($"\t\t\tvar item = new {entityClassFile.ClassName}");
			results.AppendLine("\t\t\t{");
			var first = true;

			foreach (var member in classMembers)
			{
				first = EmitEntiyMemeberSetting(Columns, Example, results, first, member);
			}

			results.AppendLine();
			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine($"\t\t\tvar collection = new RqlCollection<{entityClassFile.ClassName}>()");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tHref = new Uri(\"https://temp.com?limit(10,10)\"),");
			results.AppendLine("\t\t\t\tNext = new Uri(\"https://temp.com?limit(20,10)\"),");
			results.AppendLine("\t\t\t\tFirst = new Uri(\"https://temp.com?limit(1,10)\"),");
			results.AppendLine("\t\t\t\tPrevious = new Uri(\"https://temp.com?limit(1,10)\"),");
			results.AppendLine("\t\t\t\tCount = 2542,");
			results.AppendLine("\t\t\t\tLimit = 10,");
			results.AppendLine($"\t\t\t\tItems = new List<{entityClassFile.ClassName}>() {{ item }}");
			results.AppendLine("\t\t\t};");
			results.AppendLine();
			results.AppendLine($"\t\t\treturn AutoMapperFactory.Map<RqlCollection<{entityClassFile.ClassName}>, RqlCollection<{domainClassFile.ClassName}>>(collection);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private string GetSqlServerValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			switch ((SqlDbType)column.DataType)
			{
				case SqlDbType.Xml:
					if (column.IsNullable)
					{
						if (value.Value<string>() == null)
							return "null";
					}

					return $"\"{value.Value<string>()}\"";

				case SqlDbType.BigInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
								return "null";
						}

						return $"{value.Value<long>()}";
					}

				case SqlDbType.Binary:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.VarBinary:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.Image:
				case SqlDbType.Timestamp:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.Bit:
					{
						if (column.IsNullable)
						{
							if (value.Value<bool?>() == null)
								return "null";
						}

						if (value.Value<bool>())
							return "true";
						else
							return "false";
					}

				case SqlDbType.Char:
				case SqlDbType.NChar:
					{
						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
								return "null";
						}

						if (column.Length == 1)
							return $"'{value.Value<string>()}'";
						else
							return $"\"{value.Value<string>()}\"";
					}

				case SqlDbType.Date:
					if (column.IsNullable)
					{
						if (value.Value<DateTime?>() == null)
							return "null";
					}

					return $"DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()}\")";

				case SqlDbType.DateTime:
				case SqlDbType.DateTime2:
				case SqlDbType.SmallDateTime:
					if (column.IsNullable)
					{
						if (value.Value<DateTime?>() == null)
							return "null";
					}

					return $"DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()} {value.Value<DateTime>().ToShortTimeString()}\")";

				case SqlDbType.DateTimeOffset:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTimeOffset?>() == null)
								return "null";
						}

						var dto = value.Value<DateTimeOffset>();
						var x = dto.ToString("MM/dd/yyyy hh:mm:ss zzz");
						return $"DateTime.Parse(\"{x}\")";
					}

				case SqlDbType.Decimal:
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					{
						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
								return "null";
						}

						return $"{value.Value<decimal>()}m";
					}

				case SqlDbType.Float:
					{
						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
								return "null";
						}

						return value.Value<double>().ToString();
					}

				case SqlDbType.Int:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case SqlDbType.NText:
				case SqlDbType.Text:
				case SqlDbType.NVarChar:
				case SqlDbType.VarChar:
					if (column.IsNullable)
					{
						if (value.Value<string>() == null)
							return "null";
					}

					return $"\"{value.Value<string>()}\"";

				case SqlDbType.Real:
					if (column.IsNullable)
					{
						if (value.Value<float?>() == null)
							return "null";
					}

					return $"{value.Value<float>()}f";

				case SqlDbType.SmallInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
								return "null";
						}

						return $"{value.Value<short>()}";
					}

				case SqlDbType.Time:
					{
						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
								return "null";
						}

						return $"TimeSpan.Parse(\"{value.Value<TimeSpan>()}\")";
					}


				case SqlDbType.TinyInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
								return "null";
						}

						return $"{value.Value<byte>()}";
					}

				case SqlDbType.UniqueIdentifier:
					if (column.IsNullable)
					{
						if (value.Value<Guid?>() == null)
							return "null";
					}

					return $"Guid.Parse(\"{value.Value<Guid>().ToString()}\")";
			}

			return "unknown";
		}
		private string GetMySqlValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			switch ((MySqlDbType)column.DataType)
			{
				case MySqlDbType.Byte:
					{
						if (column.IsNullable)
						{
							if (value.Value<sbyte?>() == null)
								return "null";
						}

						return $"{value.Value<sbyte>()}";
					}

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
				case MySqlDbType.TinyBlob:
				case MySqlDbType.Blob:
				case MySqlDbType.MediumBlob:
				case MySqlDbType.LongBlob:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						var str = Convert.ToBase64String(value.Value<byte[]>());
						return $"Convert.FromBase64String(\"{str}\")";
					}

				case MySqlDbType.Enum:
				case MySqlDbType.Set:
					{
						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.UByte:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
								return "null";
						}

						return $"{value.Value<byte>()}";
					}

				case MySqlDbType.Int16:
					{
						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
								return "null";
						}

						return $"{value.Value<short>()}";
					}

				case MySqlDbType.UInt16:
					{
						if (column.IsNullable)
						{
							if (value.Value<ushort?>() == null)
								return "null";
						}

						return $"{value.Value<ushort>()}";
					}

				case MySqlDbType.Int24:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.UInt24:
					{
						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
								return "null";
						}

						return $"{value.Value<uint>()}";
					}

				case MySqlDbType.Int32:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.UInt32:
					{
						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
								return "null";
						}

						return $"{value.Value<uint>()}";
					}

				case MySqlDbType.Int64:
					{
						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
								return "null";
						}

						return $"{value.Value<long>()}";
					}

				case MySqlDbType.UInt64:
					{
						if (column.IsNullable)
						{
							if (value.Value<ulong?>() == null)
								return "null";
						}

						return $"{value.Value<ulong>()}";
					}

				case MySqlDbType.Decimal:
					{
						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
								return "null";
						}

						return $"{value.Value<decimal>()}m";
					}

				case MySqlDbType.Double:
					{
						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
								return "null";
						}

						return $"{value.Value<double>()}";
					}

				case MySqlDbType.Float:
					{
						if (column.IsNullable)
						{
							if (value.Value<float?>() == null)
								return "null";
						}

						return $"{value.Value<float>()}f";
					}

				case MySqlDbType.String:
					if (column.Length == 1)
					{
						if (column.IsNullable)
						{
							if ( value.Value<char?>() == null )
								return "null";
						}

						return $"'{value.Value<char>()}'";
					}
					else
					{
						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.Text:
				case MySqlDbType.TinyText:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
					{
						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
								return "null";
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
						return $"DateTime.Parse(\"{x}\")";

					}

				case MySqlDbType.Date:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
								return "null";
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
						return $"DateTime.Parse(\"{x}\")";

					}

				case MySqlDbType.Time:
					{
						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
								return "null";
						}

						var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
						return $"TimeSpan.Parse(\"{x}\")";
					}

				case MySqlDbType.Year:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.Bit:
					{
						if (string.Equals(column.dbDataType, "bit(1)", StringComparison.OrdinalIgnoreCase))
						{
							if (column.IsNullable)
							{
								if (value.Value<bool?>() == null)
									return "null";
							}

							return $"{value.Value<bool>().ToString().ToLower()}";
						}
						else
						{
							if (column.IsNullable)
							{
								if (value.Value<ulong?>() == null)
									return "null";
							}

							return $"{value.Value<ulong>()}";
						}
					}

			}

			return "unknown";
		}
		private string GetPostgresqlValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			try
			{
				switch ((NpgsqlDbType)column.DataType)
				{
					case NpgsqlDbType.Smallint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<short>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new short[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<int>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Integer:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<int>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new int[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<int>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Bigint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<long>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new long[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<long>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Real:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<float>()}f";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Real:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new float[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<float>()}f");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Double:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<double>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Double:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new double[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<double>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<decimal>()}m";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
					case NpgsqlDbType.Array | NpgsqlDbType.Money:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new decimal[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<decimal>()}m");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Uuid:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"Guid.Parse(\"{value.Value<Guid>()}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new Guid[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"Guid.Parse(\"{charValue.Value<Guid>()}\")");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Json:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var str = value.Value<string>();
							str = str.Replace("\"", "\\\"");

							return $"\"{str}\"";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Json:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new string[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								var str = charValue.Value<string>();
								str = str.Replace("\"", "\\\"");

								result.Append($"\"{str}\"");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Varbit:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Array && value.Value<JArray>() == null)
									return "null";
								else if (value.Type == JTokenType.String && value.Value<string>() == null)
									return "null";
							}

							var result = new StringBuilder("new bool[] {");
							bool first = true;

							if (value.Type == JTokenType.Array)
							{
								foreach (var c in value.Value<JArray>())
								{
									if (first)
										first = false;
									else
										result.Append(", ");

									result.Append(c.Value<bool>().ToString().ToLower());
								}
							}
							else if (value.Type == JTokenType.String)
							{
								var v = value.Value<string>();
								foreach (var c in v)
								{
									if (first)
										first = false;
									else
										result.Append(", ");

									if (c == '1')
										result.Append("true");
									else
										result.Append("false");
								}
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder();
							var answer = value.Value<JArray>();

							result.Append("new bool[][] {");
							bool firstGroup = true;

							foreach (var group in answer)
							{
								if (firstGroup)
									firstGroup = false;
								else
									result.Append(", ");

								result.Append("new bool[] {");

								bool first = true;

								if (group.Type == JTokenType.Array)
								{
									foreach (var b in group.Value<JArray>())
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										result.Append(b.Value<bool>().ToString().ToLower());
									}
								}
								else if (group.Type == JTokenType.String)
								{
									var v = group.Value<string>();
									foreach (var c in v)
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										if (c == '1')
											result.Append("true");
										else
											result.Append("false");
									}
								}

								result.Append("}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Bit:
						{
							if (column.IsNullable)
							{
								if (column.Length == 1)
								{
									if (value.Value<bool?>() == null)
										return "null";
								}
								else if (value.Type == JTokenType.String && value.Value<string>() == null)
								{
									return "null";
								}
								else if (value.Type == JTokenType.Array && value.Value<JArray>() == null)
								{
									return "null";
								}
							}

							if (column.Length == 1)
							{
								return value.Value<bool>().ToString().ToLower();
							}
							else
							{
								var result = new StringBuilder("new bool[] {");
								bool first = true;

								if (value.Type == JTokenType.Array)
								{
									foreach (var c in value.Value<JArray>())
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										result.Append(c.Value<bool>().ToString().ToLower());
									}
								}
								else if (value.Type == JTokenType.String)
								{
									var v = value.Value<string>();

									foreach (var c in v)
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										if (c == '1')
											result.Append("true");
										else
											result.Append("false");
									}
								}
								
								result.Append("}");
								return result.ToString();
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bit:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder();
							var answer = value.Value<JArray>();

							result.Append("new bool[][] {");
							bool firstGroup = true;

							foreach (var group in answer)
							{
								if (firstGroup)
									firstGroup = false;
								else
									result.Append(", ");

								result.Append("new bool[] {");

								bool first = true;

								if (group.Type == JTokenType.Array)
								{
									foreach (var b in group.Value<JArray>())
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										result.Append(b.Value<bool>().ToString().ToLower());
									}
								}
								else if (group.Type == JTokenType.String)
								{
									var v = group.Value<string>();
									foreach (var c in v)
									{
										if (first)
											first = false;
										else
											result.Append(", ");

										if (c == '1')
											result.Append("true");
										else
											result.Append("false");
									}
								}

								result.Append("}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Bytea:
						{
							if (column.IsNullable)
							{
								if (value.Value<byte[]>() == null)
									return "null";
							}

							return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new byte[][] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"Convert.FromBase64String(\"{Convert.ToBase64String(group.Value<byte[]>())}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Boolean:
						{
							if (column.IsNullable)
							{
								if (value.Value<bool?>() == null)
									return "null";
							}

							if (value.Value<bool>())
								return "true";

							return "false";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new bool[] {");
							bool first = true;

							foreach (var c in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (c.Value<bool>())
									result.Append("true");
								else
									result.Append("false");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Xml:
						{
							if (column.IsNullable)
							{
								if (value.Value<string>() == null)
									return "null";
							}

							return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Text:
					case NpgsqlDbType.Array | NpgsqlDbType.Char:
					case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var answer = new StringBuilder("new string[] {");
							bool first = true;

							foreach (var str in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									answer.Append(", ");

								answer.Append($"\"{str.Value<string>()}\"");
							}

							answer.Append("}");
							return answer.ToString();
						}

					case NpgsqlDbType.Char:
						{
							if (column.IsNullable)
							{
								if (value.Value<string>() == null)
									return "null";
							}

							if (column.Length == 1)
								return $"'{value.Value<string>()}'";
							else
								return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Text:
					case NpgsqlDbType.Varchar:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Date:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
								return $"DateTime.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd");
								return $"DateTime.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Date:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Time:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Interval:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Time:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Interval:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Timestamp:
					case NpgsqlDbType.TimestampTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								return $"DateTime.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								return $"DateTime.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.TimeTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");
								return $"DateTimeOffset.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTimeOffset.Parse(value.Value<string>());
								var x = dt.ToString("HH':'mm':'ss.fffffffK");
								return $"DateTimeOffset.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTimeOffset[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");
									result.Append( $"DateTimeOffset.Parse(\"{x}\")");
								}

								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTimeOffset.Parse(dt.Value<string>());
									var x = dt2.ToString("HH':'mm':'ss.fffffffK");
									result.Append($"DateTimeOffset.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}
				}

				return "unknown";
			}
			catch (Exception error)
			{
				throw error;
			}
		}
	}
}
