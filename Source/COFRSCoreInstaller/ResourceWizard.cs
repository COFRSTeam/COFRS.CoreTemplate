using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System.Collections.Generic;
using System.Data;
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
