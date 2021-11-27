using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
	public class DBServer
	{
		public DBServerType DBType { get; set; }
		public DBAuthentication DBAuth { get; set; }
		public string ServerName { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public bool RememberPassword { get; set; }
		public int PortNumber { get; set; }

		public override string ToString()
		{
			return ServerName;
		}
	}
}
