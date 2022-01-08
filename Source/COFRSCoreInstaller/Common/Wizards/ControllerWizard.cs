using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
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
			var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
			IVsThreadedWaitDialog2 waitDialog = null;

			try
			{
				var projectMapping = codeService.LoadProjectMapping();
				var solutionPath = dte2.Solution.Properties.Item("Path").Value.ToString();
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;

				//  Make sure we are where we're supposed to be
				if (!codeService.IsChildOf(projectMapping.ControllersFolder, installationFolder.Folder))
				{
					var controllersFolder = projectMapping.GetControllersFolder();

					if (!VsShellUtilities.PromptYesNo(
							$"You are attempting to install a controller model into {codeService.GetRelativeFolder(installationFolder)}. Typically, controller models reside in {codeService.GetRelativeFolder(controllersFolder)}.\r\n\r\nDo you wish to place the new controller model in this non-standard location?",
							"Microsoft Visual Studio",
							OLEMSGICON.OLEMSGICON_WARNING,
							shell))
					{
						Proceed = false;
						return;
					}

					controllersFolder = installationFolder;

					projectMapping.ControllersFolder = controllersFolder.Folder;
					projectMapping.ControllersNamespace = controllersFolder.Namespace;
					projectMapping.ControllersProject = controllersFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new ControllerDialog()
				{
					DefaultConnectionString = connectionString,	
					ServiceProvider = ServiceProvider.GlobalProvider,
					Policies = codeService.Policies
				};

				var result = form.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					if (ServiceProvider.GlobalProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) is IVsThreadedWaitDialogFactory dialogFactory)
					{
						dialogFactory.CreateInstance(out waitDialog);
					}

					if (waitDialog != null && waitDialog.StartWaitDialog("Microsoft Visual Studio",
																 "Building controller",
																 $"Building {replacementsDictionary["$safeitemname$"]}",
																 null,
																 $"Building {replacementsDictionary["$safeitemname$"]}",
																 0,
																 false, true) == VSConstants.S_OK)
					{
						var resourceModel = form.ResourceModel;
						var moniker = codeService.Moniker;
						string policy = form.Policy;

						var orchestrationNamespace = codeService.FindOrchestrationNamespace();

						replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
						replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
						replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
						replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
						replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);
						replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
						replacementsDictionary.Add("$examplesnamespace$", projectMapping.ExampleNamespace);

						var emitter = new Emitter();
						var model = emitter.EmitController(
							resourceModel,
							moniker,
							replacementsDictionary["$safeitemname$"],
							policy);

						replacementsDictionary.Add("$model$", model);
						waitDialog.EndWaitDialog(out int usercancel);
					}

					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				if ( waitDialog != null )
					waitDialog.EndWaitDialog(out int usercancel);

				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
												error.Message,
												"Microsoft Visual Studio",
												OLEMSGICON.OLEMSGICON_CRITICAL,
												OLEMSGBUTTON.OLEMSGBUTTON_OK,
												OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
