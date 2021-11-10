using COFRSCoreCommandsPackage.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSCoreCommandsPackage.Forms
{
    public partial class AddCollectionDialog : Form
    {
        public DTE2 _dte2;
		private ResourceMap resourceMap;

        public AddCollectionDialog()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var projectMapping = OpenProjectMapping(_dte2.Solution);

			var entityModelsFolder = projectMapping.GetEntityModelsFolder();
			var resourceModelsFolder = projectMapping.GetResourceModelsFolder();

            var connectionString = GetConnectionString(_dte2.Solution);
            var defultServerType = GetDefaultServerType(connectionString);
            var entityMap = LoadEntityModels(_dte2.Solution, entityModelsFolder);
            resourceMap = LoadResourceModels(_dte2.Solution, entityMap, resourceModelsFolder, defultServerType);

			var sourceResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase));
			var sourceTableName = sourceResourceModel.EntityModel.TableName;

			foreach ( var resourceModel in resourceMap.Maps )
            {
				if ( !resourceModel.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase) && resourceModel.EntityModel != null )
                {
					var foreignKeys = resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey && c.ForeignTableName.Equals(sourceTableName, StringComparison.OrdinalIgnoreCase));
					if (foreignKeys.Count() > 0)
					{
						var existingMember = sourceResourceModel.Columns.FirstOrDefault(c => c.ModelDataType.Equals($"IEnumerable<{resourceModel.ClassName}>", StringComparison.OrdinalIgnoreCase));
						
						if ( existingMember == null )	
							ChildResourceList.Items.Add(resourceModel.ClassName);
					}
				}
            }
        }

		private void OnOK(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (ChildResourceList.SelectedIndex != -1)
			{
				var sourceResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase));
				var memberResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ChildResourceList.SelectedItem.ToString(), StringComparison.OrdinalIgnoreCase));
				var nn = new NameNormalizer(memberResourceModel.ClassName);

				var fileName = Path.GetFileName(sourceResourceModel.Folder);
				ProjectItem sourceResource = _dte2.Solution.FindProjectItem(fileName);

				bool wasSourceOpen = sourceResource.IsOpen[Constants.vsViewKindAny];               //	Record if it was already open

				if (!wasSourceOpen)                                                               //	If it wasn't open, open it.
					sourceResource.Open(Constants.vsViewKindCode);

				var window = sourceResource.Open(Constants.vsViewKindTextView);              //	Get the window (so we can close it later)
				Document doc = sourceResource.Document;                                      //	Get the doc 
				TextSelection sel = (TextSelection)doc.Selection;                           //	Get the current selection
				var activePoint = sel.ActivePoint;                                          //	Get the active point

				foreach ( CodeNamespace namespaceElement in sourceResource.FileCodeModel.CodeElements.OfType<CodeNamespace>())
                {
					foreach ( CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>() )
                    {
						var editPoint = (EditPoint2)classElement.EndPoint.CreateEditPoint();
						editPoint.StartOfLine();
						editPoint.InsertNewLine();

						editPoint.Insert($"\t\t///\t<summary>\r\n\t\t///\tGets or sets the collection of {memberResourceModel.ClassName} resources\r\n\t\t///\t</summary>\r\n\t\tpublic IEnumerable<{memberResourceModel.ClassName}> {nn.PluralForm} {{ get; set; }}\r\n");
					}
				}

				ProjectItem orchestrator = _dte2.Solution.FindProjectItem("ServiceOrchestrator.cs");

				bool wasOrchestratorOpen = sourceResource.IsOpen[Constants.vsViewKindAny];               //	Record if it was already open

				if (!wasOrchestratorOpen)                                                               //	If it wasn't open, open it.
					sourceResource.Open(Constants.vsViewKindCode);

				var orchestratorWindow = orchestrator.Open(Constants.vsViewKindTextView);              //	Get the window (so we can close it later)
				Document orchestratorDoc = orchestrator.Document;                                      //	Get the doc 

				bool AddSystemText = true;
				FileCodeModel2 codeModel = (FileCodeModel2) orchestrator.FileCodeModel;

				foreach (CodeImport usingElement in codeModel.CodeElements.OfType<CodeImport>())
				{
					if (usingElement.Namespace.Equals("System.Text", StringComparison.OrdinalIgnoreCase))
						AddSystemText = false;
				}

				if (AddSystemText)
				{
					codeModel.AddImport("System.Text", -1);
				}

				foreach (CodeNamespace namespaceElement in codeModel.CodeElements.OfType<CodeNamespace>())
				{
					foreach (CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>())
					{
						foreach ( CodeFunction2 aFunction in classElement.Children.OfType<CodeFunction2>())
                        {
							if (aFunction.Name.Equals($"Get{sourceResourceModel.ClassName}Async", StringComparison.OrdinalIgnoreCase))
                            {
								var startPoint = aFunction.StartPoint;
								var endPoint = aFunction.EndPoint;

								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								
								if ( editPoint.FindPattern($"return await GetSingleAsync<{sourceResourceModel.ClassName}>(node);"))
                                {
									editPoint.ReplaceText(6, "var item =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();

									editPoint.Insert($"\r\n\t\t\tvar subNode = RqlNode.Parse($\"{sourceResourceModel.ClassName}=uri:\\\"{{item.HRef.LocalPath}}\\\"\");\r\n");
									editPoint.Insert($"\r\n\t\t\tvar {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);\r\n");
									editPoint.Insert($"\t\t\titem.{nn.PluralForm} = {memberResourceModel.ClassName}Collection.Items;\r\n");
									editPoint.Insert("\r\n\t\t\treturn item;");
                                }
							}
							else if ( aFunction.Name.Equals($"Get{sourceResourceModel.ClassName}CollectionAsync"))
                            {
								var startPoint = aFunction.StartPoint;
								var endPoint = aFunction.EndPoint;

								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

								if (editPoint.FindPattern($"return await GetCollectionAsync<Client>"))
								{
									editPoint.ReplaceText(6, "var collection =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();

									editPoint.Insert($"\r\n\t\t\tStringBuilder rqlBody = new(\"in({sourceResourceModel.ClassName}\");\r\n");
									editPoint.Insert($"\t\t\tforeach (var item in collection.Items)\r\n");
									editPoint.Insert($"\t\t\t{{\r\n");
									editPoint.Insert("\t\t\t\trqlBody.Append($\", uri:\\\"{item.HRef.LocalPath}\\\"\");\r\n");
									editPoint.Insert($"\t\t\t}}\r\n");
									editPoint.Insert($"\t\t\trqlBody.Append(\")\");\r\n\r\n");

									editPoint.Insert($"\t\t\tvar subNode = RqlNode.Parse(rqlBody.ToString());\r\n\r\n");

									editPoint.Insert($"\t\t\tvar {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);\r\n\r\n");

									editPoint.Insert($"\t\t\tforeach ( var item in {memberResourceModel.ClassName}Collection.Items)\r\n");
									editPoint.Insert($"\t\t\t{{\r\n");
									editPoint.Insert($"\t\t\t\tvar mainItem = collection.Items.FirstOrDefault(i => i.HRef == item.Client);\r\n\r\n");
									editPoint.Insert($"\t\t\t\tif (mainItem.{nn.PluralForm} == null)\r\n");
									editPoint.Insert($"\t\t\t\t{{\r\n");
									editPoint.Insert($"\t\t\t\t\tmainItem.{nn.PluralForm} = new {memberResourceModel.ClassName}[] {{ item }};\r\n");
									editPoint.Insert($"\t\t\t\t}}\r\n");
									editPoint.Insert($"\t\t\t\telse\r\n");
									editPoint.Insert($"\t\t\t\t{{\r\n");
									editPoint.Insert($"\t\t\t\t\tmainItem.{nn.PluralForm} = new List<{memberResourceModel.ClassName}>(mainItem.{nn.PluralForm}) {{ item }}.ToArray();\r\n");
									editPoint.Insert($"\t\t\t\t}}\r\n");
									editPoint.Insert($"\t\t\t}}\r\n\r\n");

									editPoint.Insert("\t\t\treturn collection;");
								}
							}
						}
					}
				}
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		#region Helper Functions
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
					FileCodeModel2 model = (FileCodeModel2) projectItem.FileCodeModel;

					foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
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

						foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
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

			foreach (Project project in solution.Projects)
			{
				if (string.Equals(project.Name, projectFolder.ProjectName, StringComparison.OrdinalIgnoreCase))
					return project;
			}

			return null;
		}

		private static DBColumn[] LoadColumns(CodeClass2 codeClass, EntityModel entityModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var columns = new List<DBColumn>();

			foreach (CodeProperty2 property in codeClass.Children.OfType<CodeProperty2>())
			{
				var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				CodeAttribute memberAttribute = null;
				try { memberAttribute = (CodeAttribute)property.Children.Item("Member"); } catch (Exception) { }

				var dbColumn = new DBColumn
				{
					ColumnName = property.Name,
					EntityName = property.Name,
					ModelDataType = parts[parts.Count() - 1]
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
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>())
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
												ModelDataType = parts[parts.Count() - 1],
												IsPrimaryKey = string.Equals(property.Name, "href", StringComparison.OrdinalIgnoreCase)
											};

											var fk = foreignKeyColumns.FirstOrDefault(c =>
											{
												var nn = new NameNormalizer(c.ForeignTableName);
												return string.Equals(nn.SingleForm, dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase);
											});

											if (fk != null)
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
										else if (memberElement.Kind == vsCMElement.vsCMElementFunction)
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
		#endregion
	}
}
