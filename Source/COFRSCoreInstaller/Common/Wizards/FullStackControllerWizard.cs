﻿using COFRS.Template.Common.Forms;
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
		private bool GenerateEntity = false;
		private bool GenerateResource = false;
		private readonly bool GenerateController = false;
		private readonly bool GenerateExample = false;
		private readonly bool GenerateValidator = false;
		private bool GenerateMapping = false;

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
			ThreadHelper.ThrowIfNotOnUIThread();
			DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
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
						GenerateEntity = true;

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
						theFile.AppendLine($"namespace {projectMapping.ExampleNamespace}");
						theFile.AppendLine("{");

						theFile.Append(emodel);
						theFile.AppendLine("}");

						File.WriteAllText(entityFilePath, theFile.ToString());

						var parentProject = codeService.GetProjectFromFolder(entityFilePath);
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
						GenerateResource = true;

						entityModel = codeService.GetEntityClass(entityClassName);

						var rmodel = standardEmitter.EmitResourceModel(entityModel,
																	   replacementsDictionary);

						replacementsDictionary.Add("$resourceModel$", rmodel);
					}
					#endregion

					#region Mapping Operations
					if (form.GenerateMappingModel)
					{
						GenerateMapping = true;

						var resourceModel = codeService.GetResourceClass(resourceClassName);

						var mappingModel = standardEmitter.EmitMappingModel(resourceModel, mappingClassName, replacementsDictionary);

						replacementsDictionary.Add("$mappingModel$", mappingModel);
					}
					else
						GenerateMapping = false;
                    #endregion

					#region Validation Operations
     //               var validatorInterface = string.Empty;
					//if (form.GenerateValidator)
					//{
					//	GenerateValidator = true;

					//	var resourceModel = codeService.GetResourceClass(resourceClassName);

					//	if ( profileMap == null)
					//		profileMap = COFRSCommonUtilities.OpenProfileMap(dte, resourceModel, out bool IsAllDefined);
					//	var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(dte);

					//	//	Emit Validation Model
					//	var validationModel = standardEmitter.EmitValidationModel(resourceModel, profileMap, resourceMap, validationClassName, out validatorInterface);
					//	replacementsDictionary.Add("$validationModel$", validationModel);

					//	//	Register the validation model
					//	COFRSCommonUtilities.RegisterValidationModel(dte,
					//									  validationClassName,
					//									  replacementsDictionary["$validatornamespace$"]);
					//}
					//else
     //               {
					//	validatorInterface = COFRSCommonUtilities.FindValidatorInterface(dte, resourceModel.ClassName);
     //               }
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
			if (string.Equals(filePath, "Controllers"))
				return Proceed && GenerateController;

			if (string.Equals(filePath, "Validation"))
				return Proceed && GenerateValidator;

			if (string.Equals(filePath, "Mapping"))
				return Proceed && GenerateMapping;

			if (string.Equals(filePath, "Examples"))
				return Proceed && GenerateExample;

			if (string.Equals(filePath, "Models"))
				return Proceed;

			if (string.Equals(filePath, "EntityModels"))
				return Proceed && GenerateEntity;

			if (string.Equals(filePath, "ResourceModels"))
				return Proceed && GenerateResource;

			return Proceed;
		}
	}
}
