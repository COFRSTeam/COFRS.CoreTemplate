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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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
			var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell2;
			ProgressForm progressDialog = new ProgressForm(_appObject, uiShell, "Loading classes and preparing project...");

			try
			{
				var projectMapping = codeService.LoadProjectMapping();
				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();
				var installationFolder = COFRSCommonUtilities.GetInstallationFolder();
				var connectionString = COFRSCommonUtilities.GetConnectionString();

				//  Make sure we are where we're supposed to be
				if (!COFRSCommonUtilities.IsChildOf(projectMapping.ExampleFolder, installationFolder.Folder))
				{
					var exampleFolder = projectMapping.GetExamplesFolder();

					var result = MessageBox.Show($"You are attempting to install an example model into {COFRSCommonUtilities.GetRelativeFolder(_appObject, installationFolder)}. Typically, example models reside in {COFRSCommonUtilities.GetRelativeFolder(_appObject, exampleFolder)}.\r\n\r\nDo you wish to place the new example model in this non-standard location?",
						"Warning: Non-Standard Location",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.No)
					{
						Proceed = false;
						return;
					}

					var validationFolder = installationFolder;

					projectMapping.ValidationFolder = validationFolder.Folder;
					projectMapping.ValidationNamespace = validationFolder.Namespace;
					projectMapping.ValidationProject = validationFolder.ProjectName;

					codeService.SaveProjectMapping();
				}

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					InstallType = 2
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					var resourceModel = (ResourceClass)form._resourceModelList.SelectedItem;
					//var profileMap = LoadMapping(solutionPath, resourceModel, entityModel);

					var emitter = new StandardEmitter();
					//var model = emitter.EmitExampleModel(resourceModel, profileMap, replacementsDictionary["$safeitemname$"], defaultServerType, connectionString);

					var orchestrationNamespace = COFRSCommonUtilities.FindOrchestrationNamespace(_appObject);

					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$model$", "");
					replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);

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

		private ProfileMap LoadMapping(string solutionPath, ResourceModel resourceModel, EntityModel entityModel)
		{
			var filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs"), $"{resourceModel.ClassName}.{entityModel.ClassName}.json");
			var jsonValue = File.ReadAllText(filePath);

			return JsonConvert.DeserializeObject<ProfileMap>(jsonValue);
		}
	}
}
