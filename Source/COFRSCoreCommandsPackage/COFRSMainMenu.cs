using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using EnvDTE80;
using EnvDTE;
using COFRSCoreCommandsPackage.Forms;

namespace COFRSCoreCommandsPackage
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class COFRSMainMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int EditMappingCommandId = 0x0100;
        public const int AddCollectionCommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("094544da-4d1f-4d27-aab7-50face2d5016");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="COFRSMainMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private COFRSMainMenu(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, EditMappingCommandId);
            var menuItem = new OleMenuCommand(this.EditMapping, menuCommandID);
            menuItem.BeforeQueryStatus += new EventHandler(OnBeforeEditMapping);
            commandService.AddCommand(menuItem);

            menuCommandID = new CommandID(CommandSet, AddCollectionCommandId);
            menuItem = new OleMenuCommand(this.AddCollection, menuCommandID);
            menuItem.BeforeQueryStatus += new EventHandler(OnBeforeAddingCollection);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static COFRSMainMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in COFRSMainMenu's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new COFRSMainMenu(package, commandService);
        }

        private bool ShouldShowMappingEditor()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            var theActiveDocument = dte.ActiveDocument;

            if (theActiveDocument == null)
                return false;

            var model = theActiveDocument.ProjectItem.FileCodeModel;

            if (model == null)
                return false;

            foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
            {
                foreach (CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>())
                {
                    foreach (var parent in classElement.Bases.OfType<CodeClass2>())
                    {
                        if (parent.Name.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool ShouldShowAddCollection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            var theActiveDocument = dte.ActiveDocument;

            if (theActiveDocument == null)
                return false;

            var model = theActiveDocument.ProjectItem.FileCodeModel;

            if (model == null)
                return false;

            foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
            {
                foreach (CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>())
                {
                    foreach ( CodeAttribute2 attribute in classElement.Attributes.OfType<CodeAttribute2>())
                    {
                        if (attribute.Name.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        private void OnBeforeEditMapping(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand myCommand)
            {
                if (((OleMenuCommand)sender).CommandID.ID == EditMappingCommandId)
                {
                    myCommand.Enabled = ShouldShowMappingEditor();
                }
            }
        }

        private void OnBeforeAddingCollection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand myCommand)
            {
                if (((OleMenuCommand)sender).CommandID.ID == AddCollectionCommandId)
                {
                    myCommand.Enabled = ShouldShowAddCollection();
                }
            }
        }

        private void EditMapping(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var menuCommand = (MenuCommand)sender;

            string message = "Hello World";

            if (menuCommand.CommandID.ID == EditMappingCommandId)
            {
                message = "Edit the Mapping for a Profile file";
            }
            else if (menuCommand.CommandID.ID == AddCollectionCommandId)
            {
                message = "Add a collection to a resource.";
            }

            string title = "COFRS";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void AddCollection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand myCommand)
            {
                if (((OleMenuCommand)sender).CommandID.ID == AddCollectionCommandId)
                {
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                    var theActiveDocument = dte.ActiveDocument;

                    if (theActiveDocument == null)
                        return;

                    var model = theActiveDocument.ProjectItem.FileCodeModel;
                    var resourceName = string.Empty;

                    if (model == null)
                        return;

                    foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
                    {
                        foreach (CodeClass2 classElement in namespaceElement.Children.OfType<CodeClass2>())
                        {
                            foreach (CodeAttribute2 attribute in classElement.Attributes.OfType<CodeAttribute2>())
                            {
                                if (attribute.Name.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                                    resourceName = classElement.Name;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(resourceName))
                        return;

                    var dialog = new AddCollectionDialog();
                    dialog.ResourceName.Text = resourceName;
                    dialog._dte2 = dte;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                    }
                }
            }
        }
    }
}
