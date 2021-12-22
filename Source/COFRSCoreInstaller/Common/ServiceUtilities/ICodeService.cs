using COFRS.Template.Common.Models;
using EnvDTE;
using EnvDTE80;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.ServiceUtilities
{
    internal interface ICodeService
    {
        #region Properties
        string ConnectionString { get; set; }
        string Moniker { get; }
        DBServerType DefaultServerType { get; }
        List<string> Policies { get; }
        ProjectFolder InstallationFolder { get; }
        List<EntityClass> EntityClassList { get; }
        List<ResourceClass> ResourceClassList { get; }
        #endregion

        ProjectMapping LoadProjectMapping();

        void SaveProjectMapping();

        void OnProjectItemRemoved(ProjectItem ProjectItem);

        void OnSolutionOpened();

        EntityClass GetEntityClassBySchema(string schema, string tableName);

        ResourceClass GetResourceClassBySchema(string schema, string tableName);

        EntityClass GetEntityClass(string name);

        ResourceClass GetResourceClass(string name);

        void AddEntity(ProjectItem projectItem);

        void AddResource(ProjectItem projectItem);

        void LoadEntityClassList(string folder = "");

        void LoadResourceClassList(string folder = "");


        string NormalizeClassName(string className);

        string CorrectForReservedNames(string columnName);
        DBColumn[] LoadEntityColumns(CodeClass2 codeClass);
        DBColumn[] LoadResourceColumns(ResourceClass resource);
        DBColumn[] LoadResourceEnumColumns(ResourceClass resource);
        DBColumn[] LoadEntityEnumColumns(CodeEnum enumElement);

        bool IsChildOf(string parentPath, string candidateChildPath);
        bool IsRootNamespace(string candidateNamespace);

        string GetRelativeFolder(ProjectFolder folder);
        string FindOrchestrationNamespace();
        ProfileMap GenerateProfileMap(ResourceClass resourceModel);


        Project GetProject(string projectName);
        object GetProjectFromFolder(string folder);
        string FindValidatorInterface(string resourceClassName, string folder = "");

        void RegisterValidationModel(string validationClass, string validationNamespace);
        void RegisterComposite(string className, string entityNamespace, ElementType elementType, string tableName);

        ProfileMap OpenProfileMap(ResourceClass resourceModel, out bool isAllDefined);

        CodeClass2 FindCollectionExampleCode(ResourceClass parentModel, string folder = "");
        ResourceMap LoadResourceMap(string folder = "");
        string GetExampleModel(int skipRecords, ResourceClass resourceModel, DBServerType serverType, string connectionString);
        string ResolveMapFunction(JObject entityJson, string columnName, ResourceClass model, string mapFunction);
        CodeClass2 FindExampleCode(ResourceClass parentModel, string folder = "");


    }
}
