using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VSLangProj;

namespace COFRSCoreInstaller
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

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;

				//	Show the user that we are busy doing things...
				var parent = new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd);

				var progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(parent);
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				HandleMessages();

				var entityModelsFolder = Utilities.FindEntityModelsFolder(_appObject.Solution);

				var form = new UserInputEntity
				{
					ReplacementsDictionary = replacementsDictionary,
					EntityModelsFolder = entityModelsFolder,
					DefaultConnectionString = Utilities.GetConnectionString(_appObject.Solution),
					ClassList = Utilities.LoadEntityDetailClassList(_appObject.Solution)
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

				if (form.ShowDialog() == DialogResult.OK)
				{
					//	Show the user that we are busy...
					progressDialog = new ProgressDialog("Building classes...");
					progressDialog.Show(parent);
					_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

					HandleMessages();


					Proceed = true;
					var connectionString = form.ConnectionString;
					Utilities.ReplaceConnectionString(_appObject.Solution, connectionString);
					var className = replacementsDictionary["$safeitemname$"];
					replacementsDictionary["$entityClass$"] = className;

					List<EntityDetailClassFile> composits = form.UndefinedClassList;

					var emitter = new Emitter();
					emitter.GenerateComposites(composits, connectionString, replacementsDictionary, form.ClassList);
					HandleMessages();

					foreach (var composite in composits)
					{
						var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
						pj.Project.ProjectItems.AddFromFile(composite.FileName);

						Utilities.RegisterComposite(_appObject.Solution, composite);
					}

					var classList = form.ClassList;
					classList.AddRange(composits);

					var etype = DBHelper.GetElementType(form.DatabaseTable.Schema, form.DatabaseTable.Table, classList, connectionString);
					string entityModel = string.Empty;

					if (etype == ElementType.Enum)
					{
						entityModel = emitter.EmitEnum(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var entityclassFile = new EntityDetailClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = replacementsDictionary["$rootNamespace$"],
							ElementType = ElementType.Enum,
						};

						Utilities.RegisterComposite(_appObject.Solution, entityclassFile);
					}
					else if (etype == ElementType.Composite)
					{
						var undefinedElements = new List<EntityDetailClassFile>();
						entityModel = emitter.EmitComposite(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString, replacementsDictionary, form.ClassList, undefinedElements);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var classFile = new EntityDetailClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = replacementsDictionary["$rootNamespace$"],
							ElementType = ElementType.Composite
						};

						Utilities.RegisterComposite(_appObject.Solution, classFile);
					}
					else
					{
						entityModel = emitter.EmitEntityModel(form.DatabaseTable, replacementsDictionary["$safeitemname$"], form.DatabaseColumns, replacementsDictionary, connectionString);
					}

					replacementsDictionary.Add("$model$", entityModel);
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
