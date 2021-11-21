using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
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
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressDialog progressDialog = null;

			try
			{
				//	Full stack must start at the root namespace. Insure that we do...
				if (!StandardUtils.IsRootNamespace(_appObject.Solution, replacementsDictionary["$rootnamespace$"]))
				{
					MessageBox.Show("The COFRS Controller Full Stack should be placed at the project root. It will add the appropriate components in the appropriate folders.", "COFRS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Proceed = false;
					return;
				}

				//	Show the user that we are busy doing things...
				var parent = new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd);

				progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(parent);

				HandleMessages();

				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				//  Load the project mapping information
				var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
				HandleMessages();

				var installationFolder = StandardUtils.GetInstallationFolder(_appObject);
				HandleMessages();

				projectMapping = StandardUtils.LoadProjectMapping(_appObject,
													projectMapping,
													installationFolder,
													out ProjectFolder entityModelsFolder,
													out ProjectFolder resourceModelsFolder,
													out ProjectFolder mappingFolder,
													out ProjectFolder validationFolder,
													out ProjectFolder exampleFolder,
													out ProjectFolder controllersFolder);
				HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				var entityMap = StandardUtils.LoadEntityModels(_appObject.Solution, entityModelsFolder);
				HandleMessages();

				var defaultServerType = StandardUtils.GetDefaultServerType(connectionString);
				var resourceMap = StandardUtils.LoadResourceModels(_appObject.Solution, entityMap, resourceModelsFolder, defaultServerType);
				HandleMessages();

				var policies = StandardUtils.LoadPolicies(_appObject.Solution);
				HandleMessages();

				//	Get folders and namespaces
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				replacementsDictionary["$entitynamespace$"] = entityModelsFolder.Namespace;
				replacementsDictionary["$resourcenamespace$"] = resourceModelsFolder.Namespace;
				replacementsDictionary["$mappingnamespace$"] = mappingFolder.Namespace;
				replacementsDictionary["$orchestrationnamespace$"] = $"{rootNamespace}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = validationFolder.Namespace;
				replacementsDictionary["$validationnamespace$"] = validationFolder.Namespace;

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
					EntityModelsFolder = entityModelsFolder,
					Policies = policies,
					DefaultConnectionString = connectionString
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				if (form.ShowDialog() == DialogResult.OK)
				{
					//	Show the user that we are busy...
					progressDialog = new ProgressDialog("Building classes...");
					progressDialog.Show(parent);
					_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					HandleMessages();
					var projectName = StandardUtils.GetProjectName(_appObject.Solution);
					connectionString = $"{form.ConnectionString}Application Name={projectName}";

					//	Replace the ConnectionString
					StandardUtils.ReplaceConnectionString(_appObject.Solution, connectionString);
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

					var moniker = StandardUtils.LoadMoniker(_appObject.Solution);
					var policy = form.Policy;
					HandleMessages();

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					List<EntityModel> undefinedModels = form.UndefinedClassList;

					EntityModel entityModel = null;
					ResourceModel resourceModel = null;

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

							standardEmitter.GenerateComposites(_appObject.Solution,
															   undefinedModels,
															   form.ConnectionString,
															   replacementsDictionary,
															   entityMap,
															   entityModelsFolder);
							HandleMessages();

							foreach (var composite in undefinedModels)
							{
								//	TO DO: This is incorret - the item could reside in another project
								var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
								pj.Project.ProjectItems.AddFromFile(composite.Folder);

								StandardUtils.RegisterComposite(_appObject.Solution, composite);
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
							Folder = Path.Combine(entityModelsFolder.Folder, replacementsDictionary["$safeitemname$"])
						};

						StandardUtils.GenerateColumns(entityModel, form.ConnectionString);

						var emodel = standardEmitter.EmitEntityModel(entityModel,
																entityMap,
																replacementsDictionary);

						var existingEntities = entityMap.Maps.ToList();

						entityModel.Folder = Path.Combine(entityModelsFolder.Folder, $"{entityModel.ClassName}.cs");

						existingEntities.Add(entityModel);
						entityMap.Maps = existingEntities.ToArray();

						replacementsDictionary.Add("$entityModel$", emodel);
						HandleMessages();

						var classMembers = StandardUtils.LoadEntityClassMembers(entityModel);
					}
					else
                    {
						//	Since we're not generating an entity model, that must mean that one
						//	already exists.

						entityModel = entityMap.Maps.FirstOrDefault(m => string.Equals(m.ClassName, entityClassName, StringComparison.OrdinalIgnoreCase));
					}

					if ( entityModel == null )
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
							ProjectName = resourceModelsFolder.ProjectName,
							Namespace = resourceModelsFolder.Namespace,
							Folder = Path.Combine(resourceModelsFolder.Folder, $"{resourceClassName}.cs"),
							ClassName = resourceClassName,
							EntityModel = entityModel,
							ServerType = form.ServerType
						};

						var rmodel = standardEmitter.EmitResourceModel(resourceModel,
																	   resourceMap,
																	   replacementsDictionary);
						replacementsDictionary.Add("$resourceModel$", rmodel);

						var existingResources = resourceMap.Maps.ToList();

						resourceModel.Folder = Path.Combine(resourceModelsFolder.Folder, $"{resourceModel.ClassName}.cs");

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

                    #region Mapping Operations
                    if (form.MappingProfileCheckBox.Checked)
					{
						GenerateMapping = true;

                        HandleMessages();
						progressDialog.Close();
						_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                        Mapper mapperDialog = new Mapper
                        {

                            //	Emit Mapping Model
                            ResourceModel = resourceModel,
                            ResourceModels = resourceMap.Maps.ToList(),
                            EntityModels = entityMap.Maps.ToList()
                        };

						if (mapperDialog.ShowDialog() == DialogResult.OK)
						{
							StandardUtils.SaveProfileMap(_appObject.Solution, mapperDialog.ProfileMap);

							var mappingModel = standardEmitter.EmitMappingModel(resourceModel, resourceModel.EntityModel, mapperDialog.ProfileMap, mappingClassName, replacementsDictionary);

							replacementsDictionary.Add("$mappingModel$", mappingModel);
						}
						else
							GenerateMapping = false;

						progressDialog = new ProgressDialog("Building classes...");
						progressDialog.Show(parent);
						_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
						HandleMessages();
					}
					#endregion

					var validatorInterface = string.Empty;

                    #region Validation Operations
                    if (form.ValidatorCheckBox.Checked)
					{
						GenerateValidator = true;

						var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();
						var profileMap = LoadMapping(solutionPath, resourceModel, entityModel);
						var orchestrationNamespace = StandardUtils.FindOrchestrationNamespace(_appObject.Solution);

						//	Emit Validation Model
						var validationModel = standardEmitter.EmitValidationModel(resourceModel, profileMap, resourceMap, entityMap, validationClassName, out validatorInterface);
						replacementsDictionary.Add("$validationModel$", validationModel);
						HandleMessages();

						//	Register the validation model
						StandardUtils.RegisterValidationModel(_appObject.Solution, 
														  validationClassName,
														  replacementsDictionary["$validatornamespace$"]);
					}
                    #endregion

                    #region Example Operations
                    if (form.ExampleCheckBox.Checked)
                    {
						GenerateExample = true;
						var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();
						var profileMap = LoadMapping(solutionPath, resourceModel, entityModel);

						var exampleModel = standardEmitter.EmitExampleModel(resourceModel, profileMap, resourceMap, entityMap, exampleClassName, defaultServerType, connectionString);
						replacementsDictionary.Add("$examplemodel$", exampleModel);
					}
					#endregion

					#region Controller Operations
					if (form.ControllerCheckbox.Checked)
					{
						GenerateController = true;

						var controllerModel = emitter.EmitController(
							_appObject,
							entityModel,
							resourceModel,
							moniker,
							controllerClassName,
							validatorInterface,
							policy,
							validationFolder.Namespace);

						replacementsDictionary.Add("$controllerModel$", controllerModel);
						HandleMessages();
					}
					#endregion

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception ex)
			{
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

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

		private ProfileMap LoadMapping(string solutionPath, ResourceModel resourceModel, EntityModel entityModel)
		{
			var filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs"), $"{resourceModel.ClassName}.{entityModel.ClassName}.json");
			var jsonValue = File.ReadAllText(filePath);

			return JsonConvert.DeserializeObject<ProfileMap>(jsonValue);
		}

	}
}
