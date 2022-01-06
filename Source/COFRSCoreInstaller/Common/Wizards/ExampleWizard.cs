using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;

namespace COFRS.Template.Common.Wizards
{
    public class ExampleWizard : IWizard
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
			var codeService = COFRSServiceFactory.GetService<ICodeService>();	
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

			try
			{
				var projectMapping = codeService.LoadProjectMapping();
				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;

				//  Make sure we are where we're supposed to be
				if (!codeService.IsChildOf(projectMapping.ExampleFolder, installationFolder.Folder))
				{
					var exampleFolder = projectMapping.GetExamplesFolder();

					if (!VsShellUtilities.PromptYesNo(
						    $"You are attempting to install an example model into {codeService.GetRelativeFolder(installationFolder)}. Typically, example models reside in {codeService.GetRelativeFolder(exampleFolder)}.\r\n\r\nDo you wish to place the new example model in this non-standard location?", 
						    "Microsoft Visual Studio",
							OLEMSGICON.OLEMSGICON_WARNING,
							shell))
					{
						Proceed = false;
						return;
					}

					exampleFolder = installationFolder;

					projectMapping.ExampleFolder = exampleFolder.Folder;
					projectMapping.ExampleNamespace = exampleFolder.Namespace;
					projectMapping.ExampleProject = exampleFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new ExamplesDialog()
				{
					DefaultConnectionString = connectionString,
					ServiceProvider = ServiceProvider.GlobalProvider
				};

				var result = form.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					var resourceModel = form.ResourceModel;
					var profileMap = codeService.OpenProfileMap(resourceModel, out bool isAllDefined);

					var emitter = new Emitter();
					var model = emitter.EmitExampleModel(resourceModel, profileMap, replacementsDictionary["$safeitemname$"], form.ServerType, connectionString);

					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);

					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
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
