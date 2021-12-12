using COFRS.Template.Common.Forms;
using COFRS.Template.Common.ServiceUtilities;
using COFRSCoreCommon.Forms;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			DTE2 dte2 = automationObject as DTE2;
			ProgressForm progressDialog = null;

            try
            {
                //	Show the user that we are busy doing things...
                progressDialog = new ProgressForm("Loading classes and preparing project...");
                progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
                dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
                HandleMessages();

                //  Load the project mapping information
                var projectMapping = COFRSCommonUtilities.OpenProjectMapping(dte2);
                HandleMessages();

                var installationFolder = COFRSCommonUtilities.GetInstallationFolder(dte2);
                HandleMessages();

                //  Make sure we are where we're supposed to be
                if ( !COFRSCommonUtilities.IsChildOf(projectMapping.EntityFolder, installationFolder.Folder))
                {
                    HandleMessages();
                    progressDialog.Close();
                    dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                    var entityModelsFolder = projectMapping.GetEntityModelsFolder();

                    var result = MessageBox.Show($"You are attempting to install an entity model into {COFRSCommonUtilities.GetRelativeFolder(dte2, installationFolder)}. Typically, entity models reside in {COFRSCommonUtilities.GetRelativeFolder(dte2, entityModelsFolder)}.\r\n\r\nDo you wish to place the new entity model in this non-standard location?", 
                        "Warning: Non-Standard Location", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        Proceed = false;
                        return;
                    }

                    progressDialog = new ProgressForm("Loading classes and preparing project...");
                    progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
                    dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
                    HandleMessages();

                    entityModelsFolder = installationFolder;

                    projectMapping.EntityFolder = entityModelsFolder.Folder;
                    projectMapping.EntityNamespace = entityModelsFolder.Namespace;
                    projectMapping.EntityProject = entityModelsFolder.ProjectName;

                    COFRSCommonUtilities.SaveProjectMapping(dte2, projectMapping);
                }

                var projectName = projectMapping.EntityProject;
                var connectionString = COFRSCommonUtilities.GetConnectionString(dte2);
                HandleMessages();

                var entityMap = COFRSCommonUtilities.LoadEntityMap(dte2);
                HandleMessages();

                //	Construct the form, and fill in all the prerequisite data
                var form = new UserInputEntity
                {
                    ReplacementsDictionary = replacementsDictionary,
                    EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
                    DefaultConnectionString = connectionString,
                    EntityMap = entityMap
                };

                HandleMessages();

                progressDialog.Close();
                dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    //	Show the user that we are busy...
                    progressDialog = new ProgressForm("Building classes...");
                    progressDialog.Show(new WindowClass((IntPtr)dte2.ActiveWindow.HWnd));
                    dte2.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

                    HandleMessages();

                    connectionString = $"{form.ConnectionString}Application Name={projectName}";

                    //	Replace the default connection string in the appSettings.Local.json, so that the 
                    //	user doesn't have to do it. Note: this function only replaces the connection string
                    //	if the appSettings.Local.json contains the original placeholder connection string.
                    COFRSCommonUtilities.ReplaceConnectionString(dte2, connectionString);

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

                        standardEmitter.GenerateComposites(dte2,
                                                           undefinedEntityModels,
                                                           form.ConnectionString,
                                                           replacementsDictionary,
                                                           entityMap,
                                                           projectMapping.GetEntityModelsFolder());
                        HandleMessages();
                    }

                    string model = string.Empty;

                    if (form.EType == ElementType.Enum)
                    {
                        var entityModel = new EntityModel()
                        {
                            ClassName = COFRSCommonUtilities.NormalizeClassName(replacementsDictionary["$safeitemname$"]),
                            SchemaName = form.DatabaseTable.Schema,
                            TableName = form.DatabaseTable.Table,
                            Namespace = replacementsDictionary["$rootnamespace$"],
                            ElementType = ElementType.Enum,
                            ServerType = form.ServerType,
                            ProjectName = projectMapping.EntityProject,
                            Folder = Path.Combine(projectMapping.EntityFolder, replacementsDictionary["$safeitemname$"])
                        };

                        DBHelper.GenerateEnumColumns(entityModel, form.ConnectionString);
                        model = standardEmitter.EmitEntityEnum(entityModel);
                        replacementsDictionary["$npgsqltypes$"] = "true";

                        COFRSCommonUtilities.RegisterComposite(dte2, entityModel);
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
                            Folder = Path.Combine(projectMapping.EntityFolder, replacementsDictionary["$safeitemname$"])
                        };

                        DBHelper.GenerateColumns(entityModel, form.ConnectionString);

                        model = standardEmitter.EmitComposite(dte2,
                                                              entityModel,
                                                              form.ConnectionString,
                                                              replacementsDictionary,
                                                              entityMap,
                                                              ref undefinedElements,
                                                              projectMapping.GetEntityModelsFolder());

                        replacementsDictionary["$npgsqltypes$"] = "true";


                        COFRSCommonUtilities.RegisterComposite(dte2, entityModel);
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
                            Folder = Path.Combine(projectMapping.EntityFolder, replacementsDictionary["$safeitemname$"]),
                            Columns = form.DatabaseColumns.ToArray()
                        };

                        model = standardEmitter.EmitEntityModel(entityModel,
                                                                entityMap,
                                                                replacementsDictionary);

                        var existingEntities = entityMap.Maps.ToList();

                        entityModel.Folder = Path.Combine(projectMapping.EntityFolder, replacementsDictionary["$safeitemname$"]);

                        existingEntities.Add(entityModel);
                        entityMap.Maps = existingEntities.ToArray();
                    }

                    replacementsDictionary.Add("$entityModel$", model);
                    HandleMessages();

                    progressDialog.Close();
                    dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

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

				dte2.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
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
