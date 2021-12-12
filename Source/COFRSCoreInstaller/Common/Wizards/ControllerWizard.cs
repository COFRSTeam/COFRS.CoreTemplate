using COFRS.Template.Common.Forms;
using COFRS.Template.Common.ServiceUtilities;
using COFRSCoreCommon.Forms;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
    public class ControllerWizard : IWizard
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
			DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressForm progressDialog = new ProgressForm("Loading classes and preparing project...");

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
				dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);
				HandleMessages();

				var solutionPath = dte2.Solution.Properties.Item("Path").Value.ToString();

				var installationFolder = COFRSCommonUtilities.GetInstallationFolder(dte2);
				HandleMessages();

				projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);
				HandleMessages();

				var connectionString = COFRSCommonUtilities.GetConnectionString(dte2);
				HandleMessages();

				//  Make sure we are where we're supposed to be
				if (!COFRSCommonUtilities.IsChildOf(projectMapping.ControllersFolder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
					var controllersFolder = projectMapping.GetControllersFolder();

					var result = MessageBox.Show($"You are attempting to install a controller model into {COFRSCommonUtilities.GetRelativeFolder(dte2, installationFolder)}. Typically, controller models reside in {COFRSCommonUtilities.GetRelativeFolder(dte2, controllersFolder)}.\r\n\r\nDo you wish to place the new controller model in this non-standard location?",
						"Warning: Non-Standard Location",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						Proceed = false;
						return;
					}

					progressDialog = new ProgressForm("Loading classes and preparing project...");
					progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
					dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
					HandleMessages();

					controllersFolder = installationFolder;

					projectMapping.ControllersFolder = controllersFolder.Folder;
					projectMapping.ControllersNamespace = controllersFolder.Namespace;
					projectMapping.ControllersProject = controllersFolder.ProjectName;

					COFRSCommonUtilities.SaveProjectMapping(dte2, projectMapping);
				}

				var entityMap = COFRSCommonUtilities.LoadEntityMap(dte2);
				HandleMessages();

				var defultServerType = COFRSCommonUtilities.GetDefaultServerType(dte2);

				var resourceMap = COFRSCommonUtilities.LoadResourceMap(dte2);
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
				dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				progressDialog = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					var entityModel = (EntityModel)form._entityModelList.SelectedItem;
					var resourceModel = (ResourceModel)form._resourceModelList.SelectedItem;
					var moniker = COFRSCommonUtilities.LoadMoniker(dte2);
					string policy = string.Empty;

					if ( form.policyCombo.Items.Count > 0 )
						policy = form.policyCombo.SelectedItem.ToString();
					
					var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(dte2);

					var validatorInterface = COFRSCommonUtilities.FindValidatorInterface(dte2, resourceModel.ClassName);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);
					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$validationnamespace$", projectMapping.ValidationNamespace);
					replacementsDictionary.Add("$examplesnamespace$", projectMapping.ExampleNamespace);

					var emitter = new Emitter();
					var model = emitter.EmitController(
						dte2,
						entityModel,
						resourceModel,
						moniker,
						replacementsDictionary["$safeitemname$"],
						validatorInterface,
						policy,
						projectMapping.ValidationNamespace);

					replacementsDictionary.Add("$model$", model);
					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				if (progressDialog != null)
					if (progressDialog.IsHandleCreated)
						progressDialog.Close();

				dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
