using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
	public class DBTable
	{
		public string Schema { get; set; }
		public string Table { get; set; }

		public override string ToString()
		{
			if (string.IsNullOrWhiteSpace(Schema))
				return $"{Table}";

			return $"{Schema}.{Table}";
		}
	}
}
