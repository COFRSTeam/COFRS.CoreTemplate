using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Data;

namespace COFRSCoreInstaller
{
	public static class DBHelper
	{
		/// <summary>
		/// Convers a Postgresql data type into its corresponding standard SQL data type
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public static NpgsqlDbType ConvertPostgresqlDataType(string dataType)
		{
			if (string.Equals(dataType, "bpchar", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Char;
			else if (string.Equals(dataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Char;
			else if (string.Equals(dataType, "_char", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Char;
			else if (string.Equals(dataType, "char", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Char;
			else if (string.Equals(dataType, "int2", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Smallint;
			else if (string.Equals(dataType, "_int2", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Smallint;
			else if (string.Equals(dataType, "int4", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Integer;
			else if (string.Equals(dataType, "_int4", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Integer;
			else if (string.Equals(dataType, "int8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Bigint;
			else if (string.Equals(dataType, "_int8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Bigint;
			else if (string.Equals(dataType, "varchar", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Varchar;
			else if (string.Equals(dataType, "_varchar", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Varchar;
			else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Text;
			else if (string.Equals(dataType, "_text", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Text;
			else if (string.Equals(dataType, "name", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Name;
			else if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Bit;
			else if (string.Equals(dataType, "_bit", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Bit;
			else if (string.Equals(dataType, "varbit", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Varbit;
			else if (string.Equals(dataType, "_varbit", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Varbit;
			else if (string.Equals(dataType, "bytea", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Bytea;
			else if (string.Equals(dataType, "_bytea", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Bytea;
			else if (string.Equals(dataType, "bool", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Boolean;
			else if (string.Equals(dataType, "_bool", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Boolean;
			else if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Date;
			else if (string.Equals(dataType, "_date", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Date;
			else if (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Timestamp;
			else if (string.Equals(dataType, "_timestamp", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Timestamp;
			else if (string.Equals(dataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.TimestampTz;
			else if (string.Equals(dataType, "_timestamptz", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.TimestampTz;
			else if (string.Equals(dataType, "timetz", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.TimeTz;
			else if (string.Equals(dataType, "_timetz", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.TimeTz;
			else if (string.Equals(dataType, "time", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Time;
			else if (string.Equals(dataType, "_time", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Time;
			else if (string.Equals(dataType, "interval", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Interval;
			else if (string.Equals(dataType, "_interval", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Interval;
			else if (string.Equals(dataType, "float8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Double;
			else if (string.Equals(dataType, "_float8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Double;
			else if (string.Equals(dataType, "float4", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Real;
			else if (string.Equals(dataType, "_float4", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Real;
			else if (string.Equals(dataType, "money", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Money;
			else if (string.Equals(dataType, "_money", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Money;
			else if (string.Equals(dataType, "numeric", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Numeric;
			else if (string.Equals(dataType, "_numeric", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Numeric;
			else if (string.Equals(dataType, "uuid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Uuid;
			else if (string.Equals(dataType, "_uuid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Uuid;
			else if (string.Equals(dataType, "json", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Json;
			else if (string.Equals(dataType, "_json", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Json;

			throw new Exception($"Unrecognized data type: {dataType}");
		}
		public static MySqlDbType ConvertMySqlDataType(string dataType)
		{
			if (string.Equals(dataType, "tinyint", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Byte;
			else if (string.Equals(dataType, "tinyint unsigned", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.UByte;
			else if (string.Equals(dataType, "smallint", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Int16;
			else if (string.Equals(dataType, "smallint unsigned", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.UInt16;
			else if (string.Equals(dataType, "mediumint", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Int24;
			else if (string.Equals(dataType, "mediumint unsigned", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.UInt24;
			else if (string.Equals(dataType, "int", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Int32;
			else if (string.Equals(dataType, "int unsigned", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.UInt32;
			else if (string.Equals(dataType, "bigint", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Int64;
			else if (string.Equals(dataType, "bigint unsigned", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.UInt64;
			else if (dataType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Decimal;
			else if (string.Equals(dataType, "double", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Double;
			else if (string.Equals(dataType, "float", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Float;
			else if (dataType.StartsWith("char", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.String;
			else if (dataType.StartsWith("bit", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Bit;
			else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Text;
			else if (string.Equals(dataType, "tinytext", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.TinyText;
			else if (string.Equals(dataType, "mediumtext", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.MediumText;
			else if (string.Equals(dataType, "longtext", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.LongText;
			else if (dataType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.VarChar;
			else if (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Timestamp;
			else if (string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.DateTime;
			else if (string.Equals(dataType, "year", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Year;
			else if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Date;
			else if (string.Equals(dataType, "time", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Time;
			else if (dataType.StartsWith("enum", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Enum;
			else if (dataType.StartsWith("set", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Set;
			else if (dataType.StartsWith("binary", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Binary;
			else if (dataType.StartsWith("varbinary", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.VarBinary;
			else if (dataType.StartsWith("tinyblob", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.TinyBlob;
			else if (dataType.StartsWith("blob", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Blob;
			else if (dataType.StartsWith("mediumblob", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.MediumBlob;
			else if (dataType.StartsWith("longblob", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.LongBlob;

			throw new Exception($"Unrecognized data type: {dataType}");
		}
		public static SqlDbType ConvertSqlServerDataType(string dataType)
		{
			if (string.Equals(dataType, "image", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Image;
			else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Text;
			else if (string.Equals(dataType, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.UniqueIdentifier;
			else if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Date;
			else if (string.Equals(dataType, "time", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Time;
			else if (string.Equals(dataType, "datetime2", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.DateTime2;
			else if (string.Equals(dataType, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.DateTimeOffset;
			else if (string.Equals(dataType, "tinyint", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.TinyInt;
			else if (string.Equals(dataType, "smallint", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.SmallInt;
			else if (string.Equals(dataType, "int", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Int;
			else if (string.Equals(dataType, "smalldatetime", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.SmallDateTime;
			else if (string.Equals(dataType, "real", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Real;
			else if (string.Equals(dataType, "money", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Money;
			else if (string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.DateTime;
			else if (string.Equals(dataType, "float", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Float;
			else if (string.Equals(dataType, "sql_variant", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Variant;
			else if (string.Equals(dataType, "ntext", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.NText;
			else if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Bit;
			else if (string.Equals(dataType, "decimal", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Decimal;
			else if (string.Equals(dataType, "numeric", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Decimal;
			else if (string.Equals(dataType, "smallmoney", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.SmallMoney;
			else if (string.Equals(dataType, "bigint", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.BigInt;
			else if (string.Equals(dataType, "hierarchyid", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.NVarChar;
			else if (string.Equals(dataType, "varbinary", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.VarBinary;
			else if (string.Equals(dataType, "varchar", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.VarChar;
			else if (string.Equals(dataType, "binary", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Binary;
			else if (string.Equals(dataType, "char", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Char;
			else if (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Timestamp;
			else if (string.Equals(dataType, "nvarchar", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.NVarChar;
			else if (string.Equals(dataType, "nchar", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.NChar;
			else if (string.Equals(dataType, "xml", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Xml;
			else if (string.Equals(dataType, "sysname", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.NVarChar;
			else if (string.Equals(dataType, "geography", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Variant;
			else if (string.Equals(dataType, "geometry", StringComparison.OrdinalIgnoreCase))
				return SqlDbType.Variant;

			throw new Exception($"Unrecognized data type: {dataType}");
		}
		public static string GetNonNullableSqlServerDataType(DBColumn column)
		{
			switch ((SqlDbType) column.DataType)
			{
				case SqlDbType.Bit:
					return "bool";

				case SqlDbType.TinyInt:
					return "byte";

				case SqlDbType.SmallInt:
					return "short";

				case SqlDbType.Int:
					return "int";

				case SqlDbType.BigInt:
					return "long";

				case SqlDbType.Real:
					return "float";

				case SqlDbType.Float:
					return "double";

				case SqlDbType.Decimal:
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					return "decimal";

				case SqlDbType.Date:
				case SqlDbType.DateTime:
				case SqlDbType.DateTime2:
				case SqlDbType.SmallDateTime:
					return "DateTime";

				case SqlDbType.DateTimeOffset:
					return "DateTimeOffset";

				case SqlDbType.NText:
				case SqlDbType.NVarChar:
				case SqlDbType.Text:
				case SqlDbType.VarChar:
					return "string";

				case SqlDbType.NChar:
				case SqlDbType.Char:
					if (column.Length == 1)
						return "char";
					else
						return "string";

				case SqlDbType.Binary:
					return $"byte[]";

				case SqlDbType.VarBinary:
				case SqlDbType.Image:
				case SqlDbType.Timestamp:
					return "byte[]";

				case SqlDbType.Time:
					return "TimeSpan";

				case SqlDbType.Xml:
					return "string";

				case SqlDbType.UniqueIdentifier:
					return "Guid";
			}

			return "Unknown";
		}
		public static string GetNonNullablePostgresqlDataType(DBColumn column)
		{
			switch ((NpgsqlDbType) column.DataType)
			{
				case NpgsqlDbType.Bit:
					return "bool";

				case NpgsqlDbType.Smallint:
					return "short";

				case NpgsqlDbType.Integer:
					return "int";

				case NpgsqlDbType.Bigint:
					return "long";

				case NpgsqlDbType.Real:
					return "float";

				case NpgsqlDbType.Money:
					return "decimal";

				case NpgsqlDbType.Date:
					return "DateTime";

				case NpgsqlDbType.Text:
				case NpgsqlDbType.Varchar:
					return "string";

				case NpgsqlDbType.Char:
					if (column.Length == 1)
						return "char";
					else
						return "string";

				case NpgsqlDbType.Bytea:
					return $"byte[]";
			}

			return "Unknown";
		}
		public static string GetNonNullableMySqlDataType(DBColumn column)
		{
			switch ((MySqlDbType) column.DataType)
			{
				case MySqlDbType.Bit:
					return "bool";

				case MySqlDbType.Byte:
					return "sbyte";

				case MySqlDbType.UByte:
					return "byte";

				case MySqlDbType.Int16:
					return "short";

				case MySqlDbType.UInt16:
					return "ushort";

				case MySqlDbType.Int24:
				case MySqlDbType.Int32:
					return "int";

				case MySqlDbType.UInt24:
				case MySqlDbType.UInt32:
					return "uint";

				case MySqlDbType.Int64:
					return "long";

				case MySqlDbType.UInt64:
					return "ulong";

				case MySqlDbType.Float:
					return "float";

				case MySqlDbType.Double:
					return "double";

				case MySqlDbType.Decimal:
					return "decimal";

				case MySqlDbType.Date:
				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					return "DateTime";

				case MySqlDbType.Text:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.TinyText:
					return "string";

				case MySqlDbType.String:
					if (column.Length == 1)
						return "char";
					return "string";

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
					return "byte[]";

				case MySqlDbType.Time:
					return "TimeSpan";

				case MySqlDbType.Guid:
					return "Guid";
			}

			return "Unknown";
		}
		public static string GetSQLServerDataType(DBColumn column)
		{
			switch ((SqlDbType) column.DataType)
			{
				case SqlDbType.Bit:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case SqlDbType.SmallInt:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case SqlDbType.Int:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case SqlDbType.TinyInt:
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case SqlDbType.BigInt:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case SqlDbType.Float:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case SqlDbType.Decimal:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case SqlDbType.Date:
				case SqlDbType.DateTime:
				case SqlDbType.SmallDateTime:
				case SqlDbType.DateTime2:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case SqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case SqlDbType.Text:
				case SqlDbType.VarChar:
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
					return "string";

				case SqlDbType.Char:
				case SqlDbType.NChar:
					if (column.Length == 1)
						return "char";

					return "string";

				case SqlDbType.Binary:
				case SqlDbType.VarBinary:
				case SqlDbType.Timestamp:
					return "byte[]";

				case SqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case SqlDbType.DateTimeOffset:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case SqlDbType.Image:
					return "byte[]";

				case SqlDbType.UniqueIdentifier:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "Unknown";
		}
		public static string GetPostgresDataType(DBColumn column)
		{
			switch ((NpgsqlDbType)column.DataType)
			{
				case NpgsqlDbType.Boolean:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
					return "bool[]";

				case NpgsqlDbType.Bit:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
					{
						return "bool[]";
					}

				case NpgsqlDbType.Varbit:
					return "bool[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
					return "bool[][]";

				case NpgsqlDbType.Array | NpgsqlDbType.Bit:
					return "bool[][]";

				case NpgsqlDbType.Smallint:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
					return "short[]";

				case NpgsqlDbType.Integer:
						if (column.IsNullable)
							return "int?";
						else
							return "int";

				case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						return "int[]";

				case NpgsqlDbType.Bigint:
						if (column.IsNullable)
							return "long?";
						else
							return "long";

				case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						return "long[]";

				case NpgsqlDbType.Bytea:
					if (column.Length == 1)
					{
						if ( column.IsNullable )
							return "byte?";
						else
							return "byte";
					}
					else
						return "byte[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						return "byte[][]";

				case NpgsqlDbType.Text:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Text:
					return "string[]";

				case NpgsqlDbType.Varchar:
				case NpgsqlDbType.Json:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
				case NpgsqlDbType.Array | NpgsqlDbType.Json:
					return "string[]";

				case NpgsqlDbType.Char:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else if (string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						return "string";
					else
						return "char[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Char:
					return "string[]";

				case NpgsqlDbType.Uuid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
					return "Guid[]";

				case NpgsqlDbType.Date:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Date:
					return "DateTime[]";

				case NpgsqlDbType.TimeTz:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
					return "DateTimeOffset[]";

				case NpgsqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Time:
					return "TimeSpan[]";

				case NpgsqlDbType.Interval:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Interval:
					return "TimeSpan[]";

				case NpgsqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					return "DateTime[]";

				case NpgsqlDbType.TimestampTz:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
					return "DateTime[]";

				case NpgsqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case NpgsqlDbType.Array | NpgsqlDbType.Double:
					return "double[]";

				case NpgsqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case NpgsqlDbType.Array | NpgsqlDbType.Real:
					return "float[]";

				case NpgsqlDbType.Numeric:
				case NpgsqlDbType.Money:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
				case NpgsqlDbType.Array | NpgsqlDbType.Money:
					return "decimal[]";

				case NpgsqlDbType.Xml:
					return "string";
			}

			return "Unknown";
		}
		public static string GetMySqlDataType(DBColumn column)
		{
			switch ((MySqlDbType)column.DataType)
			{
				case MySqlDbType.Bit:
					if (string.Equals(column.dbDataType, "bit(1)", StringComparison.OrdinalIgnoreCase))
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
					{
						if (column.IsNullable)
							return "ulong?";
						else
							return "ulong";
					}

				case MySqlDbType.Byte:
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case MySqlDbType.UByte:
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case MySqlDbType.Int16:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case MySqlDbType.UInt16:
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case MySqlDbType.Int24:
				case MySqlDbType.Int32:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case MySqlDbType.UInt24:
				case MySqlDbType.UInt32:
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case MySqlDbType.Int64:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case MySqlDbType.UInt64:
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case MySqlDbType.Float:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case MySqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case MySqlDbType.Decimal:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case MySqlDbType.Date:
				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case MySqlDbType.Year:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case MySqlDbType.Text:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.TinyText:
					return "string";

				case MySqlDbType.String:
					if (column.Length == 1)
						return "char";
					return "string";

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
				case MySqlDbType.TinyBlob:
				case MySqlDbType.Blob:
				case MySqlDbType.MediumBlob:
				case MySqlDbType.LongBlob:
					return "byte[]";

				case MySqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case MySqlDbType.Guid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case MySqlDbType.Enum:
				case MySqlDbType.Set:
					return "string";
			}

			return "Unknown";
		}
		public static string GetPostgresqlResourceDataType(DBColumn column)
		{
			switch ((NpgsqlDbType) column.DataType)
			{
				case NpgsqlDbType.Bit:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "bool[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Bit:
					return "bool[][]";

				case NpgsqlDbType.Boolean:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
					return "bool[]";

				case NpgsqlDbType.Smallint:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
					return "short[]";

				case NpgsqlDbType.Integer:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case NpgsqlDbType.Array | NpgsqlDbType.Integer:
					return "int[]";

				case NpgsqlDbType.Bigint:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
					return "long[]";

				case NpgsqlDbType.Text:
				case NpgsqlDbType.Varchar:
				case NpgsqlDbType.Json:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Text:
				case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
				case NpgsqlDbType.Array | NpgsqlDbType.Char:
				case NpgsqlDbType.Array | NpgsqlDbType.Json:
					return "string[]";

				case NpgsqlDbType.Char:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else
						return "string";

				case NpgsqlDbType.Varbit:
					return "bool[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
					return "bool[][]";

				case NpgsqlDbType.Bytea:
					return "byte[]";

				case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
					return "byte[][]";

				case NpgsqlDbType.Date:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Date:
					return "DateTime[]";

				case NpgsqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Time:
					return "TimeSpan[]";

				case NpgsqlDbType.Interval:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Interval:
					return "TimeSpan[]";

				case NpgsqlDbType.TimeTz:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
					return "DateTimeOffset[]";

				case NpgsqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					return "DateTime[]";

				case NpgsqlDbType.TimestampTz:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
					return "DateTime[]";

				case NpgsqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case NpgsqlDbType.Array | NpgsqlDbType.Double:
					return "double[]";

				case NpgsqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case NpgsqlDbType.Array | NpgsqlDbType.Real:
					return "float[]";

				case NpgsqlDbType.Numeric:
				case NpgsqlDbType.Money:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
				case NpgsqlDbType.Array | NpgsqlDbType.Money:
					return "decimal[]";

				case NpgsqlDbType.Uuid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
					return "Guid[]";
			}

			return "Unknown";
		}
		public static string GetMySqlResourceDataType(DBColumn column)
		{
			switch ((MySqlDbType)column.DataType)
			{
				case MySqlDbType.Bit:
					if (string.Equals(column.dbDataType, "bit(1)", StringComparison.OrdinalIgnoreCase))
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
					{
						if (column.IsNullable)
							return "ulong?";
						else
							return "ulong";
					}

				case MySqlDbType.Byte:
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case MySqlDbType.UByte:
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case MySqlDbType.Int16:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case MySqlDbType.UInt16:
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case MySqlDbType.Int24:
				case MySqlDbType.Int32:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case MySqlDbType.UInt24:
				case MySqlDbType.UInt32:
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case MySqlDbType.Int64:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case MySqlDbType.UInt64:
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case MySqlDbType.Float:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case MySqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case MySqlDbType.Decimal:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case MySqlDbType.Date:
				case MySqlDbType.DateTime:
				case MySqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case MySqlDbType.Year:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case MySqlDbType.Text:
				case MySqlDbType.MediumText:
				case MySqlDbType.LongText:
				case MySqlDbType.VarChar:
				case MySqlDbType.VarString:
				case MySqlDbType.TinyText:
					return "string";

				case MySqlDbType.String:
					if (column.Length == 1)
						return "char";
					return "string";

				case MySqlDbType.Binary:
				case MySqlDbType.VarBinary:
				case MySqlDbType.TinyBlob:
				case MySqlDbType.Blob:
				case MySqlDbType.MediumBlob:
				case MySqlDbType.LongBlob:
					return "byte[]";

				case MySqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case MySqlDbType.Guid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case MySqlDbType.Enum:
				case MySqlDbType.Set:
					return "string";
			}

			return "Unknown";
		}
		public static string GetSqlServerResourceDataType(DBColumn column)
		{
			switch ((SqlDbType)column.DataType)
			{
				case SqlDbType.Bit:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case SqlDbType.SmallInt:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case SqlDbType.Int:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case SqlDbType.TinyInt:
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case SqlDbType.BigInt:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case SqlDbType.Float:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case SqlDbType.Decimal:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case SqlDbType.Date:
				case SqlDbType.DateTime:
				case SqlDbType.SmallDateTime:
				case SqlDbType.DateTime2:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case SqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case SqlDbType.Text:
				case SqlDbType.VarChar:
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
					return "string";

				case SqlDbType.Char:
				case SqlDbType.NChar:
					if (column.Length == 1)
						return "char";

					return "string";

				case SqlDbType.Binary:
				case SqlDbType.VarBinary:
				case SqlDbType.Timestamp:
					return "byte[]";

				case SqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case SqlDbType.DateTimeOffset:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case SqlDbType.Image:
					return "Image";

				case SqlDbType.UniqueIdentifier:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "Unknown";
		}
	}
}
