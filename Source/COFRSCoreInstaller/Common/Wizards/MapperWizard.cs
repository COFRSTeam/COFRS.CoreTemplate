using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
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
    public class MapperWizard : IWizard
	{
		private bool Proceed = false;

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			DTE2 mDte = automationObject as DTE2;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

			try
			{
				//  Load the project mapping information
				var projectMapping = codeService.LoadProjectMapping();
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;

				//  Make sure we are where we're supposed to be
				if (!codeService.IsChildOf(projectMapping.MappingFolder, installationFolder.Folder))
				{
					mDte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
					var mappingFolder = projectMapping.GetMappingFolder();

					if (!VsShellUtilities.PromptYesNo(
							$"You are attempting to install a resource/entity mapping model into {codeService.GetRelativeFolder(installationFolder)}. Typically, resource/entity mapping models reside in {codeService.GetRelativeFolder(mappingFolder)}.\r\n\r\nDo you wish to place the new resource/entity mapping model in this non-standard location?", 
							"Microsoft Visual Studio",
							OLEMSGICON.OLEMSGICON_WARNING,
							shell))
					{
						Proceed = false;
						return;
					}

					projectMapping.MappingFolder = installationFolder.Folder;
					projectMapping.MappingNamespace = installationFolder.Namespace;
					projectMapping.MappingProject = installationFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new NewProfileDialog()
				{
					DefaultConnectionString = connectionString,
					ServiceProvider = ServiceProvider.GlobalProvider
				};

				var result = form.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					var resourceModel = form.ResourceModel;

					var emitter = new StandardEmitter();
					var model = emitter.EmitMappingModel(resourceModel, replacementsDictionary["$safeitemname$"], replacementsDictionary);

					replacementsDictionary["$resourcenamespace$"] = resourceModel.Namespace;
					replacementsDictionary["$entitynamespace$"] = resourceModel.Entity.Namespace;
					replacementsDictionary["$model$"] = model;

					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch ( Exception error )
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

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
