using COFRS.Template.Common.Models;
using MySql.Data.MySqlClient;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;

namespace COFRS.Template.Common.ServiceUtilities
{
	public static class DBHelper
	{
		public static MemoryCache _cache = new MemoryCache("ClassCache");

		public static string GetPostgresqlExampleValue(DBColumn column)
        {
			if ( string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
            {
				if (column.Length == 1)
					return "a";
				else
					return "string";
            }
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "int2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "oid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "xid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "cid", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "name", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "citext", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			else if (string.Equals(column.DBDataType, "bool", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "timestamp", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("s");
			}
			else if (string.Equals(column.DBDataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "float4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "money", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "bytea", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbit", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.DBDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}

		public static string GetMySqlExampleValue(DBColumn column)
		{
			if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "sysname", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "nvarchar", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			if (string.Equals(column.DBDataType, "year", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy");
			}
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase) ||
			         string.Equals(column.DBDataType, "nchar", StringComparison.OrdinalIgnoreCase))
					{
						if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyint(1)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint unsigned", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "datetime", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbinary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "blob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "tinyblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "mediumblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "longblob", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.DBDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}
		public static string GetSqlServerExampleValue(DBColumn column)
		{
			if (string.Equals(column.DBDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "ntext", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.DBDataType, "nvarchar", StringComparison.OrdinalIgnoreCase) )
			{
				return "string";
			}
			else if (string.Equals(column.DBDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallint)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "int)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "bigint", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.DBDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.DBDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.DBDataType, "datetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "datetime2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smalldatetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.DBDataType, "real", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "money", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "smallmoney", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.DBDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.DBDataType, "varbinary", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.DBDataType, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}

			return "example";
		}


		/// <summary>
		/// Convers a Postgresql data type into its corresponding standard SQL data type
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public static string ConvertPostgresqlDataType(string dataType)
		{
			if (string.Equals(dataType, "bpchar", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "_char", StringComparison.OrdinalIgnoreCase))
				return "char[]";
			else if (string.Equals(dataType, "char", StringComparison.OrdinalIgnoreCase))
				return "char";
			else if (string.Equals(dataType, "int2", StringComparison.OrdinalIgnoreCase))
				return "short";
			else if (string.Equals(dataType, "_int2", StringComparison.OrdinalIgnoreCase))
				return "short[]";
			else if (string.Equals(dataType, "int4", StringComparison.OrdinalIgnoreCase))
				return "int";
			else if (string.Equals(dataType, "_int4", StringComparison.OrdinalIgnoreCase))
				return "int[]";
			else if (string.Equals(dataType, "oid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_oid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "xid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_xid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "cid", StringComparison.OrdinalIgnoreCase))
				return "uint";
			else if (string.Equals(dataType, "_cid", StringComparison.OrdinalIgnoreCase))
				return "uint[]";
			else if (string.Equals(dataType, "point", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPoint";
			else if (string.Equals(dataType, "_point", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPoint[]";
			else if (string.Equals(dataType, "lseg", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLSeg";
			else if (string.Equals(dataType, "_lseg", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLSeg[]";
			else if (string.Equals(dataType, "line", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLine";
			else if (string.Equals(dataType, "_line", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlLine[]";
			else if (string.Equals(dataType, "circle", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlCircle";
			else if (string.Equals(dataType, "_circle", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlCircle[]";
			else if (string.Equals(dataType, "path", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPath";
			else if (string.Equals(dataType, "_path", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPath[]";
			else if (string.Equals(dataType, "polygon", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPolygon";
			else if (string.Equals(dataType, "_polygon", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlPolygon[]";
			else if (string.Equals(dataType, "box", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlBox";
			else if (string.Equals(dataType, "_box", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlBox[]";
			else if (string.Equals(dataType, "int8", StringComparison.OrdinalIgnoreCase))
				return "long";
			else if (string.Equals(dataType, "_int8", StringComparison.OrdinalIgnoreCase))
				return "long[]";
			else if (string.Equals(dataType, "varchar", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_varchar", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "text", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_text", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "citext", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_citext", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "name", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_name", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "_bit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "varbit", StringComparison.OrdinalIgnoreCase))
				return "BitArray";
			else if (string.Equals(dataType, "_varbit", StringComparison.OrdinalIgnoreCase))
				return "BitArray[][]";
			else if (string.Equals(dataType, "bytea", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_bytea", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";
			else if (string.Equals(dataType, "bool", StringComparison.OrdinalIgnoreCase))
				return "bool";
			else if (string.Equals(dataType, "_bool", StringComparison.OrdinalIgnoreCase))
				return "bool[]";
			else if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase))
				return "DateTime";
			else if (string.Equals(dataType, "_date", StringComparison.OrdinalIgnoreCase))
				return "DateTime[]";
			else if (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase))
				return "DateTime";
			else if (string.Equals(dataType, "_timestamp", StringComparison.OrdinalIgnoreCase))
				return "DateTime[]";
			else if (string.Equals(dataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset";
			else if (string.Equals(dataType, "_timestamptz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset[]";
			else if (string.Equals(dataType, "timetz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset";
			else if (string.Equals(dataType, "_timetz", StringComparison.OrdinalIgnoreCase))
				return "DateTimeOffset[]";
			else if (string.Equals(dataType, "time", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan";
			else if (string.Equals(dataType, "_time", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan[]";
			else if (string.Equals(dataType, "interval", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan";
			else if (string.Equals(dataType, "_interval", StringComparison.OrdinalIgnoreCase))
				return "TimeSpan[]";
			else if (string.Equals(dataType, "float8", StringComparison.OrdinalIgnoreCase))
				return "double";
			else if (string.Equals(dataType, "_float8", StringComparison.OrdinalIgnoreCase))
				return "double[]";
			else if (string.Equals(dataType, "float4", StringComparison.OrdinalIgnoreCase))
				return "single";
			else if (string.Equals(dataType, "_float4", StringComparison.OrdinalIgnoreCase))
				return "single[]";
			else if (string.Equals(dataType, "money", StringComparison.OrdinalIgnoreCase))
				return "decimal";
			else if (string.Equals(dataType, "_money", StringComparison.OrdinalIgnoreCase))
				return "decimal[]";
			else if (string.Equals(dataType, "numeric", StringComparison.OrdinalIgnoreCase))
				return "decimal";
			else if (string.Equals(dataType, "_numeric", StringComparison.OrdinalIgnoreCase))
				return "decimal[]";
			else if (string.Equals(dataType, "uuid", StringComparison.OrdinalIgnoreCase))
				return "Guid";
			else if (string.Equals(dataType, "_uuid", StringComparison.OrdinalIgnoreCase))
				return "Guid[]";
			else if (string.Equals(dataType, "json", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_json", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "jsonb", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_jsonb", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "jsonpath", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_jsonpath", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "xml", StringComparison.OrdinalIgnoreCase))
				return "string";
			else if (string.Equals(dataType, "_xml", StringComparison.OrdinalIgnoreCase))
				return "string[]";
			else if (string.Equals(dataType, "inet", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet";
			else if (string.Equals(dataType, "_inet", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet[]";
			else if (string.Equals(dataType, "cidr", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet";
			else if (string.Equals(dataType, "_cidr", StringComparison.OrdinalIgnoreCase))
				return "NpgsqlInet[]"; 
			else if (string.Equals(dataType, "macaddr", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_macaddr", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";
			else if (string.Equals(dataType, "macaddr8", StringComparison.OrdinalIgnoreCase))
				return "byte[]";
			else if (string.Equals(dataType, "_macaddr8", StringComparison.OrdinalIgnoreCase))
				return "byte[][]";

			return "";
		}

		/// <summary>
		/// Returns a model type based upon the SQL Server database metadata
		/// </summary>
		/// <param name="column">The <see cref="DBColumn"/> that contains the database metadata</param>
		/// <returns>The corresponding C# model type</returns>
		public static string GetSQLServerDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "int":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "tinyint":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "float":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
				case "numeric":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "smalldatetime":
				case "datetime2":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "text":
				case "varchar":
				case "ntext":
				case "nvarchar":
					return "string";

				case "char":
				case "nchar":
					if (column.Length == 1)
						return "char";

					return "string";

				case "binary":
				case "varbinary":
				case "timestamp":
					return $"IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "datetimeoffset":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "money":
				case "smallmoney":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "image":
					return "Image";

				case "uniqueidentifier":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "";
		}

		public static string GetPostgresDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "boolean":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "_boolean":
					return "BitArray";

				case "bit":
				case "varbit":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case "_varbit":
				case "_bit":
					if (column.Length == 1)
						return "BitArray";
					else
						return "BitArray[]";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "_smallint":
					return "short[]";

				case "integer":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "_integer":
					return "int[]";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "_bigint":
					return "long[]";

				case "oid":
				case "xid":
				case "cid":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "_oid":
				case "_xid":
				case "_cid":
					return "uint[]";

				case "point":
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case "_point":
					return "NpgsqlPoint[]";

				case "lseg":
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case "_lseg":
					return "NpgsqlLSeg[]";

				case "line":
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case "_line":
					return "NpgsqlLine[]";

				case "circle":
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case "_circle":
					return "NpgsqlCircle[]";

				case "box":
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case "_box":
					return "NpgsqlBox[]";

				case "path":
					return "NpgsqlPoint[]";

				case "_path":
					return "NpgsqlPoint[][]";

				case "polygon":
					return "NpgsqlPoint[]";

				case "_polygon":
					return "NpgsqlPoint[][]";

				case "bytea":
					return "byte[]";

				case "_bytea":
					return "byte[][]";

				case "text":
				case "citext":
					return "string";

				case "name":
					return "string";

				case "_text":
				case "_name":
				case "_citext":
					return "string[]";

				case "varchar":
				case "json":
					return "string";

				case "_varchar":
				case "_json":
					return "string[]";

				case "char":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else
						return "char[]";

				case "bpchar":
					return "string";

				case "_char":
					return "string[]";

				case "uuid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "_uuid":
					return "Guid[]";

				case "date":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_date":
					return "DateTime[]";

				case "timetz":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "_timetz":
					return "DateTimeOffset[]";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_time":
					return "TimeSpan[]";

				case "interval":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_interval":
					return "TimeSpan[]";

				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamp":
					return "DateTime[]";

				case "timestamptz":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamptz":
					return "DateTime[]";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "_double":
					return "double[]";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "_real":
					return "float[]";

				case "numeric":
				case "money":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "_numeric":
				case "_money":
					return "decimal[]";

				case "xml":
					return "string";

				case "_xml":
					return "string[]";

				case "jsonb":
					return "string";

				case "_jsonb":
					return "string[]";

				case "jsonpath":
					return "string";

				case "_jsonpath":
					return "string[]";

				case "inet":
					return "IPAddress";

				case "cidr":
					return "ValueTuple<IPAddress, int>";

				case "_inet":
					return "IPAddress[]";

				case "_cidr":
					return "ValueTuple<IPAddress, int>[]";

				case "macaddr":
				case "macaddr8":
					return "PhysicalAddress";

				case "_macaddr":
				case "_macaddr8":
					return "PhysicalAddress[]";
			}

			return "";
		}

		public static string GetMySqlDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit(1)":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "bit":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "byte":
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case "ubyte":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "int16":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "uint16":
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case "int24":
				case "int32":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "uint24":
				case "uint32":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "int64":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "uint64":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "float":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "year":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "text":
				case "mediumtext":
				case "longtext":
				case "varchar":
				case "varstring":
				case "tinytext":
					return "string";

				case "string":
					if (column.Length == 1)
						return "char";
					return "string";

				case "binary":
				case "varbinary":
				case "tinyblob":
				case "blob":
				case "mediumblob":
				case "longblob":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "guid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "enum":
				case "set":
				case "json":
					return "string";
			}

			return "";
		}

		public static string GetPostgresqlResourceDataType(DBColumn column, List<ResourceModel> resourceModels)
		{
			switch (column.DBDataType.ToLower())
			{
				case "boolean":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "_boolean":
					return "BitArray";

				case "bit":
				case "varbit":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case "_varbit":
					if (column.Length == 1)
						return "BitArray";
					else
						return "BitArray[]";

				case "_bit":
						if (column.Length == 1)
						{
							if (column.IsNullable)
								return "bool?";
							else
								return "bool";
						}
						else
							return "BitArray";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "_smallint":
					return "short[]";

				case "integer":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "_integer":
					return "int[]";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "_bigint":
					return "long[]";

				case "oid":
				case "xid":
				case "cid":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "_oid":
				case "_xid":
				case "_cid":
					return "uint[]";

				case "point":
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case "_point":
					return "NpgsqlPoint[]";

				case "lseg":
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case "_lseg":
					return "NpgsqlLSeg[]";

				case "line":
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case "_line":
					return "NpgsqlLine[]";

				case "circle":
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case "_circle":
					return "NpgsqlCircle[]";

				case "box":
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case "_box":
					return "NpgsqlBox[]";

				case "path":
					return "NpgsqlPoint[]";

				case "_path":
					return "NpgsqlPoint[][]";

				case "polygon":
					return "NpgsqlPoint[]";

				case "_polygon":
					return "NpgsqlPoint[][]";

				case "bytea":
					return "byte[]";

				case "_bytea":
					return "byte[][]";

				case "text":
				case "citext":
					return "string";

				case "name":
					if (string.Equals(column.DBDataType, "_name", StringComparison.OrdinalIgnoreCase))
						return "string[]";
					else
						return "string";

				case "_text":
				case "_name":
				case "_citext":
					return "string[]";

				case "varchar":
				case "json":
					return "string";

				case "_varchar":
				case "_json":
					return "string[]";

				case "char":
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "char?";
						else
							return "char";
					}
					else
						return "char[]";

				case "bpchar":
					return "string";

				case "_char":
					return "string[]";

				case "uuid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "_uuid":
					return "Guid[]";

				case "date":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_date":
					return "DateTime[]";

				case "timetz":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "_timetz":
					return "DateTimeOffset[]";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_time":
					return "TimeSpan[]";

				case "interval":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "_interval":
					return "TimeSpan[]";

				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamp":
					return "DateTime[]";

				case "timestamptz":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "_timestamptz":
					return "DateTime[]";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "_double":
					return "double[]";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "_real":
					return "float[]";

				case "numeric":
				case "money":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "_numeric":
				case "_money":
					return "decimal[]";

				case "xml":
					return "string";

				case "_xml":
					return "string[]";

				case "jsonb":
					return "string";

				case "_jsonb":
					return "string[]";

				case "jsonpath":
					return "string";

				case "_jsonpath":
					return "string[]";

				case "inet":
					return "IPAddress";

				case "cidr":
					return "ValueTuple<IPAddress, int>";

				case "_inet":
					return "IPAddress[]";

				case "_cidr":
					return "ValueTuple<IPAddress, int>[]";

				case "macaddr":
				case "macaddr8":
					return "PhysicalAddress";

				case "_macaddr":
				case "_macaddr8":
					return "PhysicalAddress[]";

				default:
					{
						var resourceModel = resourceModels.FirstOrDefault(e =>
							string.Equals(e.EntityModel.TableName, column.DBDataType, StringComparison.OrdinalIgnoreCase));

						if (resourceModel != null)
							return resourceModel.ClassName;
					}
					break;
			}

			return "";
		}

		public static string GetMySqlResourceDataType(DBColumn column)
		{
			switch (column.DBDataType.ToLower())
			{
				case "bit(1)":
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
				case "bit":
						if (column.IsNullable)
							return "ulong?";
						else
							return "ulong";

				case "byte":
					if (column.IsNullable)
						return "sbyte?";
					else
						return "sbyte";

				case "ubyte":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "int16":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "uint16":
					if (column.IsNullable)
						return "ushort?";
					else
						return "ushort";

				case "int24":
				case "int32":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "uint24":
				case "uint32":
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case "int64":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "uint64":
					if (column.IsNullable)
						return "ulong?";
					else
						return "ulong";

				case "float":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "double":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "timestamp":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "year":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "text":
				case "mediumtext":
				case "longtext":
				case "varchar":
				case "varstring":
				case "tinytext":
					return "string";

				case "string":
					if (column.Length == 1)
						return "char";
					return "string";

				case "binary":
				case "varbinary":
				case "tinyblob":
				case "blob":
				case "mediumblob":
				case "longblob":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "guid":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case "enum":
				case "set":
				case "json":
					return "string";
			}

			return "";
		}

		public static string GetSqlServerResourceDataType(DBColumn column)
		{
			switch ( column.DBDataType.ToLower())
			{
				case "bit":
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case "smallint":
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case "int":
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case "tinyint":
					if (column.IsNullable)
						return "byte?";
					else
						return "byte";

				case "bigint":
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case "float":
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case "decimal":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "date":
				case "datetime":
				case "smalldatetime":
				case "datetime2":
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case "real":
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case "text":
				case "varchar":
				case "ntext":
				case "nvarchar":
					return "string";

				case "char":
				case "nchar":
					if (column.Length == 1)
						return "char";

					return "string";

				case "binary":
				case "varbinary":
				case "timestamp":
					return "IEnumerable<byte>";

				case "time":
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case "datetimeoffset":
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case "money":
				case "smallmoney":
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case "image":
					return "Image";

				case "uniqueidentifier":
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";
			}

			return "Unknown";
		}

		#region Postgrsql Helper Functions
		public static ElementType GetElementType(string schema, string datatype, EntityMap entityMap, string connectionString)
		{
				var entityModel = entityMap.Maps.FirstOrDefault(c =>
					c.GetType() == typeof(EntityModel) &&
					string.Equals(((EntityModel)c).SchemaName, schema, StringComparison.OrdinalIgnoreCase) && 
					string.Equals(((EntityModel)c).TableName, datatype, StringComparison.OrdinalIgnoreCase));

				if (entityModel != null)
					return entityModel.ElementType;

			string query = @"
select t.typtype
  from pg_type as t 
 inner join pg_catalog.pg_namespace n on n.oid = t.typnamespace
 WHERE ( t.typrelid = 0 OR ( SELECT c.relkind = 'c' FROM pg_catalog.pg_class c WHERE c.oid = t.typrelid ) )
   AND NOT EXISTS ( SELECT 1 FROM pg_catalog.pg_type el WHERE el.oid = t.typelem AND el.typarray = t.oid )
   and ( t.typcategory = 'C' or t.typcategory = 'E' ) 
   and n.nspname = @schema
   and t.typname = @element
";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@schema", schema);
					command.Parameters.AddWithValue("@element", datatype);

					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							var theType = reader.GetChar(0);

							if (theType == 'c')
								return ElementType.Composite;

							else if (theType == 'e')
								return ElementType.Enum;
						}
					}
				}
			}

			return ElementType.Table;
		}

		#endregion
	}
}
