using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class Emitter
	{
		public string EmitController(List<ClassMember> columns, bool hasValidator, string moniker, string resourceClassName, string controllerClassName, string validationClassName, string exampleClassName, string exampleCollectionClassName, string policy)
		{
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceClassName);
			var pkcolumns = columns.Where(c => c.EntityNames.Count > 0 && c.EntityNames[0].IsPrimaryKey);

			// --------------------------------------------------------------------------------
			//	Class
			// --------------------------------------------------------------------------------

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName} Controller");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine("\t[ApiVersion(\"1.0\")]");
			results.AppendLine($"\tpublic class {controllerClassName} : COFRSController");
			results.AppendLine("\t{");
			results.AppendLine($"\t\tprivate readonly ILogger<{controllerClassName}> Logger;");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	Constructor
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes a {controllerClassName}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {controllerClassName}(ILogger<{controllerClassName}> logger)");
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

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(RqlCollection<{resourceClassName}>))]");

			if (!string.IsNullOrWhiteSpace(exampleCollectionClassName))
				results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({exampleCollectionClassName}))]");

			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Get{nn.PluralForm}Async()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.Path}\");");
			results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.QueryString.Value);");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine($"\t\t\t{{");
			results.AppendLine($"\t\t\t\tvar collection = await service.GetCollectionAsync<{resourceClassName}>(Request.QueryString.Value, node).ConfigureAwait(false);");
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

				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClassName}))]");

				if (!string.IsNullOrWhiteSpace(exampleClassName))
					results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({exampleClassName}))]");

				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine("\t\t[SupportRQL]");

				EmitEndpoint(resourceClassName, "Get", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tvar item = await service.GetSingleAsync<{resourceClassName}>(node).ConfigureAwait(false);");
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

			if (!string.IsNullOrWhiteSpace(exampleClassName))
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClassName}), typeof({exampleClassName}))]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({resourceClassName}))]");

			if (!string.IsNullOrWhiteSpace(exampleClassName))
				results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.Created, typeof({exampleClassName}))]");

			results.AppendLine($"\t\t[SwaggerResponseHeader((int)HttpStatusCode.Created, \"Location\", \"string\", \"Returns Href of new {resourceClassName}\")]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Add{resourceClassName}Async([FromBody] {resourceClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
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

			if (!string.IsNullOrWhiteSpace(exampleClassName))
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClassName}), typeof({exampleClassName}))]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Update{resourceClassName}Async([FromBody] {resourceClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
			results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:{{item.Href}}\")");
			results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
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

				results.AppendLine($"\t\t[SwaggerRequestExample(typeof(IEnumerable<PatchCommand>), typeof(PatchExample))]");
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				EmitEndpoint(resourceClassName, "Patch", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForPatchAsync(commands, node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tawait service.PatchAsync<{resourceClassName}>(commands, node).ConfigureAwait(false);");
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

				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");

				EmitEndpoint(resourceClassName, "Delete", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = HttpContext.RequestServices.Get<I{validationClassName}>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForDeleteAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = HttpContext.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tawait service.DeleteAsync<{resourceClassName}>(node).ConfigureAwait(false);");
				results.AppendLine($"\t\t\t\treturn NoContent();");
				results.AppendLine("\t\t\t}");

				results.AppendLine("\t\t}");
				results.AppendLine("\t}");
			}

			return results.ToString();
		}

		public string EmitValidationModel(string resourceClassName, string validatorClassName)
		{
			var results = new StringBuilder();

			//	IValidator interface
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\tInterface for the {resourceClassName} Validator");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic interface I{validatorClassName} : IValidator<{resourceClassName}>");
			results.AppendLine("\t{");
			results.AppendLine("\t}");
			results.AppendLine();

			//	Validator Class with constructor
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{validatorClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {validatorClassName} : Validator<{resourceClassName}>, I{validatorClassName}");
			results.AppendLine("\t{");
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {validatorClassName}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {validatorClassName}() : base()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//	Validator Class with constructor with user
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {validatorClassName}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {validatorClassName}(ClaimsPrincipal user) : base(user)");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForGetAsync(RqlNode node, object[] parms)");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic async Task ValidateForAddAndUpdateAsync({resourceClassName} item, object[] parms)");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForUpdateAsync({resourceClassName} item, RqlNode node, object[] parms)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, parms).ConfigureAwait(false);");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForAddAsync({resourceClassName} item, object[] parms)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, parms).ConfigureAwait(false);");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine("\t\tpublic override async Task ValidateForPatchAsync(IEnumerable<PatchCommand> patchCommands, RqlNode node, object[] parms)");
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
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForDeleteAsync(RqlNode node, object[] parms)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to deleting an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitExampleModel(string schema, string connectionString, List<ClassMember> classMembers, string entityClassName, string resourceClassName, string exampleClassName, List<DBColumn> Columns, JObject Example, Dictionary<string, string> replacementsDictionary, List<EntityDetailClassFile> classFiles)
		{
			var results = new StringBuilder();
			replacementsDictionary.Add("$exampleimage$", "false");
			replacementsDictionary.Add("$examplenet$", "false");
			replacementsDictionary.Add("$examplenetinfo$", "false");
			replacementsDictionary.Add("$examplebarray$", "false");

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName} Example");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {exampleClassName} : IExamplesProvider<{resourceClassName}>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tGet Example");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<returns>An example of {resourceClassName}</returns>");
			results.AppendLine($"\t\tpublic {resourceClassName} GetExamples()");
			results.AppendLine("\t\t{");
			results.AppendLine($"\t\t\tvar item = new {entityClassName}");
			results.AppendLine("\t\t\t{");
			var first = true;

			var parentClass = classFiles.FirstOrDefault(c => string.Equals(c.ClassName, entityClassName, StringComparison.OrdinalIgnoreCase));

			foreach (var member in classMembers)
			{
				foreach (var column in member.EntityNames)
				{
					if (first)
						first = false;
					else
						results.AppendLine(",");

					//	Set Flags to include necessary usings...

					if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
						replacementsDictionary["$exampleimage$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
						replacementsDictionary["$examplenet$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
						replacementsDictionary["$examplenet$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
						replacementsDictionary["$examplenetinfo$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
						replacementsDictionary["$examplenetinfo$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
						replacementsDictionary["$examplebarray$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
						replacementsDictionary["$examplebarray$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
						replacementsDictionary["$examplebarray$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Point)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Line)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Box)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Circle)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Polygon)
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Point))
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.LSeg))
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Line))
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Box))
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Circle))
						replacementsDictionary["$usenpgtypes$"] = "true";
					else if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Polygon))
						replacementsDictionary["$usenpgtypes$"] = "true";

					//	Get the member value 

					if (column.ServerType == DBServerType.MYSQL)
						GetMySqlValue(column, Example, results);
					else if (column.ServerType == DBServerType.POSTGRESQL)
						EmitPostgresValue(column, parentClass, Example, results, classFiles, 0);
					else if (column.ServerType == DBServerType.SQLSERVER)
						GetSqlServerValue(column, Example, results);

					//	Write the member set function

				}
			}

			results.AppendLine();
			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine($"\t\t\treturn AutoMapperFactory.Map<{entityClassName}, {resourceClassName}>(item);");

			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitExampleCollectionModel(string schema, string connectionString, List<ClassMember> classMembers, string entityClassName, string resourceClassName, string exampleCollectionClassName, List<DBColumn> Columns, JObject Example, Dictionary<string, string> replacementsDictionary, List<EntityDetailClassFile> classFiles)
		{
			var results = new StringBuilder();

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName} Collection Example");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {exampleCollectionClassName} : IExamplesProvider<RqlCollection<{resourceClassName}>>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tGet Example");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<returns>An example of {resourceClassName} collection</returns>");
			results.AppendLine($"\t\tpublic RqlCollection<{resourceClassName}> GetExamples()");
			results.AppendLine("\t\t{");
			results.AppendLine($"\t\t\tvar item = new {entityClassName}");
			results.AppendLine("\t\t\t{");
			var first = true;

			var parentClass = classFiles.FirstOrDefault(c => string.Equals(c.ClassName, entityClassName, StringComparison.OrdinalIgnoreCase));

			foreach (var member in classMembers)
			{
				first = EmitEntiyMemeberSetting(Columns, parentClass, schema, connectionString, replacementsDictionary["$solutiondirectory$"], Example, results, first, member, classFiles);
			}

			results.AppendLine();
			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine($"\t\t\tvar collection = new RqlCollection<{entityClassName}>()");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tHref = new Uri(\"https://temp.com?limit(10,10)\"),");
			results.AppendLine("\t\t\t\tNext = new Uri(\"https://temp.com?limit(20,10)\"),");
			results.AppendLine("\t\t\t\tFirst = new Uri(\"https://temp.com?limit(1,10)\"),");
			results.AppendLine("\t\t\t\tPrevious = new Uri(\"https://temp.com?limit(1,10)\"),");
			results.AppendLine("\t\t\t\tCount = 2542,");
			results.AppendLine("\t\t\t\tLimit = 10,");
			results.AppendLine($"\t\t\t\tItems = new List<{entityClassName}>() {{ item }}");
			results.AppendLine("\t\t\t};");
			results.AppendLine();
			results.AppendLine($"\t\t\treturn AutoMapperFactory.Map<RqlCollection<{entityClassName}>, RqlCollection<{resourceClassName}>>(collection);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitEnum(string schema, string dataType, string className, string connectionString)
		{
			var nn = new NameNormalizer(className);
			var builder = new StringBuilder();

			builder.Clear();
			builder.AppendLine("\t///\t<summary>");
			builder.AppendLine($"\t///\tEnumerates a list of {nn.PluralForm}");
			builder.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				builder.AppendLine($"\t[PgEnum(\"{dataType}\")]");
			else
				builder.AppendLine($"\t[PgEnum(\"{dataType}\", Schema = \"{schema}\")]");

			builder.AppendLine($"\tpublic enum {className}");
			builder.AppendLine("\t{");

			string query = @"
select e.enumlabel as enum_value
from pg_type t 
   join pg_enum e on t.oid = e.enumtypid  
   join pg_catalog.pg_namespace n ON n.oid = t.typnamespace
where t.typname = @dataType
  and n.nspname = @schema";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", dataType);
					command.Parameters.AddWithValue("@schema", schema);

					bool firstUse = true;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							if (firstUse)
								firstUse = false;
							else
							{
								builder.AppendLine(",");
								builder.AppendLine();
							}

							var element = reader.GetString(0);

							builder.AppendLine("\t\t///\t<summary>");
							builder.AppendLine($"\t\t///\t{element}");
							builder.AppendLine("\t\t///\t</summary>");
							builder.AppendLine($"\t\t[PgName(\"{element}\")]");

							var elementName = Utilities.NormalizeClassName(element);
							builder.Append($"\t\t{elementName}");
						}
					}
				}
			}

			builder.AppendLine();
			builder.AppendLine("\t}");

			return builder.ToString();
		}

		public string EmitComposite(string schema, string dataType, string className, string connectionString, Dictionary<string, string> replacementsDictionary, List<EntityClassFile> definedElements, List<EntityClassFile> undefinedElements)
		{
			var nn = new NameNormalizer(className);
			var result = new StringBuilder();

			result.Clear();
			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{className}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				result.AppendLine($"\t[PgComposite(\"{dataType}\")]");
			else
				result.AppendLine($"\t[PgComposite(\"{dataType}\", Schema = \"{schema}\")]");

			result.AppendLine($"\tpublic class {className}");
			result.AppendLine("\t{");

			string query = @"
select a.attname as columnname,
	   t.typname as datatype,
	   case when t.typname = 'varchar' then a.atttypmod-4
	        when t.typname = 'bpchar' then a.atttypmod-4
			when t.typname = '_varchar' then a.atttypmod-4
			when t.typname = '_bpchar' then a.atttypmod-4
	        when a.atttypmod > -1 then a.atttypmod
	        else a.attlen end as max_len,
	   case atttypid
            when 21 /*int2*/ then 16
            when 23 /*int4*/ then 32
            when 20 /*int8*/ then 64
         	when 1700 /*numeric*/ then
              	case when atttypmod = -1
                     then 0
                     else ((atttypmod - 4) >> 16) & 65535     -- calculate the precision
                     end
         	when 700 /*float4*/ then 24 /*FLT_MANT_DIG*/
         	when 701 /*float8*/ then 53 /*DBL_MANT_DIG*/
         	else 0
  			end as numeric_precision,
  		case when atttypid in (21, 23, 20) then 0
    		 when atttypid in (1700) then            
        		  case when atttypmod = -1 then 0       
            		   else (atttypmod - 4) & 65535            -- calculate the scale  
        			   end
       		else 0
  			end as numeric_scale,		
	   not a.attnotnull as is_nullable,
	   case when ( a.attgenerated = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_computed,

	   case when ( a.attidentity = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_identity,

	   case when (select indrelid from pg_index as px where px.indisprimary = true and px.indrelid = c.oid and a.attnum = ANY(px.indkey)) = c.oid then true else false end as is_primary,
	   case when (select indrelid from pg_index as ix where ix.indrelid = c.oid and a.attnum = ANY(ix.indkey)) = c.oid then true else false end as is_indexed,
	   case when (select conrelid from pg_constraint as cx where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) = c.oid then true else false end as is_foreignkey,
       (  select cc.relname from pg_constraint as cx inner join pg_class as cc on cc.oid = cx.confrelid where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) as foeigntablename
   from pg_class as c
  inner join pg_namespace as ns on ns.oid = c.relnamespace
  inner join pg_attribute as a on a.attrelid = c.oid and not a.attisdropped and attnum > 0
  inner join pg_type as t on t.oid = a.atttypid
  left outer join pg_attrdef as ad on ad.adrelid = a.attrelid and ad.adnum = a.attnum 
  where ns.nspname = @schema
    and c.relname = @dataType
 order by a.attnum";

			var columns = new List<DBColumn>();
			var candidates = new List<EntityClassFile>();

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", dataType);
					command.Parameters.AddWithValue("@schema", schema);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							NpgsqlDbType theDataType = NpgsqlDbType.Unknown;

							try
							{
								theDataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1));
							}
							catch (InvalidCastException)
							{
								var classFile = new EntityClassFile()
								{
									ClassName = Utilities.NormalizeClassName(reader.GetString(1)),
									SchemaName = schema,
									TableName = reader.GetString(1)
								};

								candidates.Add(classFile);
							}

							var column = new DBColumn
							{
								ColumnName = reader.GetString(0),
								DataType = theDataType,
								dbDataType = reader.GetString(1),
								Length = Convert.ToInt64(reader.GetValue(2)),
								NumericPrecision = Convert.ToInt32(reader.GetValue(3)),
								NumericScale = Convert.ToInt32(reader.GetValue(4)),
								IsNullable = Convert.ToBoolean(reader.GetValue(5)),
								IsComputed = Convert.ToBoolean(reader.GetValue(6)),
								IsIdentity = Convert.ToBoolean(reader.GetValue(7)),
								IsPrimaryKey = Convert.ToBoolean(reader.GetValue(8)),
								IsIndexed = Convert.ToBoolean(reader.GetValue(9)),
								IsForeignKey = Convert.ToBoolean(reader.GetValue(10)),
								ForeignTableName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
								ServerType = DBServerType.POSTGRESQL
							};

							columns.Add(column);
						}
					}
				}
			}

			foreach ( var candidate in candidates )
            {
				if (definedElements.FirstOrDefault(c => string.Equals(c.SchemaName, candidate.SchemaName, StringComparison.OrdinalIgnoreCase) &&
														string.Equals(c.TableName, candidate.TableName, StringComparison.OrdinalIgnoreCase)) == null)
                {
					candidate.ElementType = DBHelper.GetElementType(candidate.SchemaName, candidate.TableName, connectionString);
					undefinedElements.Add(candidate);
				}
            }

			if (undefinedElements.Count > 0)
				return string.Empty;

			bool firstColumn = true;

			foreach (var column in columns)
			{

				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
				}

				AppendNullable(result, column.IsNullable, ref first);


				if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					((NpgsqlDbType)column.DataType == NpgsqlDbType.Name) ||
					((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)))
				{
					if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Bit) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varbit)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
						 ((NpgsqlDbType)column.DataType == NpgsqlDbType.Citext) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Text)))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if (((NpgsqlDbType)column.DataType == NpgsqlDbType.Char) ||
						 ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Char)))
				{
					//	Insert the column definition
					if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
					{
						AppendFixed(result, column.Length, true, ref first);
					}
					else if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
					{
						AppendFixed(result, column.Length, true, ref first);
					}
				}

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea)
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Numeric)
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, column, ref first);

				if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
					replacementsDictionary["$net$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
					replacementsDictionary["$net$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
					replacementsDictionary["$netinfo$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
					replacementsDictionary["$netinfo$"] = "true";

				else if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Bit && column.Length > 1)
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
					replacementsDictionary["$barray$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Point)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Circle)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Box)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Line)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Path)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Polygon)
					replacementsDictionary["$npgsqltypes$"] = "true";

				//	Correct for reserved words
				CorrectForReservedNames(result, column, ref first);

				result.AppendLine(")]");

				var memberName = Utilities.NormalizeClassName(column.ColumnName);
				result.AppendLine($"\t\t[PgName(\"{column.ColumnName}\")]");

				//	Insert the column definition
				result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(schema, column, connectionString, replacementsDictionary["$solutiondirectory$"])} {memberName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		public string EmitMappingModel(List<ClassMember> classMembers, string resourceClassName, string entityClassName, string mappingClassName, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			var ImageConversionRequired = false;
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceClassName);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{nn.SingleForm} Profile for AutoMapper");
			results.AppendLine("\t///\t</summary>");

			results.AppendLine($"\tpublic class {mappingClassName} : Profile");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {nn.SingleForm} Profile");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {mappingClassName}()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tvar rootUrl = Startup.AppConfig.GetSection(\"ApiSettings\").GetValue<string>(\"RootUrl\");");
			results.AppendLine("\t\t\twhile (rootUrl.EndsWith(\"/\") || rootUrl.EndsWith(\"\\\\\"))");

			if (replacementsDictionary["$targetframeworkversion$"] == "3.1" || replacementsDictionary["$targetframeworkversion$"] == "5.0")
			{
				results.AppendLine("\t\t\t\trootUrl = rootUrl[0..^1];");
			}
			else
			{
				results.AppendLine("\t\t\t\trootUrl = rootUrl.Substring(0, rootUrl.Length - 1);");
			}

			#region Create the Resource to Entity Mapping
			results.AppendLine();
			results.AppendLine($"\t\t\tCreateMap<{resourceClassName}, {entityClassName}>()");

			bool first = true;

			//	Emit known mappings
			foreach (var member in classMembers)
			{
				if (string.IsNullOrWhiteSpace(member.ResourceMemberName))
				{
				}
				else if (member.ChildMembers.Count == 0 && member.EntityNames.Count == 0)
				{
				}
				else if (string.Equals(member.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase))
				{
					int ix = 0 - member.EntityNames.Count + 1;
					foreach (var entityColumn in member.EntityNames)
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						string dataType = "Unknown";

						if (entityColumn.ServerType == DBServerType.MYSQL)
							dataType = DBHelper.GetNonNullableMySqlDataType(entityColumn);
						else if (entityColumn.ServerType == DBServerType.POSTGRESQL)
							dataType = DBHelper.GetNonNullablePostgresqlDataType(entityColumn);
						else if (entityColumn.ServerType == DBServerType.SQLSERVER)
							dataType = DBHelper.GetNonNullableSqlServerDataType(entityColumn);

						if (ix == 0)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.GetId<{dataType}>()))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.GetId<{dataType}>({ix})))");
						ix++;
					}
				}
				else if (member.EntityNames.Count > 0 && member.EntityNames[0].IsForeignKey)
				{
					int ix = 0 - member.EntityNames.Count + 1;
					foreach (var entityColumn in member.EntityNames)
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						string dataType = "Unknown";

						if (entityColumn.ServerType == DBServerType.MYSQL)
							dataType = DBHelper.GetNonNullableMySqlDataType(entityColumn);
						else if (entityColumn.ServerType == DBServerType.POSTGRESQL)
							dataType = DBHelper.GetNonNullablePostgresqlDataType(entityColumn);
						else if (entityColumn.ServerType == DBServerType.SQLSERVER)
							dataType = DBHelper.GetNonNullableSqlServerDataType(entityColumn);

						if (entityColumn.IsNullable)
						{
							if (ix == 0)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName} == null ? ({dataType}?) null : src.{member.ResourceMemberName}.GetId<{dataType}>()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName} == null ? ({dataType}?) null : src.{member.ResourceMemberName}.GetId<{dataType}>({ix})))");
						}
						else
						{
							if (ix == 0)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.GetId<{dataType}>()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.GetId<{dataType}>({ix})))");
						}
						ix++;
					}
				}
				else
				{
					EmitResourceToEntityMapping(results, member, "", ref first, ref ImageConversionRequired);
				}
			}
			results.AppendLine(";");

			//	Emit To Do for unknown mappings
			foreach (var member in classMembers)
			{
				if (string.IsNullOrWhiteSpace(member.ResourceMemberName))
				{
					foreach (var entityMember in member.EntityNames)
					{
						results.AppendLine($"\t\t\t\t//\tTo do: Write mapping for {entityMember.EntityName}");
					}
				}
			}
			results.AppendLine();
			#endregion

			#region Create Entity to Resource Mapping
			results.AppendLine($"\t\t\tCreateMap<{entityClassName}, {resourceClassName}>()");

			//	Emit known mappings
			first = true;
			var activeDomainMembers = classMembers.Where(m => !string.IsNullOrWhiteSpace(m.ResourceMemberName) && CheckMapping(m));

			foreach (var member in activeDomainMembers)
			{
				if (member.EntityNames.Count > 0 && member.EntityNames[0].IsPrimaryKey)
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src => new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}/id");
					foreach (var entityColumn in member.EntityNames)
					{
						results.Append($"/{{src.{entityColumn.ColumnName}}}");
					}
					results.Append("\")))");
				}
				else if (member.EntityNames.Count > 0 && member.EntityNames[0].IsForeignKey)
				{
					var nf = new NameNormalizer(member.EntityNames[0].ForeignTableName);
					var isNullable = member.EntityNames.Where(c => c.IsNullable).Count() > 0;

					if (first)
						first = false;
					else
						results.AppendLine();

					if (isNullable)
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src => src.{member.EntityNames[0].EntityName} == null ? (Uri) null : new Uri($\"{{rootUrl}}/{nf.PluralCamelCase}/id");
						foreach (var entityColumn in member.EntityNames)
						{
							results.Append($"/{{src.{entityColumn.ColumnName}}}");
						}
						results.Append("\")))");
					}
					else
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src => new Uri($\"{{rootUrl}}/{nf.PluralCamelCase}/id");
						foreach (var entityColumn in member.EntityNames)
						{
							results.Append($"/{{src.{entityColumn.EntityName}}}");
						}
						results.Append("\")))");
					}
				}
				else
				{
					EmityEntityToResourceMapping(results, member, ref first, ref ImageConversionRequired);
				}
			}
			results.AppendLine(";");

			var inactiveDomainMembers = classMembers.Where(m => !string.IsNullOrWhiteSpace(m.ResourceMemberName) && !CheckMapping(m));

			//	Emit To Do for unknown Mappings
			foreach (var member in inactiveDomainMembers)
			{
				results.AppendLine($"\t\t\t\t//\tTo do: Write mapping for {member.ResourceMemberName}");
			}
			results.AppendLine();
			#endregion

			results.AppendLine($"\t\t\tCreateMap<RqlCollection<{entityClassName}>, RqlCollection<{resourceClassName}>>()");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Href, opts => opts.MapFrom(src => new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Href.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.First, opts => opts.MapFrom(src => src.First == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.First.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Next, opts => opts.MapFrom(src => src.Next == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Next.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Previous, opts => opts.MapFrom(src => src.Previous == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Previous.Query}}\")));");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitResourceModel(List<ClassMember> entityClassMembers, string resourceClassName, string entityClassName, DBTable table, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary, string connectionString)
		{
			replacementsDictionary.Add("$resourceimage$", "false");
			replacementsDictionary.Add("$resourcenet$", "false");
			replacementsDictionary.Add("$resourcenetinfo$", "false");
			replacementsDictionary.Add("$resourcebarray$", "false");
			replacementsDictionary.Add("$usenpgtypes$", "false");

			var results = new StringBuilder();
			bool hasPrimary = false;

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({entityClassName}))]");
			results.AppendLine($"\tpublic class {resourceClassName}");
			results.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var member in entityClassMembers)
			{
				if (firstColumn)
					firstColumn = false;
				else
					results.AppendLine();

				if (member.EntityNames[0].IsPrimaryKey)
				{
					if (!hasPrimary)
					{
						results.AppendLine("\t\t///\t<summary>");
						results.AppendLine($"\t\t///\tThe hypertext reference that identifies the resource.");
						results.AppendLine("\t\t///\t</summary>");
						results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
						hasPrimary = true;
					}
				}
				else if (member.EntityNames[0].IsForeignKey)
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\tA hypertext reference that identifies the associated {member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");
					results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
				}
				else
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\t{member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");

					if (member.EntityNames[0].ServerType == DBServerType.SQLSERVER && (SqlDbType)member.EntityNames[0].DataType == SqlDbType.Image)
						replacementsDictionary["$resourceimage$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Inet)
						replacementsDictionary["$resourcenet$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Cidr)
						replacementsDictionary["$resourcenet$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr)
						replacementsDictionary["$resourcenetinfo$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr8)
						replacementsDictionary["$resourcenetinfo$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
						replacementsDictionary["$resourcebarray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
						replacementsDictionary["$resourcebarray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Bit && member.EntityNames[0].Length > 1)
						replacementsDictionary["$resourcebarray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Varbit)
						replacementsDictionary["$resourcebarray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Unknown ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Point ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.LSeg ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Path ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Circle ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Polygon ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Line ||
						member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Box	)
						replacementsDictionary["$usenpgtypes$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL)
					{
						var solutionFolder = replacementsDictionary["$solutiondirectory$"];
						var dataType = DBHelper.GetPostgresqlResourceDataType(member.EntityNames[0], connectionString, table.Schema, solutionFolder);
						results.AppendLine($"\t\tpublic {dataType} {member.ResourceMemberName} {{ get; set; }}");
					}
					else if (member.EntityNames[0].ServerType == DBServerType.MYSQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetMySqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (member.EntityNames[0].ServerType == DBServerType.SQLSERVER)
						results.AppendLine($"\t\tpublic {DBHelper.GetSqlServerResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
				}
			}

			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitEntityModel(DBTable table, string entityClassName, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary, string connectionString)
		{
			var result = new StringBuilder();
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");
			replacementsDictionary.Add("$barray$", "false");

			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{entityClassName}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(table.Schema))
				result.AppendLine($"\t[Table(\"{table.Table}\")]");
			else
				result.AppendLine($"\t[Table(\"{table.Table}\", Schema = \"{table.Schema}\")]");

			result.AppendLine($"\tpublic class {entityClassName}");
			result.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var column in columns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
				}

				AppendNullable(result, column.IsNullable, ref first);

				if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
				{
					if (column.Length > 1)
						AppendFixed(result, column.Length, true, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Name) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
				{
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bit) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if ((column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varbit)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Citext) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Text)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Char)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String))
				{
					//	Insert the column definition
					if (column.ServerType == DBServerType.POSTGRESQL)
					{
						if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
						else if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
					}
					else if (column.ServerType == DBServerType.MYSQL)
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
					else
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
				{
					AppendFixed(result, column.Length, true, ref first);
					AppendAutofield(result, ref first);
				}

				if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Decimal) ||
					(column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal) ||
					(column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Numeric))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, column, ref first);

				if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
					replacementsDictionary["$image$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
					replacementsDictionary["$net$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
					replacementsDictionary["$net$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
					replacementsDictionary["$netinfo$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
					replacementsDictionary["$netinfo$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
					replacementsDictionary["$barray$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
					replacementsDictionary["$barray$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bit && column.Length > 1)
					replacementsDictionary["$barray$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
					replacementsDictionary["$barray$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Point)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Circle)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Box)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Line)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Path)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.LSeg)
					replacementsDictionary["$npgsqltypes$"] = "true";

				if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Polygon)
					replacementsDictionary["$npgsqltypes$"] = "true";

				//	Correct for reserved words
				CorrectForReservedNames(result, column, ref first);

				result.AppendLine(")]");

				//	Insert the column definition
				if (column.ServerType == DBServerType.POSTGRESQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(table.Schema, column, connectionString, replacementsDictionary["$solutiondirectory$"])} {column.ColumnName} {{ get; set; }}");
				else if (column.ServerType == DBServerType.MYSQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetMySqlDataType(column)} {column.ColumnName} {{ get; set; }}");
				else if (column.ServerType == DBServerType.SQLSERVER)
					result.AppendLine($"\t\tpublic {DBHelper.GetSQLServerDataType(column)} {column.ColumnName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		private void AppendPrimaryKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsPrimaryKey = true");
		}
		
		private void AppendComma(StringBuilder result, ref bool first)
		{
			if (first)
				first = false;
			else
				result.Append(", ");
		}
		
		private void AppendIdentity(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIdentity = true, AutoField = true");
		}
		
		private void AppendIndexed(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIndexed = true");
		}
		
		private void AppendForeignKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsForeignKey = true");
		}

		private void AppendNullable(StringBuilder result, bool isNullable, ref bool first)
		{
			AppendComma(result, ref first);

			if (isNullable)
				result.Append("IsNullable = true");
			else
				result.Append("IsNullable = false");
		}
		
		private void CorrectForReservedNames(StringBuilder result, DBColumn column, ref bool first)
		{
			if (string.Equals(column.ColumnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "as", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "base", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "bool", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "break", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "byte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "case", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "catch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "char", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "checked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "class", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "const", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "continue", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "default", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "do", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "double", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "else", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "enum", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "event", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "extern", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "false", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "finally", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "float", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "for", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "goto", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "if", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "in", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "int", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "interface", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "internal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "is", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "lock", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "long", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "new", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "null", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "object", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "operator", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "out", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "override", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "params", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "private", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "protected", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "public", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ref", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "return", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "short", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "static", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "string", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "struct", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "switch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "this", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "throw", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "true", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "try", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "uint", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "using", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "void", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "while", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "add", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "alias", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "async", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "await", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "by", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "descending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "equals", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "from", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "get", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "global", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "group", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "into", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "join", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "let", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "on", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "partial", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "remove", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "select", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "set", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "value", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "var", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "when", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "where", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(column.ColumnName, "yield", StringComparison.OrdinalIgnoreCase))
			{
				AppendComma(result, ref first);
				result.Append($"ColumnName = \"{column.ColumnName}\"");
				column.ColumnName += "_Value";
			}
		}
		
		private void AppendDatabaseType(StringBuilder result, DBColumn column, ref bool first)
		{
			AppendComma(result, ref first);

			if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar)
				result.Append("NativeDataType=\"VarChar\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary)
				result.Append("NativeDataType=\"VarBinary\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
				result.Append("NativeDataType=\"char\"");
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Decimal)
				result.Append("NativeDataType=\"Decimal\"");
			else
				result.Append($"NativeDataType=\"{column.dbDataType}\"");
		}

		private void AppendFixed(StringBuilder result, long length, bool isFixed, ref bool first)
		{
			AppendComma(result, ref first);

			if (length == -1)
			{
				if (isFixed)
					result.Append($"IsFixed = true");
				else
					result.Append($"IsFixed = false");
			}
			else
			{
				if (isFixed)
					result.Append($"Length = {length}, IsFixed = true");
				else
					result.Append($"Length = {length}, IsFixed = false");
			}
		}
		
		private void AppendAutofield(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("AutoField = true");
		}
		
		private void AppendPrecision(StringBuilder result, int NumericPrecision, int NumericScale, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"Precision={NumericPrecision}, Scale={NumericScale}");
		}
		
		private bool CheckMapping(ClassMember member)
		{
			if (member.EntityNames.Count > 0)
				return true;

			bool HasMapping = false;

			foreach (var childMember in member.ChildMembers)
			{
				HasMapping |= CheckMapping(childMember);
			}

			return HasMapping;
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

		private void EmitChildSet(StringBuilder results, ClassMember member, ref bool subFirst)
		{
			if (member.EntityNames.Count > 0)
			{
				if (subFirst)
					subFirst = false;
				else
					results.AppendLine(",");

				results.Append($"\t\t\t\t{member.ResourceMemberName} = src.{member.EntityNames[0].EntityName}");
			}
		}

		private void EmitNullTest(StringBuilder results, ClassMember member, ref bool first)
		{
			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					EmitNullTest(results, childMember, ref first);
				}
			}
			else
			{
				foreach (var entityMember in member.EntityNames)
				{
					if (first)
						first = false;
					else
					{
						results.AppendLine(" &&");
						results.Append("\t\t\t\t\t ");
					}

					if (string.Equals(entityMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
					{
						results.Append($"string.IsNullOrWhiteSpace(src.{entityMember.EntityName})");
					}
					else
					{
						results.Append($"src.{entityMember.EntityName} == null");
					}
				}
			}
		}

		private void EmityEntityToResourceMapping(StringBuilder results, ClassMember member, ref bool first, ref bool ImageConversionRequired)
		{
			if (member.ChildMembers.Count > 0)
			{
				bool isNullable = IsNullable(member);

				if (isNullable)
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					results.AppendLine($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src =>");
					results.Append("\t\t\t\t\t(");
					bool subFirst = true;

					foreach (var childMember in member.ChildMembers)
					{
						EmitNullTest(results, childMember, ref subFirst);
					}

					results.Append($") ? null : new {member.ResourceMemberType}() {{");

					subFirst = true;
					foreach (var childMember in member.ChildMembers)
					{
						EmitChildSet(results, childMember, ref subFirst);
					}

					results.Append("}))");
				}
				else
				{
					bool doThis = true;

					foreach (var childMember in member.ChildMembers)
					{
						if (childMember.ChildMembers.Count == 0 &&
							 childMember.EntityNames.Count == 0)
						{
							doThis = false;
						}
					}

					if (doThis)
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						results.AppendLine($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src =>");
						results.AppendLine($"\t\t\t\t\tnew {member.ResourceMemberType}() {{");

						bool subFirst = true;
						foreach (var childMember in member.ChildMembers)
						{
							EmitChildSet(results, childMember, ref subFirst);
						}

						results.Append($"}}))");
					}
				}
			}
			else
			{
				var entityColumn = member.EntityNames[0];

				if (!string.Equals(entityColumn.EntityType, member.ResourceMemberType, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(entityColumn.EntityType, "char[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : new string(src.{entityColumn.EntityName})))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => new string(src.{entityColumn.EntityName})))");
					}
					else if (string.Equals(entityColumn.EntityType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "char[]", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : src.{entityColumn.EntityName}.ToArray()))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName}.ToArray()))");
					}
					else if (string.Equals(entityColumn.EntityType, "Image", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "byte[]", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						ImageConversionRequired = true;

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : src.{entityColumn.EntityName}.GetBytes()))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName}.GetBytes()))");
					}
					else if (string.Equals(entityColumn.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Image", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						ImageConversionRequired = true;

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : ImageEx.Parse(src.{entityColumn.EntityName})))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => ImageEx.Parse(src.{entityColumn.EntityName})))");
					}
					else if (string.Equals(entityColumn.EntityType, "DateTime", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTimeOFfset", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.HasValue ? new DateTimeOffset(src.{member.ResourceMemberName}.Value) : (DateTimeOffset?) null))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => new DateTimeOffset(src.{member.ResourceMemberName})))");
					}
					else if (string.Equals(entityColumn.EntityType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTime", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.HasValue ? src.{member.ResourceMemberName}.Value.DateTime : (DateTime?) null))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.DateTime))");
					}
				}
				else if (!string.Equals(member.ResourceMemberName, member.EntityNames[0].EntityName, StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src => src.{entityColumn.EntityName}))");
				}
			}
		}

		private void EmitResourceToEntityMapping(StringBuilder results, ClassMember member, string prefix, ref bool first, ref bool ImageConversionRequired)
		{
			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					EmitResourceToEntityMapping(results, childMember, $"{prefix}{member.ResourceMemberName}", ref first, ref ImageConversionRequired);
				}
			}
			else if (member.EntityNames.Count > 0)
			{
				var entityColumn = member.EntityNames[0];

				if (!string.Equals(entityColumn.EntityType, member.ResourceMemberType, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(entityColumn.EntityType, "char[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName} == null ? null : src.{prefix}.{member.ResourceMemberName}.ToArray()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName}.ToArray()))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName} == null ? null : src.{prefix}{member.ResourceMemberName}.ToArray()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName}.ToArray()))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "char[]", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName} == null ? null : new string(src.{prefix}.{member.ResourceMemberName})))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => new string(src.{prefix}.{member.ResourceMemberName})))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName} == null ? null : new string(src.{prefix}{member.ResourceMemberName})))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => new string(src.{prefix}.{member.ResourceMemberName})))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "Image", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "byte[]", StringComparison.OrdinalIgnoreCase))
					{
						ImageConversionRequired = true;
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName} == null ? null : ImageEx.Parse(src.{member.ResourceMemberName})))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => ImageEx.Parse(src.{member.ResourceMemberName})))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName} == null ? null : ImageEx.Parse(src.{prefix}{member.ResourceMemberName})))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => ImageEx.Parse(src.{prefix}.{member.ResourceMemberName})))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Image", StringComparison.OrdinalIgnoreCase))
					{
						ImageConversionRequired = true;
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName} == null ? null : src.{member.ResourceMemberName}.GetBytes()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.GetBytes()))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName} == null ? null : src.{prefix}{member.ResourceMemberName}.GetBytes()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName}.GetBytes()))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "DateTime", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.HasValue ? src.{member.ResourceMemberName}.Value.DateTime : (DateTime?) null))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.DateTime))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName}.HasValue ? src.{prefix}.{member.ResourceMemberName}.Value.DateTime : (DateTime?) null))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix}.{member.ResourceMemberName}.DateTime))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTime", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{member.ResourceMemberName}.HasValue ? new DateTimeOffset(src.{member.ResourceMemberName}.Value) : (DateTimeOffset?) null))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => new DateTimeOffset(src.{member.ResourceMemberName})))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName}.HasValue ? new DateTimeOffset(src.{prefix}.{member.ResourceMemberName}.Value) : (DateTimeOffset?) null))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom( src => new DateTimeOffset(src.{prefix}.{member.ResourceMemberName})))");
						}
					}
					else if (string.Equals(entityColumn.EntityType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Uri", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (string.IsNullOrWhiteSpace(prefix))
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName} == null ? (string) null : src.{member.ResourceMemberName}.IsAbsoluteUri ? src.{member.ResourceMemberName}.ToString() : (new Uri(new Uri(rootUrl), src.{member.ResourceMemberName}.ToString())).ToString()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.IsAbsoluteUri ? src.{member.ResourceMemberName}.ToString() : (new Uri(new Uri(rootUrl), src.{member.ResourceMemberName}.ToString())).ToString()))");
						}
						else
						{
							if (entityColumn.IsNullable)
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? (string) null : {prefix}.{member.ResourceMemberName} == null ? (string) null : src.{prefix}.{member.ResourceMemberName}.IsAbsoluteUri ? src.{prefix}.{member.ResourceMemberName}.ToString() : (new Uri(new Uri(rootUrl), src.{prefix}.{member.ResourceMemberName}.ToString())).ToString()))");
							else
								results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? null : src => src.{prefix}.{member.ResourceMemberName}.IsAbsoluteUri ? src.{prefix}.{member.ResourceMemberName}.ToString() : (new Uri(new Uri(rootUrl), src.{prefix}.{member.ResourceMemberName}.ToString())).ToString()))");
						}
					}
				}
				else if (string.Equals(entityColumn.EntityType, "Uri", StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					if (string.IsNullOrWhiteSpace(prefix))
					{
						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName} == null ? (Uri) null : src.{member.ResourceMemberName}.IsAbsoluteUri ? src.{member.ResourceMemberName} : new Uri(new Uri(rootUrl), src.{member.ResourceMemberName}.ToString())))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}.IsAbsoluteUri ? src.{member.ResourceMemberName} : new Uri(new Uri(rootUrl), src.{member.ResourceMemberName}.ToString())))");
					}
					else
					{
						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? (Uri) null : {prefix}.{member.ResourceMemberName} == null ? (Uri) null : src.{prefix}.{member.ResourceMemberName}.IsAbsoluteUri ? src.{prefix}.{member.ResourceMemberName} : new Uri(new Uri(rootUrl), src.{prefix}.{member.ResourceMemberName}.ToString())))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? (Uri) null : src => src.{prefix}.{member.ResourceMemberName}.IsAbsoluteUri ? src.{prefix}.{member.ResourceMemberName} : new Uri(new Uri(rootUrl), src.{prefix}.{member.ResourceMemberName}.ToString())))");
					}
				}
				else if (!string.Equals(member.ResourceMemberName, member.EntityNames[0].EntityName, StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					if (string.IsNullOrWhiteSpace(prefix))
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix}.{member.ResourceMemberName}))");
					}
					else
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName}))");
					}
				}
			}
		}
		
		private void GetSqlServerValue(DBColumn column, JObject ExampleValue, StringBuilder results)
		{

			switch ((SqlDbType)column.DataType)
			{
				case SqlDbType.Xml:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
						break;
					}

				case SqlDbType.BigInt:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<long>()}L");
						break;
					}

				case SqlDbType.Binary:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")");
						break;
					}

				case SqlDbType.VarBinary:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")");
						break;
					}

				case SqlDbType.Image:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = ImageEx.Parse(\"{Convert.ToBase64String(value.Value<byte[]>())}\")");
						break;
					}

				case SqlDbType.Timestamp:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")");
						break;
					}

				case SqlDbType.Bit:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<bool?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						if (value.Value<bool>())
							results.Append($"\t\t\t\t{column.EntityName} = true");
						else
							results.Append($"\t\t\t\t{column.EntityName} = false");
						break;
					}

				case SqlDbType.Char:
				case SqlDbType.NChar:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						if (column.Length == 1)
							results.Append($"\t\t\t\t{column.EntityName} = '{value.Value<string>()}'");
						else
							results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");

						break;
					}

				case SqlDbType.Date:
					{
						var value = ExampleValue[column.ColumnName];


						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()}\")");
						break;
					}

				case SqlDbType.DateTime:
				case SqlDbType.DateTime2:
				case SqlDbType.SmallDateTime:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()} {value.Value<DateTime>().ToShortTimeString()}\")");
						break;
					}

				case SqlDbType.DateTimeOffset:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<DateTimeOffset?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						var dto = value.Value<DateTimeOffset>();
						var x = dto.ToString("MM/dd/yyyy hh:mm:ss zzz");

						results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
						break;
					}

				case SqlDbType.Decimal:
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<decimal>()}m");
						break;
					}

				case SqlDbType.Float:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<double>()}");
						break;
					}

				case SqlDbType.Int:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<int>()}");
						break;
					}

				case SqlDbType.NText:
				case SqlDbType.Text:
				case SqlDbType.NVarChar:
				case SqlDbType.VarChar:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
						break;
					}

				case SqlDbType.Real:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<float?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<float>()}f");
						break;
					}

				case SqlDbType.SmallInt:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<short>()}");
						break;
					}

				case SqlDbType.Time:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{value.Value<TimeSpan>()}\")");
						break;
					}


				case SqlDbType.TinyInt:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<byte>()}");
						break;
					}

				case SqlDbType.UniqueIdentifier:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<Guid?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = Guid.Parse(\"{value.Value<Guid>().ToString()}\")");
						break;
					}

				default:
					results.Append($"\t\t\t\t{column.EntityName} = Unknown");
					break;
			}
		}
		
		private void GetMySqlValue(DBColumn column,  JObject ExampleValue, StringBuilder results)
		{

			switch ((MySqlDbType)column.DataType)
			{
				case MySqlDbType.Byte:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<sbyte?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<sbyte>()}");
					}
					break;

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
				case MySqlDbType.TinyBlob:
				case MySqlDbType.Blob:
				case MySqlDbType.MediumBlob:
				case MySqlDbType.LongBlob:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						var str = Convert.ToBase64String(value.Value<byte[]>());
						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String(\"{str}\")");
					}
					break;

				case MySqlDbType.Enum:
				case MySqlDbType.Set:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
								if (value.Value<byte[]>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
						}

						results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
					}
					break;

				case MySqlDbType.UByte:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<byte>()}");
						break;
					}

				case MySqlDbType.Int16:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<short>()}");
						break;
					}

				case MySqlDbType.UInt16:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<ushort?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<ushort>()}u");
						break;
					}

				case MySqlDbType.Int24:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<int>()}");
						break;
					}

				case MySqlDbType.UInt24:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<uint>()}u");
						break;
					}

				case MySqlDbType.Int32:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<int>()}");
						break;
					}

				case MySqlDbType.UInt32:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<uint>()}u");
						break;
					}

				case MySqlDbType.Int64:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<long>()}L");
						break;
					}

				case MySqlDbType.UInt64:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<ulong?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<ulong>()}uL");
						break;
					}

				case MySqlDbType.Decimal:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<decimal>()}m");
						break;
					}

				case MySqlDbType.Double:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<double>()}");
						break;
					}

				case MySqlDbType.Float:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<float?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<float>()}f");
						break;
					}

				case MySqlDbType.String:
					if (column.Length == 1)
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<char?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = '{value.Value<char>()}'");
						break;
					}
					else
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
						break;
					}

				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.Text:
				case MySqlDbType.TinyText:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
						break;
					}

				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
						results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
						break;
					}

				case MySqlDbType.Date:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
						results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
						break;

					}

				case MySqlDbType.Time:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
						results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{x}\")");
						break;
					}

				case MySqlDbType.Year:
					{
						var value = ExampleValue[column.ColumnName];

						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}
						}

						results.Append($"\t\t\t\t{column.EntityName} = {value.Value<int>()}");
						break;
					}

				case MySqlDbType.Bit:
					{
						var value = ExampleValue[column.ColumnName];

						if (string.Equals(column.dbDataType, "bit(1)", StringComparison.OrdinalIgnoreCase))
						{
							if (column.IsNullable)
							{
								if (value.Value<bool?>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = {value.Value<bool>().ToString().ToLower()}");
							break;
						}
						else
						{
							if (column.IsNullable)
							{
								if (value.Value<ulong?>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = {value.Value<ulong>()}uL");
							break;
						}
					}

				default:
					results.Append($"\t\t\t\t{column.EntityName} = unknown");
					break;
			}
		}

		private void EmitPostgresValue(DBColumn column, EntityDetailClassFile parentclass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles, int indents)
		{
			for (var i = 0; i < indents; i++)
				results.Append("\t");

			try
			{
				switch ((NpgsqlDbType)column.DataType)
				{
					case NpgsqlDbType.Point:
						EmitPostgresPointValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Point:
						EmitPostgresPointArrayValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.LSeg:
						EmitPostgresLSegValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.LSeg:
						EmitPostgresLSegArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Path:
						EmitPostgresPathValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Path:
						EmitPostgresPathArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Polygon:
						EmitPostgresPolygonValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Polygon:
						EmitPostgresPolygonValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Circle:
						EmitPostgresCircleValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Circle:
						EmitPostgresCircleArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Line:
						EmitPostgresLineValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Line:
						EmitPostgresLineArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Box:
						EmitPostgresBoxValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Box:
						EmitPostgresBoxArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Oid:
					case NpgsqlDbType.Xid:
					case NpgsqlDbType.Cid:
						EmitPostgresUintValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Oid:
					case NpgsqlDbType.Array | NpgsqlDbType.Xid:
					case NpgsqlDbType.Array | NpgsqlDbType.Cid:
						EmitPostgresUintArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Smallint:
						EmitPostgresShortValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
						EmitPostgresShortArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Integer:
						EmitPostgresIntValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						EmitPostgresIntArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Bigint:
						EmitPostgresLongValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						EmitPostgresLongArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Real:
						EmitPostgresRealValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Real:
						EmitPostgresRealArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Double:
						EmitPostgresDoubleValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Double:
						EmitPostgresDoubleArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						EmitPostgresDecimalValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
					case NpgsqlDbType.Array | NpgsqlDbType.Money:
						EmitPostgresDecimalArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Uuid:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = Guid.Parse(\"{value.Value<Guid>()}\")");
							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder();
							builder.Append("new Guid[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"Guid.Parse(\"{charValue.Value<Guid>()}\")");
							}

							builder.Append("}");
							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							break;
						}

					case NpgsqlDbType.Json:
					case NpgsqlDbType.Jsonb:
					case NpgsqlDbType.JsonPath:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var str = value.Value<string>();
							str = str.Replace("\"", "\\\"");

							results.Append($"\t\t\t\t{column.EntityName} = \"{str}\"");
							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Json:
					case NpgsqlDbType.Array | NpgsqlDbType.Jsonb:
					case NpgsqlDbType.Array | NpgsqlDbType.JsonPath:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder();
							builder.Append("new string[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								var str = charValue.Value<string>();
								str = str.Replace("\"", "\\\"");

								builder.Append($"\"{str}\"");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							break;
						}

					case NpgsqlDbType.Varbit:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Array)
								{
									if (value.Value<JArray>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
								else if (value.Type == JTokenType.String)
								{
									if (string.IsNullOrWhiteSpace(value.Value<string>()))
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
							}

							if (value.Type == JTokenType.String)
							{
								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
								break;
							}
							else if ( value.Type == JTokenType.Boolean)
                            {
								var strval = value.Value<bool>() ? "true" : "false";
								results.Append($"\t\t\t\t{column.EntityName} = {strval}");
								break;
							}
							else if ( value.Type == JTokenType.Array)
							{
								if (column.Length == 1)
								{
									var strval = value.Value<JArray>()[0].Value<bool>() ? "true" : "false";
									results.Append($"\t\t\t\t{column.EntityName} = {strval}");
									break;
								}
								else
								{
									var strVal = new StringBuilder();
									foreach (bool bVal in value.Value<JArray>())
									{
										strVal.Append(bVal ? "1" : "0");
									}

									results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
									break;
								}
							}
							else
                            {
								results.Append($"\t\t\t\t{column.EntityName} = Unknown(Varbit)");
								break;
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.String)
								{
									if (string.IsNullOrWhiteSpace(value.Value<string>()))
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
								else if (value.Type == JTokenType.Array)
								{
									if (value.Value<JArray>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
							}

							if (value.Type == JTokenType.String)
							{
								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
								return;
							}
							else if (value.Type == JTokenType.Boolean)
							{
								var strval = value.Value<bool>() ? "true" : "false";
								results.Append($"\t\t\t\t{column.EntityName} = {strval}");
								return;
							}
							else if (value.Type == JTokenType.Array)
							{
								var array = value.Value<JArray>();

								if (array.Count == 0)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}

								var childElement = array[0];

								if (childElement.Type == JTokenType.Boolean)
								{
									var sresult = new StringBuilder();
									foreach (bool bVal in array)
									{
										sresult.Append(bVal ? "1" : "0");
									}

									results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{sresult.ToString()}\")");
									return;
								}
								else
								{
									var builder = new StringBuilder();
									var answer = value.Value<JArray>();

									builder.Append("new BitArray[] {");
									bool firstGroup = true;

									foreach (var group in answer)
									{
										if (firstGroup)
											firstGroup = false;
										else
											builder.Append(", ");

										if (group.Type == JTokenType.String)
										{
											builder.Append($"BitArrayExt.Parse(\"{group.Value<string>()}\")");
										}
										else
										{
											var strValue = new StringBuilder();

											foreach (bool bVal in group)
											{
												strValue.Append(bVal ? "1" : "0");
											}

											builder.Append($"BitArrayExt.Parse(\"{strValue.ToString()}\")");
										}
									}

									builder.Append("}");
									results.Append($"\t\t\t\t{column.EntityName} = {builder}");
									return;
								}
							}
							else
							{
								results.Append($"\t\t\t\t{column.EntityName} = Unknown");
								return;
							}
						}

					case NpgsqlDbType.Bit:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (column.Length == 1)
								{
									if (value.Value<bool?>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
								else if (value.Type == JTokenType.String && value.Value<string>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Array && value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (column.Length == 1)
							{
								results.Append($"\t\t\t\t{column.EntityName} = {value.Value<bool>().ToString().ToLower()}");
								return;
							}
							else
							{
								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
								return;
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bit:
						{
							var value = ExampleValue[column.ColumnName];

							if (value.Type == JTokenType.Null)
							{
								results.Append($"\t\t\t\t{column.EntityName} = null");
								return;
							}

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.String)
								{
									if (string.IsNullOrWhiteSpace(value.Value<string>()))
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
								else if (value.Type == JTokenType.Array)
								{
									if (value.Value<JArray>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
							}

							if (value.Type == JTokenType.String)
							{
								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
								return;
							}
							else if (value.Type == JTokenType.Boolean)
							{
								var strval = value.Value<bool>() ? "true" : "false";
								results.Append($"\t\t\t\t{column.EntityName} = {strval}");
								return;
							}
							else if (value.Type == JTokenType.Array)
							{
								var array = value.Value<JArray>();

								if (array.Count == 0)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}

								var childElement = array[0];

								if (childElement.Type == JTokenType.Boolean)
								{
									var sresult = new StringBuilder();
									foreach (bool bVal in array)
									{
										sresult.Append(bVal ? "1" : "0");
									}

									results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{sresult.ToString()}\")");
									return;
								}
								else
								{
									var builder = new StringBuilder();
									var answer = value.Value<JArray>();

									builder.Append("new BitArray[] {");
									bool firstGroup = true;

									foreach (var group in answer)
									{
										if (firstGroup)
											firstGroup = false;
										else
											builder.Append(", ");

										if (group.Type == JTokenType.String)
										{
											builder.Append($"BitArrayExt.Parse(\"{group.Value<string>()}\")");
										}
										else
										{
											var strValue = new StringBuilder();

											foreach (bool bVal in group)
											{
												strValue.Append(bVal ? "1" : "0");
											}

											builder.Append($"BitArrayExt.Parse(\"{strValue.ToString()}\")");
										}
									}

									builder.Append("}");
									results.Append($"\t\t\t\t{column.EntityName} = {builder}");
									return;
								}
							}
							else
							{
								results.Append($"\t\t\t\t{column.EntityName} = Unknown");
								return;
							}
						}

					case NpgsqlDbType.Bytea:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<byte[]>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")");
							return;
						}

					case NpgsqlDbType.Inet:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (string.IsNullOrWhiteSpace(value.Value<string>()))
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = IPAddress.Parse(\"{value.Value<string>()}\")");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Inet:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new IPAddress[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"IPAddress.Parse(\"{group.Value<string>()}\")");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Cidr:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var ipAddress = value["IPAddress"].Value<string>();
							var filter = Convert.ToInt32(value["Filter"].Value<string>());

							results.Append($"\t\t\t\t{column.EntityName} = new ValueTuple<IPAddress,int>(IPAddress.Parse(\"{ipAddress}\"), {filter})");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Cidr:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new ValueTuple<IPAddress,int>[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								var ipAddress = group["IPAddress"].Value<string>();
								var filter = Convert.ToInt32(group["Filter"].Value<string>());

								builder.Append($"new ValueTuple<IPAddress,int>(IPAddress.Parse(\"{ipAddress}\"), {filter})");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.MacAddr:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (string.IsNullOrWhiteSpace(value.Value<string>()))
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = PhysicalAddress.Parse(\"{value.Value<string>()}\")");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.MacAddr:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new PhysicalAddress[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"PhysicalAddress.Parse(\"{group.Value<string>()}\")");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.MacAddr8:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (string.IsNullOrWhiteSpace(value.Value<string>()))
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = PhysicalAddress.Parse(\"{value.Value<string>()}\")");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.MacAddr8:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new PhysicalAddress[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"PhysicalAddress.Parse(\"{group.Value<string>()}\")");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new byte[][] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"Convert.FromBase64String(\"{Convert.ToBase64String(group.Value<byte[]>())}\")");
							}

							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Boolean:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<bool?>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var strval = value.Value<bool>() ? "true" : "false";
							results.Append($"\t\t\t\t{column.EntityName} = {strval}");
							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (value.Type == JTokenType.String)
							{
								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{value.Value<string>()}\")");
							}
							else
							{
								var strValue = new StringBuilder();

								foreach (bool bVal in value.Value<JArray>())
								{
									strValue.Append(bVal ? "1" : "0");
								}

								results.Append($"\t\t\t\t{column.EntityName} = BitArrayExt.Parse(\"{strValue.ToString()}\")");
							}
							break;
						}

					case NpgsqlDbType.Xml:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<string>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var str = value.Value<string>();
							str = str.Replace("\"", "\\\"");

							results.Append($"\t\t\t\t{column.EntityName} = \"{str}\"");
							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Xml:
						{
							var value = ExampleValue[column.ColumnName];

							{
								if (column.IsNullable)
								{
									if (value == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
									else if (value.Value<JArray>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}

								var builder = new StringBuilder("new string[] {");
								bool first = true;

								foreach (var str in value.Value<JArray>())
								{
									if (first)
										first = false;
									else
										builder.Append(", ");

									var xmlstring = str.Value<string>();
									xmlstring = xmlstring.Replace("\"", "\\\"");

									builder.Append($"\"{xmlstring}\"");
								}

								builder.Append("}");
								results.Append($"\t\t\t\t{column.EntityName} = {builder}");
								return;
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Text:
					case NpgsqlDbType.Array | NpgsqlDbType.Char:
					case NpgsqlDbType.Array | NpgsqlDbType.Name:
					case NpgsqlDbType.Array | NpgsqlDbType.Citext:
					case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<JArray>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new string[] {");
							bool first = true;

							foreach (var str in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								builder.Append($"\"{str.Value<string>()}\"");
							}

							builder.Append("}");
							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Char:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Value<string>() == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (column.Length == 1)
								results.Append($"\t\t\t\t{column.EntityName} = '{value.Value<string>()}'");
							else
								results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
							break;
						}

					case NpgsqlDbType.Text:
					case NpgsqlDbType.Citext:
					case NpgsqlDbType.Varchar:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if ( value == null )
                                {
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.String)
								{
									if (string.IsNullOrWhiteSpace(value.Value<string>()))
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}
							}

							results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
							break;
						}

					case NpgsqlDbType.Name:
						{
							var value = ExampleValue[column.ColumnName];

							if (string.Equals(column.dbDataType, "_name", StringComparison.OrdinalIgnoreCase))
							{
								if (column.IsNullable)
								{
									if (value == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
									else if (value.Value<JArray>() == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}

								var builder = new StringBuilder("new string[] {");
								bool first = true;

								foreach (var str in value.Value<JArray>())
								{
									if (first)
										first = false;
									else
										builder.Append(", ");

									builder.Append($"\"{str.Value<string>()}\"");
								}

								builder.Append("}");
								results.Append($"\t\t\t\t{column.EntityName} = {builder}");
								break;
							}
							else
							{
								if (column.IsNullable)
								{
									if (value == null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
									else if (value.Type == JTokenType.Null)
									{
										results.Append($"\t\t\t\t{column.EntityName} = null");
										return;
									}
								}

								results.Append($"\t\t\t\t{column.EntityName} = \"{value.Value<string>()}\"");
								break;
							}
						}

					case NpgsqlDbType.Date:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
								results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd");
								results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
							}
							else
								results.Append($"\t\t\t\t{column.EntityName} = Unknown cast");

							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Date:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
									builder.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd");
									builder.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							break;
						}

					case NpgsqlDbType.Time:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{x}\")");

							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{x}\")");
							}
							else
								results.Append($"\t\t\t\t{column.EntityName} = Unknown cast");
							break;
						}

					case NpgsqlDbType.Interval:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{x}\")");
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								results.Append($"\t\t\t\t{column.EntityName} = TimeSpan.Parse(\"{x}\")");
							}
							else
								results.Append($"\t\t\t\t{column.EntityName} = Unknown cast");

							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Time:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									builder.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									builder.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									builder.Append("Unknown cast");
							}
							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Interval:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									builder.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									builder.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.Timestamp:
					case NpgsqlDbType.TimestampTz:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								results.Append($"\t\t\t\t{column.EntityName} = DateTime.Parse(\"{x}\")");
							}
							else
								results.Append($"\t\t\t\t{column.EntityName} = Unknown cast");

							break;
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
						{
							var value = ExampleValue[column.ColumnName];

							if (column.IsNullable)
							{
								if (value == null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
								else if (value.Type == JTokenType.Null)
								{
									results.Append($"\t\t\t\t{column.EntityName} = null");
									return;
								}
							}

							var builder = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									builder.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									builder.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									builder.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							builder.Append("}");

							results.Append($"\t\t\t\t{column.EntityName} = {builder}");
							return;
						}

					case NpgsqlDbType.TimeTz:
						EmitPostgresTimeTzValue(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
						EmitPostgresTimeTzArray(column, parentclass, ExampleValue, results, classfiles);
						break;

					case NpgsqlDbType.Unknown:
						EmitPostgresUnknownValue(column, parentclass, ExampleValue, results, classfiles, indents);
						break;

					default:
						if ( parentclass.ElementType == ElementType.Table)	
							results.Append($"\t\t\t\t{column.EntityName} = Unknown");
						else
							results.Append($"\t\t\t\t{column.ColumnName} = Unknown");
						break;
				}
			}
			catch (Exception error)
			{
				throw error;
			}
		}

		private void EmitPostgresDecimalValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<decimal>()}m");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<decimal>()}m");
			}
		}

		private void EmitPostgresDecimalArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new decimal[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<decimal>()}m");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresDoubleValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<double>()}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<double>()}");
			}
		}

		private void EmitPostgresDoubleArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new double[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<double>()}");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresRealValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<float>()}f");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<float>()}f");
			}
		}

		private void EmitPostgresRealArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new float[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<float>()}f");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresLongValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<long>()}L");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<long>()}L");
			}
		}

		private void EmitPostgresLongArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new long[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<long>()}L");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresIntValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<int>()}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<int>()}");
			}
		}

		private void EmitPostgresIntArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new int[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<int>()}");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresShortValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<short>()}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<short>()}");
			}
		}

		private void EmitPostgresShortArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new short[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<short>()}");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresUintValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {value.Value<uint>()}u");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {value.Value<uint>()}u");
			}
		}

		private void EmitPostgresUintArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder();
				builder.Append("new uint[] {");
				bool first = true;
				foreach (var charValue in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					builder.Append($"{charValue.Value<uint>()}u");
				}

				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresBoxValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var pointa = value["UpperRight"];
				var pointb = value["LowerLeft"];

				var x1 = pointa["X"];
				var y1 = pointa["Y"];
				var x2 = pointb["X"];
				var y2 = pointb["Y"];

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlBox(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()},{y2.Value<double>()}))");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlBox(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()},{y2.Value<double>()}))");
			}
		}

		private void EmitPostgresBoxArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var boxlist = new StringBuilder();
				var first = true;

				foreach (var box in value)
				{
					var pointa = box["UpperRight"];
					var pointb = box["LowerLeft"];

					var x1 = pointa["X"];
					var y1 = pointa["Y"];
					var x2 = pointb["X"];
					var y2 = pointb["Y"];

					if (first)
						first = false;
					else
						boxlist.Append(",");

					boxlist.Append($"new NpgsqlBox(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()},{y2.Value<double>()}))");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlBox[] {{{boxlist}}}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlBox[] {{{boxlist}}}");
			}
		}

		private void EmitPostgresLineValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var x = value["A"];
				var y = value["B"];
				var r = value["C"];

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlLine({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlLine({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
			}
		}

		private void EmitPostgresLineArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var lineList = new StringBuilder();
				var first = true;

				foreach (var line in value)
				{
					var x = line["A"];
					var y = line["B"];
					var r = line["C"];

					if (first)
						first = false;
					else
						lineList.Append(",");

					lineList.Append($"new NpgsqlLine({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlLine[] {{{lineList}}}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlLine[] {{{lineList}}}");
			}
		}

		private void EmitPostgresCircleValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var x = value["X"];
				var y = value["Y"];
				var r = value["Radius"];

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlCircle({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlCircle({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
			}
		}

		private void EmitPostgresCircleArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var circleList = new StringBuilder();
				var first = true;

				foreach (var circle in value)
				{
					var x = circle["X"];
					var y = circle["Y"];
					var r = circle["Radius"];

					if (first)
						first = false;
					else
						circleList.Append(",");

					circleList.Append($"new NpgsqlCircle({x.Value<double>()}, {y.Value<double>()}, {r.Value<double>()})");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlCircle[] {{{circleList}}}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlCircle[] {{{circleList}}}");
			}
		}

		private void EmitPostgresPolygonValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var pointlist = new StringBuilder();
				bool first = true;

				foreach (var pointValue in value)
				{
					var x = pointValue["X"];
					var y = pointValue["Y"];

					if (first)
						first = false;
					else
						pointlist.Append(",");

					pointlist.Append($"new NpgsqlPoint({x},{y})");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint[] {{{pointlist}}}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint[] {{{pointlist}}}");
			}
		}

		private void EmitPostgresPolygonArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var polylist = new StringBuilder();
				var firstpoly = true;

				foreach (var poly in value)
				{

					var pointlist = new StringBuilder();
					bool first = true;

					foreach (var pointValue in poly)
					{
						var x = pointValue["X"];
						var y = pointValue["Y"];

						if (first)
							first = false;
						else
							pointlist.Append(",");

						pointlist.Append($"new NpgsqlPoint({x},{y})");
					}

					if (firstpoly)
						firstpoly = false;
					else
						polylist.Append(",");

					polylist.Append($"new NpgsqlPoint[] {{{pointlist}}}");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint[][] {{ {polylist} }}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint[][] {{ {polylist} }}");
			}
		}

		private void EmitPostgresPathValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var pointlist = new StringBuilder();
				bool first = true;

				foreach (var pointValue in value)
				{
					var x = pointValue["X"];
					var y = pointValue["Y"];

					if (first)
						first = false;
					else
						pointlist.Append(",");

					pointlist.Append($"new NpgsqlPoint({x},{y})");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint[] {{{pointlist}}}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint[] {{{pointlist}}}");
			}
		}

		private void EmitPostgresPathArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var pathlist = new StringBuilder();
				var firstpath = true;

				foreach (var path in value)
				{
					var pointlist = new StringBuilder();
					bool first = true;

					foreach (var pointValue in path)
					{
						var x = pointValue["X"];
						var y = pointValue["Y"];

						if (first)
							first = false;
						else
							pointlist.Append(",");

						pointlist.Append($"new NpgsqlPoint({x},{y})");
					}

					if (firstpath)
						firstpath = false;
					else
						pathlist.Append(",");

					pathlist.Append($"new NpgsqlPoint[] {{{pointlist}}}");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint[][] {{ {pathlist} }}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint[][] {{ {pathlist} }}");
			}
		}

		private void EmitPostgresLSegValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var start = value["Start"].Value<JObject>();
				var end = value["End"].Value<JObject>();

				var x1 = start["X"];
				var y1 = start["Y"];

				var x2 = end["X"];
				var y2 = end["Y"];

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlLSeg(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()}, {y2.Value<double>()}))");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlLSeg(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()}, {y2.Value<double>()}))");
			}
		}

		private void EmitPostgresLSegArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var seglist = new StringBuilder();
				var first = true;

				foreach (var lseg in value)
				{
					var start = lseg["Start"].Value<JObject>();
					var end = lseg["End"].Value<JObject>();

					var x1 = start["X"];
					var y1 = start["Y"];

					var x2 = end["X"];
					var y2 = end["Y"];

					if (first)
						first = false;
					else
						seglist.Append(",");

					seglist.Append($"new NpgsqlLSeg(new NpgsqlPoint({x1.Value<double>()}, {y1.Value<double>()}), new NpgsqlPoint({x2.Value<double>()}, {y2.Value<double>()}))");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlLSeg[] {{ {seglist} }}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlLSeg[] {{ {seglist} }}");
			}
		}

		private void EmitPostgresPointValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Object && value.Value<JObject>() == null)))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var x = value["X"];
				var y = value["Y"];

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint({x.Value<double>()}, {y.Value<double>()})");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint({x.Value<double>()}, {y.Value<double>()})");
			}
		}

		private void EmitPostgresPointArrayValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.Array && value.Value<JArray>() == null)))
			{
				if ( parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var pointList = new StringBuilder();
				bool first = true;

				foreach (var point in value)
				{
					var x = point["X"];
					var y = point["Y"];

					if (first)
						first = false;
					else
						pointList.Append(",");

					pointList.Append($"new NpgsqlPoint({x.Value<double>()}, {y.Value<double>()})");
				}

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = new NpgsqlPoint[] {{ {pointList} }}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = new NpgsqlPoint[] {{ {pointList} }}");
			}
		}

		private void EmitPostgresTimeTzValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
		{
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null))
			{
				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else if (value.Type == JTokenType.Date)
			{
				var x = value.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = DateTimeOffset.Parse(\"{x}\")");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = DateTimeOffset.Parse(\"{x}\")");
			}
			else if (value.Type == JTokenType.String)
			{
				var dt = DateTimeOffset.Parse(value.Value<string>());
				var x = dt.ToString("HH':'mm':'ss.fffffffK");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = DateTimeOffset.Parse(\"{x}\")");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = DateTimeOffset.Parse(\"{x}\")");
			}
			else if (parentClass.ElementType == ElementType.Table)
				results.Append($"\t\t\t\t{column.EntityName} = Unknown cast");
			else
				results.Append($"\t\t\t\t{column.ColumnName} = Unknown cast");
		}

		private void EmitPostgresTimeTzArray(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles)
        {
			var value = ExampleValue[column.ColumnName];

			if (column.IsNullable && (value == null || value.Type == JTokenType.Null || (value.Type == JTokenType.String && string.IsNullOrWhiteSpace(value.Value<string>()))))
			{
				if ( parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = null");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = null");
			}
			else
			{
				var builder = new StringBuilder("new DateTimeOffset[] {");
				bool first = true;

				foreach (var dt in value.Value<JArray>())
				{
					if (first)
						first = false;
					else
						builder.Append(", ");

					if (dt.Type == JTokenType.Date)
					{
						var x = dt.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");
						builder.Append($"DateTimeOffset.Parse(\"{x}\")");
					}

					else if (dt.Type == JTokenType.String)
					{
						var dt2 = DateTimeOffset.Parse(dt.Value<string>());
						var x = dt2.ToString("HH':'mm':'ss.fffffffK");
						builder.Append($"DateTimeOffset.Parse(\"{x}\")");
					}
					else
						throw new Exception($"Unrecognized type {value.Type}");
				}
				builder.Append("}");

				if (parentClass.ElementType == ElementType.Table)
					results.Append($"\t\t\t\t{column.EntityName} = {builder}");
				else
					results.Append($"\t\t\t\t{column.ColumnName} = {builder}");
			}
		}

		private void EmitPostgresUnknownValue(DBColumn column, EntityDetailClassFile parentClass, JObject ExampleValue, StringBuilder results, List<EntityDetailClassFile> classfiles, int indents)
        {
			var entityClass = classfiles.FirstOrDefault(c => string.Equals(c.ClassName, column.EntityType, StringComparison.OrdinalIgnoreCase));

			if (entityClass.ElementType == ElementType.Enum)
			{
				var value = ExampleValue[column.ColumnName];

				for (int i = 0; i < indents; i++)
					results.Append("\t");

				if (value.Type == JTokenType.Null)
				{
					results.Append($"\t\t\t\t{column.EntityName} = null");
				}
				else if (value.Type == JTokenType.String)
				{
					var strValue = value.Value<string>();
					var childColumn = entityClass.Columns.FirstOrDefault(c => string.Equals(c.EntityName, strValue, StringComparison.OrdinalIgnoreCase));

					if ( parentClass == null || parentClass.ElementType == ElementType.Table)
						results.Append($"\t\t\t\t{column.EntityName} = {entityClass.ClassName}.{childColumn.ColumnName}");
					else
						results.Append($"\t\t\t\t{column.ColumnName} = {entityClass.ClassName}.{childColumn.ColumnName}");
				}
				else if ( value.Type == JTokenType.Integer)
                {
					var childColumn = entityClass.Columns[value.Value<int>()];

					if (parentClass == null || parentClass.ElementType == ElementType.Table)
						results.Append($"\t\t\t\t{column.EntityName} = {entityClass.ClassName}.{childColumn}");
					else
						results.Append($"\t\t\t\t{column.ColumnName} = {entityClass.ClassName}.{childColumn}");
				}
				else
                {
					if (parentClass == null || parentClass.ElementType == ElementType.Table)
						results.Append($"\t\t\t\t{column.EntityName} = Unknown Enum Cast");
					else
						results.Append($"\t\t\t\t{column.ColumnName} = Unknown Enum Cast");
				}
			}
			else if ( entityClass.ElementType == ElementType.Composite)
            {
				bool first = true;
				var compositeValue = ExampleValue[column.ColumnName].Value<JObject>();

				for (int i = 0; i < indents; i++)
					results.Append("\t");

				if (parentClass == null || parentClass.ElementType == ElementType.Table)
					results.AppendLine($"\t\t\t\t{column.EntityName} = new {column.EntityType}() {{");
				else
					results.AppendLine($"\t\t\t\t{column.ColumnName} = new {column.EntityType}() {{");

					foreach ( var child in entityClass.Columns )
                {
					if (first)
						first = false;
					else
						results.AppendLine(",");

					for (int i = 0; i < indents; i++)
						results.Append("\t");

					EmitPostgresValue(child, entityClass, compositeValue, results, classfiles, indents + 1);
                }

				for (int i = 0; i < indents; i++)
					results.Append("\t");

				results.Append($"\t\t\t\t}}");
			}
		}

		private bool EmitEntiyMemeberSetting(List<DBColumn> Columns, EntityDetailClassFile parentClass, string schema, string connectionString, string solutionFolder, JObject Example, StringBuilder results, bool first, ClassMember member, List<EntityDetailClassFile> classFiles)
		{
			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					first = EmitEntiyMemeberSetting(Columns, parentClass, schema, connectionString, solutionFolder, Example, results, first, childMember, classFiles);
				}
			}
			else
			{
				foreach (var column in member.EntityNames)
				{
					if (first)
						first = false;
					else
						results.AppendLine(",");

					if (column.ServerType == DBServerType.MYSQL)
						GetMySqlValue(column, Example, results);
					else if (column.ServerType == DBServerType.POSTGRESQL)
						EmitPostgresValue(column, parentClass, Example, results, classFiles, 0);
					else if (column.ServerType == DBServerType.SQLSERVER)
						GetSqlServerValue(column, Example, results);
				}
			}

			return first;
		}
		
		public bool UpdateServices(string solutionFolder, string validationClass, string entityNamespace, string resourceNamespace, string validationNamespace)
		{
			var servicesFile = FindServices(solutionFolder);

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
											if (line.ToLower().Contains(resourceNamespace.ToLower()))
											{
												hasDomainNamespace = true;
											}

											if (line.ToLower().Contains(validationNamespace.ToLower()))
											{
												hasValidationNamespace = true;
											}

											if (line.ToLower().Contains(entityNamespace.ToLower()))
											{
												hasEntityNamespace = true;
											}

											if (string.IsNullOrWhiteSpace(line))
											{
												if (!hasDomainNamespace)
												{
													writer.WriteLine($"using {resourceNamespace};");
												}

												if (!hasValidationNamespace)
												{
													writer.WriteLine($"using {validationNamespace};");
												}

												if (!hasEntityNamespace)
												{
													writer.WriteLine($"using {entityNamespace};");
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
											if (line.ToLower().Contains(($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>()").ToLower()))
												validatorRegistered = true;

											state += line.CountOf('{') - line.CountOf('}');

											if (line.Contains("return ApiOptions;"))
												state--;

											if (state == 3)
											{
												if (!validatorRegistered)
												{
													writer.WriteLine($"\t\t\tservices.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
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

		public string FindServices(string folder)
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
		
		private void EmitEndpoint(string resourceClassName, string action, StringBuilder results, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IActionResult> {action}{resourceClassName}Async(");
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

		private string FindEntityModelsFolder(string folder)
		{
			if (string.Equals(Path.GetFileName(folder), "EntityModels", StringComparison.OrdinalIgnoreCase))
				return folder;

			foreach (var childfolder in Directory.GetDirectories(folder))
			{
				var result = FindEntityModelsFolder(childfolder);

				if (!string.IsNullOrWhiteSpace(result))
					return result;
			}

			return string.Empty;
		}


		/// <summary>
		/// Generate undefined elements
		/// </summary>
		/// <param name="composites">The list of elements to be defined"/></param>
		/// <param name="connectionString">The connection string to the database server</param>
		/// <param name="rootnamespace">The root namespace for the newly defined elements</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		/// <param name="definedElements">The lise of elements that are defined</param>
		public void GenerateComposites(List<EntityClassFile> composites, string connectionString, string rootnamespace, Dictionary<string, string> replacementsDictionary, List<EntityClassFile> definedElements)
        {
			foreach ( var composite in composites )
            {
				if (composite.ElementType == ElementType.Enum)
				{
					var result = new StringBuilder();

					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");
					result.AppendLine();
					result.AppendLine($"namespace {rootnamespace}");
					result.AppendLine("{");
					result.Append(EmitEnum(composite.SchemaName, composite.TableName, composite.ClassName, connectionString));
					result.AppendLine("}");

					var destinationFolder = FindEntityModelsFolder(replacementsDictionary["$solutiondirectory$"]);

					var fileName = Path.Combine(destinationFolder, $"{composite.ClassName}.cs");
					File.WriteAllText(fileName, result.ToString());
				}
				else if (composite.ElementType == ElementType.Composite)
				{
					var result = new StringBuilder();
					var allElementsDefined = false;
					string body = string.Empty;

					while (!allElementsDefined)
					{
						var undefinedElements = new List<EntityClassFile>();
						body = EmitComposite(composite.SchemaName, composite.TableName, composite.ClassName, connectionString, replacementsDictionary, definedElements, undefinedElements);

						if (undefinedElements.Count > 0)
						{
							GenerateComposites(undefinedElements, connectionString, rootnamespace, replacementsDictionary, definedElements);
							definedElements.AddRange(undefinedElements);
						}
						else
							allElementsDefined = true;
					}

					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");

					if (replacementsDictionary.ContainsKey("$net$"))
					{
						if (string.Equals(replacementsDictionary["$net$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Net;");
					}

					if (replacementsDictionary.ContainsKey("$barray$"))
					{
						if (string.Equals(replacementsDictionary["$barray$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Collections;");
					}

					if (replacementsDictionary.ContainsKey("$image$"))
					{
						if (string.Equals(replacementsDictionary["$image$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Drawing;");
					}

					if (replacementsDictionary.ContainsKey("$netinfo$"))
					{
						if (string.Equals(replacementsDictionary["$netinfo$"], "true", StringComparison.OrdinalIgnoreCase))
							result.AppendLine("using System.Net.NetworkInformation;");
					}

					result.AppendLine();
					result.AppendLine($"namespace {rootnamespace}");
					result.AppendLine("{");
					result.Append(body);
					result.AppendLine("}");

					var destinationFolder = FindEntityModelsFolder(replacementsDictionary["$solutiondirectory$"]);

					var fileName = Path.Combine(destinationFolder, $"{composite.ClassName}.cs");
					File.WriteAllText(fileName, result.ToString());
				}
			}
		}
	}
}
