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

		public string EmitValidationModel(string entityClassName, string resourceClassName, string validatorClassName)
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

		public string EmitExampleModel(string version, List<ClassMember> classMembers, string entityClassName, string resourceClassName, string exampleClassName, List<DBColumn> Columns, JObject Example, Dictionary<string, string> replacementsDictionary)
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

			foreach (var member in classMembers)
			{
				foreach (var column in member.EntityNames)
				{
					if (first)
						first = false;
					else
						results.AppendLine(",");

					string value = "Unknown";

					if (column.ServerType == DBServerType.MYSQL)
						value = GetMySqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.POSTGRESQL)
						value = GetPostgresqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.SQLSERVER)
						value = GetSqlServerValue(column.ColumnName, Columns, Example);

					if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
						replacementsDictionary["$exampleimage$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Inet)
						replacementsDictionary["$examplenet$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Cidr)
						replacementsDictionary["$examplenet$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr)
						replacementsDictionary["$examplenetinfo$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.MacAddr8)
						replacementsDictionary["$examplenetinfo$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
						replacementsDictionary["$examplebarray$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
						replacementsDictionary["$examplebarray$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bit && column.Length > 1)
						replacementsDictionary["$examplebarray$"] = "true";
					if (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varbit)
						replacementsDictionary["$examplebarray$"] = "true";

					if (string.Equals(column.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
						results.Append($"\t\t\t\t{column.EntityName} = ImageEx.Parse({value})");
					else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String({value})");
					else
						results.Append($"\t\t\t\t{column.EntityName} = {value}");
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

		public string EmitExampleCollectionModel(string version, List<ClassMember> classMembers, string entityClassName, string resourceClassName, string exampleCollectionClassName,  List<DBColumn> Columns, JObject Example, Dictionary<string, string> replacementsDictionary)
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

			foreach (var member in classMembers)
			{
				first = EmitEntiyMemeberSetting(Columns, Example, results, first, member);
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

		public string EmitResourceModel(List<ClassMember> entityClassMembers, string resourceClassName, string entityClassName, DBTable table, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			replacementsDictionary.Add("$resourceimage$", "false");
			replacementsDictionary.Add("$resourcenet$", "false");
			replacementsDictionary.Add("$resourcenetinfo$", "false");
			replacementsDictionary.Add("$resourcebarray$", "false");

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

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetPostgresqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (member.EntityNames[0].ServerType == DBServerType.MYSQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetMySqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (member.EntityNames[0].ServerType == DBServerType.SQLSERVER)
						results.AppendLine($"\t\tpublic {DBHelper.GetSqlServerResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
				}
			}

			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitEntityModel(DBTable table, string entityClassName, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
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
						 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == (NpgsqlDbType.Array | NpgsqlDbType.Varchar)) ||
						 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
				{
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

						if (string.Equals(column.dbDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
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

				//	Correct for reserved words
				CorrectForReservedNames(result, column, ref first);

				result.AppendLine(")]");

				//	Insert the column definition
				if (column.ServerType == DBServerType.POSTGRESQL)
					result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column)} {column.ColumnName} {{ get; set; }}");
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
		private string GetSqlServerValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			switch ((SqlDbType)column.DataType)
			{
				case SqlDbType.Xml:
					if (column.IsNullable)
					{
						if (value.Value<string>() == null)
							return "null";
					}

					return $"\"{value.Value<string>()}\"";

				case SqlDbType.BigInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
								return "null";
						}

						return $"{value.Value<long>()}";
					}

				case SqlDbType.Binary:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.VarBinary:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.Image:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"\"{Convert.ToBase64String(value.Value<byte[]>())}\"";
					}

				case SqlDbType.Timestamp:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
					}

				case SqlDbType.Bit:
					{
						if (column.IsNullable)
						{
							if (value.Value<bool?>() == null)
								return "null";
						}

						if (value.Value<bool>())
							return "true";
						else
							return "false";
					}

				case SqlDbType.Char:
				case SqlDbType.NChar:
					{
						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
								return "null";
						}

						if (column.Length == 1)
							return $"'{value.Value<string>()}'";
						else
							return $"\"{value.Value<string>()}\"";
					}

				case SqlDbType.Date:
					if (column.IsNullable)
					{
						if (value.Value<DateTime?>() == null)
							return "null";
					}

					return $"DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()}\")";

				case SqlDbType.DateTime:
				case SqlDbType.DateTime2:
				case SqlDbType.SmallDateTime:
					if (column.IsNullable)
					{
						if (value.Value<DateTime?>() == null)
							return "null";
					}

					return $"DateTime.Parse(\"{value.Value<DateTime>().ToShortDateString()} {value.Value<DateTime>().ToShortTimeString()}\")";

				case SqlDbType.DateTimeOffset:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTimeOffset?>() == null)
								return "null";
						}

						var dto = value.Value<DateTimeOffset>();
						var x = dto.ToString("MM/dd/yyyy hh:mm:ss zzz");
						return $"DateTime.Parse(\"{x}\")";
					}

				case SqlDbType.Decimal:
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					{
						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
								return "null";
						}

						return $"{value.Value<decimal>()}m";
					}

				case SqlDbType.Float:
					{
						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
								return "null";
						}

						return value.Value<double>().ToString();
					}

				case SqlDbType.Int:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case SqlDbType.NText:
				case SqlDbType.Text:
				case SqlDbType.NVarChar:
				case SqlDbType.VarChar:
					if (column.IsNullable)
					{
						if (value.Value<string>() == null)
							return "null";
					}

					return $"\"{value.Value<string>()}\"";

				case SqlDbType.Real:
					if (column.IsNullable)
					{
						if (value.Value<float?>() == null)
							return "null";
					}

					return $"{value.Value<float>()}f";

				case SqlDbType.SmallInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
								return "null";
						}

						return $"{value.Value<short>()}";
					}

				case SqlDbType.Time:
					{
						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
								return "null";
						}

						return $"TimeSpan.Parse(\"{value.Value<TimeSpan>()}\")";
					}


				case SqlDbType.TinyInt:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
								return "null";
						}

						return $"{value.Value<byte>()}";
					}

				case SqlDbType.UniqueIdentifier:
					if (column.IsNullable)
					{
						if (value.Value<Guid?>() == null)
							return "null";
					}

					return $"Guid.Parse(\"{value.Value<Guid>().ToString()}\")";
			}

			return "unknown";
		}
		private string GetMySqlValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			switch ((MySqlDbType)column.DataType)
			{
				case MySqlDbType.Byte:
					{
						if (column.IsNullable)
						{
							if (value.Value<sbyte?>() == null)
								return "null";
						}

						return $"{value.Value<sbyte>()}";
					}

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
				case MySqlDbType.TinyBlob:
				case MySqlDbType.Blob:
				case MySqlDbType.MediumBlob:
				case MySqlDbType.LongBlob:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte[]>() == null)
								return "null";
						}

						var str = Convert.ToBase64String(value.Value<byte[]>());
						return $"Convert.FromBase64String(\"{str}\")";
					}

				case MySqlDbType.Enum:
				case MySqlDbType.Set:
					{
						if (column.IsNullable)
						{
							if (value.Value<string>() == null)
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.UByte:
					{
						if (column.IsNullable)
						{
							if (value.Value<byte?>() == null)
								return "null";
						}

						return $"{value.Value<byte>()}";
					}

				case MySqlDbType.Int16:
					{
						if (column.IsNullable)
						{
							if (value.Value<short?>() == null)
								return "null";
						}

						return $"{value.Value<short>()}";
					}

				case MySqlDbType.UInt16:
					{
						if (column.IsNullable)
						{
							if (value.Value<ushort?>() == null)
								return "null";
						}

						return $"{value.Value<ushort>()}";
					}

				case MySqlDbType.Int24:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.UInt24:
					{
						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
								return "null";
						}

						return $"{value.Value<uint>()}";
					}

				case MySqlDbType.Int32:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.UInt32:
					{
						if (column.IsNullable)
						{
							if (value.Value<uint?>() == null)
								return "null";
						}

						return $"{value.Value<uint>()}";
					}

				case MySqlDbType.Int64:
					{
						if (column.IsNullable)
						{
							if (value.Value<long?>() == null)
								return "null";
						}

						return $"{value.Value<long>()}";
					}

				case MySqlDbType.UInt64:
					{
						if (column.IsNullable)
						{
							if (value.Value<ulong?>() == null)
								return "null";
						}

						return $"{value.Value<ulong>()}";
					}

				case MySqlDbType.Decimal:
					{
						if (column.IsNullable)
						{
							if (value.Value<decimal?>() == null)
								return "null";
						}

						return $"{value.Value<decimal>()}m";
					}

				case MySqlDbType.Double:
					{
						if (column.IsNullable)
						{
							if (value.Value<double?>() == null)
								return "null";
						}

						return $"{value.Value<double>()}";
					}

				case MySqlDbType.Float:
					{
						if (column.IsNullable)
						{
							if (value.Value<float?>() == null)
								return "null";
						}

						return $"{value.Value<float>()}f";
					}

				case MySqlDbType.String:
					if (column.Length == 1)
					{
						if (column.IsNullable)
						{
							if (value.Value<char?>() == null)
								return "null";
						}

						return $"'{value.Value<char>()}'";
					}
					else
					{
						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.Text:
				case MySqlDbType.TinyText:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
					{
						if (column.IsNullable)
						{
							if (string.IsNullOrWhiteSpace(value.Value<string>()))
								return "null";
						}

						return $"\"{value.Value<string>()}\"";
					}

				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
								return "null";
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
						return $"DateTime.Parse(\"{x}\")";

					}

				case MySqlDbType.Date:
					{
						if (column.IsNullable)
						{
							if (value.Value<DateTime?>() == null)
								return "null";
						}

						var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
						return $"DateTime.Parse(\"{x}\")";

					}

				case MySqlDbType.Time:
					{
						if (column.IsNullable)
						{
							if (value.Value<TimeSpan?>() == null)
								return "null";
						}

						var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
						return $"TimeSpan.Parse(\"{x}\")";
					}

				case MySqlDbType.Year:
					{
						if (column.IsNullable)
						{
							if (value.Value<int?>() == null)
								return "null";
						}

						return $"{value.Value<int>()}";
					}

				case MySqlDbType.Bit:
					{
						if (string.Equals(column.dbDataType, "bit(1)", StringComparison.OrdinalIgnoreCase))
						{
							if (column.IsNullable)
							{
								if (value.Value<bool?>() == null)
									return "null";
							}

							return $"{value.Value<bool>().ToString().ToLower()}";
						}
						else
						{
							if (column.IsNullable)
							{
								if (value.Value<ulong?>() == null)
									return "null";
							}

							return $"{value.Value<ulong>()}";
						}
					}

			}

			return "unknown";
		}
		private string GetPostgresqlValue(string columnName, List<DBColumn> Columns, JObject ExampleValue)
		{
			var column = Columns.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
			var value = ExampleValue[columnName];

			try
			{
				switch ((NpgsqlDbType)column.DataType)
				{
					case NpgsqlDbType.Smallint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<short>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new short[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<int>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Integer:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<int>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new int[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<int>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Bigint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<long>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new long[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<long>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Real:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<float>()}f";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Real:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new float[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<float>()}f");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Double:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<double>()}";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Double:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new double[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<double>()}");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"{value.Value<decimal>()}m";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
					case NpgsqlDbType.Array | NpgsqlDbType.Money:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new decimal[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"{charValue.Value<decimal>()}m");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Uuid:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"Guid.Parse(\"{value.Value<Guid>()}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new Guid[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"Guid.Parse(\"{charValue.Value<Guid>()}\")");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Json:
					case NpgsqlDbType.Jsonb:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var str = value.Value<string>();
							str = str.Replace("\"", "\\\"");

							return $"\"{str}\"";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Json:
					case NpgsqlDbType.Array | NpgsqlDbType.Jsonb:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder();
							result.Append("new string[] {");
							bool first = true;
							foreach (var charValue in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								var str = charValue.Value<string>();
								str = str.Replace("\"", "\\\"");

								result.Append($"\"{str}\"");
							}

							result.Append("}");
							return result.ToString();
						}

					case NpgsqlDbType.Varbit:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
								else if (value.Type == JTokenType.Array && value.Value<JArray>() == null)
									return "null";
								else if (value.Type == JTokenType.String && value.Value<string>() == null)
									return "null";
							}

							if (value.Type == JTokenType.String)
							{
								if (string.IsNullOrWhiteSpace(value.Value<string>()))
									return "null";

								return $"BitArrayExt.Parse(\"{value.Value<string>()}\")";
							}
							else
							{
								var strVal = new StringBuilder();
								foreach (bool bVal in value.Value<JArray>())
								{
									strVal.Append(bVal ? "1" : "0");
								}

								return $"BitArrayExt.Parse(\"{value.Value<string>()}\")";
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
								else if (value.Value<JArray>() == null)
									return "null";
							}

							var array = value.Value<JArray>();

							if (array.Count == 0)
								return "null";

							var childValue = array[0];

							if (childValue.Type == JTokenType.String)
							{
								var result = new StringBuilder("new BitArray[] {");
								bool first = true;

								foreach (string strValue in array)
								{
									if (first)
										first = false;
									else
										result.Append(",");

									result.Append($"BitArrayExt.Parse(\"{value.Value<string>()}\")");
								}

								result.Append("}");
								return result.ToString();
							}
							else
							{
								var result = new StringBuilder("new BitArray[] {");
								bool first = true;

								foreach (JArray avalue in array)
								{
									if (first)
										first = false;
									else
										result.Append(",");

									var sValue = new StringBuilder();
									foreach (bool bVal in avalue)
									{
										sValue.Append(bVal ? "1" : "0");
									}

									result.Append($"BitArrayExt.Parse(\"{sValue}\")");
								}

								result.Append("}");
								return result.ToString();
							}
						}

					case NpgsqlDbType.Bit:
						{
							if (column.IsNullable)
							{
								if (column.Length == 1)
								{
									if (value.Value<bool?>() == null)
										return "null";
								}
								else if (value.Type == JTokenType.String && value.Value<string>() == null)
								{
									return "null";
								}
								else if (value.Type == JTokenType.Array && value.Value<JArray>() == null)
								{
									return "null";
								}
							}

							if (column.Length == 1)
							{
								return value.Value<bool>().ToString().ToLower();
							}
							else
							{
								return $"BitArrayExt.Parse(\"{value.Value<string>()}\")";
							}
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bit:
						{
							if (value.Type == JTokenType.Null)
							{
								return null;
							}

							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
								else if (value.Type == JTokenType.String)
								{
									if (string.IsNullOrWhiteSpace(value.Value<string>()))
										return null;
								}
								else if (value.Value<JArray>() == null)
									return "null";
							}

							if (value.Type == JTokenType.String)
							{
								return $"BitArrayExt.Parse(\"{value.Value<string>()}\")";
							}
							else if (value.Type == JTokenType.Array)
							{
								var array = value.Value<JArray>();

								if (array.Count == 0)
									return null;

								var childElement = array[0];

								if (childElement.Type == JTokenType.Boolean)
								{
									var sresult = new StringBuilder();
									foreach (bool bVal in array)
									{
										sresult.Append(bVal ? "1" : "0");
									}

									return $"BitArrayExt.Parse(\"{sresult.ToString()}\")";
								}
								else
								{
									var result = new StringBuilder();
									var answer = value.Value<JArray>();

									result.Append("new BitArray[] {");
									bool firstGroup = true;

									foreach (var group in answer)
									{
										if (firstGroup)
											firstGroup = false;
										else
											result.Append(", ");

										if (group.Type == JTokenType.String)
										{
											result.Append($"BitArrayExt.Parse(\"{group.Value<string>()}\")");
										}
										else
										{
											var strValue = new StringBuilder();

											foreach (bool bVal in group)
											{
												strValue.Append(bVal ? "1" : "0");
											}

											result.Append($"BitArrayExt.Parse(\"{strValue.ToString()}\")");
										}
									}

									result.Append("}");
									return result.ToString();
								}
							}
							else
								return "Unknown";
						}

					case NpgsqlDbType.Bytea:
						{
							if (column.IsNullable)
							{
								if (value.Value<byte[]>() == null)
									return "null";
							}

							return $"Convert.FromBase64String(\"{Convert.ToBase64String(value.Value<byte[]>())}\")";
						}

					case NpgsqlDbType.Inet:
						{
							if (column.IsNullable)
							{
								if (string.IsNullOrWhiteSpace(value.Value<string>()))
									return "null";
							}

							return $"IPAddress.Parse(\"{value.Value<string>()}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Inet:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new IPAddress[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"IPAddress.Parse(\"{group.Value<string>()}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Cidr:
						{
							if (column.IsNullable)
							{
								if (string.IsNullOrWhiteSpace(value.Value<string>()))
									return "null";
							}

							return $"IPEndPointExt.Parse(\"{value.Value<string>()}\")";
						}


					case NpgsqlDbType.Array | NpgsqlDbType.Cidr:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new IPEndPoint[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"IPEndPointExt.Parse(\"{group.Value<string>()}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.MacAddr:
						{
							if (column.IsNullable)
							{
								if (string.IsNullOrWhiteSpace(value.Value<string>()))
									return "null";
							}

							return $"PhysicalAddress.Parse(\"{value.Value<string>()}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.MacAddr:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new string[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"PhysicalAddress.Parse(\"{group.Value<string>()}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.MacAddr8:
						{
							if (column.IsNullable)
							{
								if (string.IsNullOrWhiteSpace(value.Value<string>()))
									return "null";
							}

							return $"PhysicalAddress.Parse(\"{value.Value<string>()}\")";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.MacAddr8:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new string[] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"PhysicalAddress.Parse(\"{group.Value<string>()}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var result = new StringBuilder("new byte[][] {");
							var array = value.Value<JArray>();

							bool first = true;

							foreach (var group in array)
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								result.Append($"Convert.FromBase64String(\"{Convert.ToBase64String(group.Value<byte[]>())}\")");
							}

							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Boolean:
						{
							if (column.IsNullable)
							{
								if (value.Value<bool?>() == null)
									return "null";
							}

							return value.Value<bool>() ? "true" : "false";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.String)
								return $"BitArrayExt.Parse(\"{value.Value<string>()}\")";
							else
							{
								var strValue = new StringBuilder();

								foreach (bool bVal in value.Value<JArray>())
								{
									strValue.Append(bVal ? "1" : "0");
								}
								return $"BitArrayExt.Parse(\"{strValue.ToString()}\")";
							}
						}

					case NpgsqlDbType.Xml:
						{
							if (column.IsNullable)
							{
								if (value.Value<string>() == null)
									return "null";
							}

							return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Text:
					case NpgsqlDbType.Array | NpgsqlDbType.Char:
					case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
						{
							if (column.IsNullable)
							{
								if (value.Value<JArray>() == null)
									return "null";
							}

							var answer = new StringBuilder("new string[] {");
							bool first = true;

							foreach (var str in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									answer.Append(", ");

								answer.Append($"\"{str.Value<string>()}\"");
							}

							answer.Append("}");
							return answer.ToString();
						}

					case NpgsqlDbType.Char:
						{
							if (column.IsNullable)
							{
								if (value.Value<string>() == null)
									return "null";
							}

							if (column.Length == 1)
								return $"'{value.Value<string>()}'";
							else
								return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Text:
					case NpgsqlDbType.Varchar:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							return $"\"{value.Value<string>()}\"";
						}

					case NpgsqlDbType.Date:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
								return $"DateTime.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd");
								return $"DateTime.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Date:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Time:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Interval:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.TimeSpan)
							{
								var x = value.Value<TimeSpan>().ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = TimeSpan.Parse(value.Value<string>());
								var x = dt.ToString("hh':'mm':'ss");
								return $"TimeSpan.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Time:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Interval:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new TimeSpan[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.TimeSpan)
								{
									var x = dt.Value<TimeSpan>().ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = TimeSpan.Parse(dt.Value<string>());
									var x = dt2.ToString("hh':'mm':'ss");
									result.Append($"TimeSpan.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.Timestamp:
					case NpgsqlDbType.TimestampTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								return $"DateTime.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTime.Parse(value.Value<string>());
								var x = dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
								return $"DateTime.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTime[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTime.Parse(dt.Value<string>());
									var x = dt2.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
									result.Append($"DateTime.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}

					case NpgsqlDbType.TimeTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							if (value.Type == JTokenType.Date)
							{
								var x = value.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");
								return $"DateTimeOffset.Parse(\"{x}\")";
							}
							else if (value.Type == JTokenType.String)
							{
								var dt = DateTimeOffset.Parse(value.Value<string>());
								var x = dt.ToString("HH':'mm':'ss.fffffffK");
								return $"DateTimeOffset.Parse(\"{x}\")";
							}
							else
								throw new Exception($"Unrecognized type {value.Type}");
						}

					case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
						{
							if (column.IsNullable)
							{
								if (value.Type == JTokenType.Null)
									return "null";
							}

							var result = new StringBuilder("new DateTimeOffset[] {");
							bool first = true;

							foreach (var dt in value.Value<JArray>())
							{
								if (first)
									first = false;
								else
									result.Append(", ");

								if (dt.Type == JTokenType.Date)
								{
									var x = dt.Value<DateTimeOffset>().ToString("HH':'mm':'ss.fffffffK");
									result.Append($"DateTimeOffset.Parse(\"{x}\")");
								}

								else if (dt.Type == JTokenType.String)
								{
									var dt2 = DateTimeOffset.Parse(dt.Value<string>());
									var x = dt2.ToString("HH':'mm':'ss.fffffffK");
									result.Append($"DateTimeOffset.Parse(\"{x}\")");
								}
								else
									throw new Exception($"Unrecognized type {value.Type}");
							}
							result.Append("}");

							return result.ToString();
						}
				}

				return "unknown";
			}
			catch (Exception error)
			{
				throw error;
			}
		}
		private bool EmitEntiyMemeberSetting(List<DBColumn> Columns, JObject Example, StringBuilder results, bool first, ClassMember member)
		{
			if (member.ChildMembers.Count > 0)
			{
				foreach (var childMember in member.ChildMembers)
				{
					first = EmitEntiyMemeberSetting(Columns, Example, results, first, childMember);
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

					string value = "Unknown";

					if (column.ServerType == DBServerType.MYSQL)
						value = GetMySqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.POSTGRESQL)
						value = GetPostgresqlValue(column.ColumnName, Columns, Example);
					else if (column.ServerType == DBServerType.SQLSERVER)
						value = GetSqlServerValue(column.ColumnName, Columns, Example);

					if (string.Equals(column.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
						results.Append($"\t\t\t\t{column.EntityName} = ImageEx.Parse({value})");
					else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
						results.Append($"\t\t\t\t{column.EntityName} = Convert.FromBase64String({value})");
					else
						results.Append($"\t\t\t\t{column.EntityName} = {value}");
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
	}
}
