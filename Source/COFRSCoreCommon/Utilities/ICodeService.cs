using COFRSCoreCommon.Models;
using EnvDTE;
using System.Collections.Generic;

namespace COFRSCoreCommon.Utilities
{
    public interface ICodeService
    {
        ProjectMapping LoadProjectMapping();
        void SaveProjectMapping();
        EntityClass GetEntityClassBySchema(string schema, string tableName);
        ResourceClass GetResourceClassBySchema(string schema, string tableName);
        EntityClass GetEntityClass(string name);
        ResourceClass GetResourceClass(string name);
        void AddEntity(ProjectItem projectItem);
        void AddResource(ProjectItem projectItem);
        void LoadEntityClassList(string folder = "");
        void LoadResourceClassList(string folder = "");
        List<EntityClass> EntityClassList { get; }
        List<ResourceClass> ResourceClassList { get; }
    }
}
