using COFRSCoreCommandsPackage.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSCoreCommandsPackage.Forms
{
	//--------------------------------------------------------------------------------------------------------------------------------
	//	The idea of this dialog is to present the user with a list of child resources that can be included into the main source
	//	resource as a collection.
	//
	//	Imagine that we have a resource called Assembly. An assembly is a collection of sub parts, all put together. An assemply
	//	class might look like:
	//
	//	public clsss Assembly
	//	{
	//		public Uri HRef { get; set; }
	//	    public string AssemblyName { get; set; }
	//	}
	//
	//	Now, assume in our system we also have a resource called Part. The Part is a single item that is one of the things that makes
	//	up an assembly. The Part class might look like...
	//
	//	public class Part
	//	{
	//		public Uri HRef { get; set; }
	//		public Uri Assembly { get; set; }
	//		public string PartName { get; set; }
	//	}
	//
	//	As you can see from the above classes, an individual assembly is referenced by it's HRef. Likewise, an individual part is
	//	referenced by it's HRef. So, Assembly #1 would have an HRef of /assembly/id/1, and part #7 would have an HRef of
	//	/parts/id/7.
	//
	//	Each part belongs to an assembly, and so each part has a reference to the Assembly that it belongs to. So, if Part #7 
	//	is a part for Assembly #1, then it's Assembly reference would be /assembly/id/1
	//
	//	This relationship, in database terminaology, is call a foreign key reference. The Assembly reference in the Part class is 
	//	a foreign key reference to the Assembly object.
	//
	//	Because this relationship exists, we might like to include all the parts that belong to the Assembly in the Assembly object
	//	itself. This would change the Assembly class to look like this:
	//
	//	public clsss Assembly
	//	{
	//		public Uri HRef { get; set; }
	//	    public string AssemblyName { get; set; }
	//		public Part[] Parts { get; set; }
	//	}
	//
	//	Now, you see that the assembly has a new member, which is a collection of parts.
	//
	//	That is what this dialog does. Given a class, it searches for other classes that have a foreign key reference pointing to it.
	//	It presents those "child" classes in a list to the user. 
	//	The user selects one of those classes, and this dialog modifies the original (source) class to include a collection of the
	//	child classes.
	//
	//	Of course, just adding the collection to the source class isn't enought. We also need to change the Orchestration layer to 
	//	process that collection.
	//
	//	This means, we need to modify the Orchestration Layer to...
	//
	//		GetSingle => include code to read the collection of child objects and populate the new collection
	//		GetCollection => Same as GetSingle, but we have to do it for each source resource in the collection
	//		Add => after adding a new source resource, we need to add any child resources that it contains
	//		Update => add,update or delete child resources as appropriate
	//		Patch => include code to patch any of the child objects in the array
	//		Delete => delete any child objects in the array before deleting the source resource.
	//--------------------------------------------------------------------------------------------------------------------------------
	public partial class AddCollectionDialog : Form
    {
        public DTE2 _dte2;
		private ResourceMap resourceMap;

        public AddCollectionDialog()
        {
            InitializeComponent();
        }

		/// <summary>
		/// Here we find all the resources that reference the source Resource (the one named in the ResourceName label control), and present them
		/// in the ChildResourceList list box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void OnLoad(object sender, EventArgs e)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var projectMapping = OpenProjectMapping(_dte2.Solution);

			var entityModelsFolder = projectMapping.GetEntityModelsFolder();
			var resourceModelsFolder = projectMapping.GetResourceModelsFolder();

            var connectionString = GetConnectionString(_dte2.Solution);
            var defultServerType = GetDefaultServerType(connectionString);
            var entityMap = LoadEntityModels(_dte2.Solution, entityModelsFolder);
            resourceMap = LoadResourceModels(_dte2.Solution, entityMap, resourceModelsFolder, defultServerType);

			var sourceResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase));
			var sourceTableName = sourceResourceModel.EntityModel.TableName;

			foreach ( var resourceModel in resourceMap.Maps )
            {
				if ( !resourceModel.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase) && resourceModel.EntityModel != null )
                {
					var foreignKeys = resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey && c.ForeignTableName.Equals(sourceTableName, StringComparison.OrdinalIgnoreCase));
					if (foreignKeys.Count() > 0)
					{
						var existingMember = sourceResourceModel.Columns.FirstOrDefault(c => c.ModelDataType.Equals($"IEnumerable<{resourceModel.ClassName}>", StringComparison.OrdinalIgnoreCase));
						
						if ( existingMember == null )	
							ChildResourceList.Items.Add(resourceModel.ClassName);
					}
				}
            }
        }

		private void OnOK(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (ChildResourceList.SelectedIndex != -1)
			{
				var sourceResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase));
				var memberResourceModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ChildResourceList.SelectedItem.ToString(), StringComparison.OrdinalIgnoreCase));
				var projectMapping = OpenProjectMapping(_dte2.Solution);

				var nn = new NameNormalizer(memberResourceModel.ClassName);

				var resourceModelsFolder = projectMapping.GetResourceModelsFolder();
				var validatorFolder = projectMapping.GetValidatorFolder();
				var sourceValidatorInterface = FindValidatorInterface(_dte2.Solution, validatorFolder, sourceResourceModel.ClassName);
				var memberValidatorInterface = FindValidatorInterface(_dte2.Solution, validatorFolder, memberResourceModel.ClassName);
				var sourceValidatorName = string.Empty;
				var memberValidatorName = string.Empty;
				var memberName = nn.PluralForm;


				var fileName = Path.GetFileName(sourceResourceModel.Folder);
				ProjectItem sourceResource = _dte2.Solution.FindProjectItem(fileName);

				bool wasSourceOpen = sourceResource.IsOpen[Constants.vsViewKindAny];	//	Record if it was already open

				if (!wasSourceOpen)														//	If it wasn't open, open it.
					sourceResource.Open(Constants.vsViewKindCode);

				var window = sourceResource.Open(Constants.vsViewKindTextView);			//	Get the window (so we can close it later)
				Document doc = sourceResource.Document;									//	Get the doc 
				TextSelection sel = (TextSelection)doc.Selection;						//	Get the current selection
				var activePoint = sel.ActivePoint;										//	Get the active point

				//	This is the resource class, add the new member to it...
				foreach ( CodeNamespace namespaceElement in sourceResource.FileCodeModel.CodeElements.OfType<CodeNamespace>())
                {
					foreach ( CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>() )
                    {
						var addMember = true;

						foreach ( CodeProperty2 property in classElement.Members.OfType<CodeProperty2>())
                        {
							if ( property.Type.AsString.Contains("IEnumerable") &&
								 property.Type.AsString.Contains(memberResourceModel.ClassName))
                            {
								addMember = false;
								memberName = property.Name;
							}
						}

						if (addMember)
						{
							var property = classElement.AddProperty(memberName, memberName, $"IEnumerable<{memberResourceModel.ClassName}>", -1, vsCMAccess.vsCMAccessPublic, null);
							property.DocComment = $"<doc>\r\n<summary>\r\nGets or sets the collection of <see cref=\"{memberResourceModel.ClassName}\"/> resources.\r\n</summary>\r\n</doc>";

							var editPoint = property.StartPoint.CreateEditPoint();
							editPoint.EndOfLine();
							editPoint.ReplaceText(property.EndPoint, " { get; set; }", 0);
						}
					}
				}

				//	Now that we've added a new member, we need to alter the orchestration layer to handle that new member...

				ProjectItem orchestrator = _dte2.Solution.FindProjectItem("ServiceOrchestrator.cs");

				bool wasOrchestratorOpen = sourceResource.IsOpen[Constants.vsViewKindAny];		//	Record if it was already open

				if (!wasOrchestratorOpen)														//	If it wasn't open, open it.
					sourceResource.Open(Constants.vsViewKindCode);

				var orchestratorWindow = orchestrator.Open(Constants.vsViewKindTextView);		//	Get the window (so we can close it later)
				Document orchestratorDoc = orchestrator.Document;								//	Get the doc 

				bool AddSystemText = true;
				FileCodeModel2 codeModel = (FileCodeModel2) orchestrator.FileCodeModel;

				//	The orchestration layer is going to need "using System.Text", ensure that it it does

				foreach (CodeImport usingElement in codeModel.CodeElements.OfType<CodeImport>())
				{
					if (usingElement.Namespace.Equals("System.Text", StringComparison.OrdinalIgnoreCase))
						AddSystemText = false;
				}

				if (AddSystemText)
				{
					codeModel.AddImport("System.Text", -1);
				}

				//	We're going to need a validator for the new members. To get it, we will use dependency injection in the 
				//	constructor, which means we will need a class variable. That variable is going to need a name. Create 
				//	the default name for this vairable as the class name of the new member followed by "Validator".
				//
				//	i.e., if the new member class is Foo, then the variable name will be FooValidator.

				memberValidatorName = $"{memberResourceModel.ClassName}Validator";

				//	Find the namespace...
				foreach (CodeNamespace namespaceElement in codeModel.CodeElements.OfType<CodeNamespace>())
				{
					//	Find the class...
					foreach (CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>())
					{
						//	Okay, we will need a validator for our new members. To get one, we will use dependency injection
						//	in the constructor. That means, we need a class member variable to hold it.
						//
						//	And, by the way, we're going to need to know the name of the source class validator. While we're
						//	muking around with the variables, find and store that name.

						//	So, do we already have that variable? If so, use it. If not, create it.
						//	We start by assuming we're going to create it.
						var shouldAddValidator = true;

						//	Look at all our class variables.
						//	The variable we are looking for will have a type of validator interface for the class. Fortunately,
						//	we know those names for both our source class and our new member class.
						foreach (CodeVariable2 variableElement in classElement.Children.OfType<CodeVariable2>())
                        {
							if ( variableElement.Type.AsFullName.EndsWith(sourceValidatorInterface, StringComparison.OrdinalIgnoreCase))
                            {
								//	This is a member variable that has a type of the Interface for the source class. Remember it's name.
								sourceValidatorName = variableElement.Name;
							}
							else if ( variableElement.Type.AsFullName.EndsWith(memberValidatorInterface, StringComparison.OrdinalIgnoreCase) )
                            {
								//	This is a member variable that has a type of the interface for the member class. It may (or may not)
								//	have the name we used as the default. No matter, whatever name it is using, remember it. Also, mark
								//	the flag to say we don't need to create one.
								memberValidatorName = variableElement.Name;	
								shouldAddValidator = false;
                            }
                        }

						//	Did we find it?
						if ( shouldAddValidator)
                        {
							//	Nope, didn't find it. Create it using that default variable name we created.
							var variable = (CodeVariable2) classElement.AddVariable(memberValidatorName, memberValidatorInterface, 0, vsCMAccess.vsCMAccessPrivate);
							variable.ConstKind = vsCMConstKind.vsCMConstKindReadOnly;
                        }

						//	Now, let's go though all the functions...
						foreach (CodeFunction2 aFunction in classElement.Children.OfType<CodeFunction2>())
						{
							//	Constructor
							if (aFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
							{
								//	This is the constructor function. We need that new validator, and we get it using dependency
								//	injection. That means, it needs to be an argument in the aruguement list of the constructor.
								//	
								//	If it's already there, then no problem, just move on. But if it isn't there, then we need 
								//	to add it and assign it's value to the new validator member we created (or found).
								//
								//	Let's start by assuming we're going to need to create it.
								var shouldAddArgument = true;
								var parameterName = memberValidatorName;
								parameterName = parameterName.Substring(0, 1).ToLower() + parameterName.Substring(1);

								//	Look at each argument...
								foreach (CodeParameter2 arg in aFunction.Parameters.OfType<CodeParameter2>())
								{
									//	if any one has a type of the interface for the new member, the the argument already
									//	exists, and we don't have to create it.
									if (arg.Type.AsString.EndsWith(memberValidatorInterface, StringComparison.OrdinalIgnoreCase))
									{
										parameterName = arg.Name;
										//	Set the flag to show it already exists, so we don't have to create it.
										shouldAddArgument = false;
									}
								}

								//	Did we find it?
								if (shouldAddArgument)
								{
									//	Nope, create it
									aFunction.AddParameter(parameterName, memberValidatorInterface, -1);
								}

								var editPoint = aFunction.StartPoint.CreateEditPoint();

								if (!editPoint.FindPattern($"{memberValidatorName} ="))
								{ 
									//	Now, within the function, add the assignment.
									editPoint = aFunction.EndPoint.CreateEditPoint();
									editPoint.LineUp();
									editPoint.EndOfLine();
									editPoint.Insert($"\r\n\t\t\t{memberValidatorName} = {parameterName};");
								}
							}

							//	Get Single
							else if (aFunction.Name.Equals($"Get{sourceResourceModel.ClassName}Async", StringComparison.OrdinalIgnoreCase))
							{
								//	This is the get single async method.
								var startPoint = aFunction.StartPoint;

								//	Find were it returns the GetSingleAsync (this may or may not be there)
								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

								bool foundit = editPoint.FindPattern($"return await GetSingleAsync<{sourceResourceModel.ClassName}>(node);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									//	We found it, so replace it with an assignment.
									editPoint.ReplaceText(6, "var item =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();

									//	And return that item.
									editPoint.Insert("\r\n\t\t\tvar subNode = RqlNode.Parse($\"Client=uri:\\\"{item.HRef.LocalPath}\\\"\");\r\n");
									editPoint.Insert("\r\n\t\t\treturn item;");
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern("var subNode = RqlNode");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.EndPoint.CreateEditPoint();
									editPoint.LineUp();
									editPoint.Insert("\t\t\tvar subNode = RqlNode.Parse($\"Client=uri:\\\"{item.HRef.LocalPath}\\\"\");\r\n\r\n");
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("return item");
									//	Now, just before you return the item, insert a call to get the collection of member items
									//	and populate the source item.
									editPoint.LineUp();
									editPoint.EndOfLine();
									editPoint.InsertNewLine();

									editPoint.Insert($"\t\t\tvar {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);\r\n");
									editPoint.Insert($"\t\t\titem.{memberName} = {memberResourceModel.ClassName}Collection.Items;\r\n");
								}
							}
			
							//	Get Collection
							else if (aFunction.Name.Equals($"Get{sourceResourceModel.ClassName}CollectionAsync"))
							{
								var startPoint = aFunction.StartPoint;
								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

								bool foundit = editPoint.FindPattern($"return await GetCollectionAsync<{sourceResourceModel.ClassName}>");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									editPoint.ReplaceText(6, "var collection =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("return collection;");
								}

 								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"StringBuilder rqlBody = new(\"in({sourceResourceModel.ClassName}\");");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
                                {
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("return collection");
                                    editPoint.LineUp();

                                    editPoint.InsertNewLine();
									editPoint.Indent(null,3);
									editPoint.Insert($"StringBuilder rqlBody = new(\"in({sourceResourceModel.ClassName}\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var item in collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("rqlBody.Append($\", uri:\\\"{item.HRef.LocalPath}\\\"\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"rqlBody.Append(')');");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"var subNode = RqlNode.Parse(rqlBody.ToString());");
									editPoint.InsertNewLine();
                                }

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"var {memberResourceModel.ClassName}Collection =");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if ( !foundit)
                                {
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("return collection");
                                    editPoint.LineUp();
                                    editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
                                    editPoint.Insert($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach ( var item in {memberResourceModel.ClassName}Collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"var mainItem = collection.Items.FirstOrDefault(i => i.HRef == item.Client);");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"if (mainItem.{nn.PluralForm} == null)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
                                    editPoint.Insert($"mainItem.{memberName} = new {memberResourceModel.ClassName}[] {{ item }};");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("else");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert($"mainItem.{memberName} = new List<{memberResourceModel.ClassName}>(mainItem.{memberName}) {{ item }}.ToArray();");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
								}
							}

							//	Add
							else if (aFunction.Name.Equals($"Add{sourceResourceModel.ClassName}Async"))
							{
								var startPoint = aFunction.StartPoint;
								var endPoint = aFunction.EndPoint;

								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint(); 
								
								bool foundit = editPoint.FindPattern("return await AddAsync");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									editPoint.ReplaceText(6, "item =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Insert("\t\t\treturn item;");
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								foundit = editPoint.FindPattern($"foreach ( var subitem in item.{memberName})");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern($"await {sourceValidatorName}.ValidateForAddAsync(item, User);");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach ( var subitem in item.{memberName})");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null,4);
									editPoint.Insert($"await {memberValidatorName}.ValidateForAddAsync(subitem, User);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern("item = await AddAsync");
								foundit = editPoint.FindPattern($"foreach ( var subitem in item.{memberName})");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern($"return item;");
									editPoint.LineUp();
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();

									editPoint.Insert($"\t\t\tforeach ( var subitem in item.{memberName})\r\n");
									editPoint.Insert($"\t\t\t{{\r\n");
									editPoint.Insert($"\t\t\t\tsubitem.{sourceResourceModel.ClassName} = item.HRef;\r\n");
									editPoint.Insert($"\t\t\t\tsubitem.HRef = (await AddAsync<{memberResourceModel.ClassName}>(subitem)).HRef;\r\n");
									editPoint.Insert($"\t\t\t}}\r\n");
								}
							}

							//	Update
							else if (aFunction.Name.Equals($"Update{sourceResourceModel.ClassName}Async"))
                            {
								var startPoint = aFunction.StartPoint;
								var endPoint = aFunction.EndPoint;

								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								bool foundit = editPoint.FindPattern($"return await UpdateAsync");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (foundit)
								{
									editPoint.ReplaceText(6, "item =", 0);
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Insert("\t\t\treturn item;");
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern($"await {sourceValidatorName}.ValidateForUpdateAsync");
								foundit = editPoint.FindPattern("var subNode =");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if ( !foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern($"await {sourceValidatorName}.ValidateForUpdateAsync");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Insert($"\t\t\tvar subNode = RqlNode.Parse($\"Client=uri:\\\"{{item.HRef.LocalPath}}\\\"\");\r\n");
                                }

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern("var subNode = ");
								foundit = editPoint.FindPattern($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("var subNode = ");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in {memberResourceModel.ClassName}Collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"var matchingItem = item.{memberName}.FirstOrDefault(m => m.HRef == subitem.HRef);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"if (matchingItem != null)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert($"await {memberValidatorName}.ValidateForUpdateAsync(matchingItem, User);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("else");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert("var dnode = RqlNode.Parse($\"HRef=uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert($"await {memberValidatorName}.ValidateForDeleteAsync(dnode, User);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in item.{memberName}.Where(c => c.HRef == null))");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"await {memberValidatorName}.ValidateForAddAsync(subitem, User);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
								}

								editPoint = (EditPoint2) aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern("item = await UpdateAsync");
								foundit = editPoint.FindPattern($"foreach (var subitem in {memberResourceModel.ClassName}Collection.Items)");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("item = await UpdateAsync");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in {memberResourceModel.ClassName}Collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"var matchingItem = item.{memberName}.FirstOrDefault(m => m.HRef == subitem.HRef);");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"if (matchingItem != null)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert($"await UpdateAsync<{memberResourceModel.ClassName}>(subitem);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("else");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert("var dnode = RqlNode.Parse($\"HRef = uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 5);
									editPoint.Insert($"await DeleteAsync<{memberResourceModel.ClassName}>(dnode);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in item.{memberName}.Where(c => c.HRef == null))");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"subitem.{sourceResourceModel.ClassName} = item.HRef;");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("subitem.HRef = (await AddAsync(subitem)).HRef;");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
								}
							}

							//	Delete
							else if (aFunction.Name.Equals($"Delete{sourceResourceModel.ClassName}Async"))
							{
								var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

								bool foundit = editPoint.FindPattern("var url = node.Value<Uri>(1);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if ( !foundit)
                                {
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.LineDown();
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("var url = node.Value<Uri>(1);");
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern($"await {sourceValidatorName}.ValidateForDeleteAsync");
								foundit = editPoint.FindPattern("var subNode =");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern($"await {sourceValidatorName}.ValidateForDeleteAsync");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Insert($"\t\t\tvar subNode = RqlNode.Parse($\"Client=uri:\\\"{{url.LocalPath}}\\\"\");\r\n");
								}

								editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
								editPoint.FindPattern("var subNode = ");
								foundit = editPoint.FindPattern($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
								foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

								if (!foundit)
								{
									editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
									editPoint.FindPattern("var subNode = ");
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"var {memberResourceModel.ClassName}Collection = await GetCollectionAsync<{memberResourceModel.ClassName}>(null, subNode, true);");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in {memberResourceModel.ClassName}Collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("var dnode = RqlNode.Parse($\"HRef = uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"await {memberValidatorName}.ValidateForDeleteAsync(dnode, User);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
									editPoint.InsertNewLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"foreach (var subitem in {memberResourceModel.ClassName}Collection.Items)");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("{");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert("var dnode = RqlNode.Parse($\"HRef = uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 4);
									editPoint.Insert($"await DeleteAsync<{memberResourceModel.ClassName}>(dnode);");
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert("}");
								}
							}
						}
					}
				}
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		#region Helper Functions
		public static ProjectMapping OpenProjectMapping(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var solutionPath = solution.Properties.Item("Path").Value.ToString();
			var mappingPath = Path.Combine(Path.GetDirectoryName(solutionPath), ".cofrs\\ProjectMap.json");

			try
			{
				var jsonData = File.ReadAllText(mappingPath);

				var projectMapping = JsonConvert.DeserializeObject<ProjectMapping>(jsonData, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore,
					Formatting = Formatting.Indented,
					MissingMemberHandling = MissingMemberHandling.Ignore
				});

				return projectMapping;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (DirectoryNotFoundException)
			{
				return null;
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
				return null;
			}
		}

		public static string GetConnectionString(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem settingsFile = solution.FindProjectItem("appsettings.Local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];
			Window window = settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = doc.Selection as TextSelection;

			VirtualPoint activePoint = sel.ActivePoint;
			VirtualPoint anchorPoint = sel.AnchorPoint;

			sel.SelectAll();
			var settings = JObject.Parse(sel.Text);
			var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
			string connectionString = connectionStrings["DefaultConnection"].Value<string>();

			if (!wasOpen)
				window.Close();
			else
			{
				sel.Mode = vsSelectionMode.vsSelectionModeStream;
				sel.MoveToPoint(anchorPoint);
				sel.SwapAnchor();
				sel.MoveToPoint(activePoint);
			}

			return connectionString;
		}

		public static DBServerType GetDefaultServerType(string DefaultConnectionString)
		{
			//	Get the location of the server configuration on disk
			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");

			ServerConfig _serverConfig;

			//	Read the ServerConfig into memory. If one does not exist
			//	create an empty one.
			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var streamReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(streamReader))
					{
						var serializer = new JsonSerializer();

						_serverConfig = serializer.Deserialize<ServerConfig>(reader);

						if (_serverConfig == null)
							_serverConfig = new ServerConfig();
					}
				}
			}

			//	If there are any servers in the list, we need to populate
			//	the windows controls.
			if (_serverConfig.Servers.Count() > 0)
			{
				int LastServerUsed = _serverConfig.LastServerUsed;
				//	When we populate the windows controls, ensure that the last server that
				//	the user used is in the visible list, and make sure it is the one
				//	selected.
				for (int candidate = 0; candidate < _serverConfig.Servers.ToList().Count(); candidate++)
				{
					var candidateServer = _serverConfig.Servers.ToList()[candidate];
					var candidateConnectionString = string.Empty;

					switch (candidateServer.DBType)
					{
						case DBServerType.MYSQL:
							candidateConnectionString = $"Server={candidateServer.ServerName};Port={candidateServer.PortNumber}";
							break;

						case DBServerType.POSTGRESQL:
							candidateConnectionString = $"Server={candidateServer.ServerName};Port={candidateServer.PortNumber}";
							break;

						case DBServerType.SQLSERVER:
							candidateConnectionString = $"Server={candidateServer.ServerName}";
							break;
					}

					if (DefaultConnectionString.StartsWith(candidateConnectionString))
					{
						LastServerUsed = candidate;
						break;
					}
				}

				var dbServer = _serverConfig.Servers.ToList()[LastServerUsed];
				return dbServer.DBType;
			}

			return DBServerType.SQLSERVER;
		}

		public static EntityMap LoadEntityModels(Solution solution, ProjectFolder entityModelsFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<EntityModel>();

			var entityFolderContents = FindProjectFolderContents(solution, entityModelsFolder);

			foreach (ProjectItem projectItem in entityFolderContents)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					FileCodeModel2 model = (FileCodeModel2) projectItem.FileCodeModel;

					foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
						{
							CodeAttribute tableAttribute = null;
							CodeAttribute compositeAttribute = null;

							try { tableAttribute = (CodeAttribute)classElement.Children.Item("Table"); } catch (Exception) { }
							try { compositeAttribute = (CodeAttribute)classElement.Children.Item("PgComposite"); } catch (Exception) { }

							if (tableAttribute != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.SQLSERVER;

								var match = Regex.Match(tableAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
									serverType = (DBServerType)Enum.Parse(typeof(DBServerType), match.Groups["dbtype"].Value);
								}

								var entityModel = new EntityModel
								{
									ClassName = classElement.Name,
									ElementType = ElementType.Table,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement, entityModel);
								map.Add(entityModel);
							}
							else if (compositeAttribute != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.POSTGRESQL;
								var match = Regex.Match(compositeAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
								}

								var entityModel = new EntityModel
								{
									ClassName = classElement.Name,
									ElementType = ElementType.Composite,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								entityModel.Columns = LoadColumns(classElement, entityModel);
								map.Add(entityModel);
							}
						}

						foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
						{
							CodeAttribute attributeElement = null;

							try { attributeElement = (CodeAttribute)enumElement.Children.Item("PgEnum"); } catch (Exception) { }

							if (attributeElement != null)
							{
								var entityName = string.Empty;
								var schemaName = string.Empty;
								DBServerType serverType = DBServerType.POSTGRESQL;

								var match = Regex.Match(attributeElement.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
									schemaName = match.Groups["schemaName"].Value;
								}

								var entityModel = new EntityModel
								{
									ClassName = enumElement.Name,
									ElementType = ElementType.Enum,
									Namespace = namespaceElement.Name,
									ServerType = serverType,
									SchemaName = schemaName,
									TableName = entityName,
									ProjectName = entityModelsFolder.ProjectName,
									Folder = projectItem.Properties.Item("FullPath").Value.ToString()
								};

								var columns = new List<DBColumn>();

								foreach (CodeElement enumVariable in enumElement.Children)
								{
									if (enumVariable.Kind == vsCMElement.vsCMElementVariable)
									{
										CodeAttribute pgNameAttribute = null;
										try { pgNameAttribute = (CodeAttribute)enumElement.Children.Item("PgName"); } catch (Exception) { }

										var dbColumn = new DBColumn
										{
											ColumnName = enumElement.Name,
										};

										if (pgNameAttribute != null)
										{
											var matchit = Regex.Match(pgNameAttribute.Value, "\\\"(?<pgName>[_A-Za-z][A-Za-z0-9_]*)\\\"");

											if (matchit.Success)
												dbColumn.EntityName = matchit.Groups["pgName"].Value;
										}

										columns.Add(dbColumn);
									}
								}

								entityModel.Columns = columns.ToArray();

								map.Add(entityModel);
							}
						}
					}
				}
			}

			return new EntityMap() { Maps = map.ToArray() };
		}

		private static ProjectItems FindProjectFolderContents(Solution solution, ProjectFolder projectFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Project project = FindProject(solution, projectFolder);

			if (project != null)
			{
				var rootFolder = project.Properties.Item("FullPath").Value.ToString();

				var solutionParts = rootFolder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
				var folderParts = projectFolder.Folder.Split(new char[] { ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

				if (solutionParts.Length == folderParts.Length)
					return project.ProjectItems;

				var projectItems = project.ProjectItems;
				ProjectItem folder = null;

				for (int i = solutionParts.Length; i < folderParts.Length; i++)
				{
					foreach (ProjectItem item in projectItems)
					{
						if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
							item.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
						{
							if (string.Equals(item.Name, folderParts[i], StringComparison.OrdinalIgnoreCase))
							{
								folder = item;
								projectItems = item.ProjectItems;
								break;
							}
						}
					}
				}

				return folder.ProjectItems;
			}

			return null;
		}

		/// <summary>
		/// Returns the <see cref="Project"/> that the <see cref="ProjectFolder"/> resides in.
		/// </summary>
		/// <param name="solution">The <see cref="Solution"/> to search</param>
		/// <param name="projectFolder">The <see cref="ProjectFolder"/> contained within the <see cref="Project"/></param>
		/// <returns></returns>
		private static Project FindProject(Solution solution, ProjectFolder projectFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				if (string.Equals(project.Name, projectFolder.ProjectName, StringComparison.OrdinalIgnoreCase))
					return project;
			}

			return null;
		}

		private static string FindValidatorInterface(Solution solution, ProjectFolder validatorFolder, string resourceClassName)
		{
			Project project = FindProject(solution, validatorFolder);

			foreach (ProjectItem projectItem in project.ProjectItems)
			{
				if (Guid.Parse(projectItem.Kind) == Guid.Parse(Constants.vsProjectItemKindVirtualFolder) ||
					Guid.Parse(projectItem.Kind) == Guid.Parse(Constants.vsProjectItemKindPhysicalFolder))
				{
					string validatorClass = FindValidatorInterface(projectItem, validatorFolder, resourceClassName);

					if (!string.IsNullOrWhiteSpace(validatorClass))
						return validatorClass;
				}
				else if (Guid.Parse(projectItem.Kind) == Guid.Parse(Constants.vsProjectItemKindPhysicalFile))
				{
					if (projectItem.FileCodeModel != null)
					{
						FileCodeModel2 codeModel = (FileCodeModel2)projectItem.FileCodeModel;

						foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
						{
							foreach (CodeInterface2 codeClass in codeNamespace.Children.OfType<CodeInterface2>())
							{
								foreach (CodeInterface2 codeBase in codeClass.Bases.OfType<CodeInterface2>())
								{
									if (codeBase.Name.Equals($"IValidator<{resourceClassName}>", StringComparison.OrdinalIgnoreCase))
									{
										return codeClass.Name;
									}
								}
							}
						}
					}
				}
			}

			return string.Empty;
		}

		private static string FindValidatorInterface(ProjectItem projectItem, ProjectFolder validatorFolder, string resourceClassName)
		{
			foreach (ProjectItem subItem in projectItem.ProjectItems)
			{
				if (Guid.Parse(subItem.Kind) == Guid.Parse(Constants.vsProjectItemKindVirtualFolder) ||
					Guid.Parse(subItem.Kind) == Guid.Parse(Constants.vsProjectItemKindPhysicalFolder))
				{
					foreach (ProjectItem projectFolder in projectItem.ProjectItems.OfType<ProjectFolder>())
					{
						var folderName = projectFolder.Name;
						string validatorClass = FindValidatorInterface(projectFolder, validatorFolder, resourceClassName);

						if ( !string.IsNullOrWhiteSpace(validatorClass) )
							return validatorClass;
					}
				}
				else if (Guid.Parse(subItem.Kind) == Guid.Parse(Constants.vsProjectItemKindPhysicalFile))
				{
					var fileName = subItem.Name;

					if (subItem.FileCodeModel != null)
					{
						FileCodeModel2 codeModel = (FileCodeModel2)subItem.FileCodeModel;

						foreach (CodeNamespace codeNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
						{
							foreach (CodeInterface2 codeClass in codeNamespace.Children.OfType<CodeInterface2>())
							{ 
								foreach (CodeInterface2 codeBase in codeClass.Bases.OfType<CodeInterface2>())
                                {
									var match = Regex.Match(codeBase.FullName, $"([a-zA-Z0-9_]+\\.)*IValidator\\<([a-zA-Z0-9_]+\\.)*{resourceClassName}\\>");

									if (match.Success)
									{
										return codeClass.Name;
									}
                                }
							}
						}
					}
				}
			}

			return string.Empty;
		}

		private static DBColumn[] LoadColumns(CodeClass2 codeClass, EntityModel entityModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var columns = new List<DBColumn>();

			foreach (CodeProperty2 property in codeClass.Children.OfType<CodeProperty2>())
			{
				var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				CodeAttribute memberAttribute = null;
				try { memberAttribute = (CodeAttribute)property.Children.Item("Member"); } catch (Exception) { }

				var dbColumn = new DBColumn
				{
					ColumnName = property.Name,
					EntityName = property.Name,
					ModelDataType = parts[parts.Count() - 1]
				};

				if (memberAttribute != null)
				{
					var matchit = Regex.Match(memberAttribute.Value, "IsPrimaryKey[ \t]*=[ \t]*(?<IsPrimary>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsPrimary"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsPrimaryKey = true;

					matchit = Regex.Match(memberAttribute.Value, "IsIdentity[ \t]*=[ \t]*(?<IsIdentity>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsIdentity"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsIdentity = true;

					matchit = Regex.Match(memberAttribute.Value, "AutoField[ \t]*=[ \t]*(?<AutoField>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["AutoField"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsComputed = true;

					matchit = Regex.Match(memberAttribute.Value, "IsIndexed[ \t]*=[ \t]*(?<IsIndexed>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsIndexed"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsIndexed = true;

					matchit = Regex.Match(memberAttribute.Value, "IsNullable[ \t]*=[ \t]*(?<IsNullable>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsNullable"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsNullable = true;

					matchit = Regex.Match(memberAttribute.Value, "IsFixed[ \t]*=[ \t]*(?<IsFixed>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsFixed"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsFixed = true;

					matchit = Regex.Match(memberAttribute.Value, "IsForeignKey[ \t]*=[ \t]*(?<IsForeignKey>(true|false))");

					if (matchit.Success)
						if (string.Equals(matchit.Groups["IsForeignKey"].Value, "true", StringComparison.OrdinalIgnoreCase))
							dbColumn.IsForeignKey = true;

					matchit = Regex.Match(memberAttribute.Value, "NativeDataType[ \t]*=[ \t]*\"(?<NativeDataType>[_a-zA-Z][_a-zA-Z0-9]*)\"");

					if (matchit.Success)
						dbColumn.DBDataType = matchit.Groups["NativeDataType"].Value;

					matchit = Regex.Match(memberAttribute.Value, "Length[ \t]*=[ \t]*(?<Length>[0-9]+)");

					if (matchit.Success)
						dbColumn.Length = Convert.ToInt32(matchit.Groups["Length"].Value);

					matchit = Regex.Match(memberAttribute.Value, "ForeignTableName[ \t]*=[ \t]*\"(?<ForeignTableName>[_a-zA-Z][_a-zA-Z0-9]*)\"");

					if (matchit.Success)
						dbColumn.ForeignTableName = matchit.Groups["ForeignTableName"].Value;
				}

				columns.Add(dbColumn);
			}

			return columns.ToArray();
		}

		public static ResourceMap LoadResourceModels(Solution solution, EntityMap entityMap, ProjectFolder resourceModelFolder, DBServerType defaultServerType)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var map = new List<ResourceModel>();

			var resourceFolderContents = FindProjectFolderContents(solution, resourceModelFolder);

			foreach (ProjectItem projectItem in resourceFolderContents)
			{
				if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					projectItem.FileCodeModel != null &&
					projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
					Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
				{
					foreach (CodeNamespace namespaceElement in projectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>())
					{
						foreach (CodeElement childElement in namespaceElement.Members)
						{
							if (childElement.Kind == vsCMElement.vsCMElementClass)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null)
								{
									var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

									var entityName = "Unknown";
									if (match.Success)
										entityName = match.Groups["entityType"].Value.ToString();

									var entityModel = entityMap.Maps.FirstOrDefault(e =>
										string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = entityModel.ServerType,
										EntityModel = entityModel,
										ResourceType = ResourceType.Class,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();
									var foreignKeyColumns = entityModel.Columns.Where(c => c.IsForeignKey);

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;
											var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

											var dbColumn = new DBColumn
											{
												ColumnName = property.Name,
												ModelDataType = parts[parts.Count() - 1],
												IsPrimaryKey = string.Equals(property.Name, "href", StringComparison.OrdinalIgnoreCase)
											};

											var fk = foreignKeyColumns.FirstOrDefault(c =>
											{
												var nn = new NameNormalizer(c.ForeignTableName);
												return string.Equals(nn.SingleForm, dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase);
											});

											if (fk != null)
											{
												dbColumn.IsForeignKey = true;
												dbColumn.ForeignTableName = fk.ForeignTableName;
											}

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();
									map.Add(resourceModel);
								}
								else
								{
									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = defaultServerType,
										EntityModel = null,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();
									var functions = new List<CodeFunction2>();

									foreach (CodeElement memberElement in childElement.Children)
									{
										if (memberElement.Kind == vsCMElement.vsCMElementProperty)
										{
											CodeProperty property = (CodeProperty)memberElement;
											var parts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

											if (property.Access == vsCMAccess.vsCMAccessPublic || property.Access == vsCMAccess.vsCMAccessProtected)
											{
												var dbColumn = new DBColumn
												{
													ColumnName = property.Name,
													ModelDataType = parts[parts.Count() - 1],
												};

												columns.Add(dbColumn);
											}
										}
										else if (memberElement.Kind == vsCMElement.vsCMElementFunction)
										{
											CodeFunction2 function = (CodeFunction2)memberElement;
											functions.Add(function);
										}
									}

									resourceModel.Columns = columns.ToArray();
									resourceModel.Functions = functions.ToArray();
									map.Add(resourceModel);
								}
							}
							else if (childElement.Kind == vsCMElement.vsCMElementEnum)
							{
								CodeAttribute entityAttribute = null;

								try { entityAttribute = (CodeAttribute)childElement.Children.Item("Entity"); } catch (Exception) { }

								if (entityAttribute != null)
								{
									var match = Regex.Match(entityAttribute.Value, "typeof\\((?<entityType>[a-zA-Z0-9_]+)\\)");

									var entityName = "Unknown";
									if (match.Success)
										entityName = match.Groups["entityType"].Value.ToString();

									var entityModel = entityMap.Maps.FirstOrDefault(e =>
										string.Equals(e.ClassName, entityName, StringComparison.OrdinalIgnoreCase));

									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = entityModel.ServerType,
										EntityModel = entityModel,
										ResourceType = ResourceType.Enum,
										ProjectName = resourceModelFolder.ProjectName,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement enumElement in childElement.Children)
									{
										if (enumElement.Kind == vsCMElement.vsCMElementVariable)
										{
											var dbColumn = new DBColumn
											{
												ColumnName = enumElement.Name,
											};

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();

									map.Add(resourceModel);
								}
								else
								{
									var resourceModel = new ResourceModel
									{
										ClassName = childElement.Name,
										Namespace = namespaceElement.Name,
										ServerType = defaultServerType,
										EntityModel = null,
										ProjectName = resourceModelFolder.ProjectName,
										ResourceType = ResourceType.Enum,
										Folder = projectItem.Properties.Item("FullPath").Value.ToString()
									};

									var columns = new List<DBColumn>();

									foreach (CodeElement enumElement in childElement.Children)
									{
										if (enumElement.Kind == vsCMElement.vsCMElementVariable)
										{
											var dbColumn = new DBColumn
											{
												ColumnName = enumElement.Name,
											};

											columns.Add(dbColumn);
										}
									}

									resourceModel.Columns = columns.ToArray();

									map.Add(resourceModel);
								}
							}
						}
					}
				}
			}

			return new ResourceMap() { Maps = map.ToArray() };
		}
		#endregion
	}
}
