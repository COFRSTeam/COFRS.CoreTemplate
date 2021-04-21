using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ExamplesWizard : IWizard
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

				var form = new UserInputGeneral()
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 4
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					string connectionString = form.ConnectionString;
					var classFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var domainFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

					var emitter = new Emitter();

					var model = emitter.EmitExampleModel(replacementsDictionary["$targetframeworkversion$"],
						classFile.SchemaName,
						connectionString,
						Utilities.LoadClassColumns(domainFile.FileName, classFile.FileName, form.DatabaseColumns),
						classFile.ClassName,
						domainFile.ClassName,
						replacementsDictionary["$safeitemname$"],
						form.DatabaseColumns, form.Examples, replacementsDictionary);

					var collectionmodel = emitter.EmitExampleCollectionModel(replacementsDictionary["$targetframeworkversion$"],
						classFile.SchemaName,
						connectionString,
						Utilities.LoadClassColumns(domainFile.FileName, classFile.FileName, form.DatabaseColumns),
						classFile.ClassName,
						domainFile.ClassName,
						"Collection" + replacementsDictionary["$safeitemname$"],
						form.DatabaseColumns, form.Examples, replacementsDictionary);

					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$collectionmodel$", collectionmodel);
					replacementsDictionary.Add("$entitynamespace$", classFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", domainFile.ClassNamespace);

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
