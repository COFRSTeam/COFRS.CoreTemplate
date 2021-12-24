using COFRS.Template.Common.Models;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace COFRS.Template
{
    public class CoreProjectWithSDKWizard : IWizard
	{
		private bool Proceed;
		private string framework;
		private string securityModel;
		private string databaseTechnology;
		private string logPath;
		private string projectMapPath;

		private DTE2 _dte;
		private string _destinationDirectory;
		private string _safeProjectName;

		// Use to communicate $saferootprojectname$ to ChildWizard
		public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

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
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			if (_destinationDirectory.EndsWith(_safeProjectName + Path.DirectorySeparatorChar + _safeProjectName))
			{
				//The projects were created under a seperate folder -- lets fix it
				var projectsObjects = new List<Tuple<Project, Project>>();

				foreach (Project childProject in _dte.Solution.Projects)
				{
					if (string.IsNullOrEmpty(childProject.FileName)) //Solution Folder
					{
						projectsObjects.AddRange(from dynamic projectItem in childProject.ProjectItems select new Tuple<Project, Project>(childProject, projectItem.Object as Project));
					}
					else
					{
						projectsObjects.Add(new Tuple<Project, Project>(null, childProject));
					}
				}

				foreach (var projectObject in projectsObjects)
				{
					var projectBadPath = projectObject.Item2.FileName;
					var projectGoodPath = projectBadPath.Replace(
						_safeProjectName + Path.DirectorySeparatorChar + _safeProjectName + Path.DirectorySeparatorChar,
						_safeProjectName + Path.DirectorySeparatorChar);

					_dte.Solution.Remove(projectObject.Item2);

					var a = Path.GetFileName(Path.GetDirectoryName(projectBadPath));
					var b = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(projectBadPath)));

					if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
					{
						//	Copy the files over
						var sourceDirectory = Path.GetDirectoryName(projectBadPath);
						var destinationDirectory = Path.GetDirectoryName(projectGoodPath);

						foreach (var childDirectory in Directory.GetDirectories(sourceDirectory))
						{
							var newPath = Path.Combine(destinationDirectory, Path.GetFileName(childDirectory));
							Directory.Move(childDirectory, newPath);
						}

						foreach (var childFile in Directory.GetFiles(sourceDirectory))
						{
							var newPath = Path.Combine(destinationDirectory, Path.GetFileName(childFile));
							File.Move(childFile, newPath);
						}

						Directory.Delete(sourceDirectory);
					}
					else
					{
						//	Move the directory over
						Directory.Move(Path.GetDirectoryName(projectBadPath), Path.GetDirectoryName(projectGoodPath));
					}

					if (projectObject.Item1 != null) //Solution Folder
					{
						var solutionFolder = (SolutionFolder)projectObject.Item1.Object;
						solutionFolder.AddFromFile(projectGoodPath);
					}
					else
					{
						_dte.Solution.AddFromFile(projectGoodPath);
					}
				}
			}

			VSLangProj.VSProject serviceProject = null;
			VSLangProj.VSProject sdkProject = null;
			Project modelsProject = null;

			//	All the projects are in their proper place. Add project references
			foreach ( Project project in _dte.Solution.Projects)
            {
				if (project.Name.EndsWith(".Models"))
					modelsProject = project;
				else if (project.Name.EndsWith(".SDK"))
					sdkProject = project.Object as VSLangProj.VSProject;
				else
					serviceProject = project.Object as VSLangProj.VSProject;
            }

			serviceProject.References.AddProject(modelsProject);
			sdkProject.References.AddProject(modelsProject);
		}

		public void RunStarted(object automationObject,
			Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{ 
			try
			{
				Proceed = true;
				Random randomNumberGenerator = new Random(Convert.ToInt32(0x0000ffffL & DateTime.Now.ToFileTimeUtc()));

				_dte = automationObject as DTE2;
				_destinationDirectory = replacementsDictionary["$destinationdirectory$"];
				_safeProjectName = replacementsDictionary["$safeprojectname$"];

				// Place "$saferootprojectname$ in the global dictionary.
				// Copy from $safeprojectname$ passed in my root vstemplate
				GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];

				// Display a form to the user. The form collects
				// input for the custom message.
				var inputForm = new COFRSNewProjectDialog();
				var result = inputForm.ShowDialog();	

				if (result.HasValue && result.Value == true)
				{
					framework = inputForm.Framework;
					securityModel = inputForm.SecurityModel;
					databaseTechnology = inputForm.DatabaseTechnology;
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
						EntityProject = $"{replacementsDictionary["$safeprojectname$"]}.Models",
						EntityFolder = Path.Combine($"{replacementsDictionary["$destinationdirectory$"]}.Models", "EntityModels"),
						EntityNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Models.EntityModels",

						ResourceProject = $"{replacementsDictionary["$safeprojectname$"]}.Models",
						ResourceFolder = Path.Combine($"{replacementsDictionary["$destinationdirectory$"]}.Models", "ResourceModels"),
						ResourceNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Models.ResourceModels",

						MappingProject = replacementsDictionary["$safeprojectname$"],
						MappingFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Mapping"),
						MappingNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Mapping",

						ValidationProject = replacementsDictionary["$safeprojectname$"],
						ValidationFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Validation"),
						ValidationNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Validation",

						ExampleProject = replacementsDictionary["$safeprojectname$"],
						ExampleFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Examples"),
						ExampleNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Examples",

						ControllersProject = replacementsDictionary["$safeprojectname$"],
						ControllersFolder = Path.Combine(replacementsDictionary["$destinationdirectory$"], "Controllers"),
						ControllersNamespace = $"{replacementsDictionary["$safeprojectname$"]}.Controllers",

						IncludeSDK = true
					};

					var jsonData = JsonSerializer.Serialize<ProjectMapping>(projectMapping, new JsonSerializerOptions()
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
						DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
						WriteIndented = true
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
					GlobalDictionary.Add("$framework$", framework);
					GlobalDictionary.Add("$securitymodel$", securityModel);
					GlobalDictionary.Add("$databaseTechnology$", databaseTechnology);
					GlobalDictionary.Add("$logPath$", logPath);
					GlobalDictionary.Add("$portNumber$", portNumber.ToString());
					GlobalDictionary.Add("$companymoniker$", inputForm.CompanyMoniker);

					if (string.Equals(securityModel, "OAuth", StringComparison.OrdinalIgnoreCase))
					{
						GlobalDictionary.Add("$security$", "OAuth31");
					}
					else
						GlobalDictionary.Add("$security$", "none");
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
