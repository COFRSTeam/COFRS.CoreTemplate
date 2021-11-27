using COFRSCoreCommon.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Utilities
{
    public static class StandardUtils
    {
		/// <summary>
		/// Loads the <see cref="ProjectMapping"/> for the project
		/// </summary>
		/// <param name="_dte2"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <returns>The <see cref="ProjectMapping"/> for the project.</returns>
		public static ProjectMapping OpenProjectMapping(DTE2 _dte2)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = _dte2.Solution.Properties.Item("Path").Value.ToString();
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
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Load the <see cref="ProfileMap"/> for the resource
		/// </summary>
		/// <param name="_dte2">The <see cref="DTE2"/> Visual Studio interface</param>
		/// <param name="resourceModel">The <see cref="ResourceModel"/> whose <see cref="ProfileMap"/> is to be loaded.</param>
		/// <returns>The <see cref="ProfileMap"/> for the <see cref="ResourceModel"/></returns>
		public static ProfileMap LoadResourceMapping(DTE2 dte2, ResourceModel resourceModel)
		{
			var solutionPath = dte2.Solution.Properties.Item("Path").Value.ToString();
			var filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs"), $"{resourceModel.ClassName}.{resourceModel.EntityModel.ClassName}.json");

			if (File.Exists(filePath))
			{
				var jsonValue = File.ReadAllText(filePath);
				return JsonConvert.DeserializeObject<ProfileMap>(jsonValue);
			}

			return null;
		}


		private static DBColumn[] LoadColumns(CodeClass2 codeClass)
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

		/// <summary>
		/// Get the default connection string from the appsettings.local.json
		/// </summary>
		/// <returns>The connection string used in the local settings</returns>
		public static string GetConnectionString(DTE2 dte2)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem settingsFile = dte2.Solution.FindProjectItem("appsettings.Local.json");

			var filepath = settingsFile.FileNames[0];

			using ( var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
				using ( var reader = new StreamReader(stream))
                {
					var jsonText = reader.ReadToEnd();
					var settings = JObject.Parse(jsonText);
					var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
					return connectionStrings["DefaultConnection"].Value<string>();
				}
			}
		}

		/// <summary>
		/// Get the default server type
		/// </summary>
		/// <param name="DefaultConnectionString">The default connection string</param>
		/// <returns>The default server type</returns>
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

		/// <summary>
		/// Get the validator interface name for a resource
		/// </summary>
		/// <param name="resourceClassName">The resource class whos validator is to be found</param>
		/// <param name="folder">The folder to search</param>
		/// <returns>The name of the interface for the validator of the resource.</returns>
		public static string FindValidatorInterface(DTE2 dte2, string resourceClassName, string folder = "")
		{
			var projectMapping = OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
			var validatorModelFolder = projectMapping.GetValidatorFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(validatorModelFolder.Folder) :
																	  dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					string validatorClass = FindValidatorInterface(dte2, resourceClassName, projectItem.Name);

					if (!string.IsNullOrWhiteSpace(validatorClass))
						return validatorClass;
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile && projectItem.FileCodeModel != null)
				{
					FileCodeModel2 codeModel = (FileCodeModel2)projectItem.FileCodeModel;

					foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeInterface2 codeClass in codeNamespace.Children.OfType<CodeInterface2>())
						{
							foreach (CodeInterface2 codeBase in codeClass.Bases.OfType<CodeInterface2>())
							{
								var parts = codeBase.FullName.Split(new char[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);

								if (parts.Length == 2)
								{
									var interfaceParts = parts[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
									var classParts = parts[1].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

									if (interfaceParts[interfaceParts.Length - 1].Equals("IValidator") &&
										 classParts[classParts.Length - 1].Equals(resourceClassName))
									{
										return codeClass.Name;
									}
								}
							}
						}
					}
				}
			}

			return string.Empty;
		}

		public static ResourceModel GetParentModel(List<ResourceModel> resourceModels, ResourceModel parent, string[] parts)
		{
			ResourceModel result = parent;

			for (int i = 0; i < parts.Count() - 1; i++)
			{
				var column = result.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[i], StringComparison.OrdinalIgnoreCase));

				if (column != null)
				{
					result = resourceModels.FirstOrDefault(p => string.Equals(p.ClassName, column.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));
				}
			}

			return result;
		}

		/// <summary>
		/// Get the validator interface name for a resource
		/// </summary>
		/// <param name="resourceClassName">The resource class whos validator is to be found</param>
		/// <param name="folder">The folder to search</param>
		/// <returns>The name of the interface for the validator of the resource.</returns>
		public static CodeClass2 FindExampleCode(DTE2 dte2, ResourceModel parentModel, string folder = "")
		{
			var projectMapping = OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
			var ExamplesFolder = projectMapping.GetExamplesFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(ExamplesFolder.Folder) :
																	  dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 codeFile = FindExampleCode(dte2, parentModel, projectItem.Name);

					if (codeFile != null)
						return codeFile;
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile && projectItem.FileCodeModel != null)
				{
					FileCodeModel2 codeModel = (FileCodeModel2)projectItem.FileCodeModel;

					foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in codeNamespace.Children.OfType<CodeClass2>())
						{
							EditPoint2 editPoint = (EditPoint2)codeClass.StartPoint.CreateEditPoint();

							bool foundit = editPoint.FindPattern($"IExamplesProvider<{parentModel.ClassName}>");
							foundit = foundit && editPoint.LessThan(codeClass.EndPoint);

							if (foundit)
							{
								return codeClass;
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Get the validator interface name for a resource
		/// </summary>
		/// <param name="resourceClassName">The resource class whos validator is to be found</param>
		/// <param name="folder">The folder to search</param>
		/// <returns>The name of the interface for the validator of the resource.</returns>
		public static CodeClass2 FindCollectionExampleCode(DTE2 dte2, ResourceModel parentModel, string folder = "")
		{
			var projectMapping = OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
			var ExamplesFolder = projectMapping.GetExamplesFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(ExamplesFolder.Folder) :
																	  dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 codeFile = FindCollectionExampleCode(dte2, parentModel, projectItem.Name);

					if (codeFile != null)
						return codeFile;
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile && projectItem.FileCodeModel != null)
				{
					FileCodeModel2 codeModel = (FileCodeModel2)projectItem.FileCodeModel;

					foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in codeNamespace.Children.OfType<CodeClass2>())
						{
							EditPoint2 editPoint = (EditPoint2)codeClass.StartPoint.CreateEditPoint();

							bool foundit = editPoint.FindPattern($"IExamplesProvider<PagedCollection<{parentModel.ClassName}>>");
							foundit = foundit && editPoint.LessThan(codeClass.EndPoint);

							if (foundit)
							{
								return codeClass;
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Load all entity models from the entity models folder
		/// </summary>
		/// <param name="folder">The child folder to search</param>
		/// <returns>A collection of all entity models in the solution</returns>
		public static EntityMap LoadEntityModels(DTE2 dte2, string folder = "")
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<EntityModel>();
			var projectMapping = OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.

			var entityFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(projectMapping.GetEntityModelsFolder().Folder) :
																   dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in entityFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
					 projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
				{
					var emap = LoadEntityModels(dte2, projectItem.Name);
					map.AddRange(emap.Maps);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
						 projectItem.FileCodeModel != null &&
						 projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
						 Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

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
									ProjectName = projectMapping.GetEntityModelsFolder().ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement);
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
									ProjectName = projectMapping.GetEntityModelsFolder().ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement);
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
									ProjectName = projectMapping.GetEntityModelsFolder().ProjectName,
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

		/// <summary>
		///	Load all resource models from the resource folder
		/// </summary>
		/// <param name="folder">The child folder to search</param>
		/// <returns>A collection of all resource models in the solution</returns>
		public static ResourceMap LoadResourceMap(DTE2 dte, string folder = "")
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<ResourceModel>();

			var projectMapping = OpenProjectMapping(dte);                        //	Contains the names and projects where various source file exist.
			var entityModelsFolder = projectMapping.GetEntityModelsFolder();
			var resourceModelFolder = projectMapping.GetResourceModelsFolder();
			var entityMap = LoadEntityModels(dte);

			var defaultServerType = GetDefaultServerType(GetConnectionString(dte));


			var resourceFolder = string.IsNullOrWhiteSpace(folder) ? dte.Solution.FindProjectItem(resourceModelFolder.Folder) :
																	 dte.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in resourceFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
				{
					var resourceMap = LoadResourceMap(dte, projectItem.Name);
					map.AddRange(resourceMap.Maps);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						//	Process any Enums found in the folder...
						foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
						{
							CodeAttribute entityAttribute = null;

							var resourceModel = new ResourceModel
							{
								ClassName = enumElement.Name,
								Namespace = namespaceElement.Name,
								ServerType = defaultServerType,
								EntityModel = null,
								ProjectName = resourceModelFolder.ProjectName,
								ResourceType = ResourceType.Enum,
								Folder = projectItem.Properties.Item("FullPath").Value.ToString()
							};

							var columns = new List<DBColumn>();

							foreach (CodeVariable2 enumMember in enumElement.Children.OfType<CodeVariable2>())
							{
								var dbColumn = new DBColumn
								{
									ColumnName = enumMember.Name
								};

								columns.Add(dbColumn);
							}

							resourceModel.Columns = columns.ToArray();

							try { entityAttribute = (CodeAttribute)enumElement.Children.Item("Entity"); } catch (Exception) { }

							if (entityAttribute != null)
							{
								var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

								var entityName = "Unknown";
								if (match.Success)
									entityName = match.Groups["entityType"].Value.ToString();

								var entityModel = entityMap.Maps.FirstOrDefault(e =>
									string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

								resourceModel.EntityModel = entityModel;
								resourceModel.ServerType = entityModel.ServerType;
							}

							map.Add(resourceModel);
						}

						//	Process any classes found in folder...
						foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
						{
							var resourceModel = new ResourceModel
							{
								ClassName = classElement.Name,
								Namespace = namespaceElement.Name,
								ServerType = defaultServerType,
								EntityModel = null,
								ProjectName = resourceModelFolder.ProjectName,
								Folder = projectItem.Properties.Item("FullPath").Value.ToString()
							};

							CodeAttribute entityAttribute = null;

							try { entityAttribute = (CodeAttribute)classElement.Children.Item("Entity"); } catch (Exception) { }

							if (entityAttribute != null)
							{
								var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

								var entityName = "Unknown";
								if (match.Success)
									entityName = match.Groups["entityType"].Value.ToString();

								var entityModel = entityMap.Maps.FirstOrDefault(e =>
									string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

								resourceModel.ServerType = entityModel.ServerType;
								resourceModel.EntityModel = entityModel;
							}

							var columns = new List<DBColumn>();
							var functions = new List<CodeFunction2>();
							var foreignKeyColumns = resourceModel.EntityModel == null ? Array.Empty<DBColumn>() : resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey).ToArray();

							foreach (CodeElement memberElement in classElement.Children)
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
				}
			}

			return new ResourceMap() { Maps = map.ToArray() };
		}

		public static string GetExampleModel(int skipRecords, ResourceModel resourceModel, DBServerType serverType, string connectionString)
		{
			if (serverType == DBServerType.MYSQL)
				return GetMySqlExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.POSTGRESQL)
				return GetPostgresExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.SQLSERVER)
				return GetSQLServerExampleModel(skipRecords, resourceModel, connectionString);

			throw new ArgumentException("Invalid or unrecognized DBServerType", "serverType");
		}

		public static string GetMySqlExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public static string GetPostgresExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public static string GetSQLServerExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			StringBuilder results = new StringBuilder();

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();

				var query = new StringBuilder();
				query.Append("select ");

				bool first = true;
				foreach (var column in resourceModel.EntityModel.Columns)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						query.Append(',');
					}

					query.Append($"[{column.ColumnName}]");
				}

				if (string.IsNullOrWhiteSpace(resourceModel.EntityModel.SchemaName))
				{
					query.Append($" from [{resourceModel.EntityModel.TableName}]");
				}
				else
				{
					query.Append($" from [{resourceModel.EntityModel.SchemaName}].[{resourceModel.EntityModel.TableName}]");
				}

				query.Append(" order by ");

				first = true;
				foreach (var column in resourceModel.EntityModel.Columns)
				{
					if (column.IsPrimaryKey)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							query.Append(',');
						}

						query.Append($"[{column.ColumnName}]");
					}
				}

				query.Append($" OFFSET {skipRecords} ROWS");
				query.Append(" FETCH NEXT 1 ROWS ONLY;");

				results.AppendLine("{");

				using (var command = new SqlCommand(query.ToString(), connection))
				{
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							first = true;
							foreach (var column in resourceModel.EntityModel.Columns)
							{
								if (first)
									first = false;
								else
									results.AppendLine(",");
								results.Append($"\t\"{column.ColumnName}\": ");

								switch (column.DBDataType.ToLower())
								{
									case "bigint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetInt64(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "binary":
									case "image":
									case "timestamp":
									case "varbinary":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var length = reader.GetBytes(0, -1, null, 1, 1);
											var byteBuffer = new byte[length];
											reader.GetBytes(0, 0, byteBuffer, 0, (int)length);
											var Value = Convert.ToBase64String(byteBuffer);
											results.Append($"{Value}");
										}
										break;

									case "bit":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
											results.Append("null");
										else
										{
											var Value = reader.GetBoolean(reader.GetOrdinal(column.ColumnName));
											results.Append(Value ? "true" : "false");
										}
										break;

									case "date":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTime(reader.GetOrdinal(column.ColumnName));
											results.Append("\"{date.ToShortDateString()}\"");
										}
										break;

									case "datetime":
									case "datetime2":
									case "smalldatetime":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTime(reader.GetOrdinal(column.ColumnName));
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "datetimeoffset":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTimeOffset(reader.GetOrdinal(column.ColumnName));
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "decimal":
									case "money":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetDecimal(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "float":
									case "real":
									case "smallmoney":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetFloat(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "int":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
											results.Append("null");
										else
										{
											var Value = reader.GetInt32(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "smallint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetInt16(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "tinyint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetByte(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "time":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetTimeSpan(reader.GetOrdinal(column.ColumnName));
											results.Append($"\"{Value}\"");
										}
										break;

									case "text":
									case "nvarchar":
									case "ntext":
									case "char":
									case "nchar":
									case "varchar":
									case "xml":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else if (string.Equals(column.DBDataType, "hierarchyid", StringComparison.OrdinalIgnoreCase))
										{
											var theValue = reader.GetFieldValue<object>(reader.GetOrdinal(column.ColumnName));
											theValue = theValue.ToString().Replace("/", "-");
											results.Append($"\"{theValue}\"");
										}
										else
										{
											var Value = reader.GetString(reader.GetOrdinal(column.ColumnName));
											results.Append($"\"{Value}\"");
										}
										break;

									default:
										throw new InvalidDataException($"Unrecognized database type: {column.ModelDataType}");
								}
							}
						}
						else
						{
							first = true;
							foreach (var column in resourceModel.EntityModel.Columns)
							{
								if (first)
									first = false;
								else
									results.AppendLine(",");
								results.Append($"\t\"{column.ColumnName}\": ");

								switch (column.DBDataType.ToLower())
								{
									case "bigint":
										results.Append("100");
										break;

									case "binary":
									case "image":
									case "timestamp":
									case "varbinary":
										{
											var str = "The cow jumped over the moon";
											var buffer = Encoding.UTF8.GetBytes(str);
											var str2 = Convert.ToBase64String(buffer);
											results.Append($"{str2}");
										}
										break;

									case "bit":
										results.Append("true");
										break;

									case "date":
										{
											var date = DateTime.Now; ;
											results.Append("\"{date.ToShortDateString()}\"");
										}
										break;

									case "datetime":
									case "datetime2":
									case "smalldatetime":
										{
											var date = DateTime.Now;
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "datetimeoffset":
										{
											var date = DateTimeOffset.Now;
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "decimal":
									case "money":
									case "float":
									case "real":
									case "smallmoney":
										{
											var Value = 124.32;
											results.Append($"{Value}");
										}
										break;

									case "int":
									case "smallint":
									case "tinyint":
										results.Append("10");
										break;

									case "time":
										{
											var Value = TimeSpan.FromSeconds(24541);
											results.Append($"\"{Value}\"");
										}
										break;

									case "text":
									case "nvarchar":
									case "ntext":
									case "char":
									case "nchar":
									case "varchar":
									case "xml":
										{
											var Value = "A string value";
											results.Append($"\"{Value}\"");
										}
										break;

									default:
										throw new InvalidDataException($"Unrecognized database type: {column.ModelDataType}");
								}
							}
						}
					}
				}

				results.AppendLine();
				results.AppendLine("}");

			}

			return results.ToString();
		}

		public static string ResolveMapFunction(JObject entityJson, string columnName, ResourceModel model, string mapFunction)
		{
			bool isDone = false;
			var originalMapFunction = mapFunction;
			var valueNumber = 1;
			List<string> valueAssignments = new List<string>();

			var simpleConversion = ExtractSimpleConversion(entityJson, model, mapFunction);

			if (!string.IsNullOrWhiteSpace(simpleConversion))
				return simpleConversion;

			var wellKnownConversion = ExtractWellKnownConversion(entityJson, model, mapFunction);

			if (!string.IsNullOrWhiteSpace(wellKnownConversion))
				return wellKnownConversion;

			while (!isDone)
			{
				var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

				if (ef.Success)
				{
					var entityColumnReference = ef.Groups["entity"];
					var textToReplace = ef.Groups["replace"];
					var token = entityJson[entityColumnReference.Value];

					var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(entityColumnReference.Value, StringComparison.OrdinalIgnoreCase));
					var resourceColumn = model.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "bool":
							switch (token.Type)
							{
								case JTokenType.Boolean:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<bool>().ToString().ToLower()};");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						case "bool?":
							switch (token.Type)
							{
								case JTokenType.Boolean:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<bool>().ToString().ToLower()};");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								case JTokenType.Null:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						case "int":
							switch (token.Type)
							{
								case JTokenType.Integer:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<int>()};");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}}}");
									break;
							}
							break;

						case "int?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<int>()};");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								case JTokenType.Null:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						case "string":
							switch (token.Type)
							{
								case JTokenType.String:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = \"{token.Value<string>()}\";");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								case JTokenType.Null:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						case "datetime":
							switch (token.Type)
							{
								case JTokenType.Date:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = DateTime.Parse(\"{token.Value<DateTime>():O}\");");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						case "datetime?":
							switch (token.Type)
							{
								case JTokenType.Date:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = DateTime.Parse(\"{token.Value<DateTime>():O}\");");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								case JTokenType.Null:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;

								default:
									valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
									mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
									break;
							}
							break;

						default:
							return "default";
					}

					valueNumber++;
				}
				else
					isDone = true;
			}

			StringBuilder results = new StringBuilder();
			results.Append("MapFrom(() => {");
			foreach (var assignment in valueAssignments)
				results.Append($"{assignment} ");
			results.Append($" return {mapFunction};");
			results.Append("})");

			return results.ToString();
		}

		public static string ExtractWellKnownConversion(JObject entityJson, ResourceModel model, string mapFunction)
		{
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				var token = entityJson[ef.Groups["entity"].Value];
				var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));
				var replaceText = ef.Groups["replace"].Value;

				var seek = $"{replaceText}\\.HasValue[ \t]*\\?[ \t]*\\(TimeSpan\\?\\)[ \t]*TimeSpan\\.FromSeconds[ \t]*\\([ \t]*\\(double\\)[ \t]*{replaceText}[ \t]*\\)[ \t]*\\:[ \t]*null";

				var sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "byte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<byte>()})";
							}
							break;

						case "sbyte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<sbyte>()})";
							}
							break;

						case "short":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<short>()})";
							}
							break;

						case "ushort":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ushort>()})";
							}
							break;

						case "int":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<int>()})";
							}
							break;

						case "uint":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<uint>()})";
							}
							break;

						case "long":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<long>()})";
							}
							break;

						case "ulong":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ulong>()})";
							}
							break;
					}
				}

				seek = $"TimeSpan\\.FromSeconds[ \t]*\\([ \t]*\\(double\\)[ \t]*{replaceText}[ \t]*\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					switch (entityColumn.ModelDataType.ToLower())
					{
						case "byte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<byte>()})";
							}
							break;

						case "sbyte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<sbyte>()})";
							}
							break;

						case "short":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<short>()})";
							}
							break;

						case "ushort":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ushort>()})";
							}
							break;

						case "int":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<int>()})";
							}
							break;

						case "uint":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<uint>()})";
							}
							break;

						case "long":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<long>()})";
							}
							break;

						case "ulong":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ulong>()})";
							}
							break;
					}
				}

				seek = $"string\\.IsNullOrWhiteSpace\\({replaceText}\\)[ \t]*\\?[ \t]*null[ \t]*\\:[ \t]*new[ \t]*Uri\\({replaceText}\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "string":
							if (token.Type == JTokenType.String)
							{
								try
								{
									var uri = new Uri(token.Value<string>(), UriKind.Absolute);
									return $"new Uri(\"{token.Value<string>()}\", UriKind.Absolute)";
								}
								catch (UriFormatException)
								{
									return $"new Uri(\"http://somedomain.com\")";
								}
							}
							break;
					}
				}


				seek = $"{replaceText}\\.HasValue[ \t]+\\?[ \t]*\\(DateTimeOffset\\?\\)[ \t]*new[ \t]+DateTimeOffset\\([ \t]*{replaceText}(\\.Value){{0,1}}[ \t]*\\)[ \t]*\\:[ \t]*null";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "DateTime?":
							if (token.Type == JTokenType.Date)
							{
								var DateTimeValue = token.Value<DateTime>();
								var DateTimeOffsetValue = new DateTimeOffset(DateTimeValue);
								return $"DateTimeOffset.Parse({DateTimeOffsetValue.ToString():O})";
							}
							break;
					}
				}

				seek = $"new[ \t]+DateTimeOffset\\([ \t]*{replaceText}[ \t]*\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "datetime":
							if (token.Type == JTokenType.Date)
							{
								var DateTimeValue = token.Value<DateTime>();
								var DateTimeOffsetValue = new DateTimeOffset(DateTimeValue);
								var dtString = DateTimeOffsetValue.ToString("O");
								return $"DateTimeOffset.Parse(\"{dtString}\")";
							}
							break;
					}
				}
			}

			return string.Empty;
		}

		public static string ExtractSimpleConversion(JObject entityJson, ResourceModel model, string mapFunction)
		{
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				if (mapFunction.Equals(ef.Groups["replace"].Value))
				{
					var token = entityJson[ef.Groups["entity"].Value];
					var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "bool":
						case "bool?":
							switch (token.Type)
							{
								case JTokenType.Boolean:
									return token.Value<bool>().ToString().ToLower();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "byte":
						case "byte?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<byte>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "sbyte":
						case "sbyte?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<sbyte>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "short":
						case "short?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<short>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "ushort":
						case "ushort?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<ushort>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "int":
						case "int?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<int>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "uint":
						case "uint?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<uint>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "long":
						case "long?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<long>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "ulong":
						case "ulong?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<ulong>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "float":
						case "float?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<float>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "double":
						case "double?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<double>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "decimal":
						case "decimal?":
							switch (token.Type)
							{
								case JTokenType.Integer:
									return token.Value<decimal>().ToString();

								case JTokenType.Null:
									return "null";

								default:
									return "default";
							}

						case "string":
							switch (token.Type)
							{
								case JTokenType.String:
									return $"\"{token.Value<string>()}\"";

								case JTokenType.Null:
									return "string.Empty";

								default:
									return "string.Empty";
							}

						case "Guid":
							switch (token.Type)
							{
								case JTokenType.Guid:
									return $"Guid.Parse(\"{token.Value<Guid>()}\")";

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}

						case "DateTime":
						case "DateTime?":
							switch (token.Type)
							{
								case JTokenType.Date:
									return $"DateTime.Parse(\"{token.Value<DateTime>().ToString():O}\")";

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}

						case "DateTimeOffset":
						case "DateTimeOffset?":
							switch (token.Type)
							{
								case JTokenType.Date:
									return $"DateTimeOffset.Parse(\"{token.Value<DateTimeOffset>().ToString():O}\")";

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}

						case "TimeSpan":
						case "TimeSpan?":
							switch (token.Type)
							{
								case JTokenType.TimeSpan:
									return $"TimeSpan.Parse(\"{token.Value<TimeSpan>()}\")";

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}

						case "byte[]":
						case "IEnumerable<byte>":
							switch (token.Type)
							{
								case JTokenType.String:
									return $"Convert.FromBase64String(\"{token.Value<string>()}\").ToArray()";

								case JTokenType.Bytes:
									{
										var theBytes = token.Value<byte[]>();
										var str = Convert.ToBase64String(theBytes);
										return $"Convert.FromBase64String(\"{str}\").ToArray()";
									}

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}

						case "List<byte>":
							switch (token.Type)
							{
								case JTokenType.String:
									return $"Convert.FromBase64String(\"{token.Value<string>()}\").ToList()";

								case JTokenType.Bytes:
									{
										var theBytes = token.Value<byte[]>();
										var str = Convert.ToBase64String(theBytes);
										return $"Convert.FromBase64String(\"{str}\").ToList()";
									}

								case JTokenType.Null:
									return null;

								default:
									return "default";
							}
					}
				}
			}

			return string.Empty;
		}
	}
}
