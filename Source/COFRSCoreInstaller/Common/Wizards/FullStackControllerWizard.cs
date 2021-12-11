using COFRS.Template.Common.Forms;
using COFRS.Template.Common.ServiceUtilities;
using COFRSCoreCommon.Forms;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
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
			ProgressDialog progressDialog = null;

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

				progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(parent);

				HandleMessages();

				dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				//  Load the project mapping information
				var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte);
				HandleMessages();

				var installationFolder = COFRSCommonUtilities.GetInstallationFolder(dte);
				HandleMessages();

				var connectionString = COFRSCommonUtilities.GetConnectionString(dte);
				HandleMessages();

				var entityMap = COFRSCommonUtilities.LoadEntityMap(dte);
				HandleMessages();

				var defaultServerType = COFRSCommonUtilities.GetDefaultServerType(dte);
				var resourceMap = COFRSCommonUtilities.LoadResourceMap(dte);
				HandleMessages();

				var policies = COFRSCommonUtilities.LoadPolicies(dte);
				HandleMessages();

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

				HandleMessages();

				var form = new UserInputFullStack
				{
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					RootNamespace = rootNamespace,
					ReplacementsDictionary = replacementsDictionary,
					EntityMap = entityMap,
					EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
					Policies = policies,
					DefaultConnectionString = connectionString
				};

				HandleMessages();

				progressDialog.Close();
				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				EntityModel entityModel = null;
				ResourceModel resourceModel = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					//	Show the user that we are busy...
					progressDialog = new ProgressDialog("Building classes...");
					progressDialog.Show(parent);
					dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					HandleMessages();
					connectionString = $"{form.ConnectionString}Application Name={projectMapping.ControllersProject}";

					//	Replace the ConnectionString
					COFRSCommonUtilities.ReplaceConnectionString(dte, connectionString);
					HandleMessages();

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
					HandleMessages();

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					List<EntityModel> undefinedModels = form.UndefinedClassList;

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

							var definedList = new List<EntityModel>();
							definedList.AddRange(undefinedModels);

							standardEmitter.GenerateComposites(dte,
															   undefinedModels,
															   form.ConnectionString,
															   replacementsDictionary,
															   entityMap,
															   projectMapping.GetEntityModelsFolder());
							HandleMessages();

							foreach (var composite in undefinedModels)
							{
								//	TO DO: This is incorret - the item could reside in another project
								var pj = (VSProject)dte.Solution.Projects.Item(1).Object;
								pj.Project.ProjectItems.AddFromFile(composite.Folder);

								COFRSCommonUtilities.RegisterComposite(dte, composite);
							}

						}

						//	Emit Entity Model
						entityModel = new EntityModel()
						{
							ClassName = entityClassName,
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							Namespace = replacementsDictionary["$rootnamespace$"],
							ElementType = ElementType.Composite,
							Folder = Path.Combine(projectMapping.EntityFolder, replacementsDictionary["$safeitemname$"])
						};

						DBHelper.GenerateColumns(entityModel, form.ConnectionString);

						var emodel = standardEmitter.EmitEntityModel(entityModel,
																entityMap,
																replacementsDictionary);

						var existingEntities = entityMap.Maps.ToList();

						entityModel.Folder = Path.Combine(projectMapping.EntityFolder, $"{entityModel.ClassName}.cs");

						existingEntities.Add(entityModel);
						entityMap.Maps = existingEntities.ToArray();

						replacementsDictionary.Add("$entityModel$", emodel);
						HandleMessages();
					}
					else
					{
						//	Since we're not generating an entity model, that must mean that one
						//	already exists.

						entityModel = entityMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, entityClassName, StringComparison.OrdinalIgnoreCase));
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

						//	Emit Resource Model

						HandleMessages();

						resourceModel = new ResourceModel()
						{
							ProjectName = projectMapping.ResourceProject,
							Namespace = projectMapping.ResourceNamespace,
							Folder = Path.Combine(projectMapping.ResourceFolder, $"{resourceClassName}.cs"),
							ClassName = resourceClassName,
							EntityModel = entityModel,
							ServerType = form.ServerType
						};

						var rmodel = standardEmitter.EmitResourceModel(resourceModel,
																	   resourceMap,
																	   replacementsDictionary);
						replacementsDictionary.Add("$resourceModel$", rmodel);

						var existingResources = resourceMap.Maps.ToList();

						resourceModel.Folder = Path.Combine(projectMapping.ResourceFolder, $"{resourceModel.ClassName}.cs");

						existingResources.Add(resourceModel);
						resourceMap.Maps = existingResources.ToArray();
						HandleMessages();
					}
					else
					{
						//	Since we're not generating a resource model, that must mean that one
						//	already exists.

						resourceModel = resourceMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, resourceClassName, StringComparison.OrdinalIgnoreCase));
					}
					#endregion

					ProfileMap profileMap = null;

					#region Mapping Operations
					if (form.MappingProfileCheckBox.Checked)
					{
						GenerateMapping = true;

						if (resourceModel == null )
							resourceModel = resourceMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, resourceClassName, StringComparison.OrdinalIgnoreCase));
						profileMap = COFRSCommonUtilities.GenerateProfileMap(resourceModel, resourceMap);

						var mappingModel = standardEmitter.EmitMappingModel(resourceModel, resourceModel.EntityModel, profileMap, mappingClassName, replacementsDictionary);

						replacementsDictionary.Add("$mappingModel$", mappingModel);
					}
					else
						GenerateMapping = false;
                    #endregion

					#region Validation Operations
                    var validatorInterface = string.Empty;
					if (form.ValidatorCheckBox.Checked)
					{
						GenerateValidator = true;

						if ( resourceModel == null)
							resourceModel = resourceMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, resourceClassName, StringComparison.OrdinalIgnoreCase));

						if ( profileMap == null)
							profileMap = COFRSCommonUtilities.OpenProfileMap(dte, resourceModel, out bool IsAllDefined);
						var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(dte);

						//	Emit Validation Model
						var validationModel = standardEmitter.EmitValidationModel(resourceModel, profileMap, resourceMap, entityMap, validationClassName, out validatorInterface);
						replacementsDictionary.Add("$validationModel$", validationModel);
						HandleMessages();

						//	Register the validation model
						COFRSCommonUtilities.RegisterValidationModel(dte,
														  validationClassName,
														  replacementsDictionary["$validatornamespace$"]);
					}
					#endregion

					#region Example Operations
					if (form.ExampleCheckBox.Checked)
					{
						GenerateExample = true;

						if ( resourceModel == null)
							resourceModel = resourceMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, resourceClassName, StringComparison.OrdinalIgnoreCase));

						if ( profileMap== null)
							profileMap = COFRSCommonUtilities.OpenProfileMap(dte, resourceModel, out bool IsAllDefined);

						var exampleModel = standardEmitter.EmitExampleModel(resourceModel, profileMap, entityMap, exampleClassName, defaultServerType, connectionString);
						replacementsDictionary.Add("$examplemodel$", exampleModel);
					}
					#endregion

					#region Controller Operations
					if (form.ControllerCheckbox.Checked)
					{
						GenerateController = true;

						if (resourceModel == null)
							resourceModel = resourceMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, resourceClassName, StringComparison.OrdinalIgnoreCase));

						var controllerModel = emitter.EmitController(
							dte,
							entityModel,
							resourceModel,
							moniker,
							controllerClassName,
							validatorInterface,
							policy,
							projectMapping.ControllersNamespace);

						replacementsDictionary.Add("$controllerModel$", controllerModel);
						HandleMessages();
					}
					#endregion
				}

				progressDialog.Close();
				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				Proceed = true;
			}
			catch (Exception ex)
			{
				dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				if (progressDialog != null)
					progressDialog.Close();

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

		private void HandleMessages()
        {
            while (WinNative.PeekMessage(out WinNative.NativeMessage msg, IntPtr.Zero, 0, (uint)0xFFFFFFFF, 1) != 0)
            {
                WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
            }
        }
	}
}
