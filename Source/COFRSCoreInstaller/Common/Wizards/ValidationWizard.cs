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
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
	public class ValidationWizard : IWizard
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
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressDialog progressDialog = new ProgressDialog("Loading classes and preparing project...");

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
				HandleMessages();

				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();

				var installationFolder = StandardUtils.GetInstallationFolder(_appObject);
				HandleMessages();

				projectMapping = LoadProjectMapping(_appObject,
									projectMapping,
									installationFolder,
									out ProjectFolder entityModelsFolder,
									out ProjectFolder resourceModelsFolder,
									out ProjectFolder validadtionFolder);
				HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				//  Make sure we are where we're supposed to be
				if (!StandardUtils.IsChildOf(validadtionFolder.Folder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

					var result = MessageBox.Show($"You are attempting to install a validator model into {StandardUtils.GetRelativeFolder(_appObject.Solution, installationFolder)}. Typically, validator models reside in {StandardUtils.GetRelativeFolder(_appObject.Solution, validadtionFolder)}.\r\n\r\nDo you wish to place the new validator model in this non-standard location?",
						"Warning: Non-Standard Location",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						Proceed = false;
						return;
					}

					progressDialog = new ProgressDialog("Loading classes and preparing project...");
					progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
					_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
					HandleMessages();

					validadtionFolder = installationFolder;

					projectMapping.ValidationFolder = validadtionFolder.Folder;
					projectMapping.ValidationNamespace = validadtionFolder.Namespace;
					projectMapping.ValidationProject = validadtionFolder.ProjectName;

					StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
				}

				var entityMap = StandardUtils.LoadEntityModels(_appObject.Solution, entityModelsFolder);
				HandleMessages();

				var defultServerType = StandardUtils.GetDefaultServerType(connectionString);

				var resourceMap = StandardUtils.LoadResourceModels(_appObject.Solution, entityMap, resourceModelsFolder, defultServerType);
				HandleMessages();

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					EntityMap = entityMap,
					ResourceMap = resourceMap,
					InstallType = 3
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				progressDialog = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					var entityModel = (EntityModel)form._entityModelList.SelectedItem;
					var resourceModel = (ResourceModel)form._resourceModelList.SelectedItem;
					var profileMap = LoadMapping(solutionPath, resourceModel, entityModel);

					var emitter = new StandardEmitter();
					var model = emitter.EmitValidationModel(resourceModel, profileMap, resourceMap, entityMap, replacementsDictionary["$safeitemname$"], out string validatorInterface);

					var orchestrationNamespace = StandardUtils.FindOrchestrationNamespace(_appObject.Solution);

					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);

					StandardUtils.RegisterValidationModel(_appObject.Solution,
													  replacementsDictionary["$safeitemname$"],
													  replacementsDictionary["$rootnamespace$"]);
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

		private void HandleMessages()
		{
			while (WinNative.PeekMessage(out WinNative.NativeMessage msg, IntPtr.Zero, 0, (uint)0xFFFFFFFF, 1) != 0)
			{
				WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
			}
		}

		private static ProjectMapping LoadProjectMapping(DTE2 _appObject, ProjectMapping projectMapping, ProjectFolder installationFolder, out ProjectFolder entityModelsFolder, out ProjectFolder resourceModelsFolder, out ProjectFolder validationFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (projectMapping == null)
			{
				entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);

				if (entityModelsFolder == null)
					entityModelsFolder = installationFolder;

				resourceModelsFolder = StandardUtils.FindResourceModelsFolder(_appObject.Solution);

				if (resourceModelsFolder == null)
					resourceModelsFolder = installationFolder;

				validationFolder = StandardUtils.FindValidationFolder(_appObject.Solution);

				if (validationFolder == null)
					validationFolder = installationFolder;
				
				projectMapping = new ProjectMapping
				{
					EntityFolder = entityModelsFolder.Folder,
					EntityNamespace = entityModelsFolder.Namespace,
					EntityProject = entityModelsFolder.ProjectName,
					ResourceFolder = resourceModelsFolder.Folder,
					ResourceNamespace = resourceModelsFolder.Namespace,
					ResourceProject = resourceModelsFolder.ProjectName,
					ValidationFolder = validationFolder.Folder,
					ValidationNamespace = validationFolder.Namespace,
					ValidationProject = validationFolder.ProjectName,
					IncludeSDK = !string.Equals(entityModelsFolder.ProjectName, resourceModelsFolder.ProjectName, StringComparison.Ordinal)
				};

				StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(projectMapping.EntityProject) ||
					string.IsNullOrWhiteSpace(projectMapping.EntityNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.EntityFolder))
				{
					entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);

					if (entityModelsFolder == null)
						entityModelsFolder = installationFolder;

					projectMapping.EntityFolder = entityModelsFolder.Folder;
					projectMapping.EntityNamespace = entityModelsFolder.Namespace;
					projectMapping.EntityProject = entityModelsFolder.ProjectName;

					StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					entityModelsFolder = new ProjectFolder
					{
						Folder = projectMapping.EntityFolder,
						Namespace = projectMapping.EntityNamespace,
						ProjectName = projectMapping.EntityProject,
						Name = Path.GetFileName(projectMapping.EntityFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.ResourceProject) ||
					string.IsNullOrWhiteSpace(projectMapping.ResourceNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.ResourceFolder))
				{
					resourceModelsFolder = StandardUtils.FindResourceModelsFolder(_appObject.Solution);

					if (resourceModelsFolder == null)
						resourceModelsFolder = installationFolder;

					projectMapping.ResourceFolder = resourceModelsFolder.Folder;
					projectMapping.ResourceNamespace = resourceModelsFolder.Namespace;
					projectMapping.ResourceProject = resourceModelsFolder.ProjectName;

					StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					resourceModelsFolder = new ProjectFolder
					{
						Folder = projectMapping.ResourceFolder,
						Namespace = projectMapping.ResourceNamespace,
						ProjectName = projectMapping.ResourceProject,
						Name = Path.GetFileName(projectMapping.ResourceFolder)
					};
				}

				if (string.IsNullOrWhiteSpace(projectMapping.MappingProject) ||
					string.IsNullOrWhiteSpace(projectMapping.MappingNamespace) ||
					string.IsNullOrWhiteSpace(projectMapping.MappingFolder))
				{
					validationFolder = StandardUtils.FindValidationFolder(_appObject.Solution);

					if (validationFolder == null)
						validationFolder = installationFolder;

					projectMapping.ValidationProject = validationFolder.Folder;
					projectMapping.ValidationNamespace = validationFolder.Namespace;
					projectMapping.ValidationFolder = validationFolder.ProjectName;

					StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
				}
				else
				{
					validationFolder = new ProjectFolder
					{
						Folder = projectMapping.ValidationFolder,
						Namespace = projectMapping.ValidationNamespace,
						ProjectName = projectMapping.ValidationProject,
						Name = Path.GetFileName(projectMapping.ValidationFolder)
					};
				}
			}

			return projectMapping;
		}

		private ProfileMap LoadMapping(string solutionPath, ResourceModel resourceModel, EntityModel entityModel)
		{
			var filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs"), $"{resourceModel.ClassName}.{entityModel.ClassName}.json");
			var jsonValue = File.ReadAllText(filePath);

			return JsonConvert.DeserializeObject<ProfileMap>(jsonValue);
		}
	}
}
