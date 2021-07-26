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
    public class MapperWizard : IWizard
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
			ProgressDialog progressDialog = new ProgressDialog("Loading classes and preparing project...");
			Mapper mapperDialog = new Mapper();

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				var entityMap = StandardUtils.OpenEntityMap(_appObject.Solution);

				HandleMessages();

				var programfiles = StandardUtils.LoadProgramDetail(_appObject.Solution);
				HandleMessages();

				var classList = StandardUtils.LoadClassList(programfiles);
				HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					EntityMap = entityMap,
					InstallType = 1
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				progressDialog = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

					var emitter = new StandardEmitter();
					var model = emitter.EmitMappingModel(entityClassFile,
														 resourceClassFile,
														 replacementsDictionary["$safeitemname$"],
														 replacementsDictionary);

					mapperDialog.resourceClassFile = resourceClassFile;
					mapperDialog.entityClassFile = entityClassFile; 

					if (mapperDialog.ShowDialog() == DialogResult.OK)
					{
						replacementsDictionary.Add("$model$", model);
						replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
						replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNameSpace);
						Proceed = true;
					}
					else
						Proceed = false;
				}
				else
					Proceed = false;
			}
			catch ( Exception error )
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
