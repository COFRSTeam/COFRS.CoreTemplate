using COFRS.Template.Common.Forms;
using COFRS.Template.Common.ServiceUtilities;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
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
		}

		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressDialog progressDialog = null;

			try
            {
                //	Show the user that we are busy doing things...
                progressDialog = new ProgressDialog("Loading classes and preparing project...");
                progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
                _appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

                var projectMapping = COFRSCommonUtilities.OpenProjectMapping(_appObject);
                HandleMessages();

                var installationFolder = COFRSCommonUtilities.GetInstallationFolder(_appObject);
                HandleMessages();

                var connectionString = COFRSCommonUtilities.GetConnectionString(_appObject);
                HandleMessages();

                //  Make sure we are where we're supposed to be
                if (!COFRSCommonUtilities.IsChildOf(projectMapping.ResourceFolder, installationFolder.Folder))
                {
                    HandleMessages();

                    progressDialog.Close();
                    _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
                    var resourceModelsFolder = projectMapping.GetResourceModelsFolder();

                    var result = MessageBox.Show($"You are attempting to install a resource model into {COFRSCommonUtilities.GetRelativeFolder(_appObject, installationFolder)}. Typically, resource models reside in {COFRSCommonUtilities.GetRelativeFolder(_appObject, resourceModelsFolder)}.\r\n\r\nDo you wish to place the new resource model in this non-standard location?",
                        "Warning: Non-Standard Location",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        Proceed = false;
                        return;
                    }

                    progressDialog = new ProgressDialog("Loading classes and preparing project...");
                    progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
                    _appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
                    HandleMessages();

                    resourceModelsFolder = installationFolder;

                    projectMapping.ResourceFolder = installationFolder.Folder;
                    projectMapping.ResourceNamespace = installationFolder.Namespace;
                    projectMapping.ResourceProject = installationFolder.ProjectName;

                    COFRSCommonUtilities.SaveProjectMapping(_appObject, projectMapping);
                }


                var entityMap = COFRSCommonUtilities.LoadEntityMap(_appObject);
                HandleMessages();

                var defaultServerType = COFRSCommonUtilities.GetDefaultServerType(_appObject);

                var resourceMap = COFRSCommonUtilities.LoadResourceMap(_appObject);
                HandleMessages();

                var form = new UserInputResource()
                {
                    EntityMap = entityMap,
                    ResourceMap = resourceMap,
                    DefaultConnectionString = connectionString,
                    ResourceModelsFolder = projectMapping.GetResourceModelsFolder()
                };

                HandleMessages();

                progressDialog.Close();
                _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var standardEmitter = new StandardEmitter();
                    var undefinedModels = form.UndefinedResources;

                    standardEmitter.GenerateResourceComposites(_appObject,
                                                                undefinedModels,
                                                                projectMapping.GetResourceModelsFolder(),
                                                                entityMap,
                                                                resourceMap);

                    var entityModel = (EntityModel)form._entityModelList.SelectedItem;
                    var resourceClassName = replacementsDictionary["$safeitemname$"];

                    var resourceModel = new ResourceModel()
                    {
                        ProjectName = projectMapping.ResourceProject,
                        Namespace = projectMapping.ResourceNamespace,
                        Folder = Path.Combine(projectMapping.ResourceFolder, $"{resourceClassName}.cs"),
                        ClassName = resourceClassName,
                        EntityModel = entityModel,
                        ServerType = form.ServerType
                    };

                    string model;

                    if (form._GenerateAsEnum.Checked)
                        model = standardEmitter.EmitResourceEnum(resourceModel,
                                                                 form.ServerType,
                                                                 connectionString);
                    else
                        model = standardEmitter.EmitResourceModel(resourceModel,
                                                                  resourceMap,
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
				if (progressDialog != null)
					if (progressDialog.IsHandleCreated)
						progressDialog.Close();

				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

        public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
		private void HandleMessages()
		{
			while (WinNative.PeekMessage(out WinNative.NativeMessage msg, IntPtr.Zero, 0, (uint)0xFFFFFFFF, 1) != 0)
			{
				WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
			}
		}
	}
}
