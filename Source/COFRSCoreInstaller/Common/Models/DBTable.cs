namespace COFRS.Template.Common.Models
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
