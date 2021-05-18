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
				var parent = new WindowClass((IntPtr) _appObject.ActiveWindow.HWnd);

				progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(parent);

				HandleMessages();

				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				//	Get folders and namespaces
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				replacementsDictionary["$entitynamespace$"] = $"{rootNamespace}.Models.EntityModels";
				replacementsDictionary["$resourcenamespace$"] = $"{rootNamespace}.Models.ResourceModels";
				replacementsDictionary["$orchestrationnamespace$"] = $"{rootNamespace}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = $"{rootNamespace}.Validation";
				replacementsDictionary["$validationnamespace$"] = $"{rootNamespace}.Validation";
				replacementsDictionary["$singleexamplenamespace$"] = $"{rootNamespace}.Models.SwaggerExamples";
				
				var candidateName = replacementsDictionary["$safeitemname$"];

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);

				HandleMessages();

				var programfiles = StandardUtils.LoadProgramDetail(_appObject.Solution);
				HandleMessages();

				var classList = StandardUtils.LoadClassList(programfiles);
				HandleMessages();


				var form = new UserInputFullStack
				{
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					RootNamespace = rootNamespace,
					ReplacementsDictionary = replacementsDictionary,
					ClassList = classList,
					EntityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution),
					Policies = StandardUtils.LoadPolicies(_appObject.Solution),
					DefaultConnectionString = StandardUtils.GetConnectionString(_appObject.Solution)
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

					//	Replace the ConnectionString
					var connectionString = form.ConnectionString;
					StandardUtils.ReplaceConnectionString(_appObject.Solution, connectionString);
					HandleMessages();

					var entityClassName = $"E{form.SingularResourceName}";
					var resourceClassName = form.SingularResourceName;
					var mappingClassName = $"{form.PluralResourceName}Profile";
					var exampleClassName = $"{form.PluralResourceName}Example";
					var exampleCollectionClassName = $"Collection{form.PluralResourceName}Example";
					var validationClassName = $"{form.PluralResourceName}Validator";
					var controllerClassName = $"{form.PluralResourceName}Controller";

					replacementsDictionary["$entityClass$"] = entityClassName;
					replacementsDictionary["$resourceClass$"] = resourceClassName;
					replacementsDictionary["$swaggerClass$"] = exampleClassName;
					replacementsDictionary["$swaggerCollectionClass$"] = exampleCollectionClassName;
					replacementsDictionary["$mapClass$"] = mappingClassName;
					replacementsDictionary["$validatorClass$"] = validationClassName;
					replacementsDictionary["$controllerClass$"] = controllerClassName;

					var moniker = StandardUtils.LoadMoniker(_appObject.Solution);
					var policy = form.Policy;
					HandleMessages();

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					List<ClassFile> composits = form.UndefinedClassList;

					var emitter = new Emitter();
					var standardEmitter = new StandardEmitter();

					if (form.ServerType == DBServerType.POSTGRESQL)
					{
						standardEmitter.GenerateComposites(composits, connectionString, replacementsDictionary, form.ClassList);
						HandleMessages();

						foreach (var composite in composits)
						{
							var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
							pj.Project.ProjectItems.AddFromFile(composite.FileName);

							StandardUtils.RegisterComposite(_appObject.Solution, composite);
						}
					}

					//	Emit Entity Model
					var entityModel = standardEmitter.EmitEntityModel(form.ServerType, form.DatabaseTable, entityClassName, form.ClassList, form.DatabaseColumns, replacementsDictionary, connectionString);
					replacementsDictionary.Add("$entityModel$", entityModel);
					HandleMessages();

					//List<ClassMember> classMembers = LoadClassMembers(form.ServerType, form.ClassList, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary["$solutiondirectory$"], connectionString);
					//List<ClassFile> ClassList = null;

					//if (form.ServerType == DBServerType.POSTGRESQL)
					//{
					//	ClassList = Utilities.LoadDetailEntityClassList(replacementsDictionary["$solutiondirectory$"], connectionString);
					//	HandleMessages();
					//}

					//	Emit Resource Model
					//var resourceModel = standardEmitter.EmitResourceModel(form.ServerType, ClassList, classMembers, resourceClassName, entityClassName, replacementsDictionary);
					//replacementsDictionary.Add("$resourceModel$", resourceModel);
					HandleMessages();

					//	Emit Mapping Model
					//var mappingModel = emitter.EmitMappingModel(form.ServerType, classMembers, resourceClassName, entityClassName, mappingClassName, form.DatabaseColumns, replacementsDictionary);
					//replacementsDictionary.Add("$mappingModel$", mappingModel);
					HandleMessages();

					//	Emit Example Model
					//var exampleModel = emitter.EmitExampleModel(
					//						form.ServerType,
					//						form.DatabaseTable.Schema,
					//						connectionString,
					//						classMembers,
					//						entityClassName,
					//						resourceClassName,
					//						exampleClassName,
					//						form.DatabaseColumns, form.Examples, replacementsDictionary,
					//						ClassList);
					//replacementsDictionary.Add("$exampleModel$", exampleModel);
					HandleMessages();


					//var exampleCollectionModel = emitter.EmitExampleCollectionModel(
					//	form.ServerType,
					//	form.DatabaseTable.Schema,
					//	connectionString,
					//	classMembers,
					//	entityClassName,
					//	resourceClassName,
					//	exampleCollectionClassName,
					//	form.DatabaseColumns, form.Examples, replacementsDictionary,
					//	ClassList);
					//replacementsDictionary.Add("$exampleCollectionModel$", exampleCollectionModel);
					HandleMessages();

					//	Emit Validation Model
					var validationModel = emitter.EmitValidationModel(resourceClassName, validationClassName);
					replacementsDictionary.Add("$validationModel$", validationModel);
					HandleMessages();

					//	Register the validation model

					StandardUtils.RegisterValidationModel(_appObject.Solution, 
						                              validationClassName,
													  replacementsDictionary["$validatornamespace$"]);

					//	Emit Controller
					//var controllerModel = emitter.EmitController(form.ServerType, classMembers,
					//			   true,
					//			   moniker,
					//			   resourceClassName,
					//			   controllerClassName,
					//			   validationClassName,
					//			   exampleClassName,
					//			   exampleCollectionClassName,
					//			   policy);
					//replacementsDictionary.Add("$controllerModel$", controllerModel);
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
			WinNative.NativeMessage msg;

			while ( WinNative.PeekMessage(out msg, IntPtr.Zero, 0, (uint) 0xFFFFFFFF, 1) != 0)
            {
				WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
            }
        }
	}
}
