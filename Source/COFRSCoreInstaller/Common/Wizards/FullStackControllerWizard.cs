using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Windows;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace COFRS.Template.Common.Wizards
{
    public class FullStackControllerWizard : IWizard
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
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			IVsThreadedWaitDialog2 waitDialog = null;
			int userCanceled = 0;

			try
			{
				//  Load the project mapping information
				var installationFolder = codeService.InstallationFolder;
				var connectionString = codeService.ConnectionString;
				var projectMapping = codeService.LoadProjectMapping();
				var defaultServerType = codeService.DefaultServerType;
				var policies = codeService.Policies;

				//	Get folders and namespaces
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				var candidateName = replacementsDictionary["$safeitemname$"];

				if (candidateName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 3);

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);

				var form = new FullStackDialog
				{
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					ReplacementsDictionary = replacementsDictionary,
					EntityModelsFolder = projectMapping.GetEntityModelsFolder(),
					Policies = policies,
					DefaultConnectionString = connectionString,
				};

				EntityClass entityModel = null;
				var result = form.ShowDialog();

				if (result.HasValue && result.Value == true)
				{
					if (ServiceProvider.GlobalProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) is IVsThreadedWaitDialogFactory dialogFactory)
					{
						dialogFactory.CreateInstance(out waitDialog);
					}

					if (waitDialog != null && waitDialog.StartWaitDialog("Microsoft Visual Studio",
																 "Building full stack",
																 $"Building full stack",
																 null,
																 $"Building full stack",
																 0,
																 false, true) == VSConstants.S_OK)
					{
						Proceed = true;
						codeService.ConnectionString = $"{form.ConnectionString}Application Name={projectMapping.ControllersProject}";

						var entityClassName = $"E{form.SingularResourceName}";
						var resourceClassName = form.SingularResourceName;
						var mappingClassName = $"{form.SingularResourceName}Profile";
						var exampleClassName = $"{form.SingularResourceName}Example";
						var controllerClassName = $"{form.PluralResourceName}Controller";

						var moniker = codeService.Moniker;
						var policy = form.Policy;

						replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
						replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
						replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

						var standardEmitter = new Emitter();
						bool pfCanceled;
						 
						#region Entity Model Operations
						//	Should we generate an entity model?
						if (form.GenerateEntityModel)
						{
							if (form.ServerType == DBServerType.POSTGRESQL && form.UndefinedEntityModels != null && form.UndefinedEntityModels.Count > 0)
							{
								//	Generate any undefined composits before we construct our entity model (because, 
								//	the entity model depends upon them)
								waitDialog.UpdateProgress("Building full stack", 
									                      $"Building entity composites",
														  $"Building entity composites",
														  0, 0, false, out pfCanceled);


								standardEmitter.GenerateComposites(form.UndefinedEntityModels,
																   form.ConnectionString,
																   replacementsDictionary,
																   projectMapping.GetEntityModelsFolder());
							}

							//	Emit Entity Model
							waitDialog.UpdateProgress("Building full stack",
													  $"Building {entityClassName}",
													  $"Building {entityClassName}",
													  0, 0, false, out pfCanceled);

							var columns = DBHelper.GenerateColumns(form.DatabaseTable.Schema,
																   form.DatabaseTable.Table,
																   form.ServerType,
																   form.ConnectionString);

							var emodel = standardEmitter.EmitEntityModel(entityClassName,
																		 form.DatabaseTable.Schema,
																		 form.DatabaseTable.Table,
																		 form.ServerType,
																		 columns,
																		 replacementsDictionary);

							var entityFilePath = Path.Combine(projectMapping.GetEntityModelsFolder().Folder, $"{entityClassName}.cs");

							var theFile = new StringBuilder();

							theFile.AppendLine("using System;");

							if (replacementsDictionary.ContainsKey("$barray$"))
								if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
									theFile.AppendLine("using System.Collections;");

							theFile.AppendLine("using System.Collections.Generic;");

							if (replacementsDictionary.ContainsKey("$image$"))
								if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
									theFile.AppendLine("using System.Drawing;");

							if (replacementsDictionary.ContainsKey("$net$"))
								if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
									theFile.AppendLine("using System.Net;");

							if (replacementsDictionary.ContainsKey("$netinfo$"))
								if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
									theFile.AppendLine("using System.Net.NetworkInformation;");

							if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
								if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
									theFile.AppendLine("using NpgsqlTypes;");

							theFile.AppendLine("using COFRS;");
							theFile.AppendLine();
							theFile.AppendLine($"namespace {projectMapping.EntityNamespace}");
							theFile.AppendLine("{");

							theFile.Append(emodel);
							theFile.AppendLine("}");

							File.WriteAllText(entityFilePath, theFile.ToString());

							var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(entityFilePath));
							ProjectItem entityItem;

							if (parentProject.GetType() == typeof(Project))
								entityItem = ((Project)parentProject).ProjectItems.AddFromFile(entityFilePath);
							else
								entityItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(entityFilePath);

							codeService.AddEntity(entityItem);

							ProjectItemFinishedGenerating(entityItem);
							BeforeOpeningFile(entityItem);

							var window = entityItem.Open();
							window.Activate();
						}
						#endregion

						#region Resource Model Operations
						if (form.GenerateResourceModel)
						{
							entityModel = codeService.GetEntityClass(entityClassName);

							if (entityModel != null)
							{
								waitDialog.UpdateProgress("Building full stack",
														  $"Building {resourceClassName}",
														  $"Building {resourceClassName}",
														  0, 0, false, out pfCanceled);

								var rmodel = standardEmitter.EmitResourceModel(resourceClassName,
																			   entityModel,
																			   replacementsDictionary);

								var resourceFilePath = Path.Combine(projectMapping.GetResourceModelsFolder().Folder, $"{resourceClassName}.cs");

								var theFile = new StringBuilder();

								theFile.AppendLine("using System;");

								if (replacementsDictionary.ContainsKey("$barray$"))
									if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
										theFile.AppendLine("using System.Collections;");

								theFile.AppendLine("using System.Collections.Generic;");
								theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

								if (replacementsDictionary.ContainsKey("$image$"))
									if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
										theFile.AppendLine("using System.Drawing;");

								if (replacementsDictionary.ContainsKey("$net$"))
									if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
										theFile.AppendLine("using System.Net;");

								if (replacementsDictionary.ContainsKey("$netinfo$"))
									if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
										theFile.AppendLine("using System.Net.NetworkInformation;");

								theFile.AppendLine($"using {projectMapping.EntityNamespace};");

								if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
									if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
										theFile.AppendLine("using NpgsqlTypes;");

								theFile.AppendLine("using COFRS;");
								theFile.AppendLine();
								theFile.AppendLine($"namespace {projectMapping.ResourceNamespace}");
								theFile.AppendLine("{");

								theFile.Append(rmodel);
								theFile.AppendLine("}");

								File.WriteAllText(resourceFilePath, theFile.ToString());

								var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(resourceFilePath));
								ProjectItem resourceItem;

								if (parentProject.GetType() == typeof(Project))
									resourceItem = ((Project)parentProject).ProjectItems.AddFromFile(resourceFilePath);
								else
									resourceItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(resourceFilePath);

								codeService.AddResource(resourceItem);

								ProjectItemFinishedGenerating(resourceItem);
								BeforeOpeningFile(resourceItem);

								var window = resourceItem.Open();
								window.Activate();
							}
							else
                            {
								waitDialog.EndWaitDialog(out userCanceled);

								VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																$"Cannot find corresponding entity model for {resourceClassName}. Aborting code generation.",
																"Microsoft Visual Studio",
																OLEMSGICON.OLEMSGICON_CRITICAL,
																OLEMSGBUTTON.OLEMSGBUTTON_OK,
																OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
								Proceed = false;
							}
						}
						#endregion

						if (Proceed)
						{
							#region Mapping Operations
							if (form.GenerateMappingModel)
							{
								var resourceModel = codeService.GetResourceClass(resourceClassName);

								if (resourceModel != null)
								{
									if (resourceModel.Entity != null)
									{
										waitDialog.UpdateProgress("Building full stack",
																  $"Building {mappingClassName}",
																  $"Building {mappingClassName}",
																  0, 0, false, out pfCanceled);

										var mappingModel = standardEmitter.EmitMappingModel(resourceModel, mappingClassName, replacementsDictionary);
										var projectItemPath = Path.Combine(projectMapping.MappingFolder, $"{mappingClassName}.cs");
										var theFile = new StringBuilder();

										theFile.AppendLine("using System;");
										theFile.AppendLine("using System.Linq;");
										theFile.AppendLine("using Microsoft.Extensions.Configuration;");

										if (replacementsDictionary.ContainsKey("$barray$"))
											if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using System.Collections;");

										theFile.AppendLine("using System.Collections.Generic;");

										if (replacementsDictionary.ContainsKey("$image$"))
											if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using System.Drawing;");

										if (replacementsDictionary.ContainsKey("$net$"))
											if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using System.Net;");

										if (replacementsDictionary.ContainsKey("$netinfo$"))
											if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using System.Net.NetworkInformation;");

										if (replacementsDictionary.ContainsKey("$annotations$"))
											if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

										if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
											if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
												theFile.AppendLine("using NpgsqlTypes;");

										theFile.AppendLine($"using {projectMapping.EntityNamespace};");
										theFile.AppendLine($"using {projectMapping.ResourceNamespace};");

										theFile.AppendLine("using AutoMapper;");
										theFile.AppendLine("using COFRS;");
										theFile.AppendLine();
										theFile.AppendLine($"namespace {projectMapping.MappingNamespace}");
										theFile.AppendLine("{");

										theFile.Append(mappingModel);
										theFile.AppendLine("}");

										File.WriteAllText(projectItemPath, theFile.ToString());

										var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(projectItemPath));
										ProjectItem mappingItem;

										if (parentProject.GetType() == typeof(Project))
											mappingItem = ((Project)parentProject).ProjectItems.AddFromFile(projectItemPath);
										else
											mappingItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(projectItemPath);

										var window = mappingItem.Open();
										window.Activate();
									}
									else
									{
										waitDialog.EndWaitDialog(out userCanceled);
										VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																		$"Cannot find entity model for {resourceClassName}. Aborting code generation.",
																		"Microsoft Visual Studio",
																		OLEMSGICON.OLEMSGICON_CRITICAL,
																		OLEMSGBUTTON.OLEMSGBUTTON_OK,
																		OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
										Proceed = false;
									}
								}
								else
                                {
									waitDialog.EndWaitDialog(out userCanceled);
									VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																	$"Cannot find resource model for {resourceClassName}. Aborting code generation.",
																	"Microsoft Visual Studio",
																	OLEMSGICON.OLEMSGICON_CRITICAL,
																	OLEMSGBUTTON.OLEMSGBUTTON_OK,
																	OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
									Proceed = false;
								}
							}
							#endregion

							if (Proceed)
							{
								#region Example Operations
								if (form.GenerateExampleData)
								{
									var resourceModel = codeService.GetResourceClass(resourceClassName);

									if (resourceModel != null)
									{
										var profileMap = codeService.OpenProfileMap(resourceModel, out bool IsAllDefined);

										if (profileMap != null)
										{
											waitDialog.UpdateProgress("Building full stack",
																	  $"Building {exampleClassName}",
																	  $"Building {exampleClassName}",
																	  0, 0, false, out pfCanceled);

											var exampleModel = standardEmitter.EmitExampleModel(resourceModel, profileMap, exampleClassName, defaultServerType, connectionString);

											var projectItemPath = Path.Combine(projectMapping.ExampleFolder, $"{exampleClassName}.cs");
											var theFile = new StringBuilder();

											theFile.AppendLine("using System;");
											theFile.AppendLine("using System.Collections;");
											theFile.AppendLine("using System.Collections.Generic;");
											theFile.AppendLine("using System.Linq;");
											theFile.AppendLine("using System.Security.Claims;");
											theFile.AppendLine("using System.Threading.Tasks;");

											if (replacementsDictionary.ContainsKey("$barray$"))
												if (replacementsDictionary["$barray$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using System.Collections;");

											if (replacementsDictionary.ContainsKey("$image$"))
												if (replacementsDictionary["$image$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using System.Drawing;");

											if (replacementsDictionary.ContainsKey("$net$"))
												if (replacementsDictionary["$net$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using System.Net;");

											if (replacementsDictionary.ContainsKey("$netinfo$"))
												if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using System.Net.NetworkInformation;");

											if (replacementsDictionary.ContainsKey("$annotations$"))
												if (replacementsDictionary["$netinfo$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using System.ComponentModel.DataAnnotations;");

											if (replacementsDictionary.ContainsKey("$npgsqltypes$"))
												if (replacementsDictionary["$npgsqltypes$"].Equals("true", StringComparison.OrdinalIgnoreCase))
													theFile.AppendLine("using NpgsqlTypes;");

											theFile.AppendLine("using Swashbuckle.AspNetCore.Filters;");
											theFile.AppendLine($"using {projectMapping.ResourceNamespace};");
											theFile.AppendLine("using COFRS;");
											theFile.AppendLine();
											theFile.AppendLine($"namespace {projectMapping.ExampleNamespace}");
											theFile.AppendLine("{");

											theFile.Append(exampleModel);
											theFile.AppendLine("}");

											File.WriteAllText(projectItemPath, theFile.ToString());

											var parentProject = codeService.GetProjectFromFolder(Path.GetDirectoryName(projectItemPath));
											ProjectItem exampleItem;

											if (parentProject.GetType() == typeof(Project))
												exampleItem = ((Project)parentProject).ProjectItems.AddFromFile(projectItemPath);
											else
												exampleItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(projectItemPath);

											var window = exampleItem.Open();
											window.Activate();
										}
										else
                                        {
											waitDialog.EndWaitDialog(out userCanceled);
											VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																			$"Cannot find profile map for {resourceClassName}. Aborting code generation.",
																			"Microsoft Visual Studio",
																			OLEMSGICON.OLEMSGICON_CRITICAL,
																			OLEMSGBUTTON.OLEMSGBUTTON_OK,
																			OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
											Proceed = false;
										}
									}
									else
                                    {
										waitDialog.EndWaitDialog(out userCanceled);
										VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																		$"Cannot find resource model for {resourceClassName}. Aborting code generation.",
																		"Microsoft Visual Studio",
																		OLEMSGICON.OLEMSGICON_CRITICAL,
																		OLEMSGBUTTON.OLEMSGBUTTON_OK,
																		OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
										Proceed = false;
									}
								}
								#endregion

								if (Proceed)
								{
									#region Controller Operations
									if (form.GenerateController)
									{
										var resourceModel = codeService.GetResourceClass(resourceClassName);

										if (resourceModel != null)
										{
											waitDialog.UpdateProgress("Building full stack",
																	  $"Building {controllerClassName}",
																	  $"Building {controllerClassName}",
																	  0, 0, false, out pfCanceled);

											var orchestrationNamespace = codeService.FindOrchestrationNamespace();

											var controllerModel = standardEmitter.EmitController(resourceModel,
																						 moniker,
																						 controllerClassName,
																						 policy);

											replacementsDictionary.Add("$entitynamespace$", resourceModel.Entity.Namespace);
											replacementsDictionary.Add("$resourcenamespace$", resourceModel.Namespace);
											replacementsDictionary.Add("$orchestrationnamespace$", orchestrationNamespace);
											replacementsDictionary.Add("$examplesnamespace$", projectMapping.ExampleNamespace);
											replacementsDictionary.Add("$model$", controllerModel);
										}
										else
										{
											waitDialog.EndWaitDialog(out userCanceled);
											VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
																			$"Cannot find resource model for {resourceClassName}. Aborting code generation.",
																			"Microsoft Visual Studio",
																			OLEMSGICON.OLEMSGICON_CRITICAL,
																			OLEMSGBUTTON.OLEMSGBUTTON_OK,
																			OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
											Proceed = false;
										}
									}
									else
										Proceed = false;
									#endregion
								}
							}
						}

						waitDialog.EndWaitDialog(out userCanceled);
					}
				}
			}
			catch (Exception error)
			{
				if ( waitDialog !=null )
                {
					waitDialog.EndWaitDialog(out userCanceled);
				}

				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
												error.Message,
												"Microsoft Visual Studio",
												OLEMSGICON.OLEMSGICON_CRITICAL,
												OLEMSGBUTTON.OLEMSGBUTTON_OK,
												OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				Proceed = false;
			}
		}

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
