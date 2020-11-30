using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace COFRSCoreInstaller
{
	public static class Utilities
	{
		public static List<ClassMember> LoadEntityClassMembers(string entityFileName, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}

				var match2 = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");

				if (match2.Success)
				{
					entityName = match2.Groups["column"].Value;

					var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;

					var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

					if (column != null)
					{
						column.EntityName = entityName;

						if (column.IsPrimaryKey)
						{
							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = "Href",
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else if (column.IsForeignKey)
						{
							string shortColumnName;

							if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
							{
								shortColumnName = column.ColumnName;
								if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
									shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
							}
							else
								shortColumnName = column.ForeignTableName;

							var normalizer = new NameNormalizer(shortColumnName);
							var domainName = normalizer.PluralForm;

							if (string.Equals(shortColumnName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
								domainName = normalizer.SingleForm;

							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = domainName,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else
						{
							var normalizer = new NameNormalizer(column.EntityName);
							var domainName = normalizer.PluralForm;

							if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
								domainName = normalizer.SingleForm;

							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								ClassMember potentialMember = null;

								potentialMember = members.FirstOrDefault(m => domainName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, domainName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) :  false);

								if (potentialMember != null)
								{
									var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, domainName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

									if (childMember != null)
										member = childMember;
								}
							}

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = domainName,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
					}
				}
			}

			return members;
		}

		public static List<ClassMember> LoadClassColumns(string resourceFileName, string entityFileName, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var resourceContent = File.ReadAllText(resourceFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in resourceContent)
			{
				var match = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");
				if (match.Success)
				{
					var memberName = match.Groups["column"].Value;
					var dataType = match.Groups["datatype"].Value;

					var member = new ClassMember()
					{
						ResourceMemberName = memberName,
						ResourceMemberType = dataType,
						EntityNames = new List<DBColumn>(),
						ChildMembers = new List<ClassMember>()
					};

					LoadChildMembers(Path.GetDirectoryName(resourceFileName), member);

					members.Add(member);
				}
			}

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}

				var match2 = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");

				if (match2.Success)
				{
					entityName = match2.Groups["column"].Value;

					var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;

					var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

					if (column != null)
					{
						column.EntityName = entityName;

						if (column.IsPrimaryKey)
						{
							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "href", StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else if (column.IsForeignKey)
						{
							string shortColumnName;

							if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
							{
								shortColumnName = column.ColumnName;
								if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
									shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
							}
							else
								shortColumnName = column.ForeignTableName;

							var normalizer = new NameNormalizer(shortColumnName);
							var domainName = normalizer.PluralForm;

							if (string.Equals(shortColumnName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
								domainName = normalizer.SingleForm;

							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
						else
						{
							var normalizer = new NameNormalizer(column.EntityName);
							var domainName = normalizer.PluralForm;

							if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
								domainName = normalizer.SingleForm;

							var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

							if (member == null)
							{
								var potentialMember = members.FirstOrDefault(m => domainName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, domainName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) : false);

								if (potentialMember != null)
								{
									var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, domainName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

									if (childMember != null)
										member = childMember;
								}
							}

							if (member == null)
							{
								member = new ClassMember()
								{
									ResourceMemberName = string.Empty,
									ResourceMemberType = string.Empty,
									EntityNames = new List<DBColumn>(),
									ChildMembers = new List<ClassMember>()
								};

								members.Add(member);
							}

							var entityColumn = new DBColumn()
							{
								EntityName = column.EntityName,
								EntityType = match2.Groups["datatype"].Value,
								ServerType = column.ServerType,
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

							SetFixed(column, entityColumn);
							member.EntityNames.Add(entityColumn);
						}
					}
				}
			}

			return members;
		}

		private static void SetFixed(DBColumn column, DBColumn entityColumn)
		{
			if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
					  (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
			{
				entityColumn.IsFixed = true;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
					  (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char))
			{
				entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
			{
				entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
			{
				entityColumn.IsFixed = true;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.MediumText) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.LongText) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.TinyText))
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
			{
				entityColumn.IsFixed = false;
			}
			else if ((column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
					 (column.ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					 (column.ServerType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
			{
				entityColumn.IsFixed = false;
			}
			else if (column.ServerType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
			{
				entityColumn.IsFixed = true;
			}
		}

		private static void LoadChildMembers(string folder, ClassMember member)
		{
			var fileName = FindFile(folder, member.ResourceMemberType + ".cs");

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				var childContent = File.ReadAllText(fileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in childContent)
				{
					var match = Regex.Match(line, "[ \\t]*public[ \\t]+(?<datatype>[^ \\t]+)[ \\t]+(?<column>[a-zA-Z0-9_]+)[ \\t]*\\{[ \\t]*get\\;[ \\t]*set\\;[ \\t]*\\}");
					if (match.Success)
					{
						var memberName = match.Groups["column"].Value;
						var dataType = match.Groups["datatype"].Value;

						var childMember = new ClassMember()
						{
							ResourceMemberName = memberName,
							ResourceMemberType = dataType,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						LoadChildMembers(folder, childMember);

						member.ChildMembers.Add(childMember);
					}
				}
			}
		}

		private static string FindFile(string folder, string fileName)
		{
			var fullPath = Path.Combine(folder, fileName);

			if (File.Exists(fullPath))
				return fullPath;

			foreach (var subfolder in Directory.GetDirectories(folder))
			{
				var childName = FindFile(subfolder, fileName);

				if (!string.IsNullOrWhiteSpace(childName))
					return childName;
			}

			return string.Empty;
		}

	}
}