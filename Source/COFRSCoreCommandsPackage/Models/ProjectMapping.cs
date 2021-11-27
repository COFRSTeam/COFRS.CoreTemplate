using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class ProjectMapping
    {
        public string EntityProject { get; set; }
        public string EntityNamespace { get; set; }
        public string EntityFolder { get; set; }
        public string ResourceProject { get; set; }
        public string ResourceNamespace { get; set; }
        public string ResourceFolder { get; set; }
        public string MappingProject { get; set; }
        public string MappingNamespace { get; set; }
        public string MappingFolder { get; set; }
        public string ValidationProject { get; set; }
        public string ValidationNamespace { get; set; }
        public string ValidationFolder { get; set; }
        public string ExampleProject { get; set; }
        public string ExampleNamespace { get; set; }
        public string ExampleFolder { get; set; }
        public string ControllersProject { get; set; }
        public string ControllersNamespace { get; set; }
        public string ControllersFolder { get; set; }
        public bool IncludeSDK { get; set; }

        public ProjectFolder GetEntityModelsFolder()
        {
            var pf = new ProjectFolder
            {
                Folder = EntityFolder,
                Namespace = EntityNamespace,
                ProjectName = EntityProject
            };

            return pf;
        }

        public ProjectFolder GetResourceModelsFolder()
        {
            var pf = new ProjectFolder
            {
                Folder = ResourceFolder,
                Namespace = ResourceNamespace,
                ProjectName = ResourceProject
            };

            return pf;
        }

        public ProjectFolder GetValidatorFolder()
        {
            var pf = new ProjectFolder
            {
                Folder = ValidationFolder,
                Namespace = ValidationNamespace,
                ProjectName = ValidationProject
            };

            return pf;
        }

        public ProjectFolder GetExamplesFolder()
        {
            var pf = new ProjectFolder
            {
                Folder = ExampleFolder,
                Namespace = ExampleNamespace,
                ProjectName = ExampleProject
            };

            return pf;
        }
    }
}
