using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ControllerWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator;
		private ResourceClassFile ExampleClass;
		private ResourceClassFile ValidatorClass;
		private ResourceClassFile CollectionExampleClass;

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
				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputGeneral
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 5
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					bool hasValidator = false;
					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var moniker = LoadMoniker(SolutionFolder);

					Orchestrator = null;
					ExampleClass = null;
					CollectionExampleClass = null;
					ValidatorClass = null;

					Utilities.LoadClassList(SolutionFolder, resourceClassFile.ClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
					var policy = LoadPolicy(SolutionFolder);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNamespace);
					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);

					if (ValidatorClass != null)
					{
						hasValidator = true;
						replacementsDictionary.Add("$validationnamespace$", ValidatorClass.ClassNamespace);
					}
					else
					{
						hasValidator = false;
						replacementsDictionary.Add("$validationnamespace$", "none");
					}

					if ( ExampleClass != null )
						replacementsDictionary.Add("$singleexamplenamespace$", ExampleClass.ClassNamespace);
					else
						replacementsDictionary.Add("$singleexamplenamespace$", "none");

					var model = Emit(replacementsDictionary,
									 hasValidator,
									 resourceClassFile,
									 entityClassFile,
									 policy,
									 form.DatabaseColumns);
					
					replacementsDictionary.Add("$model$", model);
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

		public string Emit(Dictionary<string, string> replacementsDictionary, bool hasValidator, ResourceClassFile domain, EntityClassFile entity, string policy, List<DBColumn> Columns)
		{
			var results = new StringBuilder();
			var nn = new NameNormalizer(domain.ClassName);
			var columns = Utilities.LoadClassColumns(domain.FileName, entity.FileName, Columns);
			var pkcolumns = columns.Where(c => c.EntityNames.Count > 0 && c.EntityNames[0].IsPrimaryKey);
			var moniker = replacementsDictionary["$companymoniker$"];

			// --------------------------------------------------------------------------------
			//	Class
			// --------------------------------------------------------------------------------

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{domain.ClassName} Controller");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine("\t[ApiVersion(\"1.0\")]");
			results.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]} : COFRSController");
			results.AppendLine("\t{");
			results.AppendLine($"\t\tprivate readonly ILogger<{replacementsDictionary["$safeitemname$"]}> Logger;");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	Constructor
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes a {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}(ILogger<{replacementsDictionary["$safeitemname$"]}> logger)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger = logger;");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Collection Endpoint
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tReturns a collection of {nn.PluralForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<remarks>This call supports RQL. The call will only return up to a maximum of \"QueryLimit\" records, where the value of the query limit is predefined in the service and cannot be changed by the user.</remarks>");
			results.AppendLine($"\t\t///\t<response code=\"200\">Returns a collection of {nn.PluralForm}</response>");
			results.AppendLine("\t\t[HttpGet]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if ( !string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
			{
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(RqlCollection<{domain.ClassName}>))]");
			}
			else
			{
				results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RqlCollection<{domain.ClassName}>))]");
			}

			if (CollectionExampleClass != null)
				results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({CollectionExampleClass.ClassName}))]");

			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Get{nn.PluralForm}Async()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.Path}\");");
			results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.QueryString.Value);");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine($"\t\t\t{{");
			results.AppendLine($"\t\t\t\tvar collection = await service.GetCollectionAsync<{domain.ClassName}>(Request, node).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn Ok(collection);");
			results.AppendLine($"\t\t\t}}");
			results.AppendLine("\t\t}");

			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Single Endpoint
			// --------------------------------------------------------------------------------

			if (pkcolumns.Count() > 0)
			{
				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tReturns a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine("\t\t///\t<remarks>This call supports RQL. Use the RQL select clause to limit the members returned.</remarks>");
				results.AppendLine("\t\t[HttpGet]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({domain.ClassName}))]");
				else
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.OK, Type = typeof({domain.ClassName}))]");

				if (ExampleClass != null)
					results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({ExampleClass.ClassName}))]");

				if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				else
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NotFound)]");

				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine("\t\t[SupportRQL]");

				EmitEndpoint(domain, "Get", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine("\t\t\tvar translationOptions = HttpContext.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tvar item = await service.GetSingleAsync<{domain.ClassName}>(node).ConfigureAwait(false);");
				results.AppendLine();
				results.AppendLine("\t\t\t\tif (item == null)");
				results.AppendLine("\t\t\t\t\treturn NotFound();");
				results.AppendLine();
				results.AppendLine("\t\t\t\treturn Ok(item);");
				results.AppendLine("\t\t\t}");

				results.AppendLine("\t\t}");
				results.AppendLine();
			}

			// --------------------------------------------------------------------------------
			//	POST Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tAdds a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Add a {nn.SingleForm} to the datastore.</remarks>");
			results.AppendLine("\t\t[HttpPost]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({domain.ClassName}), typeof({ExampleClass.ClassName}))]");

			if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({domain.ClassName}))]");
			else
				results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.Created, Type = typeof({domain.ClassName}))]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.Created, typeof({ExampleClass.ClassName}))]");
			
			results.AppendLine($"\t\t[SwaggerResponseHeader((int)HttpStatusCode.Created, \"Location\", \"string\", \"Returns Href of new {domain.ClassName}\")]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Add{domain.ClassName}Async([FromBody] {domain.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForAddAsync(item).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\titem = await service.AddAsync(item).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn Created(item.Href.AbsoluteUri, item);");

			results.AppendLine("\t\t\t}");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	PUT Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
			results.AppendLine("\t\t[HttpPut]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			if (ExampleClass != null)
			{
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({domain.ClassName}), typeof({ExampleClass.ClassName}))]");
			}

			if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
			{
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
			}
			else
			{
				results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NotFound)]");
			}

			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Update{domain.ClassName}Async([FromBody] {domain.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
			results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href={{item.Href.AbsoluteUri}}\")");
			results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForUpdateAsync(item, node).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\tawait service.UpdateAsync(item, node).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn NoContent();");
			results.AppendLine("\t\t\t}");

			results.AppendLine("\t\t}");
			results.AppendLine();

			if (pkcolumns.Count() > 0)
			{
				// --------------------------------------------------------------------------------
				//	PATCH Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm} using patch commands");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpPatch]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
				{
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				}
				else
				{
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NoContent)]");
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NotFound)]");
				}

				results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				EmitEndpoint(domain, "Patch", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine("\t\t\tvar translationOptions = HttpContext.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForPatchAsync(commands, node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tawait service.PatchAsync<{domain.ClassName}>(commands, node).ConfigureAwait(false);");
				results.AppendLine($"\t\t\t\treturn NoContent();");
				results.AppendLine("\t\t\t}");
				results.AppendLine("\t\t}");
				results.AppendLine();

				// --------------------------------------------------------------------------------
				//	DELETE Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tDelete a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine($"\t\t///\t<remarks>Deletes a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpDelete]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				if (string.Equals(replacementsDictionary["$targetframeworkversion$"], "3.1", StringComparison.OrdinalIgnoreCase))
				{
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
					results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				}
				else
				{
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NoContent)]");
					results.AppendLine($"\t\t[ProducesResponseType((int)HttpStatusCode.NotFound)]");
				}

				EmitEndpoint(domain, "Delete", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine("\t\t\tvar translationOptions = HttpContext.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{ValidatorClass.ClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForDeleteAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tawait service.DeleteAsync<{domain.ClassName}>(node).ConfigureAwait(false);");
				results.AppendLine($"\t\t\t\treturn NoContent();");
				results.AppendLine("\t\t\t}");

				results.AppendLine("\t\t}");
				results.AppendLine("\t}");
			}

			return results.ToString();
		}

		private void EmitEndpoint(ResourceClassFile domain, string action, StringBuilder results, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IActionResult> {action}{domain.ClassName}Async(");
			bool first = true;

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					if (first)
						first = false;
					else
						results.Append(", ");

					string dataType = "Unrecognized";

					if (column.ServerType == DBServerType.POSTGRESQL)
						dataType = DBHelper.GetNonNullablePostgresqlDataType(column);
					else if (column.ServerType == DBServerType.MYSQL)
						dataType = DBHelper.GetNonNullableMySqlDataType(column);
					else if (column.ServerType == DBServerType.SQLSERVER)
						dataType = DBHelper.GetNonNullableSqlServerDataType(column);

					results.Append($"{dataType} {column.EntityName}");
				}
			}

			if (string.Equals(action, "patch", StringComparison.OrdinalIgnoreCase))
				results.AppendLine(", [FromBody] IEnumerable<PatchCommand> commands)");
			else
				results.AppendLine(")");
		}

		private static void EmitRoute(StringBuilder results, string routeName, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\t[Route(\"{routeName}/id");

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					results.Append($"/{{{column.EntityName}}}");
				}
			}

			results.AppendLine("\")]");
		}

		private static string BuildRoute(string routeName, IEnumerable<ClassMember> pkcolumns)
		{
			var route = new StringBuilder();

			route.Append(routeName);
			route.Append("/id");

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					route.Append($"/{{{column.EntityName}}}");
				}
			}

			return route.ToString();
		}

		private string LoadPolicy(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["policy"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string policy = LoadPolicy(subfolder);

					if (!string.IsNullOrWhiteSpace(policy))
						return policy;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}
		private string LoadMoniker(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["moniker"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string moniker = LoadMoniker(subfolder);

					if (!string.IsNullOrWhiteSpace(moniker))
						return moniker;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}
	}
}
