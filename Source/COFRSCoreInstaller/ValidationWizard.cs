using EnvDTE;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ValidationWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator { get; set; }
		private ResourceClassFile ExampleClass { get; set; }
		private ResourceClassFile AddExampleClass { get; set; }
		private ResourceClassFile CollectionExampleClass { get; set; }

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
					AddExampleClass = null;
					CollectionExampleClass = null;

					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var profileClassFile = (ProfileClassFile)form._profileModelList.SelectedItem;

					LoadClassList(resourceClassFile.ClassName);

					var model = EmitModel(entityClassFile, resourceClassFile, profileClassFile, form.DatabaseColumns, replacementsDictionary);

					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$domainnamespace$", resourceClassFile.ClassNamespace);

					Proceed = UpdateServices(resourceClassFile, replacementsDictionary);
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

		private string EmitModel(EntityClassFile entityClassFile, ResourceClassFile resourceClassFile, ProfileClassFile profileClassFile, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			var results = new StringBuilder();

			//	IValidator interface
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\tInterface for the {resourceClassFile.ClassName} Validator");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic interface I{resourceClassFile.ClassName}Validator : IValidator<{resourceClassFile.ClassName}>");
			results.AppendLine("\t{");
			results.AppendLine("\t}");
			results.AppendLine();

			//	Validator Class with constructor
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]} : Validator<{resourceClassFile.ClassName}>, I{resourceClassFile.ClassName}Validator");
			results.AppendLine("\t{");
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}() : base()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//	Validator Class with constructor with user
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}(ClaimsPrincipal user) : base(user)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for GET
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for Queries");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the query</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForGetAsync(RqlNode node)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tUn-comment out the line below if this table is large, and you want to prevent users from requesting a full table scan");
			results.AppendLine("\t\t\t//\tRequireIndexedQuery(node, \"The query is too broad. Please specify a more refined query that will produce fewer records.\");");
			results.AppendLine();
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT and POST
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidations common to adding and updating items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being added or updated</param>");
			results.AppendLine($"\t\tpublic async Task ValidateForAddAndUpdateAsync({resourceClassFile.ClassName} item)");
			results.AppendLine("\t\t{");

			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to");
			results.AppendLine("\t\t\t//\t       adding or updating an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidation for updating existing items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being updated</param>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the update</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForUpdateAsync({resourceClassFile.ClassName} item, RqlNode node)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item).ConfigureAwait(false);");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: add any specific validations pertaining to updating an item.");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for POST
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for adding new items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being added</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForAddAsync({resourceClassFile.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item).ConfigureAwait(false);");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: add any specific validations pertaining to adding an item.");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PATCH
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidates a set of patch commands on an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"patchCommands\">The set of patch commands to validate</param>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the update</param>");
			results.AppendLine("\t\tpublic override async Task ValidateForPatchAsync(IEnumerable<PatchCommand> patchCommands, RqlNode node)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tforeach (var command in patchCommands)");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tif (string.Equals(command.Op, \"replace\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"add\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"delete\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to patching an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for DELETE
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for deleting an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the delete</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForDeleteAsync(RqlNode node)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to deleting an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
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
											if (line.ToLower().Contains(replacementsDictionary["$domainnamespace$"].ToLower()))
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
													writer.WriteLine($"using {replacementsDictionary["$domainnamespace$"]};");
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
											if (line.ToLower().Contains(($"services.AddTransientWithParameters<I{domainClassFile.ClassName}Validator, {replacementsDictionary["$safeitemname$"]}>()").ToLower()))
												validatorRegistered = true;

											state += line.CountOf('{') - line.CountOf('}');

											if (line.Contains("return ApiOptions;"))
												state--;

											if (state == 3)
											{
												if (!validatorRegistered)
												{
													writer.WriteLine($"\t\t\tservices.AddTransientWithParameters<I{domainClassFile.ClassName}Validator, {replacementsDictionary["$safeitemname$"]}>();");
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

		private void LoadClassList(string DomainClassName)
		{
			try
			{
				foreach (var file in Directory.GetFiles(SolutionFolder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName);
				}

				foreach (var folder in Directory.GetDirectories(SolutionFolder))
				{
					LoadDomainList(folder, DomainClassName);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadDomainClass(string file, string domainClassName)
		{
			try
			{
				var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
				var className = string.Empty;
				var namespaceName = string.Empty;

				foreach (var line in data)
				{
					var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

					if (match.Success)
						className = match.Groups["className"].Value;

					match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*)");

					if (match.Success)
						namespaceName = match.Groups["namespaceName"].Value;

					if (!string.IsNullOrWhiteSpace(className) &&
						 !string.IsNullOrWhiteSpace(namespaceName))
					{
						var classfile = new ResourceClassFile
						{
							ClassName = $"{className}",
							FileName = file,
							EntityClass = string.Empty,
							ClassNamespace = namespaceName
						};

						if (string.Equals(classfile.ClassName, "ServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
							Orchestrator = classfile;

						if (string.Equals(classfile.ClassName, $"{domainClassName}Example", StringComparison.OrdinalIgnoreCase))
							ExampleClass = classfile;

						if (string.Equals(classfile.ClassName, $"Add{domainClassName}Example", StringComparison.OrdinalIgnoreCase))
							AddExampleClass = classfile;

						if (string.Equals(classfile.ClassName, $"{domainClassName}CollectionExample", StringComparison.OrdinalIgnoreCase))
							CollectionExampleClass = classfile;
					}

					className = string.Empty;
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadDomainList(string folder, string DomainClassName)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadDomainList(subfolder, DomainClassName);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
