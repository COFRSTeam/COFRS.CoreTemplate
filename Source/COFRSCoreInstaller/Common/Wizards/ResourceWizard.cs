using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;

namespace COFRS.Template.Common.Wizards
{
    public class ResourceWizard : IWizard
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
            codeService.AddResource(projectItem);
        }

        public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
            var mDte = automationObject as DTE2;
            var codeService = COFRSServiceFactory.GetService<ICodeService>();
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            IVsThreadedWaitDialog2 waitDialog = null;
            bool fpCanceled = false;

            try
            {
                var projectMapping = codeService.LoadProjectMapping();
                var installationFolder = codeService.InstallationFolder;
                var connectionString = codeService.ConnectionString;

                //  Make sure we are where we're supposed to be
                if (!codeService.IsChildOf(projectMapping.ResourceFolder, installationFolder.Folder))
                {
                    mDte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
                    var resourceModelsFolder = projectMapping.GetResourceModelsFolder();

                    if (!VsShellUtilities.PromptYesNo(
                            $"You are attempting to install a resource model into {codeService.GetRelativeFolder(installationFolder)}. Typically, resource models reside in {codeService.GetRelativeFolder(resourceModelsFolder)}.\r\n\r\nDo you wish to place the new resource model in this non-standard location?",                            "Microsoft Visual Studio",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            shell))
                    {
                        Proceed = false;
                        return;
                    }

                    resourceModelsFolder = installationFolder;

                    projectMapping.ResourceFolder = installationFolder.Folder;
                    projectMapping.ResourceNamespace = installationFolder.Namespace;
                    projectMapping.ResourceProject = installationFolder.ProjectName;

                    codeService.SaveProjectMapping();
                }

                var form = new NewResourceDialog()
                {
                    DefaultConnectionString = connectionString,
                    ResourceModelsFolder = projectMapping.GetResourceModelsFolder(),
                    ServiceProvider = ServiceProvider.GlobalProvider
                };

                var result = form.ShowDialog();

                if (form.DialogResult.HasValue && form.DialogResult.Value == true)
                {
                    if (ServiceProvider.GlobalProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) is IVsThreadedWaitDialogFactory dialogFactory)
                    {
                        dialogFactory.CreateInstance(out waitDialog);
                    }

                    if (waitDialog != null && waitDialog.StartWaitDialog("Microsoft Visual Studio",
                                                                 "Building resource model",
                                                                 $"Building {replacementsDictionary["$safeitemname$"]}",
                                                                 null,
                                                                 $"Building {replacementsDictionary["$safeitemname$"]}",
                                                                 0,
                                                                 false, true) == VSConstants.S_OK)
                    {
                        var standardEmitter = new Emitter();

                        if (form.UndefinedResources != null && form.UndefinedResources.Count > 0)
                        {
                            waitDialog.UpdateProgress($"Building resource model",
                                                      $"Building composites",
                                                      $"Building composites",
                                                      0,
                                                      0,
                                                      true,
                                                      out fpCanceled);

                            standardEmitter.GenerateResourceComposites(form.UndefinedResources,
                                                                       projectMapping.GetResourceModelsFolder(),
                                                                       form.ConnectionString);
                        }

                        var entityModel = form.EntityModel;

                        string model;

                        waitDialog.UpdateProgress($"Building resource model",
                                                  $"Building {replacementsDictionary["$safeitemname$"]}",
                                                  $"Building {replacementsDictionary["$safeitemname$"]}",
                                                  0,
                                                  0,
                                                  true,
                                                  out fpCanceled);

                        if (form.GenerateAsEnum)
                            model = standardEmitter.EmitResourceEnum(replacementsDictionary["$safeitemname$"],
                                                                     entityModel,
                                                                     form.ConnectionString);
                        else
                            model = standardEmitter.EmitResourceModel(replacementsDictionary["$safeitemname$"],
                                                                      entityModel,
                                                                      replacementsDictionary);

                        replacementsDictionary.Add("$model$", model);
                        replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);

                        waitDialog.EndWaitDialog(out int usercancel);
                    }

                    Proceed = true;
                }
                else
                    Proceed = false;
            }
            catch ( Exception error)
            {
                if ( waitDialog != null)
                {
                    waitDialog.EndWaitDialog(out int usercancel);
                }

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
