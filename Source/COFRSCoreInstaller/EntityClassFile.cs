namespace COFRSCoreInstaller
{
	public class EntityClassFile
	{
		public string ClassName { get; set; }
		public string FileName { get; set; }
		public string TableName { get; set; }
		public string SchemaName { get; set; }
		public string ClassNameSpace { get; set; }
		public ElementType ElementType { get; set; }

		public override string ToString()
		{
			return ClassName;
		}
	}
}
