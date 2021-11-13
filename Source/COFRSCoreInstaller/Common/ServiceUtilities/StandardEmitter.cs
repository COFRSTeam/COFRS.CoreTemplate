using COFRS.Template.Common.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace COFRS.Template.Common.ServiceUtilities
{
	public class StandardEmitter
	{
		/// <summary>
		/// Generates a validation model for the specified resource
		/// </summary>
		/// <param name="resourceModel">The resource model for the resource being validated.</param>
		/// <param name="entityModel">The corresponding entity model for the resource being validated.</param>
		/// <param name="profileMap">The profile map between the resource and entity models.</param>
		/// <param name="validatorClassName">The name of the validator class</param>
		/// <param name="validatorInterface">The output parameter returning the validator interface name.</param>
		/// <returns>The code for the validator class in a string.</returns>
		public string EmitValidationModel(ResourceModel resourceModel, ProfileMap profileMap, ResourceMap resourceMap, EntityMap entityMap, string validatorClassName)
		{
			//	Instantiate a string builder. We will use the string builder to construct our code.
			var results = new StringBuilder();

			//	The validator interface is nothing more than I followed by the validator class name.
			var validatorInterface = $"I{validatorClassName}";

			//	Define the IValidator interface. This is nothing more than an interface that is derrived from
			//	the IValidator<T> interface. The IValidator<T> interface has all the important methods defined
			//	in it, so that this interface we are creating doesn't need any methods.
			//
			//	It will be up the the user to add any additional validation methods to this interface that they
			//	deem necessasry.
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\tInterface for the {resourceModel.ClassName} Validator");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic interface {validatorInterface} : IValidator<{resourceModel.ClassName}>");
			results.AppendLine("\t{");
			results.AppendLine("\t}");
			results.AppendLine();

			//	Define the validator class and it's constructor. This class is derrived from the Validator<T> base class,
			//	and the interface we just defined above.
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{validatorClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {validatorClassName} : Validator<{resourceModel.ClassName}>, {validatorInterface}");
			results.AppendLine("\t{");


			results.AppendLine("\t\tprivate readonly IServiceOrchestrator _orchestrator;");
			results.AppendLine();


			//	Here is the constructor - nothing much to do here, just an empty constructor.
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {validatorClassName}");
			results.AppendLine("\t\t///\t</summary>");	
			results.AppendLine($"\t\tpublic {validatorClassName}(IServiceOrchestrator orchestrator)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t_orchestrator = orchestrator;");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for GET
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for Queries");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the query.</param>");
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForGetAsync(RqlNode node, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tUn-comment out the line below if this table is large, and you want to prevent users from requesting a full table scan");
			results.AppendLine("\t\t\t//\tRequireIndexedQuery(node, \"The query is too broad. Please specify a more refined query that will produce fewer records.\");");
			results.AppendLine();
			results.AppendLine("\t\t\tawait base.ValidateForGetAsync(node, User, parms);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT and POST
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidations common to adding and updating items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being added or updated</param>");
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic async Task ValidateForAddAndUpdateAsync({resourceModel.ClassName} item, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");

			//	To do:	Scan the profileMap to see which resource elements are nullable (if they are not, write code here to verify that 
			//			they are not null), and write code to check the length of any strings.
			//
			//			This code will look like:	Require(item.membername != null, "The value for membername cannot be null."); 
			//										Require(!string.IsNullOrWhiteSpace(item.membername), "The value for membername cannot be null."); 
			//										Require(item.membername.Length <= 10, "The value of membername must be 10 characters or less.");

			results.AppendLine("\t\t\t//\tTo do: Add any additinal code to perform any specific validations pertaining to");
			results.AppendLine("\t\t\t//\t       adding or updating an item here.");
			results.AppendLine("\t\t\tawait Task.CompletedTask;");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidation for updating existing items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being updated</param>");
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForUpdateAsync({resourceModel.ClassName} item, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, User, parms);");
			results.AppendLine();

			//	To do:	If the item has an href, then that href cannot be null for an update. Write the code to ensure it is not null
			//			here, if needed.

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
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForAddAsync({resourceModel.ClassName} item, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, User, parms);");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: add any specific validations pertaining to adding an item.");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PATCH
			//------------------------------------------------------------------------------------------

			//	A patch command consists of 3 fields:
			//		command.Op => The operation to perform. Can be "replace", "add" or "delete"
			//		command.Path => The fully qualified reference to the member to acted upon
			//		command.Value => The new value (if applicable)
			//
			//	For example: command.Op = "replace", command.Path = "Name.FirstName", command.Value = "John"
			//	instructs the program to replace the value for Name.FirstName with "John".
			//

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidates a set of patch commands on an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"patchCommands\">The set of patch commands to validate</param>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the update</param>");
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine("\t\tpublic override async Task ValidateForPatchAsync(IEnumerable<PatchCommand> patchCommands, RqlNode node, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tforeach (var command in patchCommands)");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tif (string.Equals(command.Op, \"replace\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			//	The command op is "replace".
			//
			//	The replace command tells the system to "replace", or update, the value of the member with the new value. Typically,
			//	the Path specifies a single value: 
			//
			//	op/path/value => replace/name.firstName/John
			//
			//	This command tells the system to set the item.name.firstName = "John".
			//
			//	However, a replace command can set an entire object:
			//
			//	op/path/value => replace/name/{ "firstName": "Jonn", "lastName": "Smith", "Suffix": "Jr." }
			//
			//	This command tells the system to replace all the values in the Name object.
			//
			//	Reserved words for value:  <null>, <default>
			//
			//	<null> will set the value to null.
			//	<default> will set the value to the default value for the member (this might be null, or whatever the min value is if
			//	null is not acceptable).
			//
			//	For a collection, the replace command will replace the entire item in the collection
			//
			//	Imagine we have a class called PhoneContact. The class has two values: Type and Number.
			//	Imagine we have a class that contains a member called phoneContacts that is a collection of PhoneContact classes:
			//
			//	public PhoneContact[] phoneContacts { get; set; }
			//
			//	Now, imagine that our collection looks like this:
			//
			//	[ { "Type": "Home", "Number": "913 754-1411" }
			//	  { "Type": "Work", "Number": "913 241-6400" }
			//	  { "Type": "Mobile", "Number": "913 624-9084" }
			//	  { "Type": "Fax", "Number": "913 313-1140" } ]
			//	  
			//	op/path/value => replace/phoneContacts[1]/{ "Type": "Work", "Number": "913 214-4572" }
			//
			//	Then our resultant collection would be:
			//
			//	[ { "Type": "Home", "Number": "913 754-1411" }
			//	  { "Type": "Work", "Number": "913 214-4572" }
			//	  { "Type": "Mobile", "Number": "913 624-9084" }
			//	  { "Type": "Fax", "Number": "913 313-1140" } ]

			//	TO DO: For single item values, check length and not nullable (if the command.Value = <null>)
			//		   For object values, check each length and not nullable in the object : note, you'll need to deserialize the json to do this
			//		   For array values, check array range (the item they are trying to replace actually exists), and check length and not nullable of the object

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"add\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			//	The command op is "add".
			//
			//	The add command tells the system to "add", or update, the value of the member with the new value. Typically,
			//	the Path specifies a collection. However, when the path specifes a single value, add acts just the same as replace.
			//
			//	op/path/value => add/name.firstName/John
			//
			//	This command tells the system to set the item.name.firstName = "John".
			//
			//	However, an add command can set an entire object (again, works just like replace):
			//
			//	op/path/value => add/name/{ "firstName": "Jonn", "lastName": "Smith", "Suffix": "Jr." }
			//
			//	This command tells the system to add (i.e. replace) all the values in the Name object.
			//
			//	Reserved words for value:  <null>, <default>
			//
			//	<null> will set the value to null.
			//	<default> will set the value to the default value for the member (this might be null, or whatever the min value is if
			//	null is not acceptable).
			//
			//	Imagine the phoneContacts class again. This time, add really does mean add.
			//	  
			//	op/path/value => add/phoneContacts/{ "Type": "Other", "Number": "913 667-5144" }
			//
			//	Then our resultant collection would be:
			//
			//	[ { "Type": "Home", "Number": "913 754-1411" }
			//	  { "Type": "Work", "Number": "913 214-4572" }
			//	  { "Type": "Mobile", "Number": "913 624-9084" }
			//	  { "Type": "Fax", "Number": "913 313-1140" }
			//	  { "Type": "Other", "Number": "913 667-5144" } ]

			//	TO DO: For single item values, check length and not nullable (if the command.Value = <null>)
			//		   For object values, check each length and not nullable in the object : note, you'll need to deserialize the json to do this
			//		   For array values,  check each length and not nullable of the object : note, you'll need to deserialize the json to do this

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"delete\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			//	The command op is "delete".
			//
			//	The delete command tells the system to "delete", or remove, the value of the member. The Value item is ignored for delete.
			//	Typically, the Path specifies a collection. However, when the path specifes a single value, delete acts like replace, except
			//	that it replaces the value of the member with the default value for that member.
			//
			//	op/path/value => delete/name.firstName
			//
			//	This command tells the system to set the item.name.firstName = null
			//
			//	However, a delete command can set an entire object (again, works just like replace):
			//
			//	op/path/value => delete/name
			//
			//	This command tells the system to delete (i.e. replace) the name object:   item.name = null
			//
			//	Imagine the phoneContacts class again. This time, delete really does mean delete.
			//	  
			//	op/path/value => delete/phoneContacts[2]
			//
			//	Then our resultant collection would be:
			//
			//	[ { "Type": "Home", "Number": "913 754-1411" }
			//	  { "Type": "Work", "Number": "913 214-4572" }
			//	  { "Type": "Fax", "Number": "913 313-1140" } ]

			//	TO DO: For single item values, check not nullable
			//		   For object values, check object not nullable 
			//		   For array values,  check valid range (the item they are trying to delete actually exists)


			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t\t//\tTo do: Add any additional code to perform any specific validations pertaining to patching an item.");
			results.AppendLine("\t\t\tawait base.ValidateForPatchAsync(patchCommands, node, User, parms);");
			results.AppendLine("\t\t}");
			results.AppendLine();
			
			//------------------------------------------------------------------------------------------
			//	Validation for DELETE
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for deleting an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the delete</param>");
			results.AppendLine("\t\t///\t<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> responsible for making this request.</param>");
			results.AppendLine("\t\t///\t<param name=\"parms\">The additional, and optional, parameters used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForDeleteAsync(RqlNode node, ClaimsPrincipal User = null, object[] parms = null)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tTo do: Add any additional code to perform any specific validations pertaining to deleting an item.");
			results.AppendLine("\t\t\tawait base.ValidateForDeleteAsync(node, User, parms);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitExampleModel(ResourceModel resourceModel, ProfileMap profileMap, ResourceMap resourceMap, EntityMap entityMap, string exampleClassName, DBServerType serverType, string connectionString)
        {
            var results = new StringBuilder();          
			
			//	Generate the patch example class
			results.AppendLine("\t///\t<summary>");

			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t///\tGenerates an example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.");
			else
				results.AppendLine($"\t///\tGenerates an example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.");

			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {resourceModel.ClassName}PatchExample : IExamplesProvider<IEnumerable<PatchCommand>>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t///\tGenerates an example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.");
			else
				results.AppendLine($"\t\t///\tGenerates an example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.");

			results.AppendLine("\t\t///\t</summary>");

			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t///\t<returns>An example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");
			else
				results.AppendLine($"\t\t///\t<returns>An example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");

			results.AppendLine($"\t\tpublic IEnumerable<PatchCommand> GetExamples()");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tvar results = new List<PatchCommand> {");

			var exampleModel = GetExampleModel(0, resourceModel, serverType, connectionString);
			var entityJson = JObject.Parse(exampleModel);
			var count = 0;

			foreach ( var property in entityJson.Children())
            {
				switch ( count )
                {
					case 1:
						{
							results.AppendLine("\t\t\t\tnew PatchCommand {");
							results.AppendLine("\t\t\t\t\tOp = \"replace\",");
							results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");

							if (property.First.Type == JTokenType.Null)
								results.AppendLine($"\t\t\t\t\tValue = null");
							else if (property.First.Type == JTokenType.String ||
									 property.First.Type == JTokenType.Date ||
									 property.First.Type == JTokenType.Guid ||
									 property.First.Type == JTokenType.TimeSpan)
								results.AppendLine($"\t\t\t\t\tValue = \"{((string)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Integer)
								results.AppendLine($"\t\t\t\t\tValue = \"{((long)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Float)
								results.AppendLine($"\t\t\t\t\tValue = \"{((double)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Boolean)
							{
								var theBooleanValue = (bool)property.Value<JProperty>();
								results.AppendLine($"\t\t\t\t\tValue = {theBooleanValue.ToString().ToLower()}");
							}
							else
								results.AppendLine($"\t\t\t\t\tValue = \"value\"");

							results.AppendLine("\t\t\t\t},");
						}
						break;

					case 2:
						{
							results.AppendLine("\t\t\t\tnew PatchCommand {");
							results.AppendLine("\t\t\t\t\tOp = \"add\",");
							results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");

							if (property.First.Type == JTokenType.Null)
								results.AppendLine($"\t\t\t\t\tValue = null");
							else if (property.First.Type == JTokenType.String ||
									 property.First.Type == JTokenType.Date ||
									 property.First.Type == JTokenType.Guid ||
									 property.First.Type == JTokenType.TimeSpan)
								results.AppendLine($"\t\t\t\t\tValue = \"{((string)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Integer)
								results.AppendLine($"\t\t\t\t\tValue = \"{((long)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Float)
								results.AppendLine($"\t\t\t\t\tValue = \"{((double)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Boolean)
							{
								var theBooleanValue = (bool)property.Value<JProperty>();
								results.AppendLine($"\t\t\t\t\tValue = {theBooleanValue.ToString().ToLower()}");
							}
							else
								results.AppendLine($"\t\t\t\t\tValue = \"value\"");

							results.AppendLine("\t\t\t\t},");
						}
						break;

					case 3:
						results.AppendLine("\t\t\t\tnew PatchCommand {");
						results.AppendLine("\t\t\t\t\tOp = \"delete\",");
						results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");
						results.AppendLine("\t\t\t\t}");
						break;
				};

				count++;
            }

			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine("\t\t\treturn results;");

			results.AppendLine("\t\t}");
			results.AppendLine("\t}");
			results.AppendLine();


			//	Generate the single example class
			results.AppendLine("\t///\t<summary>");

            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t///\tGenerates an example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.");
            else
                results.AppendLine($"\t///\tGenerates an example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.");

            results.AppendLine("\t///\t</summary>");
            results.AppendLine($"\tpublic class {resourceModel.ClassName}Example : IExamplesProvider<{resourceModel.ClassName}>");
            results.AppendLine("\t{");

			results.AppendLine("\t\tprivate static R MapFrom<R>(Func<R> f)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\treturn f();");
			results.AppendLine("\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t///\t<summary>");
            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t\t///\tGenerates an example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.");
            else
                results.AppendLine($"\t\t///\tGenerates an example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.");

            results.AppendLine("\t\t///\t</summary>");

            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t\t///\t<returns>An example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");
            else
                results.AppendLine($"\t\t///\t<returns>An example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");

            results.AppendLine($"\t\tpublic {resourceModel.ClassName} GetExamples()");
            results.AppendLine("\t\t{");

            results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");
            results.AppendLine();

			results.Append("\t\t\tvar singleExample = ");
            EmitSingleModel("", resourceModel, profileMap, results, entityJson);
            results.AppendLine(";");

            results.AppendLine();
            results.AppendLine("\t\t\treturn singleExample;");

            results.AppendLine("\t\t}");
            results.AppendLine("\t}");
            results.AppendLine();

			//	Generate the collection example class
            results.AppendLine("\t///\t<summary>");
            results.AppendLine($"\t///\tGenerates an example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.");
            results.AppendLine("\t///\t</summary>");
            results.AppendLine($"\t///\t<returns>An example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.</returns>");
            results.AppendLine($"\t\tpublic class {resourceModel.ClassName}CollectionExample : IExamplesProvider<RqlCollection<{resourceModel.ClassName}>>");
            results.AppendLine("\t{");

			results.AppendLine("\t\tprivate static R MapFrom<R>(Func<R> f)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\treturn f();");
			results.AppendLine("\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t///\t<summary>");
            results.AppendLine($"\t\t///\tGenerates an example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.");
            results.AppendLine("\t\t///\t</summary>");
            results.AppendLine($"\t\t///\t<returns>An example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.</returns>");
            results.AppendLine($"\t\tpublic RqlCollection<{resourceModel.ClassName}> GetExamples()");
            results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");
			results.AppendLine();
			results.AppendLine($"\t\t\tvar exampleList = new List<{resourceModel.ClassName}>() {{");

			var first = true;

			for ( int j = 0; j < 3; j++ )
            {
				if (first)
					first = false;
				else
					results.AppendLine(",");

				int r = j + 6;
				exampleModel = "";

				while ( string.IsNullOrWhiteSpace(exampleModel.Replace("{","").Replace("}","")))
					exampleModel = GetExampleModel(r--, resourceModel, serverType, connectionString);

				entityJson = JObject.Parse(exampleModel);

				results.Append("\t\t\t\t");
				EmitSingleModel("\t", resourceModel, profileMap, results, entityJson);
			}

			var baseUrl = ExtractBaseUrl(profileMap);

			results.AppendLine();
			results.AppendLine("\t\t\t};");
			results.AppendLine();
			results.AppendLine($"\t\t\tvar collection = new RqlCollection<{resourceModel.ClassName}>() {{");
			results.AppendLine($"\t\t\t\tHRef = new Uri(rootUrl, \"{baseUrl}?limit(6,3)\"),");
			results.AppendLine($"\t\t\t\tFirst = new Uri(rootUrl, \"{baseUrl}?limit(1,3)\"),");
			results.AppendLine($"\t\t\t\tPrevious = new Uri(rootUrl, \"{baseUrl}?limit(3,3)\"),");
			results.AppendLine($"\t\t\t\tNext = new Uri(rootUrl, \"{baseUrl}?limit(9,3)\"),");
			results.AppendLine($"\t\t\t\tLimit = 3,");
			results.AppendLine($"\t\t\t\tCount = 20,");
			results.AppendLine($"\t\t\t\tItems = exampleList.ToArray()");
			results.AppendLine("\t\t\t\t};");
			results.AppendLine("\t\t\treturn collection;");
			results.AppendLine("\t\t}");
            results.AppendLine("\t}");
            results.AppendLine();
			
            return results.ToString();
        }

		public string EmitResourceEnum(ResourceModel model, DBServerType serverType, string connectionString)
        {
			if (serverType == DBServerType.MYSQL)
				return EmitResourceMySqlEnum(model, connectionString);
			else if (serverType == DBServerType.POSTGRESQL)
				return EmitResourcePostgresqlEnum(model, connectionString);
			else if (serverType == DBServerType.SQLSERVER)
				return EmitResourceSqlServerEnum(model, connectionString);

			return "Invalid DB Server Type";
        }

		private static string EmitResourceMySqlEnum(ResourceModel model, string connectionString)
		{
			throw new NotImplementedException("not implemented yet");
		}
		private static string EmitResourcePostgresqlEnum(ResourceModel model, string connectionString)
		{
			throw new NotImplementedException("not implemented yet");
		}
		private static string EmitResourceSqlServerEnum(ResourceModel resourceModel, string connectionString)
		{
			StringBuilder results = new StringBuilder();

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceModel.ClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({resourceModel.EntityModel.ClassName}))]");

			var dataType = resourceModel.EntityModel.Columns[0].ModelDataType;

			results.AppendLine($"\tpublic enum {resourceModel.ClassName} : {dataType}");
			results.AppendLine("\t{");

			bool firstColumn = true;

			string query = "select ";

			foreach ( var col in resourceModel.EntityModel.Columns)
            {
				if (firstColumn)
					firstColumn = false;
				else
				{
						query += ", ";
				}

				query += col.ColumnName;
            }

			query += " from ";
			query += resourceModel.EntityModel.TableName;

			firstColumn = true;

			using ( var connection = new SqlConnection(connectionString))
            {
				connection.Open();

				using ( var command = new SqlCommand(query, connection))
                {
					using (var reader = command.ExecuteReader())
                    {
						while ( reader.Read())
                        {
							if ( firstColumn )
                            {
								firstColumn = false;
                            }
							else
                            {
								results.AppendLine(",");
								results.AppendLine();
                            }

							if ( string.Equals(resourceModel.EntityModel.Columns[0].DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetByte(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(resourceModel.EntityModel.Columns[0].DBDataType, "smallint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt16(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(resourceModel.EntityModel.Columns[0].DBDataType, "int", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt32(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(resourceModel.EntityModel.Columns[0].DBDataType, "bigint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt64(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
						}
					}
                }
            }

			results.AppendLine();

			results.AppendLine("\t}");

			return results.ToString();
		}

		/// <summary>
		/// Extracts the base URL for the resource
		/// </summary>
		/// <param name="profileMap"></param>
		/// <returns></returns>
		private static string ExtractBaseUrl(ProfileMap profileMap)
        {
			var baseUrl = "";
			var map = profileMap.ResourceProfiles.FirstOrDefault(rp => string.Equals(rp.ResourceColumnName, "href", StringComparison.OrdinalIgnoreCase));

			if ( map != null )
            {
				var mapFunction = map.MapFunction;
				var indexStart = mapFunction.ToLower().IndexOf("$\"");
				var indexEnd = mapFunction.ToLower().IndexOf("/id");
				baseUrl = mapFunction.Substring(indexStart+2, indexEnd - indexStart-2);
            }

			return baseUrl;
        }

        private static void EmitSingleModel(string prefix, ResourceModel resourceModel, ProfileMap profileMap, StringBuilder results, JObject entityJson)
        {
			bool first = true;
			results.AppendLine($"new {resourceModel.ClassName} {{");

			foreach (var map in profileMap.ResourceProfiles)
            {
                if (first)
                    first = false;
                else
                    results.AppendLine(",");

                results.Append($"{prefix}\t\t\t\t{map.ResourceColumnName} = ");
				results.Append(ResolveMapFunction(entityJson, map.ResourceColumnName, resourceModel, map.MapFunction));
			}

			results.AppendLine();
			results.Append($"{prefix}\t\t\t}}");
        }

		private static string ResolveMapFunction(JObject entityJson, string columnName, ResourceModel model, string mapFunction)
        {
            bool isDone = false;
            var originalMapFunction = mapFunction;
            var valueNumber = 1;
            List<string> valueAssignments = new List<string>();

			var simpleConversion = ExtractSimpleConversion(entityJson, model, mapFunction);

			if (!string.IsNullOrWhiteSpace(simpleConversion))
				return simpleConversion;

			var wellKnownConversion = ExtractWellKnownConversion(entityJson, model, mapFunction);

			if (!string.IsNullOrWhiteSpace(wellKnownConversion))
				return wellKnownConversion;

			while (!isDone)
            {
                var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

                if (ef.Success)
                {
                    var entityColumnReference = ef.Groups["entity"];
                    var textToReplace = ef.Groups["replace"];
                    var token = entityJson[entityColumnReference.Value];

                    var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(entityColumnReference.Value, StringComparison.OrdinalIgnoreCase));
                    var resourceColumn = model.Columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                    switch (entityColumn.ModelDataType.ToLower())
                    {
                        case "bool":
                            switch (token.Type)
                            {
                                case JTokenType.Boolean:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<bool>().ToString().ToLower()};");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        case "bool?":
                            switch (token.Type)
                            {
                                case JTokenType.Boolean:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<bool>().ToString().ToLower()};");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                case JTokenType.Null:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        case "int":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<int>()};");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}}}");
                                    break;
                            }
                            break;

                        case "int?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = {token.Value<int>()};");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                case JTokenType.Null:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        case "string":
                            switch (token.Type)
                            {
                                case JTokenType.String:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = \"{token.Value<string>()}\";");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                case JTokenType.Null:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        case "datetime":
                            switch (token.Type)
                            {
                                case JTokenType.Date:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = DateTime.Parse(\"{token.Value<DateTime>():O}\");");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = string.Empty;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        case "datetime?":
                            switch (token.Type)
                            {
                                case JTokenType.Date:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = DateTime.Parse(\"{token.Value<DateTime>():O}\");");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                case JTokenType.Null:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = null;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;

                                default:
                                    valueAssignments.Add($"{entityColumn.ModelDataType} Value{valueNumber} = default;");
                                    mapFunction = mapFunction.Replace(textToReplace.Value, $"Value{valueNumber}");
                                    break;
                            }
                            break;

                        default:
                            return "default";
                    }

                    valueNumber++;
                }
                else
                    isDone = true;
            }

            StringBuilder results = new StringBuilder();
            results.Append("MapFrom(() => {");
            foreach (var assignment in valueAssignments)
                results.Append($"{assignment} ");
            results.Append($" return {mapFunction};");
            results.Append("})");

            return results.ToString();
        }

		private static string ExtractWellKnownConversion(JObject entityJson, ResourceModel model, string mapFunction)
		{
			var ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

			if (ef.Success)
			{
				var token = entityJson[ef.Groups["entity"].Value];
				var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));
				var replaceText = ef.Groups["replace"].Value;

				var seek = $"{replaceText}\\.HasValue[ \t]*\\?[ \t]*\\(TimeSpan\\?\\)[ \t]*TimeSpan\\.FromSeconds[ \t]*\\([ \t]*\\(double\\)[ \t]*{replaceText}[ \t]*\\)[ \t]*\\:[ \t]*null";

				var sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "byte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<byte>()})";
							}
							break;

						case "sbyte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<sbyte>()})";
							}
							break;

						case "short":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<short>()})";
							}
							break;

						case "ushort":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ushort>()})";
							}
							break;

						case "int":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<int>()})";
							}
							break;

						case "uint":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<uint>()})";
							}
							break;

						case "long":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<long>()})";
							}
							break;

						case "ulong":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ulong>()})";
							}
							break;
					}
				}

				seek = $"TimeSpan\\.FromSeconds[ \t]*\\([ \t]*\\(double\\)[ \t]*{replaceText}[ \t]*\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					switch (entityColumn.ModelDataType.ToLower())
					{
						case "byte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<byte>()})";
							}
							break;

						case "sbyte":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<sbyte>()})";
							}
							break;

						case "short":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<short>()})";
							}
							break;

						case "ushort":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ushort>()})";
							}
							break;

						case "int":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<int>()})";
							}
							break;

						case "uint":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<uint>()})";
							}
							break;

						case "long":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<long>()})";
							}
							break;

						case "ulong":
							if (token.Type == JTokenType.Integer)
							{
								return $"TimeSpan.FromSeconds({token.Value<ulong>()})";
							}
							break;
					}
				}

				seek = $"string\\.IsNullOrWhiteSpace\\({replaceText}\\)[ \t]*\\?[ \t]*null[ \t]*\\:[ \t]*new[ \t]*Uri\\({replaceText}\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "string":
							if (token.Type == JTokenType.String)
							{
								return $"new Uri(\"{token.Value<string>()}\")";
							}
							break;
					}
				}


				seek = $"{replaceText}\\.HasValue[ \t]+\\?[ \t]*\\(DateTimeOffset\\?\\)[ \t]*new[ \t]+DateTimeOffset\\([ \t]*{replaceText}(\\.Value){{0,1}}[ \t]*\\)[ \t]*\\:[ \t]*null";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "DateTime?":
							if (token.Type == JTokenType.Date)
							{
								var DateTimeValue = token.Value<DateTime>();
								var DateTimeOffsetValue = new DateTimeOffset(DateTimeValue);
								return $"DateTimeOffset.Parse({DateTimeOffsetValue.ToString():O})";
							}
							break;
					}
				}

				seek = $"new[ \t]+DateTimeOffset\\([ \t]*{replaceText}[ \t]*\\)";

				sf = Regex.Match(mapFunction, seek);

				if (sf.Success)
				{
					if (token.Type == JTokenType.Null)
						return "null";

					switch (entityColumn.ModelDataType.ToLower())
					{
						case "datetime":
							if (token.Type == JTokenType.Date)
							{
								var DateTimeValue = token.Value<DateTime>();
								var DateTimeOffsetValue = new DateTimeOffset(DateTimeValue);
								var dtString = DateTimeOffsetValue.ToString("O");
								return $"DateTimeOffset.Parse(\"{dtString}\")";
							}
							break;
					}
				}
			}

			return string.Empty;
		}

		private static string ExtractSimpleConversion(JObject entityJson, ResourceModel model, string mapFunction)
        {
            var  ef = Regex.Match(mapFunction, "(?<replace>source\\.(?<entity>[a-zA-Z0-9_]+))");

            if (ef.Success)
            {
                if (mapFunction.Equals(ef.Groups["replace"].Value))
                {
                    var token = entityJson[ef.Groups["entity"].Value];
                    var entityColumn = model.EntityModel.Columns.FirstOrDefault(c => c.ColumnName.Equals(ef.Groups["entity"].Value, StringComparison.OrdinalIgnoreCase));

                    switch (entityColumn.ModelDataType.ToLower())
                    {
                        case "bool":
                        case "bool?":
                            switch (token.Type)
                            {
                                case JTokenType.Boolean:
                                    return token.Value<bool>().ToString().ToLower();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "byte":
                        case "byte?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<byte>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "sbyte":
                        case "sbyte?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<sbyte>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "short":
                        case "short?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<short>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "ushort":
                        case "ushort?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<ushort>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "int":
                        case "int?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<int>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "uint":
                        case "uint?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<uint>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "long":
                        case "long?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<long>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "ulong":
                        case "ulong?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<ulong>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "float":
                        case "float?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<float>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "double":
                        case "double?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<double>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "decimal":
                        case "decimal?":
                            switch (token.Type)
                            {
                                case JTokenType.Integer:
                                    return token.Value<decimal>().ToString();

                                case JTokenType.Null:
                                    return "null";

                                default:
                                    return "default";
                            }

                        case "string":
                            switch (token.Type)
                            {
                                case JTokenType.String:
                                    return $"\"{token.Value<string>()}\"";

                                case JTokenType.Null:
                                    return "string.Empty";

                                default:
                                    return "string.Empty";
                            }

                        case "Guid":
                            switch (token.Type)
                            {
                                case JTokenType.Guid:
                                    return $"Guid.Parse(\"{token.Value<Guid>()}\")";

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }

                        case "DateTime":
                        case "DateTime?":
                            switch (token.Type)
                            {
                                case JTokenType.Date:
                                    return $"DateTime.Parse(\"{token.Value<DateTime>().ToString():O}\")";

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }

                        case "DateTimeOffset":
                        case "DateTimeOffset?":
                            switch (token.Type)
                            {
                                case JTokenType.Date:
                                    return $"DateTimeOffset.Parse(\"{token.Value<DateTimeOffset>().ToString():O}\")";

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }

                        case "TimeSpan":
                        case "TimeSpan?":
                            switch (token.Type)
                            {
                                case JTokenType.TimeSpan:
                                    return $"TimeSpan.Parse(\"{token.Value<TimeSpan>()}\")";

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }

                        case "byte[]":
                        case "IEnumerable<byte>":
                            switch (token.Type)
                            {
                                case JTokenType.String:
                                    return $"Convert.FromBase64String(\"{token.Value<string>()}\").ToArray()";

                                case JTokenType.Bytes:
                                    {
                                        var theBytes = token.Value<byte[]>();
                                        var str = Convert.ToBase64String(theBytes);
                                        return $"Convert.FromBase64String(\"{str}\").ToArray()";
                                    }

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }

                        case "List<byte>":
                            switch (token.Type)
                            {
                                case JTokenType.String:
                                    return $"Convert.FromBase64String(\"{token.Value<string>()}\").ToList()";

                                case JTokenType.Bytes:
                                    {
                                        var theBytes = token.Value<byte[]>();
                                        var str = Convert.ToBase64String(theBytes);
                                        return $"Convert.FromBase64String(\"{str}\").ToList()";
                                    }

                                case JTokenType.Null:
                                    return null;

                                default:
                                    return "default";
                            }
                    }
                }
            }

			return string.Empty;
        }

        public string GetExampleModel(int skipRecords, ResourceModel resourceModel, DBServerType serverType, string connectionString)
        {
			if (serverType == DBServerType.MYSQL)
				return GetMySqlExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.POSTGRESQL)
				return GetPostgresExampleModel(skipRecords, resourceModel, connectionString);
			else if (serverType == DBServerType.SQLSERVER)
				return GetSQLServerExampleModel(skipRecords, resourceModel, connectionString);

			throw new ArgumentException("Invalid or unrecognized DBServerType", "serverType");
        }

		public string GetMySqlExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public string GetPostgresExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			throw new NotImplementedException();
		}

		public string GetSQLServerExampleModel(int skipRecords, ResourceModel resourceModel, string connectionString)
		{
			StringBuilder results = new StringBuilder();

			using ( var connection = new SqlConnection(connectionString))
            {
				connection.Open();

				var query = new StringBuilder();
                query.Append("select ");

				bool first = true;
				foreach ( var column in resourceModel.EntityModel.Columns)
                {
					if ( first )
                    {
						first = false;
                    }
					else
                    {
						query.Append(',');
                    }

					query.Append($"[{column.ColumnName}]");
                }

				if ( string.IsNullOrWhiteSpace(resourceModel.EntityModel.SchemaName))
                {
					query.Append($" from [{resourceModel.EntityModel.TableName}]");
                }
				else
                {
					query.Append($" from [{resourceModel.EntityModel.SchemaName}].[{resourceModel.EntityModel.TableName}]");
				}

				query.Append(" order by ");

				first = true;
				foreach (var column in resourceModel.EntityModel.Columns)
				{
					if (column.IsPrimaryKey)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							query.Append(',');
						}

						query.Append($"[{column.ColumnName}]");
					}
				}

				query.Append($" OFFSET {skipRecords} ROWS");
				query.Append(" FETCH NEXT 1 ROWS ONLY;");

				results.AppendLine("{");

				using (var command = new SqlCommand(query.ToString(), connection))
				{
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							first = true;
							foreach (var column in resourceModel.EntityModel.Columns)
							{
								if (first)
									first = false;
								else
									results.AppendLine(",");
								results.Append($"\t\"{column.ColumnName}\": ");

								switch (column.DBDataType.ToLower())
								{
									case "bigint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetInt64(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "binary":
									case "image":
									case "timestamp":
									case "varbinary":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var length = reader.GetBytes(0, -1, null, 1, 1);
											var byteBuffer = new byte[length];
											reader.GetBytes(0, 0, byteBuffer, 0, (int)length);
											var Value = Convert.ToBase64String(byteBuffer);
											results.Append($"{Value}");
										}
										break;

									case "bit":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
											results.Append("null");
										else
										{
											var Value = reader.GetBoolean(reader.GetOrdinal(column.ColumnName));
											results.Append(Value ? "true" : "false");
										}
										break;

									case "date":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTime(reader.GetOrdinal(column.ColumnName));
											results.Append("\"{date.ToShortDateString()}\"");
										}
										break;

									case "datetime":
									case "datetime2":
									case "smalldatetime":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTime(reader.GetOrdinal(column.ColumnName));
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "datetimeoffset":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var date = reader.GetDateTimeOffset(reader.GetOrdinal(column.ColumnName));
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "decimal":
									case "money":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetDecimal(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "float":
									case "real":
									case "smallmoney":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetFloat(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "int":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
											results.Append("null");
										else
										{
											var Value = reader.GetInt32(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "smallint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetInt16(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "tinyint":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetByte(reader.GetOrdinal(column.ColumnName));
											results.Append($"{Value}");
										}
										break;

									case "time":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else
										{
											var Value = reader.GetTimeSpan(reader.GetOrdinal(column.ColumnName));
											results.Append($"\"{Value}\"");
										}
										break;

									case "text":
									case "nvarchar":
									case "ntext":
									case "char":
									case "nchar":
									case "varchar":
									case "xml":
										if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
										{
											results.Append("null");
										}
										else if (string.Equals(column.DBDataType, "hierarchyid", StringComparison.OrdinalIgnoreCase))
										{
											var theValue = reader.GetFieldValue<object>(reader.GetOrdinal(column.ColumnName));
											theValue = theValue.ToString().Replace("/", "-");
											results.Append($"\"{theValue}\"");
										}
										else
										{
											var Value = reader.GetString(reader.GetOrdinal(column.ColumnName));
											results.Append($"\"{Value}\"");
										}
										break;

									default:
										throw new InvalidDataException($"Unrecognized database type: {column.ModelDataType}");
								}
							}
						}
						else
						{
							first = true;
							foreach (var column in resourceModel.EntityModel.Columns)
							{
								if (first)
									first = false;
								else
									results.AppendLine(",");
								results.Append($"\t\"{column.ColumnName}\": ");

								switch (column.DBDataType.ToLower())
								{
									case "bigint":
										results.Append("100");
										break;

									case "binary":
									case "image":
									case "timestamp":
									case "varbinary":
										{
											var str = "The cow jumped over the moon";
											var buffer = Encoding.UTF8.GetBytes(str);
											var str2 = Convert.ToBase64String(buffer);
											results.Append($"{str2}");
										}
										break;

									case "bit":
										results.Append("true");
										break;

									case "date":
										{
											var date = DateTime.Now; ;
											results.Append("\"{date.ToShortDateString()}\"");
										}
										break;

									case "datetime":
									case "datetime2":
									case "smalldatetime":
										{
											var date = DateTime.Now;
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "datetimeoffset":
										{ 
											var date = DateTimeOffset.Now;
											var Value = date.ToString("o");
											results.Append($"\"{Value}\"");
										}
										break;

									case "decimal":
									case "money":
									case "float":
									case "real":
									case "smallmoney":
 										{
											var Value = 124.32;
											results.Append($"{Value}");
										}
										break;

									case "int":
									case "smallint":
									case "tinyint":
										results.Append("10");
										break;

									case "time":
										{
											var Value = TimeSpan.FromSeconds(24541);
											results.Append($"\"{Value}\"");
										}
										break;

									case "text":
									case "nvarchar":
									case "ntext":
									case "char":
									case "nchar":
									case "varchar":
									case "xml":
										{
											var Value = "A string value";
											results.Append($"\"{Value}\"");
										}
										break;

									default:
										throw new InvalidDataException($"Unrecognized database type: {column.ModelDataType}");
								}
							}
						}
					}
				}

				results.AppendLine();
				results.AppendLine("}");

			}

			return results.ToString();
		}

		public string EmitMappingModel(ResourceModel resourceModel, EntityModel entityModel, ProfileMap profileMap, string mappingClassName, Dictionary<string, string> replacementsDictionary)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			var ImageConversionRequired = false;
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceModel.ClassName);

			results.AppendLine("\t//=============================================================================");
			results.AppendLine("\t//\t*** GENERATED CODE ***");
			results.AppendLine("\t//");
			results.AppendLine("\t//\tDO NOT MODIFY this code. This code was generated by COFRS.");
			results.AppendLine("\t//\tUse the COFRS AutoMapper Profile Generator to change the mapping for these");
			results.AppendLine("\t//\tobjects; otherwise, COFRS will be unable to generate the cooresponding");
			results.AppendLine("\t//\tvalidators and sample objects for you.");
			results.AppendLine("\t//=============================================================================");
			results.AppendLine();

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
			results.AppendLine($"\t\t\t//\tGets the root URL of the service from the configuration settings.");
			results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");

			#region Create the Resource to Entity Mapping
			results.AppendLine();
			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a {resourceModel.ClassName} model instance (the source)");
			results.AppendLine($"\t\t\t//\tinto a {entityModel.ClassName} model instance (the destination).");
			results.AppendLine($"\t\t\tCreateMap<{resourceModel.ClassName}, {entityModel.ClassName}>()");

			bool first = true;

			foreach ( var resourceMap in profileMap.EntityProfiles)
            {
				if (first)
					first = false;
				else
					results.AppendLine("))");

				results.Append($"\t\t\t\t.ForMember(destination => destination.{resourceMap.EntityColumnName}, opts => opts.MapFrom(source =>");
				results.Append(resourceMap.MapFunction);
            }

			results.AppendLine("));");
			results.AppendLine();

			#endregion

			#region Create Entity to Resource Mapping

			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a {entityModel.ClassName} model instance (the source)");
			results.AppendLine($"\t\t\t//\tinto a {resourceModel.ClassName} model instance (the destination).");
			results.AppendLine($"\t\t\tCreateMap<{entityModel.ClassName}, {resourceModel.ClassName}>()");

			first = true;

			for ( int j = 0; j < profileMap.ResourceProfiles.Count(); j++)
            {
				if ( first )
                {
					first = false;
					results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
					results.Append(profileMap.ResourceProfiles[j].MapFunction);
				}
				else
                {
					if (profileMap.ResourceProfiles[j].ResourceColumnName.CountOf('.') > 0)
                    {
						j = GenerateChildMappings(results, j, profileMap.ResourceProfiles, profileMap.ResourceProfiles[j].ResourceColumnName.GetBaseColumn(), ref ImageConversionRequired);

                        if (j < profileMap.ResourceProfiles.Count())
                        {
                            results.AppendLine("))");
                            results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
                            results.Append(profileMap.ResourceProfiles[j].MapFunction);
                        }
                    }
					else
                    {
						results.AppendLine("))");
						results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
						results.Append(profileMap.ResourceProfiles[j].MapFunction);
					}
				}
			}

			results.AppendLine("));");
			results.AppendLine();

			#endregion

			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a collection of {entityModel.ClassName} model instances (the source)");
			results.AppendLine($"\t\t\t//\tinto a collection of {resourceModel.ClassName} model instances (the destination).");
			results.AppendLine($"\t\t\tCreateMap<RqlCollection<{entityModel.ClassName}>, RqlCollection<{resourceModel.ClassName}>>()");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.HRef, opts => opts.MapFrom(source => new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{source.HRef.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.First, opts => opts.MapFrom(source => source.First == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{source.First.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.Next, opts => opts.MapFrom(source => source.Next == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{source.Next.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.Previous, opts => opts.MapFrom(source => source.Previous == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{source.Previous.Query}}\")));");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private int GenerateChildMappings(StringBuilder results, int j, List<ResourceProfile> resourceProfiles, string baseColumn, ref bool ImageConversionRequired)
        {
			var baseCount = baseColumn.CountOf('.');
			var previousCount = resourceProfiles[j].ResourceColumnName.CountOf('.');
			results.AppendLine(" {");

			bool first = true;

			while ( j < resourceProfiles.Count() - 1 && resourceProfiles[j].ResourceColumnName.CountOf('.') > baseCount)
            {
				var resourceCount = resourceProfiles[j].ResourceColumnName.CountOf('.');

				if (resourceCount > previousCount)
                {
					j = GenerateChildMappings(results, j, resourceProfiles, resourceProfiles[j].ResourceColumnName.GetBaseColumn(), ref ImageConversionRequired);

					if (j < resourceProfiles.Count() - 1)
					{
						results.AppendLine("))");
						results.Append($"\t\t\t\t.ForMember(destination => destination.{resourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
						results.Append(resourceProfiles[j].MapFunction);
					}
				}
				else
                {
					if (first)
						first = false;
					else
						results.AppendLine(",");

					results.Append("\t\t\t\t\t");

					for (int q = 0; q < resourceCount; q++)
						results.Append("\t");

					var parts = resourceProfiles[j].ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
					results.Append($"{parts[parts.Count() - 1]} = {resourceProfiles[j].MapFunction}");
                }

				previousCount = resourceCount;
				j++;
			}

			results.AppendLine();
			results.Append("\t\t\t\t\t}");

			return j;
        }

		/// <summary>
		/// Emits the mapping model
		/// </summary>
		/// <param name="serverType"></param>
		/// <param name="resourceClassFile"></param>
		/// <param name="entityClassFile"></param>
		/// <param name="mappingClassName"></param>
		/// <param name="replacementsDictionary"></param>
		/// <returns></returns>
		public string EmitMappingModel(EntityClassFile entityClassFile, ResourceClassFile resourceClassFile, string mappingClassName, Dictionary<string, string> replacementsDictionary)
		{
			var ImageConversionRequired = false;
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceClassFile.ClassName);

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
			results.AppendLine("\t\t\tvar rootUrl = Startup.AppConfig.GetRootUrl();");

			#region Create the Resource to Entity Mapping
			results.AppendLine();
			results.AppendLine($"\t\t\tCreateMap<{resourceClassFile.ClassName}, {entityClassFile.ClassName}>()");

			bool first = true;

			//	Emit known mappings
			foreach (var member in resourceClassFile.Members)
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

						string dataType = entityColumn.ModelDataType;

						if (ix == 0)
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(e => e.{member.ResourceMemberName}.GetId<{dataType}>()))");
						else
							results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(e => e.{member.ResourceMemberName}.GetId<{dataType}>({ix})))");
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

						string dataType = entityColumn.ModelDataType;

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
			foreach (var member in resourceClassFile.Members)
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
			results.AppendLine($"\t\t\tCreateMap<{entityClassFile.ClassName}, {resourceClassFile.ClassName}>()");

			//	Emit known mappings
			first = true;
			var activeDomainMembers = resourceClassFile.Members.Where(m => !string.IsNullOrWhiteSpace(m.ResourceMemberName) && CheckMapping(m));

			foreach (var member in activeDomainMembers)
			{
				if (member.EntityNames.Count > 0 && member.EntityNames[0].IsPrimaryKey)
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(e => new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}/id");
					foreach (var entityColumn in member.EntityNames)
					{
						results.Append($"/{{e.{entityColumn.ColumnName}}}");
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
					EmitEntityToResourceMapping(results, member, ref first, ref ImageConversionRequired);
				}
			}
			results.AppendLine(";");

			var inactiveDomainMembers = resourceClassFile.Members.Where(m => !string.IsNullOrWhiteSpace(m.ResourceMemberName) && !CheckMapping(m));

			//	Emit To Do for unknown Mappings
			foreach (var member in inactiveDomainMembers)
			{
				results.AppendLine($"\t\t\t\t//\tTo do: Write mapping for {member.ResourceMemberName}");
			}
			results.AppendLine();
			#endregion

			results.AppendLine($"\t\t\tCreateMap<RqlCollection<{entityClassFile.ClassName}>, RqlCollection<{resourceClassFile.ClassName}>>()");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Href, opts => opts.MapFrom(src => new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Href.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.First, opts => opts.MapFrom(src => src.First == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.First.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Next, opts => opts.MapFrom(src => src.Next == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Next.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(dest => dest.Previous, opts => opts.MapFrom(src => src.Previous == null ? null : new Uri($\"{{rootUrl}}/{nn.PluralCamelCase}{{src.Previous.Query}}\")));");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
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

		private void EmitEntityToResourceMapping(StringBuilder results, ClassMember member, ref bool first, ref bool ImageConversionRequired)
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

					results.AppendLine($"\t\t\t\t.ForMember(destination => destination.{member.ResourceMemberName}, opts => opts.MapFrom(source =>");
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

						results.AppendLine($"\t\t\t\t.ForMember(destination => destination.{member.ResourceMemberName}, opts => opts.MapFrom(source =>");
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

				if (!string.Equals(entityColumn.ModelDataType, member.ResourceMemberType, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(entityColumn.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "string", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(destination => destination.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : new string(src.{entityColumn.EntityName})))");
						else
							results.Append($"\t\t\t\t.ForMember(destination => destination.{member.ResourceMemberName}, opts => opts.MapFrom( src => new string(src.{entityColumn.EntityName})))");
					}
					else if (string.Equals(entityColumn.ModelDataType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "char[]", StringComparison.OrdinalIgnoreCase))
					{
						if (first)
							first = false;
						else
							results.AppendLine();

						if (entityColumn.IsNullable)
							results.Append($"\t\t\t\t.ForMember(destination => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName} == null ? null : src.{entityColumn.EntityName}.ToArray()))");
						else
							results.Append($"\t\t\t\t.ForMember(destination => dest.{member.ResourceMemberName}, opts => opts.MapFrom( src => src.{entityColumn.EntityName}.ToArray()))");
					}
					else if (string.Equals(entityColumn.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "byte[]", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Image", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTimeOFfset", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTime", StringComparison.OrdinalIgnoreCase))
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
				else if (string.Equals(entityColumn.ModelDataType, "BitArray", StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					results.Append($"\t\t\t\t.ForMember(dest => dest.{member.ResourceMemberName}, opts => opts.MapFrom(src => src.{entityColumn.EntityName} ?? new System.Collections.BitArray(Array.Empty<bool>())))");
				}
				else if (!string.Equals(member.ResourceMemberName, member.EntityNames[0].ColumnName, StringComparison.OrdinalIgnoreCase))
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

				if (!string.Equals(entityColumn.ModelDataType, member.ResourceMemberType, StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(entityColumn.ModelDataType, "char[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "string", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "char[]", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "string", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Uri", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "Image", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "byte[]", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "byte[]", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "Image", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "DateTime", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
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
					else if (string.Equals(entityColumn.ModelDataType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase) && string.Equals(member.ResourceMemberType, "DateTime", StringComparison.OrdinalIgnoreCase))
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
				}
				else if (string.Equals(entityColumn.ModelDataType, "BitArray", StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					if (string.IsNullOrWhiteSpace(prefix))
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName} ?? new System.Collections.BitArray(Array.Empty<bool>())))");
					}
					else
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix}.{member.ResourceMemberName} ?? new System.Collections.BitArray(Array.Empty<bool>())))");
					}
				}
				else if (string.Equals(entityColumn.ModelDataType, "Uri", StringComparison.OrdinalIgnoreCase))
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
				else if (!string.Equals(member.ResourceMemberName, entityColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
				{
					if (first)
						first = false;
					else
						results.AppendLine();

					if (string.IsNullOrWhiteSpace(prefix))
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{member.ResourceMemberName}))");
					}
					else
					{
						results.Append($"\t\t\t\t.ForMember(dest => dest.{entityColumn.EntityName}, opts => opts.MapFrom(src => src.{prefix} == null ? null : src.{prefix}.{member.ResourceMemberName}))");
					}
				}
			}
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

					if (string.Equals(entityMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
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


		/// <summary>
		/// Emits an entity data model based upon the fields contained within the database table
		/// </summary>
		/// <param name="serverType">The type of server used to house the table</param>
		/// <param name="table">The name of the database table</param>
		/// <param name="entityClassName">The class name for the model?</param>
		/// <param name="columns">The list of columns contained in the database</param>
		/// <param name="replacementsDictionary">List of replacements key/value pairs for the solution</param>
		/// <param name="connectionString">The connection string to connect to the database, if necessary</param>
		/// <returns>A model of the entity data table</returns>
		public string EmitEntityModel(EntityModel entityModel, EntityMap entityMap, Dictionary<string, string> replacementsDictionary)
		{
			var result = new StringBuilder();
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");
			replacementsDictionary.Add("$barray$", "false");

			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{entityModel.ClassName}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(entityModel.SchemaName))
				result.AppendLine($"\t[Table(\"{entityModel.TableName}\", DBType = \"{entityModel.ServerType}\")]");
			else
				result.AppendLine($"\t[Table(\"{entityModel.TableName}\", Schema = \"{entityModel.SchemaName}\", DBType = \"{entityModel.ServerType}\")]");

			result.AppendLine($"\tpublic class {entityModel.ClassName}");
			result.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var column in entityModel.Columns)
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
					result.Append($", ForeignTableName=\"{column.ForeignTableName}\"");
				}

				AppendNullable(result, column.IsNullable, ref first);

				if (entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NVarChar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NChar", StringComparison.OrdinalIgnoreCase))
				{
					if (column.Length > 1)
						AppendFixed(result, column.Length, true, ref first);
				}

				else if (entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NText", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Name", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Varchar", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)))
				{
					if (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Varbit", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Citext", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Text", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Char", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "String", StringComparison.OrdinalIgnoreCase)))
				{
					//	Insert the column definition
					if (entityModel.ServerType == DBServerType.POSTGRESQL)
					{
						if (string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
						else if (string.Equals(column.DBDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
					}
					else if (entityModel.ServerType == DBServerType.MYSQL)
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

				else if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Bytea", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)) ||
						 (entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Timestamp", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
					AppendAutofield(result, ref first);
				}

				if ((entityModel.ServerType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Decimal", StringComparison.OrdinalIgnoreCase)) ||
					(entityModel.ServerType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Decimal", StringComparison.OrdinalIgnoreCase)) ||
					(entityModel.ServerType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Numeric", StringComparison.OrdinalIgnoreCase)))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, entityModel.ServerType, column, ref first);
				AppendEntityName(result, column, ref first);

				if (entityModel.ServerType == DBServerType.POSTGRESQL)
				{
					if (string.Equals(column.DBDataType, "Inet", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && column.Length > 1)
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Point", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Line", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Path", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";
				}
				else if (entityModel.ServerType == DBServerType.SQLSERVER)
				{
					if (string.Equals(column.DBDataType, "Image", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$image$"] = "true";
				}

				result.AppendLine(")]");

				//	Insert the column definition
				if (entityModel.ServerType == DBServerType.POSTGRESQL)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
				else if (entityModel.ServerType == DBServerType.MYSQL)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
				else if (entityModel.ServerType == DBServerType.SQLSERVER)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		/// <summary>
		/// Generates a resource model from a given entity model
		/// </summary>
		/// <param name="resourceModel">The <see cref="ResourceModel"/> to generate</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		/// <returns></returns>
		public string EmitResourceModel(ResourceModel resourceModel, ResourceMap resourceMap, Dictionary<string, string> replacementsDictionary)
		{
			List<DBColumn> resourceColumns = new List<DBColumn>();

			replacementsDictionary.Add("$resourceimage$", "false");
			replacementsDictionary.Add("$resourcenet$", "false");
			replacementsDictionary.Add("$resourcenetinfo$", "false");
			replacementsDictionary.Add("$resourcebarray$", "false");
			replacementsDictionary.Add("$usenpgtypes$", "false");
			replacementsDictionary.Add("$annotations$", "false");

			var results = new StringBuilder();
			bool hasPrimary = false;

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceModel.ClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({resourceModel.EntityModel.ClassName}))]");

			if (resourceModel.EntityModel.ElementType == ElementType.Enum)
			{
				results.AppendLine($"\tpublic enum {resourceModel.ClassName}");
				results.AppendLine("\t{");

				bool firstColumn = true;
				foreach ( var member in resourceModel.EntityModel.Columns)
                {
					if (firstColumn)
						firstColumn = false;
					else
					{
						results.AppendLine(",");
						results.AppendLine();
					}

					var membername = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(member.ColumnName));

					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\t{membername}");
					results.AppendLine("\t\t///\t</summary>");
					results.Append($"\t\t{membername}");

					var resourceColumn = new DBColumn()
					{
						ColumnName = membername
					};

					resourceColumns.Add(resourceColumn);

				}
			}
			else
			{
				results.AppendLine($"\tpublic class {resourceModel.ClassName}");
				results.AppendLine("\t{");
				var foreignTableList = new List<string>();

				bool firstColumn = true;
				foreach (var member in resourceModel.EntityModel.Columns)
				{
					if (firstColumn)
						firstColumn = false;
					else
						results.AppendLine();

					if (member.IsPrimaryKey)
					{
						if (!hasPrimary)
						{
							results.AppendLine("\t\t///\t<summary>");
							results.AppendLine($"\t\t///\tThe hypertext reference that identifies the resource.");
							results.AppendLine("\t\t///\t</summary>");
							results.AppendLine($"\t\tpublic Uri HRef {{ get; set; }}");
							hasPrimary = true;
						}

						var resourceColumn = new DBColumn()
						{
							ColumnName = "HRef",
							IsPrimaryKey = member.IsPrimaryKey,
							IsForeignKey = member.IsForeignKey,
							IsComputed = member.IsComputed,
						    IsFixed = member.IsFixed,
							IsIdentity = member.IsIdentity,
							IsIndexed = member.IsIndexed,
							IsNullable = member.IsNullable,
							ModelDataType = "Uri"
						};

						resourceColumns.Add(resourceColumn);

					}
					else if (member.IsForeignKey)
					{
						//	This is a foreign key reference. Normally, that is simply an href to the table.
						//	However, there is one exception.
						//
						//	If the foreign table is a lookup table (i.e., consists of a numeric key/value pair) then 
						//	the user has the option of generating its resource model as an enum. If so, we need to 
						//	treat it as an enum here.

						//	If such a resource model exists, it will have a resource type of Enum, and it's corresponding
						//	entity model will have the same table name as the foreign table name
						var enumResourceModel = resourceMap.Maps.FirstOrDefault(m =>
									m.ResourceType == ResourceType.Enum &&
									m.EntityModel != null &&
									string.Equals(m.EntityModel.TableName, member.ForeignTableName, StringComparison.OrdinalIgnoreCase));

						if (enumResourceModel != null)
						{
							//	This is the enum version
							results.AppendLine("\t\t///\t<summary>");
							results.AppendLine($"\t\t///\tThe enum for {member.ColumnName}");
							results.AppendLine("\t\t///\t</summary>");
							results.AppendLine($"\t\tpublic {enumResourceModel.ClassName} {member.ColumnName} {{ get; set; }}");

							var resourceColumn = new DBColumn()
							{
								ColumnName = member.ColumnName,
								IsPrimaryKey = member.IsPrimaryKey,
								IsForeignKey = member.IsForeignKey,
								IsComputed = member.IsComputed,
								IsFixed = member.IsFixed,
								IsIdentity = member.IsIdentity,
								IsIndexed = member.IsIndexed,
								IsNullable = member.IsNullable,
								ModelDataType = enumResourceModel.ClassName
							};

							resourceColumns.Add(resourceColumn);
						}
						else
						{
							//	This is just the plain old foreign key reference
							if (!foreignTableList.Contains(member.ForeignTableName))
							{
								var nn = new NameNormalizer(member.ForeignTableName);
								var memberName = nn.SingleForm;

								memberName = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(memberName));

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\tA hypertext reference that identifies the associated {memberName}");
								results.AppendLine("\t\t///\t</summary>");
								results.AppendLine($"\t\tpublic Uri {memberName} {{ get; set; }}");

								foreignTableList.Add(member.ForeignTableName);

								var resourceColumn = new DBColumn()
								{
									ColumnName = memberName,
									IsPrimaryKey = member.IsPrimaryKey,
									IsForeignKey = member.IsForeignKey,
									IsComputed = member.IsComputed,
									IsFixed = member.IsFixed,
									IsIdentity = member.IsIdentity,
									IsIndexed = member.IsIndexed,
									IsNullable = member.IsNullable,
									ModelDataType = "Uri"
								};

								resourceColumns.Add(resourceColumn);
							}
						}
					}
					else
					{
						var membername = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(member.ColumnName));

						results.AppendLine("\t\t///\t<summary>");
						results.AppendLine($"\t\t///\t{membername}");
						results.AppendLine("\t\t///\t</summary>");

						if (resourceModel.ServerType == DBServerType.SQLSERVER)
						{
							if (string.Equals(member.DBDataType, "Image", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourceimage$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}
						else if (resourceModel.ServerType == DBServerType.POSTGRESQL)
						{
							if (string.Equals(member.DBDataType, "Inet", StringComparison.OrdinalIgnoreCase) ||
								string.Equals(member.DBDataType, "Cidr", StringComparison.OrdinalIgnoreCase) ||
								string.Equals(member.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcenet$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcenetinfo$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase) ||
									 (string.Equals(member.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && member.Length > 1) ||
									 string.Equals(member.DBDataType, "VarBit", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcebarray$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Point", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Path", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Line", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$usenpgtypes$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "TimeTz", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_TimeTz", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"HH:mm:ss.fffffffzzz\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}
						else if (resourceModel.ServerType == DBServerType.MYSQL)
						{
							if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}

						results.AppendLine($"\t\tpublic {member.ModelDataType} {membername} {{ get; set; }}");

						var resourceColumn = new DBColumn()
						{
							ColumnName = membername,
							IsPrimaryKey = member.IsPrimaryKey,
							IsForeignKey = member.IsForeignKey,
							IsComputed = member.IsComputed,
							IsFixed = member.IsFixed,
							IsIdentity = member.IsIdentity,
							IsIndexed = member.IsIndexed,
							IsNullable = member.IsNullable,
							ModelDataType = member.ModelDataType
						};

						resourceColumns.Add(resourceColumn);

					}
				}
			}

			results.AppendLine("\t}");

			resourceModel.Columns = resourceColumns.ToArray();
			return results.ToString();
		}

		public string EmitEntityEnum(EntityModel entityModel)
		{
			var nn = new NameNormalizer(entityModel.ClassName);
			var builder = new StringBuilder();

			builder.Clear();
			builder.AppendLine("\t///\t<summary>");
			builder.AppendLine($"\t///\tEnumerates a list of {nn.PluralForm}");
			builder.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(entityModel.SchemaName))
				builder.AppendLine($"\t[PgEnum(\"{entityModel.TableName}\")]");
			else
				builder.AppendLine($"\t[PgEnum(\"{entityModel.TableName}\", Schema = \"{entityModel.SchemaName}\")]");

			builder.AppendLine($"\tpublic enum {entityModel.ClassName}");
			builder.AppendLine("\t{");
			bool firstUse = true;

			foreach (var column in entityModel.Columns)
			{
				if (firstUse)
					firstUse = false;
				else
				{
					builder.AppendLine(",");
					builder.AppendLine();
				}

				builder.AppendLine("\t\t///\t<summary>");
				builder.AppendLine($"\t\t///\t{StandardUtils.NormalizeClassName(column.ColumnName)}");
				builder.AppendLine("\t\t///\t</summary>");
				builder.AppendLine($"\t\t[PgName(\"{column.EntityName}\")]");

				var elementName = StandardUtils.NormalizeClassName(column.ColumnName);
				builder.Append($"\t\t{elementName}");
			}

			builder.AppendLine();
			builder.AppendLine("\t}");

			return builder.ToString();
		}
		public string EmitResourceEnum(ResourceModel resourceModel)
		{
			var nn = new NameNormalizer(resourceModel.ClassName);
			var builder = new StringBuilder();

			builder.Clear();
			builder.AppendLine("\t///\t<summary>");
			builder.AppendLine($"\t///\tEnumerates a list of {nn.PluralForm}");
			builder.AppendLine("\t///\t</summary>");

			builder.AppendLine($"\t[Entity(typeof({resourceModel.EntityModel.ClassName}))]");

			builder.AppendLine($"\tpublic enum {resourceModel.ClassName}");
			builder.AppendLine("\t{");
			bool firstUse = true;

			foreach (var column in resourceModel.EntityModel.Columns)
			{
				if (firstUse)
					firstUse = false;
				else
				{
					builder.AppendLine(",");
					builder.AppendLine();
				}

				builder.AppendLine("\t\t///\t<summary>");
				builder.AppendLine($"\t\t///\t{StandardUtils.NormalizeClassName(column.ColumnName)}");
				builder.AppendLine("\t\t///\t</summary>");
				var elementName = StandardUtils.NormalizeClassName(column.ColumnName);
				builder.Append($"\t\t{elementName}");
			}

			builder.AppendLine();
			builder.AppendLine("\t}");

			return builder.ToString();
		}

		public string EmitComposite(Solution solution, EntityModel undefinedModel, string connectionString, Dictionary<string, string> replacementsDictionary, EntityMap entityMap, ref List<EntityModel> undefinedElements, ProjectFolder entityModelsFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var nn = new NameNormalizer(undefinedModel.ClassName);
			var result = new StringBuilder();

			result.Clear();
			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{undefinedModel.ClassName}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(undefinedModel.SchemaName))
				result.AppendLine($"\t[PgComposite(\"{undefinedModel.TableName}\")]");
			else
				result.AppendLine($"\t[PgComposite(\"{undefinedModel.TableName}\", Schema = \"{undefinedModel.SchemaName}\")]");

			result.AppendLine($"\tpublic class {undefinedModel.ClassName}");
			result.AppendLine("\t{");

			var candidates = new List<EntityModel>();

			foreach (var column in undefinedModel.Columns)
			{
				if (string.IsNullOrWhiteSpace(column.ModelDataType))
				{
					//	Is it already defined?
					if (entityMap.Maps.ToList().FirstOrDefault(m =>
						string.Equals(m.SchemaName, undefinedModel.SchemaName, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(m.TableName, column.DBDataType, StringComparison.OrdinalIgnoreCase)) == null)
					{
						//	It's not defined. Is is already included in the undefined list?
						if (undefinedElements.FirstOrDefault(m =>
							string.Equals(m.SchemaName, undefinedModel.SchemaName, StringComparison.OrdinalIgnoreCase) &&
							string.Equals(m.TableName, column.DBDataType, StringComparison.OrdinalIgnoreCase)) == null)
						{
							//	It's not in the list. We need to add it...
							var classFile = new EntityModel
							{
								ClassName = $"E{StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.DBDataType))}",
								SchemaName = undefinedModel.SchemaName,
								TableName = column.DBDataType,
								Namespace = entityModelsFolder.Namespace,
								ServerType = undefinedModel.ServerType,
								Folder = Path.Combine(entityModelsFolder.Folder, $"E{StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.DBDataType))}.cs"),
								ElementType = DBHelper.GetElementType(undefinedModel.SchemaName, column.DBDataType, entityMap, connectionString),
								ProjectName = undefinedModel.ProjectName
							};

							if (classFile.ElementType == ElementType.Enum)
								StandardUtils.GenerateEnumColumns(classFile, connectionString);
							else
								StandardUtils.GenerateColumns(classFile, connectionString);

							undefinedElements.Add(classFile);
						}
					}
				}
			}

			bool firstColumn = true;

			foreach (var column in undefinedModel.Columns)
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


				if (string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(column.DBDataType, "Name", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(column.DBDataType, "_Varchar", StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) ||
					     string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(column.DBDataType, "_Varbit", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(column.DBDataType, "Citext", StringComparison.OrdinalIgnoreCase) ||
					     string.Equals(column.DBDataType, "_Text", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, -1, false, ref first);
				}

					//	Insert the column definition
				else if (string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
				}
				else if (string.Equals(column.DBDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (string.Equals(column.DBDataType, "bytea", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, DBServerType.POSTGRESQL, column, ref first);
				AppendEntityName(result, column, ref first);

				if (string.Equals(column.DBDataType, "INet", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$net$"] = "true";

				else if (string.Equals(column.DBDataType, "Cidr", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$net$"] = "true";

				else if (string.Equals(column.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$netinfo$"] = "true";

				else if (string.Equals(column.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$netinfo$"] = "true";

				else if (string.Equals(column.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && column.Length > 1)
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "varbit", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "Point", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Line", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Path", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				result.AppendLine(")]");

				var memberName = StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.ColumnName));
				result.AppendLine($"\t\t[PgName(\"{column.ColumnName}\")]");

				var knownEntityModels = new EntityMap();
				var theList = entityMap.Maps.ToList();
				theList.AddRange(undefinedElements);
				knownEntityModels.Maps = theList.ToArray();

				//	Insert the column definition
				result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column)} {memberName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		public void GenerateResourceComposites(Solution solution, List<ResourceModel> undefinedModels, ProjectFolder resourceModelFolder, EntityMap entityMap, ResourceMap resourceMap)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			while ( undefinedModels.Count > 0)
            {
				var undefinedModel = undefinedModels[0];
				undefinedModels.RemoveAt(0);

				//	Generate the model
				var result = new StringBuilder();

				if (undefinedModel.EntityModel.ElementType == ElementType.Enum)
				{
					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");
					result.AppendLine($"using {undefinedModel.EntityModel.Namespace};");
					result.AppendLine();
					result.AppendLine($"namespace {undefinedModel.Namespace}");
					result.AppendLine("{");
					result.Append(EmitResourceEnum(undefinedModel));
					result.AppendLine("}");

					//	Save the model to disk
					if (!Directory.Exists(Path.GetDirectoryName(undefinedModel.Folder)))
						Directory.CreateDirectory(Path.GetDirectoryName(undefinedModel.Folder));

					File.WriteAllText(undefinedModel.Folder, result.ToString());

					//	Add the model to the project
					var parentProject = StandardUtils.GetProject(solution, undefinedModel.ProjectName);
					parentProject.ProjectItems.AddFromFile(undefinedModel.Folder);

					//	Now, it's no longer undefined. Add it to the list of defined models
					resourceMap.AddModel(undefinedModel);
				}
			}
		}

		/// <summary>
		/// Generate undefined elements
		/// </summary>
		/// <param name="undefinedEntityModels">The list of elements to be defined"/></param>
		/// <param name="connectionString">The connection string to the database server</param>
		/// <param name="rootnamespace">The root namespace for the newly defined elements</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		/// <param name="definedElements">The lise of elements that are defined</param>
		public void GenerateComposites(Solution solution, List<EntityModel> undefinedEntityModels, string connectionString, Dictionary<string, string> replacementsDictionary, EntityMap entityMap, ProjectFolder entityModelsFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	As we generate each model, the model itself may contain undefined types, which in turn need to be added
			//	to the list of undefinedEntityModels so that they too can be generated. Because of this, each successive
			//	generation might (or might not) add to the list of undefinedEntityModels.
			//
			//	This is why the undefinedEntityModels object is being passed as a reference.
			//
			//	This also means that we can't simply iterate through the list with a foreach - because that list is liable to change.
			//
			//	Therefore, we simply pop the top one off the list, treating the list as a todo stack. And we keep doing that until
			//	the stack is empty.

			while (undefinedEntityModels.Count > 0 )
			{
				var undefinedModel = undefinedEntityModels[0];
				undefinedEntityModels.RemoveAt(0);

				undefinedModel.ElementType = DBHelper.GetElementType(undefinedModel.SchemaName,
																undefinedModel.TableName,
																entityMap,
																connectionString);

				if (undefinedModel.ElementType == ElementType.Enum)
				{
					//	Has it already been previously defined? We don't want two of these...
					if (entityMap.Maps.ToList().FirstOrDefault(m => 
						string.Equals(m.SchemaName, undefinedModel.SchemaName, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(m.TableName, undefinedModel.TableName, StringComparison.OrdinalIgnoreCase)) == null)
					{
						//	Generate the model
						var result = new StringBuilder();

						result.AppendLine("using COFRS;");
						result.AppendLine("using NpgsqlTypes;");
						result.AppendLine();
						result.AppendLine($"namespace {undefinedModel.Namespace}");
						result.AppendLine("{");
						result.Append(EmitEntityEnum(undefinedModel));
						result.AppendLine("}");

						//	Save the model to disk
						if (!Directory.Exists(Path.GetDirectoryName(undefinedModel.Folder)))
							Directory.CreateDirectory(Path.GetDirectoryName(undefinedModel.Folder));

						File.WriteAllText(undefinedModel.Folder, result.ToString());

						//	Add the model to the project
						var parentProject = StandardUtils.GetProject(solution, entityModelsFolder.ProjectName);
						parentProject.ProjectItems.AddFromFile(undefinedModel.Folder);

						//	Register the composite model
						StandardUtils.RegisterComposite(solution, undefinedModel);

						//	Now, it's no longer undefined. Add it to the list of defined models
						entityMap.AddModel(undefinedModel);
					}
				}
				else if (undefinedModel.ElementType == ElementType.Composite)
				{
					//	Has it already been defined? We don't want two of these...
					if (entityMap.Maps.ToList().FirstOrDefault(m =>
						string.Equals(m.SchemaName, undefinedModel.SchemaName, StringComparison.OrdinalIgnoreCase) &&
						string.Equals(m.TableName, undefinedModel.TableName, StringComparison.OrdinalIgnoreCase)) == null)
					{
						var result = new StringBuilder();

						//	Generate the model (and any child models that might be necessary)

						var body = EmitComposite(solution,
												 undefinedModel,
												 connectionString,
												 replacementsDictionary,
												 entityMap,
												 ref undefinedEntityModels,
												 entityModelsFolder);

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
						result.AppendLine($"namespace {undefinedModel.Namespace}");
						result.AppendLine("{");
						result.Append(body);
						result.AppendLine("}");

						//	Save the model to disk
						if (!Directory.Exists(Path.GetDirectoryName(undefinedModel.Folder)))
							Directory.CreateDirectory(Path.GetDirectoryName(undefinedModel.Folder));

						File.WriteAllText(undefinedModel.Folder, result.ToString());

						//	Add the model to the project
						var parentProject = StandardUtils.GetProject(solution, entityModelsFolder.ProjectName);
						parentProject.ProjectItems.AddFromFile(undefinedModel.Folder);

						//	Register the composite model
						StandardUtils.RegisterComposite(solution, undefinedModel);

						//	Now, it's no longer undefined. Add it to the list of defined models
						entityMap.AddModel(undefinedModel);
					}
				}
			}
		}



		#region Helper Functions
		private void AppendComma(StringBuilder result, ref bool first)
		{
			if (first)
				first = false;
			else
				result.Append(", ");
		}
		private void AppendPrimaryKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsPrimaryKey = true");
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

		private void AppendDatabaseType(StringBuilder result, DBServerType serverType, DBColumn column, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"NativeDataType=\"{column.DBDataType}\"");
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

		private void AppendEntityName(StringBuilder result, DBColumn column, ref bool first)
        {
			if (!string.IsNullOrWhiteSpace(column.EntityName) && !string.Equals(column.ColumnName, column.EntityName, StringComparison.Ordinal))
			{
				if (!string.Equals(column.EntityName, column.DBDataType, StringComparison.Ordinal))
				{
					AppendComma(result, ref first);
					result.Append($"ColumnName = \"{column.EntityName}\"");
				}
			}
		}

		private void AppendPrecision(StringBuilder result, int NumericPrecision, int NumericScale, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"Precision={NumericPrecision}, Scale={NumericScale}");
		}
		#endregion
	}
}
