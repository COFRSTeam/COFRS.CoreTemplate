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

namespace COFRSCoreCommon.Utilities
{
    public static class COFRSCommonUtilities
    {
		#region Miscellaneous Operations
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

		public static List<string> LoadPolicies(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<string>();
			var appSettings = dte.Solution.FindProjectItem("appSettings.json");

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

		public static string FindOrchestrationNamespace(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var projectItem = dte.Solution.FindProjectItem("ServiceOrchestrator.cs");
			var code = projectItem.FileCodeModel;

			foreach (CodeElement c in code.CodeElements)
			{
				if (c.Kind == vsCMElement.vsCMElementNamespace)
					return c.Name;
			}

			return string.Empty;
		}


		public static string LoadMoniker(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = dte.Solution.FindProjectItem("appSettings.json");

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
		/// <summary>
		/// This function will add the appropriate code to regster the validation model in the ServicesConfig.cs file.
		/// </summary>
		/// <param name="_dte2>"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <param name="validationClass">The name of the validation class.</param>
		/// <param name="validationNamespace">The namespace where the validation class resides.</param>
		public static void RegisterValidationModel(DTE2 dte, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Get the ServicesConfig.cs project item.
			ProjectItem serviceConfig = dte.Solution.FindProjectItem("ServicesConfig.cs");
			bool wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];               //	Record if it was already open
			bool wasModified = false;                                                   //	We haven't modified it yet

			if (!wasOpen)                                                               //	If it wasn't open, open it.
				serviceConfig.Open(Constants.vsViewKindCode);

			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);       //	Get the window (so we can close it later)
			Document doc = serviceConfig.Document;                                      //	Get the doc 
			TextSelection sel = (TextSelection)doc.Selection;                           //	Get the current selection
			var activePoint = sel.ActivePoint;                                          //	Get the active point

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

					if (string.Equals(theNamespace, validationNamespace, StringComparison.OrdinalIgnoreCase))
					{
						hasValidationUsing = true;
					}
				}
			}

			//	If we don't have the using validation statement, we need to add it.
			//	We've kept track of the end of statement point on the last using
			//	statement, so insert the new one there.

			if (!hasValidationUsing)
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
			foreach (CodeNamespace namespaceElement in serviceConfig.FileCodeModel.CodeElements.OfType<CodeNamespace>())
			{
				foreach (CodeClass2 candidateClass in namespaceElement.Members.OfType<CodeClass2>())
				{
					if (string.Equals(candidateClass.Name, "ServiceCollectionExtension", StringComparison.OrdinalIgnoreCase))
					{
						foreach (CodeFunction2 candidateFunction in candidateClass.Members.OfType<CodeFunction2>())
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
			if (wasModified)
				doc.Save();

			//	If we were previously open, restore the active point to what it was before we changed it.
			//	Otherwise, if we were not previously open, the close the window.
			if (wasOpen)
				sel.MoveToPoint(activePoint);
			else
				window.Close();
		}

		public static void RegisterComposite(DTE2 dte2, EntityModel entityModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (entityModel.ElementType == ElementType.Undefined ||
				entityModel.ElementType == ElementType.Table)
				return;

			ProjectItem serviceConfig = dte2.Solution.FindProjectItem("ServicesConfig.cs");

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

		/// <summary>
		/// Checks to see if the candidate namespace is the root namespace of the startup project
		/// </summary>
		/// <param name="_dte2>"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <param name="candidateNamespace">The candidate namesapce</param>
		/// <returns><see langword="true"/> if the candidate namespace is the root namespace of the startup project; <see langword="false"/> otherwise</returns>
		public static bool IsRootNamespace(DTE2 dte2, string candidateNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in dte2.Solution.Projects)
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

		public static string GetRelativeFolder(DTE2 dte2, ProjectFolder folder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Project project = COFRSCommonUtilities.GetProject(dte2, folder.ProjectName);
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

		#endregion

		#region Project Operations
		public static Project GetProject(DTE2 dte2, string projectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in dte2.Solution.Projects)
			{
				if (string.Equals(project.Name, projectName, StringComparison.OrdinalIgnoreCase))
					return project;
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
		public static ProjectFolder FindEntityModelsFolder(DTE2 dte2)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte2.Solution.Projects)
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

			var candidates = COFRSCommonUtilities.FindProjectFolder(dte2, theCandidateNamespace);

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
		public static ProjectFolder FindResourceModelsFolder(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte.Solution.Projects)
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

			var candidates = COFRSCommonUtilities.FindProjectFolder(dte, theCandidateNamespace);

			if (candidates.Count > 0)
				return candidates[0];

			//	We didn't find any resource models in the project. Search for the default resource models folder.
			theCandidateNamespace = "*.ResourceModels";

			candidates = COFRSCommonUtilities.FindProjectFolder(dte, theCandidateNamespace);

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

		public static ProjectFolder FindMappingFolder(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for an entity model. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte.Solution.Projects)
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

			var candidates = COFRSCommonUtilities.FindProjectFolder(dte, theCandidateNamespace);

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
		/// Find Controllers Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindControllersFolder(DTE2 dte2)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte2.Solution.Projects)
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

			var candidates = FindProjectFolder(dte2, theCandidateNamespace);

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

		#region Discovery Operations
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

		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindValidationFolder(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte.Solution.Projects)
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

			var candidates = COFRSCommonUtilities.FindProjectFolder(dte, theCandidateNamespace);

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
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass codeClass in namespaceElement.Members.OfType<CodeClass2>())
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
					foreach (CodeNamespace namespaceElement in candidate.FileCodeModel.CodeElements.OfType<CodeNamespace>())
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


		private static CodeClass2 FindValidator(DTE2 dte2, ResourceModel resourceModel, string folder = "")
		{
			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
			var validatorModelFolder = projectMapping.GetValidatorFolder();

			var validatorFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(validatorModelFolder.Folder) :
																	  dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in validatorFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindVirtualFolder ||
					projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder)
				{
					CodeClass2 validatorClass = FindValidator(dte2, resourceModel, projectItem.Name);

					if (validatorClass != null )
						return validatorClass;
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile && projectItem.FileCodeModel != null)
				{
					FileCodeModel2 codeModel = (FileCodeModel2)projectItem.FileCodeModel;

					foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 codeClass in codeNamespace.Children.OfType<CodeClass2>())
						{
							foreach (CodeClass2 codeBase in codeClass.Bases.OfType<CodeClass2>())
							{
								var parts = codeBase.FullName.Split(new char[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);

								if (parts.Length == 2)
								{
									var interfaceParts = parts[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
									var classParts = parts[1].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

									if (interfaceParts[interfaceParts.Length - 1].Equals("IValidator") &&
										 classParts[classParts.Length - 1].Equals(resourceModel.ClassName))
									{
										return codeClass;
									}
								}
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
		public static string FindValidatorInterface(DTE2 dte2, string resourceClassName, string folder = "")
		{
			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
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
		#endregion

		#region Example Functions
		/// <summary>
		/// Get the validator interface name for a resource
		/// </summary>
		/// <param name="resourceClassName">The resource class whos validator is to be found</param>
		/// <param name="folder">The folder to search</param>
		/// <returns>The name of the interface for the validator of the resource.</returns>
		public static CodeClass2 FindExampleCode(DTE2 dte2, ResourceModel parentModel, string folder = "")
		{
			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
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
			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.
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

		/// <summary>
		/// Find Validation Folder
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindExampleFolder(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Search the solution for a validator class. If one is found then return the 
			//	project folder for the folder in which it resides.
			foreach (Project project in dte.Solution.Projects)
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

			var candidates = FindProjectFolder(dte, theCandidateNamespace);

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
		public static List<ProjectFolder> FindProjectFolder(DTE2 dte, string destinationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var projectFolderCollection = new List<ProjectFolder>();

			foreach (Project project in dte.Solution.Projects)
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
		#endregion

		#region Models Functions
		/// <summary>
		/// Load all entity models from the entity models folder
		/// </summary>
		/// <param name="folder">The child folder to search</param>
		/// <returns>A collection of all entity models in the solution</returns>
		public static EntityMap LoadEntityMap(DTE2 dte2, string folder = "")
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<EntityModel>();
			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);                        //	Contains the names and projects where various source file exist.

			var entityFolder = string.IsNullOrWhiteSpace(folder) ? dte2.Solution.FindProjectItem(projectMapping.GetEntityModelsFolder().Folder) :
																   dte2.Solution.FindProjectItem(folder);

			foreach (ProjectItem projectItem in entityFolder.ProjectItems)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
					 projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
				{
					var emap = LoadEntityMap(dte2, projectItem.Name);
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

			var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte);                        //	Contains the names and projects where various source file exist.
			var entityModelsFolder = projectMapping.GetEntityModelsFolder();
			var resourceModelFolder = projectMapping.GetResourceModelsFolder();
			var entityMap = LoadEntityMap(dte);

			var defaultServerType = COFRSCommonUtilities.GetDefaultServerType(dte);


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
		#endregion

		#region Database Operations
		/// <summary>
		/// Get the default connection string from the appsettings.local.json
		/// </summary>
		/// <returns>The connection string used in the local settings</returns>
		public static string GetConnectionString(DTE2 dte2)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem settingsFile = dte2.Solution.FindProjectItem("appsettings.Local.json");

			var filepath = settingsFile.FileNames[0];

			using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var reader = new StreamReader(stream))
				{
					var jsonText = reader.ReadToEnd();
					var settings = JObject.Parse(jsonText);
					var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
					return connectionStrings["DefaultConnection"].Value<string>();
				}
			}
		}

		public static void ReplaceConnectionString(DTE2 dte2, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = dte2.Solution.FindProjectItem("appsettings.Local.json");

			bool wasOpen = settingsFile.IsOpen;

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

			if ( !wasOpen)
				window.Close();
		}

		/// <summary>
		/// Get the default server type
		/// </summary>
		/// <param name="DefaultConnectionString">The default connection string</param>
		/// <returns>The default server type</returns>
		public static DBServerType GetDefaultServerType(DTE2 dte2)
		{
			//	Get the location of the server configuration on disk
			var DefaultConnectionString = GetConnectionString(dte2);
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

		#region Mapping Functions
		/// <summary>
		/// Loads the <see cref="ProjectMapping"/> for the project
		/// </summary>
		/// <param name="dte>"The <see cref="DTE2"/> Visual Studio interface</param>
		/// <returns>The <see cref="ProjectMapping"/> for the project.</returns>
		public static ProjectMapping OpenProjectMapping(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = dte.Solution.Properties.Item("Path").Value.ToString();
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

				return AutoFillProjectMapping(dte, projectMapping);
			}
			catch (FileNotFoundException)
			{
				var projectMapping = AutoFillProjectMapping(dte, new ProjectMapping());
				SaveProjectMapping(dte, projectMapping);
				return projectMapping;
			}
			catch (DirectoryNotFoundException)
			{
				var projectMapping = AutoFillProjectMapping(dte, new ProjectMapping());
				SaveProjectMapping(dte, projectMapping);
				return projectMapping;
			}
			catch (Exception)
			{
				var projectMapping = AutoFillProjectMapping(dte, new ProjectMapping());
				SaveProjectMapping(dte, projectMapping);
				return projectMapping;
			}
		}

		private static ProjectMapping AutoFillProjectMapping(DTE2 dte, ProjectMapping projectMapping)
        {
			var installationFolder = GetInstallationFolder(dte);

			if (string.IsNullOrWhiteSpace(projectMapping.EntityFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.EntityNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.EntityProject))
			{
				var modelFolder = FindEntityModelsFolder(dte);
				projectMapping.EntityFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.EntityNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.EntityProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ResourceFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ResourceNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ResourceProject))
			{
				var modelFolder = FindResourceModelsFolder(dte);
				projectMapping.ResourceFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ResourceNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ResourceProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.MappingFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.MappingNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.MappingProject))
			{
				var modelFolder = FindMappingFolder(dte);
				projectMapping.MappingFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.MappingNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.MappingProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ValidationFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ValidationNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ValidationProject))
			{
				var modelFolder = FindMappingFolder(dte);
				projectMapping.ValidationFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ValidationNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ValidationProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ExampleFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ExampleNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ExampleProject))
			{
				var modelFolder = FindExampleFolder(dte);
				projectMapping.ExampleFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ExampleNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ExampleProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			if (string.IsNullOrWhiteSpace(projectMapping.ControllersFolder) ||
				string.IsNullOrWhiteSpace(projectMapping.ControllersNamespace) ||
				string.IsNullOrWhiteSpace(projectMapping.ControllersProject))
			{
				var modelFolder = FindControllersFolder(dte);
				projectMapping.ControllersFolder = modelFolder == null ? installationFolder.Folder : modelFolder.Folder;
				projectMapping.ControllersNamespace = modelFolder == null ? installationFolder.Namespace : modelFolder.Namespace;
				projectMapping.ControllersProject = modelFolder == null ? installationFolder.ProjectName : modelFolder.ProjectName;
			}

			return projectMapping;
        }

		public static void SaveProjectMapping(DTE2 dte, ProjectMapping projectMapping)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var jsonData = JsonConvert.SerializeObject(projectMapping, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				MissingMemberHandling = MissingMemberHandling.Ignore
			});

			var solutionPath = dte.Solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\ProjectMap.json");

			File.WriteAllText(mappingPath, jsonData);
		}

		/// <summary>
		/// Save the <see cref="ProfileMap"/> to disk
		/// </summary>
		/// <param name="dte">The <see cref="DTE2"/> Visual Studio interface.</param>
		/// <param name="theMap">The <see cref="ProfileMap"/> to save.</param>
		public static void SaveProfileMap(DTE2 dte, ProfileMap theMap)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = dte.Solution.Properties.Item("Path").Value.ToString();
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
		#endregion
	}
}
