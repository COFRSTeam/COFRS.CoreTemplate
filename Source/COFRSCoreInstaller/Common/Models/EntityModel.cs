using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
    public class EntityModel
    {
        public string ProjectName { get; set; }
        public string Namespace { get; set; }
        public string Folder { get; set; }
        public string ClassName { get; set; }
        public ElementType ElementType { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public DBColumn[] Columns { get; set; }
        public DBServerType ServerType { get; set; }

        public override string ToString()
        {
            return ClassName;
        }
    }
}
