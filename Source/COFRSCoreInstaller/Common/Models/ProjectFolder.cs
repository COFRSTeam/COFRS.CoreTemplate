using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
    public class ProjectFolder
    {
        public string ProjectName { get; set; }
        public string Namespace { get; set; }
        public string Folder { get; set; }

        public override string ToString()
        {
            return Folder;
        }
    }
}
