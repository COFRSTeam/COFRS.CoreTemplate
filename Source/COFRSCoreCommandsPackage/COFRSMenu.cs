using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using COFRSCoreCommandsPackage.Forms;
using COFRSCoreCommon.Utilities;
using System.Text.RegularExpressions;
using COFRS.Template.Common.Forms;
using System.Windows.Forms;
using COFRSCoreCommon.Models;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using COFRSCoreCommon.Forms;

namespace COFRSCoreCommandsPackage
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class COFRSMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int AddCollectionId = 0x0100;
        public const int EditMappingId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5563b5a3-cd74-44df-b9e4-a25fcd9a2d03");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="COFRSMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private COFRSMenu(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var addCollectionCommandId = new CommandID(CommandSet, AddCollectionId);
            OleMenuCommand AddCollectionMenu = new OleMenuCommand(new EventHandler(OnAddCollection), addCollectionCommandId);
            AddCollectionMenu.BeforeQueryStatus += new EventHandler(OnBeforeAddCollection);
            commandService.AddCommand(AddCollectionMenu);

            var editMappingCommandId = new CommandID(CommandSet, EditMappingId);
            OleMenuCommand EditMappingMenu = new OleMenuCommand(new EventHandler(OnEditMapping), editMappingCommandId);
            EditMappingMenu.BeforeQueryStatus += new EventHandler(OnBeforeEditMapping);
            commandService.AddCommand(EditMappingMenu);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static COFRSMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage ownerPackage)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ownerPackage.DisposalToken);

            OleMenuCommandService commandService = await ownerPackage.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new COFRSMenu(ownerPackage, commandService);
        }

        private void OnBeforeAddCollection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var myCommand = sender as OleMenuCommand;
            var dte2 = package.GetService<SDTE, DTE2>() as DTE2;
            object[] selectedItems = (object[])dte2.ToolWindows.SolutionExplorer.SelectedItems;

            if (selectedItems.Length > 1)
            {
                myCommand.Visible = false;
            }
            else
            {
                EnvDTE.ProjectItem item = ((EnvDTE.UIHierarchyItem)selectedItems[0]).Object as EnvDTE.ProjectItem;
                var theNamespace = item.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();

                if (theNamespace != null)
                {
                    var theClass = theNamespace.Children.OfType<CodeClass2>().First();

                    if (theClass != null)
                    {
                        var theAttribute = theClass.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));

                        if (theAttribute != null)
                        {
                            myCommand.Enabled = true;
                            myCommand.Visible = true;
                        }
                        else
                        {
                            myCommand.Enabled = false;
                            myCommand.Visible = false;
                        }
                    }
                    else
                    {
                        myCommand.Enabled = false;
                        myCommand.Visible = false;
                    }
                }
                else
                {
                    myCommand.Enabled = false;
                    myCommand.Visible = false;
                }
            }
        }

        private void OnAddCollection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte2 = package.GetService<SDTE, DTE2>() as DTE2;
            object[] selectedItems = (object[])dte2.ToolWindows.SolutionExplorer.SelectedItems;

			if (selectedItems.Length == 1)
            {
                EnvDTE.ProjectItem item = ((EnvDTE.UIHierarchyItem)selectedItems[0]).Object as EnvDTE.ProjectItem;
                var theNamespace = item.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();

                if (theNamespace != null)
                {
                    var theClass = theNamespace.Children.OfType<CodeClass2>().First();

                    if (theClass != null)
                    {
						var resourceMap = COFRSCommonUtilities.LoadResourceMap(dte2);         //	The resource map contains all the resource classes in the project.

						//	The parent resource is the resource selected by the user. This is the resource we will be adding the collection to.
						//	Get the resource model for the parent resource.
						var parentModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(theClass.Name, StringComparison.OrdinalIgnoreCase));
                        var dialog = new AddCollectionDialog();
                        dialog.ResourceName.Text = theClass.Name;
						dialog._dte2 = dte2;

                        if ( dialog.ShowDialog() == DialogResult.OK)
                        {
							var progressDialog = new ProgressDialog("Loading classes and preparing project...");
							progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
							dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
							HandleMessages();


							var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);  //	Contains the names and projects where various source file exist.
                            ProjectItem orchestrator = dte2.Solution.FindProjectItem("ServiceOrchestrator.cs");
                            FileCodeModel2 codeModel = (FileCodeModel2)orchestrator.FileCodeModel;

                            //	The orchestration layer is going to need "using System.Linq", ensure that it it does
                            if (codeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Linq")) == null)
                                codeModel.AddImport("System.Linq", -1);

                            //	The orchestration layer is going to need "using System.Text", ensure that it it does
                            if (codeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Text")) == null)
                                codeModel.AddImport("System.Text", -1);

                            foreach (var childItem in dialog.ChildResourceList.CheckedItems)
                            {
                                //	Get the child model from the resource map
                                var childModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(childItem.ToString(), StringComparison.OrdinalIgnoreCase));

                                //	Setup the default name of our new member
                                var nn = new NameNormalizer(childModel.ClassName);
                                var memberName = nn.PluralForm;                                     // The memberName is will be the name of the new collection in the parent resource. By default, it will be
                                                                                                // the plural of the child model class name.

                                var parentValidatorInterface = COFRSCommonUtilities.FindValidatorInterface(dte2, parentModel.ClassName);
                                var childValidatorInterface = COFRSCommonUtilities.FindValidatorInterface(dte2, childModel.ClassName);
								string memberType = string.Empty;

                                //	Now that we have all the information we need, add the collection member to the parent resource
                                AddCollectionToResource(dte2, parentModel, childModel, memberName, ref memberType);

                                //	Now that we've added a new collection, we need to alter the orchestration layer to handle that new collection...

                                //	We're going to need a validator for the new members. To get it, we will use dependency injection in the 
                                //	constructor, which means we will need a class variable. That variable is going to need a name. Create 
                                //	the default name for this vairable as the class name of the new member followed by "Validator".
                                //
                                //	i.e., if the new member class is Foo, then the variable name will be FooValidator.

                                var childValidatorName = $"{childModel.ClassName}Validator";
								var parentValidatorName = $"{parentModel.ClassName}Validator";

                                //	Find the namespace...
                                foreach (CodeNamespace orchestratorNamespace in codeModel.CodeElements.OfType<CodeNamespace>())
                                {
                                    CodeClass2 classElement = orchestratorNamespace.Children.OfType<CodeClass2>().FirstOrDefault(c => c.Name.Equals("ServiceOrchestrator"));

                                    //	The new collection of child items will need to be validated for the various operations.
                                    //	Add a validator the for the child items 
                                    AddChildValidatorInterfaceMember(classElement, parentValidatorInterface, childValidatorInterface, childValidatorName, ref parentValidatorName);

                                    //	Now, let's go though all the functions...
                                    foreach (CodeFunction2 aFunction in classElement.Children.OfType<CodeFunction2>())
                                    {
                                        //	Constructor
                                        if (aFunction.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
                                        {
                                            ModifyConstructor(aFunction, parentModel, childModel, parentValidatorName, childValidatorName, childValidatorInterface, memberName);
                                        }

                                        //	Get Single
                                        else if (aFunction.Name.Equals($"Get{parentModel.ClassName}Async", StringComparison.OrdinalIgnoreCase))
                                        {
                                            ModifyGetSingle(aFunction, parentModel, childModel, parentValidatorName, childValidatorName, memberName, memberType);
                                        }

                                        //	Get Collection
                                        else if (aFunction.Name.Equals($"Get{parentModel.ClassName}CollectionAsync"))
                                        {
                                            ModifyGetCollection(aFunction, parentModel, childModel, childValidatorName, memberName);
                                        }

                                        //	Add
                                        else if (aFunction.Name.Equals($"Add{parentModel.ClassName}Async"))
                                        {
                                            ModifyAdd(aFunction, parentModel, childModel, parentValidatorName, childValidatorName, memberName);
                                        }

                                        //	Update
                                        else if (aFunction.Name.Equals($"Update{parentModel.ClassName}Async"))
                                        {
                                            ModifyUpdate(aFunction, parentModel, childModel, parentValidatorName, childValidatorName, memberName);
                                        }

                                        //	Update
                                        else if (aFunction.Name.Equals($"Patch{parentModel.ClassName}Async"))
                                        {
                                            ModifyPatch(aFunction, parentModel, childModel, parentValidatorName, childValidatorName, memberName);
                                        }

                                        //	Delete
                                        else if (aFunction.Name.Equals($"Delete{parentModel.ClassName}Async"))
                                        {
                                            ModifyDelete(aFunction, childModel, parentValidatorName, childValidatorName, memberName);
                                        }
                                    }
                                }

                                AddSingleExample(dte2, parentModel, childModel, memberName);
                                AddCollectionExample(dte2, parentModel, childModel, memberName);

								progressDialog.Close();
                            }
                        }
                    }
                }
            }
        }

        private void OnBeforeEditMapping(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var myCommand = sender as OleMenuCommand;
            var dte2 = package.GetService<SDTE, DTE2>() as DTE2;
            object[] selectedItems = (object[])dte2.ToolWindows.SolutionExplorer.SelectedItems;

            if (selectedItems.Length > 1)
            {
                myCommand.Visible = false;
            }
            else
            {
                EnvDTE.ProjectItem item = ((EnvDTE.UIHierarchyItem)selectedItems[0]).Object as EnvDTE.ProjectItem;
                var theNamespace = item.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();

                if (theNamespace != null)
                {
                    var theClass = theNamespace.Children.OfType<CodeClass2>().First();

                    if (theClass != null)
                    {
                        var theBaseClass = theClass.Bases.OfType<CodeClass2>().FirstOrDefault(a => a.Name.Equals("Profile"));

                        if (theBaseClass != null)
                        {
                            myCommand.Enabled = true;
                            myCommand.Visible = true;
                        }
                        else
                        {
                            myCommand.Enabled = false;
                            myCommand.Visible = false;
                        }
                    }
                    else
                    {
                        myCommand.Enabled = false;
                        myCommand.Visible = false;
                    }
                }
                else
                {
                    myCommand.Enabled = false;
                    myCommand.Visible = false;
                }
            }
        }

        private void OnEditMapping(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte2 = package.GetService<SDTE, DTE2>() as DTE2;
            object[] selectedItems = (object[])dte2.ToolWindows.SolutionExplorer.SelectedItems;

            if (selectedItems.Length == 1)
            {
                EnvDTE.ProjectItem item = ((EnvDTE.UIHierarchyItem)selectedItems[0]).Object as EnvDTE.ProjectItem;
                var theNamespace = item.FileCodeModel.CodeElements.OfType<CodeNamespace>().First();

                if (theNamespace != null)
                {
                    var theClass = theNamespace.Children.OfType<CodeClass2>().First();

                    if (theClass != null)
                    {
                        var resourceMap = COFRSCommonUtilities.LoadResourceMap(dte2);

                        var constructor = theClass.Children
                                                  .OfType<CodeFunction2>()
                                                  .First(c => c.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

                        if (constructor != null)
                        {

                            EditPoint2 editPoint = (EditPoint2) constructor.StartPoint.CreateEditPoint();
                            bool foundit = editPoint.FindPattern("CreateMap<");
                            foundit = foundit && editPoint.LessThan(constructor.EndPoint);

                            if (foundit)
                            {
                                editPoint.StartOfLine();
                                EditPoint2 start = (EditPoint2) editPoint.CreateEditPoint();
                                editPoint.EndOfLine();
                                EditPoint2 end = (EditPoint2) editPoint.CreateEditPoint();
                                var text = start.GetText(end);

                                var match = Regex.Match(text, "[ \t]*CreateMap\\<(?<resource>[a-zA-Z0-9_]+)[ \t]*\\,[ \t]*(?<entity>[a-zA-Z0-9_]+)\\>[ \t]*\\([ \t]*\\)");

                                if (match.Success)
                                {
                                    Mapper dialog = new Mapper
                                    {
                                        ResourceMap = resourceMap,
                                        ResourceModel = resourceMap.Maps.FirstOrDefault(c => c.ClassName.Equals(match.Groups["resource"].Value)),
                                        Dte = dte2
                                    };

                                    if ( dialog.ShowDialog() == DialogResult.OK )
                                    {
                                        var ProfileMap = dialog.ProfileMap;
                                        var resourceModel = dialog.ResourceModel;

                                        editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();
                                        foundit = editPoint.FindPattern($"CreateMap<{resourceModel.ClassName}");
                                        foundit = foundit && editPoint.LessThan(constructor.EndPoint);

                                        if (foundit)
                                        {
                                            editPoint.LineDown();

                                            while (!IsLineEmpty(editPoint))
                                            {
                                                DeleteLine(editPoint);
                                            }

                                            bool first = true;

                                            foreach (var rmap in ProfileMap.EntityProfiles)
                                            {
                                                if (first)
                                                    first = false;
                                                else
                                                    editPoint.InsertNewLine();

                                                editPoint.StartOfLine();
                                                editPoint.Indent(null, 4);
                                                editPoint.Insert(".ForMember(destination => destination.");
                                                editPoint.Insert(rmap.EntityColumnName);
                                                editPoint.Insert(", opts => opts.MapFrom(source => ");
                                                editPoint.Insert(rmap.MapFunction);
                                                editPoint.Insert("))");
                                            }

                                            editPoint.Insert(";");
                                            editPoint.InsertNewLine();
                                        }

                                        editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();
                                        foundit = editPoint.FindPattern($"CreateMap<{resourceModel.EntityModel.ClassName}");
                                        foundit = foundit && editPoint.LessThan(constructor.EndPoint);

                                        if (foundit)
                                        {
                                            editPoint.LineDown();

                                            while (!IsLineEmpty(editPoint))
                                            {
                                                DeleteLine(editPoint);
                                            }

                                            bool first = true;

                                            foreach (var rmap in ProfileMap.ResourceProfiles)
                                            {
                                                if (first)
                                                    first = false;
                                                else
                                                    editPoint.InsertNewLine();

                                                editPoint.StartOfLine();
                                                editPoint.Indent(null, 4);
                                                editPoint.Insert(".ForMember(destination => destination.");
                                                editPoint.Insert(rmap.ResourceColumnName);
                                                editPoint.Insert(", opts => opts.MapFrom(source => ");
                                                editPoint.Insert(rmap.MapFunction);
                                                editPoint.Insert("))");
                                            }

                                            editPoint.Insert(";");
                                            editPoint.InsertNewLine();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsLineEmpty(EditPoint2 editPoint)
        {
            EditPoint2 startOfLine = (EditPoint2)editPoint.CreateEditPoint();
            startOfLine.StartOfLine();
            EditPoint2 eol = (EditPoint2)startOfLine.CreateEditPoint();
            eol.EndOfLine();
            return string.IsNullOrWhiteSpace(editPoint.GetText(eol));
        }

        private static void DeleteLine(EditPoint2 editPoint)
        {
            EditPoint2 startOfLine = (EditPoint2)editPoint.CreateEditPoint();
            startOfLine.StartOfLine();
            EditPoint2 eol = (EditPoint2)startOfLine.CreateEditPoint();
            eol.LineDown();
            eol.StartOfLine();
            startOfLine.Delete(eol);
        }

		#region Helper Functions
		private void AddCollectionExample(DTE2 dte2, ResourceModel parentModel, ResourceModel childModel, string memberName)
		{
			var collectionExampleClass = COFRSCommonUtilities.FindCollectionExampleCode(dte2, parentModel);

			if (collectionExampleClass != null)
			{
				var getExampleFunction = collectionExampleClass.Children
															   .OfType<CodeFunction2>()
															   .FirstOrDefault(c => c.Name.Equals("GetExamples", StringComparison.OrdinalIgnoreCase));

				if (getExampleFunction != null)
				{
					EditPoint2 nextClassStart;
					EditPoint2 classStart = (EditPoint2)getExampleFunction.StartPoint.CreateEditPoint();
					bool foundit = classStart.FindPattern($"new {parentModel.ClassName} {{");
					foundit = foundit && classStart.LessThan(getExampleFunction.EndPoint);

					if (foundit)
					{
						while (foundit)
						{
							nextClassStart = (EditPoint2)classStart.CreateEditPoint();
							nextClassStart.LineDown();
							foundit = nextClassStart.FindPattern($"new {parentModel.ClassName} {{");
							foundit = foundit && nextClassStart.LessThan(getExampleFunction.EndPoint);

							if (foundit)
							{
								EditPoint2 AssignPoint = (EditPoint2)classStart.CreateEditPoint();
								bool alreadyAssigned = AssignPoint.FindPattern($"{memberName} = new {childModel.ClassName}[]");
								alreadyAssigned = alreadyAssigned && AssignPoint.LessThan(nextClassStart);
								if (!alreadyAssigned)
								{
									classStart = (EditPoint2)nextClassStart.CreateEditPoint();
									nextClassStart.LineUp();
									nextClassStart.LineUp();
									nextClassStart.EndOfLine();
									nextClassStart.Insert(",");
									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 5);
									nextClassStart.Insert($"{memberName} = new {childModel.ClassName}[]");
									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 5);
									nextClassStart.Insert("{");
									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 6);
									nextClassStart.Insert($"new {childModel.ClassName}");
									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 6);
									nextClassStart.Insert("{");

									var serverType = COFRSCommonUtilities.GetDefaultServerType(dte2);
									var connectionString = COFRSCommonUtilities.GetConnectionString(dte2);

									var exampleModel = COFRSCommonUtilities.GetExampleModel(0, childModel, serverType, connectionString);
									var entityJson = JObject.Parse(exampleModel);
									var profileMap = COFRSCommonUtilities.LoadResourceMapping(dte2, childModel);

									bool first = true;

									foreach (var map in profileMap.ResourceProfiles)
									{
										if (first)
										{
											first = false;
										}
										else
										{
											nextClassStart.Insert(",");
										}
										nextClassStart.InsertNewLine();
										nextClassStart.Indent(null, 7);
										nextClassStart.Insert($"{map.ResourceColumnName} = ");
										nextClassStart.Insert(COFRSCommonUtilities.ResolveMapFunction(entityJson, map.ResourceColumnName, childModel, map.MapFunction));
									}

									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 6);
									nextClassStart.Insert("}");
									nextClassStart.InsertNewLine();
									nextClassStart.Indent(null, 5);
									nextClassStart.Insert("}");
								}
								else
									classStart = (EditPoint2)nextClassStart.CreateEditPoint();
							}
						}
					}

					nextClassStart = (EditPoint2)classStart.CreateEditPoint();
					nextClassStart.LineDown();
					foundit = nextClassStart.FindPattern("};");
					foundit = foundit && nextClassStart.LessThan(getExampleFunction.EndPoint);

					if (foundit)
					{
						EditPoint2 AssignPoint = (EditPoint2)classStart.CreateEditPoint();
						bool alreadyAssigned = AssignPoint.FindPattern($"{memberName} = new {childModel.ClassName}[]");
						alreadyAssigned = alreadyAssigned && AssignPoint.LessThan(nextClassStart);

						if (!alreadyAssigned)
						{
							nextClassStart.LineUp();
							nextClassStart.LineUp();
							nextClassStart.EndOfLine();
							nextClassStart.Insert(",");
							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 5);
							nextClassStart.Insert($"{memberName} = new {childModel.ClassName}[]");
							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 5);
							nextClassStart.Insert("{");
							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 6);
							nextClassStart.Insert($"new {childModel.ClassName}");
							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 6);
							nextClassStart.Insert("{");

							var serverType = COFRSCommonUtilities.GetDefaultServerType(dte2);
							var connectionString = COFRSCommonUtilities.GetConnectionString(dte2);

							var exampleModel = COFRSCommonUtilities.GetExampleModel(0, childModel, serverType, connectionString);
							var entityJson = JObject.Parse(exampleModel);
							var profileMap = COFRSCommonUtilities.LoadResourceMapping(dte2, childModel);

							bool first = true;

							foreach (var map in profileMap.ResourceProfiles)
							{
								if (first)
								{
									first = false;
								}
								else
								{
									nextClassStart.Insert(",");
								}
								nextClassStart.InsertNewLine();
								nextClassStart.Indent(null, 7);
								nextClassStart.Insert($"{map.ResourceColumnName} = ");
								nextClassStart.Insert(COFRSCommonUtilities.ResolveMapFunction(entityJson, map.ResourceColumnName, childModel, map.MapFunction));
							}

							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 6);
							nextClassStart.Insert("}");
							nextClassStart.InsertNewLine();
							nextClassStart.Indent(null, 5);
							nextClassStart.Insert("}");
						}
					}
				}
			}
		}

		private void AddSingleExample(DTE2 dte2, ResourceModel parentModel, ResourceModel childModel, string memberName)
		{
			var singleExampleClass = COFRSCommonUtilities.FindExampleCode(dte2, parentModel);

			if (singleExampleClass != null)
			{
				var getExampleFunction = singleExampleClass.Children
														   .OfType<CodeFunction2>()
														   .FirstOrDefault(c => c.Name.Equals("GetExamples", StringComparison.OrdinalIgnoreCase));

				if (getExampleFunction != null)
				{
					EditPoint2 editPoint = (EditPoint2)getExampleFunction.StartPoint.CreateEditPoint();
					bool foundit = editPoint.FindPattern($"{memberName} = new {childModel.ClassName}[]");
					foundit = foundit && editPoint.LessThan(getExampleFunction.EndPoint);

					if (!foundit)
					{
						editPoint = (EditPoint2)getExampleFunction.StartPoint.CreateEditPoint();
						foundit = editPoint.FindPattern($"return singleExample;");
						foundit = foundit && editPoint.LessThan(getExampleFunction.EndPoint);

						if (foundit)
						{
							foundit = false;
							while (!foundit)
							{
								editPoint.LineUp();
								var editPoint2 = editPoint.CreateEditPoint();

								foundit = editPoint2.FindPattern("};");
								foundit = foundit && editPoint2.LessThan(getExampleFunction.EndPoint);
							}

							editPoint.LineUp();
							editPoint.EndOfLine();
							editPoint.Insert(",");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 4);
							editPoint.Insert($"{memberName} = new {childModel.ClassName}[]");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 4);
							editPoint.Insert("{");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 5);
							editPoint.Insert($"new {childModel.ClassName}");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 5);
							editPoint.Insert("{");

							var serverType = COFRSCommonUtilities.GetDefaultServerType(dte2);
							var connectionString = COFRSCommonUtilities.GetConnectionString(dte2);

							var exampleModel = COFRSCommonUtilities.GetExampleModel(0, childModel, serverType, connectionString);
							var entityJson = JObject.Parse(exampleModel);
							var profileMap = COFRSCommonUtilities.LoadResourceMapping(dte2, childModel);

							bool first = true;

							foreach (var map in profileMap.ResourceProfiles)
							{
								if (first)
								{
									first = false;
								}
								else
								{
									editPoint.Insert(",");
								}

								editPoint.InsertNewLine();
								editPoint.Indent(null, 6);
								editPoint.Insert($"{map.ResourceColumnName} = ");
								editPoint.Insert(COFRSCommonUtilities.ResolveMapFunction(entityJson, map.ResourceColumnName, childModel, map.MapFunction));
							}

							editPoint.InsertNewLine();
							editPoint.Indent(null, 5);
							editPoint.Insert("}");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 4);
							editPoint.Insert("}");
						}
					}
				}
			}
		}
		/// <summary>
		/// Adds a class property for the child class validator
		/// </summary>
		/// <param name="classElement"></param>
		private void AddChildValidatorInterfaceMember(CodeClass2 classElement, string parentValidatorInterface, string childValidatorInterface, string childValidatorName, ref string parentValidatorName)
		{
			//	Okay, we will need a validator for our new child resources. To get one, we will use dependency injection
			//	in the constructor. That means, we need a class member variable to hold it.
			//
			//	And, by the way, we're going to need to know the name of the source class validator. While we're
			//	muking around with the variables, find and store that name.
			//	muking around with the variables, find and store that name.

			//	So, do we already have that variable? If so, use it. If not, create it.
			//	We start by assuming we're going to create it.
			var shouldAddValidator = true;

			//	Look at all our class variables.
			//	The variable we are looking for will have a type of validator interface for the class. Fortunately,
			//	we know those names for both our source class and our new member class.
			foreach (CodeVariable2 variableElement in classElement.Children.OfType<CodeVariable2>())
			{
				var parts = variableElement.Type.AsFullName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

				if (parts[parts.Length - 1].Equals(parentValidatorInterface, StringComparison.OrdinalIgnoreCase))
				{
					//	This is a member variable that has a type of the Interface for the source class. Remember it's name.
					parentValidatorName = variableElement.Name;
				}
				else if (parts[parts.Length - 1].Equals(childValidatorInterface, StringComparison.OrdinalIgnoreCase))
				{
					//	This is a member variable that has a type of the interface for the member class. It may (or may not)
					//	have the name we used as the default. No matter, whatever name it is using, remember it. Also, mark
					//	the flag to say we don't need to create one.
					childValidatorName = variableElement.Name;
					shouldAddValidator = false;
				}
			}

			//	Did we find it?
			if (shouldAddValidator)
			{
				//	Nope, didn't find it. Create it using that default variable name we created.
				var variable = (CodeVariable2)classElement.AddVariable(childValidatorName, childValidatorInterface, 0, vsCMAccess.vsCMAccessPrivate);
				variable.ConstKind = vsCMConstKind.vsCMConstKindReadOnly;
			}
		}

		/// <summary>
		/// Adds the new collection member to the parent resource
		/// </summary>
		private void AddCollectionToResource(DTE2 dte2, ResourceModel parentModel, ResourceModel childModel, string memberName, ref string memberType)
		{
			//	First we need to open the code file for the parent resource
			var fileName = Path.GetFileName(parentModel.Folder);
			ProjectItem resourceCodeFile = dte2.Solution.FindProjectItem(fileName);
			EditPoint2 editPoint;

			//	Now we have the code file for our main resource.
			//	First, find the namespace with this file...
			foreach (CodeNamespace namespaceElement in resourceCodeFile.FileCodeModel.CodeElements.OfType<CodeNamespace>())
			{
				CodeClass2 resourceClass = namespaceElement.Children
														   .OfType<CodeClass2>()
														   .FirstOrDefault(c => c.Name.Equals(parentModel.ClassName));

				if (resourceClass != null)
				{
					CodeFunction2 constructor = resourceClass.Children
															 .OfType<CodeFunction2>()
															 .FirstOrDefault(c => c.FunctionKind == vsCMFunction.vsCMFunctionConstructor);

					if (constructor == null)
					{
						constructor = (CodeFunction2)resourceClass.AddFunction(resourceClass.Name, vsCMFunction.vsCMFunctionConstructor, "", -1, vsCMAccess.vsCMAccessPublic);

						StringBuilder doc = new StringBuilder();
						doc.AppendLine("<doc>");
						doc.AppendLine("<summary>");
						doc.AppendLine($"Constructor for the resource.");
						doc.AppendLine("</summary>");
						doc.AppendLine("</doc>");

						constructor.DocComment = doc.ToString();
					}

					//	We're in the class. Now we need to add a new property of type IEnumerable<childClass>
					//	However, this may not be the first time the user has done this, and they may have already added the member.
					//	So, we need to determine if such a member already exists...

					CodeProperty2 enumerableChild = resourceClass.Members
																 .OfType<CodeProperty2>()
																 .FirstOrDefault(c =>
																 {
																	 var parts = c.Type.AsString.Split('.');
																	 if (parts.Contains("IEnumerable"))
																	 {
																		 if (parts[parts.Length - 1].Equals(childModel.ClassName))
																			 return true;
																	 }

																	 return false;
																 });

					if (enumerableChild != null)
					{
						memberName = enumerableChild.Name;
						memberType = $"IEnumerable<{childModel.ClassName}>";

						editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();
						if (!editPoint.FindPattern($"{memberName} = Array.Empty<{childModel.ClassName}>();"))
						{
							editPoint = (EditPoint2)constructor.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.EndOfLine();
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"{memberName} = Array.Empty<{childModel.ClassName}>();");
						}
					}
					else
					{
						CodeProperty2 listChild = resourceClass.Members
																	 .OfType<CodeProperty2>()
																	 .FirstOrDefault(c =>
																	 {
																		 var parts = c.Type.AsString.Split('.');
																		 if (parts.Contains("List"))
																		 {
																			 if (parts[parts.Length - 1].Equals(childModel.ClassName))
																				 return true;
																		 }

																		 return false;
																	 });

						if (listChild != null)
						{
							memberName = listChild.Name;
							memberType = $"List<{childModel.ClassName}>";
							editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

							if (!editPoint.FindPattern($"{memberName} = new List<{childModel.ClassName}>();"))
							{
								editPoint = (EditPoint2)constructor.EndPoint.CreateEditPoint();
								editPoint.LineUp();
								editPoint.EndOfLine();
								editPoint.InsertNewLine();
								editPoint.Indent(null, 3);
								editPoint.Insert($"{memberName} = new List<{childModel.ClassName}>();");
							}
						}
						else
						{
							CodeProperty2 arrayChild = resourceClass.Members
																		 .OfType<CodeProperty2>()
																		 .FirstOrDefault(c =>
																		 {
																			 var parts = c.Type.AsString.Split('.');
																			 if (parts[parts.Length - 1].Equals($"{childModel.ClassName}[]"))
																				 return true;

																			 return false;
																		 });

							if (arrayChild != null)
							{
								memberName = arrayChild.Name;
								memberType = $"{childModel.ClassName}[]";
								editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

								if (!editPoint.FindPattern($"{memberName} = Array.Empty<{childModel.ClassName}>();"))
								{
									editPoint = (EditPoint2)constructor.EndPoint.CreateEditPoint();
									editPoint.LineUp();
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"{memberName} = Array.Empty<{childModel.ClassName}>();");
								}
							}
							else
							{
								var count = resourceClass.Children.OfType<CodeProperty>().Count();

								var property = resourceClass.AddProperty(memberName, memberName,
																		 $"{childModel.ClassName}[]",
																		 count,
																		 vsCMAccess.vsCMAccessPublic, null);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Gets or sets the collection of <see cref=\"{childModel.ClassName}\"/> resources.");
								doc.AppendLine("</summary>");
								doc.AppendLine("</doc>");

								property.DocComment = doc.ToString();
								memberType = $"{childModel.ClassName}[]";

								editPoint = (EditPoint2)property.StartPoint.CreateEditPoint();
								editPoint.EndOfLine();
								editPoint.ReplaceText(property.EndPoint, " { get; set; }", 0);
								editPoint = (EditPoint2)constructor.StartPoint.CreateEditPoint();

								if (!editPoint.FindPattern($"{memberName} = Array.Empty<{childModel.ClassName}>();"))
								{
									editPoint = (EditPoint2)constructor.EndPoint.CreateEditPoint();
									editPoint.LineUp();
									editPoint.EndOfLine();
									editPoint.InsertNewLine();
									editPoint.Indent(null, 3);
									editPoint.Insert($"{memberName} = Array.Empty<{childModel.ClassName}>();");
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Modify the delete function to accomodate the new collection
		/// </summary>
		/// <param name="aFunction">The <see cref="CodeFunction2"/> instance of the delete function.</param>
		private void ModifyDelete(CodeFunction2 aFunction, ResourceModel childModel, string parentValidatorName, string childValidatorName, string memberName)
		{
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

			bool foundit = editPoint.FindPattern("var url = node.Value<Uri>(1);");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.LineDown();
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("var url = node.Value<Uri>(1);");
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			editPoint.FindPattern($"await {parentValidatorName}.ValidateForDeleteAsync");

			foundit = editPoint.FindPattern("var subNode =");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				string searchText = $"await {parentValidatorName}.ValidateForDeleteAsync";

				if (editPoint.FindPattern(searchText))
				{
					editPoint.EndOfLine();
					editPoint.InsertNewLine(2);
					editPoint.Indent(null, 3);
					editPoint.Insert($"var subNode = RqlNode.Parse($\"{memberName}=uri:\\\"{{url.LocalPath}}\\\"\");");
					editPoint.InsertNewLine(2);
				}
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			editPoint.FindPattern("var subNode = ");
			foundit = editPoint.FindPattern($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern("var subNode = ");
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach (var subitem in {childModel.ClassName}Collection.Items)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("var dnode = RqlNode.Parse($\"HRef = uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"await {childValidatorName}.ValidateForDeleteAsync(dnode, User);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine();

				editPoint = (EditPoint2)aFunction.EndPoint.CreateEditPoint();
				editPoint.LineUp(3);
				editPoint.StartOfLine();
				var theLine = editPoint.GetText(editPoint.LineLength);

				if (!string.IsNullOrWhiteSpace(theLine))
				{
					editPoint.EndOfLine();
				}
			}
			editPoint.InsertNewLine();
			editPoint.Indent(null, 3);
			editPoint.Insert($"foreach (var subitem in {childModel.ClassName}Collection.Items)");
			editPoint.InsertNewLine();
			editPoint.Indent(null, 3);
			editPoint.Insert("{");
			editPoint.InsertNewLine();
			editPoint.Indent(null, 4);
			editPoint.Insert("var dnode = RqlNode.Parse($\"HRef = uri:\\\"{subitem.HRef.LocalPath}\\\"\");");
			editPoint.InsertNewLine();
			editPoint.Indent(null, 4);
			editPoint.Insert($"await DeleteAsync<{childModel.ClassName}>(dnode);");
			editPoint.InsertNewLine();
			editPoint.Indent(null, 3);
			editPoint.Insert("}");
			editPoint.InsertNewLine();
		}

		/// <summary>
		/// Modify the update function to accomodate the new collection
		/// </summary>
		/// <param name="aFunction"></param>
		private void ModifyUpdate(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string parentValidatorName, string childValidatorName, string memberName)
		{
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			bool foundit = editPoint.FindPattern($"return await UpdateAsync");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (foundit)
			{
				editPoint.ReplaceText(6, "item =", 0);
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("return item;");
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			editPoint.FindPattern($"await {parentValidatorName}.ValidateForUpdateAsync");
			foundit = editPoint.FindPattern("var subNode =");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"await {parentValidatorName}.ValidateForUpdateAsync");
				editPoint.EndOfLine();
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"var subNode = RqlNode.Parse($\"{memberName}=uri:\\\"{{item.HRef.LocalPath}}\\\"\");\r\n");
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			editPoint.FindPattern("var subNode = ");
			foundit = editPoint.FindPattern($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern("var subNode = ");
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach (var subitem in {childModel.ClassName}Collection.Items)");
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
				editPoint.Insert($"await {childValidatorName}.ValidateForUpdateAsync(matchingItem, User);");
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
				editPoint.Insert($"await {childValidatorName}.ValidateForDeleteAsync(dnode, User);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach (var subitem in item.{memberName}.Where(c => c.HRef == null))");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"await {childValidatorName}.ValidateForAddAsync(subitem, User);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			editPoint.FindPattern("item = await UpdateAsync");
			foundit = editPoint.FindPattern($"foreach (var subitem in {childModel.ClassName}Collection.Items)");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern("item = await UpdateAsync");
				editPoint.EndOfLine();
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach (var subitem in {childModel.ClassName}Collection.Items)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"var matchingItem = item.{memberName}.FirstOrDefault(m => m.HRef == subitem.HRef);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 4);
				editPoint.Insert($"if (matchingItem != null)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert($"await UpdateAsync<{childModel.ClassName}>(subitem);");
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
				editPoint.Insert($"await DeleteAsync<{childModel.ClassName}>(dnode);");
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
				editPoint.Insert($"subitem.{parentModel.ClassName} = item.HRef;");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("subitem.HRef = (await AddAsync(subitem)).HRef;");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
			}
		}

		/// <summary>
		/// Modify teh patch function to accomodate the new collection
		/// </summary>
		/// <param name="aFunction"></param>
		private void ModifyPatch(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string parentValidatorName, string childValidatorName, string memberName)
		{
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			bool foundit = editPoint.FindPattern("var baseCommands = new List<PatchCommand>();");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.LineDown();
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("var baseCommands = new List<PatchCommand>();");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert("foreach (var command in commands)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"if (typeof({parentModel.ClassName}).GetProperties().FirstOrDefault(p => p.Name.Equals(command.Path, StringComparison.OrdinalIgnoreCase)) != null)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("baseCommands.Add(command);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert($"var subNode = RqlNode.Parse($\"{parentModel.ClassName}=uri:\\\"{{node.Value<Uri>(1).LocalPath}}\\\"\");");
				editPoint.InsertNewLine();
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"await {parentModel.ClassName}Validator.ValidateForPatchAsync(commands, node, User)");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"await {parentModel.ClassName}Validator.ValidateForPatchAsync(commands, node, User)");
				editPoint.FindPattern("commands");
				editPoint.ReplaceText(8, "baseCommands", 0);
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"await PatchAsync<{parentModel.ClassName}>(commands, node);");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);
			if (foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"await PatchAsync<{parentModel.ClassName}>(commands, node);");
				editPoint.FindPattern("commands");
				editPoint.ReplaceText(8, "baseCommands", 0);
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"var {childModel.ClassName}Collection = ");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"var subNode = ");
				editPoint.LineDown();
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var {childModel.ClassName}Array = {childModel.ClassName}Collection.Items.ToArray();");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert("foreach (var command in commands)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("var parts = command.Path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 4);
				editPoint.Insert("if ( parts.Length > 1)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("var sections = parts[0].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 5);
				editPoint.Insert("if (sections.Length > 1)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("var index = Convert.ToInt32(sections[1]);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 6);
				editPoint.Insert($"if (index < {childModel.ClassName}Collection.Count)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert($"if (sections[0].Equals(\"{memberName}\"))");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert($"var childNode = RqlNode.Parse($\"HRef=uri:\\\"{{{childModel.ClassName}Array[index].HRef}}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("StringBuilder newPath = new();");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("bool first = true;");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("for (int i = 1; i < parts.Length; i++)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("if ( first ) { first = false;  } else { newPath.Append('.'); }");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("newPath.Append(parts[i]);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("}");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("var cmd = new PatchCommand {");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Op = command.Op,");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Path = newPath.ToString(),");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Value = command.Value");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("};");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("var cmds = new PatchCommand[] { cmd };");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert($"await {childValidatorName}.ValidateForPatchAsync(cmds, childNode, User);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine();

				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"await {parentValidatorName}.ValidateForPatchAsync");
				editPoint.LineDown();
				editPoint.EndOfLine();
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert("foreach (var command in commands)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("var parts = command.Path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 4);
				editPoint.Insert("if ( parts.Length > 1)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("var sections = parts[0].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 5);
				editPoint.Insert("if (sections.Length > 1)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("var index = Convert.ToInt32(sections[1]);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 6);
				editPoint.Insert($"if (index < {childModel.ClassName}Collection.Count)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert($"if (sections[0].Equals(\"{ memberName}\"))");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert($"var childNode = RqlNode.Parse($\"HRef=uri:\\\"{{{childModel.ClassName}Array[index].HRef}}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("StringBuilder newPath = new();");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("bool first = true;");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("for (int i = 1; i < parts.Length; i++)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("if ( first ) { first = false;  } else { newPath.Append('.'); }");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("newPath.Append(parts[i]);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("}");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("var cmd = new PatchCommand {");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Op = command.Op,");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Path = newPath.ToString(),");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 9);
				editPoint.Insert("Value = command.Value");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert("};");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 8);
				editPoint.Insert("var cmds = new PatchCommand[] { cmd };");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 8);
				editPoint.Insert($"await PatchAsync<{childModel.ClassName}>(cmds, childNode);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
			}
		}

		/// <summary>
		/// Modify the add function to accomodate the new collection
		/// </summary>
		/// <param name="aFunction"></param>
		private void ModifyAdd(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string parentValidatorName, string childValidatorName, string memberName)
		{
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			bool foundit = editPoint.FindPattern("return await AddAsync");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (foundit)
			{
				editPoint.ReplaceText(6, "item =", 0);
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("return item;");
			}
			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"foreach ( var subitem in item.{memberName})");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern($"await {parentValidatorName}.ValidateForAddAsync(item, User);");
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach ( var subitem in item.{memberName})");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"await {childValidatorName}.ValidateForAddAsync(subitem, User);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
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
				editPoint.Indent(null, 3);
				editPoint.Insert($"foreach ( var subitem in item.{memberName})");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"subitem.{parentModel.ClassName} = item.HRef;");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"subitem.HRef = (await AddAsync<{childModel.ClassName}>(subitem)).HRef;");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
			}
		}

		/// <summary>
		/// Modify the get collection function to accomodate the new collection
		/// </summary>
		/// <param name="aFunction"></param>
		private void ModifyGetCollection(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string childValidatorName, string memberName)
		{
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

			bool foundit = editPoint.FindPattern($"return await GetCollectionAsync<{parentModel.ClassName}>");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (foundit)
			{
				editPoint.ReplaceText(6, "var collection =", 0);
				editPoint.EndOfLine();
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert("return collection;");
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"StringBuilder rqlBody = new(\"in({parentModel.ClassName}\");");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);
			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern("return collection");
				editPoint.LineUp();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine();

				editPoint.LineUp(3);
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("if (collection.Count > 0)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"StringBuilder rqlBody = new(\"in({parentModel.ClassName}\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"foreach (var item in collection.Items)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("rqlBody.Append($\", uri:\\\"{item.HRef.LocalPath}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"rqlBody.Append(')');");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 4);
				editPoint.Insert($"var subNode = RqlNode.Parse(rqlBody.ToString());");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"var selectNode = node?.ExtractSelectClause();");
				editPoint.InsertNewLine();
			}

			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"var {childModel.ClassName}Collection =");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
				editPoint.FindPattern("return collection");
				editPoint.LineUp(2);
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"if (selectNode == null || selectNode.SelectContains(\"{memberName}\"))");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 5);
				editPoint.Insert($"foreach ( var item in {childModel.ClassName}Collection.Items)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert($"var mainItem = collection.Items.FirstOrDefault(i => i.HRef == item.{parentModel.ClassName});");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 6);
				editPoint.Insert($"if (mainItem.{memberName} == null)");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert($"mainItem.{memberName} = new {childModel.ClassName}[] {{ item }};");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("else");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 7);
				editPoint.Insert($"mainItem.{memberName} = new List<{childModel.ClassName}>(mainItem.{memberName}) {{ item }}.ToArray();");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 6);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 5);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
			}
		}

		/// <summary>
		/// Modify the Get Single function to populate the new collection
		/// </summary>
		/// <param name="aFunction">The <see cref="CodeFunction2"/> instance of the get single function.</param>
		private void ModifyGetSingle(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string parentValidatorName, string childValidatorName, string memberName, string memberType)
		{
			//	Find were it returns the GetSingleAsync (this may or may not be there)
			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

			bool foundit = editPoint.FindPattern($"return await GetSingleAsync<{parentModel.ClassName}>(node);");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (foundit)
			{
				//	We found it, so replace it with an assignment.
				editPoint.ReplaceText(6, "var item =", 0);
				editPoint.EndOfLine();
				editPoint.InsertNewLine();

				//	And return that item.
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var subNode = RqlNode.Parse($\"{memberName}=uri:\\\"{{item.HRef.LocalPath}}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("var selectNode = node?.ExtractSelectClause();");
				editPoint.InsertNewLine(2);
				editPoint.Indent(null, 3);
				editPoint.Insert("return item;");
			}
			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern("var subNode = RqlNode");
			foundit = foundit && editPoint.LessThan(aFunction.EndPoint);

			if (!foundit)
			{
				editPoint = (EditPoint2)aFunction.EndPoint.CreateEditPoint();
				editPoint.LineUp();
				editPoint.Indent(null, 3);
				editPoint.Insert($"var subNode = RqlNode.Parse($\"{memberName}=uri:\\\"{{item.HRef.LocalPath}}\\\"\");");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("var selectNode = node?.ExtractSelectClause();");
				editPoint.InsertNewLine(2);
			}
			editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();
			foundit = editPoint.FindPattern($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
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
				editPoint.Indent(null, 3);
				editPoint.Insert($"if (selectNode == null || selectNode.SelectContains(\"{memberName}\"))");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("{");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);
				editPoint.Insert($"var {childModel.ClassName}Collection = await GetCollectionAsync<{childModel.ClassName}>(null, subNode, true);");
				editPoint.InsertNewLine();
				editPoint.Indent(null, 4);

				if (memberType.StartsWith("List"))
					editPoint.Insert($"item.{memberName} = {childModel.ClassName}Collection.Items.ToList();");
				else if (memberType.EndsWith("[]"))
					editPoint.Insert($"item.{memberName} = {childModel.ClassName}Collection.Items;");
				else
					editPoint.Insert($"item.{memberName} = {childModel.ClassName}Collection.Items;");

				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert("}");
				editPoint.InsertNewLine();
			}
		}

		/// <summary>
		/// Modify the consructor to add and assign the needed validator for the child items
		/// </summary>
		/// <param name="aFunction"></param>
		private void ModifyConstructor(CodeFunction2 aFunction, ResourceModel parentModel, ResourceModel childModel, string parentValidatorName, string childValidatorName, string childValidatorInterface, string memberName)
		{
			//	This is the constructor function. We need that new validator, and we get it using dependency
			//	injection. That means, it needs to be an argument in the aruguement list of the constructor.
			//	
			//	If it's already there, then no problem, just move on. But if it isn't there, then we need 
			//	to add it and assign it's value to the new validator member we created (or found).
			//
			//	Let's start by assuming we're going to need to create it.
			var shouldAddArgument = true;
			var parameterName = childValidatorName;
			parameterName = parameterName.Substring(0, 1).ToLower() + parameterName.Substring(1);

			//	Look at each argument...
			foreach (CodeParameter2 arg in aFunction.Parameters.OfType<CodeParameter2>())
			{
				//	if any one has a type of the interface for the new member, the the argument already
				//	exists, and we don't have to create it.
				if (arg.Type.AsString.EndsWith(childValidatorInterface, StringComparison.OrdinalIgnoreCase))
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
				aFunction.AddParameter(parameterName, childValidatorInterface, -1);
			}

			var editPoint = (EditPoint2)aFunction.StartPoint.CreateEditPoint();

			if (!editPoint.FindPattern($"{childValidatorName} ="))
			{
				//	Now, within the function, add the assignment.
				editPoint = (EditPoint2)aFunction.EndPoint.CreateEditPoint();
				editPoint.LineUp();
				editPoint.EndOfLine();
				editPoint.InsertNewLine();
				editPoint.Indent(null, 3);
				editPoint.Insert($"{childValidatorName} = {parameterName};");
			}
		}
		private void HandleMessages()
		{
			while (WinNative.PeekMessage(out WinNative.NativeMessage msg, IntPtr.Zero, 0, (uint)0xFFFFFFFF, 1) != 0)
			{
				WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
			}
		}
		#endregion

	}
}
