using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
	public class ControllerWizard : IWizard
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
		public void ProjectItemFinishedGenerating(ProjectItem
			projectItem)
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

				var projectMapping = StandardUtils.OpenProjectMapping(_appObject.Solution);
				HandleMessages();

				var solutionPath = _appObject.Solution.Properties.Item("Path").Value.ToString();

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
				if (!StandardUtils.IsChildOf(controllersFolder.Folder, installationFolder.Folder))
				{
					HandleMessages();

					progressDialog.Close();
					_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);

					var result = MessageBox.Show($"You are attempting to install a controller model into {StandardUtils.GetRelativeFolder(_appObject.Solution, installationFolder)}. Typically, controller models reside in {StandardUtils.GetRelativeFolder(_appObject.Solution, controllersFolder)}.\r\n\r\nDo you wish to place the new controller model in this non-standard location?",
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

					controllersFolder = installationFolder;

					projectMapping.ControllersFolder = controllersFolder.Folder;
					projectMapping.ControllersNamespace = controllersFolder.Namespace;
					projectMapping.ControllersProject = controllersFolder.ProjectName;

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
					InstallType = 3
				};

				HandleMessages();

				progressDialog.Close();
				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				progressDialog = null;

				if (form.ShowDialog() == DialogResult.OK)
				{
					var entityModel = (EntityModel)form._entityModelList.SelectedItem;
					var resourceModel = (ResourceModel)form._resourceModelList.SelectedItem;
					var moniker = StandardUtils.LoadMoniker(_appObject.Solution);
					string policy = string.Empty;

					if ( form.policyCombo.Items.Count > 0 )
						policy = form.policyCombo.SelectedItem.ToString();
					
					var orchestrationNamespace = StandardUtils.FindOrchestrationNamespace(_appObject.Solution);

					var validatorInterface = StandardUtils.FindValidatorInterface(_appObject.Solution, $"{resourceModel.Namespace}.{resourceModel.ClassName}");

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", entityModel.Namespace);
					replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);
					replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
					replacementsDictionary.Add("$validationnamespace$", validationFolder.Namespace);

					var columns = new List<ClassMember>();

					var emitter = new Emitter();
					var model = emitter.EmitController(
						entityModel,
						resourceModel,
						moniker,
						replacementsDictionary["$safeitemname$"],
						validatorInterface,
						policy);

					replacementsDictionary.Add("$model$", model);
					Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				if (progressDialog != null)
					if (progressDialog.IsHandleCreated)
						progressDialog.Close();

				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
