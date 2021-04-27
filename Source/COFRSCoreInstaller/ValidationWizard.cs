using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
    public class ValidationWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator;
		private ResourceClassFile ExampleClass;
		private ResourceClassFile CollectionExampleClass;
		private ResourceClassFile ValidatorClass;

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

				var form = new UserInputValidation()
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 3
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					Orchestrator = null;
					ExampleClass = null;
					CollectionExampleClass = null;
					ValidatorClass = null;

					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

					Utilities.LoadClassList(SolutionFolder, resourceClassFile.ClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);

					var emitter = new Emitter();
					var model = emitter.EmitValidationModel(resourceClassFile.ClassName, replacementsDictionary["$safeitemname$"]);

					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNamespace);

					Proceed = emitter.UpdateServices(solutionDirectory, replacementsDictionary["$safeitemname$"],
														entityClassFile.ClassNameSpace, resourceClassFile.ClassNamespace,
														rootNamespace);
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

		private bool UpdateServices(ResourceClassFile domainClassFile, Dictionary<string, string> replacementsDictionary)
		{
			var servicesFile = FindServices(replacementsDictionary["$solutiondirectory$"]);

			if (!string.IsNullOrWhiteSpace(servicesFile))
			{
				var serviceFolder = Path.GetDirectoryName(servicesFile);
				var tempFile = Path.Combine(serviceFolder, "Services.old.cs");

				try
				{
					File.Delete(tempFile);
					File.Move(servicesFile, tempFile);

					using (var stream = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
					{
						using (var reader = new StreamReader(stream))
						{
							using (var outStream = new FileStream(servicesFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
							{
								using (var writer = new StreamWriter(outStream))
								{
									var state = 1;
									bool hasDomainNamespace = false;
									bool hasValidationNamespace = false;
									bool hasEntityNamespace = false;
									bool validatorRegistered = false;

									while (!reader.EndOfStream)
									{
										var line = reader.ReadLine();

										if (state == 1)
										{
											if (line.ToLower().Contains(replacementsDictionary["$resourcenamespace$"].ToLower()))
											{
												hasDomainNamespace = true;
											}

											if (line.ToLower().Contains(replacementsDictionary["$rootnamespace$"].ToLower()))
											{
												hasValidationNamespace = true;
											}

											if (line.ToLower().Contains(replacementsDictionary["$entitynamespace$"].ToLower()))
											{
												hasEntityNamespace = true;
											}

											if (string.IsNullOrWhiteSpace(line))
											{
												if (!hasDomainNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$resourcenamespace$"]};");
												}

												if (!hasValidationNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$rootnamespace$"]};");
												}

												if (!hasEntityNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$entitynamespace$"]};");
												}

												state = 2;
											}

										}
										else if (state == 2)
										{
											if (line.ToLower().Contains("public static iapioptions configureservices"))
											{
												state = 3;
											}
										}
										else if (state == 3)
										{
											if (line.Contains("{"))
												state++;
										}
										else if (state == 4)
										{
											if (line.ToLower().Contains(($"services.AddTransientWithParameters<I{replacementsDictionary["$safeitemname$"]}, {replacementsDictionary["$safeitemname$"]}>()").ToLower()))
												validatorRegistered = true;

											state += line.CountOf('{') - line.CountOf('}');

											if (line.Contains("return ApiOptions;"))
												state--;

											if (state == 3)
											{
												if (!validatorRegistered)
												{
													writer.WriteLine($"\t\t\tservices.AddTransientWithParameters<I{replacementsDictionary["$safeitemname$"]}, {replacementsDictionary["$safeitemname$"]}>();");
												}
												state = 1000000;
											}
										}
										else
										{
											state += line.CountOf('{') - line.CountOf('}');
										}

										writer.WriteLine(line);
									}
								}
							}
						}
					}

					File.Delete(tempFile);
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					File.Delete(servicesFile);
					File.Move(tempFile, servicesFile);
					return false;
				}
			}

			return true;
		}

		private string FindServices(string folder)
		{
			string filePath = Path.Combine(folder, "ServiceConfig.cs");

			if (File.Exists(filePath))
				return filePath;

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				filePath = FindServices(childFolder);

				if (!string.IsNullOrWhiteSpace(filePath))
					return filePath;
			}

			return string.Empty;
		}

	}
}
