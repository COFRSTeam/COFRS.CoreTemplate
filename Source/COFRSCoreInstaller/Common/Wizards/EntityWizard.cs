using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
    public class EntityWizard : IWizard
	{
		private bool Proceed = false;

        public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
            codeService.AddEntity(projectItem);
		}

		public void RunFinished()
		{
		}

		/// <summary>
		/// Start generating the entity model
		/// </summary>
		/// <param name="automationObject"></param>
		/// <param name="replacementsDictionary"></param>
		/// <param name="runKind"></param>
		/// <param name="customParams"></param>
		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = automationObject as DTE2;
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

            try
            {
                var projectMapping = codeService.LoadProjectMapping();
                var installationFolder = codeService.InstallationFolder;
                var connectionString = codeService.ConnectionString;

                //  Make sure we are where we're supposed to be
                if ( !codeService.IsChildOf(projectMapping.EntityFolder, installationFolder.Folder))
                {
                    if (!VsShellUtilities.PromptYesNo(
                                $"You are attempting to install an entity model into {codeService.GetRelativeFolder(installationFolder)}. Typically, entity models reside in {codeService.GetRelativeFolder(projectMapping.GetEntityModelsFolder())}.\r\n\r\nDo you wish to place the new entity model in this non-standard location?",
                                "Microsoft Visual Studio",
                                OLEMSGICON.OLEMSGICON_WARNING,
                                shell))
                    {
                        Proceed = false;
                        return;
                    }

                    projectMapping.EntityFolder = installationFolder.Folder;
                    projectMapping.EntityNamespace = installationFolder.Namespace;
                    projectMapping.EntityProject = installationFolder.ProjectName;

                    codeService.SaveProjectMapping();
                }

                //	Construct the form, and fill in all the prerequisite data
                var form = new NewEntityDialog
                {
                    ReplacementsDictionary = replacementsDictionary,
                    EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
                    DefaultConnectionString = connectionString,
                    ServiceProvider = ServiceProvider.GlobalProvider
                };

                var isok = form.ShowDialog();

                if (isok.HasValue && isok.Value == true)
                {
                    try
                    {
                        //	Replace the default connection string in the appSettings.Local.json, so that the 
                        //	user doesn't have to do it. Note: this function only replaces the connection string
                        //	if the appSettings.Local.json contains the original placeholder connection string.
                        codeService.ConnectionString = $"{form.ConnectionString}Application Name={mDte.Solution.FullName}";

                        //	We will need these when we replace placeholders in the class
                        var className = replacementsDictionary["$safeitemname$"];
                        replacementsDictionary["$entityClass$"] = className;

                        var standardEmitter = new Emitter();

                        if (form.ServerType == DBServerType.POSTGRESQL)
                        {
                            //	Generate any undefined composits before we construct our entity model (because, 
                            //	the entity model depends upon them)

                            standardEmitter.GenerateComposites(form.UndefinedEntityModels,
                                                               form.ConnectionString,
                                                               replacementsDictionary,
                                                               projectMapping.GetEntityModelsFolder());
                        }

                        string model = string.Empty;

                        if (form.EType == ElementType.Enum)
                        {
                            var columns = DBHelper.GenerateEnumColumns(form.DatabaseTable.Schema,
                                                                       form.DatabaseTable.Table,
                                                                       form.ConnectionString);

                            model = standardEmitter.EmitEntityEnum(replacementsDictionary["$safeitemname$"],
                                                                   form.DatabaseTable.Schema,
                                                                   form.DatabaseTable.Table,
                                                                   columns);

                            replacementsDictionary["$npgsqltypes$"] = "true";

                            codeService.RegisterComposite(replacementsDictionary["$safeitemname$"],
                                                                   replacementsDictionary["$rootnamespace$"],
                                                                   ElementType.Enum,
                                                                   form.DatabaseTable.Table);
                        }
                        else if (form.EType == ElementType.Composite)
                        {
                            var  columns = DBHelper.GenerateColumns(form.DatabaseTable.Schema, form.DatabaseTable.Table, form.ServerType, form.ConnectionString);

                            model = standardEmitter.EmitComposite(replacementsDictionary["$safeitemname$"],
                                                                  form.DatabaseTable.Schema,
                                                                  form.DatabaseTable.Table,
                                                                  ElementType.Composite,
                                                                  columns,
                                                                  form.ConnectionString,
                                                                  replacementsDictionary,
                                                                  projectMapping.GetEntityModelsFolder());

                            replacementsDictionary["$npgsqltypes$"] = "true";

                            codeService.RegisterComposite(replacementsDictionary["$safeitemname$"],
                                                                   replacementsDictionary["$rootnamespace$"],
                                                                   ElementType.Enum,
                                                                   form.DatabaseTable.Table);
                        }
                        else
                        {
                            model = standardEmitter.EmitEntityModel(replacementsDictionary["$safeitemname$"],
                                                                    form.DatabaseTable.Schema,
                                                                    form.DatabaseTable.Table,
                                                                    form.ServerType,
                                                                    form.DatabaseColumns.ToArray(),
                                                                    replacementsDictionary);
                        }

                        replacementsDictionary.Add("$entityModel$", model);
                        Proceed = true;
                    }
                    catch ( Exception error )
                    {
                        MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Proceed = false;
                    }
                }
                else
                    Proceed = false;
            }
            catch (Exception error)
			{
                VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                                error.Message,
                                                "Microsoft Visual Studio",
                                                OLEMSGICON.OLEMSGICON_CRITICAL,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                Proceed = false;
			}
		}

        public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
