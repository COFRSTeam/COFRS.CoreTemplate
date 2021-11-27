using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class ResourceProfile
    {
        public string ResourceColumnName { get; set; }
        public string MapFunction { get; set; }
        public string[] EntityColumnNames { get; set; }

        public List<ResourceProfile> ChildProfiles { get; set; }
    }
}
