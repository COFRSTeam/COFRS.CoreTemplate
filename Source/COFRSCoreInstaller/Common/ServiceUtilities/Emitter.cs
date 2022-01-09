using COFRS.Template.Common.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace COFRS.Template.Common.ServiceUtilities
{
    internal class Emitter
	{
		public string EmitExampleModel(ICodeService codeService, ResourceClass resourceModel, ProfileMap profileMap, string exampleClassName, DBServerType serverType, string connectionString)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var results = new StringBuilder();

            #region Patch Example
            //	Generate the patch example class
            results.AppendLine("\t///\t<summary>");

			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t///\tGenerates an example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.");
			else
				results.AppendLine($"\t///\tGenerates an example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.");

			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {resourceModel.ClassName}PatchExample : IExamplesProvider<IEnumerable<PatchCommand>>");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t///\tGenerates an example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.");
			else
				results.AppendLine($"\t\t///\tGenerates an example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.");

			results.AppendLine("\t\t///\t</summary>");

			if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
				resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t///\t<returns>An example model of a patch command affecting an <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");
			else
				results.AppendLine($"\t\t///\t<returns>An example model of a patch command affecting a <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");

			results.AppendLine($"\t\tpublic IEnumerable<PatchCommand> GetExamples()");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tvar results = new List<PatchCommand> {");

			var exampleModel = codeService.GetExampleModel(0, resourceModel, serverType, connectionString);
			var entityJson = JObject.Parse(exampleModel);
			var count = 0;

			foreach ( var property in entityJson.Children())
            {
				switch ( count )
                {
					case 1:
						{
							results.AppendLine("\t\t\t\tnew PatchCommand {");
							results.AppendLine("\t\t\t\t\tOp = \"replace\",");
							results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");

							if (property.First.Type == JTokenType.Null)
								results.AppendLine($"\t\t\t\t\tValue = null");
							else if (property.First.Type == JTokenType.String ||
									 property.First.Type == JTokenType.Date ||
									 property.First.Type == JTokenType.Guid ||
									 property.First.Type == JTokenType.TimeSpan)
								results.AppendLine($"\t\t\t\t\tValue = \"{((string)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Integer)
								results.AppendLine($"\t\t\t\t\tValue = \"{((long)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Float)
								results.AppendLine($"\t\t\t\t\tValue = \"{((double)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Boolean)
							{
								var theBooleanValue = (bool)property.Value<JProperty>();
								results.AppendLine($"\t\t\t\t\tValue = {theBooleanValue.ToString().ToLower()}");
							}
							else
								results.AppendLine($"\t\t\t\t\tValue = \"value\"");

							results.AppendLine("\t\t\t\t},");
						}
						break;

					case 2:
						{
							results.AppendLine("\t\t\t\tnew PatchCommand {");
							results.AppendLine("\t\t\t\t\tOp = \"add\",");
							results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");

							if (property.First.Type == JTokenType.Null)
								results.AppendLine($"\t\t\t\t\tValue = null");
							else if (property.First.Type == JTokenType.String ||
									 property.First.Type == JTokenType.Date ||
									 property.First.Type == JTokenType.Guid ||
									 property.First.Type == JTokenType.TimeSpan)
								results.AppendLine($"\t\t\t\t\tValue = \"{((string)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Integer)
								results.AppendLine($"\t\t\t\t\tValue = \"{((long)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Float)
								results.AppendLine($"\t\t\t\t\tValue = \"{((double)property.Value<JProperty>())}\"");
							else if (property.First.Type == JTokenType.Boolean)
							{
								var theBooleanValue = (bool)property.Value<JProperty>();
								results.AppendLine($"\t\t\t\t\tValue = {theBooleanValue.ToString().ToLower()}");
							}
							else
								results.AppendLine($"\t\t\t\t\tValue = \"value\"");

							results.AppendLine("\t\t\t\t},");
						}
						break;

					case 3:
						results.AppendLine("\t\t\t\tnew PatchCommand {");
						results.AppendLine("\t\t\t\t\tOp = \"delete\",");
						results.AppendLine($"\t\t\t\t\tPath = \"{property.Path}\",");
						results.AppendLine("\t\t\t\t}");
						break;
				};

				count++;
            }

			results.AppendLine("\t\t\t};");

			results.AppendLine();
			results.AppendLine("\t\t\treturn results;");

			results.AppendLine("\t\t}");
			results.AppendLine("\t}");
			results.AppendLine();
            #endregion

            #region Single Example
            //	Generate the single example class
            results.AppendLine("\t///\t<summary>");

            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t///\tGenerates an example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.");
            else
                results.AppendLine($"\t///\tGenerates an example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.");

            results.AppendLine("\t///\t</summary>");
            results.AppendLine($"\tpublic class {resourceModel.ClassName}Example : IExamplesProvider<{resourceModel.ClassName}>");
            results.AppendLine("\t{");

			results.AppendLine("\t\tprivate static R MapFrom<R>(Func<R> f)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\treturn f();");
			results.AppendLine("\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t///\t<summary>");
            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t\t///\tGenerates an example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.");
            else
                results.AppendLine($"\t\t///\tGenerates an example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.");

            results.AppendLine("\t\t///\t</summary>");

            if (resourceModel.ClassName.StartsWith("a", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("e", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                resourceModel.ClassName.StartsWith("u", StringComparison.OrdinalIgnoreCase))
                results.AppendLine($"\t\t///\t<returns>An example model of an <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");
            else
                results.AppendLine($"\t\t///\t<returns>An example model of a <see cref=\"{resourceModel.ClassName}\"/> resource.</returns>");

            results.AppendLine($"\t\tpublic {resourceModel.ClassName} GetExamples()");
            results.AppendLine("\t\t{");

            results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");
            results.AppendLine();

			results.Append("\t\t\tvar singleExample = ");
            EmitSingleModel("", resourceModel, profileMap, results, entityJson);
            results.AppendLine(";");

            results.AppendLine();
            results.AppendLine("\t\t\treturn singleExample;");

            results.AppendLine("\t\t}");
            results.AppendLine("\t}");
            results.AppendLine();
            #endregion

            #region Collection Example
            //	Generate the collection example class
            results.AppendLine("\t///\t<summary>");
            results.AppendLine($"\t///\tGenerates an example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.");
            results.AppendLine("\t///\t</summary>");
            results.AppendLine($"\t///\t<returns>An example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.</returns>");
            results.AppendLine($"\t\tpublic class {resourceModel.ClassName}CollectionExample : IExamplesProvider<PagedCollection<{resourceModel.ClassName}>>");
            results.AppendLine("\t{");

			results.AppendLine("\t\tprivate static R MapFrom<R>(Func<R> f)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\treturn f();");
			results.AppendLine("\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t///\t<summary>");
            results.AppendLine($"\t\t///\tGenerates an example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.");
            results.AppendLine("\t\t///\t</summary>");
            results.AppendLine($"\t\t///\t<returns>An example model of a collection of <see cref=\"{resourceModel.ClassName}\"/> resources.</returns>");
            results.AppendLine($"\t\tpublic PagedCollection<{resourceModel.ClassName}> GetExamples()");
            results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");
			results.AppendLine();
			results.AppendLine($"\t\t\tvar exampleList = new List<{resourceModel.ClassName}>() {{");

			var first = true;

			for ( int j = 0; j < 3; j++ )
            {
				if (first)
					first = false;
				else
					results.AppendLine(",");

				int r = j + 6;
				exampleModel = "";

				while (string.IsNullOrWhiteSpace(exampleModel.Replace("{", "").Replace("}", "")))
				{
					exampleModel = codeService.GetExampleModel(r--, resourceModel, serverType, connectionString);
				}

				entityJson = JObject.Parse(exampleModel);

				results.Append("\t\t\t\t");
				EmitSingleModel("\t", resourceModel, profileMap, results, entityJson);
			}

			var baseUrl = ExtractBaseUrl(profileMap);

			results.AppendLine();
			results.AppendLine("\t\t\t};");
			results.AppendLine();
			results.AppendLine($"\t\t\tvar collection = new PagedCollection<{resourceModel.ClassName}>() {{");
			results.AppendLine($"\t\t\t\tHRef = new Uri(rootUrl, \"{baseUrl}?limit(6,3)\"),");
			results.AppendLine($"\t\t\t\tFirst = new Uri(rootUrl, \"{baseUrl}?limit(1,3)\"),");
			results.AppendLine($"\t\t\t\tPrevious = new Uri(rootUrl, \"{baseUrl}?limit(3,3)\"),");
			results.AppendLine($"\t\t\t\tNext = new Uri(rootUrl, \"{baseUrl}?limit(9,3)\"),");
			results.AppendLine($"\t\t\t\tLimit = 3,");
			results.AppendLine($"\t\t\t\tCount = 20,");
			results.AppendLine($"\t\t\t\tItems = exampleList.ToArray()");
			results.AppendLine("\t\t\t\t};");
			results.AppendLine("\t\t\treturn collection;");
			results.AppendLine("\t\t}");
            results.AppendLine("\t}");
            results.AppendLine();
			#endregion

			return results.ToString();
        }

		public string EmitResourceEnum(string resourceClassName, EntityClass model, string connectionString)
        {
			ThreadHelper.ThrowIfNotOnUIThread();

			if (model.ServerType == DBServerType.MYSQL)
				return EmitResourceMySqlEnum(resourceClassName, model, connectionString);
			else if (model.ServerType == DBServerType.POSTGRESQL)
				return EmitResourcePostgresqlEnum(resourceClassName, model, connectionString);
			else if (model.ServerType == DBServerType.SQLSERVER)
				return EmitResourceSqlServerEnum(resourceClassName, model, connectionString);

			return "Invalid DB Server Type";
        }

		private static string EmitResourceMySqlEnum(string resourceClassName, EntityClass model, string connectionString)
		{
			throw new NotImplementedException("not implemented yet");
		}

		private static string EmitResourcePostgresqlEnum(string resourceClassName, EntityClass model, string connectionString)
		{
			throw new NotImplementedException("not implemented yet");
		}

		private static string EmitResourceSqlServerEnum(string resourceClassName, EntityClass entityModel, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			StringBuilder results = new StringBuilder();

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({entityModel.ClassName}))]");

			var dataType = entityModel.Columns[0].ModelDataType;

			results.AppendLine($"\tpublic enum {resourceClassName} : {dataType}");
			results.AppendLine("\t{");

			bool firstColumn = true;

			string query = "select ";

			foreach ( var col in entityModel.Columns)
            {
				if (firstColumn)
					firstColumn = false;
				else
				{
					query += ", ";
				}

				query += col.ColumnName;
            }

			query += " from ";
			query += entityModel.TableName;

			firstColumn = true;

			using ( var connection = new SqlConnection(connectionString))
            {
				connection.Open();

				using ( var command = new SqlCommand(query, connection))
                {
					using (var reader = command.ExecuteReader())
                    {
						while ( reader.Read())
                        {
							if ( firstColumn )
                            {
								firstColumn = false;
                            }
							else
                            {
								results.AppendLine(",");
								results.AppendLine();
                            }

							if ( string.Equals(entityModel.Columns[0].DBDataType, "tinyint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetByte(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(entityModel.Columns[0].DBDataType, "smallint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt16(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(entityModel.Columns[0].DBDataType, "int", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt32(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
							else if (string.Equals(entityModel.Columns[0].DBDataType, "bigint", StringComparison.OrdinalIgnoreCase))
							{
								var theValue = reader.GetInt64(0);
								var name = reader.GetString(1);

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\t{name}");
								results.AppendLine("\t\t///\t</summary>");
								results.Append($"\t\t{name.Replace(" ", "")} = {theValue}");
							}
						}
					}
                }
            }

			results.AppendLine();

			results.AppendLine("\t}");

			return results.ToString();
		}

		/// <summary>
		/// Extracts the base URL for the resource
		/// </summary>
		/// <param name="profileMap"></param>
		/// <returns></returns>
		private static string ExtractBaseUrl(ProfileMap profileMap)
        {
			var baseUrl = "";
			var map = profileMap.ResourceProfiles.FirstOrDefault(rp => string.Equals(rp.ResourceColumnName, "href", StringComparison.OrdinalIgnoreCase));

			if ( map != null )
            {
				var mapFunction = map.MapFunction;
				var indexStart = mapFunction.ToLower().IndexOf("$\"");
				var indexEnd = mapFunction.ToLower().IndexOf("/id");
				baseUrl = mapFunction.Substring(indexStart+2, indexEnd - indexStart-2);
            }

			return baseUrl;
        }

        private static void EmitSingleModel(string prefix, ResourceClass resourceModel, ProfileMap profileMap, StringBuilder results, JObject entityJson)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			bool first = true;
			results.AppendLine($"new {resourceModel.ClassName} {{");
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			foreach (var map in profileMap.ResourceProfiles)
            {
                if (first)
                    first = false;
                else
                    results.AppendLine(",");

                results.Append($"{prefix}\t\t\t\t{map.ResourceColumnName} = ");
				results.Append(codeService.ResolveMapFunction(entityJson, map.ResourceColumnName, resourceModel.Entity.Columns, resourceModel, map.MapFunction));
			}

			results.AppendLine();
			results.Append($"{prefix}\t\t\t}}");
        }

		public string EmitMappingModel(ResourceClass resourceModel, string mappingClassName, Dictionary<string, string> replacementsDictionary)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var ImageConversionRequired = false;
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceModel.ClassName);

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{nn.SingleForm} Profile for AutoMapper");
			results.AppendLine("\t///\t</summary>");

			results.AppendLine($"\tpublic class {mappingClassName} : Profile");
			results.AppendLine("\t{");

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {nn.SingleForm} Profile");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {mappingClassName}()");
			results.AppendLine("\t\t{");
			results.AppendLine($"\t\t\t//\tGets the root URL of the service from the configuration settings.");
			results.AppendLine("\t\t\tvar rootUrl = new Uri(Startup.AppConfig.GetRootUrl());");

			#region Create the Resource to Entity Mapping
			results.AppendLine();
			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a {resourceModel.ClassName} model instance (the source)");
			results.AppendLine($"\t\t\t//\tinto a {resourceModel.Entity.ClassName} model instance (the destination).");
			results.AppendLine($"\t\t\tCreateMap<{resourceModel.ClassName}, {resourceModel.Entity.ClassName}>()");

			bool first = true;

			var profileMap = codeService.GenerateProfileMap(resourceModel);

			foreach ( var resourceMap in profileMap.EntityProfiles)
            {
				if (first)
					first = false;
				else
					results.AppendLine("))");

				results.Append($"\t\t\t\t.ForMember(destination => destination.{resourceMap.EntityColumnName}, opts => opts.MapFrom(source =>");
				results.Append(resourceMap.MapFunction);
            }

			results.AppendLine("));");
			results.AppendLine();

			#endregion

			#region Create Entity to Resource Mapping

			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a {resourceModel.Entity.ClassName} model instance (the source)");
			results.AppendLine($"\t\t\t//\tinto a {resourceModel.ClassName} model instance (the destination).");
			results.AppendLine($"\t\t\tCreateMap<{resourceModel.Entity.ClassName}, {resourceModel.ClassName}>()");

			first = true;

			for ( int j = 0; j < profileMap.ResourceProfiles.Count(); j++)
            {
				if ( first )
                {
					first = false;
					results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
					results.Append(profileMap.ResourceProfiles[j].MapFunction);
				}
				else
                {
					if (profileMap.ResourceProfiles[j].ResourceColumnName.CountOf('.') > 0)
                    {
						j = GenerateChildMappings(results, j, profileMap.ResourceProfiles, profileMap.ResourceProfiles[j].ResourceColumnName.GetBaseColumn(), ref ImageConversionRequired);

                        if (j < profileMap.ResourceProfiles.Count())
                        {
                            results.AppendLine("))");
                            results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
                            results.Append(profileMap.ResourceProfiles[j].MapFunction);
                        }
                    }
					else
                    {
						results.AppendLine("))");
						results.Append($"\t\t\t\t.ForMember(destination => destination.{profileMap.ResourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
						results.Append(profileMap.ResourceProfiles[j].MapFunction);
					}
				}
			}

			results.AppendLine("));");
			results.AppendLine();

			#endregion

			results.AppendLine($"\t\t\t//\tCreates a mapping to transform a collection of {resourceModel.Entity.ClassName} model instances (the source)");
			results.AppendLine($"\t\t\t//\tinto a collection of {resourceModel.ClassName} model instances (the destination).");
			results.AppendLine($"\t\t\tCreateMap<PagedCollection<{resourceModel.Entity.ClassName}>, PagedCollection<{resourceModel.ClassName}>>()");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.HRef, opts => opts.MapFrom(source => new Uri(rootUrl, $\"{nn.PluralCamelCase}{{source.HRef.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.First, opts => opts.MapFrom(source => source.First == null ? null : new Uri(rootUrl, $\"{nn.PluralCamelCase}{{source.First.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.Next, opts => opts.MapFrom(source => source.Next == null ? null : new Uri(rootUrl, $\"{nn.PluralCamelCase}{{source.Next.Query}}\")))");
			results.AppendLine($"\t\t\t\t.ForMember(destination => destination.Previous, opts => opts.MapFrom(source => source.Previous == null ? null : new Uri(rootUrl, $\"{nn.PluralCamelCase}{{source.Previous.Query}}\")));");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private int GenerateChildMappings(StringBuilder results, int j, List<ResourceProfile> resourceProfiles, string baseColumn, ref bool ImageConversionRequired)
        {
			var baseCount = baseColumn.CountOf('.');
			var previousCount = resourceProfiles[j].ResourceColumnName.CountOf('.');
			results.AppendLine(" {");

			bool first = true;

			while ( j < resourceProfiles.Count() - 1 && resourceProfiles[j].ResourceColumnName.CountOf('.') > baseCount)
            {
				var resourceCount = resourceProfiles[j].ResourceColumnName.CountOf('.');

				if (resourceCount > previousCount)
                {
					j = GenerateChildMappings(results, j, resourceProfiles, resourceProfiles[j].ResourceColumnName.GetBaseColumn(), ref ImageConversionRequired);

					if (j < resourceProfiles.Count() - 1)
					{
						results.AppendLine("))");
						results.Append($"\t\t\t\t.ForMember(destination => destination.{resourceProfiles[j].ResourceColumnName}, opts => opts.MapFrom(source => ");
						results.Append(resourceProfiles[j].MapFunction);
					}
				}
				else
                {
					if (first)
						first = false;
					else
						results.AppendLine(",");

					results.Append("\t\t\t\t\t");

					for (int q = 0; q < resourceCount; q++)
						results.Append("\t");

					var parts = resourceProfiles[j].ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
					results.Append($"{parts[parts.Count() - 1]} = {resourceProfiles[j].MapFunction}");
                }

				previousCount = resourceCount;
				j++;
			}

			results.AppendLine();
			results.Append("\t\t\t\t\t}");

			return j;
        }

		/// <summary>
		/// Emits an entity data model based upon the fields contained within the database table
		/// </summary>
		/// <param name="serverType">The type of server used to house the table</param>
		/// <param name="table">The name of the database table</param>
		/// <param name="entityClassName">The class name for the model?</param>
		/// <param name="columns">The list of columns contained in the database</param>
		/// <param name="replacementsDictionary">List of replacements key/value pairs for the solution</param>
		/// <param name="connectionString">The connection string to connect to the database, if necessary</param>
		/// <returns>A model of the entity data table</returns>
		public string EmitEntityModel(string entityClassName, string schema, string tablename, DBServerType serverType, DBColumn[] columns, Dictionary<string, string> replacementsDictionary)
		{
			var result = new StringBuilder();
			replacementsDictionary.Add("$image$", "false");
			replacementsDictionary.Add("$net$", "false");
			replacementsDictionary.Add("$netinfo$", "false");
			replacementsDictionary.Add("$barray$", "false");

			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{entityClassName}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				result.AppendLine($"\t[Table(\"{tablename}\", DBType = \"{serverType}\")]");
			else
				result.AppendLine($"\t[Table(\"{tablename}\", Schema = \"{schema}\", DBType = \"{serverType}\")]");

			result.AppendLine($"\tpublic class {entityClassName}");
			result.AppendLine("\t{");

			bool firstColumn = true;
			foreach (var column in columns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
					result.Append($", ForeignTableName=\"{column.ForeignTableName}\"");
				}

				AppendNullable(result, column.IsNullable, ref first);

				if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NVarChar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NChar", StringComparison.OrdinalIgnoreCase))
				{
					if (column.Length > 1)
						AppendFixed(result, column.Length, true, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "NText", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Name", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Varchar", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarChar", StringComparison.OrdinalIgnoreCase)))
				{
					if (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase)))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if ((serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Varbit", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Citext", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Text", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, -1, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Char", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "_Char", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "String", StringComparison.OrdinalIgnoreCase)))
				{
					//	Insert the column definition
					if (serverType == DBServerType.POSTGRESQL)
					{
						if (string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
						else if (string.Equals(column.DBDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
						{
							AppendFixed(result, column.Length, true, ref first);
						}
					}
					else if (serverType == DBServerType.MYSQL)
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
					else
					{
						if (column.Length != 1)
							AppendFixed(result, column.Length, true, ref first);
					}
				}

				else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Bytea", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "VarBinary", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)) ||
						 (serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Binary", StringComparison.OrdinalIgnoreCase)))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Timestamp", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
					AppendAutofield(result, ref first);
				}

				if ((serverType == DBServerType.SQLSERVER && string.Equals(column.DBDataType, "Decimal", StringComparison.OrdinalIgnoreCase)) ||
					(serverType == DBServerType.MYSQL && string.Equals(column.DBDataType, "Decimal", StringComparison.OrdinalIgnoreCase)) ||
					(serverType == DBServerType.POSTGRESQL && string.Equals(column.DBDataType, "Numeric", StringComparison.OrdinalIgnoreCase)))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, column, ref first);
				AppendEntityName(result, column, ref first);

				if (serverType == DBServerType.POSTGRESQL)
				{
					if (string.Equals(column.DBDataType, "Inet", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$net$"] = "true";

					if (string.Equals(column.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && column.Length > 1)
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$barray$"] = "true";

					if (string.Equals(column.DBDataType, "Point", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Line", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Path", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";

					if (string.Equals(column.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$npgsqltypes$"] = "true";
				}
				else if (serverType == DBServerType.SQLSERVER)
				{
					if (string.Equals(column.DBDataType, "Image", StringComparison.OrdinalIgnoreCase))
						replacementsDictionary["$image$"] = "true";
				}

				result.AppendLine(")]");

				//	Insert the column definition
				if (serverType == DBServerType.POSTGRESQL)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
				else if (serverType == DBServerType.MYSQL)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
				else if (serverType == DBServerType.SQLSERVER)
				{
					column.ModelDataType = column.ModelDataType;
					result.AppendLine($"\t\tpublic {column.ModelDataType} {column.ColumnName} {{ get; set; }}");
				}
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		/// <summary>
		/// Generates a resource model from a given entity model
		/// </summary>
		/// <param name="resourceClass">The <see cref="ResourceClass"/> to generate</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		/// <returns></returns>
		public string EmitResourceModel(string resourceClassName, EntityClass entityModel, Dictionary<string, string> replacementsDictionary)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			List<DBColumn> resourceColumns = new List<DBColumn>();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			replacementsDictionary.Add("$resourceimage$", "false");
			replacementsDictionary.Add("$resourcenet$", "false");
			replacementsDictionary.Add("$resourcenetinfo$", "false");
			replacementsDictionary.Add("$resourcebarray$", "false");
			replacementsDictionary.Add("$usenpgtypes$", "false");
			replacementsDictionary.Add("$annotations$", "false");

			var results = new StringBuilder();
			bool hasPrimary = false;

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClassName}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\t[Entity(typeof({entityModel.ClassName}))]");

			if (entityModel.ElementType == ElementType.Enum)
			{
				results.AppendLine($"\tpublic enum {resourceClassName}");
				results.AppendLine("\t{");

				bool firstColumn = true;
				foreach ( var member in entityModel.Columns)
                {
					if (firstColumn)
						firstColumn = false;
					else
					{
						results.AppendLine(",");
						results.AppendLine();
					}

					var membername = codeService.CorrectForReservedNames(codeService.NormalizeClassName(member.ColumnName));

					results.AppendLine("\t\t///\t<summary>");
					results.AppendLine($"\t\t///\t{membername}");
					results.AppendLine("\t\t///\t</summary>");
					results.Append($"\t\t{membername}");

					var resourceColumn = new DBColumn()
					{
						ColumnName = membername
					};

					resourceColumns.Add(resourceColumn);

				}
			}
			else
			{
				results.AppendLine($"\tpublic class {resourceClassName} : IValidatableObject");
				results.AppendLine("\t{");
				var foreignTableList = new List<string>();

				bool firstColumn = true;
				foreach (var member in entityModel.Columns)
				{
					if (firstColumn)
						firstColumn = false;
					else
						results.AppendLine();

					if (member.IsPrimaryKey)
					{
						if (!hasPrimary)
						{
							results.AppendLine("\t\t///\t<summary>");
							results.AppendLine($"\t\t///\tThe hypertext reference that identifies the resource.");
							results.AppendLine("\t\t///\t</summary>");
							results.AppendLine("\t\t[Url]");
							results.AppendLine($"\t\tpublic Uri HRef {{ get; set; }}");
							hasPrimary = true;
						}

						var resourceColumn = new DBColumn()
						{
							ColumnName = "HRef",
							IsPrimaryKey = member.IsPrimaryKey,
							IsForeignKey = member.IsForeignKey,
							IsComputed = member.IsComputed,
						    IsFixed = member.IsFixed,
							IsIdentity = member.IsIdentity,
							IsIndexed = member.IsIndexed,
							IsNullable = member.IsNullable,
							ModelDataType = "Uri"
						};

						resourceColumns.Add(resourceColumn);

					}
					else if (member.IsForeignKey)
					{
						//	This is a foreign key reference. Normally, that is simply an href to the table.
						//	However, there is one exception.
						//
						//	If the foreign table is a lookup table (i.e., consists of a numeric key/value pair) then 
						//	the user has the option of generating its resource model as an enum. If so, we need to 
						//	treat it as an enum here.

						//	If such a resource model exists, it will have a resource type of Enum, and it's corresponding
						//	entity model will have the same table name as the foreign table name

						var enumResourceModel = codeService.GetResourceClassBySchema(entityModel.SchemaName, member.ForeignTableName);

						if (enumResourceModel != null && enumResourceModel.ResourceType == ResourceType.Enum)
						{
							//	This is the enum version
							results.AppendLine("\t\t///\t<summary>");
							results.AppendLine($"\t\t///\tThe enum for {member.ColumnName}");
							results.AppendLine("\t\t///\t</summary>");
							results.AppendLine($"\t\tpublic {enumResourceModel.ClassName} {member.ColumnName} {{ get; set; }}");

							var resourceColumn = new DBColumn()
							{
								ColumnName = member.ColumnName,
								IsPrimaryKey = member.IsPrimaryKey,
								IsForeignKey = member.IsForeignKey,
								IsComputed = member.IsComputed,
								IsFixed = member.IsFixed,
								IsIdentity = member.IsIdentity,
								IsIndexed = member.IsIndexed,
								IsNullable = member.IsNullable,
								ModelDataType = enumResourceModel.ClassName
							};

							resourceColumns.Add(resourceColumn);
						}
						else
						{
							//	This is just the plain old foreign key reference
							if (!foreignTableList.Contains(member.ForeignTableName))
							{
								var childResource = codeService.GetResourceClassBySchema(entityModel.SchemaName, member.ForeignTableName);
								string memberName;

								if (childResource != null)
									memberName = childResource.ClassName;
								else
								{
									var nn = new NameNormalizer(member.ForeignTableName);
									memberName = nn.SingleForm;
								}

								memberName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(memberName));

								results.AppendLine("\t\t///\t<summary>");
								results.AppendLine($"\t\t///\tA hypertext reference that identifies the associated {memberName}");
								results.AppendLine("\t\t///\t</summary>");
								results.AppendLine("\t\t[Url]");
								results.AppendLine($"\t\tpublic Uri {memberName} {{ get; set; }}");

								foreignTableList.Add(member.ForeignTableName);

								var resourceColumn = new DBColumn()
								{
									ColumnName = memberName,
									IsPrimaryKey = member.IsPrimaryKey,
									IsForeignKey = member.IsForeignKey,
									IsComputed = member.IsComputed,
									IsFixed = member.IsFixed,
									IsIdentity = member.IsIdentity,
									IsIndexed = member.IsIndexed,
									IsNullable = member.IsNullable,
									ForeignTableName = member.ForeignTableName,
									ModelDataType = "Uri"
								};

								resourceColumns.Add(resourceColumn);
							}
						}
					}
					else
					{
						var membername = codeService.CorrectForReservedNames(codeService.NormalizeClassName(member.ColumnName));

						results.AppendLine("\t\t///\t<summary>");
						results.AppendLine($"\t\t///\t{membername}");
						results.AppendLine("\t\t///\t</summary>");

						if (entityModel.ServerType == DBServerType.SQLSERVER)
						{
							if (string.Equals(member.DBDataType, "Image", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourceimage$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}
						else if (entityModel.ServerType == DBServerType.POSTGRESQL)
						{
							if (string.Equals(member.DBDataType, "Inet", StringComparison.OrdinalIgnoreCase) ||
								string.Equals(member.DBDataType, "Cidr", StringComparison.OrdinalIgnoreCase) ||
								string.Equals(member.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcenet$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcenetinfo$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase) ||
									 (string.Equals(member.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && member.Length > 1) ||
									 string.Equals(member.DBDataType, "VarBit", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$resourcebarray$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Point", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Path", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Line", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
							{
								replacementsDictionary["$usenpgtypes$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
							else if (string.Equals(member.DBDataType, "TimeTz", StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(member.DBDataType, "_TimeTz", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"HH:mm:ss.fffffffzzz\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}
						else if (entityModel.ServerType == DBServerType.MYSQL)
						{
							if (string.Equals(member.DBDataType, "Date", StringComparison.OrdinalIgnoreCase))
							{
								results.AppendLine("\t\t[JsonFormat(\"yyyy-MM-dd\")]");
								replacementsDictionary["$annotations$"] = "true";
							}
						}

						if (member.ModelDataType.Equals("string", StringComparison.OrdinalIgnoreCase))
                        {
							if ( member.ModelDataType.Equals("string", StringComparison.OrdinalIgnoreCase))
                            {
								if (!member.IsNullable)
									results.AppendLine($"\t\t[Required(AllowEmptyStrings=false, ErrorMessage=\"{membername} cannot be blank or null.\")]");

								results.AppendLine($"\t\t[StringLength({member.Length}, ErrorMessage=\"{membername} cannot exceed {member.Length} characters.\")]");
                            }
                        }
						else if ( member.ModelDataType.Equals("Uri"))
                        {
							if (!member.IsNullable)
							{
								results.AppendLine($"\t\t[Required(ErrorMessage=\"{membername} cannot be null.\")]");
							}

							results.AppendLine("\t\t[Url]");
						}
						else if ( !member.IsNullable )
                        {
							results.AppendLine($"\t\t[Required(ErrorMessage=\"{membername} cannot be null.\")]");
						}

						results.AppendLine($"\t\tpublic {member.ModelDataType} {membername} {{ get; set; }}");

						var resourceColumn = new DBColumn()
						{
							ColumnName = membername,
							IsPrimaryKey = member.IsPrimaryKey,
							IsForeignKey = member.IsForeignKey,
							IsComputed = member.IsComputed,
							IsFixed = member.IsFixed,
							IsIdentity = member.IsIdentity,
							IsIndexed = member.IsIndexed,
							IsNullable = member.IsNullable,
							ModelDataType = member.ModelDataType
						};

						resourceColumns.Add(resourceColumn);

					}
				}

				results.AppendLine();
				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tDetermines whether the specified object is valid.");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine("\t\t///\t<param name=\"validationContext\">The <see cref=\"ValidationContext\"/> for the validation.</param>");
				results.AppendLine("\t\t///\t<returns>A collection that holds failed-validation information.</returns>");
				results.AppendLine("\t\tpublic IEnumerable<ValidationResult> Validate(ValidationContext validationContext)");
				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tvar results = new List<ValidationResult>();");
				results.AppendLine("\t\t\treturn results.ToArray();");
				results.AppendLine("\t\t}");
			}

			results.AppendLine("\t}");

			return results.ToString();
		}

		public string EmitEntityEnum(string className, string schema, string tablename, DBColumn[] columns)
		{
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var nn = new NameNormalizer(className);
			var builder = new StringBuilder();

			builder.Clear();
			builder.AppendLine("\t///\t<summary>");
			builder.AppendLine($"\t///\tEnumerates a list of {nn.PluralForm}");
			builder.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				builder.AppendLine($"\t[PgEnum(\"{tablename}\")]");
			else
				builder.AppendLine($"\t[PgEnum(\"{tablename}\", Schema = \"{schema}\")]");

			builder.AppendLine($"\tpublic enum {className}");
			builder.AppendLine("\t{");
			bool firstUse = true;

			foreach (var column in columns)
			{
				if (firstUse)
					firstUse = false;
				else
				{
					builder.AppendLine(",");
					builder.AppendLine();
				}

				builder.AppendLine("\t\t///\t<summary>");
				builder.AppendLine($"\t\t///\t{codeService.NormalizeClassName(column.ColumnName)}");
				builder.AppendLine("\t\t///\t</summary>");
				builder.AppendLine($"\t\t[PgName(\"{column.EntityName}\")]");

				var elementName = codeService.NormalizeClassName(column.ColumnName);
				builder.Append($"\t\t{elementName}");
			}

			builder.AppendLine();
			builder.AppendLine("\t}");

			return builder.ToString();
		}

		public string EmitComposite(string className, string schema, string tableName, ElementType elementType, DBColumn[] columns, string connectionString, Dictionary<string, string> replacementsDictionary)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();
			var result = new StringBuilder();

			result.Clear();
			result.AppendLine("\t///\t<summary>");
			result.AppendLine($"\t///\t{className}");
			result.AppendLine("\t///\t</summary>");

			if (string.IsNullOrWhiteSpace(schema))
				result.AppendLine($"\t[PgComposite(\"{tableName}\")]");
			else
				result.AppendLine($"\t[PgComposite(\"{tableName}\", Schema = \"{schema}\")]");

			result.AppendLine($"\tpublic class {className}");
			result.AppendLine("\t{");

			bool firstColumn = true;

			foreach (var column in columns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					result.AppendLine();

				result.AppendLine("\t\t///\t<summary>");
				result.AppendLine($"\t\t///\t{column.ColumnName}");
				result.AppendLine("\t\t///\t</summary>");

				//	Construct the [Member] attribute
				result.Append("\t\t[Member(");
				bool first = true;

				if (column.IsPrimaryKey)
				{
					AppendPrimaryKey(result, ref first);
				}

				if (column.IsIdentity)
				{
					AppendIdentity(result, ref first);
				}

				if (column.IsIndexed || column.IsForeignKey)
				{
					AppendIndexed(result, ref first);
				}

				if (column.IsForeignKey)
				{
					AppendForeignKey(result, ref first);
				}

				AppendNullable(result, column.IsNullable, ref first);


				if (string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(column.DBDataType, "Name", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(column.DBDataType, "_Varchar", StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(column.DBDataType, "Varchar", StringComparison.OrdinalIgnoreCase) && column.Length < 0)
						AppendFixed(result, -1, false, ref first);
					else
						AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) ||
					     string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
				{
					//	Insert the column definition
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (string.Equals(column.DBDataType, "Varbit", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(column.DBDataType, "_Varbit", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "Text", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(column.DBDataType, "Citext", StringComparison.OrdinalIgnoreCase) ||
					     string.Equals(column.DBDataType, "_Text", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, -1, false, ref first);
				}

					//	Insert the column definition
				else if (string.Equals(column.DBDataType, "bpchar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
				}
				else if (string.Equals(column.DBDataType, "_bpchar", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, true, ref first);
				}

				else if (string.Equals(column.DBDataType, "bytea", StringComparison.OrdinalIgnoreCase))
				{
					AppendFixed(result, column.Length, false, ref first);
				}

				else if (string.Equals(column.DBDataType, "numeric", StringComparison.OrdinalIgnoreCase))
				{
					AppendPrecision(result, column.NumericPrecision, column.NumericScale, ref first);
				}

				AppendDatabaseType(result, column, ref first);
				AppendEntityName(result, column, ref first);

				if (string.Equals(column.DBDataType, "INet", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$net$"] = "true";

				else if (string.Equals(column.DBDataType, "Cidr", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$net$"] = "true";

				else if (string.Equals(column.DBDataType, "MacAddr", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$netinfo$"] = "true";

				else if (string.Equals(column.DBDataType, "MacAddr8", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$netinfo$"] = "true";

				else if (string.Equals(column.DBDataType, "_Boolean", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "_Bit", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "Bit", StringComparison.OrdinalIgnoreCase) && column.Length > 1)
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "varbit", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$barray$"] = "true";

				else if (string.Equals(column.DBDataType, "Point", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "LSeg", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Circle", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Box", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Line", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Path", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				else if (string.Equals(column.DBDataType, "Polygon", StringComparison.OrdinalIgnoreCase))
					replacementsDictionary["$npgsqltypes$"] = "true";

				result.AppendLine(")]");

				var memberName = codeService.CorrectForReservedNames(codeService.NormalizeClassName(column.ColumnName));
				result.AppendLine($"\t\t[PgName(\"{column.ColumnName}\")]");

				//	Insert the column definition
				result.AppendLine($"\t\tpublic {DBHelper.GetPostgresDataType(column)} {memberName} {{ get; set; }}");
			}

			result.AppendLine("\t}");

			return result.ToString();
		}

		public void GenerateResourceComposites(List<ResourceClass> undefinedModels, ProjectFolder resourceModelFolder, string connectionString)
        {
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			while ( undefinedModels.Count > 0)
            {
				var undefinedModel = undefinedModels[0];
				undefinedModels.RemoveAt(0);

				//	Generate the model
				var result = new StringBuilder();

				if (undefinedModel.Entity.ElementType == ElementType.Enum)
				{
					var nn = new NameNormalizer(undefinedModel.Entity.TableName); 

					var className = $"{codeService.CorrectForReservedNames(codeService.NormalizeClassName(nn.SingleForm))}";

					result.AppendLine("using COFRS;");
					result.AppendLine("using NpgsqlTypes;");
					result.AppendLine($"using {undefinedModel.Entity.Namespace};");
					result.AppendLine();
					result.AppendLine($"namespace {resourceModelFolder.Namespace}");
					result.AppendLine("{");
					result.Append(EmitResourceEnum(className, undefinedModel.Entity, connectionString));
					result.AppendLine("}");

					//	Save the model to disk
					if (!Directory.Exists(Path.GetDirectoryName(resourceModelFolder.Folder)))
						Directory.CreateDirectory(Path.GetDirectoryName(resourceModelFolder.Folder));

					var fullPath = Path.Combine(resourceModelFolder.Folder, $"{className}.cs");

					File.WriteAllText(fullPath, result.ToString());

					//	Add the model to the project
					var parentProject = codeService.GetProjectFromFolder(resourceModelFolder.Folder);
					ProjectItem resourceItem;

					if (parentProject.GetType() == typeof(Project))
						resourceItem = ((Project)parentProject).ProjectItems.AddFromFile(resourceModelFolder.Folder);
					else
						resourceItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(resourceModelFolder.Folder);

					codeService.AddResource(resourceItem);
				}
			}
		}

		/// <summary>
		/// Generate undefined elements
		/// </summary>
		/// <param name="dte2"></param>
		/// <param name="undefinedEntityModels">The list of elements to be defined"/></param>
		/// <param name="connectionString">The connection string to the database server</param>
		/// <param name="replacementsDictionary">The replacements dictionary</param>
		public void GenerateComposites(List<EntityModel> undefinedEntityModels, string connectionString, Dictionary<string, string> replacementsDictionary, ProjectFolder entityModelsFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			//	As we generate each model, the model itself may contain undefined types, which in turn need to be added
			//	to the list of undefinedEntityModels so that they too can be generated. Because of this, each successive
			//	generation might (or might not) add to the list of undefinedEntityModels.
			//
			//	This is why the undefinedEntityModels object is being passed as a reference.
			//
			//	This also means that we can't simply iterate through the list with a foreach - because that list is liable to change.
			//
			//	Therefore, we simply pop the top one off the list, treating the list as a todo stack. And we keep doing that until
			//	the stack is empty.

			foreach ( var undefinedModel in undefinedEntityModels )
			{
				if (undefinedModel.ElementType == ElementType.Enum)
				{
					//	Has it already been previously defined? We don't want two of these...
					if ( codeService.GetEntityClass(undefinedModel.ClassName) == null)
					{
						//	Generate the model
						var result = new StringBuilder();
						
						result.AppendLine("using COFRS;");
						result.AppendLine("using NpgsqlTypes;");
						result.AppendLine();
						result.AppendLine($"namespace {undefinedModel.Namespace}");
						result.AppendLine("{");

						var columns = DBHelper.GenerateColumns(undefinedModel.SchemaName, undefinedModel.TableName, undefinedModel.ServerType, connectionString);
						result.Append(EmitEntityEnum(undefinedModel.ClassName, undefinedModel.SchemaName, undefinedModel.TableName, columns));
						result.AppendLine("}");

						//	Save the model to disk
						if (!Directory.Exists(Path.GetDirectoryName(undefinedModel.Folder)))
							Directory.CreateDirectory(Path.GetDirectoryName(undefinedModel.Folder));

						File.WriteAllText(undefinedModel.Folder, result.ToString());

						//	Add the model to the project
						var parentProject = codeService.GetProjectFromFolder(undefinedModel.Folder);
						ProjectItem entityItem;

						if (parentProject.GetType() == typeof(Project))
							entityItem = ((Project)parentProject).ProjectItems.AddFromFile(undefinedModel.Folder);
						else
							entityItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(undefinedModel.Folder);

						codeService.AddEntity(entityItem);

						//	Register the composite model
						codeService.RegisterComposite(undefinedModel.ClassName, 
							                          undefinedModel.Namespace,
													  undefinedModel.ElementType,
													  undefinedModel.TableName);
					}
				}
				else if (undefinedModel.ElementType == ElementType.Composite)
				{
					//	Has it already been defined? We don't want two of these...
					if (codeService.GetEntityClass(undefinedModel.ClassName) == null)
					{
						var result = new StringBuilder();

						//	Generate the model (and any child models that might be necessary)

						var columns = DBHelper.GenerateColumns(undefinedModel.SchemaName, undefinedModel.TableName, undefinedModel.ServerType, connectionString);

						var body = EmitComposite(undefinedModel.ClassName,
							                     undefinedModel.SchemaName,
												 undefinedModel.TableName,
												 undefinedModel.ElementType,
												 columns,
												 connectionString,
												 replacementsDictionary);

						result.AppendLine("using COFRS;");
						result.AppendLine("using NpgsqlTypes;");

						if (replacementsDictionary.ContainsKey("$net$"))
						{
							if (string.Equals(replacementsDictionary["$net$"], "true", StringComparison.OrdinalIgnoreCase))
								result.AppendLine("using System.Net;");
						}

						if (replacementsDictionary.ContainsKey("$barray$"))
						{
							if (string.Equals(replacementsDictionary["$barray$"], "true", StringComparison.OrdinalIgnoreCase))
								result.AppendLine("using System.Collections;");
						}

						if (replacementsDictionary.ContainsKey("$image$"))
						{
							if (string.Equals(replacementsDictionary["$image$"], "true", StringComparison.OrdinalIgnoreCase))
								result.AppendLine("using System.Drawing;");
						}

						if (replacementsDictionary.ContainsKey("$netinfo$"))
						{
							if (string.Equals(replacementsDictionary["$netinfo$"], "true", StringComparison.OrdinalIgnoreCase))
								result.AppendLine("using System.Net.NetworkInformation;");
						}

						result.AppendLine();
						result.AppendLine($"namespace {undefinedModel.Namespace}");
						result.AppendLine("{");
						result.Append(body);
						result.AppendLine("}");

						//	Save the model to disk
						if (!Directory.Exists(Path.GetDirectoryName(undefinedModel.Folder)))
							Directory.CreateDirectory(Path.GetDirectoryName(undefinedModel.Folder));

						File.WriteAllText(undefinedModel.Folder, result.ToString());

						//	Add the model to the project
						var parentProject = codeService.GetProjectFromFolder(undefinedModel.Folder);
						ProjectItem entityItem;

						if (parentProject.GetType() == typeof(Project))
							entityItem = ((Project)parentProject).ProjectItems.AddFromFile(undefinedModel.Folder);
						else
							entityItem = ((ProjectItem)parentProject).ProjectItems.AddFromFile(undefinedModel.Folder);

						codeService.AddEntity(entityItem);

						//	Register the composite model
						codeService.RegisterComposite(undefinedModel.ClassName,
							                          undefinedModel.Namespace,
													  undefinedModel.ElementType,
													  undefinedModel.TableName);
					}
				}
			}
		}

		#region Helper Functions
		private void AppendComma(StringBuilder result, ref bool first)
		{
			if (first)
				first = false;
			else
				result.Append(", ");
		}
		private void AppendPrimaryKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsPrimaryKey = true");
		}

		private void AppendIdentity(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIdentity = true, AutoField = true");
		}

		private void AppendIndexed(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsIndexed = true");
		}

		private void AppendForeignKey(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("IsForeignKey = true");
		}

		private void AppendNullable(StringBuilder result, bool isNullable, ref bool first)
		{
			AppendComma(result, ref first);

			if (isNullable)
				result.Append("IsNullable = true");
			else
				result.Append("IsNullable = false");
		}

		private void AppendDatabaseType(StringBuilder result, DBColumn column, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"NativeDataType=\"{column.DBDataType}\"");
		}

		private void AppendFixed(StringBuilder result, long length, bool isFixed, ref bool first)
		{
			AppendComma(result, ref first);

			if (length == -1)
			{
				if (isFixed)
					result.Append($"IsFixed = true");
				else
					result.Append($"IsFixed = false");
			}
			else
			{
				if (isFixed)
					result.Append($"Length = {length}, IsFixed = true");
				else
					result.Append($"Length = {length}, IsFixed = false");
			}
		}

		private void AppendAutofield(StringBuilder result, ref bool first)
		{
			AppendComma(result, ref first);
			result.Append("AutoField = true");
		}

		private void AppendEntityName(StringBuilder result, DBColumn column, ref bool first)
        {
			if (!string.IsNullOrWhiteSpace(column.EntityName) && !string.Equals(column.ColumnName, column.EntityName, StringComparison.Ordinal))
			{
				if (!string.Equals(column.EntityName, column.DBDataType, StringComparison.Ordinal))
				{
					AppendComma(result, ref first);
					result.Append($"ColumnName = \"{column.EntityName}\"");
				}
			}
		}

		private void AppendPrecision(StringBuilder result, int NumericPrecision, int NumericScale, ref bool first)
		{
			AppendComma(result, ref first);

			result.Append($"Precision={NumericPrecision}, Scale={NumericScale}");
		}
		#endregion
		/// <summary>
		/// Emits the code for a standard controller.
		/// </summary>
		/// <param name="resourceClass">The <see cref="ResourceClassFile"/> associated with the controller.</param>
		/// <param name="moniker">The company monier used in various headers</param>
		/// <param name="controllerClassName">The class name for the controller</param>
		/// <param name="ValidatorInterface">The validiator interface used for validations</param>
		/// <param name="policy">The authentication policy used by the controller</param>
		/// <param name="ValidationNamespace">The validation namespace/param>
		/// <returns></returns>
		public string EmitController(ResourceClass resourceClass, string moniker, string controllerClassName, string policy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			StringBuilder results = new StringBuilder();
			var nn = new NameNormalizer(resourceClass.ClassName);
			var pkcolumns = resourceClass.Entity.Columns.Where(c => c.IsPrimaryKey);

			BuildControllerInterface(resourceClass.ClassName, resourceClass.Namespace);
			BuildControllerOrchestration(resourceClass.ClassName, resourceClass.Namespace);

			// --------------------------------------------------------------------------------
			//	Class
			// --------------------------------------------------------------------------------

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClass.ClassName} Controller");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine("\t[ApiVersion(\"1.0\")]");
			results.AppendLine($"\tpublic class {controllerClassName} : COFRSController");
			results.AppendLine("\t{");
			results.AppendLine("\t\t///\t<value>A generic interface for logging where the category name is derrived from");
			results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name.</value>");
			results.AppendLine($"\t\tprivate readonly ILogger<{controllerClassName}> Logger;");
			results.AppendLine();
			results.AppendLine("\t\t///\t<value>The interface to the orchestration layer.</value>");
			results.AppendLine($"\t\tprotected readonly IServiceOrchestrator Orchestrator;");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	Constructor
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInstantiates a {controllerClassName}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"logger\">A generic interface for logging where the category name is derrived from");
			results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name. The logger is activated from dependency injection.</param>");
			results.AppendLine("\t\t///\t<param name=\"orchestrator\">The <see cref=\"IServiceOrchestrator\"/> interface for the Orchestration layer. The orchestrator is activated from dependency injection.</param>");
			results.AppendLine($"\t\tpublic {controllerClassName}(ILogger<{controllerClassName}> logger, IServiceOrchestrator orchestrator)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger = logger;");
			results.AppendLine("\t\t\tOrchestrator = orchestrator;");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Collection Endpoint
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tReturns a collection of {nn.PluralForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<remarks>This call supports RQL. The call will only return up to a maximum of \"QueryLimit\" records, where the value of the query limit is predefined in the service and cannot be changed by the user.</remarks>");
			results.AppendLine($"\t\t///\t<response code=\"200\">Returns a collection of {nn.PluralForm}</response>");
			results.AppendLine("\t\t[HttpGet]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedCollection<{resourceClass.ClassName}>))]");
			results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}CollectionExample))]");

			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Get{nn.PluralForm}Async()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.QueryString.Value);");
			results.AppendLine();
			results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
			results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
			results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
			results.AppendLine();

			results.AppendLine($"\t\t\tvar collection = await Orchestrator.Get{resourceClass.ClassName}CollectionAsync(Request.QueryString.Value, node, User);");
			results.AppendLine($"\t\t\treturn Ok(collection);");
			results.AppendLine("\t\t}");

			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Single Endpoint
			// --------------------------------------------------------------------------------

			if (pkcolumns.Count() > 0)
			{
				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tReturns a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine("\t\t///\t<remarks>This call supports RQL. Use the RQL select clause to limit the members returned.</remarks>");
				results.AppendLine($"\t\t///\t<response code=\"200\">Returns the specified {nn.SingleForm}.</response>");
				results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
				results.AppendLine("\t\t[HttpGet]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");
				results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");

				results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.NotFound)]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine("\t\t[SupportRQL]");

				EmitEndpoint(resourceClass.ClassName, "Get", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.QueryString.Value));");
				results.AppendLine();

				results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
				results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
				results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
				results.AppendLine($"\t\t\tvar item = await Orchestrator.Get{resourceClass.ClassName}Async(node, User);");
				results.AppendLine();
				results.AppendLine("\t\t\tif (item == null)");
				results.AppendLine("\t\t\t\treturn NotFound();");
				results.AppendLine();
				results.AppendLine("\t\t\treturn Ok(item);");

				results.AppendLine("\t\t}");
				results.AppendLine();
			}

			// --------------------------------------------------------------------------------
			//	POST Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tAdds a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Add a {nn.SingleForm} to the datastore.</remarks>");
			results.AppendLine($"\t\t///\t<response code=\"201\">Created - returned when the new {nn.SingleForm} was successfully added to the datastore.</response>");
			results.AppendLine("\t\t[HttpPost]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({resourceClass.ClassName}))]");
			results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
			results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.Created, typeof({resourceClass.ClassName}Example))]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Add{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tLogContext.PushProperty(\"Item\", JsonSerializer.Serialize(item));");
			results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
			results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
			results.AppendLine();
			results.AppendLine($"\t\t\titem = await Orchestrator.Add{resourceClass.ClassName}Async(item, User);");
			results.AppendLine($"\t\t\treturn Created(item.HRef.AbsoluteUri, item);");

			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	PUT Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
			results.AppendLine($"\t\t///\t<response code=\"200\">OK - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
			results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
			results.AppendLine("\t\t[HttpPut]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");


			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");
			results.AppendLine($"\t\t[SwaggerRequestExample(typeof({resourceClass.ClassName}), typeof({resourceClass.ClassName}Example))]");
			results.AppendLine($"\t\t[SwaggerResponseExample((int)HttpStatusCode.OK, typeof({resourceClass.ClassName}Example))]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IActionResult> Update{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tLogContext.PushProperty(\"Item\", JsonSerializer.Serialize(item));");
			results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
			results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
			results.AppendLine();

			results.AppendLine($"\t\t\titem = await Orchestrator.Update{resourceClass.ClassName}Async(item, User);");
			results.AppendLine($"\t\t\treturn Ok(item);");

			results.AppendLine("\t\t}");
			results.AppendLine();

			if (pkcolumns.Count() > 0)
			{
				// --------------------------------------------------------------------------------
				//	PATCH Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm} using patch commands");
				results.AppendLine("\t\t///\t</summary>");
				EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine("\t\t///\t<param name=\"commands\">The patch commands to perform.</param>");
				results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully updated in the datastore.</response>");
				results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
				results.AppendLine("\t\t[HttpPatch]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerRequestExample(typeof(IEnumerable<PatchCommand>), typeof({resourceClass.ClassName}PatchExample))]");
				results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				EmitEndpoint(resourceClass.ClassName, "Patch", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");
				results.AppendLine();
				results.AppendLine("\t\t\tLogContext.PushProperty(\"Commands\", JsonSerializer.Serialize(commands));");
				results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
				results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
				results.AppendLine();

				results.AppendLine($"\t\t\tawait Orchestrator.Patch{resourceClass.ClassName}Async(commands, node, User);");
				results.AppendLine($"\t\t\treturn NoContent();");
				results.AppendLine("\t\t}");
				results.AppendLine();

				// --------------------------------------------------------------------------------
				//	DELETE Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tDelete a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				EmitEndpointExamples(resourceClass.Entity.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine($"\t\t///\t<remarks>Deletes a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine($"\t\t///\t<response code=\"204\">No Content - returned when the {nn.SingleForm} was successfully deleted from the datastore.</response>");
				results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
				results.AppendLine("\t\t[HttpDelete]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy) && !policy.Equals("Anonymous", StringComparison.OrdinalIgnoreCase))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				EmitEndpoint(resourceClass.ClassName, "Delete", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"HRef=uri:\\\"/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\\\"\");");
				results.AppendLine();
				results.AppendLine("\t\t\tLogContext.PushProperty(\"RqlNode\", node);");
				results.AppendLine("\t\t\tLogContext.PushProperty(\"ClaimsPrincipal\", User.ListClaims());");
				results.AppendLine("\t\t\tLogger.LogInformation(\"{s1} {s2}\", Request.Method, Request.Path);");
				results.AppendLine();

				results.AppendLine($"\t\t\tawait Orchestrator.Delete{resourceClass.ClassName}Async(node, User);");
				results.AppendLine($"\t\t\treturn NoContent();");

				results.AppendLine("\t\t}");
				results.AppendLine("\t}");
			}

			return results.ToString();
		}

		private static void BuildControllerInterface(string resourceClassName, string resourceNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			ProjectItem orchInterface = mDte.Solution.FindProjectItem("IServiceOrchestrator.cs");
			orchInterface.Open(EnvDTE.Constants.vsViewKindCode);
			FileCodeModel2 fileCodeModel = (FileCodeModel2)orchInterface.FileCodeModel;

			//  Ensure that the interface contains all the required imports
			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Threading.Tasks")) == null)
				fileCodeModel.AddImport("System.Threading.Tasks");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Security.Claims")) == null)
				fileCodeModel.AddImport("System.Security.Claims");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Collections.Generic")) == null)
				fileCodeModel.AddImport("System.Collections.Generic");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(resourceNamespace)) == null)
				fileCodeModel.AddImport(resourceNamespace);

			//  Ensure all the required functions are present
			foreach (CodeNamespace orchestrationNamespace in fileCodeModel.CodeElements.OfType<CodeNamespace>())
			{
				CodeInterface2 orchestrationInterface = orchestrationNamespace.Children
																			  .OfType<CodeInterface2>()
																			  .FirstOrDefault(c => c.Name.Equals("IServiceOrchestrator"));

				if (orchestrationInterface != null)
				{
					if (orchestrationInterface.Name.Equals("IServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
					{
						// List<DBColumn> primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();

						string deleteFunctionName = $"Delete{resourceClassName}Async";
						string patchFunctionName = $"Patch{resourceClassName}Async";
						string updateFunctionName = $"Update{resourceClassName}Async";
						string addFunctionName = $"Add{resourceClassName}Async";
						string getSingleFunctionName = $"Get{resourceClassName}Async";
						string collectionFunctionName = $"Get{resourceClassName}CollectionAsync";

						string article = resourceClassName.ToLower().StartsWith("a") ||
										 resourceClassName.ToLower().StartsWith("e") ||
										 resourceClassName.ToLower().StartsWith("i") ||
										 resourceClassName.ToLower().StartsWith("o") ||
										 resourceClassName.ToLower().StartsWith("u") ? "an" : "a";

						try
						{
							#region Get Single Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(getSingleFunctionName)) == null)
							{
								var theGetSingleFunction = (CodeFunction2)orchestrationInterface.AddFunction(getSingleFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task<{resourceClassName}>",
														  -1);

								theGetSingleFunction.AddParameter("node", "RqlNode", -1);
								theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously gets {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
								doc.AppendLine("</summary>");
								doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>");
								doc.AppendLine($"<returns>The specified {resourceClassName} resource.</returns>");
								doc.AppendLine("</doc>");

								theGetSingleFunction.DocComment = doc.ToString();
							}
							#endregion

							#region Get Collection Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(collectionFunctionName)) == null)
							{
								var theCollectionFunction = (CodeFunction2)orchestrationInterface.AddFunction(collectionFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task<PagedCollection<{resourceClassName}>>",
														  -1);

								theCollectionFunction.AddParameter("originalQuery", "string", -1);
								theCollectionFunction.AddParameter("node", "RqlNode", -1);
								theCollectionFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously gets a collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.");
								doc.AppendLine("</summary>");
								doc.AppendLine("<param name=\"originalQuery\">The original query string.</param>");
								doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection.</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
								doc.AppendLine($"<returns>The collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.</returns>");
								doc.AppendLine("</doc>");

								theCollectionFunction.DocComment = doc.ToString();
							}
							#endregion

							#region Add Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(addFunctionName)) == null)
							{
								var theAddFunction = (CodeFunction2)orchestrationInterface.AddFunction(addFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task<{resourceClassName}>",
														  -1);

								theAddFunction.AddParameter("item", resourceClassName, -1);
								theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously adds {article} {resourceClassName} resource.");
								doc.AppendLine("</summary>");
								doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to add.</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
								doc.AppendLine("<returns>The newly added resource.</returns>");
								doc.AppendLine("</doc>");

								theAddFunction.DocComment = doc.ToString();
							}
							#endregion

							#region Update Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(updateFunctionName)) == null)
							{
								var theUpdateFunction = (CodeFunction2)orchestrationInterface.AddFunction(updateFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task<{resourceClassName}>",
														  -1);

								theUpdateFunction.AddParameter("item", resourceClassName, -1);
								theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource.");
								doc.AppendLine("</summary>");
								doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to update.</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
								doc.AppendLine("<returns>The updated item.</returns>");
								doc.AppendLine("</doc>");

								theUpdateFunction.DocComment = doc.ToString();
							}
							#endregion

							#region Patch Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(patchFunctionName)) == null)
							{
								var thePatchFunction = (CodeFunction2)orchestrationInterface.AddFunction(patchFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task",
														  -1);

								thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
								thePatchFunction.AddParameter("node", "RqlNode", -1);
								thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource using patch commands.");
								doc.AppendLine("</summary>");
								doc.AppendLine("<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>");
								doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
								doc.AppendLine("</doc>");

								thePatchFunction.DocComment = doc.ToString();
							}
							#endregion

							#region Delete Function
							if (orchestrationInterface.Children.OfType<CodeFunction2>().FirstOrDefault(c => c.Name.Equals(deleteFunctionName)) == null)
							{
								var theDeleteFunction = (CodeFunction2)orchestrationInterface.AddFunction(deleteFunctionName,
														  vsCMFunction.vsCMFunctionFunction,
														  $"Task", -1);

								theDeleteFunction.AddParameter("node", "RqlNode", -1);
								theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);

								StringBuilder doc = new StringBuilder();
								doc.AppendLine("<doc>");
								doc.AppendLine("<summary>");
								doc.AppendLine($"Asynchronously deletes {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
								doc.AppendLine("</summary>");
								doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection of resources to delete.</param>");
								doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
								doc.AppendLine("</doc>");

								theDeleteFunction.DocComment = doc.ToString();
							}
							#endregion
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		private static void BuildControllerOrchestration(string resourceClassName, string resourceNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var mDte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

			ProjectItem orchCode = mDte.Solution.FindProjectItem("ServiceOrchestrator.cs");
			orchCode.Open(EnvDTE.Constants.vsViewKindCode);
			var fileCodeModel = (FileCodeModel2)orchCode.FileCodeModel;

			//  Ensure that the interface contains all the required imports
			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Threading.Tasks")) == null)
				fileCodeModel.AddImport("System.Threading.Tasks");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Security.Claims")) == null)
				fileCodeModel.AddImport("System.Security.Claims");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Collections.Generic")) == null)
				fileCodeModel.AddImport("System.Collections.Generic");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("System.Text.Json")) == null)
				fileCodeModel.AddImport("System.Text.Json");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Microsoft.Extensions.DependencyInjection")) == null)
				fileCodeModel.AddImport("Microsoft.Extensions.DependencyInjection");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Microsoft.Extensions.Logging")) == null)
				fileCodeModel.AddImport("Microsoft.Extensions.Logging");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals("Serilog.Context")) == null)
				fileCodeModel.AddImport("Serilog.Context");

			if (fileCodeModel.CodeElements.OfType<CodeImport>().FirstOrDefault(c => c.Namespace.Equals(resourceNamespace)) == null)
				fileCodeModel.AddImport(resourceNamespace);

			//  Add all the functions
			foreach (CodeNamespace orchestrattorNamespace in fileCodeModel.CodeElements.OfType<CodeNamespace>())
			{
				CodeClass2 orchestratorClass = orchestrattorNamespace.Children
																	 .OfType<CodeClass2>()
																	 .FirstOrDefault(c => c.Name.Equals("ServiceOrchestrator"));

				if (orchestratorClass != null)
				{
					//var primaryKeyColumns = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey == true).ToList();

					var deleteFunctionName = $"Delete{resourceClassName}Async";
					var patchFunctionName = $"Patch{resourceClassName}Async";
					var updateFunctionName = $"Update{resourceClassName}Async";
					var addFunctionName = $"Add{resourceClassName}Async";
					var getSingleFunctionName = $"Get{resourceClassName}Async";
					var collectionFunctionName = $"Get{resourceClassName}CollectionAsync";

					var article = resourceClassName.ToLower().StartsWith("a") ||
								  resourceClassName.ToLower().StartsWith("e") ||
								  resourceClassName.ToLower().StartsWith("i") ||
								  resourceClassName.ToLower().StartsWith("o") ||
								  resourceClassName.ToLower().StartsWith("u") ? "an" : "a";

					EditPoint2 editPoint;

                    try
                    {
                        #region Get Single Function
                        if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(getSingleFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var theGetSingleFunction = (CodeFunction2)orchestratorClass.AddFunction(getSingleFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task<{resourceClassName}>",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							theGetSingleFunction.AddParameter("node", "RqlNode", -1);
							theGetSingleFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously gets {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
							doc.AppendLine("</summary>");
							doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function</param>");
							doc.AppendLine($"<returns>The specified {resourceClassName} resource.</returns>");
							doc.AppendLine("</doc>");

							theGetSingleFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)theGetSingleFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)theGetSingleFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{getSingleFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"return await GetSingleAsync<{resourceClassName}>(node);");
						}
						#endregion

						#region Get Collection Function
						if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(collectionFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var theCollectionFunction = (CodeFunction2)orchestratorClass.AddFunction(collectionFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task<PagedCollection<{resourceClassName}>>",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							theCollectionFunction.AddParameter("originalQuery", "string", -1);
							theCollectionFunction.AddParameter("node", "RqlNode", -1);
							theCollectionFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously gets a collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.");
							doc.AppendLine("</summary>");
							doc.AppendLine("<param name=\"originalQuery\">The original query string.</param>");
							doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection.</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
							doc.AppendLine($"<returns>The collection of {resourceClassName} resources filtered by the <see cref=\"RqlNode\"/>.</returns>");
							doc.AppendLine("</doc>");

							theCollectionFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)theCollectionFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)theCollectionFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{collectionFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"return await GetCollectionAsync<{resourceClassName}>(originalQuery, node);");
						}
						#endregion

						#region Add Function
						if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(addFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var theAddFunction = (CodeFunction2)orchestratorClass.AddFunction(addFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task<{resourceClassName}>",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							theAddFunction.AddParameter("item", resourceClassName, -1);
							theAddFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously adds {article} {resourceClassName} resource.");
							doc.AppendLine("</summary>");
							doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to add.</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
							doc.AppendLine("<returns>The newly added resource.</returns>");
							doc.AppendLine("</doc>");

							theAddFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)theAddFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)theAddFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{addFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"return await AddAsync<{resourceClassName}>(item);");
						}
						#endregion

						#region Update Function
						if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(updateFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var theUpdateFunction = (CodeFunction2)orchestratorClass.AddFunction(updateFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task<{resourceClassName}>",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							theUpdateFunction.AddParameter("item", resourceClassName, -1);
							theUpdateFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource.");
							doc.AppendLine("</summary>");
							doc.AppendLine($"<param name=\"item\">The {resourceClassName} resource to update.</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
							doc.AppendLine("<returns>The updated item.</returns>");
							doc.AppendLine("</doc>");

							theUpdateFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)theUpdateFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)theUpdateFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{updateFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"return await UpdateAsync<{resourceClassName}>(item);");
						}
						#endregion

						#region Patch Function
						if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(patchFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var thePatchFunction = (CodeFunction2)orchestratorClass.AddFunction(patchFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							thePatchFunction.AddParameter("commands", "IEnumerable<PatchCommand>", -1);
							thePatchFunction.AddParameter("node", "RqlNode", -1);
							thePatchFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously updates {article} {resourceClassName} resource using patch commands.");
							doc.AppendLine("</summary>");
							doc.AppendLine("<param name=\"commands\">The list of <see cref=\"PatchCommand\"/>s to perform.</param>");
							doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
							doc.AppendLine("</doc>");

							thePatchFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)thePatchFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)thePatchFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{patchFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"await PatchAsync<{resourceClassName}>(commands, node);");
						}
						#endregion

						#region Delete Function
						if (orchestratorClass.Children
											 .OfType<CodeFunction2>()
											 .FirstOrDefault(c => c.Name.Equals(deleteFunctionName, StringComparison.OrdinalIgnoreCase)) == null)
						{
							var theDeleteFunction = (CodeFunction2)orchestratorClass.AddFunction(deleteFunctionName,
													  vsCMFunction.vsCMFunctionFunction,
													  $"Task",
													  -1,
													  vsCMAccess.vsCMAccessPublic);

							theDeleteFunction.AddParameter("node", "RqlNode", -1);
							theDeleteFunction.AddParameter("User", "ClaimsPrincipal", -1);

							StringBuilder doc = new StringBuilder();
							doc.AppendLine("<doc>");
							doc.AppendLine("<summary>");
							doc.AppendLine($"Asynchronously deletes {article} {resourceClassName} resource specified by the <see cref=\"RqlNode\"/>.");
							doc.AppendLine("</summary>");
							doc.AppendLine("<param name=\"node\">The <see cref=\"RqlNode\"/> that further restricts the selection of resources to delete.</param>");
							doc.AppendLine("<param name=\"User\">The <see cref=\"ClaimsPrincipal\"/> of the actor calling the function.</param>");
							doc.AppendLine("</doc>");

							theDeleteFunction.DocComment = doc.ToString();

							editPoint = (EditPoint2)theDeleteFunction.StartPoint.CreateEditPoint();
							editPoint.ReplaceText(6, "public async", 0);

							editPoint = (EditPoint2)theDeleteFunction.EndPoint.CreateEditPoint();
							editPoint.LineUp();
							editPoint.StartOfLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"Logger.LogDebug(\"{deleteFunctionName}\");");
							editPoint.InsertNewLine();
							editPoint.Indent(null, 3);
							editPoint.Insert($"await DeleteAsync<{resourceClassName}>(node);");
						}
						#endregion
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private void EmitEndpointExamples(DBServerType serverType, string resourceClassName, StringBuilder results, IEnumerable<DBColumn> pkcolumns)
		{
			bool first = true;

			foreach (var entityColumn in pkcolumns)
			{
				if (first)
					first = false;
				else
					results.Append(", ");

				string exampleValue = "example";

				if (serverType == DBServerType.POSTGRESQL)
					exampleValue = DBHelper.GetPostgresqlExampleValue(entityColumn);
				else if (serverType == DBServerType.MYSQL)
					exampleValue = DBHelper.GetMySqlExampleValue(entityColumn);
				else if (serverType == DBServerType.SQLSERVER)
					exampleValue = DBHelper.GetSqlServerExampleValue(entityColumn);

				results.AppendLine($"\t\t///\t<param name=\"{entityColumn.EntityName}\" example=\"{exampleValue}\">The {entityColumn.EntityName} of the {resourceClassName}.</param>");
			}
		}

		private void EmitEndpoint(string resourceClassName, string action, StringBuilder results, IEnumerable<DBColumn> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IActionResult> {action}{resourceClassName}Async(");
			bool first = true;

			foreach (var entityColumn in pkcolumns)
			{
				if (first)
					first = false;
				else
					results.Append(", ");

				string dataType = entityColumn.ModelDataType;

				results.Append($"{dataType} {entityColumn.EntityName}");
			}

			if (string.Equals(action, "patch", StringComparison.OrdinalIgnoreCase))
				results.AppendLine(", [FromBody] IEnumerable<PatchCommand> commands)");
			else
				results.AppendLine(")");
		}

		private static void EmitRoute(StringBuilder results, string routeName, IEnumerable<DBColumn> pkcolumns)
		{
			results.Append($"\t\t[Route(\"{routeName}/id");

			foreach (var entityColumn in pkcolumns)
			{
				results.Append($"/{{{entityColumn.EntityName}}}");
			}

			results.AppendLine("\")]");
		}

		private static string BuildRoute(string routeName, IEnumerable<DBColumn> pkcolumns)
		{
			var route = new StringBuilder();

			route.Append(routeName);
			route.Append("/id");

			foreach (var entityColumn in pkcolumns)
			{
				route.Append($"/{{{entityColumn.ColumnName}}}");
			}

			return route.ToString();
		}
	}
}
