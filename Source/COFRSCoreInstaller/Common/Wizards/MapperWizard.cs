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

					var result = MessageBox.Show($"You are attempting to install a resource/entity mapping model into {codeService.GetRelativeFolder(installationFolder)}. Typically, resource/entity mapping models reside in {codeService.GetRelativeFolder(mappingFolder)}.\r\n\r\nDo you wish to place the new resource/entity mapping model in this non-standard location?",
						"Warning: Non-Standard Location",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						Proceed = false;
						return;
					}

					mDte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					projectMapping.MappingFolder = installationFolder.Folder;
					projectMapping.MappingNamespace = installationFolder.Namespace;
					projectMapping.MappingProject = installationFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					InstallType = 1
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					var resourceModel = (ResourceClass)form._resourceModelList.SelectedItem;

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
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
