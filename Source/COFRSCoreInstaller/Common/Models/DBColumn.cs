namespace COFRS.Template.Common.Models
{
	/// <summary>
	/// DB Column structure
	/// </summary>
	public class DBColumn
	{
		/// <summary>
		/// The name of the column
		/// </summary>
		public string ColumnName { get; set; }

		/// <summary>
		/// The Entity type in the entity class
		/// </summary>
		public string EntityType { get; set; }

		/// <summary>
		/// The data type of the column as defined by the database
		/// </summary>
		public string dbDataType { get; set; }

		/// <summary>
		/// The SQL Data type that best corresponds to the data type
		/// </summary>
		public object DataType { get; set; }

		/// <summary>
		/// The length of the column
		/// </summary>
		public long Length { get; set; }

		/// <summary>
		/// The numeric percision of the column
		/// </summary>
		public int NumericPrecision { get; set; }

		/// <summary>
		/// The numeric scale of the column
		/// </summary>
		public int NumericScale { get; set; }

		/// <summary>
		/// True if this column is part of the primary key; false otherwise
		/// </summary>
		public bool IsPrimaryKey { get; set; }

		/// <summary>
		/// True if this column is an identity column; false otherwise
		/// </summary>
		public bool IsIdentity { get; set; }

		/// <summary>
		/// True if this column can contain null values; false otherwise
		/// </summary>
		public bool IsNullable { get; set; }

		/// <summary>
		/// True if this column is computed by the database; false otherwise
		/// </summary>
		public bool IsComputed { get; set; }

		/// <summary>
		/// True if this column is part of an index; false otherwise
		/// </summary>
		public bool IsIndexed { get; set; }

		/// <summary>
		/// True if this column is part of a foreign key; false otherwise
		/// </summary>
		public bool IsForeignKey { get; set; }

		/// <summary>
		/// The name of the foreign table if this column is part of a foreign key
		/// </summary>
		public string ForeignTableName { get; set; }

		/// <summary>
		/// The entity name of that corresponds to this column
		/// </summary>
		public string EntityName { get; set; }

		/// <summary>
		/// True if this is a fixed length column; false otherwise
		/// </summary>
		public bool IsFixed { get; set; }

		public override string ToString()
		{
			return ColumnName;
		}
	}
}
