using System.Collections.Generic;

namespace COFRS.Template.Common.Models
{
    public class EntityClassFile : ClassFile
    {
		public string TableName { get; set; }
		public string SchemaName { get; set; }

		public DBServerType ServerType { get; set; }
		public List<DBColumn> Columns { get; set; }

		public override string ToString()
		{
			return ClassName;
		}
    }
}
