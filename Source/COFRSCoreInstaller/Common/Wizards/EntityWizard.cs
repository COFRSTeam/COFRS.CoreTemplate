using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VSLangProj;

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
			DTE2 _appObject = automationObject as DTE2;
			ProgressDialog progressDialog = null;

            try
            {
                //	Show the user that we are busy doing things...
                progressDialog = new ProgressDialog("Loading classes and preparing project...");
                progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
                _appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
                HandleMessages();

                var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
                ProjectFolder entityModelsFolder = null;
                var installationFolder = StandardUtils.GetInstallationFolder(_appObject);

                //  Load the project mapping information
                if (projectMapping == null)
                {
                    entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);

                    if (entityModelsFolder == null)
                        entityModelsFolder = installationFolder;

                    projectMapping = new ProjectMapping
                    {
                        EntityFolder = entityModelsFolder.Folder,
                        EntityNamespace = entityModelsFolder.Namespace,
                        EntityProject = entityModelsFolder.ProjectName
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
                }
                
                //  Make sure we are where we're supposed to be
                if ( !StandardUtils.IsChildOf(entityModelsFolder.Folder, installationFolder.Folder))
                {
                    HandleMessages();

                    progressDialog.Close();
                    _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                    var result = MessageBox.Show($"You are attempting to install an entity model into {StandardUtils.GetRelativeFolder(_appObject.Solution, installationFolder)}. Typically, entity models reside in {StandardUtils.GetRelativeFolder(_appObject.Solution, entityModelsFolder)}.\r\n\r\nDo you wish to place the new entity model in this non-standard location?", 
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

                    entityModelsFolder = installationFolder;

                    projectMapping.EntityFolder = entityModelsFolder.Folder;
                    projectMapping.EntityNamespace = entityModelsFolder.Namespace;
                    projectMapping.EntityProject = entityModelsFolder.ProjectName;

                    StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
                }

                var projectName = entityModelsFolder.ProjectName;
                var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
                HandleMessages();

                var entityMap = StandardUtils.LoadEntityModels(_appObject.Solution, entityModelsFolder);
                HandleMessages();

                //	Construct the form, and fill in all the prerequisite data
                var form = new UserInputEntity
                {
                    ReplacementsDictionary = replacementsDictionary,
                    EntityModelsFolder = entityModelsFolder,
                    DefaultConnectionString = connectionString,
                    EntityMap = entityMap
                };

                HandleMessages();

                progressDialog.Close();
                _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    //	Show the user that we are busy...
                    progressDialog = new ProgressDialog("Building classes...");
                    progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
                    _appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

                    HandleMessages();

                    connectionString = $"{form.ConnectionString}Application Name={projectName}";

                    //	Replace the default connection string in the appSettings.Local.json, so that the 
                    //	user doesn't have to do it. Note: this function only replaces the connection string
                    //	if the appSettings.Local.json contains the original placeholder connection string.
                    StandardUtils.ReplaceConnectionString(_appObject.Solution, connectionString);

                    //	We well need these when we replace placeholders in the class
                    var className = replacementsDictionary["$safeitemname$"];
                    replacementsDictionary["$entityClass$"] = className;

                    //	Get the list of any undefined items that we encountered. (This list will only contain
                    //	items if we are using the Postgrsql database)
                    List<EntityModel> undefinedEntityModels = form.UndefinedEntityModels;

                    var emitter = new Emitter();
                    var standardEmitter = new StandardEmitter();

                    if (form.ServerType == DBServerType.POSTGRESQL)
                    {
                        //	Generate any undefined composits before we construct our entity model (because, 
                        //	the entity model depends upon them)

                        standardEmitter.GenerateComposites(_appObject.Solution, 
                                                           undefinedEntityModels,
                                                           form.ConnectionString,
                                                           replacementsDictionary,
                                                           entityMap,
                                                           entityModelsFolder);
                        HandleMessages();
                    }

                    string model = string.Empty;

                    if (form.EType == ElementType.Enum)
                    {
                        var entityModel = new EntityModel()
                        {
                            ClassName = StandardUtils.NormalizeClassName(replacementsDictionary["$safeitemname$"]),
                            SchemaName = form.DatabaseTable.Schema,
                            TableName = form.DatabaseTable.Table,
                            Namespace = replacementsDictionary["$rootnamespace$"],
                            ElementType = ElementType.Enum,
                            ServerType = form.ServerType,
                            ProjectName = entityModelsFolder.ProjectName,
                            Folder = Path.Combine(entityModelsFolder.Folder, replacementsDictionary["$safeitemname$"])
                        };

                        StandardUtils.GenerateEnumColumns(entityModel, form.ConnectionString);
                        model = standardEmitter.EmitEntityEnum(entityModel, form.ConnectionString);
                        replacementsDictionary["$npgsqltypes$"] = "true";

                        StandardUtils.RegisterComposite(_appObject.Solution, entityModel);
                    }
                    else if (form.EType == ElementType.Composite)
                    {
                        var undefinedElements = new List<EntityModel>();

                        var entityModel = new EntityModel()
                        {
                            ClassName = replacementsDictionary["$safeitemname$"],
                            SchemaName = form.DatabaseTable.Schema,
                            TableName = form.DatabaseTable.Table,
                            Namespace = replacementsDictionary["$rootnamespace$"],
                            ElementType = ElementType.Composite,
                            ServerType = form.ServerType,
                            Folder = Path.Combine(entityModelsFolder.Folder, replacementsDictionary["$safeitemname$"])
                        };

                        StandardUtils.GenerateColumns(entityModel, form.ConnectionString);

                        model = standardEmitter.EmitComposite(_appObject.Solution,
                                                              entityModel,
                                                              form.ConnectionString,
                                                              replacementsDictionary,
                                                              entityMap,
                                                              ref undefinedElements,
                                                              entityModelsFolder);

                        replacementsDictionary["$npgsqltypes$"] = "true";


                        StandardUtils.RegisterComposite(_appObject.Solution, entityModel);
                    }
                    else
                    {
                        var entityModel = new EntityModel()
                        {
                            ClassName = replacementsDictionary["$safeitemname$"],
                            SchemaName = form.DatabaseTable.Schema,
                            TableName = form.DatabaseTable.Table,
                            Namespace = replacementsDictionary["$rootnamespace$"],
                            ElementType = ElementType.Composite,
                            ServerType = form.ServerType,
                            Folder = Path.Combine(entityModelsFolder.Folder, replacementsDictionary["$safeitemname$"]),
                            Columns = form.DatabaseColumns.ToArray()
                        };

                        model = standardEmitter.EmitEntityModel(entityModel,
                                                                entityMap,
                                                                replacementsDictionary);

                        var existingEntities = entityMap.Maps.ToList();

                        entityModel.Folder = Path.Combine(entityModelsFolder.Folder, replacementsDictionary["$safeitemname$"]);

                        existingEntities.Add(entityModel);
                        entityMap.Maps = existingEntities.ToArray();
                    }

                    replacementsDictionary.Add("$entityModel$", model);
                    HandleMessages();

                    progressDialog.Close();
                    _appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                    Proceed = true;
                }
                else
                    Proceed = false;
            }
            catch (Exception error)
			{
				if (progressDialog != null)
					if ( progressDialog.IsHandleCreated)
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
