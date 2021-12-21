using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class ResourceClass
    {
        public string ClassName
        {
            get { return Resource.Name; }
        }

        public CodeElement2 Resource { get; set; }
        public EntityClass Entity { get; set; }

        public ProjectItem ProjectItem
        {
            get
            {
                return Resource.ProjectItem;
            }
        }

        public DBColumn[] Columns
        {
            get
            {
                if (Resource.Kind == vsCMElement.vsCMElementClass)
                    return COFRSCommonUtilities.LoadResourceColumns(this);
                else
                    return COFRSCommonUtilities.LoadResourceEnumColumns(this);
            }
        }

        public string Namespace
        {
            get
            {
                if (Resource.Kind == vsCMElement.vsCMElementClass)
                    return ((CodeClass2)Resource).Namespace.Name;
                else
                    return ((CodeEnum)Resource).Namespace.Name;
            }
        }

        public ResourceType ResourceType
        {
            get
            {
                if (Resource.Kind == vsCMElement.vsCMElementClass)
                    return ResourceType.Class;
                else
                    return ResourceType.Enum;
            }
        }


        public override string ToString()
        {
            return Resource.Name;
        }
    }
}
