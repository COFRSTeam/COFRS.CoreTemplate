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
using System.Linq;
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

			try
			{
				//	Show the user that we are busy doing things...
				progressDialog.Show(new WindowClass((IntPtr)_appObject.ActiveWindow.HWnd));
				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);

				//  Load the project mapping information
				var projectMapping = COFRSCommonUtilities.OpenProjectMapping(_appObject);
				HandleMessages();
				
				var installationFolder = COFRSCommonUtilities.GetInstallationFolder(_appObject);
				HandleMessages();

				var connectionString = COFRSCommonUtilities.GetConnectionString(_appObject);
				HandleMessages();

				//  Make sure we are where we're supposed to be
				if (!COFRSCommonUtilities.IsChildOf(projectMapping.MappingFolder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
					var mappingFolder = projectMapping.GetMappingFolder();

					var result = MessageBox.Show($"You are attempting to install a resource/entity mapping model into {COFRSCommonUtilities.GetRelativeFolder(_appObject, installationFolder)}. Typically, resource/entity mapping models reside in {COFRSCommonUtilities.GetRelativeFolder(_appObject, mappingFolder)}.\r\n\r\nDo you wish to place the new resource/entity mapping model in this non-standard location?",
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

					projectMapping.MappingFolder = installationFolder.Folder;
					projectMapping.MappingNamespace = installationFolder.Namespace;
					projectMapping.MappingProject = installationFolder.ProjectName;

					COFRSCommonUtilities.SaveProjectMapping(_appObject, projectMapping);
				}

				var entityMap = COFRSCommonUtilities.LoadEntityMap(_appObject);	
				HandleMessages();

				var defultServerType = COFRSCommonUtilities.GetDefaultServerType(_appObject);

				var resourceMap = COFRSCommonUtilities.LoadResourceMap(_appObject);
				HandleMessages();

				var form = new UserInputGeneral()
				{
					DefaultConnectionString = connectionString,
					EntityMap = entityMap,
					ResourceMap = resourceMap,
					InstallType = 1
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				progressDialog = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					var resourceModel = (ResourceModel)form._resourceModelList.SelectedItem;
					var profileMap = COFRSCommonUtilities.GenerateProfileMap(resourceModel, resourceMap);

					var emitter = new StandardEmitter();
					var model = emitter.EmitMappingModel(resourceModel, resourceModel.EntityModel, profileMap, replacementsDictionary["$safeitemname$"], replacementsDictionary);

					replacementsDictionary["$resourcenamespace$"] = resourceModel.Namespace;
					replacementsDictionary["$entitynamespace$"] = resourceModel.EntityModel.Namespace;
					replacementsDictionary["$model$"] = model;

					Proceed = true;
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
