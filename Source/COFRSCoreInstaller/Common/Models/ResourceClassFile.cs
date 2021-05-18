using System.Collections.Generic;

namespace COFRS.Template.Common.Models
{
    public class ResourceClassFile : ClassFile
	{
		public string EntityClass { get; set; }

		public List<ClassMember> Members { get; set; }

		public override string ToString()
		{
			return ClassName;
		}
	}
}
