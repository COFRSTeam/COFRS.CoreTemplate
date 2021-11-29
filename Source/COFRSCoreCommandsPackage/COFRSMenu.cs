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
                        var dialog = new AddCollectionDialog();
                        dialog.ResourceName.Text = theClass.Name;
                        dialog._dte2 = dte2;

                        dialog.ShowDialog();
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

                                    dialog.ShowDialog();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
