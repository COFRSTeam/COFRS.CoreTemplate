using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
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
				InstallType = 1
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
				var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

				var classMembers = Utilities.LoadClassColumns(resourceClassFile.FileName, entityClassFile.FileName, form.DatabaseColumns);

				var emitter = new Emitter();
				var model = emitter.EmitMappingModel(classMembers, resourceClassFile.ClassName, entityClassFile.ClassName,
													 replacementsDictionary["$safeitemname$"],
													 form.DatabaseColumns, replacementsDictionary);

				replacementsDictionary.Add("$model$", model);
				replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
				replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNamespace);
				Proceed = true;
			}
			else
				Proceed = false;
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
