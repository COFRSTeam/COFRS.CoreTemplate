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
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;

			try
			{
				//	Show the user that we are busy doing things...
				dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				var projectMapping = codeService.LoadProjectMapping();
				var solutionPath = dte2.Solution.Properties.Item("Path").Value.ToString();
				var installationFolder = COFRSCommonUtilities.GetInstallationFolder();
				var connectionString = COFRSCommonUtilities.GetConnectionString();

				//  Make sure we are where we're supposed to be
				if (!COFRSCommonUtilities.IsChildOf(projectMapping.ControllersFolder, installationFolder.Folder))
				{
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

					dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					controllersFolder = installationFolder;

					projectMapping.ControllersFolder = controllersFolder.Folder;
					projectMapping.ControllersNamespace = controllersFolder.Namespace;
					projectMapping.ControllersProject = controllersFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					InstallType = 3
				};

				dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				if (form.ShowDialog() == DialogResult.OK)
				{
					var resourceModel = (ResourceClass)form._resourceModelList.SelectedItem;
					var moniker = COFRSCommonUtilities.LoadMoniker(dte2);
					string policy = string.Empty;

					if ( form.policyCombo.Items.Count > 0 )
						policy = form.policyCombo.SelectedItem.ToString();
					
					var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(dte2);
					var validatorInterface = COFRSCommonUtilities.FindValidatorInterface(dte2, resourceModel.ClassName);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);
					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$validationnamespace$", projectMapping.ValidationNamespace);
					replacementsDictionary.Add("$examplesnamespace$", projectMapping.ExampleNamespace);

					var emitter = new Emitter();
					var model = emitter.EmitController(
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
	}
}
