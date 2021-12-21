using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Constants = EnvDTE.Constants;

namespace COFRS.Template
{
    public class Emitter
	{
		/// <summary>
		/// Emits the code for a standard controller.
		/// </summary>
		/// <param name="entityClass">The <see cref="EntityClassFile"/> associated with the controller.</param>
		/// <param name="resourceClass">The <see cref="ResourceClassFile"/> associated with the controller.</param>
		/// <param name="moniker">The company monier used in various headers</param>
		/// <param name="controllerClassName">The class name for the controller</param>
		/// <param name="ValidatorInterface">The validiator interface used for validations</param>
		/// <param name="policy">The authentication policy used by the controller</param>
		/// <returns></returns>
		public string EmitController(ResourceClass resourceClass, string moniker, string controllerClassName, string ValidatorInterface, string policy, string ValidationNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            StringBuilder results = new StringBuilder();
            var nn = new NameNormalizer(resourceClass.ClassName);
            var pkcolumns = resourceClass.Entity.Columns.Where(c => c.IsPrimaryKey);

            BuildControllerInterface(resourceClass.ClassName, resourceClass.Namespace);
            BuildControllerOrchestration(resourceClass.ClassName, resourceClass.Namespace, ValidatorInterface, ValidationNamespace);

            // --------------------------------------------------------------------------------
            //	Class
            // --------------------------------------------------------------------------------

            results.AppendLine("\t///\t<summary>");
            results.AppendLine($"\t///\t{resourceClass.ClassName} Controller");
            results.AppendLine("\t///\t</summary>");
            results.AppendLine("\t[ApiVersion(\"1.0\")]");
            results.AppendLine($"\tpublic class {controllerClassName} : COFRSController");
            results.AppendLine("\t{");
            results.AppendLine("\t\t///\t<value>A generic interface for logging where the category name is derrived from");
            results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name.</value>");
            results.AppendLine($"\t\tprivate readonly ILogger<{controllerClassName}> Logger;");
            results.AppendLine();
            results.AppendLine("\t\t///\t<value>The validator used to validate any requested actions.</value>");
            results.AppendLine($"\t\tprotected readonly {ValidatorInterface} Validator;");
            results.AppendLine();
            results.AppendLine("\t\t///\t<value>The interface to the orchestration layer.</value>");
            results.AppendLine($"\t\tprotected readonly IServiceOrchestrator Orchestrator;");
            results.AppendLine();

            // --------------------------------------------------------------------------------
            //	Constructor
            // --------------------------------------------------------------------------------

            results.AppendLine("\t\t///\t<summary>");
            results.AppendLine($"\t\t///\tInstantiates a {controllerClassName}");
            results.AppendLine("\t\t///\t</summary>");
            results.AppendLine("\t\t///\t<param name=\"logger\">A generic interface for logging where the category name is derrived from");
            results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name. The logger is activated from dependency injection.</param>");
            results.AppendLine("\t\t///\t<param name=\"orchestrator\">The <see cref=\"IServiceOrchestrator\"/> interface for the Orchestration layer. The orchestrator is activated from dependency injection.</param>");
            results.AppendLine($"\t\tpublic {controllerClassName}(ILogger<{controllerClassName}> logger, IServiceOrchestrator orchestrator)");
            results.AppendLine("\t\t{");
            results.AppendLine("\t\t\tLogger = logger;");
            results.AppendLine("\t\t\tOrchestrator = orchestrator;");
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

            results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedCollection<{resourceClass.ClassName}>))]");
            results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}CollectionExample))]");

            results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine("\t\t[SupportRQL]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Get{nn.PluralForm}Async()");
            results.AppendLine("\t\t{");
            results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.QueryString.Value);");
            results.AppendLine();
            results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
            results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
            results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method} {Request.Path}\");");
            results.AppendLine();

            results.AppendLine($"\t\t\tvar collection = await Orchestrator.Get{resourceClass.ClassName}CollectionAsync(Request.QueryString.Value, node, User);");
            results.AppendLine($"\t\t\treturn Ok(collection);");
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
                EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine("\t\t///\t<remarks>This call supports RQL. Use the RQL select clause to limit the members returned.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"200\">Returns the specified {nn.SingleForm}.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine("\t\t[HttpGet]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");
                results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");

                results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
                results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                results.AppendLine("\t\t[SupportRQL]");

                EmitEndpoint(resourceClass.Entity.ServerType, resourceClass.ClassName, "Get", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\")");
                results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");
                results.AppendLine();

                results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
                results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
                results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method} {Request.Path}\");");
                results.AppendLine($"\t\t\tvar item = await Orchestrator.Get{resourceClass.ClassName}Async(node, User);");
                results.AppendLine();
                results.AppendLine("\t\t\tif (item == null)");
                results.AppendLine("\t\t\t\treturn NotFound();");
                results.AppendLine();
                results.AppendLine("\t\t\treturn Ok(item);");

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
            results.AppendLine($"\t\t///\t<response code=\"201\">Created - returned when the new {nn.SingleForm} was successfully added to the datastore.</response>");
            results.AppendLine("\t\t[HttpPost]");
            results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
            results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

            if (!string.IsNullOrWhiteSpace(policy))
                results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

            results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({resourceClass.ClassName}))]");
            results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
            results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.Created, typeof({resourceClass.ClassName}Example))]");
            results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Add{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
            results.AppendLine("\t\t{");

            results.AppendLine("\t\t\tLogContext.PushProperty(\"Item\", JsonSerializer.Serialize(item));");
            results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
            results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method} {Request.Path}\");");
            results.AppendLine();
            results.AppendLine($"\t\t\titem = await Orchestrator.Add{resourceClass.ClassName}Async(item, User);");
            results.AppendLine($"\t\t\treturn Created(item.HRef.AbsoluteUri, item);");

            results.AppendLine("\t\t}");
            results.AppendLine();

            // --------------------------------------------------------------------------------
            //	PUT Endpoint
            // --------------------------------------------------------------------------------
            results.AppendLine("\t\t///\t<summary>");
            results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm}");
            results.AppendLine("\t\t///\t</summary>");
            results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
            results.AppendLine($"\t\t///\t<response code=\"200\">OK - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
            results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
            results.AppendLine("\t\t[HttpPut]");
            results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
            results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

            if (!string.IsNullOrWhiteSpace(policy))
                results.AppendLine($"\t\t[Authorize(\"{policy}\")]");


            results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");
            results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
            results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");
            results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine("\t\t[SupportRQL]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Update{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
            results.AppendLine("\t\t{");

            results.AppendLine("\t\t\tLogContext.PushProperty(\"Item\", JsonSerializer.Serialize(item));");
            results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
            results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method}	{Request.Path}\");");
            results.AppendLine();

            results.AppendLine($"\t\t\titem = await Orchestrator.Update{resourceClass.ClassName}Async(item, User);");
            results.AppendLine($"\t\t\treturn Ok(item);");

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
                EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine("\t\t///\t<param name=\"commands\">The patch commands to perform.</param>");
                results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine("\t\t[HttpPatch]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                results.AppendLine($"\t\t[SwaggerRequestExample(typeof(IEnumerable<PatchCommand>), typeof({resourceClass.ClassName}PatchExample))]");
                results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                EmitEndpoint(resourceClass.Entity.ServerType, resourceClass.ClassName, "Patch", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");
                results.AppendLine();
                results.AppendLine("\t\t\tLogContext.PushProperty(\"Commands\", JsonSerializer.Serialize(commands));");
                results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
                results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method}	{Request.Path}\");");
                results.AppendLine();

                results.AppendLine($"\t\t\tawait Orchestrator.Patch{resourceClass.ClassName}Async(commands, node, User);");
                results.AppendLine($"\t\t\treturn NoContent();");
                results.AppendLine("\t\t}");
                results.AppendLine();

                // --------------------------------------------------------------------------------
                //	DELETE Endpoint
                // --------------------------------------------------------------------------------

                results.AppendLine("\t\t///\t<summary>");
                results.AppendLine($"\t\t///\tDelete a {nn.SingleForm}");
                results.AppendLine("\t\t///\t</summary>");
                EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine($"\t\t///\t<remarks>Deletes a {nn.SingleForm} in the datastore.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully deleted from the datastore.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine("\t\t[HttpDelete]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                EmitEndpoint(resourceClass.Entity.ServerType, resourceClass.ClassName, "Delete", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");
                results.AppendLine();
                results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
                results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
                results.AppendLine("\t\t\tLogger.LogInformation($\"{Request.Method}	{Request.Path}\");");
                results.AppendLine();

                results.AppendLine($"\t\t\tawait Orchestrator.Delete{resourceClass.ClassName}Async(node, User);");
                results.AppendLine($"\t\t\treturn NoContent();");

                results.AppendLine("\t\t}");
                results.AppendLine("\t}");
            }

            return results.ToString();
        }

        private static void BuildControllerInterface(string resourceClassName, string resourceNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            ProjectItem orchInterface = mDte.Solution.FindProjectItem("IServiceOrchestrator.cs");
            orchInterface.Open(Constants.vsViewKindCode);
            FileCodeModel2 fileCodeModel = (FileCodeModel2)orchInterface.FileCodeModel;

            //  Ensure that the interface contains all the required imports
            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Threading.Tasks")) == null)
                fileCodeModel.AddImport("System.Threading.Tasks");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Security.Claims")) == null)
                fileCodeModel.AddImport("System.Security.Claims");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Collections.Generic")) == null)
                fileCodeModel.AddImport("System.Collections.Generic");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(resourceNamespace)) == null)
                fileCodeModel.AddImport(resourceNamespace);

            //  Ensure all the required functions are present
            foreach (CodeNamespace orchestrationNamespace in fileCodeModel.CodeElements.OfType<CodeNamespace>())
            {
                CodeInterface2 orchestrationInterface = orchestrationNamespace.Children
                                                                              .OfType<CodeInterface2>()
                                                                              .FirstOrDefault(c => c.Name.Equals("IServiceOrchestrator"));

                if (orchestrationInterface != null)
                {
                    if (orchestrationInterface.Name.Equals("IServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
                    {
                       // List<DBColumn> primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();

                        string deleteFunctionName = $"Delete{resourceClassName}Async";
                        string patchFunctionName = $"Patch{resourceClassName}Async";
                        string updateFunctionName = $"Update{resourceClassName}Async";
                        string addFunctionName = $"Add{resourceClassName}Async";
                        string getSingleFunctionName = $"Get{resourceClassName}Async";
                        string collectionFunctionName = $"Get{resourceClassName}CollectionAsync";

                        string article = resourceClassName.ToLower().StartsWith("a") ||
                                         resourceClassName.ToLower().StartsWith("e") ||
                                         resourceClassName.ToLower().StartsWith("i") ||
                                         resourceClassName.ToLower().StartsWith("o") ||
                                         resourceClassName.ToLower().StartsWith("u") ? "an" : "a";

                        try
                        {
                            #region Get Single Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(getSingleFunctionName)) == null)
                            {
                                var theGetSingleFunction = (CodeFunction2)orchestrationInterface.AddFunction(getSingleFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task<{resourceClassName}>",
                                                          -1);

                                theGetSingleFunction.AddParameter("node", "RqlNode", -1);
                                theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously gets {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>");
                                doc.AppendLine($"<returns>The specified {resourceClassName} resource.</returns>");
                                doc.AppendLine("</doc>");

                                theGetSingleFunction.DocComment = doc.ToString();
                            }
                            #endregion

                            #region Get Collection Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(collectionFunctionName)) == null)
                            {
                                var theCollectionFunction = (CodeFunction2)orchestrationInterface.AddFunction(collectionFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task<PagedCollection<{resourceClassName}>>",
                                                          -1);

                                theCollectionFunction.AddParameter("originalQuery", "string", -1);
                                theCollectionFunction.AddParameter("node", "RqlNode", -1);
                                theCollectionFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously gets a collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine("<param name=\"originalQuery\">The original query string.</param>");
                                doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection.</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                                doc.AppendLine($"<returns>The collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.</returns>");
                                doc.AppendLine("</doc>");

                                theCollectionFunction.DocComment = doc.ToString();
                            }
                            #endregion

                            #region Add Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(addFunctionName)) == null)
                            {
                                var theAddFunction = (CodeFunction2)orchestrationInterface.AddFunction(addFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task<{resourceClassName}>",
                                                          -1);

                                theAddFunction.AddParameter("item", resourceClassName, -1);
                                theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously adds {article} {resourceClassName} resource.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to add.</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                                doc.AppendLine("<returns>The newly added resource.</returns>");
                                doc.AppendLine("</doc>");

                                theAddFunction.DocComment = doc.ToString();
                            }
                            #endregion

                            #region Update Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(updateFunctionName)) == null)
                            {
                                var theUpdateFunction = (CodeFunction2)orchestrationInterface.AddFunction(updateFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task<{resourceClassName}>",
                                                          -1);

                                theUpdateFunction.AddParameter("item", resourceClassName, -1);
                                theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to update.</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                                doc.AppendLine("<returns>The updated item.</returns>");
                                doc.AppendLine("</doc>");

                                theUpdateFunction.DocComment = doc.ToString();
                            }
                            #endregion

                            #region Patch Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(patchFunctionName)) == null)
                            {
                                var thePatchFunction = (CodeFunction2)orchestrationInterface.AddFunction(patchFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task",
                                                          -1);

                                thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
                                thePatchFunction.AddParameter("node", "RqlNode", -1);
                                thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource using patch commands.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine("<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>");
                                doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                                doc.AppendLine("</doc>");

                                thePatchFunction.DocComment = doc.ToString();
                            }
                            #endregion

                            #region Delete Function
                            if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(deleteFunctionName)) == null)
                            {
                                var theDeleteFunction = (CodeFunction2)orchestrationInterface.AddFunction(deleteFunctionName,
                                                          vsCMFunction.vsCMFunctionFunction,
                                                          $"Task", -1);

                                theDeleteFunction.AddParameter("node", "RqlNode", -1);
                                theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                StringBuilder doc = new StringBuilder();
                                doc.AppendLine("<doc>");
                                doc.AppendLine("<summary>");
                                doc.AppendLine($"Asynchronously deletes {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
                                doc.AppendLine("</summary>");
                                doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection of resources to delete.</param>");
                                doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                                doc.AppendLine("</doc>");

                                theDeleteFunction.DocComment = doc.ToString();
                            }
                            #endregion
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private static void BuildControllerOrchestration(string resourceClassName, string resourceNamespace, string ValidatorInterface, string ValidationNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            ProjectItem orchCode = mDte.Solution.FindProjectItem("ServiceOrchestrator.cs");
            orchCode.Open(Constants.vsViewKindCode);
            var fileCodeModel = (FileCodeModel2)orchCode.FileCodeModel;

            //  Ensure that the interface contains all the required imports
            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Threading.Tasks")) == null)
                fileCodeModel.AddImport("System.Threading.Tasks");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Security.Claims")) == null)
                fileCodeModel.AddImport("System.Security.Claims");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Collections.Generic")) == null)
                fileCodeModel.AddImport("System.Collections.Generic");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Text.Json")) == null)
                fileCodeModel.AddImport("System.Text.Json");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Microsoft.Extensions.Logging")) == null)
                fileCodeModel.AddImport("Microsoft.Extensions.Logging");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Serilog.Context")) == null)
                fileCodeModel.AddImport("Serilog.Context");

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(resourceNamespace)) == null)
                fileCodeModel.AddImport(resourceNamespace);

            if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(ValidationNamespace)) == null)
                fileCodeModel.AddImport(ValidationNamespace);

            //  Add all the functions
            foreach (CodeNamespace orchestrattorNamespace in fileCodeModel.CodeElements.OfType<CodeNamespace>())
            {
                CodeClass2 orchestratorClass = orchestrattorNamespace.Children
                                                                     .OfType<CodeClass2>()
                                                                     .FirstOrDefault(c => c.Name.Equals("ServiceOrchestrator"));

                if (orchestratorClass != null)
                {
                    //var primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();

                    var deleteFunctionName = $"Delete{resourceClassName}Async";
                    var patchFunctionName = $"Patch{resourceClassName}Async";
                    var updateFunctionName = $"Update{resourceClassName}Async";
                    var addFunctionName = $"Add{resourceClassName}Async";
                    var getSingleFunctionName = $"Get{resourceClassName}Async";
                    var collectionFunctionName = $"Get{resourceClassName}CollectionAsync";

                    var article = resourceClassName.ToLower().StartsWith("a") ||
                                  resourceClassName.ToLower().StartsWith("e") ||
                                  resourceClassName.ToLower().StartsWith("i") ||
                                  resourceClassName.ToLower().StartsWith("o") ||
                                  resourceClassName.ToLower().StartsWith("u") ? "an" : "a";

                    var parameterName = ValidatorInterface.Substring(1, 1).ToLower() + ValidatorInterface.Substring(2);
                    var validatorInterfaceMemberName = ValidatorInterface.Substring(1, 1).ToUpper() + ValidatorInterface.Substring(2);

                    try
                    {
                        #region Constructor
                        CodeFunction2 constructorFunction = orchestratorClass.Children.OfType<CodeFunction2>()
                            .FirstOrDefault(c => c.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

                        if (constructorFunction == null)
                        {
                            constructorFunction = (CodeFunction2)orchestratorClass.AddFunction(orchestratorClass.Name,
                               vsCMFunction.vsCMFunctionConstructor,
                               $"",
                               -1,
                               vsCMAccess.vsCMAccessPublic);
                        }

                        //  Does the varialble already exist in the class?
                        if (orchestratorClass.Children.OfType<CodeVariable2>().FirstOrDefault(c =>
                           {
                               ThreadHelper.ThrowIfNotOnUIThread();
                               var parts = c.Type.AsString.Split('.');
                               return parts[parts.Length - 1].Equals(ValidatorInterface);

                           }) != null)
                        {
                            validatorInterfaceMemberName = orchestratorClass.Children.OfType<CodeVariable2>().FirstOrDefault(c =>
                            {
                                ThreadHelper.ThrowIfNotOnUIThread();
                                var parts = c.Type.AsString.Split('.');
                                return parts[parts.Length - 1].Equals(ValidatorInterface);

                            }).Name;
                        }
                        else
                        {
                            var variable = (CodeVariable2)orchestratorClass.AddVariable(validatorInterfaceMemberName, ValidatorInterface, 0, vsCMAccess.vsCMAccessPrivate);
                            variable.ConstKind = vsCMConstKind.vsCMConstKindReadOnly;
                        }


                        //  Does the parameter already exist in the class?
                        if (constructorFunction.Children.OfType<CodeParameter2>().FirstOrDefault(c =>
                                {
                                    ThreadHelper.ThrowIfNotOnUIThread();
                                    var parts = c.Type.AsString.Split('.');
                                    return parts[parts.Length - 1].Equals(ValidatorInterface);
                                }) != null)
                        {
                            parameterName = constructorFunction.Children.OfType<CodeParameter2>().FirstOrDefault(c =>
                            {
                                ThreadHelper.ThrowIfNotOnUIThread();
                                var parts = c.Type.AsString.Split('.');
                                return parts[parts.Length - 1].Equals(ValidatorInterface);
                            }).Name;
                        }
                        else
                        {
                            constructorFunction.AddParameter(parameterName, ValidatorInterface, -1);
                        }

                        //  Is the variable already assigned?
                        var editPoint = (EditPoint2)constructorFunction.StartPoint.CreateEditPoint();
                        if (!editPoint.FindPattern($"{validatorInterfaceMemberName} = {parameterName};"))
                        {
                            editPoint = (EditPoint2)constructorFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();

                            var codeLine = editPoint.GetText(editPoint.LineLength);

                            if (!string.IsNullOrWhiteSpace(codeLine))
                            {
                                editPoint.EndOfLine();
                                editPoint.InsertNewLine();
                            }

                            editPoint.Indent(null, 3);
                            editPoint.Insert($"{validatorInterfaceMemberName} = {parameterName};");
                        }
                        #endregion

                        #region Get Single Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(getSingleFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var theGetSingleFunction = (CodeFunction2)orchestratorClass.AddFunction(getSingleFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task<{resourceClassName}>",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            theGetSingleFunction.AddParameter("node", "RqlNode", -1);
                            theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously gets {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>");
                            doc.AppendLine($"<returns>The specified {resourceClassName} resource.</returns>");
                            doc.AppendLine("</doc>");

                            theGetSingleFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)theGetSingleFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)theGetSingleFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{getSingleFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForGetAsync(node, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"return await GetSingleAsync<{resourceClassName}>(node);");
                        }
                        #endregion

                        #region Get Collection Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(collectionFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var theCollectionFunction = (CodeFunction2)orchestratorClass.AddFunction(collectionFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task<PagedCollection<{resourceClassName}>>",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            theCollectionFunction.AddParameter("originalQuery", "string", -1);
                            theCollectionFunction.AddParameter("node", "RqlNode", -1);
                            theCollectionFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously gets a collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine("<param name=\"originalQuery\">The original query string.</param>");
                            doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection.</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                            doc.AppendLine($"<returns>The collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.</returns>");
                            doc.AppendLine("</doc>");

                            theCollectionFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)theCollectionFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)theCollectionFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{collectionFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForGetAsync(node, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"return await GetCollectionAsync<{resourceClassName}>(originalQuery, node);");
                        }
                        #endregion

                        #region Add Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(addFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var theAddFunction = (CodeFunction2)orchestratorClass.AddFunction(addFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task<{resourceClassName}>",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            theAddFunction.AddParameter("item", resourceClassName, -1);
                            theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously adds {article} {resourceClassName} resource.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to add.</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                            doc.AppendLine("<returns>The newly added resource.</returns>");
                            doc.AppendLine("</doc>");

                            theAddFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)theAddFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)theAddFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{addFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForAddAsync(item, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"return await AddAsync<{resourceClassName}>(item);");
                        }
                        #endregion

                        #region Update Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(updateFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var theUpdateFunction = (CodeFunction2)orchestratorClass.AddFunction(updateFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task<{resourceClassName}>",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            theUpdateFunction.AddParameter("item", resourceClassName, -1);
                            theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to update.</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                            doc.AppendLine("<returns>The updated item.</returns>");
                            doc.AppendLine("</doc>");

                            theUpdateFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)theUpdateFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)theUpdateFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{updateFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForUpdateAsync(item, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"return await UpdateAsync<{resourceClassName}>(item);");
                        }
                        #endregion

                        #region Patch Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(patchFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var thePatchFunction = (CodeFunction2)orchestratorClass.AddFunction(patchFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
                            thePatchFunction.AddParameter("node", "RqlNode", -1);
                            thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource using patch commands.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine("<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>");
                            doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                            doc.AppendLine("</doc>");

                            thePatchFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)thePatchFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)thePatchFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{patchFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForPatchAsync(commands, node, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await PatchAsync<{resourceClassName}>(commands, node);");
                        }
                        #endregion

                        #region Delete Function
                        if (orchestratorClass.Children
                                             .OfType<CodeFunction2>()
                                             .FirstOrDefault(c => c.Name.Equals(deleteFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var theDeleteFunction = (CodeFunction2)orchestratorClass.AddFunction(deleteFunctionName,
                                                      vsCMFunction.vsCMFunctionFunction,
                                                      $"Task",
                                                      -1,
                                                      vsCMAccess.vsCMAccessPublic);

                            theDeleteFunction.AddParameter("node", "RqlNode", -1);
                            theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);

                            StringBuilder doc = new StringBuilder();
                            doc.AppendLine("<doc>");
                            doc.AppendLine("<summary>");
                            doc.AppendLine($"Asynchronously deletes {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
                            doc.AppendLine("</summary>");
                            doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection of resources to delete.</param>");
                            doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
                            doc.AppendLine("</doc>");

                            theDeleteFunction.DocComment = doc.ToString();

                            editPoint = (EditPoint2)theDeleteFunction.StartPoint.CreateEditPoint();
                            editPoint.ReplaceText(6, "public async", 0);

                            editPoint = (EditPoint2)theDeleteFunction.EndPoint.CreateEditPoint();
                            editPoint.LineUp();
                            editPoint.StartOfLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"Logger.LogDebug($\"{deleteFunctionName}\");");
                            editPoint.InsertNewLine(2);
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForDeleteAsync(node, User);");
                            editPoint.InsertNewLine();
                            editPoint.Indent(null, 3);
                            editPoint.Insert($"await DeleteAsync<{resourceClassName}>(node);");
                        }
                        #endregion
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void EmitEndpointExamples(DBServerType serverType, string resourceClassName, StringBuilder results, IEnumerable<DBColumn> pkcolumns)
		{
			bool first = true;

			foreach (var entityColumn in pkcolumns)
			{
				if (first)
					first = false;
				else
					results.Append(", ");

				string exampleValue = "example";

				if (serverType == DBServerType.POSTGRESQL)
					exampleValue = DBHelper.GetPostgresqlExampleValue(entityColumn);
				else if (serverType == DBServerType.MYSQL)
					exampleValue = DBHelper.GetMySqlExampleValue(entityColumn);
				else if (serverType == DBServerType.SQLSERVER)
					exampleValue = DBHelper.GetSqlServerExampleValue(entityColumn);

				results.AppendLine($"\t\t///\t<param name=\"{entityColumn.EntityName}\" example=\"{exampleValue}\">The {entityColumn.EntityName} of the {resourceClassName}.</param>");
			}
		}

		private void EmitEndpoint(DBServerType serverType, string resourceClassName, string action, StringBuilder results, IEnumerable<DBColumn> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IActionResult> {action}{resourceClassName}Async(");
			bool first = true;

			foreach (var entityColumn in pkcolumns)
			{
				if (first)
					first = false;
				else
					results.Append(", ");

				string dataType = entityColumn.ModelDataType;

				results.Append($"{dataType} {entityColumn.EntityName}");
			}

			if (string.Equals(action, "patch", StringComparison.OrdinalIgnoreCase))
				results.AppendLine(", [FromBody] IEnumerable<PatchCommand> commands)");
			else
				results.AppendLine(")");
		}

		private static void EmitRoute(StringBuilder results, string routeName, IEnumerable<DBColumn> pkcolumns)
		{
			results.Append($"\t\t[Route(\"{routeName}/id");

			foreach (var entityColumn in pkcolumns)
			{
				results.Append($"/{{{entityColumn.EntityName}}}");
			}

			results.AppendLine("\")]");
		}

		private static string BuildRoute(string routeName, IEnumerable<DBColumn> pkcolumns)
		{
			var route = new StringBuilder();

			route.Append(routeName);
			route.Append("/id");

			foreach (var entityColumn in pkcolumns)
			{
				route.Append($"/{{{entityColumn.ColumnName}}}");
			}

			return route.ToString();
		}
	}
}
