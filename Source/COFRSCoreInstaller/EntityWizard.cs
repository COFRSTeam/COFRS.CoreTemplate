using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class EntityWizard : IWizard
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

				var form = new UserInputEntity();
				form.SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					Proceed = true;
					var connectionString = form.ConnectionString;
					ReplaceConnectionString(connectionString, replacementsDictionary);
					var className = replacementsDictionary["$safeitemname$"];
					replacementsDictionary["$entityClass$"] = className;

					var emitter = new Emitter();
					var entityModel = emitter.EmitEntityModel(form.DatabaseTable, replacementsDictionary["$safeitemname$"], form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$model$", entityModel);

				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}




		private void ReplaceConnectionString(string connectionString, Dictionary<string, string> replacementsDictionary)
		{
			//	The first thing we need to do, is we need to load the appSettings.local.json file
			var fileName = GetLocalFileName(replacementsDictionary["$solutiondirectory$"]);
			string content;

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				using (var reader = new StreamReader(stream))
				{
					content = reader.ReadToEnd();
				}
			}

			var appSettings = JObject.Parse(content);
			var connectionStrings = appSettings.Value<JObject>("ConnectionStrings");

			if (string.Equals(connectionStrings.Value<string>("DefaultConnection"), "Server=developmentdb;Database=master;Trusted_Connection=True;", StringComparison.OrdinalIgnoreCase))
			{
				connectionStrings["DefaultConnection"] = connectionString;

				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				{
					using (var writer = new StreamWriter(stream))
					{
						writer.Write(appSettings.ToString());
						writer.Flush();
					}
				}
			}
		}

		private string GetLocalFileName(string rootFolder)
		{
			var files = Directory.GetFiles(rootFolder);

			foreach (var file in files)
			{
				if (file.ToLower().Contains("appsettings.local.json"))
					return file;
			}

			var childFolders = Directory.GetDirectories(rootFolder);

			foreach (var childFolder in childFolders)
			{
				var theFile = GetLocalFileName(childFolder);

				if (!string.IsNullOrWhiteSpace(theFile))
					return theFile;
			}


			return string.Empty;
		}
	}
}
