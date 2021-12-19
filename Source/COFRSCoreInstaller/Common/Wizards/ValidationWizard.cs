using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
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
			DTE2 _appObject = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			try
			{
				//	Show the user that we are busy doing things...
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				var projectMapping = codeService.LoadProjectMapping();
				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;

				//  Make sure we are where we're supposed to be
				if (!codeService.IsChildOf(projectMapping.ValidationFolder, installationFolder.Folder))
				{
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
					var validationFolder = projectMapping.GetValidatorFolder();

					var result = MessageBox.Show($"You are attempting to install a validator model into {codeService.GetRelativeFolder(installationFolder)}. Typically, validator models reside in {codeService.GetRelativeFolder(validationFolder)}.\r\n\r\nDo you wish to place the new validator model in this non-standard location?",
						"Warning: Non-Standard Location",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						Proceed = false;
						return;
					}

					_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					validationFolder = installationFolder;

					projectMapping.ValidationFolder = validationFolder.Folder;
					projectMapping.ValidationNamespace = validationFolder.Namespace;
					projectMapping.ValidationProject = validationFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					InstallType = 3
				};

				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				if (form.ShowDialog() == DialogResult.OK)
				{
					var resourceModel = (ResourceClass)form._resourceModelList.SelectedItem;
					var profileMap = codeService.OpenProfileMap(resourceModel, out bool isAllDefined);

					var emitter = new StandardEmitter();
					//var model = emitter.EmitValidationModel(resourceModel, profileMap, resourceMap, replacementsDictionary["$safeitemname$"], out string ValidatorInterface);

					var orchestrationNamespace = codeService.FindOrchestrationNamespace();

					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$model$", "");
					replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);

					codeService.RegisterValidationModel(replacementsDictionary["$safeitemname$"],
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
	}
}
