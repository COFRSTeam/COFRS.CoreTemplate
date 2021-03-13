using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class FullStackControllerWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }

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
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				var computedRootNamespace = Utilities.GetRootNamespace(solutionDirectory);

				if (!string.Equals(rootNamespace, computedRootNamespace, StringComparison.OrdinalIgnoreCase))
				{
					MessageBox.Show("The COFRS Controller Full Stack should be placed at the project root. It will add the appropriate components in the appropriate folders.", "Placement Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Proceed = false;
					return;
				}

				var candidateName = replacementsDictionary["$safeitemname$"];

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);

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

				var root = string.Empty;

				for (int i = 0; i < namespaceParts.Count() - 1; i++)
				{
					if (i > 0)
						root += ".";

					root += namespaceParts[i];
				}

				replacementsDictionary["$entitynamespace$"] = $"{root}.Models.EntityModels";
				replacementsDictionary["$resourcenamespace$"] = $"{root}.Models.ResourceModels";
				replacementsDictionary["$orchestrationnamespace$"] = $"{root}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = $"{root}.Validation";
				replacementsDictionary["$validationnamespace$"] = $"{root}.Validation";
				replacementsDictionary["$singleexamplenamespace$"] = $"{root}.Models.SwaggerExamples";

				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputFullStack
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					var connectionString = form.ConnectionString;
					ReplaceConnectionString(connectionString, replacementsDictionary);

					replacementsDictionary["$safeitemname$"] = form.PluralResourceName;

					var entityClassName = $"E{form.SingularResourceName}";
					var resourceClassName = form.SingularResourceName;
					var mappingClassName = $"{form.PluralResourceName}Profile";
					var exampleClassName = $"{form.PluralResourceName}Example";
					var exampleCollectionClassName = $"Collection{form.PluralResourceName}Example";
					var validationClassName = $"{form.PluralResourceName}Validator";
					var controllerClassName = $"{form.PluralResourceName}Controller";

					replacementsDictionary["$entityClass$"] = entityClassName;
					replacementsDictionary["$resourceClass$"] = resourceClassName;
					replacementsDictionary["$controllerClass$"] = controllerClassName;
					replacementsDictionary["$swaggerClass$"] = exampleClassName;
					replacementsDictionary["$swaggerCollectionClass$"] = exampleCollectionClassName;
					replacementsDictionary["$mapClass$"] = mappingClassName;
					replacementsDictionary["$validatorClass$"] = validationClassName;

					var moniker = LoadMoniker(SolutionFolder);
					var policy = LoadPolicy(SolutionFolder);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					var emitter = new Emitter();
					var entityModel = emitter.EmitEntityModel(form.DatabaseTable, entityClassName, form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$entityModel$", entityModel);

					List<ClassMember> classMembers = LoadClassMembers(form.DatabaseTable, form.DatabaseColumns);

					var resourceModel = emitter.EmitResourceModel(classMembers, resourceClassName, entityClassName, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$resourceModel$", resourceModel);

					var mappingModel = emitter.EmitMappingModel(classMembers, resourceClassName, entityClassName, mappingClassName, form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$mappingModel$", mappingModel);

					var exampleModel = emitter.EmitExampleModel(replacementsDictionary["$targetframeworkversion$"],
											classMembers,
											entityClassName,
											resourceClassName,
											exampleClassName,
											form.DatabaseColumns, form.Examples, replacementsDictionary);
					replacementsDictionary.Add("$exampleModel$", exampleModel);

					var exampleCollectionModel = emitter.EmitExampleCollectionModel(replacementsDictionary["$targetframeworkversion$"],
						classMembers,
						entityClassName,
						resourceClassName,
						exampleCollectionClassName,
						form.DatabaseColumns, form.Examples, replacementsDictionary);
					replacementsDictionary.Add("$exampleCollectionModel$", exampleCollectionModel);

					var validationModel = emitter.EmitValidationModel(entityClassName, resourceClassName, validationClassName);
					replacementsDictionary.Add("$validationModel$", validationModel);

					Proceed = emitter.UpdateServices(solutionDirectory, validationClassName,
									replacementsDictionary["$entitynamespace$"], replacementsDictionary["$resourcenamespace$"],
									replacementsDictionary["$validatornamespace$"]);


					var controllerModel = emitter.EmitController(classMembers,
								   true,
								   moniker,
								   resourceClassName,
								   controllerClassName,
								   validationClassName,
								   exampleClassName,
								   exampleCollectionClassName,
								   policy);
					replacementsDictionary.Add("$controllerModel$", controllerModel);


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

		private List<ClassMember> LoadClassMembers(DBTable table, List<DBColumn> columns)
		{
			var members = new List<ClassMember>();

			var member = new ClassMember()
			{
				ResourceMemberName = "Href",
				ResourceMemberType = string.Empty,
				EntityNames = new List<DBColumn>(),
				ChildMembers = new List<ClassMember>()
			};

			foreach (var column in columns)
			{
				if (column.IsPrimaryKey)
					member.EntityNames.Add(column);
			}

			members.Add(member);

			foreach (var column in columns)
			{
				if (string.Equals(column.ColumnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
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
					 string.Equals(column.ColumnName, "yield", StringComparison.OrdinalIgnoreCase))
				{
					column.EntityName = $"{column.ColumnName}_Value";
				}
				else
				{
					column.EntityName = column.ColumnName;
				}

				if (column.ServerType == DBServerType.POSTGRESQL)
					column.EntityType = DBHelper.GetPostgresDataType(column);
				else if (column.ServerType == DBServerType.MYSQL)
					column.EntityType = DBHelper.GetMySqlDataType(column);
				else if (column.ServerType == DBServerType.SQLSERVER)
					column.EntityType = DBHelper.GetSQLServerDataType(column);

				if (!column.IsPrimaryKey)
				{
					if (!column.IsForeignKey)
					{
						var childMember = new ClassMember()
						{
							ResourceMemberName = column.ColumnName,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>() { column },
							ChildMembers = new List<ClassMember>()
						};

						members.Add(childMember);
					}
					else
					{
						string shortColumnName;

						if (string.Equals(column.ForeignTableName, table.Table, StringComparison.OrdinalIgnoreCase))
						{
							shortColumnName = column.ColumnName;
							if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
								shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
						}
						else
							shortColumnName = column.ForeignTableName;

						var normalizer = new NameNormalizer(shortColumnName);

						var childMember = new ClassMember()
						{
							ResourceMemberName = normalizer.SingleForm,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>() { column },
							ChildMembers = new List<ClassMember>()
						};

						members.Add(childMember);
					}
				}
			}

			return members;
		}

		private string LoadPolicy(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["policy"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string policy = LoadPolicy(subfolder);

					if (!string.IsNullOrWhiteSpace(policy))
						return policy;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}

		private string LoadMoniker(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["moniker"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string moniker = LoadMoniker(subfolder);

					if (!string.IsNullOrWhiteSpace(moniker))
						return moniker;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
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
