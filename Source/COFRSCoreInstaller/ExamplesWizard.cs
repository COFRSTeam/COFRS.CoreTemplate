﻿using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
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

namespace COFRS.Template
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
					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

					var emitter = new Emitter();

					var model = emitter.EmitExampleModel(
						entityClassFile,
						resourceClassFile,
						replacementsDictionary["$safeitemname$"],
						form.Examples, 
						replacementsDictionary,
						form.ClassList);

					var collectionmodel = emitter.EmitExampleCollectionModel(
						entityClassFile,
						resourceClassFile,
						replacementsDictionary["$safeitemname$"],
						form.Examples,
						replacementsDictionary,
						form.ClassList);

					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$collectionmodel$", collectionmodel);
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNameSpace);

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
