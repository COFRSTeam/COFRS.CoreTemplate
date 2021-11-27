using System;

namespace COFRS.Template.Common.ServiceUtilities
{
	public class NameNormalizer
	{
		public string SingleForm { get; set; }
		public string PluralForm { get; set; }

		public string PluralCamelCase { get; set; }
		public string SingleCamelCase { get; set; }

		public NameNormalizer(string name)
		{
			if (name.EndsWith("status", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("campus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("circus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("walrus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("syllabus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("virus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("bonus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("anus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("octopus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("platypus", StringComparison.OrdinalIgnoreCase) ||
				 name.EndsWith("terminus", StringComparison.OrdinalIgnoreCase))
			{
				SingleForm = name;
				PluralForm = name + "es";
			}
			else if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
			{
				PluralForm = name;

				if (name.EndsWith("eys", StringComparison.OrdinalIgnoreCase))
				{
					SingleForm = $"{name.Substring(0, name.Length - 1)}";
				}
				else if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
				{
					SingleForm = $"{name.Substring(0, name.Length - 3)}y";
				}
				else if (name.EndsWith("ses", StringComparison.OrdinalIgnoreCase))
				{
					SingleForm = $"{name.Substring(0, name.Length - 2)}";
				}
				else if (name.EndsWith("oes", StringComparison.OrdinalIgnoreCase))
				{
					SingleForm = $"{name.Substring(0, name.Length - 2)}";
				}
				else if (name.EndsWith("us", StringComparison.OrdinalIgnoreCase))
				{
					SingleForm = $"{name.Substring(0, name.Length - 2)}i";
				}
				else
				{
					SingleForm = $"{name.Substring(0, name.Length - 1)}";
				}
			}
			else
			{
				SingleForm = name;

				if (name.EndsWith("eau", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}x";
				}
				if (name.EndsWith("ion", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 3)}ia";
				}
				if (name.EndsWith("is", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 2)}es";
				}
				if (name.EndsWith("ix", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 2)}ices";
				}
				if (name.EndsWith("oo", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}s";
				}
				if (name.EndsWith("um", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 2)}a";
				}
				if (name.EndsWith("ey", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}s";
				}
				else if (name.EndsWith("o", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}es";
				}
				else if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 1)}ies";
				}
				else if (name.EndsWith("i", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 1)}us";
				}
				else if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}es";
				}
				else if (name.EndsWith("a", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name}ae";
				}
				else if (name.EndsWith("ex", StringComparison.OrdinalIgnoreCase))
				{
					PluralForm = $"{name.Substring(0, name.Length - 2)}ices";
				}
				else
				{
					PluralForm = $"{name}s";
				}
			}

			SingleCamelCase = SingleForm.Substring(0, 1).ToLower() + SingleForm.Substring(1);
			PluralCamelCase = PluralForm.Substring(0, 1).ToLower() + PluralForm.Substring(1);
		}
	}
}
