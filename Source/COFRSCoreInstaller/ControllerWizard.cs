using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ControllerWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator;
		private ResourceClassFile ExampleClass;
		private ResourceClassFile ValidatorClass;
		private ResourceClassFile CollectionExampleClass;

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
			try
			{
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];

				var namespaceParts = rootNamespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				var filePath = solutionDirectory;

				for (int i = 0; i < namespaceParts.Length; i++)
				{
					if (i == 0)
					{
						var candidate = Path.Combine(filePath, namespaceParts[i]);

						if (Directory.Exists(candidate))
							filePath = candidate;
					}
					else
						filePath = Path.Combine(filePath, namespaceParts[i]);
				}

				if (!Directory.Exists(filePath))
					Directory.CreateDirectory(filePath);

				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputGeneral
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 5
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					bool hasValidator = false;
					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var moniker = LoadMoniker(SolutionFolder);

					Orchestrator = null;
					ExampleClass = null;
					CollectionExampleClass = null;
					ValidatorClass = null;

					Utilities.LoadClassList(SolutionFolder, resourceClassFile.ClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
					var policy = LoadPolicy(SolutionFolder);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNamespace);
					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);

					if (ValidatorClass != null)
					{
						hasValidator = true;
						replacementsDictionary.Add("$validationnamespace$", ValidatorClass.ClassNamespace);
					}
					else
					{
						hasValidator = false;
						replacementsDictionary.Add("$validationnamespace$", "none");
					}

					if ( ExampleClass != null )
						replacementsDictionary.Add("$singleexamplenamespace$", ExampleClass.ClassNamespace);
					else
						replacementsDictionary.Add("$singleexamplenamespace$", "none");

					var columns = Utilities.LoadClassColumns(resourceClassFile.FileName, entityClassFile.FileName, form.DatabaseColumns);

					var emitter = new Emitter();
					var model = emitter.EmitController(columns,
													   hasValidator,
													   moniker,
													   resourceClassFile.ClassName,
													   replacementsDictionary["$safeitemname$"],
													   hasValidator ? ValidatorClass.ClassName : null,
													   ExampleClass != null ? ExampleClass.ClassName : null,
													   CollectionExampleClass != null ? CollectionExampleClass.ClassName : null,
													   policy);
					
					replacementsDictionary.Add("$model$", model);
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

		private string LoadPolicy(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["policy"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string policy = LoadPolicy(subfolder);

					if (!string.IsNullOrWhiteSpace(policy))
						return policy;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}

		private string LoadMoniker(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["moniker"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string moniker = LoadMoniker(subfolder);

					if (!string.IsNullOrWhiteSpace(moniker))
						return moniker;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}
	}
}
