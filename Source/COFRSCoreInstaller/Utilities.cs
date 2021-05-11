using EnvDTE;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VSLangProj;

namespace COFRSCoreInstaller
{
	public static class Utilities
	{
		#region Solution functions

		public static List<EntityDetailClassFile> LoadEntityDetailClassList(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var classList = new List<EntityDetailClassFile>();

			foreach (Project project in solution.Projects)
			{
				if (project.Kind == PrjKind.prjKindCSharpProject)
				{
					classList.AddRange(Utilities.ScanProject(project.ProjectItems));
				}
			}

			return classList;
		}

		public static List<string> LoadPolicies(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<string>();
			var appSettings = FindProjectItem(solution, "appSettings.json");

			var wasOpen = appSettings.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				appSettings.Open(Constants.vsViewKindTextView);

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

		public static List<EntityDetailClassFile> ScanProject(ProjectItems projectItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var results = new List<EntityDetailClassFile>();

			foreach (ProjectItem item in projectItems)
			{
				if (item.FileCodeModel != null)
				{
					int buildAction = 0;

					foreach (Property property in item.Properties)
					{
						if (property.Name == "BuildAction")
							buildAction = Convert.ToInt32(property.Value);
					}

					if (item.Name.Contains(".cs") && buildAction == 1)
					{
						var wasOpen = item.IsOpen[Constants.vsViewKindAny];

						if (!wasOpen)
							item.Open(Constants.vsViewKindCode);

						var doc = item.Document;
						var sel = (TextSelection)doc.Selection;

						var anchorPoint = sel.AnchorPoint;
						var activePoint = sel.ActivePoint;

						bool isComposite = false;
						bool isEnum = false;
						bool isEntity = false;

						sel.StartOfDocument();
						isComposite = sel.FindText("[PgComposite");

						if (!isComposite)
						{
							sel.StartOfDocument();
							isEnum = sel.FindText("[PgEnum");

							if (!isEnum)
							{
								sel.StartOfDocument();
								isEntity = sel.FindText("[Table");
							}
						}

						if (!wasOpen)
							doc.Close();
						else
						{
							sel.MoveToPoint(anchorPoint);
							sel.SwapAnchor();
							sel.MoveToPoint(activePoint);
						}

						if (isComposite || isEnum || isEntity)
						{
							var entity = LoadEntityClass(item.FileNames[0]);

							results.Add(entity);
						}
					}
				}
				else
				{
					results.AddRange(ScanProject(item.ProjectItems));
				}
			}

			return results;
		}

		public static bool IsRootNamespace(Solution solution, string candidateNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				var projectNamespace = string.Empty;

				foreach (Property property in project.Properties)
				{
					try
					{
						if (string.Equals(property.Name, "RootNamespace", StringComparison.OrdinalIgnoreCase))
							projectNamespace = property.Value.ToString();
					}
					catch (Exception) { }
				}

				if (string.Equals(candidateNamespace, projectNamespace, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		public static ProjectFolder FindEntityModelsFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var entityModelsFolder = FindProjectFolder(solution, "EntityModels");

			if (entityModelsFolder != null)
				return entityModelsFolder;

			var modelsFolder = FindProjectItem(solution, "Models");

			if (modelsFolder != null)
			{
				modelsFolder.ProjectItems.AddFolder("EntityModels");
				return FindProjectFolder(solution, "EntityModels");
			}

			Project project = solution.Projects.Item(0);

			modelsFolder = project.ProjectItems.AddFolder("Models");
			modelsFolder.ProjectItems.AddFolder("EntityModels");
			return FindProjectFolder(solution, "EntityModels");
		}

		public static ProjectItem FindProjectItem(Solution solution, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
					{
						return projectItem;
					}

					var candidate = FindProjectItem(projectItem, itemName);

					if (candidate != null)
						return candidate;
				}
			}
			return null;
		}

		public static ProjectItem FindProjectItem(ProjectItem parent, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in parent.ProjectItems)
			{
				if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
				{
					return projectItem;
				}

				var candidate = FindProjectItem(projectItem, itemName);

				if (candidate != null)
					return candidate;
			}

			return null;
		}

		public static ProjectFolder FindProjectFolder(Solution solution, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				var projectNamespace = string.Empty;

				foreach (Property property in project.Properties)
				{
					try
					{
						if (string.Equals(property.Name, "RootNamespace", StringComparison.OrdinalIgnoreCase))
							projectNamespace = property.Value.ToString();
					}
					catch (Exception) { }
				}

				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
					{
						var folderNamespace = $"{projectNamespace}.{projectItem.Name}";

						if (string.Equals(projectItem.Name, folderName, StringComparison.OrdinalIgnoreCase))
						{
							var folder = new ProjectFolder { Namespace = folderNamespace, Folder = projectItem.FileNames[0] };
							return folder;
						}

						var candidate = FindProjectFolder(folderNamespace, projectItem, folderName);

						if (candidate != null)
							return candidate;
					}
				}
			}
			return null;
		}

		private static ProjectFolder FindProjectFolder(string projectNamespace, ProjectItem projectItem, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem child in projectItem.ProjectItems)
			{
				if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
				{
					var folderNamespace = $"{projectNamespace}.{child.Name}";

					if (string.Equals(child.Name, folderName, StringComparison.OrdinalIgnoreCase))
					{
						var folder = new ProjectFolder { Namespace = folderNamespace, Folder = child.FileNames[0] };
						return folder;
					}

					var candidate = FindProjectFolder(folderNamespace, child, folderName);

					if (candidate != null)
						return candidate;
				}
			}

			return null;
		}

		public static string LoadPolicy(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

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

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return string.Empty;
		}

		public static string LoadMoniker(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

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

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return moniker;
		}

		public static string GetConnectionString(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var connectionString = string.Empty;

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appsettings.local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.SelectAll();
			var settings = JObject.Parse(sel.Text);
			var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
			connectionString = connectionStrings["DefaultConnection"].Value<string>();

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return connectionString;
		}

		public static void ReplaceConnectionString(Solution solution, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appsettings.local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			if (sel.FindText("Server=developmentdb;Database=master;Trusted_Connection=True;"))
			{
				sel.SelectLine();
				sel.Text = $"\t\t\"DefaultConnection\": \"{connectionString}\"";
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static void RegisterValidationModel(Solution solution, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				serviceConfig.Open(Constants.vsViewKindCode);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

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

			if (!sel.FindText($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("services.InitializeFactories();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddTransientWithParameters<IServiceOrchestrator"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Validators");
					sel.NewLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static void RegisterComposite(Solution solution, EntityDetailClassFile classFile)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			if (classFile.ElementType == ElementType.Undefined || classFile.ElementType == ElementType.Table)
				return;

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				serviceConfig.Open(Constants.vsViewKindCode);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasNpgsql = sel.FindText($"using Npgsql;");

			sel.StartOfDocument();
			var hasClassNamespace = sel.FindText($"using {classFile.ClassNameSpace};");

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
					sel.Insert($"using {classFile.ClassNameSpace};");
				}
			}

			if (!sel.FindText($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");", (int)vsFindOptions.vsFindOptionsFromStart))
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
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");");
					sel.NewLine();
				}
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static ProjectItem GetProjectItem(Solution solution, string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

            if (DBHelper._cache.TryGetValue($"ProjectItem_{name}", out object theItem))
                return (ProjectItem)theItem;

            if (theItem == null)
			{
				foreach (Project project in solution.Projects)
				{
					theItem = GetProjectItem(project.ProjectItems, name);

					if (theItem != null)
					{
						DBHelper._cache.CreateEntry($"ProjectItem_{name}").Value = theItem;
						return (ProjectItem)theItem;
					}
				}
			}

			return null;
		}

		public static ProjectItem GetProjectItem(ProjectItems items, string name)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in items)
			{
				if (string.Equals(projectItem.Name, name, StringComparison.OrdinalIgnoreCase))
					return projectItem;

				var theChildItem = GetProjectItem(projectItem.ProjectItems, name);

				if (theChildItem != null)
					return theChildItem;
			}

			return null;
		}
		#endregion

		public static string LoadBaseFolder(string folder)
		{
			var files = Directory.GetFiles(folder, "*.csproj");

			if (files.Length > 0)
				return folder;

			foreach (var childfolder in Directory.GetDirectories(folder))
			{
				if (!string.IsNullOrWhiteSpace(LoadBaseFolder(childfolder)))
					return childfolder;
			}

			return string.Empty;
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
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
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
				if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					//	The following will recoginze these types of data types:
					//	
					//	Simple data types:  int, long, string, Guid, Datetime, etc.
					//	Typed data types:  List<T>, IEnumerable<int>
					//	Embedded Typed Data types: IEnumerable<ValueTuple<string, int>>

					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match = Regex.Match(line, pattern);

					if (match.Success)
					{
						var memberName = match.Groups["columnname"].Value;
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

				else if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}
				else if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match2 = Regex.Match(line, pattern);

					if (match2.Success)
					{
						entityName = match2.Groups["columnname"].Value;
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
				memberProperName = member.ResourceMemberType.Substring(0, member.ResourceMemberType.Length - 1);
			else
				memberProperName = member.ResourceMemberType;

			var fileName = FindFile(folder, memberProperName + ".cs");

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				var childContent = File.ReadAllText(fileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in childContent)
				{
					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match = Regex.Match(line, pattern);
					if (match.Success)
					{
						var memberName = match.Groups["columnname"].Value;
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

		public static List<EntityDetailClassFile> LoadEntityClassList(string folder)
		{
			var theList = new List<EntityDetailClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadEntityClass(file);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadEntityClassList(childFolder));
			}

			return theList;
		}

		public static List<EntityDetailClassFile> LoadDetailEntityClassList(string folder, string connectionString)
		{
			var theList = new List<EntityDetailClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadDetailEntityClass(file, connectionString);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadDetailEntityClassList(childFolder, connectionString));
			}

			return theList;
		}

		public static List<EntityDetailClassFile> LoadDetailEntityClassList(List<EntityDetailClassFile> UndefinedClassList, List<EntityDetailClassFile> DefinedClassList, string solutionFolder, string connectionString)
		{
			List<EntityDetailClassFile> resultList = new List<EntityDetailClassFile>();

			foreach (var classFile in UndefinedClassList)
			{
				var newClassFile = LoadDetailEntityClass(classFile, connectionString);
				resultList.Add(newClassFile);

				if (newClassFile.ElementType != ElementType.Enum)
				{
					foreach (var column in newClassFile.Columns)
					{
						if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
						{
							if (DefinedClassList.FirstOrDefault(c => string.Equals(c.TableName, column.EntityName, StringComparison.OrdinalIgnoreCase)) == null)
							{
								var aList = new List<EntityDetailClassFile>();
								var bList = new List<EntityDetailClassFile>();

								var aClassFile = new EntityDetailClassFile()
								{
									ClassName = NormalizeClassName(column.EntityName),
									TableName = column.EntityName,
									SchemaName = classFile.SchemaName,
									FileName = Path.Combine(LoadBaseFolder(solutionFolder), $"Models\\EntityModels\\{NormalizeClassName(column.EntityName)}.cs"),
									ClassNameSpace = classFile.ClassNameSpace,
									ElementType = DBHelper.GetElementType(classFile.SchemaName, column.EntityName, DefinedClassList, connectionString)
								};
								aList.Add(aClassFile);
								bList.AddRange(DefinedClassList);
								bList.AddRange(UndefinedClassList);

								resultList.AddRange(LoadDetailEntityClassList(aList, bList, solutionFolder, connectionString));
							}
						}
					}
				}
			}

			return resultList;
		}

		public static EntityDetailClassFile LoadEntityClass(string file)
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
							var classfile = new EntityDetailClassFile
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
							var classfile = new EntityDetailClassFile
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
							var classfile = new EntityDetailClassFile
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

		private static EntityDetailClassFile LoadDetailEntityClass(EntityDetailClassFile classFile, string connectionString)
		{
			classFile.ElementType = DBHelper.GetElementType(classFile.SchemaName, classFile.TableName, null, connectionString);

			if (classFile.ElementType == ElementType.Enum)
				LoadEnumColumns(connectionString, classFile);
			else
				LoadColumns(connectionString, classFile);

			return classFile;
		}

		private static EntityDetailClassFile LoadDetailEntityClass(string file, string ConnectionString)
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
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Table,
							};

							LoadColumns(ConnectionString, classfile);

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
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Enum
							};

							LoadEnumColumns(classfile);

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
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Composite,
							};

							LoadColumns(ConnectionString, classfile);

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

		private static void LoadEnumColumns(EntityDetailClassFile classFile)
		{
			var contents = File.ReadAllText(classFile.FileName);
			classFile.Columns = new List<DBColumn>();
			string entityName = string.Empty;
			bool foundPgName = false;

			var lines = contents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (line.Trim().StartsWith("[PgName", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "\\[PgName[ \\t]*\\([ \\t]*\\\"(?<columnName>[a-zA-Z_][a-zA-Z0-9_]*)\\\"\\)\\]");

					//	If the entity specified a different column name than the member name, remember it.
					if (match.Success)
					{
						entityName = match.Groups["columnName"].Value;
						foundPgName = true;
					}
				}
				else if (foundPgName)
				{
					var match = Regex.Match(line, "[ \\t]*(?<classname>[a-zA-Z_][a-zA-Z0-9_]*)");

					if (match.Success)
					{
						var dbColumn = new DBColumn()
						{
							ColumnName = match.Groups["classname"].Value,
							EntityName = entityName,
							ServerType = DBServerType.POSTGRESQL
						};

						classFile.Columns.Add(dbColumn);

						foundPgName = false;
					}
				}
			}
		}

		private static void LoadEnumColumns(string connectionString, EntityDetailClassFile classFile)
		{
			classFile.Columns = new List<DBColumn>();
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
					command.Parameters.AddWithValue("@dataType", classFile.TableName);
					command.Parameters.AddWithValue("@schema", classFile.SchemaName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var element = reader.GetString(0);
							var elementName = Utilities.NormalizeClassName(element);

							var column = new DBColumn()
							{
								ColumnName = elementName,
								EntityName = element,
								ServerType = DBServerType.POSTGRESQL
							};

							classFile.Columns.Add(column);
						}
					}
				}
			}
		}

		private static void LoadColumns(string connectionString, EntityDetailClassFile classFile)
		{
			if (File.Exists(classFile.FileName))
			{
				var contents = File.ReadAllText(classFile.FileName);
				classFile.Columns = new List<DBColumn>();

				var lines = contents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				var tableName = classFile.TableName;
				var schema = classFile.SchemaName;
				var entityName = string.Empty;

				foreach (var line in lines)
				{
					//	Is this a member annotation?
					if (line.Trim().StartsWith("[PgName", StringComparison.OrdinalIgnoreCase))
					{
						var match = Regex.Match(line, "\\[PgName[ \\t]*\\([ \\t]*\\\"(?<columnName>[a-zA-Z_][a-zA-Z0-9_]*)\\\"\\)\\]");

						//	If the entity specified a different column name than the member name, remember it.
						if (match.Success)
							entityName = match.Groups["columnName"].Value;
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
						var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
						var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
						var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
						var typedecl = $"{variableName}(({singletype})|({multitype}))*";
						var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
						var match2 = Regex.Match(line, pattern);

						if (match2.Success)
						{
							//	Okay, we got a column. Get the member name can call it the entityName.
							var className = match2.Groups["columnname"].Value;

							if (string.IsNullOrWhiteSpace(entityName))
								entityName = className;

							var entityColumn = new DBColumn()
							{
								ColumnName = className,
								EntityName = entityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = DBServerType.POSTGRESQL
							};

							classFile.Columns.Add(entityColumn);

							entityName = string.Empty;
						}
					}
				}
			}
			else
			{
				classFile.Columns = new List<DBColumn>();
			}

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
							NpgsqlDbType dataType = NpgsqlDbType.Unknown;

							try
							{
								dataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1));
							}
							catch (InvalidCastException)
							{
							}

							var dbColumn = classFile.Columns.FirstOrDefault(c => string.Equals(c.EntityName, reader.GetString(0), StringComparison.OrdinalIgnoreCase));

							if (dbColumn == null)
							{
								dbColumn = new DBColumn();
								dbColumn.EntityName = reader.GetString(0);
								dbColumn.ColumnName = Utilities.NormalizeClassName(reader.GetString(0));
								classFile.Columns.Add(dbColumn);
							}

							dbColumn.DataType = dataType;
							dbColumn.dbDataType = reader.GetString(1);
							dbColumn.Length = Convert.ToInt64(reader.GetValue(2));
							dbColumn.IsNullable = Convert.ToBoolean(reader.GetValue(3));
							dbColumn.IsComputed = Convert.ToBoolean(reader.GetValue(4));
							dbColumn.IsIdentity = Convert.ToBoolean(reader.GetValue(5));
							dbColumn.IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6));
							dbColumn.IsIndexed = Convert.ToBoolean(reader.GetValue(7));
							dbColumn.IsForeignKey = Convert.ToBoolean(reader.GetValue(8));
							dbColumn.ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
						}
					}
				}
			}
		}
	}
}