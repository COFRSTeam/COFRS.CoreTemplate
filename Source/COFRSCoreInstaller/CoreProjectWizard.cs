using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class CoreProjectWizard : IWizard
	{
		private bool Proceed;
		private UserInputProject inputForm;
		private string framework;
		private string securityModel;
		private string databaseTechnology;
		private string logPath;

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
			try
			{
				Proceed = true;
				Random randomNumberGenerator = new Random(Convert.ToInt32(0x0000ffffL & DateTime.Now.ToFileTimeUtc()));
				// Display a form to the user. The form collects
				// input for the custom message.
				inputForm = new UserInputProject();

				if (inputForm.ShowDialog() == DialogResult.OK)
				{

					framework = inputForm.Framework;
					securityModel = inputForm.SecurityModel;
					databaseTechnology = inputForm.DatabaseTechnology;
					logPath = Path.Combine(replacementsDictionary["$destinationdirectory$"], "App_Data\\log-{Date}.json").Replace("\\", "\\\\");
					var portNumber = randomNumberGenerator.Next(1024, 65534);

					// Add custom parameters.
					replacementsDictionary.Add("$framework$", framework);
					replacementsDictionary.Add("$securitymodel$", securityModel);
					replacementsDictionary.Add("$databaseTechnology$", databaseTechnology);
					replacementsDictionary.Add("$logPath$", logPath);
					replacementsDictionary.Add("$portNumber$", portNumber.ToString());
					replacementsDictionary.Add("$companymoniker$", inputForm.companyMoniker.Text);

					if (string.Equals(securityModel, "OAuth", StringComparison.OrdinalIgnoreCase))
					{
						if ( string.Equals(framework, "netcoreapp2.1", StringComparison.OrdinalIgnoreCase))
							replacementsDictionary.Add("$security$", "OAuth21");
						else
							replacementsDictionary.Add("$security$", "OAuth31");
					}
					else
						replacementsDictionary.Add("$security$", "none");
				}
				else
				{
					Proceed = false;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
