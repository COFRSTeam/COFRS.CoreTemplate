using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreInstaller
{
	/// <summary>
	/// Entity Member
	/// </summary>
	public class EntityMember
	{
		/// <summary>
		/// Member name
		/// </summary>
		public string MemberName { get; set; }

		/// <summary>
		/// Length
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// DataType
		/// </summary>
		public Type DataType { get; set; }

		/// <summary>
		/// IsPrimaryKey
		/// </summary>
		public bool IsPrimaryKey { get; set; }
	}
}
