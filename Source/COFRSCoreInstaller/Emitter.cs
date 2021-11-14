using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
		public string EmitController(DTE2 app, EntityModel entityClass, ResourceModel resourceClass, string moniker, string controllerClassName, string ValidatorInterface, string policy, string ValidationNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var results = new StringBuilder();
            var nn = new NameNormalizer(resourceClass.ClassName);
            var pkcolumns = resourceClass.EntityModel.Columns.Where(c => c.IsPrimaryKey);

            BuildControllerInterface(app, resourceClass, nn);
            BuildControllerOrchestration(app, resourceClass, nn, ValidatorInterface, ValidationNamespace);

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

            results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(RqlCollection<{resourceClass.ClassName}>))]");
            results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}CollectionExample))]");

            results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine("\t\t[SupportRQL]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Get{nn.PluralForm}Async()");
            results.AppendLine("\t\t{");
            results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.Path}\");");
            results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.QueryString.Value);");
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
                EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine("\t\t///\t<remarks>This call supports RQL. Use the RQL select clause to limit the members returned.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"200\">Returns the specified {nn.SingleForm}.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");
                results.AppendLine("\t\t[HttpGet]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");

                results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
                results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                results.AppendLine("\t\t[SupportRQL]");

                EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Get", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
                results.AppendLine();
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\")");
                results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");

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
            results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
            results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");
            results.AppendLine("\t\t[HttpPost]");
            results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
            results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

            if (!string.IsNullOrWhiteSpace(policy))
                results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

            results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({resourceClass.ClassName}))]");
            results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Add{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
            results.AppendLine("\t\t{");
            results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
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
            results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
            results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
            results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
            results.AppendLine("\t\t[HttpPut]");
            results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
            results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

            if (!string.IsNullOrWhiteSpace(policy))
                results.AppendLine($"\t\t[Authorize(\"{policy}\")]");


            results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
            results.AppendLine("\t\t[SupportRQL]");
            results.AppendLine($"\t\tpublic async Task<IActionResult> Update{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
            results.AppendLine("\t\t{");
            results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
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
                EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine("\t\t///\t<param name=\"commands\">The patch commands to perform.</param>");
                results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine($"\t\t[SwaggerRequestExample(typeof(IEnumerable<PatchCommand>), typeof({resourceClass.ClassName}PatchExample))]");
                results.AppendLine("\t\t[HttpPatch]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
                EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Patch", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
                results.AppendLine();
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");

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
                EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
                results.AppendLine($"\t\t///\t<remarks>Deletes a {nn.SingleForm} in the datastore.</remarks>");
                results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
                results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
                results.AppendLine("\t\t[HttpDelete]");
                results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
                EmitRoute(results, nn.PluralCamelCase, pkcolumns);

                if (!string.IsNullOrWhiteSpace(policy))
                    results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

                EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Delete", results, pkcolumns);

                results.AppendLine("\t\t{");
                results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.Path}\");");
                results.AppendLine();
                results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");

                results.AppendLine($"\t\t\tawait Orchestrator.Delete{resourceClass.ClassName}Async(node, User);");
                results.AppendLine($"\t\t\treturn NoContent();");

                results.AppendLine("\t\t}");
                results.AppendLine("\t}");
            }

            return results.ToString();
        }

        private static void BuildControllerInterface(DTE2 app, ResourceModel resourceModel, NameNormalizer nn)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItem orchInterface = app.Solution.FindProjectItem("IServiceOrchestrator.cs");
            bool wasOpen = orchInterface.IsOpen[Constants.vsViewKindAny];               //	Record if it was already open

            if (!wasOpen)                                                               //	If it wasn't open, open it.
                orchInterface.Open(Constants.vsViewKindCode);

            var window = orchInterface.Open(Constants.vsViewKindTextView);              //	Get the window (so we can close it later)
            Document doc = orchInterface.Document;                                      //	Get the doc 
            TextSelection sel = (TextSelection)doc.Selection;                           //	Get the current selection
            var activePoint = sel.ActivePoint;                                          //	Get the active point

            bool taskNamespace = false;
            bool resourceNamespace = false;
            bool securityNamespace = false;
            bool collectionsNamespace = false;
            var fileCodeModel = (FileCodeModel2)orchInterface.FileCodeModel;

            foreach (CodeElement element in fileCodeModel.CodeElements)
            {
                if (element.Kind == vsCMElement.vsCMElementImportStmt)
                {
                    var usingStatement = (CodeImport)element;
                    var name = usingStatement.Namespace;

                    if (name.Equals("System.Threading.Tasks", StringComparison.OrdinalIgnoreCase))
                        taskNamespace = true;

                    if (name.Equals("System.Security.Claims", StringComparison.OrdinalIgnoreCase))
                        securityNamespace = true;

                    if (name.Equals("System.Collections.Generic", StringComparison.OrdinalIgnoreCase))
                        collectionsNamespace = true;

                    if (name.Equals(resourceModel.Namespace, StringComparison.OrdinalIgnoreCase))
                        resourceNamespace = true;
                }

                else if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement childElement in element.Children)
                    {
                        if (childElement.Kind == vsCMElement.vsCMElementInterface)
                        {
                            var interfaceCode = (CodeInterface2)childElement;

                            if (interfaceCode.Name.Equals("IServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
                            {
                                var primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();
                                var deleteFunctionName = $"Delete{resourceModel.ClassName}Async";
                                var patchFunctionName = $"Patch{resourceModel.ClassName}Async";
                                var updateFunctionName = $"Update{resourceModel.ClassName}Async";
                                var addFunctionName = $"Add{resourceModel.ClassName}Async";
                                var getSingleFunctionName = $"Get{resourceModel.ClassName}Async";
                                var collectionFunctionName = $"Get{resourceModel.ClassName}CollectionAsync";

                                bool addDeleteFunction = true;
                                bool addPatchFunction = true;
                                bool addUpdateFunction = true;
                                bool addAddFunction = true;
                                bool addSingleFunction = true;
                                bool addCollectionFunction = true;

                                foreach (CodeElement interfaceElement in interfaceCode.Children)
                                {
                                    if (interfaceElement.Kind == vsCMElement.vsCMElementFunction )
                                    {
                                        var functionElement = (CodeFunction2)interfaceElement;

                                        if (functionElement.Name.Equals(deleteFunctionName))
                                            addDeleteFunction = false;

                                        if (functionElement.Name.Equals(patchFunctionName))
                                            addPatchFunction = false;

                                        if (functionElement.Name.Equals(updateFunctionName))
                                            addUpdateFunction = false;

                                        if (functionElement.Name.Equals(addFunctionName))
                                            addAddFunction = false;

                                        if (functionElement.Name.Equals(getSingleFunctionName))
                                            addSingleFunction = false;

                                        if (functionElement.Name.Equals(collectionFunctionName))
                                            addCollectionFunction = false;
                                    }
                                }

                                try
                                {
                                    #region Get Single Function
                                    if (addSingleFunction)
                                    {
                                        var theGetSingleFunction = (CodeFunction2)interfaceCode.AddFunction(getSingleFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>", 
                                                                  -1);

                                        theGetSingleFunction.AddParameter("node", "RqlNode", -1);
                                        theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                            resourceModel.ClassName.ToLower().StartsWith("e") ||
                                            resourceModel.ClassName.ToLower().StartsWith("i") ||
                                            resourceModel.ClassName.ToLower().StartsWith("o") ||
                                            resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        var editPoint = (EditPoint2)theGetSingleFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tGets {article} {resourceModel.ClassName} resource\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
                                    }
                                    #endregion

                                    #region Get Collection Function
                                    if (addCollectionFunction)
                                    {
                                        var theFunction = (CodeFunction2)interfaceCode.AddFunction(collectionFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<RqlCollection<{resourceModel.ClassName}>>",
                                                                  -1);

                                        theFunction.AddParameter("originalQuery", "string", -1);
                                        theFunction.AddParameter("node", "RqlNode", -1);
                                        theFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var editPoint = (EditPoint2)theFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"\r\n\t\t///\t<summary>\r\n\t\t///\tReturns a collection of {resourceModel.ClassName} resources\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"originalQuery\">The original query string</param>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
                                    }
                                    #endregion

                                    #region Add Function
                                    if (addAddFunction)
                                    {
                                        var theAddFunction = (CodeFunction2)interfaceCode.AddFunction(addFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>",
                                                                  -1);

                                        theAddFunction.AddParameter("item", resourceModel.ClassName, -1);
                                        theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        var editPoint = (EditPoint2)theAddFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"\r\n\t\t///\t<summary>\r\n\t\t///\tAdd {article} {resourceModel.ClassName} resource.\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"item\">The {resourceModel.ClassName} resource to add.</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
                                    }
                                    #endregion

                                    #region Update Function
                                    if (addUpdateFunction)
                                    {
                                        var theUpdateFunction = (CodeFunction2)interfaceCode.AddFunction(updateFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>", 
                                                                  -1);

                                        theUpdateFunction.AddParameter("item", resourceModel.ClassName, -1);
                                        theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        var editPoint = (EditPoint2)theUpdateFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"\r\n\t\t///\t<summary>\r\n\t\t///\tUpdate {article} {resourceModel.ClassName} resource.\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"item\">The {resourceModel.ClassName} resource to update.</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
                                    }
                                    #endregion

                                    #region Patch Function
                                    if (addPatchFunction)
                                    {
                                        var thePatchFunction = (CodeFunction2)interfaceCode.AddFunction(patchFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task", 
                                                                  -1);

                                        thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
                                        thePatchFunction.AddParameter("node", "RqlNode", -1);
                                        thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        var editPoint = (EditPoint2)thePatchFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"\r\n\t\t///\t<summary>\r\n\t\t///\tUpdate {article} {resourceModel.ClassName} resource using patch commands\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
                                    }
                                    #endregion

                                    #region Delete Function
                                    if (addDeleteFunction)
                                    {
                                        var theDeleteFunction = (CodeFunction2)interfaceCode.AddFunction(deleteFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task", -1);

                                        theDeleteFunction.AddParameter("node", "RqlNode", -1);
                                        theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                            resourceModel.ClassName.ToLower().StartsWith("e") ||
                                            resourceModel.ClassName.ToLower().StartsWith("i") ||
                                            resourceModel.ClassName.ToLower().StartsWith("o") ||
                                            resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        EditPoint2 editPoint = (EditPoint2)theDeleteFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"\r\n\t\t///\t<summary>\r\n\t\t///\tDelete {article} {resourceModel.ClassName} resource\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");
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
            }

            if (!taskNamespace)
                fileCodeModel.AddImport("System.Threading.Tasks");

            if (!securityNamespace)
                fileCodeModel.AddImport("System.Security.Claims");

            if (!collectionsNamespace)
                fileCodeModel.AddImport("System.Collections.Generic");

            if (!resourceNamespace)
                fileCodeModel.AddImport(resourceModel.Namespace);

            if (!wasOpen)
                window.Close();
        }

        private static void BuildControllerOrchestration(DTE2 app, ResourceModel resourceModel, NameNormalizer nn, string ValidatorInterface, string ValidationNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItem orchCode = app.Solution.FindProjectItem("ServiceOrchestrator.cs");
            bool wasOpen = orchCode.IsOpen[Constants.vsViewKindAny];               //	Record if it was already open

            if (!wasOpen)                                                               //	If it wasn't open, open it.
                orchCode.Open(Constants.vsViewKindCode);

            var window = orchCode.Open(Constants.vsViewKindTextView);              //	Get the window (so we can close it later)
            Document doc = orchCode.Document;                                      //	Get the doc 
            TextSelection sel = (TextSelection)doc.Selection;                           //	Get the current selection
            var activePoint = sel.ActivePoint;                                          //	Get the active point

            bool taskNamespace = false;
            bool resourceNamespace = false;
            bool securityNamespace = false;
            bool collectionsNamespace = false;
            bool validationNamespace = false;
            var fileCodeModel = (FileCodeModel2)orchCode.FileCodeModel;

            foreach (CodeElement element in fileCodeModel.CodeElements)
            {
                if (element.Kind == vsCMElement.vsCMElementImportStmt)
                {
                    var usingStatement = (CodeImport)element;
                    var name = usingStatement.Namespace;

                    if (name.Equals("System.Threading.Tasks", StringComparison.OrdinalIgnoreCase))
                        taskNamespace = true;

                    if (name.Equals("System.Security.Claims", StringComparison.OrdinalIgnoreCase))
                        securityNamespace = true;

                    if (name.Equals("System.Collections.Generic", StringComparison.OrdinalIgnoreCase))
                        collectionsNamespace = true;

                    if (name.Equals(ValidationNamespace, StringComparison.OrdinalIgnoreCase))
                        validationNamespace = true;

                    if (name.Equals(resourceModel.Namespace, StringComparison.OrdinalIgnoreCase))
                        resourceNamespace = true;
                }

                else if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement childElement in element.Children)
                    {
                        if (childElement.Kind == vsCMElement.vsCMElementClass)
                        {
                            var orchestratorClass = (CodeClass2)childElement;

                            if (orchestratorClass.Name.Equals("ServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
                            {
                                var primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();
                                var deleteFunctionName = $"Delete{resourceModel.ClassName}Async";
                                var patchFunctionName = $"Patch{resourceModel.ClassName}Async";
                                var updateFunctionName = $"Update{resourceModel.ClassName}Async";
                                var addFunctionName = $"Add{resourceModel.ClassName}Async";
                                var getSingleFunctionName = $"Get{resourceModel.ClassName}Async";
                                var collectionFunctionName = $"Get{resourceModel.ClassName}CollectionAsync";

                                bool addDeleteFunction = true;
                                bool addPatchFunction = true;
                                bool addUpdateFunction = true;
                                bool addAddFunction = true;
                                bool addSingleFunction = true;
                                bool addCollectionFunction = true;

                                var parameterName = ValidatorInterface.Substring(1, 1).ToLower() + ValidatorInterface.Substring(2);
                                var validatorInterfaceMemberName = ValidatorInterface.Substring(1, 1).ToUpper() + ValidatorInterface.Substring(2);

                                CodeFunction2 constructorFunction = null;

                                foreach (CodeElement interfaceElement in orchestratorClass.Children)
                                {
                                    if (interfaceElement.Kind == vsCMElement.vsCMElementFunction)
                                    {
                                        var functionElement = (CodeFunction2)interfaceElement;

                                        if (functionElement.Name.Equals(deleteFunctionName))
                                            addDeleteFunction = false;

                                        if (functionElement.Name.Equals(patchFunctionName))
                                            addPatchFunction = false;

                                        if (functionElement.Name.Equals(updateFunctionName))
                                            addUpdateFunction = false;

                                        if (functionElement.Name.Equals(addFunctionName))
                                            addAddFunction = false;

                                        if (functionElement.Name.Equals(getSingleFunctionName))
                                            addSingleFunction = false;

                                        if (functionElement.Name.Equals(collectionFunctionName))
                                            addCollectionFunction = false;

                                        if (functionElement.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                                            constructorFunction = functionElement;
                                    }
                                }

                                try
                                {
                                    #region Constructor
                                    bool addParameter = true;
                                    bool addVariable = true;

                                    //  Does the varialble already exist in the class?
                                    foreach (CodeVariable2 theVariable in orchestratorClass.Children.OfType<CodeVariable2>() )
                                    {
                                        if (theVariable.Type.AsString.Contains(ValidatorInterface))
                                        {
                                            validatorInterfaceMemberName = theVariable.Name;
                                            addVariable = false;
                                        }
                                    }

                                    //  Does the parameter already exist in the class?
                                    foreach (CodeParameter2 parameter in constructorFunction.Parameters.OfType<CodeParameter2>())
                                    {
                                        if (parameter.Type.AsString.Contains(ValidatorInterface))
                                        {
                                            parameterName = parameter.Name;
                                            addParameter = false;
                                        }
                                    }

                                    //  Is the variable already assigned?
                                    var editPoint = (EditPoint2) constructorFunction.StartPoint.CreateEditPoint();
                                    bool addAssignment = !editPoint.FindPattern($"{validatorInterfaceMemberName} = {parameterName};");

                                    //  Add the new variable if we need to.
                                    if (addVariable)
                                    {
                                        var variable = (CodeVariable2)orchestratorClass.AddVariable(validatorInterfaceMemberName, ValidatorInterface, 0, vsCMAccess.vsCMAccessPrivate);
                                        variable.ConstKind = vsCMConstKind.vsCMConstKindReadOnly;
                                    }

                                    //  Add the parameter if we need to.
                                    if (addParameter)
                                    {
                                        constructorFunction.AddParameter(parameterName, ValidatorInterface, -1);
                                    }

                                    //  Add the assignment if we need to
                                    if ( addAssignment )
                                    { 
                                        editPoint = (EditPoint2) constructorFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        var codeLine = editPoint.GetText(editPoint.LineLength);

                                        if ( !string.IsNullOrWhiteSpace(codeLine))
                                        {
                                            editPoint.EndOfLine();
                                            editPoint.InsertNewLine();
                                        }

                                        editPoint.Indent(null, 3);
                                        editPoint.Insert($"{validatorInterfaceMemberName} = {parameterName};");
                                    }
                                    #endregion

                                    #region Get Single Function
                                    if (addSingleFunction)
                                    {
                                        var theGetSingleFunction = (CodeFunction2)orchestratorClass.AddFunction(getSingleFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>",
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        theGetSingleFunction.AddParameter("node", "RqlNode", -1);
                                        theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        theGetSingleFunction.DocComment = $@"
<doc>
<summary>
Gets {article} {resourceModel.ClassName} resource.
</summary>
<param name=""node"">The <see cref=""RqlNode""/> that further restricts the selection</param>
<param name=""User"">The <see cref=""ClaimsPrincipal""/> of the actor calling the function.</param>
</doc>";

                                        sel.MoveToPoint(theGetSingleFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)theGetSingleFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();
                                        editPoint.Indent(null, 3);
                                        editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForGetAsync(node, User);");
                                        editPoint.InsertNewLine();
                                        editPoint.Indent(null, 3);
                                        editPoint.Insert($"return await GetSingleAsync<{resourceModel.ClassName}>(node);");
                                    }
                                    #endregion

                                    #region Get Collection Function
                                    if (addCollectionFunction)
                                    {
                                        var theFunction = (CodeFunction2)orchestratorClass.AddFunction(collectionFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<RqlCollection<{resourceModel.ClassName}>>",
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        theFunction.AddParameter("originalQuery", "string", -1);
                                        theFunction.AddParameter("node", "RqlNode", -1);
                                        theFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        editPoint = (EditPoint2)theFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tReturns a collection of {resourceModel.ClassName} resources\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"originalQuery\">The original query string</param>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");

                                        sel.MoveToPoint(theFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)theFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        editPoint.Insert($"\t\t\tawait {validatorInterfaceMemberName}.ValidateForGetAsync(node, User);\r\n\t\t\treturn await GetCollectionAsync<{resourceModel.ClassName}>(originalQuery, node);");
                                    }
                                    #endregion

                                    #region Add Function
                                    if (addAddFunction)
                                    {
                                        var theAddFunction = (CodeFunction2)orchestratorClass.AddFunction(addFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>",
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        theAddFunction.AddParameter("item", resourceModel.ClassName, -1);
                                        theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        editPoint = (EditPoint2)theAddFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tAdd {article} {resourceModel.ClassName} resource\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"item\">The {resourceModel.ClassName} resource to add.</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");

                                        sel.MoveToPoint(theAddFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)theAddFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        editPoint.Insert($"\t\t\tawait {validatorInterfaceMemberName}.ValidateForAddAsync(item, User);\r\n\t\t\treturn await AddAsync<{resourceModel.ClassName}>(item);");
                                    }
                                    #endregion

                                    #region Update Function
                                    if (addUpdateFunction)
                                    {
                                        var theUpdateFunction = (CodeFunction2)orchestratorClass.AddFunction(updateFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task<{resourceModel.ClassName}>", 
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        theUpdateFunction.AddParameter("item", resourceModel.ClassName, -1);
                                        theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        editPoint = (EditPoint2)theUpdateFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tUpdate {article} {resourceModel.ClassName} resource\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"item\">The {resourceModel.ClassName} resource to update.</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");

                                        sel.MoveToPoint(theUpdateFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)theUpdateFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        editPoint.Indent(null, 3);
                                        editPoint.Insert($"await {validatorInterfaceMemberName}.ValidateForUpdateAsync(item, User);");
                                        editPoint.InsertNewLine();
                                        editPoint.Indent(null, 3);
                                        editPoint.Insert($"return await UpdateAsync<{resourceModel.ClassName}>(item);");
                                    }
                                    #endregion

                                    #region Patch Function
                                    if (addPatchFunction)
                                    {
                                        var thePatchFunction = (CodeFunction2)orchestratorClass.AddFunction(patchFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task",
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
                                        thePatchFunction.AddParameter("node", "RqlNode", -1);
                                        thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                     resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                     resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                     resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                     resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        editPoint = (EditPoint2)thePatchFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tUpdate {article} {resourceModel.ClassName} resource using patch commands\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");


                                        sel.MoveToPoint(thePatchFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)thePatchFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        editPoint.Insert($"\t\t\tawait {validatorInterfaceMemberName}.ValidateForPatchAsync(commands, node, User);\r\n\t\t\tawait PatchAsync<{resourceModel.ClassName}>(commands, node);");
                                    }
                                    #endregion

                                    #region Delete Function
                                    if (addDeleteFunction)
                                    {
                                        var theDeleteFunction = (CodeFunction2)orchestratorClass.AddFunction(deleteFunctionName,
                                                                  vsCMFunction.vsCMFunctionFunction,
                                                                  $"Task",
                                                                  -1,
                                                                  vsCMAccess.vsCMAccessPublic);

                                        theDeleteFunction.AddParameter("node", "RqlNode", -1); 
                                        theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);
                                        
                                        var article = resourceModel.ClassName.ToLower().StartsWith("a") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("e") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("i") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("o") ||
                                                      resourceModel.ClassName.ToLower().StartsWith("u") ? "an" : "a";

                                        editPoint = (EditPoint2)theDeleteFunction.StartPoint.CreateEditPoint();
                                        editPoint.Insert($"///\t<summary>\r\n\t\t///\tDelete {article} {resourceModel.ClassName} resource\r\n\t\t///\t</summary>\r\n\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>\r\n\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>\r\n\t\t");

                                        sel.MoveToPoint(theDeleteFunction.StartPoint);
                                        editPoint.ReplaceText(6, "public async", 0);

                                        editPoint = (EditPoint2)theDeleteFunction.EndPoint.CreateEditPoint();
                                        editPoint.LineUp();
                                        editPoint.StartOfLine();

                                        editPoint.Insert($"\t\t\tawait {validatorInterfaceMemberName}.ValidateForDeleteAsync(node, User);\r\n\t\t\tawait DeleteAsync<{resourceModel.ClassName}>(node);");
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
            }

            if (!taskNamespace)
                fileCodeModel.AddImport("System.Threading.Tasks");

            if (!securityNamespace)
                fileCodeModel.AddImport("System.Security.Claims");

            if (!collectionsNamespace)
                fileCodeModel.AddImport("System.Collections.Generic");

            if (!validationNamespace)
                fileCodeModel.AddImport(ValidationNamespace);

            if (!resourceNamespace)
                fileCodeModel.AddImport(resourceModel.Namespace);

            if (!wasOpen)
                window.Close();
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
