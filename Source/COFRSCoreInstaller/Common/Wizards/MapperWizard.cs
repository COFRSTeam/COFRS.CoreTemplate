using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
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

			//	Show the user that we are busy doing things...
			ProgressDialog progressDialog = new ProgressDialog("Loading classes and preparing project...");
			progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
			_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

			HandleMessages();

			var programfiles = StandardUtils.LoadProgramDetail(_appObject.Solution);
			HandleMessages();

			var classList = StandardUtils.LoadClassList(programfiles);
			HandleMessages();


			var form = new UserInputGeneral()
			{
				ClassList = classList,
				InstallType = 1
			};

			HandleMessages();

			progressDialog.Close();
			_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

			if (form.ShowDialog() == DialogResult.OK)
			{
				var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
				var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;

				var emitter = new Emitter();
				var model = emitter.EmitMappingModel(form.ServerType, 
													 resourceClassFile, 
													 entityClassFile,
													 replacementsDictionary["$safeitemname$"],
													 replacementsDictionary);

				replacementsDictionary.Add("$model$", model);
				replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
				replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNameSpace);
				Proceed = true;
			}
			else
				Proceed = false;
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
