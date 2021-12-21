using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

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

                    var result = MessageBox.Show($"You are attempting to install a resource model into {codeService.GetRelativeFolder(installationFolder)}. Typically, resource models reside in {codeService.GetRelativeFolder(resourceModelsFolder)}.\r\n\r\nDo you wish to place the new resource model in this non-standard location?",
                        "Warning: Non-Standard Location",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        Proceed = false;
                        return;
                    }

                    mDte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

                    resourceModelsFolder = installationFolder;

                    projectMapping.ResourceFolder = installationFolder.Folder;
                    projectMapping.ResourceNamespace = installationFolder.Namespace;
                    projectMapping.ResourceProject = installationFolder.ProjectName;

                    codeService.SaveProjectMapping();
                }

                var form = new UserInputResource()
                {
                    DefaultConnectionString = connectionString,
                    ResourceModelsFolder = projectMapping.GetResourceModelsFolder()
                };

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var standardEmitter = new StandardEmitter();
                    var undefinedModels = form.UndefinedResources;

                    standardEmitter.GenerateResourceComposites(undefinedModels,
                                                               projectMapping.GetResourceModelsFolder(),
                                                               form.ConnectionString);

                    var entityModel = (EntityClass)form._entityModelList.SelectedItem;
                    var resourceClassName = replacementsDictionary["$safeitemname$"];

                    string model;

                    if (form._GenerateAsEnum.Checked)
                        model = standardEmitter.EmitResourceEnum(entityModel,
                                                                 form.ConnectionString);
                    else
                        model = standardEmitter.EmitResourceModel(entityModel,
                                                                  replacementsDictionary);

                    replacementsDictionary.Add("$model$", model);
                    replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);
                    Proceed = true;
                }
                else
                    Proceed = false;
            }
            catch ( Exception error)
            {
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

        public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
