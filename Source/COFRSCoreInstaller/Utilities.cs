using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public static class Utilities
	{
		public static List<string> LoadPolicies(string solutionFolder)
		{
			var results = new List<string>();

			try
			{
				var configFile = Utilities.FindFile(solutionFolder, "appSettings.json");

				if (string.IsNullOrWhiteSpace(configFile))
					return null;

				using (var stream = new FileStream(configFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
				{
					using (var textReader = new StreamReader(stream))
					{
						using (var reader = new JsonTextReader(textReader))
						{
							var jsonConfig = JObject.Load(reader, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });
							var oAuth2Settings = jsonConfig["OAuth2"].Value<JObject>();
							var policyArray = oAuth2Settings["Policies"].Value<JArray>();

							foreach (var policy in policyArray)
								results.Add(policy["Policy"].Value<string>());
						}
					}
				}
			}
			catch (Exception)
			{
				results = null;
			}

			return results;
		}

		public static List<ClassMember> LoadEntityClassMembers(string entityFileName, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				//	Is this a table annotation?
				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					//	If this was a table annotation, we now know the tablename the entity class was based upon
					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				//	Is this a member annotation?
				else if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					//	If the entity specified a different column name than the member name, remember it.
					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}

				//	Is this a member?
				else if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					//	The following will recoginze these types of data types:
					//	
					//	Simple data types:  int, long, string, Guid, Datetime, etc.
					//	Typed data types:  List<T>, IEnumerable<int>
					//	Embedded Typed Data types: IEnumerable<ValueTuple<string, int>>

					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match2 = Regex.Match(line, pattern);

					if (match2.Success)
					{
						//	Okay, we got a column. Get the member name can call it the entityName.
						entityName = match2.Groups["columnname"].Value;

						//	But if the previous annotation specified a column name, then use that instead
						var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;

						var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

						if (column != null)
						{
							column.EntityName = entityName;

							if (column.IsPrimaryKey)
							{
								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = "Href",
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ServerType = column.ServerType,
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

								SetFixed(column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else if (column.IsForeignKey)
							{
								string shortColumnName;

								if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
								{
									shortColumnName = column.ColumnName;
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
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ServerType = column.ServerType,
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

								SetFixed(column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else
							{
								var normalizer = new NameNormalizer(column.EntityName);
								var domainName = normalizer.PluralForm;

								if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
									domainName = normalizer.SingleForm;

								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									ClassMember potentialMember = null;

									potentialMember = members.FirstOrDefault(m => domainName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, domainName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) : false);

									if (potentialMember != null)
									{
										var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, domainName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

										if (childMember != null)
											member = childMember;
									}
								}

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = domainName,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ServerType = column.ServerType,
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

								SetFixed(column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
						}
					}
				}
			}

			return members;
		}

		public static List<ClassMember> LoadClassColumns(string resourceFileName, string entityFileName, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var resourceContent = File.ReadAllText(resourceFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in resourceContent)
			{
				var match = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_\\<\\>]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");
				if (match.Success)
				{
					var memberName = match.Groups["column"].Value;
					var dataType = match.Groups["datatype"].Value;

					var member = new ClassMember()
					{
						ResourceMemberName = memberName,
						ResourceMemberType = dataType,
						EntityNames = new List<DBColumn>(),
						ChildMembers = new List<ClassMember>()
					};

					LoadChildMembers(Path.GetDirectoryName(resourceFileName), member);

					members.Add(member);
				}
			}

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}

				var match2 = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");

				if (match2.Success)
				{
					entityName = match2.Groups["column"].Value;

					var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;

					var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

					if (column != null)
					{
						column.EntityName = entityName;

						if (column.IsPrimaryKey)
						{
							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "href", StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else if (column.IsForeignKey)
						{
							string shortColumnName;

							if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
							{
								shortColumnName = column.ColumnName;
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
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else
						{
							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, column.EntityName, StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								var potentialMember = members.FirstOrDefault(m => column.EntityName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, column.EntityName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) : false);

								if (potentialMember != null)
								{
									var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, column.EntityName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

									if (childMember != null)
										member = childMember;
								}
							}

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
					}
				}
			}

			return members;
		}

		private static void SetFixed(DBColumn column, DBColumn entityColumn)
		{
			if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
					  (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
			{
				entityColumn.IsFixed = true;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
					  (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char))
			{
				entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
			{
				entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
			{
				entityColumn.IsFixed = true;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.MediumText) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.LongText) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.TinyText))
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
			{
				entityColumn.IsFixed = false;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
			{
				entityColumn.IsFixed = true;
			}
		}

		private static void LoadChildMembers(string folder, ClassMember member)
		{
			string memberProperName = string.Empty;

			if (member.ResourceMemberType.Contains("<"))
				return;

			if (member.ResourceMemberType.Contains(">"))
				return;

			if (member.ResourceMemberType.EndsWith("?"))
				memberProperName = member.ResourceMemberType.Substring(0, member.ResourceMemberType.Length-1);
			else
				memberProperName = member.ResourceMemberType;

			var fileName = FindFile(folder, memberProperName + ".cs");

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				var childContent = File.ReadAllText(fileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in childContent)
				{
					var match = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");
					if (match.Success)
					{
						var memberName = match.Groups["column"].Value;
						var dataType = match.Groups["datatype"].Value;

						var childMember = new ClassMember()
						{
							ResourceMemberName = memberName,
							ResourceMemberType = dataType,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						LoadChildMembers(folder, childMember);

						member.ChildMembers.Add(childMember);
					}
				}
			}
		}

		private static string FindFile(string folder, string fileName)
		{
			var fullPath = Path.Combine(folder, fileName);

			if (File.Exists(fullPath))
				return fullPath;

			foreach (var subfolder in Directory.GetDirectories(folder))
			{
				var childName = FindFile(subfolder, fileName);

				if (!string.IsNullOrWhiteSpace(childName))
					return childName;
			}

			return string.Empty;
		}

		public static void LoadClassList(string SolutionFolder, string resourceClass, 
											ref ResourceClassFile Orchestrator, 
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				foreach (var file in Directory.GetFiles(SolutionFolder, "*.cs"))
				{
					LoadDomainClass(file, resourceClass, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}

				foreach (var folder in Directory.GetDirectories(SolutionFolder))
				{
					LoadDomainList(folder, resourceClass, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static void LoadDomainList(string folder, string DomainClassName, ref ResourceClassFile Orchestrator,
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadDomainList(subfolder, DomainClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}


		private static void LoadDomainClass(string file, string resourceClassName, ref ResourceClassFile Orchestrator,
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
				var className = string.Empty;
				var baseClassName = string.Empty;
				var namespaceName = string.Empty;

				foreach (var line in data)
				{
					var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)([ \t]*\\:[ \t]*(?<baseClass>[A-Za-z][A-Za-z0-9_\\<\\>]*))*");

					if (match.Success)
					{
						className = match.Groups["className"].Value;
						baseClassName = match.Groups["baseClass"].Value;
					}

					match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*)");

					if (match.Success)
						namespaceName = match.Groups["namespaceName"].Value;

					if (!string.IsNullOrWhiteSpace(className) &&
						!string.IsNullOrWhiteSpace(namespaceName))
					{
						var classfile = new ResourceClassFile
						{
							ClassName = $"{className}",
							FileName = file,
							EntityClass = string.Empty,
							ClassNamespace = namespaceName
						};

						if (string.Equals(classfile.ClassName, "ServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
							Orchestrator = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"Validator<{resourceClassName}>", StringComparison.OrdinalIgnoreCase))
							ValidatorClass = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"IExamplesProvider<{resourceClassName}>", StringComparison.OrdinalIgnoreCase))
							ExampleClass = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"IExamplesProvider<RqlCollection<{resourceClassName}>>", StringComparison.OrdinalIgnoreCase))
							CollectionExampleClass = classfile;
					}

					className = string.Empty;
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static string GetRootNamespace(string SolutionFolder)
		{
			string fullPath = Path.Combine(SolutionFolder, "Startup.cs");

			if (File.Exists(fullPath))
			{
				using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (var reader = new StreamReader(stream))
					{
						while (!reader.EndOfStream)
						{
							var line = reader.ReadLine();

							var match = Regex.Match(line, "[ \t]*namespace[ \t]+(?<namespace>[a-zA-Z_][a-zA-Z0-9_\\.]*)");

							if (match.Success)
							{
								return match.Groups["namespace"].Value;
							}
						}
					}
				}
			}
			else
			{
				var subFolders = Directory.GetDirectories(SolutionFolder);

				foreach (var subFolder in subFolders)
				{
					var ns = GetRootNamespace(subFolder);

					if (!string.IsNullOrWhiteSpace(ns))
						return ns;
				}
			}

			return string.Empty;
		}

		public static void ReplaceConnectionString(string connectionString, Dictionary<string, string> replacementsDictionary)
		{
			//	The first thing we need to do, is we need to load the appSettings.local.json file
			var fileName = GetLocalFileName("appsettings.local.json", replacementsDictionary["$solutiondirectory$"]);
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

		private static string GetLocalFileName(string fileName, string rootFolder)
		{
			var files = Directory.GetFiles(rootFolder);

			foreach (var file in files)
			{
				if (file.ToLower().Contains(fileName))
					return file;
			}

			var childFolders = Directory.GetDirectories(rootFolder);

			foreach (var childFolder in childFolders)
			{
				var theFile = GetLocalFileName(fileName, childFolder);

				if (!string.IsNullOrWhiteSpace(theFile))
					return theFile;
			}


			return string.Empty;
		}

		public static void RegisterEnum(string solutionFolder, EntityClassFile classFile)
		{
			var fileName = FindFile(solutionFolder, "ServiceConfig.cs");
			var content = File.ReadAllText(fileName, Encoding.UTF8);


			if (!content.Contains($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");"))
			{
				var lines = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);

				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				{
					using (var writer = new StreamWriter(stream))
					{
						if (!content.Contains("Npgsql"))
							writer.WriteLine("using Npgsql;");

						if (!content.Contains(classFile.ClassNameSpace))
							writer.WriteLine($"using {classFile.ClassNameSpace};");

						bool insertLines = false;

						foreach (var line in lines)
						{
							if (line.Contains("AutoMapperFactory.CreateMapper();"))
							{
								insertLines = true;
							}

							writer.WriteLine(line);

							if ( insertLines )
                            {
								writer.WriteLine($"\t\t\tNpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");");
								insertLines = false;
                            }
						}
					}
				}
			}
		}

		public static void RegisterComposite(string solutionFolder, EntityClassFile classFile)
		{
			var fileName = FindFile(solutionFolder, "ServiceConfig.cs");
			var content = File.ReadAllText(fileName, Encoding.UTF8);

			if (!content.Contains($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");"))
			{
				var lines = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);

				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
				{
					using (var writer = new StreamWriter(stream))
					{
						if (!content.Contains("Npgsql"))
							writer.WriteLine("using Npgsql;");

						if (!content.Contains(classFile.ClassNameSpace))
							writer.WriteLine($"using {classFile.ClassNameSpace};");

						bool insertLines = false;

						foreach (var line in lines)
						{
							if (line.Contains("AutoMapperFactory.CreateMapper();"))
							{
								insertLines = true;
							}

							writer.WriteLine(line);

							if (insertLines)
							{
								writer.WriteLine($"\t\t\tNpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");");
								insertLines = false;
							}
						}
					}
				}
			}
		}

		public static List<ResourceClassFile> LoadResourceClassList(string folder)
        {
			var theList = new List<ResourceClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadResourceClass(file);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadResourceClassList(childFolder));
			}

			return theList;
		}

		public static ResourceClassFile LoadResourceClass(string file)
        {
			var content = File.ReadAllText(file);
			var namespaceName = string.Empty;
			var entityClass = string.Empty;

			if (content.Contains("[Entity"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[Entity"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[Entity[ \t]*\\([ \t]*typeof\\((?<entityClass>[A-Za-z][A-Za-z0-9_]*[ \t]*)\\)[ \t]*\\)[ \t]*\\]");

						if (match.Success)
						{
							entityClass = match.Groups["entityClass"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new ResourceClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								EntityClass = entityClass,
  							    ClassNamespace = namespaceName
							};

							return classfile;
						}
					}
				}
			}

			return null;
		}

		public static List<EntityClassFile> LoadEntityClassList(string folder)
        {
			var theList = new List<EntityClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadEntityClass(file);

				if ( classFile != null )
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadEntityClassList(childFolder));
			}

			return theList;
		}

		private static EntityClassFile LoadEntityClass(string file)
		{ 
			var content = File.ReadAllText(file);
			var namespaceName = string.Empty;
			var schemaName = string.Empty;
			var tableName = string.Empty;

			if (content.Contains("[Table"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[Table"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Table
							};

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgEnum"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgEnum"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgEnum[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("enum"))
					{
						var match = Regex.Match(line, "enum[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Enum
							};

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgComposite"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgComposite"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Composite
							};

							return classfile;
						}
					}
				}
			}

			return null;
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
				//	display_name

				normalizedName.Append(className.Substring(indexStart, index - indexStart));
				normalizedName.Append(className.Substring(index - indexStart + 1, 1).ToUpper());
				indexStart = index - indexStart + 3;

				index = className.IndexOf("_", indexStart);
			}

			normalizedName.Append(className.Substring(indexStart));
			return normalizedName.ToString();
		}
	}
}