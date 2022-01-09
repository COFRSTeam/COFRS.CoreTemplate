using COFRS.Template.Common.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Constants = EnvDTE.Constants;

namespace COFRS.Template.Common.ServiceUtilities
{
    internal class CodeService : ICodeService
    {
        private ProjectMapping projectMapping = null;
        private readonly List<EntityClass> entityClassList = new List<EntityClass>();
        private readonly List<ResourceClass> resourceClassList = new List<ResourceClass>();
		private ProjectItem _localSettingsFile = null;
		private ProjectItem _globalSettingsFile = null;

		public CodeService()
        {
		}

		#region Properties
		public string ConnectionString
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                string jsonText;

				if (_localSettingsFile == null)
				{
					var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
					_localSettingsFile = mDte.Solution.FindProjectItem("appSettings.Local.json");
				}

				if (!_localSettingsFile.IsOpen)
                {
                    var filePath = _localSettingsFile.Properties.OfType<Property>().FirstOrDefault(p =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        return p.Name.Equals("FullPath");
                    });

                    if (filePath != null)
                    {
                        var thePath = filePath.Value.ToString();
                        jsonText = File.ReadAllText(thePath);
                    }
                    else
                    {
                        _localSettingsFile.Open();

                        TextSelection sel = (TextSelection)_localSettingsFile.Document.Selection;
                        sel.SelectAll();
                        jsonText = sel.Text;

                        _localSettingsFile.Document.Close();
                    }
                }
                else
                {
                    TextSelection sel = (TextSelection)_localSettingsFile.Document.Selection;
                    sel.SelectAll();
                    jsonText = sel.Text;
                }

                var settings = JObject.Parse(jsonText);
                var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
                return connectionStrings["DefaultConnection"].Value<string>();
            }

            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_localSettingsFile.IsOpen)
                {
                    TextSelection sel = (TextSelection)_localSettingsFile.Document.Selection;

                    sel.StartOfDocument();

                    if (sel.FindText("Server=localdb;Database=master;Trusted_Connection=True;"))
                    {
                        sel.SelectLine();
                        sel.Text = $"\t\t\"DefaultConnection\": \"{value}\"\r\n";
                        _localSettingsFile.Document.Save();
                    }
                }
                else
                {
                    _localSettingsFile.Open();
                    TextSelection sel = (TextSelection)_localSettingsFile.Document.Selection;

                    sel.StartOfDocument();

                    if (sel.FindText("Server=localdb;Database=master;Trusted_Connection=True;"))
                    {
                        sel.SelectLine();
                        sel.Text = $"\t\t\"DefaultConnection\": \"{value}\"\r\n";
                        _localSettingsFile.Document.Save();
                    }

                    _localSettingsFile.Document.Close();
                }
            }
        }

		/// <summary>
		/// Returns the <see cref="ProjectFolder"/> where the new item is being installed.
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <param name="replacementsDictionary">The dictionary of replacement values</param>
		/// <returns>The <see cref="ProjectFolder"/> where the new item is being installed.</returns>

		public ProjectFolder InstallationFolder
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

				var selectedItem = mDte.SelectedItems.Item(1);

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

					if (projectItem == null)
						return null;

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
		}

		/// <summary>
		/// Get the default server type
		/// </summary>
		/// <param name="dte2>"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <returns>The default server type</returns>
		public DBServerType DefaultServerType
		{
			get
			{
				var codeService = COFRSServiceFactory.GetService<ICodeService>();

				//	Get the location of the server configuration on disk
				var DefaultConnectionString = codeService.ConnectionString;
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
		}

		/// <summary>
		/// Loads the policies from the configureation file
		/// </summary>
		/// <param name="dte>"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <returns></returns>
		public List<string> Policies
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				var results = new List<string>();
				string jsonText;

				if (_globalSettingsFile == null)
				{
					var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
					_globalSettingsFile = mDte.Solution.FindProjectItem("appSettings.json");
				}

				if (!_globalSettingsFile.IsOpen)
				{
					var filePath = _globalSettingsFile.Properties.OfType<Property>().FirstOrDefault(p =>
					{
						ThreadHelper.ThrowIfNotOnUIThread();
						return p.Name.Equals("FullPath");
					});

					if (filePath != null)
					{
						var thePath = filePath.Value.ToString();
						jsonText = File.ReadAllText(thePath);
					}
					else
					{
						_globalSettingsFile.Open();

						TextSelection sel = (TextSelection)_localSettingsFile.Document.Selection;
						sel.SelectAll();
						jsonText = sel.Text;

						_globalSettingsFile.Document.Close();
					}
				}
				else
				{
					TextSelection sel = (TextSelection)_globalSettingsFile.Document.Selection;
					sel.SelectAll();
					jsonText = sel.Text;
				}

				var settings = JObject.Parse(jsonText);

				if (settings["OAuth2"] == null)
					return null;

				var oAuth2Settings = settings["OAuth2"].Value<JObject>();

				if (oAuth2Settings["Policies"] == null)
					return null;

				var policyArray = oAuth2Settings["Policies"].Value<JArray>();

				foreach (var policy in policyArray)
					results.Add(policy["Policy"].Value<string>());

				return results;
			}
		}

		public string Moniker
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				var results = new List<string>();
				string jsonText;

				if (_globalSettingsFile == null)
				{
					var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
					_globalSettingsFile = mDte.Solution.FindProjectItem("appSettings.json");
				}

				if (!_globalSettingsFile.IsOpen)
				{
					var filePath = _globalSettingsFile.Properties.OfType<Property>().FirstOrDefault(p =>
					{
						ThreadHelper.ThrowIfNotOnUIThread();
						return p.Name.Equals("FullPath");
					});

					if (filePath != null)
					{
						var thePath = filePath.Value.ToString();
						jsonText = File.ReadAllText(thePath);
					}
					else
					{
						_globalSettingsFile.Open();

						TextSelection sel = (TextSelection)_globalSettingsFile.Document.Selection;
						sel.SelectAll();
						jsonText = sel.Text;

						_globalSettingsFile.Document.Close();
					}
				}
				else
				{
					TextSelection sel = (TextSelection)_globalSettingsFile.Document.Selection;
					sel.SelectAll();
					jsonText = sel.Text;
				}

				var settings = JObject.Parse(jsonText);

				if (settings["ApiSettings"] == null)
					return "acme";

				var apiSettings = settings["ApiSettings"].Value<JObject>();

				if (apiSettings["CompanyName"] == null)
					return "acme";

				return apiSettings["CompanyName"].Value<string>();
			}
		}

		public List<EntityClass> EntityClassList
        {
            get
            {
				ThreadHelper.ThrowIfNotOnUIThread();
				if (entityClassList == null || entityClassList.Count == 0)
                    LoadEntityClassList();

                return entityClassList;
            }
        }

        public List<ResourceClass> ResourceClassList
        {
            get
            {
				ThreadHelper.ThrowIfNotOnUIThread();
				if (resourceClassList.Count == 0)
                    LoadResourceClassList();

                return resourceClassList;
            }
        }
        #endregion

        public void OnProjectItemRemoved(ProjectItem ProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var entity = entityClassList.FirstOrDefault(c =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
				try
				{
					return c.ProjectItem != null && (string.IsNullOrWhiteSpace(c.ProjectItem.Name) ? false : c.ProjectItem.Name.Equals(ProjectItem.Name));
				}
				catch (Exception ex)
                {
					var ss = ex.Message;
					return false;
                }
			});

            if (entity != null)
                entityClassList.Remove(entity);

            var resource = resourceClassList.FirstOrDefault(c =>
            {
				ThreadHelper.ThrowIfNotOnUIThread();
				try
				{
					return c.ProjectItem != null && (string.IsNullOrWhiteSpace(c.ProjectItem.Name) ? false : c.ProjectItem.Name.Equals(ProjectItem.Name));
				}
				catch (Exception ex)
				{
					var ss = ex.Message;
					return false;
				}
			});

            if (resource != null)
                resourceClassList.Remove(resource);
        }

		public void OnProjectItemAdded(ProjectItem ProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

			AddEntity(ProjectItem);
			AddResource(ProjectItem);
        }

        public void OnSolutionOpened()
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			entityClassList.Clear();
            resourceClassList.Clear();
            projectMapping = null;

            projectMapping = LoadProjectMapping();
            LoadEntityClassList();
            LoadResourceClassList();

			_localSettingsFile = mDte.Solution.FindProjectItem("appsettings.Local.json");
			_globalSettingsFile = mDte.Solution.FindProjectItem("appsettings.json");
		}

		public ProjectMapping LoadProjectMapping()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectMapping == null)
            {
				var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
				var solutionPath = mDte.Solution.Properties.Item("Path").Value.ToString();
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

					return AutoFillProjectMapping(projectMapping);
				}
				catch (FileNotFoundException)
				{
					var projectMapping = AutoFillProjectMapping(new ProjectMapping());
					SaveProjectMapping();
					return projectMapping;
				}
				catch (DirectoryNotFoundException)
				{
					var projectMapping = AutoFillProjectMapping(new ProjectMapping());
					SaveProjectMapping();
					return projectMapping;
				}
				catch (Exception)
				{
					var projectMapping = AutoFillProjectMapping(new ProjectMapping());
					SaveProjectMapping();
					return projectMapping;
				}
			}

            return projectMapping;
        }

        public void SaveProjectMapping()
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			var jsonData = JsonConvert.SerializeObject(projectMapping, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				MissingMemberHandling = MissingMemberHandling.Ignore
			});

			var solutionPath = mDte.Solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\ProjectMap.json");

			File.WriteAllText(mappingPath, jsonData);
		}

		public EntityClass GetEntityClassBySchema(string schema, string tableName)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (entityClassList.Count == 0)
                LoadEntityClassList();

            return entityClassList.FirstOrDefault(c => c.SchemaName.Equals(schema, StringComparison.OrdinalIgnoreCase) &&
                                                       c.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }

        public ResourceClass GetResourceClassBySchema(string schema, string tableName)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (resourceClassList == null || resourceClassList.Count == 0)
                LoadResourceClassList();

            return resourceClassList.FirstOrDefault(c => c.Entity != null &&
			                                             c.Entity.SchemaName.Equals(schema, StringComparison.OrdinalIgnoreCase) &&
                                                         c.Entity.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityClass GetEntityClass(string name)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			if (entityClassList.Count == 0)
                LoadEntityClassList();

            return entityClassList.FirstOrDefault(c => c.ClassName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ResourceClass GetResourceClass(string name)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (resourceClassList.Count == 0)
                LoadResourceClassList();

            return resourceClassList.FirstOrDefault(c => c.ClassName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void AddEntity(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
			if (entityClassList.Count == 0)
			{
				LoadEntityClassList();
			}
			else
			{
				FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

				if (model != null)
				{
					foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
					{
						var projectMapping = LoadProjectMapping();
						if (namespaceElement.Name.Contains(projectMapping.EntityNamespace))
						{
							foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
							{
								CodeAttribute2 tableAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

								if (tableAttribute != null)
								{
									var code = new EntityClass((CodeElement2)classElement);
									var existingClass = entityClassList.FirstOrDefault(c => c.ClassName.Equals(code.ClassName));

									if ( existingClass == null )
										entityClassList.Add(code);
								}
								else
								{
									CodeAttribute2 compositeAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

									if (compositeAttribute != null)
									{
										var code = new EntityClass((CodeElement2)classElement);
										var existingClass = entityClassList.FirstOrDefault(c => c.ClassName.Equals(code.ClassName));

										if (existingClass == null)
											entityClassList.Add(code);
									}
								}
							}

							foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
							{
								CodeAttribute2 enumAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

								if (enumAttribute != null)
								{
									var code = new EntityClass((CodeElement2)enumElement);
									var existingClass = entityClassList.FirstOrDefault(c => c.ClassName.Equals(code.ClassName));

									if (existingClass == null)
										entityClassList.Add(code);
								}
							}
						}
					}
				}
			}
        }

        public void AddResource(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
			if (resourceClassList.Count == 0)
			{
				LoadResourceClassList();
			}
			else
			{
				FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

				if (model != null)
				{
					foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
					{
						var projectMapping = LoadProjectMapping();
						if (namespaceElement.Name.Contains(projectMapping.ResourceNamespace))
						{
							foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
							{
								CodeAttribute2 entityAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));
								EntityClass entityClass = null;

								if (entityAttribute != null)
								{
									var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

									var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

									if (match.Success)
									{
										entityClass = GetEntityClass(match.Groups["entityClass"].Value);
									}
								}

								var code = new ResourceClass((CodeElement2)classElement, entityClass);
								var existingClass = resourceClassList.FirstOrDefault(c => c.ClassName.Equals(code.ClassName));

								if (existingClass == null)
									resourceClassList.Add(code);
							}

							foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
							{
								CodeAttribute2 entityAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));
								EntityClass entityClass = null;

								if (entityAttribute != null)
								{
									var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

									var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

									if (match.Success)
									{
										entityClass = GetEntityClass(match.Groups["entityClass"].Value);
									}
								}

								var code = new ResourceClass((CodeElement2)enumElement, entityClass);
								var existingClass = resourceClassList.FirstOrDefault(c => c.ClassName.Equals(code.ClassName));

								if (existingClass == null)
									resourceClassList.Add(code);
							}
						}
					}
				}
			}
        }

        public void LoadEntityClassList(string folder = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projectMapping = LoadProjectMapping();  //	Contains the names and projects where various source file exist.
            var mDte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var entityFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(projectMapping.GetEntityModelsFolder().Folder) :
                                                                   mDte.Solution.FindProjectItem(folder);

            foreach (ProjectItem projectItem in entityFolder.ProjectItems)
            {
                if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
                     projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
                {
                    LoadEntityClassList(projectItem.Name);
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
                            CodeAttribute2 tableAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                            if (tableAttribute != null)
                            {
								var code = new EntityClass((CodeElement2)classElement);
                                entityClassList.Add(code);
                            }
                            else
                            {
                                CodeAttribute2 compositeAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                                if (compositeAttribute != null)
                                {
									var code = new EntityClass((CodeElement2)classElement);
									entityClassList.Add(code);
                                }
                            }
                        }

                        foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                        {
                            CodeAttribute2 tableAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                            if (tableAttribute != null)
                            {
								var code = new EntityClass((CodeElement2)enumElement);
                                entityClassList.Add(code);
                            }
                        }
                    }
                }
            }
        }

        public void LoadResourceClassList(string folder = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projectMapping = LoadProjectMapping();  //	Contains the names and projects where various source file exist.
            var mDte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var entityFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(projectMapping.GetResourceModelsFolder().Folder) :
                                                                   mDte.Solution.FindProjectItem(folder);

            foreach (ProjectItem projectItem in entityFolder.ProjectItems)
            {
                if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
                     projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
                {
                    LoadEntityClassList(projectItem.Name);
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
                            CodeAttribute2 entityAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));
							EntityClass entityClass = null;

                            if (entityAttribute != null)
                            {
                                var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                                var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                                if (match.Success)
                                {
									entityClass = GetEntityClass(match.Groups["entityClass"].Value);
                                }
                            }

							var code = new ResourceClass((CodeElement2)classElement, entityClass);
							resourceClassList.Add(code);
                        }

                        foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                        {
                            CodeAttribute2 entityAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));
							EntityClass entityClass = null;

                            if (entityAttribute != null)
                            {
                                var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                                var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                                if (match.Success)
                                {
									entityClass = GetEntityClass(match.Groups["entityClass"].Value);
                                }
                            }

							var code = new ResourceClass((CodeElement2)enumElement, entityClass);
							resourceClassList.Add(code);
                        }
                    }
                }
            }
        }

		#region Miscellaneous Operations
		/// <summary>
		/// Returns <see langword="true"/> if the candidate child path is a child of the parent path; <see langword="false"/> otherwise.
		/// </summary>
		/// <param name="parentPath">The prospective parent path.</param>
		/// <param name="candidateChildPath">The prospective child path.</param>
		/// <returns>Returns <see langword="true"/> if the candidate child path is a child of the parent path; <see langword="false"/> otherwise.</returns>
		public bool IsChildOf(string parentPath, string candidateChildPath)
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

		public string FindOrchestrationNamespace()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var projectItem = mDte.Solution.FindProjectItem("ServiceOrchestrator.cs");
			var code = projectItem.FileCodeModel;

			foreach (CodeElement c in code.CodeElements)
			{
				if (c.Kind == vsCMElement.vsCMElementNamespace)
					return c.Name;
			}

			return string.Empty;
		}

		public void RegisterComposite(string className, string entityNamespace, ElementType elementType, string tableName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			if (elementType == ElementType.Undefined ||
				elementType == ElementType.Table)
				return;

			ProjectItem serviceConfig = mDte.Solution.FindProjectItem("ServicesConfig.cs");
			FileCodeModel2 codeModel = (FileCodeModel2)serviceConfig.FileCodeModel;

			if (codeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Npgsql")) == null)
				codeModel.AddImport("Npgsql", -1);

			if (codeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(entityNamespace)) == null)
				codeModel.AddImport(entityNamespace, -1);

			foreach (var codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
			{
				var codeClass = codeNamespace.Children.OfType<CodeClass2>().FirstOrDefault(c => c.Name.Equals("ServiceCollectionExtension"));

				if (codeClass != null)
				{
					var aFunction = codeClass.Children.OfType<CodeFunction2>().FirstOrDefault(f => f.Name.Equals("ConfigureServices"));

					if (aFunction != null)
					{
						if (elementType == ElementType.Composite)
						{
							var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
							bool foundit = editPoint.FindPattern($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{className}>(\"{tableName}\");");
							foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

							if (!foundit)
							{
								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"services.AddSingleton<ITranslationOptions>(TranslationOptions);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									var editPoint2 = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									foundit = editPoint2.FindPattern($"var myAssembly = Assembly.GetExecutingAssembly();");
									foundit = foundit && editPoint2.LessThan(aFunction.EndPoint);

									if (foundit)
									{
										if (editPoint2.Line == editPoint.Line)
										{
											editPoint2.EndOfLine();
											editPoint2.InsertNewLine(2);
											editPoint2.Indent(null, 3);
											editPoint2.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{className}>(\"{tableName}\");");

										}
										else
										{
											editPoint2.EndOfLine();
											editPoint2.InsertNewLine();
											editPoint2.Indent(null, 3);
											editPoint2.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{className}>(\"{tableName}\");");
										}
									}
								}
							}
						}
						else
						{
							var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
							bool foundit = editPoint.FindPattern($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{className}>(\"{tableName}\");");
							foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

							if (!foundit)
							{
								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"services.AddSingleton<ITranslationOptions>(TranslationOptions);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									var editPoint2 = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									foundit = editPoint2.FindPattern($"var myAssembly = Assembly.GetExecutingAssembly();");
									foundit = foundit && editPoint2.LessThan(aFunction.EndPoint);

									if (foundit)
									{
										if (editPoint2.Line == editPoint.Line)
										{
											editPoint2.EndOfLine();
											editPoint2.InsertNewLine(2);
											editPoint2.Indent(null, 3);
											editPoint2.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{className}>(\"{tableName}\");");

										}
										else
										{
											editPoint2.EndOfLine();
											editPoint2.InsertNewLine();
											editPoint2.Indent(null, 3);
											editPoint2.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{className}>(\"{tableName}\");");
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public string GetRelativeFolder(ProjectFolder folder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Project project = GetProject(folder.ProjectName);
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

		public string CorrectForReservedNames(string columnName)
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

		public string NormalizeClassName(string className)
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

		#endregion

		#region Project Operations
		public Project GetProject(string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDTE2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			foreach (Project project in mDTE2.Solution.Projects)
			{
				if (string.Equals(project.Name, projectName, StringComparison.OrdinalIgnoreCase))
					return project;
			}

			return null;
		}

		public object GetProjectFromFolder(string folder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			foreach (Project project in mDte.Solution.Projects.OfType<Project>())
			{
				var projectPath = project.Properties.OfType<Property>().First(p =>
				{
					ThreadHelper.ThrowIfNotOnUIThread();
					return p.Name.Equals("FullPath", StringComparison.OrdinalIgnoreCase);
				}).Value.ToString().Trim('\\');

				if (folder.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
				{
					var remainder = folder.Substring(projectPath.Length).Trim('\\');

					if (string.IsNullOrWhiteSpace(remainder))
						return project;

					var parts = remainder.Split(new char[] { '\\', ':' });
					var projectItem = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(p =>
					{
						ThreadHelper.ThrowIfNotOnUIThread();
						return p.Name.Equals(parts[0]);
					});

					if (projectItem != null && 1 >= parts.Count())
						return projectItem;

					for (int j = 1; j < parts.Count(); j++)
					{
						var item = projectItem.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi =>
						{
							ThreadHelper.ThrowIfNotOnUIThread();
							return pi.Name.Equals(parts[j]);
						});

						if (item == null)
							return projectItem;
						else
							projectItem = item;
					}

					return projectItem;
				}
			}

			return null;
		}

		#endregion

		#region Find the Entity Models Folder
		/// <summary>
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <returns>The first <see cref="ProjectFolder"/> that contains an entity model, or null if none are found.</returns>
		public ProjectFolder FindEntityModelsFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in mDte.Solution.Projects)
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

			var candidates = FindProjectFolder(theCandidateNamespace);

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
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
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
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
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
		public ProjectFolder FindResourceModelsFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in mDte.Solution.Projects)
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

			var candidates = FindProjectFolder(theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			theCandidateNamespace = "*.ResourceModels";

			candidates = FindProjectFolder(theCandidateNamespace);

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
		private ProjectFolder FindResourceModelsFolder(ProjectItem parent, string projectName)
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

		public ProjectFolder FindMappingFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in mDte.Solution.Projects)
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

			var candidates = FindProjectFolder(theCandidateNamespace);

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
		private ProjectFolder FindMappingFolder(ProjectItem parent, string projectName)
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
		/// Find Controllers Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public ProjectFolder FindControllersFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in mDte2.Solution.Projects)
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

			var candidates = FindProjectFolder(theCandidateNamespace);

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
		private ProjectFolder FindControllersFolder(ProjectItem parent, string projectName)
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
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 candidateClass in namespaceElement.Members.OfType<CodeClass2>())
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
		private ProjectFolder ScanForControllers(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in namespaceElement.Members.OfType<CodeClass2>())
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
		/// Scans the project folder for an entity model
		/// </summary>
		/// <param name="parent">The <see cref="ProjectItem"/> folder to scan</param>
		/// <param name="projectName">the name of the project</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="ProjectItem"/> folder if the folder contains an entity model</returns>
		private ProjectFolder ScanForMapping(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in namespaceElement.Members.OfType<CodeClass2>())
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
		private ProjectFolder ScanForMapping(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in namespaceElement.Members.OfType<CodeClass2>())
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
		private ProjectFolder ScanForResource(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 candidateClass in namespaceElement.Members.OfType<CodeClass2>())
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
		private ProjectFolder ScanForResource(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 candidateClass in namespaceElement.Members.OfType<CodeClass2>())
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

		#region Example Functions
		/// <summary>
		/// Get the validator interface name for a resource
		/// </summary>
		/// <param name="resourceClassName">The resource class whos validator is to be found</param>
		/// <param name="folder">The folder to search</param>
		/// <returns>The name of the interface for the validator of the resource.</returns>
		public CodeClass2 FindExampleCode(ResourceClass parentModel, string folder = "")
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var projectMapping = codeService.LoadProjectMapping();                        //	Contains the names and projects where various source file exist.
			var ExamplesFolder = projectMapping.GetExamplesFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(ExamplesFolder.Folder) :
																	  mDte.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 codeFile = FindExampleCode(parentModel, projectItem.Name);

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
		public CodeClass2 FindProfileCode(ResourceClass parentModel, string folder = "")
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var projectMapping = LoadProjectMapping();  //	Contains the names and projects where various source file exist.
			var ProfileFolder = projectMapping.GetMappingFolder();

			var profileFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(ProfileFolder.Folder) :
																	mDte.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in profileFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 codeFile = FindProfileCode(parentModel, projectItem.Name);

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
							var constructor = codeClass.Children.OfType<CodeFunction2>().FirstOrDefault(f => f.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

							EditPoint2 editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

							bool foundit = editPoint.FindPattern($"CreateMap<{parentModel.ClassName}, {parentModel.Entity.ClassName}>()");
							foundit = foundit && editPoint.LessThan(constructor.EndPoint);

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
		public CodeClass2 FindCollectionExampleCode(ResourceClass parentModel, string folder = "")
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var projectMapping = codeService.LoadProjectMapping();                        //	Contains the names and projects where various source file exist.
			var ExamplesFolder = projectMapping.GetExamplesFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(ExamplesFolder.Folder) :
																	  mDte.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 codeFile = FindCollectionExampleCode(parentModel, projectItem.Name);

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

		public string GetExampleModel(int skipRecords, ResourceClass resourceModel, DBServerType serverType, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (serverType == DBServerType.MYSQL)
				return GetMySqlExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.POSTGRESQL)
				return GetPostgresExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.SQLSERVER)
				return GetSQLServerExampleModel(skipRecords, resourceModel, connectionString);

			throw new ArgumentException("Invalid or unrecognized DBServerType", "serverType");
		}

		public string GetMySqlExampleModel(int skipRecords, ResourceClass resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public string GetPostgresExampleModel(int skipRecords, ResourceClass resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public string GetSQLServerExampleModel(int skipRecords, ResourceClass resourceModel, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			StringBuilder results = new StringBuilder();
			var entityColumns = resourceModel.Entity.Columns;

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();

				var query = new StringBuilder();
				query.Append("select ");

				bool first = true;
				foreach (var column in entityColumns)
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

				if (string.IsNullOrWhiteSpace(resourceModel.Entity.SchemaName))
				{
					query.Append($" from [{resourceModel.Entity.TableName}]");
				}
				else
				{
					query.Append($" from [{resourceModel.Entity.SchemaName}].[{resourceModel.Entity.TableName}]");
				}

				query.Append(" order by ");

				first = true;
				foreach (var column in entityColumns)
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


				using (var command = new SqlCommand(query.ToString(), connection))
				{
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							results = BuildModelFromReader(entityColumns, reader);
						}
						else
						{
							results = BuildModelFromMetadata(skipRecords, resourceModel, entityColumns);
						}
					}
				}
			}

			return results.ToString();
		}

		private StringBuilder BuildModelFromReader(DBColumn[] entityColumns, SqlDataReader reader)
		{
			StringBuilder results = new StringBuilder();
			bool first;
			results.AppendLine("{");
			first = true;

			foreach (var column in entityColumns)
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

			results.AppendLine();
			results.AppendLine("}");
			return results;
		}

		private StringBuilder BuildModelFromMetadata(int skipRecords, ResourceClass resourceClass, DBColumn[] entityColumns)
		{
			StringBuilder results = new StringBuilder();
			bool first = true;
			var rnd = new Random(DateTime.Now.Second);

			results.AppendLine("{");

			foreach (var column in entityColumns)
			{
				if (first)
					first = false;
				else
					results.AppendLine(",");
				results.Append($"\t\"{column.ColumnName}\": ");

				switch (column.DBDataType.ToLower())
				{
					case "bigint":
						if (column.IsPrimaryKey)
						{
							if (skipRecords > 0)
								results.Append(skipRecords.ToString());
							else
								results.Append(rnd.Next(5, 25487).ToString());
						}
						else
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
						if (column.IsPrimaryKey)
						{
							if (skipRecords > 0)
								results.Append(skipRecords.ToString());
							else
								results.Append(rnd.Next(5, 25487).ToString());
						}
						else
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
					case "nchar":
					case "varchar":
						{
							var Value = $"{resourceClass.ClassName} {column.ColumnName}";

							if (Value.Length > column.Length)
								Value = Value.Substring(0, Convert.ToInt32(column.Length));

							results.Append($"\"{Value}\"");
						}
						break;

					case "char":
						{
							if (column.Length == 1)
								results.Append($"'A'");
							else
							{
								var Value = $"{resourceClass.ClassName} {column.ColumnName}";

								if (Value.Length > column.Length)
									Value = Value.Substring(0, Convert.ToInt32(column.Length));

								results.Append($"\"{Value}\"");
							}

						}
						break;

					case "xml":
						{
							var Value = "<xml></xml>";
							results.Append($"\"{Value}\"");
						}
						break;

					default:
						throw new InvalidDataException($"Unrecognized database type: {column.ModelDataType}");
				}
			}

			results.AppendLine();
			results.AppendLine("}");
			return results;
		}

		public string ResolveMapFunction(JObject entityJson, string columnName, DBColumn[] entityColumns, ResourceClass model, string mapFunction)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			bool isDone = false;
			var originalMapFunction = mapFunction;
			var valueNumber = 1;
			List<string> valueAssignments = new List<string>();
			var resourceColumns = model.Columns;

			var linkConversion = ExtractLinkConversion(entityJson, columnName, model, resourceColumns, entityColumns);

			if (!string.IsNullOrWhiteSpace(linkConversion))
				return linkConversion;

			var enumConversion = ExtractEnumConversion(entityJson, columnName, resourceColumns);

			if (!string.IsNullOrWhiteSpace(enumConversion))
				return enumConversion;

			var simpleConversion = ExtractSimpleConversion(entityJson, model, entityColumns, mapFunction);

			if (!string.IsNullOrWhiteSpace(simpleConversion))
				return simpleConversion;

			var wellKnownConversion = ExtractWellKnownConversion(entityJson, entityColumns, model, mapFunction);

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

					var entityColumn = entityColumns.FirstOrDefault(c => c.ColumnName.Equals(entityColumnReference.Value, StringComparison.OrdinalIgnoreCase));
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

		private string ExtractEnumConversion(JObject entityJson, string columnName, DBColumn[] resourceColumns)
		{
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var column = resourceColumns.FirstOrDefault(c => c.ColumnName.Equals(columnName));
			var enumClassName = column.ModelDataType.Trim('?');
			var parentResource = codeService.ResourceClassList.FirstOrDefault(r => r.ClassName.Equals(enumClassName));

			if (parentResource != null && parentResource.ResourceType == ResourceType.Enum)
			{
				StringBuilder conversion = new StringBuilder($"{parentResource.ClassName}.");
				var jsonValue = entityJson[columnName].Value<string>();

				if (jsonValue == null)
					return "null";

				foreach (var colValue in parentResource.Columns)
				{
					if (jsonValue.Equals(colValue.ToString(), StringComparison.OrdinalIgnoreCase))
					{
						conversion.Append(colValue);
						return conversion.ToString();
					}
				}

				if (Int64.TryParse(jsonValue, out long jValue))
				{
					foreach (var colValue in parentResource.Columns)
					{
						if (Int64.TryParse(colValue.DBDataType, out long cValue))
						{
							if (jValue == cValue)
							{
								conversion.Append(colValue);
								return conversion.ToString();
							}
						}
					}
				}

				conversion.Append(parentResource.Columns.ToList()[0]);
				return conversion.ToString();
			}

			return String.Empty;
		}

		public string ExtractLinkConversion(JObject entityJson, string columnName, ResourceClass model, DBColumn[] resourceColumns, DBColumn[] entityColumns)
		{
			var column = resourceColumns.FirstOrDefault(c => c.ColumnName.Equals(columnName));

			if (column.IsPrimaryKey)
			{
				var nn = new NameNormalizer(model.ClassName);
				var conversion = new StringBuilder($"new Uri(rootUrl, \"{nn.PluralCamelCase}/id");

				var primaryKeyColumns = entityColumns.Where(c => c.IsPrimaryKey);

				foreach (var keyColumn in primaryKeyColumns)
				{
					var theValue = entityJson[keyColumn.ColumnName].Value<string>();
					conversion.Append($"/{theValue}");
				}

				conversion.Append("\")");
				return conversion.ToString();
			}
			else if (column.IsForeignKey)
			{
				var foreignKeyColumns = entityColumns.Where(c => !string.IsNullOrWhiteSpace(c.ForeignTableName) && c.ForeignTableName.Equals(column.ForeignTableName));
				var nn = new NameNormalizer(column.ForeignTableName);
				var conversion = new StringBuilder($"new Uri(rootUrl, \"{nn.PluralCamelCase}/id");

				foreach (var keyColumn in foreignKeyColumns)
				{
					var theValue = entityJson[keyColumn.ColumnName].Value<string>();
					conversion.Append($"/{theValue}");
				}

				conversion.Append("\")");
				return conversion.ToString();
			}

			return string.Empty;
		}

		private static string ExtractSimpleConversion(JObject entityJson, ResourceClass model, DBColumn[] entityColumns, string mapFunction)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				if (mapFunction.Equals(ef.Groups["replace"].Value))
				{
					var token = entityJson[ef.Groups["entity"].Value];
					var entityColumn = entityColumns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));

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

		public static string ExtractWellKnownConversion(JObject entityJson, DBColumn[] entityColumns, ResourceClass model, string mapFunction)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				var token = entityJson[ef.Groups["entity"].Value];
				var entityColumn = entityColumns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));
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

		public static string ExtractSimpleConversion(JObject entityJson, ResourceClass model, string mapFunction)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				if (mapFunction.Equals(ef.Groups["replace"].Value))
				{
					var token = entityJson[ef.Groups["entity"].Value];
					var entityColumn = model.Entity.Columns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));

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

		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public ProjectFolder FindExampleFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in mDte.Solution.Projects)
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

			var candidates = FindProjectFolder(theCandidateNamespace);

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
		private ProjectFolder FindExampleFolder(ProjectItem parent, string projectName)
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
		/// Scans the projects root folder for a validator class
		/// </summary>
		/// <param name="parent">The <see cref="Project"/> to scan</param>
		/// <returns>Returns the <see cref="ProjectFolder"/> for the <see cref="Project"/> if the root folder contains an entity model</returns>
		private ProjectFolder ScanForExample(Project parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in namespaceElement.Members.OfType<CodeClass2>())
						{
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
		private ProjectFolder ScanForExample(ProjectItem parent, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem candidate in parent.ProjectItems)
			{
				if (candidate.Kind == Constants.vsProjectItemKindPhysicalFile &&
					candidate.FileCodeModel != null &&
					candidate.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(candidate.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
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
		/// Find the project folder associated with the namespace
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> that contains the projects</param>
		/// <param name="destinationNamespace">The <see langword="namespace"/> to search for.</param>
		/// <returns>The collection of <see cref="ProjectFolder"/>s that contains the namespace</returns>
		public List<ProjectFolder> FindProjectFolder(string destinationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var projectFolderCollection = new List<ProjectFolder>();

			foreach (Project project in mDte.Solution.Projects)
			{
				try
				{
					var projectNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();
					string targetNamespace = destinationNamespace;


					var searchTemplate = targetNamespace.Replace(".", "\\.").Replace("*", "[a-zA-Z_0-9]+");

					var match = Regex.Match(projectNamespace, searchTemplate);

					if (match.Success)
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
				catch (Exception error)
				{
					Console.WriteLine(error.Message);
				}
			}

			return projectFolderCollection;
		}
		#endregion

		#region Models Functions
		public DBColumn[] LoadEntityEnumColumns(CodeEnum enumElement)
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var columns = new List<DBColumn>();

			foreach (CodeVariable2 enumVariable in enumElement.Children.OfType<CodeVariable2>())
			{
				var dbColumn = new DBColumn
				{
					ColumnName = enumVariable.Name,
					DBDataType = enumVariable.InitExpression.ToString()
				};

				CodeAttribute2 pgNameAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgName"));

				if (pgNameAttribute != null)
				{
					var matchit = Regex.Match(pgNameAttribute.Value, "\\\"(?<pgName>[_A-Za-z][A-Za-z0-9_]*)\\\"");

					if (matchit.Success)
						dbColumn.EntityName = matchit.Groups["pgName"].Value;
				}

				columns.Add(dbColumn);
			}

			return columns.ToArray();
		}

		public DBColumn[] LoadResourceEnumColumns(ResourceClass resource)
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            CodeEnum enumElement = (CodeEnum)resource.Resource;
			var columns = new List<DBColumn>();

			foreach (CodeVariable2 enumVariable in enumElement.Children.OfType<CodeVariable2>())
			{
				var dbColumn = new DBColumn
				{
					ColumnName = enumVariable.Name,
					DBDataType = enumVariable.InitExpression.ToString()
				};

				columns.Add(dbColumn);
			}

			return columns.ToArray();
		}

		private void LoadEntityTableAssociations(DTE2 dte, ref Dictionary<string, string> emap, ProjectItem parentFolder = null)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			ProjectItems projectItems = null;

			if (parentFolder == null)
			{
				var projectMapping = LoadProjectMapping();    //	Contains the names and projects where various source file exist.
				var EntityModelsFolder = projectMapping.GetEntityModelsFolder();
				var project = GetProject(EntityModelsFolder.ProjectName);
				projectItems = project.ProjectItems;
			}
			else
				projectItems = parentFolder.ProjectItems;

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
				{
					LoadEntityTableAssociations(dte, ref emap, projectItem);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 childElement in namespaceElement.Members.OfType<CodeClass2>())
						{
							var attribute = childElement.Children.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

							if (attribute != null)
							{
								var match = Regex.Match(attribute.Value, "\"(?<tablename>[a-zA-Z0-9_]+)\"");

								if (match.Success)
								{
									emap.Add(childElement.Name, match.Groups["tablename"].Value);
								}
							}
						}
					}
				}
			}
		}

		private void LoadResourceEntityAssociations(DTE2 dte, ref Dictionary<string, string> rmap, ProjectItem parentFolder = null)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			ProjectItems projectItems = null;

			if (parentFolder == null)
			{
				var projectMapping = LoadProjectMapping();    //	Contains the names and projects where various source file exist.
				var ResourceModelsFolder = projectMapping.GetResourceModelsFolder();
				var project = GetProject(ResourceModelsFolder.ProjectName);
				projectItems = project.ProjectItems;
			}
			else
				projectItems = parentFolder.ProjectItems;

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
				{
					LoadResourceEntityAssociations(dte, ref rmap, projectItem);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 childElement in namespaceElement.Members.OfType<CodeClass2>())
						{
							var entityAttribute = childElement.Children.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));

							if (entityAttribute != null)
							{
								Match match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityName>[a-zA-Z0-9_]+)\\)");

								if (match.Success)
								{
									rmap.Add(childElement.Name, match.Groups["entityName"].Value);
								}
							}
						}
					}
				}
			}
		}

		public DBColumn[] LoadEntityColumns(CodeClass2 codeClass)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var columns = new List<DBColumn>();

			foreach (CodeProperty2 property in codeClass.Children.OfType<CodeProperty2>())
			{
				var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				CodeAttribute2 memberAttribute = property.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Member"));

				var dbColumn = new DBColumn
				{
					ColumnName = property.Name,
					EntityName = property.Name,
					ModelDataType = parts[parts.Count() - 1]
				};

				if (memberAttribute != null)
				{
					var isPrimaryKeyArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsPrimaryKey"));

					if (isPrimaryKeyArgument != null)
						dbColumn.IsPrimaryKey = isPrimaryKeyArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var isIdentityArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsIdentity"));

					if (isIdentityArgument != null)
						dbColumn.IsIdentity = isIdentityArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var autoFieldArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("AutoField"));

					if (autoFieldArgument != null)
						dbColumn.IsComputed = autoFieldArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var isIndexedArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsIndexed"));

					if (isIndexedArgument != null)
						dbColumn.IsIndexed = isIndexedArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var isNullableArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsNullable"));

					if (isNullableArgument != null)
						dbColumn.IsNullable = isNullableArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var isFixedArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsFixed"));

					if (isFixedArgument != null)
						dbColumn.IsFixed = isFixedArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var isForeignKeyArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("IsForeignKey"));

					if (isForeignKeyArgument != null)
						dbColumn.IsForeignKey = isForeignKeyArgument.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

					var nativeDataTypeArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("NativeDataType"));

					if (nativeDataTypeArgument != null)
						dbColumn.DBDataType = nativeDataTypeArgument.Value.Trim(new char[] { '"' });

					var lengthArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("Length"));

					if (lengthArgument != null)
						dbColumn.Length = Convert.ToInt32(lengthArgument.Value);

					var foreignTableNameArgument = memberAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("ForeignTableName"));

					if (foreignTableNameArgument != null)
						dbColumn.ForeignTableName = foreignTableNameArgument.Value.Trim(new char[] { '"' });
				}

				columns.Add(dbColumn);
			}

			return columns.ToArray();
		}

		public DBColumn[] LoadResourceColumns(ResourceClass resource)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var columns = new List<DBColumn>();
			var codeClass = (CodeClass2)resource.Resource;

			foreach (CodeProperty2 property in codeClass.Children.OfType<CodeProperty2>())
			{
				var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				var dbColumn = new DBColumn
				{
					ColumnName = property.Name,
					EntityName = property.Name,
					ModelDataType = parts[parts.Count() - 1]
				};

				if (property.Name.Equals("HRef", StringComparison.OrdinalIgnoreCase))
					dbColumn.IsPrimaryKey = true;
				else
				{
					if (parts[parts.Count() - 1].Equals("Uri", StringComparison.OrdinalIgnoreCase))
					{
						var referenceModel = codeService.GetResourceClass(property.Name);

						if (referenceModel != null)
						{
							dbColumn.IsForeignKey = true;
							dbColumn.ForeignTableName = referenceModel.Entity.TableName;
						}
					}
				}

				columns.Add(dbColumn);
			}

			return columns.ToArray();
		}
		#endregion

		#region Mapping Functions
		private ProjectMapping AutoFillProjectMapping(ProjectMapping projectMapping)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var installationFolder = InstallationFolder;

			if (string.IsNullOrWhiteSpace(projectMapping.EntityFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.EntityNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.EntityProject))
			{
				var modelFolder = FindEntityModelsFolder();
				projectMapping.EntityFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.EntityNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.EntityProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ResourceFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ResourceNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ResourceProject))
			{
				var modelFolder = FindResourceModelsFolder();
				projectMapping.ResourceFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ResourceNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ResourceProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.MappingFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.MappingNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.MappingProject))
			{
				var modelFolder = FindMappingFolder();
				projectMapping.MappingFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.MappingNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.MappingProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ExampleFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ExampleNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ExampleProject))
			{
				var modelFolder = FindExampleFolder();
				projectMapping.ExampleFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ExampleNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ExampleProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ControllersFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ControllersNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ControllersProject))
			{
				var modelFolder = FindControllersFolder();
				projectMapping.ControllersFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ControllersNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ControllersProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			return projectMapping;
		}

		/// <summary>
		/// Load the <see cref="ProfileMap"/> for the resource
		/// </summary>
		/// <param name="_dte2">The <see cref="DTE2"/> Visual Studio interface</param>
		/// <param name="resourceModel">The <see cref="ResourceModel"/> whose <see cref="ProfileMap"/> is to be loaded.</param>
		/// <returns>The <see cref="ProfileMap"/> for the <see cref="ResourceModel"/></returns>
		public ProfileMap OpenProfileMap(ResourceClass resourceModel, out bool isAllDefined)
		{
            ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			CodeClass2 codeClass = FindProfileCode(resourceModel);
			isAllDefined = true;
			var unmappedResources = new List<string>();
			var unmappedEntities = new List<string>();

			foreach (var column in resourceModel.Columns)
				unmappedResources.Add(column.ColumnName);

			foreach (var column in resourceModel.Entity.Columns)
				unmappedEntities.Add(column.ColumnName);

			if (codeClass != null)
			{
				var profileMap = new ProfileMap()
				{
					ResourceClassName = resourceModel.ClassName,
					EntityClassName = resourceModel.Entity.ClassName,
					ResourceProfiles = new List<ResourceProfile>(),
					EntityProfiles = new List<EntityProfile>()
				};

				var constructor = codeClass.Children.OfType<CodeFunction2>().FirstOrDefault(f => f.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

				//	Read Edit Profile
				EditPoint2 editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

				bool foundit = editPoint.FindPattern($"CreateMap<{resourceModel.ClassName}, {resourceModel.Entity.ClassName}>()");
				foundit = foundit && editPoint.LessThan(constructor.EndPoint);

				if (foundit)
				{
					editPoint.EndOfLine();
					editPoint.LineDown();

					while (!IsEmptyLine(editPoint))
					{
						var theLine = GetText(ref editPoint);
						var match = Regex.Match(theLine, "\\.ForMember\\([a-zA-Z0-9_]+[ \t\r\n]*\\=\\>[ \t\r\n]*[a-zA-Z0-9_]+\\.(?<entityMember>[a-zA-Z0-9_]+)\\,[ \t\r\n]*opts[ \t\r\n]*\\=\\>[ \t\r\n]*opts\\.MapFrom\\((?<source>[a-zA-Z0-9_]+)[ \t\r\n]*\\=\\>[ \t\r\n]*(?<mapfunction>.+)\\)\\)", RegexOptions.Multiline);

						if (match.Success)
						{
							var sourceDesignation = match.Groups["source"].Value;

							var entityProfile = new EntityProfile
							{
								EntityColumnName = match.Groups["entityMember"].Value,
								MapFunction = match.Groups["mapfunction"].Value,
								IsDefined = true,
								ResourceColumns = Array.Empty<string>()
							};

							var mapFunction = entityProfile.MapFunction;

							bool isDone = false;

							while (!isDone)
							{
								var match2 = Regex.Match(mapFunction, $"{sourceDesignation}\\.(?<resourceColumn>[a-zA-Z0-9_]+)", RegexOptions.Multiline);

								if (match2.Success)
								{
									var resourceColumnName = match2.Groups["resourceColumn"].Value;
									mapFunction = mapFunction.Substring(match2.Index + match2.Length);

									if (!entityProfile.ResourceColumns.Contains(resourceColumnName))
									{
										List<string> resourceColumns = new List<string>();
										resourceColumns.AddRange(entityProfile.ResourceColumns);
										resourceColumns.Add(resourceColumnName);
										entityProfile.ResourceColumns = resourceColumns.ToArray();
									}

									unmappedResources.Remove(resourceColumnName);
								}
								else
									isDone = true;
							}

							profileMap.EntityProfiles.Add(entityProfile);
						}
					}
				}

				//	Read Resource Profile
				editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

				foundit = editPoint.FindPattern($"CreateMap<{resourceModel.Entity.ClassName}, {resourceModel.ClassName}>()");
				foundit = foundit && editPoint.LessThan(constructor.EndPoint);

				if (foundit)
				{
					editPoint.EndOfLine();
					editPoint.LineDown();

					while (!IsEmptyLine(editPoint))
					{
						var theLine = GetText(ref editPoint);
						var match = Regex.Match(theLine, "\\.ForMember\\([a-zA-Z0-9_]+[ \t\r\n]*\\=\\>[ \t\r\n]*[a-zA-Z0-9_]+\\.(?<resourceMember>[a-zA-Z0-9_]+)\\,[ \t\r\n]*opts[ \t\r\n]*\\=\\>[ \t\r\n]*opts\\.MapFrom\\((?<source>[a-zA-Z0-9_]+)[ \t\r\n]*\\=\\>[ \t\r\n]*(?<mapfunction>.+)\\)\\)", RegexOptions.Multiline);

						if (match.Success)
						{
							var sourceDesignation = match.Groups["source"].Value;

							var reourceProfile = new ResourceProfile
							{
								ResourceColumnName = match.Groups["resourceMember"].Value,
								MapFunction = match.Groups["mapfunction"].Value,
								IsDefined = true,
								EntityColumnNames = Array.Empty<string>()
							};

							var mapFunction = reourceProfile.MapFunction;

							bool isDone = false;

							while (!isDone)
							{
								var match2 = Regex.Match(mapFunction, $"{sourceDesignation}\\.(?<entityColumn>[a-zA-Z0-9_]+)", RegexOptions.Multiline);

								if (match2.Success)
								{
									var entityColumnName = match2.Groups["entityColumn"].Value;
									mapFunction = mapFunction.Substring(match2.Index + match2.Length);

									if (!reourceProfile.EntityColumnNames.Contains(entityColumnName))
									{
										List<string> resourceColumns = new List<string>();
										resourceColumns.AddRange(reourceProfile.EntityColumnNames);
										resourceColumns.Add(entityColumnName);
										reourceProfile.EntityColumnNames = resourceColumns.ToArray();
									}

									unmappedEntities.Remove(entityColumnName);
								}
								else
									isDone = true;
							}

							profileMap.ResourceProfiles.Add(reourceProfile);
						}
					}
				}

				if (unmappedEntities.Count > 0)
					isAllDefined = false;

				if (unmappedResources.Count > 0)
					isAllDefined = false;

				return profileMap;
			}

			return null;

		}

		private static bool IsEmptyLine(EditPoint2 editPoint)
		{
			EditPoint2 startOfLine = (EditPoint2)editPoint.CreateEditPoint();
			startOfLine.StartOfLine();
			EditPoint2 endOFLine = (EditPoint2)editPoint.CreateEditPoint();
			endOFLine.EndOfLine();

			var text = startOfLine.GetText(endOFLine);
			if (string.IsNullOrWhiteSpace(text))
				return true;

			return false;
		}

		private static string GetText(ref EditPoint2 editPoint)
		{
			StringBuilder theText = new StringBuilder();

			EditPoint2 startOfLine = (EditPoint2)editPoint.CreateEditPoint();
			startOfLine.StartOfLine();
			EditPoint2 endOFLine = (EditPoint2)editPoint.CreateEditPoint();
			endOFLine.EndOfLine();

			string thisLine = startOfLine.GetText(endOFLine);

			if (!thisLine.Trim().StartsWith(".ForMember"))
				return string.Empty;

			theText.Append(thisLine);

			bool isCodeComplete = false;

			while (!isCodeComplete)
			{
				editPoint.LineDown();

				if (IsEmptyLine(editPoint))
				{
					isCodeComplete = true;
				}
				else
				{
					startOfLine = (EditPoint2)editPoint.CreateEditPoint();
					startOfLine.StartOfLine();
					endOFLine = (EditPoint2)editPoint.CreateEditPoint();
					endOFLine.EndOfLine();

					string nextLineText = startOfLine.GetText(endOFLine);

					if (nextLineText.Trim().StartsWith(".ForMember"))
					{
						isCodeComplete = true;
					}
					else
					{
						theText.AppendLine();
						theText.Append(nextLineText);
					}
				}
			}

			return theText.ToString().Trim();
		}

		public ProfileMap GenerateProfileMap(ResourceClass resourceModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var profileMap = new ProfileMap
			{
				ResourceClassName = resourceModel.ClassName,
				EntityClassName = resourceModel.Entity.ClassName,
				ResourceProfiles = new List<ResourceProfile>(),
				EntityProfiles = new List<EntityProfile>()
			};

			profileMap.ResourceProfiles.AddRange(GenerateResourceFromEntityMapping(resourceModel));
			profileMap.EntityProfiles.AddRange(GenerateEntityFromResourceMapping(resourceModel, profileMap));

			return profileMap;
		}

		/// <summary>
		/// Generates a mapping to construct the entity member from the corresponding resource members
		/// </summary>
		/// <param name="unmappedColumns"></param>
		/// <param name="resourceModel"></param>
		public static List<EntityProfile> GenerateEntityFromResourceMapping(ResourceClass resourceModel, ProfileMap profileMap)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var result = new List<EntityProfile>();
			var unmappedMembers = new List<String>();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var entityColumns = resourceModel.Entity.Columns;

			foreach (var column in entityColumns)
				unmappedMembers.Add(column.ColumnName);

			//  Let's create a mapping for each entity member
			foreach (var entityMember in entityColumns)
			{
				//  Now, construct the mapping
				if (entityMember.IsPrimaryKey)
				{
					var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(entityMember.EntityName, StringComparison.OrdinalIgnoreCase));

					if (!string.IsNullOrWhiteSpace(matchedColumn))
					{
						unmappedMembers.Remove(matchedColumn);

						//  Construct a data row for this entity member, and populate the column name
						var entityProfile = new EntityProfile
						{
							EntityColumnName = entityMember.ColumnName
						};

						//  This entity member is part of the primary key. In the resource model, the primary
						//  key is contained in the hRef. Therefore, the formula to extract the value of this
						//  entity member is going to take the form: source.HRef.GetId<datatype>(n), were datatype
						//  is the datatype of this entity member, and n is the position of this member in the 
						//  HRef, counting backwards.
						//
						//  For example, suppose we have the HRef = /resourcename/id/10/20
						//  Where 10 is the dealer id and it is an int.
						//  And 20 is the user id and it is a short.
						//
						//  To extract the dealer id, the formula would be: source.HRef.GetId<int>(1)
						//  To extract the user id, the formula would be: source.HRef.GetId<short>(0)
						string dataType = "Unknown";

						//  We're going to need that datatype. Get it here.
						dataType = entityMember.ModelDataType;

						//  We need the list of all the primary keys in the entity model, so that we know the
						//  position of this member.
						var primaryKeys = entityColumns.Where(c => c.IsPrimaryKey);

						if (primaryKeys.Count() == 1)
						{
							//  There is only one primary key element, so we don't need to bother with the count.
							entityProfile.MapFunction = $"source.HRef == null ? default : source.HRef.GetId<{dataType}>()";
							entityProfile.ResourceColumns = new string[] { "HRef" };
							entityProfile.IsDefined = true;
						}
						else
						{
							var formula = new StringBuilder($"source.HRef == null ? default : source.HRef.GetId<{dataType}>(");

							//  Compute the index and append it to the above formula.
							if (primaryKeys.Count() > 1)
							{
								var index = primaryKeys.Count() - 1;

								foreach (var pk in primaryKeys)
								{
									if (pk == entityMember)
										formula.Append(index.ToString());
									else
										index--;
								}
							}

							//  Close the formula
							formula.Append(")");
							entityProfile.MapFunction = formula.ToString();
							entityProfile.ResourceColumns = new string[] { "HRef" };
							entityProfile.IsDefined = true;
						}

						result.Add(entityProfile);
					}
				}
				else if (entityMember.IsForeignKey)
				{
					//  This is a special case of a foreign key. The first challenge is to discover which resource member
					//  is used to represent this foreign key. In all likelyhood, it will be the resource member that has
					//  the same name as the foreign table that this foreign key represents. It's probably going to be the
					//  single form, but it could be the plural form. Look for either one.
					var nnx = new NameNormalizer(entityMember.ForeignTableName);
					DBColumn resourceMember = null;

					foreach (var rmap in profileMap.ResourceProfiles)
					{
						if (rmap.EntityColumnNames != null)
						{
							foreach (var entityColumnUsed in rmap.EntityColumnNames)
							{
								if (entityColumnUsed.Equals(entityMember.ColumnName, StringComparison.OrdinalIgnoreCase))
								{
									var resourceMemberName = rmap.ResourceColumnName;
									resourceMember = resourceModel.Columns.FirstOrDefault(c =>
										string.Equals(c.ColumnName, resourceMemberName, StringComparison.OrdinalIgnoreCase));

									break;
								}
							}
						}

						if (resourceMember != null)
							break;
					}

					if (resourceMember != null)
					{
						var matchedMember = unmappedMembers.FirstOrDefault(c => c.Equals(entityMember.EntityName));

						if (!string.IsNullOrWhiteSpace(matchedMember))
						{
							unmappedMembers.Remove(matchedMember);

							//  Construct a data row for this entity member, and populate the column name
							var entityProfile = new EntityProfile
							{
								EntityColumnName = entityMember.ColumnName
							};

							//  We found a resource member.
							//
							//  Foreign keys generally come in one of two forms. Either it is a reference to a primary key
							//  in a foreign table, or it is an enumeration. If it is a reference to a primary key in a 
							//  foreign table, then it will be a Uri.

							if (string.Equals(resourceMember.ModelDataType.ToString(), "uri", StringComparison.OrdinalIgnoreCase))
							{
								//  This is an href. First, get the data type.
								string dataType = entityMember.ModelDataType;

								//  Now, we need the list of entity members that correspond to this resource member.
								//  To get that, we need to look at the resource mapping.

								//  This is an href. Very much like the primary key, there can be more than one single 
								//  element in this href. 

								var foreignKeys = entityColumns.Where(c => c.IsForeignKey &&
													string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

								if (foreignKeys.Count() == 1)
								{
									var foreignKey = foreignKeys.First();
									entityProfile.MapFunction = $"source.{resourceMember.ColumnName}.GetId<{dataType}>()";
									entityProfile.ResourceColumns = new string[] { resourceMember.ColumnName };
									entityProfile.IsDefined = true;
								}
								else
								{
									var formula = new StringBuilder($"source.{resourceMember.ColumnName}.GetId<{dataType}>(");
									var foreignKeyList = new List<string>();

									if (foreignKeys.Count() > 1)
									{
										var index = foreignKeys.Count() - 1;

										foreach (var pk in foreignKeys)
										{
											var foreignKey = foreignKeys.ToList()[index];
											foreignKeyList.Add(foreignKey.ColumnName);

											if (pk == entityMember)
												formula.Append(index.ToString());
											else
												index--;
										}
									}

									formula.Append(")");
									entityProfile.MapFunction = formula.ToString();
									entityProfile.ResourceColumns = foreignKeyList.ToArray();
									entityProfile.IsDefined = true;
								}
							}
							else
							{
								//  The resource member is not a URI. It should be an enum. If it is, we sould be able to
								//  find it in our resource models list.

								var referenceModel = codeService.GetResourceClass(resourceMember.ModelDataType.ToString());

								if (referenceModel != null)
								{
									if (referenceModel.ResourceType == ResourceType.Enum)
									{
										var foreignKeys = entityColumns.Where(c => c.IsForeignKey &&
											string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

										//  If the resource member is an enum that represents the value of the primary key of a foreign table,
										//  then that foreign key can only have one member. If it has more than one member, then this 
										//  is not the proper mapping.
										if (foreignKeys.Count() == 1)
										{
											string dataType = entityMember.ModelDataType;

											var formula = $"Convert.ChangeType(source.{resourceMember.ColumnName}, source.{resourceMember.ColumnName}.GetTypeCode())";
											entityProfile.MapFunction = formula.ToString();
											entityProfile.ResourceColumns = new string[] { "unknown" };
											entityProfile.IsDefined = false;
										}
									}
								}
							}

							result.Add(entityProfile);
						}
					}
				}
				else
				{
					//  Is there a corresponding Entity Column for this resource Column?
					var resourceMember = resourceModel.Columns.FirstOrDefault(u =>
															u.ColumnName.Equals(entityMember.ColumnName, StringComparison.OrdinalIgnoreCase));

					var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(entityMember.EntityName, StringComparison.OrdinalIgnoreCase));

					if (!string.IsNullOrWhiteSpace(matchedColumn))
					{
						unmappedMembers.Remove(matchedColumn);
						//  Construct a data row for this entity member, and populate the column name
						var entityProfile = new EntityProfile
						{
							EntityColumnName = entityMember.ColumnName
						};

						if (resourceMember != null)
						{
							MapResourceDestinationFromSource(entityMember, entityProfile, resourceMember);
						}
						else
						{
							var rp = profileMap.ResourceProfiles.FirstOrDefault(c => c.MapFunction.IndexOf(entityMember.ColumnName, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

							if (rp != null)
							{
								if (rp.ResourceColumnName.Contains("."))
								{
									var parts = rp.ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
									StringBuilder mapFunction = new StringBuilder();
									StringBuilder parent = new StringBuilder("");
									string NullValue = "null";

									var parentModel = GetParentModel(resourceModel, parts);

									if (parentModel != null)
									{
										var parentColumn = parentModel.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[parts.Count() - 1], StringComparison.OrdinalIgnoreCase));

										if (parentColumn != null)
										{
											if (string.Equals(parentColumn.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
											{
												NullValue = "string.Empty";
											}
											else
											{
												var theDataType = Type.GetType(parentColumn.ModelDataType.ToString());

												if (theDataType != null)
												{
													NullValue = "default";
												}
												else
												{
													NullValue = "default";
												}
											}
										}
									}

									for (int i = 0; i < parts.Count() - 1; i++)
									{
										var parentClass = parts[i];

										if (string.IsNullOrWhiteSpace(parent.ToString()))
											mapFunction.Append($"source.{parentClass} == null ? {NullValue} : ");
										else
											mapFunction.Append($"source.{parent}.{parentClass} == null ? {NullValue} : ");
										parent.Append($"source.{parentClass}");

									}

									mapFunction.Append($"{parent}.{parts[parts.Count() - 1]}");
									entityProfile.MapFunction = mapFunction.ToString();

									StringBuilder childColumn = new StringBuilder();

									foreach (var p in parts)
									{
										if (childColumn.Length > 0)
											childColumn.Append(".");
										childColumn.Append(p);
									}

									var cc = parentModel.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, childColumn.ToString(), StringComparison.OrdinalIgnoreCase));
									if (cc != null)
									{
										entityProfile.ResourceColumns = new string[] { cc.ColumnName };
									}
								}
								else
								{
									StringBuilder mc = new StringBuilder();

									resourceMember = resourceModel.Columns.FirstOrDefault(c =>
										string.Equals(c.ModelDataType.ToString(), rp.ResourceColumnName, StringComparison.OrdinalIgnoreCase));

									MapResourceDestinationFromSource(entityMember, entityProfile, resourceMember);
								}
							}
						}

						result.Add(entityProfile);
					}
				}
			}

			return result;
		}

		public static List<ResourceProfile> GenerateResourceFromEntityMapping(ResourceClass resourceModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var nn = new NameNormalizer(resourceModel.ClassName);
			var result = new List<ResourceProfile>();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var entityColumns = resourceModel.Entity.Columns;
			var unmappedMembers = new List<string>();

			foreach (var column in resourceModel.Columns)
				unmappedMembers.Add(column.ColumnName);

			foreach (var resourceMember in resourceModel.Columns)
			{
				//  If this is the HRef (the primary key), then the columns that comprise it are all the primary
				//  key values of the entity in the order in which they appear in the entity
				if (resourceMember.IsPrimaryKey)
				{
					var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(resourceMember.ColumnName));

					if (!string.IsNullOrWhiteSpace(matchedColumn))
					{
						unmappedMembers.Remove(matchedColumn);

						var resourceProfile = new ResourceProfile
						{
							ResourceColumnName = resourceMember.ColumnName
						};

						var primaryKeys = entityColumns.Where(c => c.IsPrimaryKey);
						var formula = new StringBuilder($"new Uri(rootUrl, $\"{nn.PluralCamelCase}/id");
						var sourceColumns = new List<string>();

						foreach (var entityMember in primaryKeys)
						{
							formula.Append($"/{{source.{entityMember.ColumnName}}}");
							sourceColumns.Add(entityMember.ColumnName);
						}

						formula.Append("\")");
						resourceProfile.MapFunction = formula.ToString();
						resourceProfile.EntityColumnNames = sourceColumns.ToArray();
						resourceProfile.IsDefined = true;
						result.Add(resourceProfile);

					}
				}

				//  If this column represents a foreign key, then the entity members that comprise it will be
				//  those entity members that are foreign keys, that have the same table name as this members name.
				else if (resourceMember.IsForeignKey)
				{
					//  A foreign key is commonly represented in one of two forms. Either it is a hypertext reference
					//  (an href), in which case the resource member should be a Uri, or it is an enum.

					//  If it is an enum, there will be a resource model of type enum, whose corresponding entity model
					//  will point to the foreign table.

					var enumResource = codeService.GetResourceClassBySchema(resourceModel.Entity.SchemaName, resourceMember.ForeignTableName);

					if (enumResource != null && enumResource.ResourceType == ResourceType.Enum)
					{
						var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(resourceMember.ColumnName));

						if (!string.IsNullOrWhiteSpace(matchedColumn))
						{
							unmappedMembers.Remove(matchedColumn);

							var resourceProfile = new ResourceProfile
							{
								ResourceColumnName = resourceMember.ColumnName
							};

							var sourceColumns = new List<string>();
							var formula = $"({enumResource.ClassName})source.{resourceMember.ColumnName}";
							//  Need to think about this...
							sourceColumns.Add(enumResource.Entity.ClassName);
							resourceProfile.MapFunction = formula.ToString();
							resourceProfile.EntityColumnNames = sourceColumns.ToArray();
							resourceProfile.IsDefined = true;
							result.Add(resourceProfile);
						}
					}
					else
					{
						if (string.Equals(resourceMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
						{
							var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(resourceMember.ColumnName));

							if (!string.IsNullOrWhiteSpace(matchedColumn))
							{
								unmappedMembers.Remove(matchedColumn);

								var resourceProfile = new ResourceProfile
								{
									ResourceColumnName = resourceMember.ColumnName
								};

								var nnn = new NameNormalizer(resourceMember.ColumnName);
								var foreignKeys = entityColumns.Where(c => c.IsForeignKey &&
									string.Equals(c.ForeignTableName, resourceMember.ForeignTableName, StringComparison.Ordinal));

								var formula = new StringBuilder($"new Uri(rootUrl, $\"{nnn.PluralCamelCase}/id");
								var sourceColumns = new List<string>();

								foreach (var entityMember in foreignKeys)
								{
									formula.Append($"/{{source.{entityMember.ColumnName}}}");
									sourceColumns.Add(entityMember.ColumnName);
								}

								formula.Append("\")");
								resourceProfile.MapFunction = formula.ToString();
								resourceProfile.EntityColumnNames = sourceColumns.ToArray();
								resourceProfile.IsDefined = true;
								result.Add(resourceProfile);
							}
						}
						else
						{
							//  This is probably an Enum. If it is, we should be able to find it in the list of 
							//  resource models.
							var referenceModel = codeService.GetResourceClass(resourceMember.ModelDataType);

							if (referenceModel != null)
							{
								if (referenceModel.ResourceType == ResourceType.Enum)
								{
									var matchedColumn = unmappedMembers.FirstOrDefault(c => c.Equals(resourceMember.ColumnName));

									if (!string.IsNullOrWhiteSpace(matchedColumn))
									{
										unmappedMembers.Remove(matchedColumn);

										var resourceProfile = new ResourceProfile
										{
											ResourceColumnName = resourceMember.ColumnName
										};

										var nnn = new NameNormalizer(resourceMember.ColumnName);
										var foreignKeys = entityColumns.Where(c => c.IsForeignKey &&
												 (string.Equals(c.ForeignTableName, nnn.SingleForm, StringComparison.Ordinal) ||
												   string.Equals(c.ForeignTableName, nnn.PluralForm, StringComparison.Ordinal)));

										if (foreignKeys.Count() == 1)
										{
											var entityMember = foreignKeys.ToList()[0];

											var formula = $"({referenceModel.ClassName}) source.{entityMember.ColumnName}";
											resourceProfile.MapFunction = formula.ToString();
											resourceProfile.EntityColumnNames = new string[] { entityMember.ColumnName };
											resourceProfile.IsDefined = true;
											result.Add(resourceProfile);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					//  Is there an existing entityMember whos column name matches the resource member?
					var entityMember = entityColumns.FirstOrDefault(u =>
						string.Equals(u.ColumnName, resourceMember.ColumnName, StringComparison.OrdinalIgnoreCase));

					if (entityMember != null)
					{
						//  There is, just assign it.
						var resourceProfile = new ResourceProfile
						{
							ResourceColumnName = resourceMember.ColumnName
						};

						MapEntityDestinationFromSource(resourceMember, ref resourceProfile, entityMember);
						result.Add(resourceProfile);
					}
					else
					{
						//  Is this resource member a class?
						var model = codeService.GetResourceClass(resourceMember.ModelDataType.ToString());

						if (model != null)
						{
							var resourceProfile = new ResourceProfile
							{
								ResourceColumnName = resourceMember.ColumnName
							};
							//  It is a class, instantiate the class
							resourceProfile.MapFunction = $"new {model.ClassName}()";

							//  Now, go map all of it's children
							MapEntityChildMembers(resourceMember, resourceProfile, model, resourceMember.ColumnName);
							result.Add(resourceProfile);
						}
						else
						{
							if (resourceMember.ModelDataType.Contains("[]"))
							{
								var resourceProfile = new ResourceProfile
								{
									ResourceColumnName = resourceMember.ColumnName
								};
								var className = resourceMember.ModelDataType.Remove(resourceMember.ModelDataType.IndexOf('['), 2);
								resourceProfile.MapFunction = $"Array.Empty<{className}>()";
								resourceProfile.EntityColumnNames = Array.Empty<string>();
								resourceProfile.IsDefined = true;
								result.Add(resourceProfile);
							}
							else if (resourceMember.ModelDataType.Contains("List<"))
							{
								var resourceProfile = new ResourceProfile
								{
									ResourceColumnName = resourceMember.ColumnName
								};
								var index = resourceMember.ModelDataType.IndexOf('<');
								var count = resourceMember.ModelDataType.IndexOf('>') - index;
								var className = resourceMember.ModelDataType.Substring(index + 1, count - 1);
								resourceProfile.MapFunction = $"new List<{className}>()";
								resourceProfile.EntityColumnNames = Array.Empty<string>();
								resourceProfile.IsDefined = true;
								result.Add(resourceProfile);
							}
							else if (resourceMember.ModelDataType.Contains("IEnumerable<"))
							{
								var resourceProfile = new ResourceProfile
								{
									ResourceColumnName = resourceMember.ColumnName
								};
								var index = resourceMember.ModelDataType.IndexOf('<');
								var count = resourceMember.ModelDataType.IndexOf('>') - index;
								var className = resourceMember.ModelDataType.Substring(index + 1, count - 1);
								resourceProfile.MapFunction = $"Array.Empty<{className}>()";
								resourceProfile.EntityColumnNames = Array.Empty<string>();
								resourceProfile.IsDefined = true;
								result.Add(resourceProfile);
							}
						}
					}
				}
			}

			return result;
		}

		public static ResourceClass GetParentModel(ResourceClass parent, string[] parts)
		{
			ResourceClass result = parent;

			for (int i = 0; i < parts.Count() - 1; i++)
			{
				var column = result.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[i], StringComparison.OrdinalIgnoreCase));

				if (column != null)
				{
					var codeService = COFRSServiceFactory.GetService<ICodeService>();
					result = codeService.GetResourceClass(column.ModelDataType.ToString());
				}
			}

			return result;
		}

		public static void MapEntityChildMembers(DBColumn member, ResourceProfile resourceProfile, ResourceClass model, string parent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var entityColumns = model.Entity.Columns;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			//  We have a model, and the parent column name.

			//  Map the children
			foreach (var childMember in model.Columns)
			{
				//  Do we have an existing entity member that matches the child resource column name?
				var entityMember = entityColumns.FirstOrDefault(u =>
					string.Equals(u.ColumnName, childMember.ColumnName, StringComparison.OrdinalIgnoreCase));

				if (entityMember != null)
				{
					//  We do, just assign it
					MapEntityDestinationFromSource(childMember, ref resourceProfile, entityMember);
				}
				else
				{
					var childModel = codeService.GetResourceClass(member.ModelDataType.ToString());

					if (model != null)
					{
						MapEntityChildMembers(member, resourceProfile, model, $"{parent}?.{childMember.ColumnName}");
					}
				}
			}
		}

		public static void MapEntityDestinationFromSource(DBColumn destinationMember, ref ResourceProfile resourceProfile, DBColumn sourceMember)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var EnumClassName = destinationMember.ModelDataType.ToString().Trim('?');

			var resourceModel = codeService.GetResourceClass(EnumClassName);

			if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = $"source.{sourceMember.ColumnName}";
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = true;
			}
			else if (resourceModel != null)
			{
				if (resourceModel.ResourceType == ResourceType.Enum)
				{
					if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
					{
						resourceProfile.MapFunction = $"({resourceModel.ClassName}) source.{sourceMember.ColumnName}";
						resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
						resourceProfile.IsDefined = true;
					}
					else if (string.Equals(sourceMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(sourceMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase))
					{
						resourceProfile.MapFunction = $"source.{sourceMember.ColumnName}.HasValue ? ({resourceModel.ClassName}) source.{sourceMember.ColumnName}.Value : ({resourceModel.ClassName}?) null";
						resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
						resourceProfile.IsDefined = true;
					}
					else if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (destinationMember.ModelDataType.EndsWith("?"))
						{
							resourceProfile.MapFunction = $"string.IsNullOrWhiteSpace(source.{sourceMember.ColumnName}) ? ({resourceModel.ClassName}?) null : Enum.Parse<{resourceModel.ClassName}>(source.{sourceMember.ColumnName})";
							resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
							resourceProfile.IsDefined = true;
						}
						else
						{
							resourceProfile.MapFunction = $"Enum.Parse<{resourceModel.ClassName}>(source.{sourceMember.ColumnName})";
							resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
							resourceProfile.IsDefined = true;
						}
					}
					else
					{
						resourceProfile.MapFunction = $"({resourceModel.ClassName}) AFunc(source.{sourceMember.ColumnName})";
						resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
						resourceProfile.IsDefined = false;
					}
				}
				else
				{
					if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (ContainsParseFunction(resourceModel))
						{
							resourceProfile.MapFunction = $"{resourceModel.ClassName}.Parse(source.{sourceMember.ColumnName})";
							resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
							resourceProfile.IsDefined = true;
						}
						else
						{
							resourceProfile.MapFunction = $"({resourceModel.ClassName}) AFunc(source.{sourceMember.ColumnName})";
							resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
							resourceProfile.IsDefined = false;
						}
					}
					else
					{
						resourceProfile.MapFunction = $"({resourceModel.ClassName}) AFunc(source.{sourceMember.ColumnName})";
						resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
						resourceProfile.IsDefined = true;
					}
				}
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToByte(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToShort(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToInt(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToLong(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToULong(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToChar(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToString(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToImage(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
			{
				resourceProfile.MapFunction = SourceConverter.ToUri(sourceMember, out bool isUndefined);
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = !isUndefined;
			}
			else
			{
				resourceProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
				resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
				resourceProfile.IsDefined = false;
			}
		}

		public static void MapResourceDestinationFromSource(DBColumn destinationMember, EntityProfile entityProfile, DBColumn sourceMember)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var enumClassName = sourceMember.ModelDataType.ToString().Trim('?');
			var model = codeService.GetResourceClass(enumClassName);

			if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = $"source.{sourceMember.ColumnName}";
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = true;
			}
			else if (model != null)
			{
				if (model.ResourceType == ResourceType.Enum)
				{
					if (string.Equals(destinationMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
					{
						entityProfile.MapFunction = $"({destinationMember.ModelDataType}) source.{sourceMember.ColumnName}";
						entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
						entityProfile.IsDefined = true;
					}
					else if (string.Equals(destinationMember.ModelDataType, "byte?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "short?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "int?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "uint?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "long?", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(destinationMember.ModelDataType, "ulong?", StringComparison.OrdinalIgnoreCase))
					{
						entityProfile.MapFunction = $"source.{sourceMember.ColumnName}.HasValue ? ({destinationMember.ModelDataType}) source.{sourceMember.ColumnName}.Value : ({destinationMember.ModelDataType}) null";
						entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
						entityProfile.IsDefined = true;
					}
					else if (string.Equals(destinationMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (sourceMember.ModelDataType.EndsWith("?"))
						{
							entityProfile.MapFunction = $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.ToString() : (string) null";
							entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
							entityProfile.IsDefined = true;
						}
						else
						{
							entityProfile.MapFunction = $"source.{sourceMember.ColumnName}.ToString()";
							entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
							entityProfile.IsDefined = true;
						}
					}
					else
					{
						entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
						entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
						entityProfile.IsDefined = false;
					}
				}
				else
				{
					if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (ContainsParseFunction(model))
						{
							entityProfile.MapFunction = $"{model.ClassName}.Parse(source.{sourceMember.ColumnName})";
							entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
							entityProfile.IsDefined = true;
						}
						else
						{
							entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
							entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
							entityProfile.IsDefined = false;
						}
					}
					else
					{
						entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
						entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
						entityProfile.IsDefined = true;
					}
				}
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToByte(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToShort(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToInt(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToLong(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToULong(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToChar(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToString(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToImage(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
			{
				entityProfile.MapFunction = SourceConverter.ToUri(sourceMember, out bool isUndefined);
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = !isUndefined;
			}
			else
			{
				entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
				entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
				entityProfile.IsDefined = false;
			}
		}

		public static bool ContainsParseFunction(ResourceClass model)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (model.Resource.Kind == vsCMElement.vsCMElementEnum)
				return false;

			var codeClass = (CodeClass2)model.Resource;
			//  Search for a static function called Parse
			var theParseFunction = codeClass.Children.OfType<CodeFunction2>().FirstOrDefault(f => f.IsShared && string.Equals(f.Name, "parse", StringComparison.OrdinalIgnoreCase));

			if (theParseFunction != null)
			{
				CodeTypeRef functionType = theParseFunction.Type;

				//  It should return a code type of ClassName
				if (functionType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType &&
					string.Equals(functionType.CodeType.Name, model.ClassName, StringComparison.OrdinalIgnoreCase))
				{
					//  It should contain only one parameter

					if (theParseFunction.Parameters.Count == 1)
					{
						//  And that parameter should be of type string
						var theParameter = (CodeParameter2)theParseFunction.Parameters.Item(1);
						var parameterType = theParameter.Type;

						if (parameterType.TypeKind == vsCMTypeRef.vsCMTypeRefString)
							return true;
					}
				}
			}

			return false;
		}

		public string GetProjectItemNamespace(ProjectItem item)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (var property in item.Properties.OfType<Property>())
			{
				if (property.Name.Equals("Namespace"))
					return property.Value.ToString();
			}

			return string.Empty;
		}

		public string GetProjectItemPath(ProjectItem item)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (var property in item.Properties.OfType<Property>())
			{
				if (property.Name.Equals("FullPath"))
					return property.Value.ToString();
			}

			return string.Empty;
		}

		#endregion
	}
}
