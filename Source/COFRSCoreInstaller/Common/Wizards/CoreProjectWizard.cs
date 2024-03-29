﻿using COFRS.Template.Common.Models;
using COFRS.Template.Common.Windows;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace COFRS.Template
{
    public class CoreProjectWizard : IWizard
	{
		private bool Proceed;
		private COFRSNewProjectDialog inputForm;
		private string framework;
		private string securityModel;
		private string databaseTechnology;
		private string moniker;
		private string logPath;
		private string projectMapPath;

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
			string solutionDirectory = replacementsDictionary["$solutiondirectory$"];
			string destinationDirectory = replacementsDictionary["$destinationdirectory$"];

			try
			{
				Proceed = true;
				Random randomNumberGenerator = new Random(Convert.ToInt32(0x0000ffffL & DateTime.Now.ToFileTimeUtc()));
				// Display a form to the user. The form collects
				// input for the custom message.
				using (inputForm = new COFRSNewProjectDialog())
				{

					var result = inputForm.ShowModal();

					if (result.HasValue && result.Value == true)
					{
						framework = inputForm.Framework;
						securityModel = inputForm.SecurityModel;
						databaseTechnology = inputForm.DatabaseTechnology;
						moniker = inputForm.CompanyMoniker;

						logPath = Path.Combine(replacementsDictionary["$destinationdirectory$"], "App_Data\\log-{Date}.json").Replace("\\", "\\\\");

						if (string.IsNullOrWhiteSpace(replacementsDictionary["$specifiedsolutionname$"]))
							projectMapPath = Path.Combine(replacementsDictionary["$destinationdirectory$"], ".cofrs");
						else
							projectMapPath = Path.Combine(replacementsDictionary["$solutiondirectory$"], ".cofrs");

						if (!Directory.Exists(projectMapPath))
						{
							DirectoryInfo dir = Directory.CreateDirectory(projectMapPath);
							dir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
						}

						var projectMapping = new ProjectMapping
						{
							EntityProject = replacementsDictionary["$safeprojectname$"],
							EntityFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Models\\EntityModels"),
							EntityNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Models.EntityModels",

							ResourceProject = replacementsDictionary["$safeprojectname$"],
							ResourceFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Models\\ResourceModels"),
							ResourceNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Models.ResourceModels",

							MappingProject = replacementsDictionary["$safeprojectname$"],
							MappingFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Mapping"),
							MappingNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Mapping",

							ExampleProject = replacementsDictionary["$safeprojectname$"],
							ExampleFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Examples"),
							ExampleNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Examples",

							ControllersProject = replacementsDictionary["$safeprojectname$"],
							ControllersFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Controllers"),
							ControllersNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Controllers",

							IncludeSDK = false
						};

						var jsonData = JsonSerializer.Serialize<ProjectMapping>(projectMapping, new JsonSerializerOptions()
						{
							PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
							WriteIndented = true,
							DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
						});

						var projectMappingPath = Path.Combine(projectMapPath, "ProjectMap.json");

						using (var stream = new FileStream(projectMappingPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
						{
							using (var writer = new StreamWriter(stream))
							{
								writer.Write(jsonData);
								writer.Flush();
							}
						}

						var portNumber = randomNumberGenerator.Next(1024, 65534);

						// Add custom parameters.
						replacementsDictionary.Add("$framework$", framework);
						replacementsDictionary.Add("$securitymodel$", securityModel);
						replacementsDictionary.Add("$databaseTechnology$", databaseTechnology);
						replacementsDictionary.Add("$logPath$", logPath);
						replacementsDictionary.Add("$portNumber$", portNumber.ToString());
						replacementsDictionary.Add("$companymoniker$", moniker);

						if (string.Equals(securityModel, "OAuth", StringComparison.OrdinalIgnoreCase))
						{
							replacementsDictionary.Add("$security$", "OAuth31");
						}
						else
							replacementsDictionary.Add("$security$", "none");
					}
					else
					{
						Proceed = false;
						throw new Exception("User canceled the operation. Aborting project creation.");
					}
				}
			}
			catch (Exception)
			{
				if (Directory.Exists(destinationDirectory))
				{
					Directory.Delete(destinationDirectory, true);
				}

				if (Directory.Exists(solutionDirectory))
				{
					Directory.Delete(solutionDirectory, true);
				}

				Proceed = false;
				throw;
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
