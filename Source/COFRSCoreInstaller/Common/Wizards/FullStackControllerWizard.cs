using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
    public class FullStackControllerWizard : IWizard
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
		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
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
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			try
			{
				//  Load the project mapping information
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;
				var projectMapping = codeService.LoadProjectMapping();
				var defaultServerType = codeService.DefaultServerType;
				var policies = codeService.Policies;

				//	Get folders and namespaces
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				replacementsDictionary["$entitynamespace$"] = projectMapping.EntityNamespace;
				replacementsDictionary["$resourcenamespace$"] = projectMapping.ResourceNamespace;
				replacementsDictionary["$mappingnamespace$"] = projectMapping.MappingNamespace;
				replacementsDictionary["$orchestrationnamespace$"] = $"{rootNamespace}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = projectMapping.ValidationNamespace;
				replacementsDictionary["$validationnamespace$"] = projectMapping.ValidationNamespace;
				replacementsDictionary["$examplesnamespace$"] = projectMapping.ExampleNamespace;

				var candidateName = replacementsDictionary["$safeitemname$"];

				if (candidateName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 3);

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);

				var form = new FullStackDialog
				{
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					ReplacementsDictionary = replacementsDictionary,
					EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
					Policies = policies,
					DefaultConnectionString = connectionString,
				};

				EntityClass entityModel = null;
				var result = form.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					codeService.ConnectionString = $"{form.ConnectionString}Application Name={projectMapping.ControllersProject}";

					var entityClassName = $"E{form.SingularResourceName}";
					var resourceClassName = form.SingularResourceName;
					var mappingClassName = $"{form.PluralResourceName}Profile";
					var validationClassName = $"{form.PluralResourceName}Validator";
					var exampleClassName = $"{form.PluralResourceName}Example";
					var controllerClassName = $"{form.PluralResourceName}Controller";

					replacementsDictionary["$entityClass$"] = entityClassName;
					replacementsDictionary["$resourceClass$"] = resourceClassName;
					replacementsDictionary["$mapClass$"] = mappingClassName;
					replacementsDictionary["$validatorClass$"] = validationClassName;
					replacementsDictionary["$exampleClass$"] = exampleClassName;
					replacementsDictionary["$controllerClass$"] = controllerClassName;

					var moniker = codeService.Moniker;
					var policy = form.Policy;

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					var emitter = new Emitter();
					var standardEmitter = new StandardEmitter();

					#region Entity Model Operations
					//	Should we generate an entity model?
					if (form.GenerateEntityModel)
					{
						if (form.ServerType == DBServerType.POSTGRESQL)
						{
							//	Generate any undefined composits before we construct our entity model (because, 
							//	the entity model depends upon them)

							standardEmitter.GenerateComposites(form.UndefinedEntityModels,
															   form.ConnectionString,
															   replacementsDictionary,
															   projectMapping.GetEntityModelsFolder());
						}

						//	Emit Entity Model
						var columns = DBHelper.GenerateColumns(form.DatabaseTable.Schema,
															   form.DatabaseTable.Table,
															   form.ServerType,
															   form.ConnectionString);

						var emodel = standardEmitter.EmitEntityModel(entityClassName,
																	 form.DatabaseTable.Schema,
																	 form.DatabaseTable.Table,
																	 form.ServerType,
																	 columns,
																	 replacementsDictionary);

						var entityFilePath = Path.Combine(projectMapping.GetEntityModelsFolder().Folder, $"{entityClassName}.cs");

						var theFile = new StringBuilder();

						theFile.AppendLine("using System;");

						if (replacementsDictionary.ContainsKey("$barray$"))
							if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Collections;");

						theFile.AppendLine("using System.Collections.Generic;");

						if (replacementsDictionary.ContainsKey("$image$"))
							if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Drawing;");

						if (replacementsDictionary.ContainsKey("$net$"))
							if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net;");

						if (replacementsDictionary.ContainsKey("$netinfo$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net.NetworkInformation;");

						if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
							if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using NpgsqlTypes;");

						theFile.AppendLine("using COFRS;");
						theFile.AppendLine();
						theFile.AppendLine($"namespace {projectMapping.EntityNamespace}");
						theFile.AppendLine("{");

						theFile.Append(emodel);
						theFile.AppendLine("}");

						File.WriteAllText(entityFilePath, theFile.ToString());

						var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(entityFilePath));
						ProjectItem entityItem;

						if (parentProject.GetType() == typeof(Project))
							entityItem = ((Project)parentProject).ProjectItems.AddFromFile(entityFilePath);
						else
							entityItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(entityFilePath);

						codeService.AddEntity(entityItem);

						ProjectItemFinishedGenerating(entityItem);
						BeforeOpeningFile(entityItem);

						var window = entityItem.Open();
						window.Activate();
					}
					#endregion

					#region Resource Model Operations
					if (form.GenerateResourceModel)
					{
						entityModel = codeService.GetEntityClass(entityClassName);

						var rmodel = standardEmitter.EmitResourceModel(resourceClassName,
							                                           entityModel,
																	   replacementsDictionary);

						var resourceFilePath = Path.Combine(projectMapping.GetResourceModelsFolder().Folder, $"{resourceClassName}.cs");

						var theFile = new StringBuilder();

						theFile.AppendLine("using System;");

						if (replacementsDictionary.ContainsKey("$barray$"))
							if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Collections;");

						theFile.AppendLine("using System.Collections.Generic;");

						if (replacementsDictionary.ContainsKey("$image$"))
							if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Drawing;");

						if (replacementsDictionary.ContainsKey("$net$"))
							if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net;");

						if (replacementsDictionary.ContainsKey("$netinfo$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net.NetworkInformation;");

						if (replacementsDictionary.ContainsKey("$annotations$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

						theFile.AppendLine($"using {projectMapping.EntityNamespace};");

						if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
							if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using NpgsqlTypes;");

						theFile.AppendLine("using COFRS;");
						theFile.AppendLine();
						theFile.AppendLine($"namespace {projectMapping.ResourceNamespace}");
						theFile.AppendLine("{");

						theFile.Append(rmodel);
						theFile.AppendLine("}");

						File.WriteAllText(resourceFilePath, theFile.ToString());

						var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(resourceFilePath));
						ProjectItem resourceItem;

						if (parentProject.GetType() == typeof(Project))
							resourceItem = ((Project)parentProject).ProjectItems.AddFromFile(resourceFilePath);
						else
							resourceItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(resourceFilePath);

						codeService.AddResource(resourceItem);

						ProjectItemFinishedGenerating(resourceItem);
						BeforeOpeningFile(resourceItem);

						var window = resourceItem.Open();
						window.Activate();
					}
					#endregion

					#region Mapping Operations
					if (form.GenerateMappingModel)
					{
						var resourceModel = codeService.GetResourceClass(resourceClassName);

						var mappingModel = standardEmitter.EmitMappingModel(resourceModel, mappingClassName, replacementsDictionary);

						var projectItemPath = Path.Combine(projectMapping.MappingFolder, $"{mappingClassName}.cs");

						var theFile = new StringBuilder();

						theFile.AppendLine("using System;");
						theFile.AppendLine("using System.Linq;");
						theFile.AppendLine("using Microsoft.Extensions.Configuration;");

						if (replacementsDictionary.ContainsKey("$barray$"))
							if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Collections;");

						theFile.AppendLine("using System.Collections.Generic;");

						if (replacementsDictionary.ContainsKey("$image$"))
							if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Drawing;");

						if (replacementsDictionary.ContainsKey("$net$"))
							if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net;");

						if (replacementsDictionary.ContainsKey("$netinfo$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net.NetworkInformation;");

						if (replacementsDictionary.ContainsKey("$annotations$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

						if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
							if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using NpgsqlTypes;");

						theFile.AppendLine($"using {projectMapping.EntityNamespace};");
						theFile.AppendLine($"using {projectMapping.ResourceNamespace};");

						theFile.AppendLine("using AutoMapper;");
						theFile.AppendLine("using COFRS;");
						theFile.AppendLine();
						theFile.AppendLine($"namespace {projectMapping.MappingNamespace}");
						theFile.AppendLine("{");

						theFile.Append(mappingModel);
						theFile.AppendLine("}");

						File.WriteAllText(projectItemPath, theFile.ToString());

						var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(projectItemPath));
						ProjectItem mappingItem;

						if (parentProject.GetType() == typeof(Project))
							mappingItem = ((Project)parentProject).ProjectItems.AddFromFile(projectItemPath);
						else
							mappingItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(projectItemPath);

						var window = mappingItem.Open();
						window.Activate();
					}
                    #endregion

                    #region Validation Operations
                    if (form.GenerateValidator)
                    {
                        var resourceModel = codeService.GetResourceClass(resourceClassName);
                        var profileMap = codeService.OpenProfileMap(resourceModel, out bool IsAllDefined);
                        var orchestrationNamespace = codeService.FindOrchestrationNamespace();

                        //	Emit Validation Model
                        var validationModel = standardEmitter.EmitValidationModel(resourceModel, profileMap, validationClassName);
						var projectItemPath = Path.Combine(projectMapping.ValidationFolder, $"{validationClassName}.cs");

						var theFile = new StringBuilder();

						theFile.AppendLine("using System;");
						theFile.AppendLine("using System.Collections.Generic;");
						theFile.AppendLine("using System.Linq;");
						theFile.AppendLine("using System.Security.Claims;");
						theFile.AppendLine("using System.Threading.Tasks;");

						if (replacementsDictionary.ContainsKey("$barray$"))
							if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Collections;");

						if (replacementsDictionary.ContainsKey("$image$"))
							if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Drawing;");

						if (replacementsDictionary.ContainsKey("$net$"))
							if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net;");

						if (replacementsDictionary.ContainsKey("$netinfo$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.Net.NetworkInformation;");

						if (replacementsDictionary.ContainsKey("$annotations$"))
							if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

						if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
							if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
								theFile.AppendLine("using NpgsqlTypes;");

						theFile.AppendLine($"using {projectMapping.EntityNamespace};");
						theFile.AppendLine($"using {projectMapping.ResourceNamespace};");
						theFile.AppendLine($"using {orchestrationNamespace};");
						theFile.AppendLine("using COFRS;");
						theFile.AppendLine();
						theFile.AppendLine($"namespace {projectMapping.ValidationNamespace}");
						theFile.AppendLine("{");

						theFile.Append(validationModel);
						theFile.AppendLine("}");

						File.WriteAllText(projectItemPath, theFile.ToString());

						var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(projectItemPath));
						ProjectItem validationItem;

						if (parentProject.GetType() == typeof(Project))
							validationItem = ((Project)parentProject).ProjectItems.AddFromFile(projectItemPath);
						else
							validationItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(projectItemPath);

						var window = validationItem.Open();
						window.Activate();

						codeService.RegisterValidationModel(validationClassName,
															projectMapping.ValidationNamespace);
					}
                    #endregion

                    #region Example Operations
                    //if (form.GenerateExampleData)
                    //{
                    //	GenerateExample = true;

                    //	var resourceModel = codeService.GetResourceClass(resourceClassName);

                    //	if ( profileMap== null)
                    //		profileMap = COFRSCommonUtilities.OpenProfileMap(dte, resourceModel, out bool IsAllDefined);

                    //	var exampleModel = standardEmitter.EmitExampleModel(resourceModel, profileMap, exampleClassName, defaultServerType, connectionString);
                    //	replacementsDictionary.Add("$examplemodel$", exampleModel);
                    //}
                    #endregion

                    #region Controller Operations
                    //if (form.GenerateController)
                    //{
                    //	GenerateController = true;

                    //	var resourceModel = codeService.GetResourceClass(resourceClassName);

                    //	var controllerModel = emitter.EmitController(
                    //		dte,
                    //		entityModel,
                    //		resourceModel,
                    //		moniker,
                    //		controllerClassName,
                    //		validatorInterface,
                    //		policy,
                    //		projectMapping.ValidationNamespace);

                    //	replacementsDictionary.Add("$controllerModel$", controllerModel);
                    //}
                    #endregion
                }

                Proceed = true;
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
	}
}
