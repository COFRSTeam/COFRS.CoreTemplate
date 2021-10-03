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

				//  Load the project mapping information
				var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
				HandleMessages();

				var installationFolder = StandardUtils.GetInstallationFolder(_appObject);
				HandleMessages();

                projectMapping = StandardUtils.LoadProjectMapping(_appObject,
                                                    projectMapping,
                                                    installationFolder,
                                                    out ProjectFolder entityModelsFolder,
                                                    out ProjectFolder resourceModelsFolder,
                                                    out ProjectFolder mappingFolder,
													out ProjectFolder validationFolder,
													out ProjectFolder controllersFolder);
                HandleMessages();

				var connectionString = StandardUtils.GetConnectionString(_appObject.Solution);
				HandleMessages();

				//  Make sure we are where we're supposed to be
				if (!StandardUtils.IsChildOf(mappingFolder.Folder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

					var result = MessageBox.Show($"You are attempting to install a resource/entity mapping model into {StandardUtils.GetRelativeFolder(_appObject.Solution, installationFolder)}. Typically, resource/entity mapping models reside in {StandardUtils.GetRelativeFolder(_appObject.Solution, mappingFolder)}.\r\n\r\nDo you wish to place the new resource/entity mapping model in this non-standard location?",
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

					mappingFolder = installationFolder;

					projectMapping.MappingFolder = mappingFolder.Folder;
					projectMapping.MappingNamespace = mappingFolder.Namespace;
					projectMapping.MappingProject = mappingFolder.ProjectName;

					StandardUtils.SaveProjectMapping(_appObject.Solution, projectMapping);
				}

				var entityMap = StandardUtils.LoadEntityModels(_appObject.Solution, entityModelsFolder);
				HandleMessages();

				var defultServerType = StandardUtils.GetDefaultServerType(connectionString);

				var resourceMap = StandardUtils.LoadResourceModels(_appObject.Solution, entityMap, resourceModelsFolder, defultServerType);
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

					mapperDialog.ResourceModel = resourceModel;
					mapperDialog.ResourceModels = resourceMap.Maps.ToList();
					mapperDialog.EntityModels = entityMap.Maps.ToList();

					if (mapperDialog.ShowDialog() == DialogResult.OK)
					{
						StandardUtils.SaveProfileMap(_appObject.Solution, mapperDialog.ProfileMap);

						var emitter = new StandardEmitter();
						var model = emitter.EmitMappingModel(resourceModel, resourceModel.EntityModel, mapperDialog.ProfileMap, replacementsDictionary["$safeitemname$"], replacementsDictionary);

						replacementsDictionary["$resourcenamespace$"] = resourceModel.Namespace;
						replacementsDictionary["$entitynamespace$"] = resourceModel.EntityModel.Namespace;
						replacementsDictionary["$model$"] = model;

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
