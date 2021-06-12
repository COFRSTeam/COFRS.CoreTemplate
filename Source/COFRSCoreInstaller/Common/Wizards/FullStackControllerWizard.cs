using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VSLangProj;

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


				var entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);
				HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				var programfiles = StandardUtils.LoadProgramDetail(_appObject.Solution);
				HandleMessages();

				var classList = StandardUtils.LoadClassList(programfiles);
				HandleMessages();

				var policies = StandardUtils.LoadPolicies(_appObject.Solution);
				HandleMessages();

				//	Get folders and namespaces
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				replacementsDictionary["$entitynamespace$"] = $"{rootNamespace}.Models.EntityModels";
				replacementsDictionary["$resourcenamespace$"] = $"{rootNamespace}.Models.ResourceModels";
				replacementsDictionary["$mappingnamespace$"] = $"{rootNamespace}.Mapping";
				replacementsDictionary["$orchestrationnamespace$"] = $"{rootNamespace}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = $"{rootNamespace}.Validation";
				replacementsDictionary["$validationnamespace$"] = $"{rootNamespace}.Validation";
				
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
					ClassList = classList,
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
					var controllerClassName = $"{form.PluralResourceName}Controller";

					replacementsDictionary["$entityClass$"] = entityClassName;
					replacementsDictionary["$resourceClass$"] = resourceClassName;
					replacementsDictionary["$mapClass$"] = mappingClassName;
					replacementsDictionary["$validatorClass$"] = validationClassName;
					replacementsDictionary["$controllerClass$"] = controllerClassName;

					var moniker = StandardUtils.LoadMoniker(_appObject.Solution);
					var policy = form.Policy;
					HandleMessages();

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					StandardUtils.EnsureFolder(_appObject.Solution, "Mapping");
					StandardUtils.EnsureFolder(_appObject.Solution, "Validation");
					StandardUtils.EnsureFolder(_appObject.Solution, "Controllers");
					StandardUtils.EnsureFolder(_appObject.Solution, "Models\\EntityModels");
					StandardUtils.EnsureFolder(_appObject.Solution, "Models\\ResourceModels");

					List<ClassFile> composits = form.UndefinedClassList;

					var emitter = new Emitter();
					var standardEmitter = new StandardEmitter();

					if (form.ServerType == DBServerType.POSTGRESQL)
					{
						//	Generate any undefined composits before we construct our entity model (because, 
						//	the entity model depends upon them)

						var definedList = new List<ClassFile>();
						definedList.AddRange(classList);
						definedList.AddRange(composits);

						standardEmitter.GenerateComposites(composits,
							                               form.ConnectionString, 
														   replacementsDictionary, 
														   definedList,
														   entityModelsFolder.Folder);
						HandleMessages();

						foreach (var composite in composits)
						{
							//	TO DO: This is incorret - the item could reside in another project
							var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
							pj.Project.ProjectItems.AddFromFile(composite.FileName);

							StandardUtils.RegisterComposite(_appObject.Solution, (EntityClassFile)composite);
						}

						classList.AddRange(composits);
					}

					//	Emit Entity Model
					var entityModel = standardEmitter.EmitEntityModel(form.ServerType, 
						                                              form.DatabaseTable, 
																	  entityClassName,
																	  classList, 
																	  form.DatabaseColumns, 
																	  replacementsDictionary, 
																	  out EntityClassFile entityClassFile);

					classList.Add(entityClassFile);

					replacementsDictionary.Add("$entityModel$", entityModel);
					HandleMessages();

					var classMembers = StandardUtils.LoadEntityClassMembers(entityClassFile);

					//	Emit Resource Model
					var resourceModel = standardEmitter.EmitResourceModel(entityClassFile, 
						                                                  classList, 
																		  classMembers, 
																		  resourceClassName, 
																		  replacementsDictionary,
																		  out ResourceClassFile resourceClassFile);
					replacementsDictionary.Add("$resourceModel$", resourceModel);
					HandleMessages();

					//	Emit Mapping Model
					var mappingModel = standardEmitter.EmitMappingModel(entityClassFile,
									 resourceClassFile,
									 mappingClassName,
									 replacementsDictionary);

					replacementsDictionary.Add("$mappingModel$", mappingModel);
					HandleMessages();


					//	Emit Validation Model
					var validationModel = standardEmitter.EmitValidationModel(resourceClassName, validationClassName, out string validatorInterface);
					replacementsDictionary.Add("$validationModel$", validationModel);
					HandleMessages();

					//	Register the validation model

					StandardUtils.RegisterValidationModel(_appObject.Solution, 
						                              validationClassName,
													  replacementsDictionary["$validatornamespace$"]);

					//	Emit Controller
					var controllerModel = emitter.EmitController(entityClassFile,
													   resourceClassFile,
	                                                   moniker,
													   controllerClassName,
	                                                   validatorInterface,
	                                                   policy);

					replacementsDictionary.Add("$controllerModel$", controllerModel);
					HandleMessages();

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
