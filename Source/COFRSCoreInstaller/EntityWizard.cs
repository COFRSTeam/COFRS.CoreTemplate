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

                var form = new UserInputEntity
                {
                    SolutionFolder = replacementsDictionary["$solutiondirectory$"],
                    RootNamespace = rootNamespace,
					replacementsDictionary = replacementsDictionary
                };

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					Proceed = true;
					var connectionString = form.ConnectionString;
					Utilities.ReplaceConnectionString(connectionString, replacementsDictionary);
					var className = replacementsDictionary["$safeitemname$"];
					replacementsDictionary["$entityClass$"] = className;

					var emitter = new Emitter();

					var etype = DBHelper.GetElementType(form.DatabaseTable.Schema, form.DatabaseTable.Table, connectionString);
					string entityModel = string.Empty;

					if (etype == ElementType.Enum)
					{
						entityModel = emitter.EmitEnum(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var entityclassFile = new EntityClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = rootNamespace
						};

						Utilities.RegisterEnum(solutionDirectory, entityclassFile);
					}
					else if (etype == ElementType.Composite)
					{
						var undefinedElements = new List<EntityClassFile>();
						entityModel = emitter.EmitComposite(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString, replacementsDictionary, form._entityClassList, undefinedElements);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var classFile = new EntityClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = rootNamespace
						};

						Utilities.RegisterComposite(solutionDirectory, classFile);
					}
					else
					{
						entityModel = emitter.EmitEntityModel(form.DatabaseTable, replacementsDictionary["$safeitemname$"], form.DatabaseColumns, replacementsDictionary, connectionString);
					}

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
	}
}
