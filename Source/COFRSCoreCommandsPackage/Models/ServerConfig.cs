using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
	public class ServerConfig
	{
		public int LastServerUsed { get; set; }
		public List<DBServer> Servers { get; set; }

		public ServerConfig()
		{
			LastServerUsed = 0;
			Servers = new List<DBServer>();
		}
	}
}
