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
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressDialog progressDialog = null;

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
				HandleMessages();

				var entityModelsFolder = StandardUtils.FindEntityModelsFolder(_appObject.Solution);
				HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				var programfiles = StandardUtils.LoadProgramDetail(_appObject.Solution);
				HandleMessages();

				var classList = StandardUtils.LoadClassList(programfiles);
				HandleMessages();

				//	Construct the form, and fill in all the prerequisite data
				var form = new UserInputEntity
				{
					ReplacementsDictionary = replacementsDictionary,
					EntityModelsFolder = entityModelsFolder,
					DefaultConnectionString = connectionString,
					ClassList = classList,
					Members = programfiles
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

					var projectName = StandardUtils.GetProjectName(_appObject.Solution);
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
					List<ClassFile> composits = form.UndefinedClassList;

					var emitter = new Emitter();
					var standardEmitter = new StandardEmitter();

					if (form.ServerType == DBServerType.POSTGRESQL)
					{
						//	Generate any undefined composits before we construct our entity model (because, 
						//	the entity model depends upon them)

						var definedList = new List<ClassFile>();
						definedList.AddRange(classList);
						definedList.AddRange(composits);

						standardEmitter.GenerateComposites(composits, 
							                               form.ConnectionString, 
														   replacementsDictionary, 
														   definedList,
														   entityModelsFolder.Folder);
						HandleMessages();

						foreach (var composite in composits)
						{
							//	TO DO: This is incorret - the item could reside in another project
							var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
							pj.Project.ProjectItems.AddFromFile(composite.FileName);

							StandardUtils.RegisterComposite(_appObject.Solution, (EntityClassFile) composite);
						}

						classList.AddRange(composits);
					}

					string entityModel = string.Empty;

					if (form.eType == ElementType.Enum)
					{
						entityModel = standardEmitter.EmitEnum(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], form.ConnectionString);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var entityclassFile = new EntityClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = replacementsDictionary["$rootnamespace$"],
							ElementType = ElementType.Enum,
						};

						StandardUtils.RegisterComposite(_appObject.Solution, entityclassFile);
					}
					else if (form.eType == ElementType.Composite)
					{
						var undefinedElements = new List<ClassFile>();
						entityModel = standardEmitter.EmitComposite(form.DatabaseTable.Schema, 
							                                        form.DatabaseTable.Table, 
																	replacementsDictionary["$safeitemname$"], 
																	form.ConnectionString, 
																	replacementsDictionary, 
																	classList, 
																	undefinedElements,
																	entityModelsFolder.Folder);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var classFile = new EntityClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = replacementsDictionary["$rootnamespace$"],
							ElementType = ElementType.Composite
						};

						StandardUtils.RegisterComposite(_appObject.Solution, classFile);
					}
					else
					{
						entityModel = standardEmitter.EmitEntityModel(form.ServerType, 
							                                          form.DatabaseTable, 
																	  replacementsDictionary["$safeitemname$"], 
																	  classList,
																	  form.DatabaseColumns, 
																	  replacementsDictionary, 
																	  out EntityClassFile entityClass);
					}

					replacementsDictionary.Add("$entityModel$", entityModel);
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
