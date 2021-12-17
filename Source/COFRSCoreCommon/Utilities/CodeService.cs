using COFRSCoreCommon.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Constants = EnvDTE.Constants;

namespace COFRSCoreCommon.Utilities
{
    public class CodeService : ICodeService
    {
        private ProjectMapping projectMapping = null;
        private readonly List<EntityClass> entityClassList = new List<EntityClass>();
        private readonly List<ResourceClass> resourceClassList = new List<ResourceClass>();
		private readonly Events2 _events2 = null;
		private readonly SolutionEvents _solutionEvents = null;
        private readonly ProjectItemsEvents _projectItemsEvents = null;

        public CodeService()
        {
			var mDte = (DTE2) Package.GetGlobalService(typeof(SDTE));

			_events2 = (Events2)mDte.Events;
			_solutionEvents = _events2.SolutionEvents;
            _projectItemsEvents = _events2.ProjectItemsEvents;

            _solutionEvents.Opened += SolutionEvents_Opened;
            _projectItemsEvents.ItemRemoved += _projectItemsEvents_ItemRemoved;
        }

        public List<EntityClass> EntityClassList
        {
            get { return entityClassList; }
        }

        public List<ResourceClass> ResourceClassList
        {
            get { return resourceClassList; }
        }


        private void _projectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
        {
            var entity = entityClassList.FirstOrDefault(c => c.ProjectItem.Name.Equals(ProjectItem.Name));

            if (entity != null)
                entityClassList.Remove(entity);

            var resource = resourceClassList.FirstOrDefault(c => c.ProjectItem.Name.Equals(ProjectItem.Name));

            if (resource != null)
                resourceClassList.Remove(resource);
        }

        private void SolutionEvents_Opened()
        {
            entityClassList.Clear();
            projectMapping = null;

            projectMapping = COFRSCommonUtilities.OpenProjectMapping();
            LoadEntityClassList();
            LoadResourceClassList();
        }

        public ProjectMapping LoadProjectMapping()
        {
            if (projectMapping == null)
            {
                projectMapping = COFRSCommonUtilities.OpenProjectMapping();
            }

            return projectMapping;
        }

        public void SaveProjectMapping()
        {
            COFRSCommonUtilities.SaveProjectMapping(projectMapping);
        }

        public EntityClass GetEntityClassBySchema(string schema, string tableName)
        {
            if (entityClassList.Count == 0)
                LoadEntityClassList();

            return entityClassList.FirstOrDefault(c => c.SchemaName.Equals(schema, StringComparison.OrdinalIgnoreCase) &&
                                                       c.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }
        public ResourceClass GetResourceClassBySchema(string schema, string tableName)
        {
            if (resourceClassList.Count == 0)
                LoadResourceClassList();

            return resourceClassList.FirstOrDefault(c => c.Entity.SchemaName.Equals(schema, StringComparison.OrdinalIgnoreCase) &&
                                                         c.Entity.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityClass GetEntityClass(string name)
        {
            if (entityClassList.Count == 0)
                LoadEntityClassList();

            return entityClassList.FirstOrDefault(c => c.ClassName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ResourceClass GetResourceClass(string name)
        {
            if (resourceClassList.Count == 0)
                LoadResourceClassList();

            return resourceClassList.FirstOrDefault(c => c.ClassName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void AddEntity(ProjectItem projectItem)
        {
            if (entityClassList.Count == 0)
            {
                LoadEntityClassList();
            }
            else
            {
                FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

                foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
                {
                    foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
                    {
                        CodeAttribute2 tableAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                        if (tableAttribute != null)
                        {
                            var code = new EntityClass
                            {
                                Entity = (CodeElement2)classElement,
                            };

                            entityClassList.Add(code);
                        }
                        else
                        {
                            CodeAttribute2 compositeAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                            if (compositeAttribute != null)
                            {
                                var code = new EntityClass
                                {
                                    Entity = (CodeElement2)classElement,
                                };

                                entityClassList.Add(code);
                            }
                        }
                    }

                    foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                    {
                        CodeAttribute2 enumAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                        if (enumAttribute != null)
                        {
                            var code = new EntityClass
                            {
                                Entity = (CodeElement2)enumElement,
                            };

                            entityClassList.Add(code);
                        }
                    }
                }
            }
        }
        public void AddResource(ProjectItem projectItem)
        {
            if (resourceClassList.Count == 0)
            {
                LoadResourceClassList();
            }
            else
            {
                FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

                foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
                {
                    foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
                    {
                        CodeAttribute2 entityAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));

                        var code = new ResourceClass
                        {
                            Resource = (CodeElement2)classElement
                        };

                        if (entityAttribute != null)
                        {
                            var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                            var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                            if (match.Success)
                            {
                                code.Entity = GetEntityClass(match.Groups["entityClass"].Value);
                            }
                        }

                        resourceClassList.Add(code);
                    }

                    foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                    {
                        CodeAttribute2 entityAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));

                        var code = new ResourceClass
                        {
                            Resource = (CodeElement2)enumElement
                        };

                        if (entityAttribute != null)
                        {
                            var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                            var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                            if (match.Success)
                            {
                                code.Entity = GetEntityClass(match.Groups["entityClass"].Value);
                            }
                        }

                        resourceClassList.Add(code);
                    }
                }
            }
        }

        public void LoadEntityClassList(string folder = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projectMapping = LoadProjectMapping();  //	Contains the names and projects where various source file exist.
            var mDte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var entityFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(projectMapping.GetEntityModelsFolder().Folder) :
                                                                   mDte.Solution.FindProjectItem(folder);

            foreach (ProjectItem projectItem in entityFolder.ProjectItems)
            {
                if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
                     projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
                {
                    LoadEntityClassList(projectItem.Name);
                }
                else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
                         projectItem.FileCodeModel != null &&
                         projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
                         Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
                {
                    FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

                    foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
                    {
                        foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
                        {
                            CodeAttribute2 tableAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                            if (tableAttribute != null)
                            {
                                var code = new EntityClass
                                {
                                    Entity = (CodeElement2)classElement,
                                };

                                entityClassList.Add(code);
                            }
                            else
                            {
                                CodeAttribute2 compositeAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                                if (compositeAttribute != null)
                                {
                                    var code = new EntityClass
                                    {
                                        Entity = (CodeElement2)classElement,
                                    };

                                    entityClassList.Add(code);
                                }
                            }
                        }

                        foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                        {
                            CodeAttribute2 tableAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                            if (tableAttribute != null)
                            {
                                var code = new EntityClass
                                {
                                    Entity = (CodeElement2)enumElement,
                                };

                                entityClassList.Add(code);
                            }
                        }
                    }
                }
            }
        }
        public void LoadResourceClassList(string folder = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projectMapping = LoadProjectMapping();  //	Contains the names and projects where various source file exist.
            var mDte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var entityFolder = string.IsNullOrWhiteSpace(folder) ? mDte.Solution.FindProjectItem(projectMapping.GetResourceModelsFolder().Folder) :
                                                                   mDte.Solution.FindProjectItem(folder);

            foreach (ProjectItem projectItem in entityFolder.ProjectItems)
            {
                if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFolder ||
                     projectItem.Kind == Constants.vsProjectItemKindVirtualFolder)
                {
                    LoadEntityClassList(projectItem.Name);
                }
                else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
                         projectItem.FileCodeModel != null &&
                         projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
                         Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1)
                {
                    FileCodeModel2 model = (FileCodeModel2)projectItem.FileCodeModel;

                    foreach (CodeNamespace namespaceElement in model.CodeElements.OfType<CodeNamespace>())
                    {
                        foreach (CodeClass2 classElement in namespaceElement.Members.OfType<CodeClass2>())
                        {
                            CodeAttribute2 entityAttribute = classElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Entity"));

                            var code = new ResourceClass
                            {
                                Resource = (CodeElement2)classElement
                            };

                            if (entityAttribute != null)
                            {
                                var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                                var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                                if ( match.Success)
                                {
                                    code.Entity = GetEntityClass(match.Groups["entityClass"].Value);
                                }
                            }

                            resourceClassList.Add(code);
                        }

                        foreach (CodeEnum enumElement in namespaceElement.Members.OfType<CodeEnum>())
                        {
                            CodeAttribute2 entityAttribute = enumElement.Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                            var code = new ResourceClass
                            {
                                Resource = (CodeElement2)enumElement
                            };

                            if (entityAttribute != null)
                            {
                                var entityTypeArgument = entityAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""));

                                var match = Regex.Match(entityTypeArgument.Value, "^typeof\\((?<entityClass>[a-zA-Z0-9_]+)\\)");

                                if (match.Success)
                                {
                                    code.Entity = GetEntityClass(match.Groups["entityClass"].Value);
                                }
                            }

                            resourceClassList.Add(code);
                        }
                    }
                }
            }
        }
    }
}