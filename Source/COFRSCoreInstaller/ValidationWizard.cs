using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

				var form = new UserInputGeneral()
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

					LoadClassList(resourceClassFile.ClassName);

					var model = EmitModel(entityClassFile, resourceClassFile, form.DatabaseColumns, replacementsDictionary);
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
		private string EmitModel(EntityClassFile entityClassFile, ResourceClassFile resourceClassFile, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			var results = new StringBuilder();
			var classMembers = Utilities.LoadClassColumns(resourceClassFile.FileName, entityClassFile.FileName, columns);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\tInterface for the {resourceClassFile.ClassName} Validator");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic interface I{resourceClassFile.ClassName}Validator : IValidator<{resourceClassFile.ClassName}>");
			results.AppendLine("\t{");
			results.AppendLine("\t}");
			results.AppendLine();
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
			results.AppendLine("\t\t///\t<param name=\"keys\">The list of keys specifically included in the query</param>");
			results.AppendLine("\t\t///\t<param name=\"queryString\">The RQL query string used to get the collection</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForGetAsync(List<KeyValuePair<string,object>> keys, string queryString)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tValidateQueryString(queryString);");
			results.AppendLine("\t\t\tRequireIndexedQuery(keys, queryString, \"The query is too broad. Please specify a more refined query that will produce fewer records.\");");
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

			foreach (var member in classMembers)
			{
				EmitUpdateValidation(results, member, 0, null);
			}

			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to adding or updating an item.");
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
			results.AppendLine($"\t\tpublic override async Task ValidateForUpdateAsync({resourceClassFile.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item).ConfigureAwait(false);");
			results.AppendLine();
			results.AppendLine("\t\t\tRequire(item.href != null, \"The href must not be null.\");");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tEnsure that the item exists before we try to update it.");
			results.AppendLine("\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\tif (await service.GetSingleAsync<{resourceClassFile.ClassName}>(item.href, \"select(href)\").ConfigureAwait(false) == null)");
			results.AppendLine("\t\t\t\t\tFailNotFound();");
			results.AppendLine("\t\t\t}");
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
			results.AppendLine("\t\t///\t<param name=\"keys\">The set of keys that uniquely identifies the object</param>");
			results.AppendLine("\t\t///\t<param name=\"patchCommands\">The set of patch commands to validate</param>");
			results.AppendLine("\t\tpublic override async Task ValidateForPatchAsync(List<KeyValuePair<string, object>> keys, IEnumerable<PatchCommand> patchCommands)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tforeach (var command in patchCommands)");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tif (string.Equals(command.op, \"replace\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			foreach (var member in classMembers)
			{
				if (member.EntityNames.Count == 1)
				{
					var entity = member.EntityNames[0];

					if (!entity.IsComputed)
					{
						if (entity.IsFixed)
						{
							if (entity.Length > 0)
							{
								if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Binary) ||
									(entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Timestamp) ||
									(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Binary))
								{
									if (entity.IsNullable)
									{
										results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
										results.AppendLine($"\t\t\t\t\t\tif (!string.IsNullOrWhiteSpace(command.value))");
										results.AppendLine($"\t\t\t\t\t\t\tRequire(Convert.FromBase64String(command.value).Length != {entity.Length}, \"{member.DomainName} must be {entity.Length} bytes in length.\");");
									}
									else
									{
										results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
										results.AppendLine($"\t\t\t\t\t\tRequire(Convert.FromBase64String(command.value).Length != {entity.Length}, \"{member.DomainName} must be {entity.Length} bytes in length.\");");
									}
								}

								else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Char) ||
										  (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NChar) ||
										  (entity.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)entity.DataType == NpgsqlDbType.Char))
								{
									if (entity.IsNullable)
									{
										results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
										results.AppendLine($"\t\t\t\t\t\tif (!string.IsNullOrWhiteSpace(command.value))");
										results.AppendLine($"\t\t\t\t\t\t\tRequire(command.value.Length != {entity.Length}, \"{member.DomainName} must be {entity.Length} characters in length.\");");
									}
									else
									{
										results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
										results.AppendLine($"\t\t\t\t\t\tRequire(command.value.Length != {entity.Length}, \"{member.DomainName} must be {entity.Length} characters in length.\");");
									}
								}
							}
						}
						else if (entity.Length > 0)
						{
							if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.VarBinary) ||
								(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarBinary))
							{
								if (entity.IsNullable)
								{
									results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
									results.AppendLine($"\t\t\t\t\t\tif (!string.IsNullOrWhiteSpace(command.value))");
									results.AppendLine($"\t\t\t\t\t\t\tRequire(Convert.FromBase64String(command.value).Length <= {entity.Length}, \"{member.DomainName} must be less than or equal to {entity.Length} bytes in length.\");");
								}
								else
								{
									results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
									results.AppendLine($"\t\t\t\t\t\tRequire(Convert.FromBase64String(command.value).Length <= {entity.Length}, \"{member.DomainName} must be less than or equal to {entity.Length} bytes in length.\");");
								}
							}

							else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.VarChar) ||
									 (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NVarChar) ||
									 (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarChar))
							{
								if (entity.IsNullable)
								{
									results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
									results.AppendLine($"\t\t\t\t\t\tif (!string.IsNullOrWhiteSpace(command.value))");
									results.AppendLine($"\t\t\t\t\t\t\tRequire(command.value.Length <= {entity.Length}, \"{member.DomainName} must be less than or equal to {entity.Length} characters in length.\");");
								}
								else
								{
									results.AppendLine($"\t\t\t\t\tif (string.Equals(command.path, \"{member.DomainName}\", StringComparison.OrdinalIgnoreCase))");
									results.AppendLine($"\t\t\t\t\t\tRequire(command.value.Length <= {entity.Length}, \"{member.DomainName} must be less than or equal to {entity.Length} characters in length.\");");
								}
							}
						}
					}
				}
			}

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
			results.AppendLine("\t\t///\t<param name=\"keys\">The list of keys used to identify the item(s) being deleted</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForDeleteAsync(List<KeyValuePair<string,object>> keys)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to deleting an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private void EmitUpdateValidation(StringBuilder results, ClassMember member, int indent, string prefix)
		{
			var parent = string.IsNullOrWhiteSpace(prefix) ? string.Empty : $"{prefix}.";

			if (!string.IsNullOrWhiteSpace(member.DomainName) &&
				 (member.ChildMembers.Count > 0 || member.EntityNames.Count > 0))
			{
				if (member.ChildMembers.Count > 0)
				{
					var newIndent = indent;

					if (IsNullable(member))
					{
						for (int i = 0; i < indent; i++) results.Append("\t");
						results.AppendLine($"\t\t\tif (item.{parent}{member.DomainName} != null )");
						for (int i = 0; i < indent; i++) results.Append("\t");
						results.AppendLine("\t\t\t{");
						newIndent = indent + 1;
					}

					foreach (var childMember in member.ChildMembers)
					{
						EmitUpdateValidation(results, childMember, newIndent, $"{parent}{member.DomainName}");
					}

					if (IsNullable(member))
					{
						for (int i = 0; i < indent; i++) results.Append("\t");
						results.AppendLine("\t\t\t}");
					}
				}
				else
				{
					var entity = member.EntityNames[0];

					if (!entity.IsComputed)
					{
						if (entity.IsFixed)
						{
							if (entity.Length > 1)
							{
								if (entity.IsNullable)
								{
									if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Binary) ||
										(entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Timestamp) ||
										(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Binary))
									{
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\tif(item.{parent}{member.DomainName} != null )");
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} bytes in length.\");");
									}

									else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Char) ||
											  (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NChar) ||
											  (entity.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)entity.DataType == NpgsqlDbType.Char))
									{
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
									}
									else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.String)
									{
										if (entity.Length > 1)
										{
											for (int i = 0; i < indent; i++) results.Append("\t");
											results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{member.DomainName}))");
											for (int i = 0; i < indent; i++) results.Append("\t");
											results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
										}
									}
								}
								else
								{
									if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Binary) ||
										(entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Timestamp) ||
										(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Binary))
									{
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} bytes in length.\");");
									}
									else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Char) ||
											  (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NChar) ||
											  (entity.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)entity.DataType == NpgsqlDbType.Char))
									{
										for (int i = 0; i < indent; i++) results.Append("\t");
										results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
									}
									else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.String)
									{
										if (entity.Length > 1)
										{
											for (int i = 0; i < indent; i++) results.Append("\t");
											results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
										}
									}
								}
							}
						}
						else if (entity.Length > 1)
						{
							if (entity.IsNullable)
							{
								if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.VarBinary) ||
									(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarBinary))
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(item.{parent}{member.DomainName} != null )");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= {entity.Length}, \"{parent}{member.DomainName} must be less than or equal to {entity.Length} bytes in length.\");");
								}

								else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.VarChar) ||
										 (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NVarChar) ||
										 (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarChar))
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= {entity.Length}, \"{parent}{member.DomainName} must be less than or equal to {entity.Length} characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarChar)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.TinyText)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 255, \"{parent}{member.DomainName} must be less than or equal to 255 characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Text)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 65535, \"{parent}{member.DomainName} must be less than or equal to 65,535 characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.MediumText)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tif(!string.IsNullOrWhiteSpace(item.{parent}{member.DomainName}))");
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 16777215, \"{parent}{member.DomainName} must be less than or equal to 16,777,215 characters in length.\");");
								}
							}
							else
							{
								if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Binary) ||
									(entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Timestamp) ||
									(entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Binary))
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length <= {entity.Length}, \"{parent}{member.DomainName} must be less than or equal to {entity.Length} bytes in length.\");");
								}
								else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.VarChar) ||
										 (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NVarChar) ||
										 (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarChar))
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\t\tRequire(item.{parent}{member.DomainName}.Length <= {entity.Length}, \"{parent}{member.DomainName} must be less than or equal to {entity.Length} characters in length.\");");
								}
								else if ((entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.Char) ||
										 (entity.ServerType == DBServerType.SQLSERVER && (SqlDbType)entity.DataType == SqlDbType.NChar) ||
										 (entity.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)entity.DataType == NpgsqlDbType.Char))
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length <= {entity.Length}, \"{parent}{member.DomainName} must be less than or equal to {entity.Length} characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.VarChar)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length == {entity.Length}, \"{parent}{member.DomainName} must be {entity.Length} characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.TinyText)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 255, \"{parent}{member.DomainName} must be less than or equal to 255 characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.Text)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 65535, \"{parent}{member.DomainName} must be less than or equal to 65,535 characters in length.\");");
								}
								else if (entity.ServerType == DBServerType.MYSQL && (MySqlDbType)entity.DataType == MySqlDbType.MediumText)
								{
									for (int i = 0; i < indent; i++) results.Append("\t");
									results.AppendLine($"\t\t\tRequire(item.{parent}{member.DomainName}.Length <= 16777215, \"{parent}{member.DomainName} must be less than or equal to 16,777,215 characters in length.\");");
								}
							}
						}
					}
				}
			}
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

											if (line.Contains("{"))
												state++;

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
											if (line.Contains("{"))
												state++;

											if (line.Contains("}"))
												state--;
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
		private bool IsNullable(ClassMember member)
		{
			bool isNullable = false;

			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					isNullable |= IsNullable(childMember);
				}
			}
			else
			{
				foreach (var entity in member.EntityNames)
				{
					isNullable |= entity.IsNullable;
				}
			}

			return isNullable;
		}
	}
}
