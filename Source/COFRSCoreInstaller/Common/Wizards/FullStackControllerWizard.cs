using COFRS.Template.Common.Forms;
using COFRS.Template.Common.ServiceUtilities;
using COFRSCoreCommon.Forms;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VSLangProj;

namespace COFRS.Template.Common.Wizards
{
    public class FullStackControllerWizard : IWizard
	{
		private bool Proceed = false;
		private bool GenerateEntity = false;
		private bool GenerateResource = false;
		private bool GenerateController = false;
		private bool GenerateExample = false;
		private bool GenerateValidator = false;
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
			var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell2;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			try
			{
				//	Full stack must start at the root namespace. Insure that we do...
				if (!COFRSCommonUtilities.IsRootNamespace(dte, replacementsDictionary["$rootnamespace$"]))
				{
					MessageBox.Show("The COFRS Controller Full Stack should be placed at the project root. It will add the appropriate components in the appropriate folders.", "COFRS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Proceed = false;
					return;
				}

				//	Show the user that we are busy doing things...
				var parent = new WindowClass((IntPtr)dte.ActiveWindow.HWnd);
				dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				//  Load the project mapping information
				var projectMapping = codeService.LoadProjectMapping();
				var installationFolder = COFRSCommonUtilities.GetInstallationFolder();
				var connectionString = COFRSCommonUtilities.GetConnectionString();

				var defaultServerType = COFRSCommonUtilities.GetDefaultServerType();

				var policies = COFRSCommonUtilities.LoadPolicies(dte);

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

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);

				var form = new UserInputFullStack
				{
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					RootNamespace = rootNamespace,
					ReplacementsDictionary = replacementsDictionary,
					EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
					Policies = policies,
					DefaultConnectionString = connectionString,
					dte2 = dte
				};

				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				EntityClass entityModel = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					//	Show the user that we are busy...
					dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
					connectionString = $"{form.ConnectionString}Application Name={projectMapping.ControllersProject}";

					//	Replace the ConnectionString
					COFRSCommonUtilities.ReplaceConnectionString(connectionString);

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

					var moniker = COFRSCommonUtilities.LoadMoniker(dte);
					var policy = form.Policy;

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					var emitter = new Emitter();
					var standardEmitter = new StandardEmitter();

					#region Entity Model Operations
					//	Should we generate an entity model?
					if (form.EntityModelCheckBox.Checked)
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


						replacementsDictionary.Add("$entityModel$", emodel);
					}

					if (entityModel == null)
					{
						MessageBox.Show($"No entity model was found for {entityClassName}. Generation canceled.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						Proceed = false;
						return;
					}
					#endregion

					#region Resource Model Operations
					if (form.ResourceModelCheckBox.Checked)
					{
						GenerateResource = true;

						var rmodel = standardEmitter.EmitResourceModel(entityModel,
																	   replacementsDictionary);

						replacementsDictionary.Add("$resourceModel$", rmodel);
					}
					#endregion

					ProfileMap profileMap = null;

					#region Mapping Operations
					if (form.MappingProfileCheckBox.Checked)
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
					//if (form.ValidatorCheckBox.Checked)
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
					//if (form.ExampleCheckBox.Checked)
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
					//if (form.ControllerCheckbox.Checked)
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

				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				Proceed = true;
			}
			catch (Exception ex)
			{
				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
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
