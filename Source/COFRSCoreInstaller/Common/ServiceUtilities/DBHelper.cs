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
			if ( string.Equals(column.dbDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
            {
				if (column.Length == 1)
					return "a";
				else
					return "string";
            }
			else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.dbDataType, "int2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "int4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "int8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "oid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "xid", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "cid", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.dbDataType, "text", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "name", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "citext", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			else if (string.Equals(column.dbDataType, "bool", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.dbDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.dbDataType, "timestamp", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("s");
			}
			else if (string.Equals(column.dbDataType, "timestamptz", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.dbDataType, "float4", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "float8", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "money", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.dbDataType, "bytea", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "varbit", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.dbDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.dbDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}

		public static string GetMySqlExampleValue(DBColumn column)
		{
			if (string.Equals(column.dbDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "sysname", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "nvarchar", StringComparison.OrdinalIgnoreCase))
			{
				return "string";
			}
			if (string.Equals(column.dbDataType, "year", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy");
			}
			else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase) ||
			         string.Equals(column.dbDataType, "nchar", StringComparison.OrdinalIgnoreCase))
					{
						if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.dbDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "tinyint(1)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "tinyint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "int", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "int unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "smallint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "smallint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "mediumint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "mediumint unsigned", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "bigint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "bigint unsigned", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.dbDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.dbDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.dbDataType, "datetime", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.dbDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.dbDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "varbinary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "blob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "tinyblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "mediumblob", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "longblob", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.dbDataType, "uuid", StringComparison.OrdinalIgnoreCase))
			{
				return Guid.NewGuid().ToString();
			}
			else if (string.Equals(column.dbDataType, "inet", StringComparison.OrdinalIgnoreCase))
			{
				return "184.241.2.54";
			}

			return "example";
		}
		public static string GetSqlServerExampleValue(DBColumn column)
		{
			if (string.Equals(column.dbDataType, "text", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "ntext", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "varchar", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(column.dbDataType, "nvarchar", StringComparison.OrdinalIgnoreCase) )
			{
				return "string";
			}
			else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase))
			{
				if (column.Length == 1)
					return "a";
				else
					return "string";
			}
			else if (string.Equals(column.dbDataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "smallint)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "int)", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "bigint", StringComparison.OrdinalIgnoreCase))
			{
				return "123";
			}
			else if (string.Equals(column.dbDataType, "bit", StringComparison.OrdinalIgnoreCase))
			{
				return "true";
			}
			else if (string.Equals(column.dbDataType, "date", StringComparison.OrdinalIgnoreCase))
			{
				return DateTime.Now.ToString("yyyy-MM-dd");
			}
			else if (string.Equals(column.dbDataType, "datetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "datetime2", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "smalldatetime", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
			{
				return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ss.fffffzzz");
			}
			else if (string.Equals(column.dbDataType, "real", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "money", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "numeric", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "smallmoney", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "float", StringComparison.OrdinalIgnoreCase))
			{
				return "123.45";
			}
			else if (string.Equals(column.dbDataType, "binary", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.dbDataType, "varbinary", StringComparison.OrdinalIgnoreCase))
			{
				return "VGhpcyBpcyBhbiBleGFtcGxlIHZhbHVl";
			}
			else if (string.Equals(column.dbDataType, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
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
			else if (string.Equals(dataType, "oid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Oid;
			else if (string.Equals(dataType, "_oid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Oid;
			else if (string.Equals(dataType, "xid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Xid;
			else if (string.Equals(dataType, "_xid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Xid;
			else if (string.Equals(dataType, "cid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Cid;
			else if (string.Equals(dataType, "_cid", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Cid;
			else if (string.Equals(dataType, "point", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Point;
			else if (string.Equals(dataType, "_point", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Point;
			else if (string.Equals(dataType, "lseg", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.LSeg;
			else if (string.Equals(dataType, "_lseg", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.LSeg;
			else if (string.Equals(dataType, "line", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Line;
			else if (string.Equals(dataType, "_line", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Line;
			else if (string.Equals(dataType, "circle", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Circle;
			else if (string.Equals(dataType, "_circle", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Circle;
			else if (string.Equals(dataType, "path", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Path;
			else if (string.Equals(dataType, "_path", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Path;
			else if (string.Equals(dataType, "polygon", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Polygon;
			else if (string.Equals(dataType, "_polygon", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Polygon;
			else if (string.Equals(dataType, "box", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Box;
			else if (string.Equals(dataType, "_box", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Box;
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
			else if (string.Equals(dataType, "citext", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Citext;
			else if (string.Equals(dataType, "_citext", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Citext;
			else if (string.Equals(dataType, "name", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Name;
			else if (string.Equals(dataType, "_name", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Name;
			else if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Bit;
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
			else if (string.Equals(dataType, "jsonb", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Jsonb;
			else if (string.Equals(dataType, "_jsonb", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Jsonb;
			else if (string.Equals(dataType, "jsonpath", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.JsonPath;
			else if (string.Equals(dataType, "_jsonpath", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.JsonPath;
			else if (string.Equals(dataType, "xml", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Xml;
			else if (string.Equals(dataType, "_xml", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Xml;
			else if (string.Equals(dataType, "inet", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Inet;
			else if (string.Equals(dataType, "_inet", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Inet;
			else if (string.Equals(dataType, "cidr", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Cidr;
			else if (string.Equals(dataType, "_cidr", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.Cidr;
			else if (string.Equals(dataType, "macaddr", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.MacAddr;
			else if (string.Equals(dataType, "_macaddr", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.MacAddr;
			else if (string.Equals(dataType, "macaddr8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.MacAddr8;
			else if (string.Equals(dataType, "_macaddr8", StringComparison.OrdinalIgnoreCase))
				return NpgsqlDbType.Array | NpgsqlDbType.MacAddr8;

			return NpgsqlDbType.Unknown;
		}

		public static MySqlDbType ConvertMySqlDataType(string dataType)
		{
			if (string.Equals(dataType, "tinyint", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.Byte;
			else if (string.Equals(dataType, "tinyint(1)", StringComparison.OrdinalIgnoreCase))
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
			else if (dataType.StartsWith("json", StringComparison.OrdinalIgnoreCase))
				return MySqlDbType.JSON;

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
			switch ((SqlDbType)column.DataType)
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
					return $"IEnumerable<byte>";

				case SqlDbType.VarBinary:
				case SqlDbType.Image:
				case SqlDbType.Timestamp:
					return $"IEnumerable<byte>";

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
			switch ((NpgsqlDbType)column.DataType)
			{
				case NpgsqlDbType.Bit:
				case NpgsqlDbType.Boolean:
					return "bool";

				case NpgsqlDbType.Smallint:
					return "short";

				case NpgsqlDbType.Integer:
					return "int";

				case NpgsqlDbType.Bigint:
					return "long";

				case NpgsqlDbType.Real:
					return "float";

				case NpgsqlDbType.Numeric:
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
					return $"IEnumerable<byte>";
			}

			return "Unknown";
		}

		public static string GetNonNullableMySqlDataType(DBColumn column)
		{
			switch ((MySqlDbType)column.DataType)
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
					return $"IEnumerable<byte>";

				case MySqlDbType.Time:
					return "TimeSpan";

				case MySqlDbType.Guid:
					return "Guid";
			}

			return "Unknown";
		}

		public static string GetSQLServerDataType(DBColumn column)
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
					return $"IEnumerable<byte>";

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

		public static string GetPostgresDataType(DBColumn column, List<ClassFile> classList)
		{
			switch ((NpgsqlDbType)column.DataType)
			{
				case NpgsqlDbType.Boolean:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
					return "BitArray";

				case NpgsqlDbType.Bit:
				case NpgsqlDbType.Varbit:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
				case NpgsqlDbType.Array | NpgsqlDbType.Bit:
					if (string.Equals(column.dbDataType, "_bit", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(column.dbDataType, "_varbit", StringComparison.OrdinalIgnoreCase))
					{
						if (column.Length == 1)
							return "BitArray";
						else
							return "IEnumerable<BitArray>";
					}
					else
					{
						if (column.Length == 1)
						{
							if (column.IsNullable)
								return "bool?";
							else
								return "bool";
						}
						else
							return "BitArray";
					}

				case NpgsqlDbType.Smallint:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
					return "IEnumerable<short>";

				case NpgsqlDbType.Integer:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case NpgsqlDbType.Array | NpgsqlDbType.Integer:
					return "IEnumerable<int>";

				case NpgsqlDbType.Bigint:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
					return "IEnumerable<long>";

				case NpgsqlDbType.Oid:
				case NpgsqlDbType.Xid:
				case NpgsqlDbType.Cid:
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case NpgsqlDbType.Array | NpgsqlDbType.Oid:
				case NpgsqlDbType.Array | NpgsqlDbType.Xid:
				case NpgsqlDbType.Array | NpgsqlDbType.Cid:
					return "IEnumerable<uint>";

				case NpgsqlDbType.Point:
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case NpgsqlDbType.Array | NpgsqlDbType.Point:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.LSeg:
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case NpgsqlDbType.Array | NpgsqlDbType.LSeg:
					return "IEnumerable<NpgsqlLSeg>";

				case NpgsqlDbType.Line:
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case NpgsqlDbType.Array | NpgsqlDbType.Line:
					return "IEnumerable<NpgsqlLine>";

				case NpgsqlDbType.Circle:
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case NpgsqlDbType.Array | NpgsqlDbType.Circle:
					return "IEnumerable<NpgsqlCircle>";

				case NpgsqlDbType.Box:
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case NpgsqlDbType.Array | NpgsqlDbType.Box:
					return "IEnumerable<NpgsqlBox>";

				case NpgsqlDbType.Path:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.Array | NpgsqlDbType.Path:
					return "IEnumerable<IEnumerable<NpgsqlPoint>>";

				case NpgsqlDbType.Polygon:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.Array | NpgsqlDbType.Polygon:
					return "IEnumerable<IEnumerable<NpgsqlPoint>>";

				case NpgsqlDbType.Bytea:
					return "IEnumerable<byte>";

				case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
					return "IEnumerable<IEnumerable<byte>>";

				case NpgsqlDbType.Text:
				case NpgsqlDbType.Citext:
					return "string";

				case NpgsqlDbType.Name:
					if (string.Equals(column.dbDataType, "_name", StringComparison.OrdinalIgnoreCase))
						return "IEnumerable<string>";
					else
						return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Text:
				case NpgsqlDbType.Array | NpgsqlDbType.Name:
				case NpgsqlDbType.Array | NpgsqlDbType.Citext:
					return "IEnumerable<string>";

				case NpgsqlDbType.Varchar:
				case NpgsqlDbType.Json:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
				case NpgsqlDbType.Array | NpgsqlDbType.Json:
					return "IEnumerable<string>";

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
						return "IEnumerable<char>";

				case NpgsqlDbType.Array | NpgsqlDbType.Char:
					return "IEnumerable<string>";

				case NpgsqlDbType.Uuid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
					return "IEnumerable<Guid>";

				case NpgsqlDbType.Date:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Date:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.TimeTz:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
					return "IEnumerable<DateTimeOffset>";

				case NpgsqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Time:
					return "IEnumerable<TimeSpan>";

				case NpgsqlDbType.Interval:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Interval:
					return "IEnumerable<TimeSpan>";

				case NpgsqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.TimestampTz:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case NpgsqlDbType.Array | NpgsqlDbType.Double:
					return "IEnumerable<double>";

				case NpgsqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case NpgsqlDbType.Array | NpgsqlDbType.Real:
					return "IEnumerable<float>";

				case NpgsqlDbType.Numeric:
				case NpgsqlDbType.Money:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
				case NpgsqlDbType.Array | NpgsqlDbType.Money:
					return "IEnumerable<decimal>";

				case NpgsqlDbType.Xml:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Xml:
					return "IEnumerable<string>";

				case NpgsqlDbType.Jsonb:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Jsonb:
					return "IEnumerable<string>";

				case NpgsqlDbType.JsonPath:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.JsonPath:
					return "IEnumerable<string>";

				case NpgsqlDbType.Inet:
					return "IPAddress";

				case NpgsqlDbType.Cidr:
					return "ValueTuple<IPAddress, int>";

				case NpgsqlDbType.Array | NpgsqlDbType.Inet:
					return "IEnumerable<IPAddress>";

				case NpgsqlDbType.Array | NpgsqlDbType.Cidr:
					return "IEnumerable<ValueTuple<IPAddress, int>>";

				case NpgsqlDbType.MacAddr:
				case NpgsqlDbType.MacAddr8:
					return "PhysicalAddress";

				case NpgsqlDbType.Array | NpgsqlDbType.MacAddr:
				case NpgsqlDbType.Array | NpgsqlDbType.MacAddr8:
					return "IEnumerable<PhysicalAddress>";

				case NpgsqlDbType.Unknown:
                    {
						var entity = classList.FirstOrDefault(e =>
							e.GetType() == typeof(EntityClassFile) &&
							string.Equals(((EntityClassFile)e).TableName, column.dbDataType, StringComparison.OrdinalIgnoreCase));

						if (entity != null)
							return entity.ClassName;
                    }					
					return StandardUtils.CorrectForReservedNames(StandardUtils.NormalizeClassName(column.dbDataType));
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
					return "IEnumerable<byte>";

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
				case MySqlDbType.JSON:
					return "string";
			}

			return "Unknown";
		}

		public static string GetPostgresqlResourceDataType(DBColumn column, List<ClassFile> classList)
		{
			switch ((NpgsqlDbType)column.DataType)
			{
				case NpgsqlDbType.Boolean:
					if (column.IsNullable)
						return "bool?";
					else
						return "bool";

				case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
					return "BitArray";

				case NpgsqlDbType.Bit:
				case NpgsqlDbType.Varbit:
					if (column.Length == 1)
					{
						if (column.IsNullable)
							return "bool?";
						else
							return "bool";
					}
					else
						return "BitArray";

				case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
				case NpgsqlDbType.Array | NpgsqlDbType.Bit:
					if (string.Equals(column.dbDataType, "_bit", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(column.dbDataType, "_varbit", StringComparison.OrdinalIgnoreCase))
					{
						if (column.Length == 1)
							return "BitArray";
						else
							return "IEnumerable<BitArray>";
					}
					else
					{
						if (column.Length == 1)
						{
							if (column.IsNullable)
								return "bool?";
							else
								return "bool";
						}
						else
							return "BitArray";
					}

				case NpgsqlDbType.Smallint:
					if (column.IsNullable)
						return "short?";
					else
						return "short";

				case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
					return "IEnumerable<short>";

				case NpgsqlDbType.Integer:
					if (column.IsNullable)
						return "int?";
					else
						return "int";

				case NpgsqlDbType.Array | NpgsqlDbType.Integer:
					return "IEnumerable<int>";

				case NpgsqlDbType.Bigint:
					if (column.IsNullable)
						return "long?";
					else
						return "long";

				case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
					return "IEnumerable<long>";

				case NpgsqlDbType.Oid:
				case NpgsqlDbType.Xid:
				case NpgsqlDbType.Cid:
					if (column.IsNullable)
						return "uint?";
					else
						return "uint";

				case NpgsqlDbType.Array | NpgsqlDbType.Oid:
				case NpgsqlDbType.Array | NpgsqlDbType.Xid:
				case NpgsqlDbType.Array | NpgsqlDbType.Cid:
					return "IEnumerable<uint>";

				case NpgsqlDbType.Point:
					if (column.IsNullable)
						return "NpgsqlPoint?";
					else
						return "NpgsqlPoint";

				case NpgsqlDbType.Array | NpgsqlDbType.Point:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.LSeg:
					if (column.IsNullable)
						return "NpgsqlLSeg?";
					else
						return "NpgsqlLSeg";

				case NpgsqlDbType.Array | NpgsqlDbType.LSeg:
					return "IEnumerable<NpgsqlLSeg>";

				case NpgsqlDbType.Line:
					if (column.IsNullable)
						return "NpgsqlLine?";
					else
						return "NpgsqlLine";

				case NpgsqlDbType.Array | NpgsqlDbType.Line:
					return "IEnumerable<NpgsqlLine>";

				case NpgsqlDbType.Circle:
					if (column.IsNullable)
						return "NpgsqlCircle?";
					else
						return "NpgsqlCircle";

				case NpgsqlDbType.Array | NpgsqlDbType.Circle:
					return "IEnumerable<NpgsqlCircle>";

				case NpgsqlDbType.Box:
					if (column.IsNullable)
						return "NpgsqlBox?";
					else
						return "NpgsqlBox";

				case NpgsqlDbType.Array | NpgsqlDbType.Box:
					return "IEnumerable<NpgsqlBox>";

				case NpgsqlDbType.Path:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.Array | NpgsqlDbType.Path:
					return "IEnumerable<IEnumerable<NpgsqlPoint>>";

				case NpgsqlDbType.Polygon:
					return "IEnumerable<NpgsqlPoint>";

				case NpgsqlDbType.Array | NpgsqlDbType.Polygon:
					return "IEnumerable<IEnumerable<NpgsqlPoint>>";

				case NpgsqlDbType.Bytea:
					return "IEnumerable<byte>";

				case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
					return "IEnumerable<IEnumerable<byte>>";

				case NpgsqlDbType.Text:
				case NpgsqlDbType.Citext:
					return "string";

				case NpgsqlDbType.Name:
					if (string.Equals(column.dbDataType, "_name", StringComparison.OrdinalIgnoreCase))
						return "IEnumerable<string>";
					else
						return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Text:
				case NpgsqlDbType.Array | NpgsqlDbType.Name:
				case NpgsqlDbType.Array | NpgsqlDbType.Citext:
					return "IEnumerable<string>";

				case NpgsqlDbType.Varchar:
				case NpgsqlDbType.Json:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
				case NpgsqlDbType.Array | NpgsqlDbType.Json:
					return "IEnumerable<string>";

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
						return "IEnumerable<char>";


				case NpgsqlDbType.Array | NpgsqlDbType.Char:
					return "IEnumerable<string>";

				case NpgsqlDbType.Uuid:
					if (column.IsNullable)
						return "Guid?";
					else
						return "Guid";

				case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
					return "IEnumerable<Guid>";

				case NpgsqlDbType.Date:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Date:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.TimeTz:
					if (column.IsNullable)
						return "DateTimeOffset?";
					else
						return "DateTimeOffset";

				case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
					return "IEnumerable<DateTimeOffset>";

				case NpgsqlDbType.Time:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Time:
					return "IEnumerable<TimeSpan>";

				case NpgsqlDbType.Interval:
					if (column.IsNullable)
						return "TimeSpan?";
					else
						return "TimeSpan";

				case NpgsqlDbType.Array | NpgsqlDbType.Interval:
					return "IEnumerable<TimeSpan>";

				case NpgsqlDbType.Timestamp:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.TimestampTz:
					if (column.IsNullable)
						return "DateTime?";
					else
						return "DateTime";

				case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
					return "IEnumerable<DateTime>";

				case NpgsqlDbType.Double:
					if (column.IsNullable)
						return "double?";
					else
						return "double";

				case NpgsqlDbType.Array | NpgsqlDbType.Double:
					return "IEnumerable<double>";

				case NpgsqlDbType.Real:
					if (column.IsNullable)
						return "float?";
					else
						return "float";

				case NpgsqlDbType.Array | NpgsqlDbType.Real:
					return "IEnumerable<float>";

				case NpgsqlDbType.Numeric:
				case NpgsqlDbType.Money:
					if (column.IsNullable)
						return "decimal?";
					else
						return "decimal";

				case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
				case NpgsqlDbType.Array | NpgsqlDbType.Money:
					return "IEnumerable<decimal>";

				case NpgsqlDbType.Xml:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Xml:
					return "IEnumerable<string>";

				case NpgsqlDbType.Jsonb:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.Jsonb:
					return "IEnumerable<string>";

				case NpgsqlDbType.JsonPath:
					return "string";

				case NpgsqlDbType.Array | NpgsqlDbType.JsonPath:
					return "IEnumerable<string>";

				case NpgsqlDbType.Inet:
					return "IPAddress";

				case NpgsqlDbType.Cidr:
					return "ValueTuple<IPAddress, int>";

				case NpgsqlDbType.Array | NpgsqlDbType.Inet:
					return "IEnumerable<IPAddress>";

				case NpgsqlDbType.Array | NpgsqlDbType.Cidr:
					return "IEnumerable<ValueTuple<IPAddress, int>>";

				case NpgsqlDbType.MacAddr:
				case NpgsqlDbType.MacAddr8:
					return "PhysicalAddress";

				case NpgsqlDbType.Array | NpgsqlDbType.MacAddr:
				case NpgsqlDbType.Array | NpgsqlDbType.MacAddr8:
					return "IEnumerable<PhysicalAddress>";

				case NpgsqlDbType.Unknown:
					{
						var entity = classList.FirstOrDefault(e =>
						{
							return e.GetType() == typeof(EntityClassFile) &&
							string.Equals(((EntityClassFile)e).TableName, column.dbDataType, StringComparison.OrdinalIgnoreCase);
						});

						if (entity != null)
							return entity.ClassName;
					}
					break;
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
					return "IEnumerable<byte>";

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
				case MySqlDbType.JSON:
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
					return "IEnumerable<byte>";

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

		#region Postgrsql Helper Functions
		public static ElementType GetElementType(string schema, string datatype, List<ClassFile> ClassList, string connectionString)
		{
			if (ClassList != null)
			{
				var classFile = ClassList.FirstOrDefault(c =>
					c.GetType() == typeof(EntityClassFile) &&
					string.Equals(((EntityClassFile)c).SchemaName, schema, StringComparison.OrdinalIgnoreCase) && 
					string.Equals(((EntityClassFile)c).TableName, datatype, StringComparison.OrdinalIgnoreCase));

				if (classFile != null)
					return classFile.ElementType;
			}

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
