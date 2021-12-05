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

				var projectMapping = COFRSCommonUtilities.OpenProjectMapping(_appObject);
				HandleMessages();

				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();

				var installationFolder = COFRSCommonUtilities.GetInstallationFolder(_appObject);
				HandleMessages();

				var connectionString = COFRSCommonUtilities.GetConnectionString(_appObject);
				HandleMessages();

				//  Make sure we are where we're supposed to be
				if (!COFRSCommonUtilities.IsChildOf(projectMapping.ValidationFolder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
					var validationFolder = projectMapping.GetValidatorFolder();

					var result = MessageBox.Show($"You are attempting to install a validator model into {COFRSCommonUtilities.GetRelativeFolder(_appObject, installationFolder)}. Typically, validator models reside in {COFRSCommonUtilities.GetRelativeFolder(_appObject, validationFolder)}.\r\n\r\nDo you wish to place the new validator model in this non-standard location?",
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

					validationFolder = installationFolder;

					projectMapping.ValidationFolder = validationFolder.Folder;
					projectMapping.ValidationNamespace = validationFolder.Namespace;
					projectMapping.ValidationProject = validationFolder.ProjectName;

					COFRSCommonUtilities.SaveProjectMapping(_appObject, projectMapping);
				}

				var entityMap = COFRSCommonUtilities.LoadEntityMap(_appObject);
				HandleMessages();

				var defultServerType = COFRSCommonUtilities.GetDefaultServerType(_appObject);

				var resourceMap = COFRSCommonUtilities.LoadResourceMap(_appObject);
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
					var model = emitter.EmitValidationModel(resourceModel, profileMap, resourceMap, entityMap, replacementsDictionary["$safeitemname$"], out string ValidatorInterface);

					var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(_appObject);

					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);

					COFRSCommonUtilities.RegisterValidationModel(_appObject,
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

		private ProfileMap LoadMapping(string solutionPath, ResourceModel resourceModel, EntityModel entityModel)
		{
			var filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs"), $"{resourceModel.ClassName}.{entityModel.ClassName}.json");
			var jsonValue = File.ReadAllText(filePath);

			return JsonConvert.DeserializeObject<ProfileMap>(jsonValue);
		}
	}
}
