using COFRS.Template.Common.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using VSLangProj;

namespace COFRS.Template.Common.ServiceUtilities
{
    public static class StandardUtils
    {
		public static JObject ConstructExample(DBServerType dbType, EntityClassFile classFile)
		{
			var values = new JObject();

			//	-----------------------------------------------------------------
			//	Read data from MySQL
			//	-----------------------------------------------------------------
			if (dbType == DBServerType.MYSQL)
			{
				return GetMySqlValues(values, classFile);
			}

			//	-----------------------------------------------------------------
			//	Read data from Postgresql
			//	-----------------------------------------------------------------
			else if (dbType == DBServerType.POSTGRESQL)
			{
				return GetPostgresqlValues(values, classFile);
			}

			//	-----------------------------------------------------------------
			//	Read data from SQL Server
			//	-----------------------------------------------------------------
			else
			{
				return GetSqlServerValues(values, classFile);
			}
		}

		private static JObject GetSqlServerValues(JObject values, EntityClassFile classFile)
		{
			foreach (var column in classFile.Columns)
			{
				var columnName = column.ColumnName;

				switch ((SqlDbType)column.DataType)
				{
					#region tinyint, smallint, int, bigint
					case SqlDbType.TinyInt:
						values.Add(columnName, JToken.FromObject((byte)1));
						break;

					case SqlDbType.SmallInt:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case SqlDbType.Int:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case SqlDbType.BigInt:
						values.Add(columnName, JToken.FromObject((long)1));
						break;
					#endregion

					#region varchar, nvarchar, text, ntext
					case SqlDbType.VarChar:
					case SqlDbType.NVarChar:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case SqlDbType.Text:
					case SqlDbType.NText:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;
					#endregion

					#region binary, varbinary
					case SqlDbType.Binary:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject((byte)32));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
							}
							else
							{
								var answer = new byte[column.Length];
								for (int i = 0; i < column.Length; i++)
								{
									var byteValue = (byte)(i & 0x00FF);
									answer[i] = byteValue;
								}

								values.Add(columnName, JToken.FromObject(answer));
							}
						}
						break;

					case SqlDbType.VarBinary:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region bit
					case SqlDbType.Bit:
						values.Add(columnName, JToken.FromObject(true));
						break;
					#endregion

					#region char, nchar
					case SqlDbType.NChar:
					case SqlDbType.Char:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject('A'));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
							}
							else
							{
								const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
								var chars = new char[column.Length];
								for (int i = 0; i < chars.Length; i++)
								{
									int j = i % alphabet.Length;
									chars[i] = alphabet[j];
								}

								values.Add(columnName, JToken.FromObject(new string(chars)));
							}
						}
						break;
					#endregion

					#region image
					case SqlDbType.Image:
						{
							var image = (Image)new Bitmap(100, 100);
							var imageConverter = new ImageConverter();

							var bytes = imageConverter.ConvertTo(image, typeof(byte[]));
							values.Add(columnName, JToken.FromObject(bytes));
						}
						break;
					#endregion

					case SqlDbType.Date: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTime2: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTimeOffset: values.Add(columnName, JToken.FromObject(DateTimeOffset.Now)); break;
					case SqlDbType.Decimal: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
					case SqlDbType.Float: values.Add(columnName, JToken.FromObject(Single.Parse("123.45"))); break;

					case SqlDbType.Money: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
					case SqlDbType.Real: values.Add(columnName, JToken.FromObject(Double.Parse("123.45"))); break;
					case SqlDbType.SmallDateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;


					case SqlDbType.SmallMoney: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;

					case SqlDbType.Time: values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(3))); break;
					case SqlDbType.Timestamp: values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })); break;
					case SqlDbType.UniqueIdentifier: values.Add(columnName, JToken.FromObject(Guid.NewGuid())); break;

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		private static JObject GetMySqlValues(JObject values, EntityClassFile classFile)
		{
			foreach (var column in classFile.Columns)
			{
				var columnName = column.ColumnName;

				switch ((MySqlDbType)column.DataType)
				{
					#region tinyint, smallint, int, bigint
					case MySqlDbType.Byte:
						values.Add(columnName, JToken.FromObject((sbyte)1));
						break;

					case MySqlDbType.UByte:
						values.Add(columnName, JToken.FromObject((byte)1));
						break;

					case MySqlDbType.Int16:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case MySqlDbType.UInt16:
						values.Add(columnName, JToken.FromObject((ushort)1));
						break;

					case MySqlDbType.Int24:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case MySqlDbType.UInt24:
						values.Add(columnName, JToken.FromObject((uint)1));
						break;

					case MySqlDbType.Int32:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case MySqlDbType.UInt32:
						values.Add(columnName, JToken.FromObject((uint)1));
						break;

					case MySqlDbType.Int64:
						values.Add(columnName, JToken.FromObject((long)1));
						break;

					case MySqlDbType.UInt64:
						values.Add(columnName, JToken.FromObject((ulong)1));
						break;
					#endregion

					#region decimal, double, float
					case MySqlDbType.Decimal:
						values.Add(columnName, JToken.FromObject((decimal)1.24m));
						break;
					case MySqlDbType.Double:
						values.Add(columnName, JToken.FromObject((double)1.24));
						break;
					case MySqlDbType.Float:
						values.Add(columnName, JToken.FromObject((float)1.24f));
						break;
					#endregion

					#region varchar, nvarchar, text, ntext
					case MySqlDbType.VarChar:
					case MySqlDbType.VarString:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case MySqlDbType.Text:
					case MySqlDbType.TinyText:
					case MySqlDbType.MediumText:
					case MySqlDbType.LongText:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;

					case MySqlDbType.String:
						if (column.Length == 1)
							values.Add(columnName, JToken.FromObject('A'));
						else
						{
							const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
							var chars = new char[column.Length];
							for (int i = 0; i < chars.Length; i++)
							{
								int j = i % alphabet.Length;
								chars[i] = alphabet[j];
							}

							values.Add(columnName, JToken.FromObject(new string(chars)));
						}
						break;

					#endregion

					#region binary, varbinary
					case MySqlDbType.Binary:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject((byte)32));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
							}
							else
							{
								var answer = new byte[column.Length];
								for (int i = 0; i < column.Length; i++)
								{
									var byteValue = (byte)(i & 0x00FF);
									answer[i] = byteValue;
								}

								values.Add(columnName, JToken.FromObject(answer));
							}
						}
						break;

					case MySqlDbType.VarBinary:
					case MySqlDbType.TinyBlob:
					case MySqlDbType.Blob:
					case MySqlDbType.MediumBlob:
					case MySqlDbType.LongBlob:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region bit
					case MySqlDbType.Bit:
						if (column.Length == 1)
							values.Add(columnName, JToken.FromObject(true));
						else
							values.Add(columnName, JToken.FromObject((ulong)1));
						break;
					#endregion

					#region enum, set
					case MySqlDbType.Enum:
						{
							var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
							values.Add(columnName, JToken.FromObject(theValues[1]));
						}
						break;

					case MySqlDbType.Set:
						{
							var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
							values.Add(columnName, JToken.FromObject(theValues[1]));
						}
						break;
					#endregion

					#region Datetime, timestamp, time, date, year
					case MySqlDbType.DateTime:
					case MySqlDbType.Date:
					case MySqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case MySqlDbType.Time:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(2)));
						break;

					case MySqlDbType.Year:
						values.Add(columnName, JToken.FromObject(2020));
						break;
					#endregion

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		private static JObject GetPostgresqlValues(JObject values, EntityClassFile classFile)
		{
			foreach (var column in classFile.Columns)
			{
				var columnName = column.ColumnName;

				switch ((NpgsqlDbType)column.DataType)
				{
					#region smallint, int, bigint
					case NpgsqlDbType.Smallint:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
						values.Add(columnName, JToken.FromObject(new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Integer:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						values.Add(columnName, JToken.FromObject(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Bigint:
						values.Add(columnName, JToken.FromObject((long)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						values.Add(columnName, JToken.FromObject(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region real, double, numeric
					case NpgsqlDbType.Real:
						values.Add(columnName, JToken.FromObject((float)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Real:
						values.Add(columnName, JToken.FromObject(new float[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
						break;

					case NpgsqlDbType.Double:
						values.Add(columnName, JToken.FromObject((double)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Double:
						values.Add(columnName, JToken.FromObject(new double[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
						break;

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						values.Add(columnName, JToken.FromObject((decimal)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
					case NpgsqlDbType.Array | NpgsqlDbType.Money:
						values.Add(columnName, JToken.FromObject(new decimal[] { 1.23m, 2.45m, 3.67m, 4.89m, 5.01m, 6.23m, 7.45m, 8.67m, 9.89m }));
						break;
					#endregion


					#region Guid
					case NpgsqlDbType.Uuid:
						values.Add(columnName, JToken.FromObject(Guid.NewGuid()));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
						values.Add(columnName, JToken.FromObject(new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }));
						break;
					#endregion

					#region json
					case NpgsqlDbType.Json:
						{
							var answer = "{ \"Name\": \"John\" }";
							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Json:
						{
							var theList = new List<string>();

							var answer1 = "{ \"Name\": \"John\" }";
							theList.Add(answer1);

							var answer2 = "{ \"Name\": \"Jane\" }";
							theList.Add(answer2);

							var answer3 = "{ \"Name\": \"Bill\" }";
							theList.Add(answer3);

							values.Add(columnName, JToken.FromObject(theList.ToArray()));
						}
						break;

					#endregion

					#region varchar, text
					case NpgsqlDbType.Varchar:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
						{
							var array = new JArray();
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cow mooed at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cat watched the dog and the cow.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							values.Add(columnName, array);
						}
						break;

					case NpgsqlDbType.Text:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Text:
						{
							var array = new JArray
							{
								new JValue("The dog barked at the moon"),
								new JValue("The cow mooed at the moon"),
								new JValue("The cat watched the dog and the cow.")
							};
							values.Add(columnName, array);
						}
						break;
					#endregion

					#region bytea
					case NpgsqlDbType.Bytea:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						{
							var array = new JArray
							{
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
							};
							values.Add(columnName, array);
						}
						break;
					#endregion

					#region bit, varbit
					case NpgsqlDbType.Bit:
						{
							if (column.Length == 1)
								values.Add(columnName, JToken.FromObject(true));
							else
							{
								var str = new StringBuilder();
								for (int i = 0; i < column.Length; i++)
									str.Append("1");

								values.Add(columnName, JToken.FromObject(str.ToString()));
							}
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bit:
						{
							if (column.Length == 1)
							{
								var array = new JArray
								{
									JToken.FromObject(true),
									JToken.FromObject(true),
									JToken.FromObject(false),
									JToken.FromObject(true)
								};
								values.Add(columnName, array);
							}
							else
							{
								var array = new JArray();

								for (int i = 0; i < 3; i++)
								{
									var str = new StringBuilder();
									for (int j = 0; j < column.Length; j++)
										str.Append("1");
									array.Add(JToken.FromObject(str.ToString()));
								}

								values.Add(columnName, array);
							}
						}
						break;

					case NpgsqlDbType.Varbit:
						{
							var str = new StringBuilder();
							for (int i = 0; i < column.Length && i < 10; i++)
								str.Append("1");

							values.Add(columnName, JToken.FromObject(str.ToString()));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
						{
							if (column.Length == 1)
							{
								var array = new JArray
								{
									JToken.FromObject(true),
									JToken.FromObject(true),
									JToken.FromObject(false),
									JToken.FromObject(true)
								};
								values.Add(columnName, array);
							}
							else
							{
								var array = new JArray();

								for (int i = 0; i < 3; i++)
								{
									var str = new StringBuilder();
									for (int j = 0; j < column.Length && j < 10; j++)
										str.Append("1");
									array.Add(JToken.FromObject(str.ToString()));
								}

								values.Add(columnName, array);
							}
						}
						break;
					#endregion

					#region char
					case NpgsqlDbType.Char:
						{
							if (string.Equals(column.dbDataType, "_char", StringComparison.OrdinalIgnoreCase))
							{
								values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
							}
							else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase))
							{
								values.Add(columnName, JToken.FromObject('A'));
							}
							else
							{
								const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
								var chars = new char[column.Length];
								for (int i = 0; i < chars.Length; i++)
								{
									int j = i % alphabet.Length;
									chars[i] = alphabet[j];
								}

								values.Add(columnName, JToken.FromObject(new string(chars)));
							}
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Char:
						{
							var array = new JArray();
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cow mooed at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cat watched the dog and the cow.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							values.Add(columnName, array);
						}
						break;
					#endregion

					#region Boolean
					case NpgsqlDbType.Boolean:
						values.Add(columnName, JToken.FromObject((bool)true));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
						values.Add(columnName, JToken.FromObject(new bool[] { true, true, false }));
						break;
					#endregion

					#region DateTime
					case NpgsqlDbType.Date:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Date:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.Time:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(10)));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Time:
						values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
						break;

					case NpgsqlDbType.Interval:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(15)));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Interval:
						values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
						break;

					case NpgsqlDbType.TimeTz:
						values.Add(columnName, JToken.FromObject(DateTimeOffset.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
						values.Add(columnName, JToken.FromObject(new DateTimeOffset[] { DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.TimestampTz:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;
					#endregion

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		public static string CorrectForReservedNames(string columnName)
		{
			if (string.Equals(columnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "as", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "base", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "bool", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "break", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "byte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "case", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "catch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "char", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "checked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "class", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "const", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "continue", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "default", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "do", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "double", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "else", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "enum", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "event", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "extern", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "false", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "finally", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "float", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "for", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "goto", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "if", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "in", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "int", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "interface", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "internal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "is", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "lock", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "long", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "new", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "null", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "object", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "operator", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "out", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "override", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "params", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "private", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "protected", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "public", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ref", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "return", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "short", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "static", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "string", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "struct", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "switch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "this", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "throw", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "true", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "try", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "uint", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "using", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "void", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "while", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "add", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "alias", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "async", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "await", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "by", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "descending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "equals", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "from", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "get", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "global", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "group", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "into", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "join", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "let", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "on", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "partial", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "remove", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "select", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "set", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "var", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "when", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "where", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "yield", StringComparison.OrdinalIgnoreCase))
			{
				return $"{columnName}_Value";
			}

			return columnName;
		}

		public static string NormalizeClassName(string className)
		{
			var normalizedName = new StringBuilder();
			var indexStart = 1;

			while (className.EndsWith("_") && className.Length > 1)
				className = className.Substring(0, className.Length - 1);

			while (className.StartsWith("_") && className.Length > 1)
				className = className.Substring(1);

			if (className == "_")
				return className;

			normalizedName.Append(className.Substring(0, 1).ToUpper());

			int index = className.IndexOf("_");

			while (index != -1)
			{
				//	0----*----1----*----2
				//	street_address_1

				normalizedName.Append(className.Substring(indexStart, index - indexStart));
				normalizedName.Append(className.Substring(index + 1, 1).ToUpper());
				indexStart = index + 2;

				if (indexStart >= className.Length)
					index = -1;
				else
					index = className.IndexOf("_", indexStart);
			}

			if (indexStart < className.Length)
				normalizedName.Append(className.Substring(indexStart));

			return normalizedName.ToString();
		}

		public static List<ClassFile> GenerateEntityClassList(List<ClassFile> UndefinedClassList, List<ClassFile> DefinedClassList, Dictionary<string, MemberInfo> Members, string baseFolder, string connectionString)
		{
			List<ClassFile> resultList = new List<ClassFile>();

			foreach (var classFile in UndefinedClassList)
			{
				var newClassFile = GenerateEntityClass((EntityClassFile) classFile, DefinedClassList, connectionString);
				resultList.Add(newClassFile);

				if (newClassFile.ElementType != ElementType.Enum)
				{
					foreach (var column in newClassFile.Columns)
					{
						if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
						{
							if (UndefinedClassList.FirstOrDefault(c => c.GetType() == typeof(EntityClassFile) && 
							                                      string.Equals(((EntityClassFile)c).TableName, column.EntityName, StringComparison.OrdinalIgnoreCase)) == null)
							{
								var aList = new List<ClassFile>();
								var bList = new List<ClassFile>();
								var className = CorrectForReservedNames(NormalizeClassName(column.EntityName));

								var elementType = DBHelper.GetElementType(((EntityClassFile)classFile).SchemaName, column.dbDataType, DefinedClassList, connectionString);

								var aClassFile = new EntityClassFile()
								{
									ClassName = className,
									TableName = column.dbDataType,
									SchemaName = ((EntityClassFile)classFile).SchemaName,
									FileName = Path.Combine(baseFolder, $"{className}.cs"),
									ClassNameSpace = classFile.ClassNameSpace,
									ElementType = elementType
								};

								aList.Add(aClassFile);
								bList.AddRange(DefinedClassList);
								bList.AddRange(UndefinedClassList);

								resultList.AddRange(GenerateEntityClassList(aList, bList, Members, baseFolder, connectionString));
							}
						}
					}
				}
			}

			return resultList;
		}

		private static EntityClassFile GenerateEntityClass(EntityClassFile classFile, List<ClassFile> definedClassList, string connectionString)
		{
			if (classFile.ElementType == ElementType.Enum)
				GenerateEnumColumns(connectionString, classFile);
			else
				GenerateColumns(connectionString, classFile, definedClassList);

			return classFile;
		}

		private static void GenerateEnumColumns(string connectionString, EntityClassFile classFile)
		{
			classFile.Columns = new List<DBColumn>();
			string query = @"
select e.enumlabel as enum_value
from pg_type t 
   join pg_enum e on t.oid = e.enumtypid  
   join pg_catalog.pg_namespace n ON n.oid = t.typnamespace
where t.typname = @dataType
  and n.nspname = @schema";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", classFile.TableName);
					command.Parameters.AddWithValue("@schema", classFile.SchemaName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var element = reader.GetString(0);
							var elementName = StandardUtils.NormalizeClassName(element);

							var column = new DBColumn()
							{
								ColumnName = elementName,
								EntityName = element
							};

							classFile.Columns.Add(column);
						}
					}
				}
			}
		}

		private static void GenerateColumns(string connectionString, EntityClassFile classFile, List<ClassFile> definedClassList)
		{
			if (classFile.Columns == null || classFile.Columns.Count() == 0)
			{
				classFile.Columns = new List<DBColumn>();

				using (var connection = new NpgsqlConnection(connectionString))
				{
					connection.Open();

					var query = @"
select a.attname as columnname,
	   t.typname as datatype,
	   case when t.typname = 'varchar' then a.atttypmod-4
	        when t.typname = 'bpchar' then a.atttypmod-4
			when t.typname = '_varchar' then a.atttypmod-4
			when t.typname = '_bpchar' then a.atttypmod-4
	        when a.atttypmod > -1 then a.atttypmod
	        else a.attlen end as max_len,
	   not a.attnotnull as is_nullable,

	   case when ( a.attgenerated = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_computed,

	   case when ( a.attidentity = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_identity,

	   case when (select indrelid from pg_index as px where px.indisprimary = true and px.indrelid = c.oid and a.attnum = ANY(px.indkey)) = c.oid then true else false end as is_primary,
	   case when (select indrelid from pg_index as ix where ix.indrelid = c.oid and a.attnum = ANY(ix.indkey)) = c.oid then true else false end as is_indexed,
	   case when (select conrelid from pg_constraint as cx where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) = c.oid then true else false end as is_foreignkey,
       (  select cc.relname from pg_constraint as cx inner join pg_class as cc on cc.oid = cx.confrelid where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) as foeigntablename
  from pg_class as c
  inner join pg_namespace as ns on ns.oid = c.relnamespace
  inner join pg_attribute as a on a.attrelid = c.oid and not a.attisdropped and attnum > 0
  inner join pg_type as t on t.oid = a.atttypid
  left outer join pg_attrdef as ad on ad.adrelid = a.attrelid and ad.adnum = a.attnum 
  where ns.nspname = @schema
    and c.relname = @tablename
 order by a.attnum
";

					using (var command = new NpgsqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@schema", classFile.SchemaName);
						command.Parameters.AddWithValue("@tablename", classFile.TableName);

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var dbColumn = new DBColumn
								{
									EntityName = reader.GetString(0),
									ColumnName = CorrectForReservedNames(NormalizeClassName(reader.GetString(0))),
									DataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1)),
									dbDataType = reader.GetString(1),
									Length = Convert.ToInt64(reader.GetValue(2)),
									IsNullable = Convert.ToBoolean(reader.GetValue(3)),
									IsComputed = Convert.ToBoolean(reader.GetValue(4)),
									IsIdentity = Convert.ToBoolean(reader.GetValue(5)),
									IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6)),
									IsIndexed = Convert.ToBoolean(reader.GetValue(7)),
									IsForeignKey = Convert.ToBoolean(reader.GetValue(8)),
									ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
								};

								classFile.Columns.Add(dbColumn);
							}
						}
					}
				}
			}
		}

		#region Solution functions
		/// <summary>
		/// Loads all the entity models in a solution
		/// </summary>
		/// <param name="solution">The open solution</param>
		/// <returns>The list of <see cref="MemberInfo"/> objects that describe all members in the solution</returns>
		public static Dictionary<string, MemberInfo> LoadProgramDetail(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Dictionary<string, MemberInfo> members = new Dictionary<string, MemberInfo>();

			foreach (Project project in solution.Projects)
			{
				if (project.Kind == PrjKind.prjKindCSharpProject)
				{
					foreach (var element in ScanProject(project.ProjectItems))
						members.Add(element.Key, element.Value);
				}
			}

			return members;
		}

		/// <summary>
		/// Loads all the entity models in a solution
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static List<ClassFile> LoadClassList(Dictionary<string, MemberInfo> members)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var classList = new List<ClassFile>();

			foreach (var item in members)
			{
				if ( item.Value.ElementType == ElementType.Composite ||
					 item.Value.ElementType == ElementType.Enum)
				{
					classList.AddRange(LoadClassFile(item.Value, classList, members));
				}
			}

			foreach (var item in members)
			{
				if (item.Value.ElementType == ElementType.Table )
					classList.AddRange(LoadClassFile(item.Value, classList, members));
			}

			foreach (var item in members)
			{
				if (item.Value.ElementType == ElementType.Resource)
					classList.AddRange(LoadClassFile(item.Value, classList, members));
			}

			return classList;
		}

		/// <summary>
		/// Loads all the entity models in a project
		/// </summary>
		/// <param name="projectItems"></param>
		/// <returns></returns>
		private static Dictionary<string, MemberInfo> ScanProject(ProjectItems projectItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var results = new Dictionary<string, MemberInfo>();

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
					projectItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
				{
					foreach (var element in ScanProject(projectItem.ProjectItems))
						results.Add(element.Key, element.Value);
				}
				else if (projectItem.Kind == Constants.vsProjectItemKindPhysicalFile &&
					     projectItem.FileCodeModel != null &&
						 projectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp &&
						 Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value) == 1 )
				{
					foreach (var element in ScanClassFile(projectItem))
						results.Add(element.Key, element.Value);
				}
			}

			return results;
		}

		private static Dictionary<string, MemberInfo> ScanClassFile(ProjectItem projectItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new Dictionary<string, MemberInfo>();

			foreach ( CodeElement element in projectItem.FileCodeModel.CodeElements)
            {
				if (element.Kind == vsCMElement.vsCMElementNamespace)
				{
					var namespaceElement = (CodeNamespace)element;

					foreach ( CodeElement childElement in namespaceElement.Members)
					{
						if (childElement.Kind == vsCMElement.vsCMElementClass)
						{
							CodeAttribute tableAttribute = null;
							CodeAttribute compositeAttribute = null;
							CodeAttribute entityAttribute = null;

							try { tableAttribute = (CodeAttribute) childElement.Children.Item("Table"); } catch (Exception) { }
							try { compositeAttribute = (CodeAttribute) childElement.Children.Item("Composite"); } catch (Exception) { }
							try { entityAttribute = (CodeAttribute) childElement.Children.Item("Entity"); } catch (Exception) { }

							if (tableAttribute != null)
							{
								var entityName = string.Empty;

								var match = Regex.Match(tableAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
								}

								var item = new MemberInfo
								{
									ClassName = childElement.Name,
									EntityName = entityName,
									ElementType = ElementType.Table,
									Namespace = (CodeElement) namespaceElement,
									Member = childElement
								};

								results.Add(item.EntityName, item);
							}
							else if (compositeAttribute != null)
							{
								var entityName = string.Empty;
								var match = Regex.Match(compositeAttribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
								}

								var item = new MemberInfo
								{
									ClassName = childElement.Name,
									EntityName = entityName,
									ElementType = ElementType.Composite,
									Namespace = (CodeElement) namespaceElement,
									Member = childElement
								};

								results.Add(item.EntityName, item);
							}
							else if (entityAttribute != null)
							{
								var entityName = string.Empty;

								var match = Regex.Match(entityAttribute.Value, "\"(?<entityName>[A-Za-z][A-Za-z0-9_]*)\"");

								if (match.Success)
								{
									entityName = match.Groups["entityName"].Value;
								}

								var item = new MemberInfo
								{
									ClassName = childElement.Name,
									EntityName = entityName,
									ElementType = ElementType.Resource,
									Namespace = (CodeElement)namespaceElement,
									Member = childElement
								};

								results.Add(item.EntityName, item);
							}
						}
						else if (childElement.Kind == vsCMElement.vsCMElementEnum)
						{
							CodeAttribute attributeElement = null;
							
							try { attributeElement = (CodeAttribute) childElement.Children.Item("PgEnum"); } catch (Exception) { }

							if (attributeElement != null)
							{
								var entityName = string.Empty;

								var match = Regex.Match(attributeElement.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

								if (match.Success)
								{
									entityName = match.Groups["tableName"].Value;
								}

								var item = new MemberInfo
								{
									ClassName = childElement.Name,
									EntityName = entityName,
									ElementType = ElementType.Enum,
									Namespace = (CodeElement)namespaceElement,
									Member = childElement
								};

								results.Add(item.EntityName, item);
							}
						}
					}
				}
            }

			return results;
		}

		/// <summary>
		/// Load all entity models in a file
		/// </summary>
		/// <param name="projectItem"></param>
		/// <returns></returns>
		private static List<ClassFile> LoadClassFile(MemberInfo member, List<ClassFile> classFiles, Dictionary<string, MemberInfo> members)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			
			var results = new List<ClassFile>();

			var entity = new EntityClassFile
			{
				ClassName = member.ClassName,
				ClassNameSpace = member.Namespace.Name,
				ElementType = member.ElementType,
				FileName = member.Member.ProjectItem.FileNames[0]
			};


			if (member.ElementType == ElementType.Composite)
			{
				CodeElement attribute = null;

				try { attribute = member.Member.Children.Item("PgComposite"); } catch (Exception) { }

				if (attribute != null)
				{
					bool wasOpen = attribute.ProjectItem.IsOpen[Constants.vsViewKindAny];
					attribute.ProjectItem.Open(Constants.vsViewKindCode);
					Document doc = attribute.ProjectItem.Document;
					TextSelection sel = doc.Selection as TextSelection;

					VirtualPoint activePoint = sel.ActivePoint;
					VirtualPoint anchorPoint = sel.AnchorPoint;

					sel.MoveToPoint(attribute.StartPoint);
					sel.SelectLine();

					var match = Regex.Match(sel.Text, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

					if (match.Success)
					{
						entity.ServerType = DBServerType.POSTGRESQL;
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;
						entity.Columns = new List<DBColumn>();

						foreach (CodeElement property in member.Member.Children)
						{
							if (property.Kind == vsCMElement.vsCMElementProperty)
							{
								CodeElement memberAttribute = null;

								sel.MoveToPoint(property.StartPoint);
								sel.SelectLine();
								var whitespace = "[ \\t]*";
								var space = "[ \\t]+";
								var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
								var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
								var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
								var typedecl = $"{variableName}(({singletype})|({multitype}))*";
								var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
								var match2 = Regex.Match(sel.Text, pattern);

								if (match2.Success)
								{
									sel.MoveToPoint(memberAttribute.StartPoint);
									sel.SelectLine();

									bool isPrimaryKey = false;
									bool isAutoField = false;
									bool isIdentity = false;
									bool isIndexed = false;
									bool isForeignKey = false;
									bool isNullable = false;
									bool isFixed = false;
									string nativeDataType = string.Empty;
									long dataLength = 0;
									int precision = 0;
									int scale = 0;
									string entityName = string.Empty;

									var className = match2.Groups["columnname"].Value;

									if (string.IsNullOrWhiteSpace(entityName))
										entityName = className;

									var entityColumn = new DBColumn()
									{
										ColumnName = className,
										EntityName = entityName,
										EntityType = match2.Groups["datatype"].Value,
										IsIdentity = isIdentity,
										IsPrimaryKey = isPrimaryKey,
										IsComputed = isAutoField,
										IsIndexed = isIndexed,
										IsForeignKey = isForeignKey,
										IsNullable = isNullable,
										IsFixed = isFixed,
										dbDataType = nativeDataType,
										Length = dataLength,
										NumericPrecision = precision,
										NumericScale = scale,
									};

									entityColumn.DataType = DBHelper.ConvertPostgresqlDataType(entityColumn.dbDataType);

									entity.Columns.Add(entityColumn);
								}
							}
						}
					}

					if (wasOpen)
					{
						sel.MoveToPoint(anchorPoint);
						sel.SwapAnchor();
						sel.MoveToPoint(activePoint);
					}
				}
			}
			else if (member.ElementType == ElementType.Table)
			{
				try
				{
					CodeAttribute attribute = (CodeAttribute)member.Member.Children.Item("Table");
					var match = Regex.Match(attribute.Value, "\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

					if (match.Success)
					{
						entity.ServerType = DBServerType.POSTGRESQL;
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;
						entity.ServerType = (DBServerType)Enum.Parse(typeof(DBServerType), match.Groups["dbtype"].Value);
						entity.Columns = new List<DBColumn>();

						foreach (CodeElement element in member.Member.Children)
						{
							if (element.Kind == vsCMElement.vsCMElementProperty)
							{
								CodeProperty property = (CodeProperty)element;
								CodeAttribute memberAttribute = (CodeAttribute)property.Children.Item("Member");

								var strCodeTypeParts = property.Type.AsString.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
								var dataType = strCodeTypeParts[strCodeTypeParts.Length - 1];

                                bool isPrimaryKey = false;
                                bool isAutoField = false;
                                bool isIdentity = false;
                                bool isIndexed = false;
                                bool isForeignKey = false;
                                bool isNullable = false;
                                bool isFixed = false;
                                string nativeDataType = string.Empty;
                                long dataLength = 0;
                                int precision = 0;
                                int scale = 0;
                                string entityName = string.Empty;

                                var match3 = Regex.Match(memberAttribute.Value, "IsPrimaryKey[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isPrimaryKey = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "AutoField[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isAutoField = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "IsIdentity[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isIdentity = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "IsIndexed[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isIndexed = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "IsForeignKey[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isForeignKey = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "IsNullable[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isNullable = bool.Parse(match3.Groups["boolValue"].Value);

                                match3 = Regex.Match(memberAttribute.Value, "IsFixed[ \t]*=[ \t]*(?<boolValue>true|false)");

                                if (match3.Success)
                                    isFixed = bool.Parse(match3.Groups["boolValue"].Value);

								var whitespace = "[ \\t]*";
								var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
								var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
								var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
								var typedecl = $"{variableName}(({singletype})|({multitype}))*";

								match3 = Regex.Match(memberAttribute.Value, $"NativeDataType[ \t]*=[ \t]*\"(?<nativeType>{typedecl})\"");

                                if (match3.Success)
                                    nativeDataType = match3.Groups["nativeType"].Value;

                                match3 = Regex.Match(memberAttribute.Value, $"Length[ \t]*=[ \t]*(?<length>[0-9]+)");

                                if (match3.Success)
                                    dataLength = Convert.ToInt64(match3.Groups["length"].Value);

                                match3 = Regex.Match(memberAttribute.Value, $"Precision[ \t]*=[ \t]*(?<precision>[0-9]+)");

                                if (match3.Success)
                                    precision = Convert.ToInt32(match3.Groups["precision"].Value);

                                match3 = Regex.Match(memberAttribute.Value, $"Scale[ \t]*=[ \t]*(?<scale>[0-9]+)");

                                if (match3.Success)
                                    scale = Convert.ToInt32(match3.Groups["scale"].Value);

                                match3 = Regex.Match(memberAttribute.Value, $"ColumnName[ \t]*=[ \t]*(?<entityName>[_a-zA-Z][_a-zA-Z0-9]*)");

                                if (match3.Success)
                                    entityName = match3.Groups["entityName"].Value;

								var className = property.Name;

                                if (string.IsNullOrWhiteSpace(entityName))
                                    entityName = className;

                                var entityColumn = new DBColumn()
                                {
                                    ColumnName = className,
                                    EntityName = entityName,
                                    EntityType = dataType,
                                    IsIdentity = isIdentity,
                                    IsPrimaryKey = isPrimaryKey,
                                    IsComputed = isAutoField,
                                    IsIndexed = isIndexed,
                                    IsForeignKey = isForeignKey,
                                    IsNullable = isNullable,
                                    IsFixed = isFixed,
                                    dbDataType = nativeDataType,
                                    Length = dataLength,
                                    NumericPrecision = precision,
                                    NumericScale = scale,
                                };

                                if (entity.ServerType == DBServerType.MYSQL)
                                    entityColumn.DataType = DBHelper.ConvertMySqlDataType(entityColumn.dbDataType);
                                else if (entity.ServerType == DBServerType.POSTGRESQL)
                                    entityColumn.DataType = DBHelper.ConvertPostgresqlDataType(entityColumn.dbDataType);
                                else if (entity.ServerType == DBServerType.SQLSERVER)
                                    entityColumn.DataType = DBHelper.ConvertSqlServerDataType(entityColumn.dbDataType);

                                entity.Columns.Add(entityColumn);
                            }
                        }
					}
				
					results.Add(entity);
				}
				catch (Exception) { }
			}
			else if (member.ElementType == ElementType.Resource)
			{
				var attribute = member.Member.Children.Item("PgComposite");

				bool wasOpen = attribute.ProjectItem.IsOpen[Constants.vsViewKindAny];
				attribute.ProjectItem.Open(Constants.vsViewKindCode);
				Document doc = attribute.ProjectItem.Document;
				TextSelection sel = doc.Selection as TextSelection;

				VirtualPoint activePoint = sel.ActivePoint;
				VirtualPoint anchorPoint = sel.AnchorPoint;

				sel.MoveToPoint(attribute.StartPoint);
				sel.SelectLine();

				var match = Regex.Match(sel.Text, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

				if (match.Success)
				{
					entity.ServerType = DBServerType.POSTGRESQL;
					entity.TableName = match.Groups["tableName"].Value;
					entity.SchemaName = match.Groups["schemaName"].Value;
					entity.Columns = new List<DBColumn>();

					foreach (CodeElement property in member.Member.Children)
					{
						if (property.Kind == vsCMElement.vsCMElementProperty)
						{
							var column = new DBColumn
							{
								ColumnName = property.Name
							};

							entity.Columns.Add(column);
						}
					}
				}

				if (wasOpen)
				{
					sel.MoveToPoint(anchorPoint);
					sel.SwapAnchor();
					sel.MoveToPoint(activePoint);
				}
			}
			else if (member.ElementType == ElementType.Enum)
			{
				try
				{
					var attribute = (CodeAttribute) member.Member.Children.Item("PgEnum"); 
					var match = Regex.Match(attribute.Value, "\\\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}");

					if (match.Success)
					{
						entity.ServerType = DBServerType.POSTGRESQL;
						entity.TableName = match.Groups["tableName"].Value;
						entity.SchemaName = match.Groups["schemaName"].Value;
						entity.Columns = new List<DBColumn>();

						foreach (CodeElement property in member.Member.Children)
						{
							if (property.Kind == vsCMElement.vsCMElementVariable)
							{
								CodeAttribute pgNameAttribute = (CodeAttribute)property.Children.Item("PgName");
								var match2 = Regex.Match(pgNameAttribute.Value, "\\\"(?<entityName>[A-Za-z][A-Za-z0-9_]*)\"");

								if (match2.Success)
								{
									var column = new DBColumn
									{
										ColumnName = property.Name,
										EntityName = match2.Groups["entityName"].Value
									};

									entity.Columns.Add(column);
								}
							}
						}

						results.Add(entity);
					}
				}
				catch (Exception) { }
			}

			return results;
		
		}
		
		private static void LoadChildMembers(DBServerType dbType, ClassMember member, List<ClassFile> classFiles)
		{
			string memberProperName = string.Empty;

			if (member.ResourceMemberType.Contains("<"))
				return;

			if (member.ResourceMemberType.Contains(">"))
				return;

			if (member.ResourceMemberType.EndsWith("?"))
				memberProperName = member.ResourceMemberType.Substring(0, member.ResourceMemberType.Length - 1);
			else
				memberProperName = member.ResourceMemberType;

			var childClass = classFiles.FirstOrDefault(c => string.Equals(c.ClassName, memberProperName, StringComparison.OrdinalIgnoreCase));

			if (childClass != null )
			{
				var entityClass = childClass as EntityClassFile;

				foreach ( var column in entityClass.Columns )
				{
					var memberName = column.ColumnName;
					var dataType = "Unknown";

					if (dbType == DBServerType.MYSQL)
						dataType = DBHelper.GetMySqlDataType(column);
					else if (dbType == DBServerType.POSTGRESQL)
						dataType = DBHelper.GetPostgresDataType(column, classFiles);
					else if (dbType == DBServerType.SQLSERVER)
						dataType = DBHelper.GetSQLServerDataType(column);

					var childMember = new ClassMember()
					{
						ResourceMemberName = memberName,
						ResourceMemberType = dataType,
						EntityNames = new List<DBColumn>(),
						ChildMembers = new List<ClassMember>()
					};

					LoadChildMembers(dbType, childMember, classFiles);

					member.ChildMembers.Add(childMember);
				}
			}
		}

		public static List<string> LoadPolicies(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<string>();
			var appSettings = FindProjectItem(solution, "appSettings.json");

			var wasOpen = appSettings.IsOpen[EnvDTE.Constants.vsViewKindAny];

			if (!wasOpen)
				appSettings.Open(EnvDTE.Constants.vsViewKindTextView);

			var doc = appSettings.Document;
			var sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sel.Text)))
			{
				using (var textReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(textReader))
					{
						var jsonConfig = JObject.Load(reader, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });

						if (jsonConfig["OAuth2"] == null)
							return null;

						var oAuth2Settings = jsonConfig["OAuth2"].Value<JObject>();

						if (oAuth2Settings["Policies"] == null)
							return null;

						var policyArray = oAuth2Settings["Policies"].Value<JArray>();

						foreach (var policy in policyArray)
							results.Add(policy["Policy"].Value<string>());
					}
				}
			}

			if (!wasOpen)
				doc.Close();

			return results;
		}

		/// <summary>
		/// Checks to see if the candidate namespace is the root namespace of the startup project
		/// </summary>
		/// <param name="solution">The solution</param>
		/// <param name="candidateNamespace">The candidate namesapce</param>
		/// <returns><see langword="true"/> if the candidate namespace is the root namespace of the startup project; <see langword="false"/> otherwise</returns>
		public static bool IsRootNamespace(Solution solution, string candidateNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				try
				{
					var projectNamespace = project.Properties.Item("RootNamespace").Value.ToString();

					if (string.Equals(candidateNamespace, projectNamespace, StringComparison.OrdinalIgnoreCase))
						return true;
				}
				catch (ArgumentException) { }
			}

			return false;
		}

		/// <summary>
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindEntityModelsFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var entityModelsFolder = FindProjectFolder(solution, "EntityModels");

			if (entityModelsFolder != null)
				return entityModelsFolder;

			var modelsFolder = FindProjectItem(solution, "Models");

			if (modelsFolder != null)
			{
				modelsFolder.ProjectItems.AddFolder("EntityModels");
				return FindProjectFolder(solution, "EntityModels");
			}

			Project project = solution.Projects.Item(0);

			modelsFolder = project.ProjectItems.AddFolder("Models");
			modelsFolder.ProjectItems.AddFolder("EntityModels");
			return FindProjectFolder(solution, "EntityModels");
		}

		public static ProjectItem FindProjectItem(Solution solution, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
					{
						return projectItem;
					}

					var candidate = FindProjectItem(projectItem, itemName);

					if (candidate != null)
						return candidate;
				}
			}
			return null;
		}

		public static ProjectItem FindProjectItem(ProjectItem parent, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in parent.ProjectItems)
			{
				if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
				{
					return projectItem;
				}

				var candidate = FindProjectItem(projectItem, itemName);

				if (candidate != null)
					return candidate;
			}

			return null;
		}

		public static ProjectFolder FindProjectFolder(Solution solution, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				var projectNamespace = project.Properties.Item("RootNamespace").Value.ToString();
				var projectName = project.Name;

				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
					{
						var folderNamespace = $"{projectNamespace}.{projectItem.Name}";

						if (string.Equals(projectItem.Name, folderName, StringComparison.OrdinalIgnoreCase))
						{
							var folder = new ProjectFolder { ProjectName = projectName, Namespace = folderNamespace, Folder = projectItem.FileNames[0] };
							return folder;
						}

						var candidate = FindProjectFolder(folderNamespace, projectItem, folderName);

						if (candidate != null)
						{
							candidate.ProjectName = projectName;
							return candidate;
						}
					}
				}
			}
			return null;
		}

		private static ProjectFolder FindProjectFolder(string projectNamespace, ProjectItem projectItem, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem child in projectItem.ProjectItems)
			{
				if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
				{
					var folderNamespace = $"{projectNamespace}.{child.Name}";

					if (string.Equals(child.Name, folderName, StringComparison.OrdinalIgnoreCase))
					{
						var folder = new ProjectFolder { Namespace = folderNamespace, Folder = child.FileNames[0] };
						return folder;
					}

					var candidate = FindProjectFolder(folderNamespace, child, folderName);

					if (candidate != null)
						return candidate;
				}
			}

			return null;
		}

		public static string LoadPolicy(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var window = settingsFile.Open(EnvDTE.Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			var lines = sel.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
				if (match.Success)
					return match.Groups["policy"].Value;
			}

			window.Close();

			return string.Empty;
		}

		public static string LoadMoniker(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var window = settingsFile.Open(EnvDTE.Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;
			string moniker = string.Empty;

			sel.StartOfDocument();
			if (sel.FindText("CompanyName"))
			{
				sel.SelectLine();

				var match = Regex.Match(sel.Text, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");

				if (match.Success)
					moniker = match.Groups["moniker"].Value;
			}

			window.Close();

			return moniker;
		}

		public static string GetConnectionString(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;

			ProjectItem settingsFile = _appObject.Solution.FindProjectItem("appsettings.Local.json");

			var wasOpen = settingsFile.IsOpen[EnvDTE.Constants.vsViewKindAny];
			Window window = settingsFile.Open(EnvDTE.Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = doc.Selection as TextSelection;

			VirtualPoint activePoint = sel.ActivePoint;
			VirtualPoint anchorPoint = sel.AnchorPoint;

			var activeLine = activePoint.Line;
			var activeColumn = activePoint.DisplayColumn;

			var anchorLine = anchorPoint.Line;
			var anchorColumn = anchorPoint.DisplayColumn;

			sel.SelectAll();
			var settings = JObject.Parse(sel.Text);
			var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
			string connectionString = connectionStrings["DefaultConnection"].Value<string>();

			if (!wasOpen)
				window.Close();
			else
			{
				sel.Mode = vsSelectionMode.vsSelectionModeStream;
				sel.MoveToPoint(anchorPoint);
				sel.SwapAnchor();
				sel.MoveToPoint(activePoint);
			}

			return connectionString;
		}

		public static void ReplaceConnectionString(Solution solution, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach ( Project project in solution.Projects )
            {
				ConfigurationManager configurationManager = project.ConfigurationManager;

				var names = configurationManager.ConfigurationRowNames;
            }



			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appsettings.local.json");

			var window = settingsFile.Open(EnvDTE.Constants.vsViewKindTextView);
			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();

			if (sel.FindText("Server=developmentdb;Database=master;Trusted_Connection=True;"))
			{
				sel.SelectLine();
				sel.Text = $"\t\t\"DefaultConnection\": \"{connectionString}\"";
				doc.Save();
			}

			window.Close();
		}

		public static void RegisterValidationModel(Solution solution, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasValidationUsing = sel.FindText($"using {validationNamespace}");

			if (!hasValidationUsing)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");
				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				sel.NewLine();
				sel.Insert($"using {validationNamespace};");
			}

			if (!sel.FindText($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("services.InitializeFactories();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddTransientWithParameters<IServiceOrchestrator"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Validators");
					sel.NewLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
			}

			doc.Save();
			window.Close();
		}

		public static void RegisterComposite(Solution solution, ClassFile classFile)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (classFile.ElementType == ElementType.Undefined || 
				classFile.ElementType == ElementType.Table ||
				classFile.ElementType == ElementType.Resource)
				return;

			var entityFile = (EntityClassFile)classFile;

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var window = serviceConfig.Open(EnvDTE.Constants.vsViewKindTextView);
			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasNpgsql = sel.FindText($"using Npgsql;");

			sel.StartOfDocument();
			var hasClassNamespace = sel.FindText($"using {entityFile.ClassNameSpace};");

			if (!hasNpgsql || !hasClassNamespace)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");

				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				if (!hasNpgsql)
				{
					sel.NewLine();
					sel.Insert($"using Npgsql;");
				}

				if (!hasClassNamespace)
				{
					sel.NewLine();
					sel.Insert($"using {entityFile.ClassNameSpace};");
				}
			}

			if (!sel.FindText($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityFile.ClassName}>(\"{entityFile.TableName}\");", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("var myAssembly = Assembly.GetExecutingAssembly();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddSingleton<IRepositoryOptions>(RepositoryOptions);"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Postgresql Composits and Enums");
					sel.NewLine();
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{entityFile.ClassName}>(\"{entityFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityFile.ClassName}>(\"{entityFile.TableName}\");");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{entityFile.ClassName}>(\"{entityFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{entityFile.ClassName}>(\"{entityFile.TableName}\");");
					sel.NewLine();
				}
			}

			doc.Save();
			window.Close();
		}

		public static ProjectItem GetProjectItem(Solution solution, string name)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			var theItem = (ProjectItem)DBHelper._cache.Get($"ProjectItem_{name}");

			if (theItem == null)
			{
				foreach (Project project in solution.Projects)
				{
					theItem = GetProjectItem(project.ProjectItems, name);

					if (theItem != null)
					{
						DBHelper._cache.Set(new CacheItem($"ProjectItem_{name}", theItem), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) });
						return theItem;
					}
				}
			}

			return theItem;
		}

		public static ProjectItem GetProjectItem(ProjectItems items, string name)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in items)
			{
				if (string.Equals(projectItem.Name, name, StringComparison.OrdinalIgnoreCase))
					return projectItem;

				var theChildItem = GetProjectItem(projectItem.ProjectItems, name);

				if (theChildItem != null)
					return theChildItem;
			}

			return null;
		}

		public static List<ClassMember> LoadEntityClassMembers(EntityClassFile entityClass)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			foreach (var column in entityClass.Columns)
			{
				if (column.IsPrimaryKey)
				{
					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = "Href",
							ResourceMemberType = "Uri",
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						EntityType = column.dbDataType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
				}
				else if (column.IsForeignKey)
				{
					string shortColumnName;

					if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
					{
						shortColumnName = CorrectForReservedNames(NormalizeClassName(column.ColumnName));
						if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
							shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
					}
					else
						shortColumnName = column.ForeignTableName;

					var normalizer = new NameNormalizer(shortColumnName);
					var domainName = normalizer.SingleForm;

					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = domainName,
							ResourceMemberType = "Uri",
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						EntityType = column.dbDataType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
				}
				else
				{
					var normalizer = new NameNormalizer(column.EntityName);
					var resourceName = normalizer.PluralForm;

					if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
						resourceName = normalizer.SingleForm;

					var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, resourceName, StringComparison.OrdinalIgnoreCase));

					if (member == null)
					{
						ClassMember potentialMember = null;

						potentialMember = members.FirstOrDefault(m => resourceName.Length > m.ResourceMemberName.Length && string.Equals(m.ResourceMemberName, resourceName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

						if (potentialMember != null)
						{
							var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, resourceName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

							if (childMember != null)
								member = childMember;
						}
					}

					if (member == null)
					{
						member = new ClassMember()
						{
							ResourceMemberName = resourceName,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						members.Add(member);
					}

					var entityColumn = new DBColumn()
					{
						EntityName = column.EntityName,
						EntityType = column.dbDataType,
						ColumnName = column.ColumnName,
						DataType = column.DataType,
						dbDataType = column.dbDataType,
						ForeignTableName = column.ForeignTableName,
						IsComputed = column.IsComputed,
						IsForeignKey = column.IsForeignKey,
						IsIdentity = column.IsIdentity,
						IsIndexed = column.IsIndexed,
						IsNullable = column.IsNullable,
						IsPrimaryKey = column.IsPrimaryKey,
						Length = column.Length
					};

					SetFixed(entityClass.ServerType, column, entityColumn);
					member.EntityNames.Add(entityColumn);
				}
			}

			return members;
		}

		private static void SetFixed(DBServerType serverType, DBColumn column, DBColumn entityColumn)
		{
			if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
					  (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
					  (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char))
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.MediumText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.LongText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.TinyText))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
			{
				entityColumn.IsFixed = false;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
			{
				entityColumn.IsFixed = true;
			}
		}
		#endregion
	}
}
