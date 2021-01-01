using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public class ResourceWizard : IWizard
	{
		private bool Proceed = false;

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
			var rootNamespace = replacementsDictionary["$rootnamespace$"];

			var namespaceParts = rootNamespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			var filePath = solutionDirectory;

			for (int i = 0; i < namespaceParts.Length; i++)
			{
				if (i == 0)
				{
					var candidate = Path.Combine(filePath, namespaceParts[i]);

					if (Directory.Exists(candidate))
						filePath = candidate;
				}
				else
					filePath = Path.Combine(filePath, namespaceParts[i]);
			}

			if (!Directory.Exists(filePath))
				Directory.CreateDirectory(filePath);

			var form = new UserInputResource()
			{
				SolutionFolder = replacementsDictionary["$solutiondirectory$"]
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				var entityClassFile = (EntityClassFile)form._entityClassList.SelectedItem;

				var model = EmitModel(entityClassFile, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary);

				replacementsDictionary.Add("$model$", model);
				replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
				Proceed = true;
			}
			else
				Proceed = false;
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}

		private string EmitModel(EntityClassFile entityClassFile, DBTable table, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");
			replacementsDictionary.Add("$barray$", "false");

			var results = new StringBuilder();
			bool hasPrimary = false;
			var entityClassMembers = Utilities.LoadEntityClassMembers(entityClassFile.FileName, columns);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({entityClassFile.ClassName}))]");
			results.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var member in entityClassMembers)
			{
				if (firstColumn)
					firstColumn = false;
				else
					results.AppendLine();

				if (member.EntityNames[0].IsPrimaryKey)
				{
					if (!hasPrimary)
					{
						results.AppendLine("\t\t///\t<summary>");
						results.AppendLine($"\t\t///\tThe hypertext reference that identifies the resource.");
						results.AppendLine("\t\t///\t</summary>");
						results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
						hasPrimary = true;
					}
				}
				else if (member.EntityNames[0].IsForeignKey)
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\tA hypertext reference that identifies the associated {member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");
					results.AppendLine($"\t\tpublic Uri {member.ResourceMemberName} {{ get; set; }}");
				}
				else
				{
					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\t{member.ResourceMemberName}");
					results.AppendLine("\t\t///\t</summary>");

					if (member.EntityNames[0].ServerType == DBServerType.SQLSERVER && (SqlDbType)member.EntityNames[0].DataType == SqlDbType.Image)
						replacementsDictionary["$image$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Inet)
						replacementsDictionary["$net$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Cidr)
						replacementsDictionary["$net$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr)
						replacementsDictionary["$netinfo$"] = "true";
					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.MacAddr8)
						replacementsDictionary["$netinfo$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Boolean))
						replacementsDictionary["$barray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == (NpgsqlDbType.Array | NpgsqlDbType.Bit))
						replacementsDictionary["$barray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Bit && member.EntityNames[0].Length > 1)
						replacementsDictionary["$barray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL && (NpgsqlDbType)member.EntityNames[0].DataType == NpgsqlDbType.Varbit)
						replacementsDictionary["$barray$"] = "true";

					if (member.EntityNames[0].ServerType == DBServerType.POSTGRESQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetPostgresqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (member.EntityNames[0].ServerType == DBServerType.MYSQL)
						results.AppendLine($"\t\tpublic {DBHelper.GetMySqlResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
					else if (member.EntityNames[0].ServerType == DBServerType.SQLSERVER)
						results.AppendLine($"\t\tpublic {DBHelper.GetSqlServerResourceDataType(member.EntityNames[0])} {member.ResourceMemberName} {{ get; set; }}");
				}
			}

			results.AppendLine("\t}");

			return results.ToString();
		}
	}
}
