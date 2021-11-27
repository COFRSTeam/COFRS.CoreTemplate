using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class ResourceModel
    {
        public string ProjectName { get; set; }
        public string Namespace { get; set; }
        public string Folder { get; set; }
        public string ClassName { get; set; }
        public ResourceType ResourceType { get; set; }
        public DBColumn[] Columns { get; set; }
        public DBServerType ServerType { get; set; }
        public EntityModel EntityModel { get; set; }

        public CodeFunction2[] Functions { get; set; }

        public override string ToString()
        {
            return ClassName;
        }
    }
}
