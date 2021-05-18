using COFRS.Template.Common.Models;
using System.Collections.Generic;

namespace COFRS.Template.Common.Models
{
	/// <summary>
	/// A representation of the domain class member
	/// </summary>
	public class ClassMember
	{
		/// <summary>
		/// The name of the domain class member
		/// </summary>
		public string ResourceMemberName { get; set; }

		/// <summary>
		/// The datatype of the domain class member
		/// </summary>
		public string ResourceMemberType { get; set; }

		/// <summary>
		/// The entity members associated with this domain class member
		/// </summary>
		public List<DBColumn> EntityNames { get; set; }

		/// <summary>
		/// Child members if this member is an object
		/// </summary>
		public List<ClassMember> ChildMembers { get; set; }

		public override string ToString()
		{
			return ResourceMemberName;
		}
	}
}
