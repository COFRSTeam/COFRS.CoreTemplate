﻿using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
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

                var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
                HandleMessages();

                var installationFolder = StandardUtils.GetInstallationFolder(_appObject);
                HandleMessages();

                //  Load the project mapping information
                projectMapping = LoadProjectMapping(_appObject,
                                                    projectMapping,
                                                    installationFolder,
                                                    out ProjectFolder entityModelsFolder,
                                                    out ProjectFolder resourceModelsFolder);

                HandleMessages();

                var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
                HandleMessages();

                //  Make sure we are where we're supposed to be
                if (!StandardUtils.IsChildOf(resourceModelsFolder.Folder, installationFolder.Folder))
                {
                    HandleMessages();

                    progressDialog.Close();
                    _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                    var result = MessageBox.Show($"You are attempting to install a resource model into {StandardUtils.GetRelativeFolder(_appObject.Solution, installationFolder)}. Typically, resource models reside in {StandardUtils.GetRelativeFolder(_appObject.Solution, resourceModelsFolder)}.\r\n\r\nDo you wish to place the new resource model in this non-standard location?",
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

                    projectMapping.ResourceFolder = resourceModelsFolder.Folder;
                    projectMapping.ResourceNamespace = resourceModelsFolder.Namespace;
                    projectMapping.ResourceProject = resourceModelsFolder.ProjectName;

                    StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
                }


                var entityMap = StandardUtils.LoadEntityModels(_appObject.Solution, entityModelsFolder);
                HandleMessages();

                var defaultServerType = StandardUtils.GetDefaultServerType(connectionString);

                var resourceMap = StandardUtils.LoadResourceModels(_appObject.Solution, entityMap, resourceModelsFolder, defaultServerType);
                HandleMessages();

                var form = new UserInputResource()
                {
                    EntityMap = entityMap,
                    ResourceMap = resourceMap,
                    DefaultConnectionString = connectionString,
                    ResourceModelsFolder = resourceModelsFolder
                };

                HandleMessages();

                progressDialog.Close();
                _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    var standardEmitter = new StandardEmitter();
                    var undefinedModels = form.UndefinedResources;

                    standardEmitter.GenerateResourceComposites(_appObject.Solution,
                                                                undefinedModels,
                                                                resourceModelsFolder,
                                                                entityMap,
                                                                resourceMap);

                    var entityModel = (EntityModel)form._entityClassList.SelectedItem;
                    var resourceClassName = replacementsDictionary["$safeitemname$"];

                    var resourceModel = new ResourceModel()
                    {
                        ProjectName = resourceModelsFolder.ProjectName,
                        Namespace = resourceModelsFolder.Namespace,
                        Folder = Path.Combine(resourceModelsFolder.Folder, $"{resourceClassName}.cs"),
                        ClassName = resourceClassName,
                        EntityModel = entityModel,
                        ServerType = form.ServerType
                    };

                    var model = standardEmitter.EmitResourceModel(resourceModel,
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

        private static ProjectMapping LoadProjectMapping(DTE2 _appObject, ProjectMapping projectMapping, ProjectFolder installationFolder, out ProjectFolder entityModelsFolder, out ProjectFolder resourceModelsFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectMapping == null)
            {
                entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);

                if (entityModelsFolder == null)
                    entityModelsFolder = installationFolder;

                resourceModelsFolder = StandardUtils.FindResourceModelsFolder(_appObject.Solution);

                if (resourceModelsFolder == null)
                    resourceModelsFolder = installationFolder;

                projectMapping = new ProjectMapping
                {
                    EntityFolder = entityModelsFolder.Folder,
                    EntityNamespace = entityModelsFolder.Namespace,
                    EntityProject = entityModelsFolder.ProjectName,
                    ResourceFolder = resourceModelsFolder.Folder,
                    ResourceNamespace = resourceModelsFolder.Namespace,
                    ResourceProject = resourceModelsFolder.ProjectName,
                    IncludeSDK = !string.Equals(entityModelsFolder.ProjectName, resourceModelsFolder.ProjectName, StringComparison.Ordinal)
                };

                StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(projectMapping.EntityProject) ||
                    string.IsNullOrWhiteSpace(projectMapping.EntityNamespace) ||
                    string.IsNullOrWhiteSpace(projectMapping.EntityFolder))
                {
                    entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);

                    if (entityModelsFolder == null)
                        entityModelsFolder = installationFolder;

                    projectMapping.EntityFolder = entityModelsFolder.Folder;
                    projectMapping.EntityNamespace = entityModelsFolder.Namespace;
                    projectMapping.EntityProject = entityModelsFolder.ProjectName;

                    StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
                }
                else
                {
                    entityModelsFolder = new ProjectFolder
                    {
                        Folder = projectMapping.EntityFolder,
                        Namespace = projectMapping.EntityNamespace,
                        ProjectName = projectMapping.EntityProject,
                        Name = Path.GetFileName(projectMapping.EntityFolder)
                    };
                }

                if (string.IsNullOrWhiteSpace(projectMapping.ResourceProject) ||
                    string.IsNullOrWhiteSpace(projectMapping.ResourceNamespace) ||
                    string.IsNullOrWhiteSpace(projectMapping.ResourceFolder))
                {
                    resourceModelsFolder = StandardUtils.FindResourceModelsFolder(_appObject.Solution);

                    if (resourceModelsFolder == null)
                        resourceModelsFolder = installationFolder;

                    projectMapping.ResourceFolder = resourceModelsFolder.Folder;
                    projectMapping.ResourceNamespace = resourceModelsFolder.Namespace;
                    projectMapping.ResourceProject = resourceModelsFolder.ProjectName;

                    StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
                }
                else
                {
                    resourceModelsFolder = new ProjectFolder
                    {
                        Folder = projectMapping.ResourceFolder,
                        Namespace = projectMapping.ResourceNamespace,
                        ProjectName = projectMapping.ResourceProject,
                        Name = Path.GetFileName(projectMapping.ResourceFolder)
                    };
                }
            }

            return projectMapping;
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
