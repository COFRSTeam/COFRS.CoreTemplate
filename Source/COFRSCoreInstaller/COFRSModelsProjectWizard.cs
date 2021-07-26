using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System.Collections.Generic;
namespace COFRS.Template
{
    public class COFRSModelsProjectWizard : IWizard
	{
		private bool Proceed;

		// This method is called before opening any item that has the OpenInEditor attribute.
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
			// Add custom parameters.
			replacementsDictionary.Add("$saferootprojectname$", CoreProjectWithSDKWizard.GlobalDictionary["$saferootprojectname$"]);

			// Add custom parameters.
			replacementsDictionary.Add("$framework$", CoreProjectWithSDKWizard.GlobalDictionary["$framework$"]);
			replacementsDictionary.Add("$securitymodel$", CoreProjectWithSDKWizard.GlobalDictionary["$securitymodel$"]);
			replacementsDictionary.Add("$databaseTechnology$", CoreProjectWithSDKWizard.GlobalDictionary["$databaseTechnology$"]);
			replacementsDictionary.Add("$logPath$", CoreProjectWithSDKWizard.GlobalDictionary["$logPath$"]);
			replacementsDictionary.Add("$portNumber$", CoreProjectWithSDKWizard.GlobalDictionary["$portNumber$"]);
			replacementsDictionary.Add("$companymoniker$", CoreProjectWithSDKWizard.GlobalDictionary["$companymoniker$"]);
			replacementsDictionary.Add("$security$", CoreProjectWithSDKWizard.GlobalDictionary["$security$"]);

			Proceed = true;
		}

		// This method is only called for item templates,
		// not for project templates.
		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
