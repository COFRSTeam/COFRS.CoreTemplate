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
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
    public class ValidationWizard : IWizard
	{
		private bool Proceed = false;

		// This method is called before opening any item that
		// has the OpenInEditor attribute.
		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		// This method is only called for item templates,
		// not for project templates.
		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		// This method is called after the project is created.
		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject,
			Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
			ProgressDialog progressDialog = new ProgressDialog("Loading classes and preparing project...");

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

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
					ClassList = classList,
					InstallType = 3
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
					var model = emitter.EmitValidationModel(resourceClassFile.ClassName, replacementsDictionary["$safeitemname$"], out string validatorInterface);

					var orchestrationNamespace = StandardUtils.FindOrchestrationNamespace(_appObject.Solution);

					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNameSpace);

					StandardUtils.RegisterValidationModel(_appObject.Solution,
													  replacementsDictionary["$safeitemname$"],
													  replacementsDictionary["$rootnamespace$"]);
					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				Proceed = false;
			}
		}

		// This method is only called for item templates,
		// not for project templates.
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
