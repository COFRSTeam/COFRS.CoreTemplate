using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
    public class EntityClass
    {
        public string ClassName
        {
            get { return Entity.Name; }
        }
        public string SchemaName
        {
            get
            {
                if (Entity.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeAttribute2 tableAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                    if (tableAttribute != null)
                    {
                        return tableAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("Schema"))?.Value.Trim(new char[] { '"' });
                    }
                    else
                    {
                        CodeAttribute2 compositeAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                        if (compositeAttribute != null)
                        {
                            return compositeAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("Schema"))?.Value.Trim(new char[] { '"' });
                        }
                        else
                            return string.Empty;
                    }
                }
                else if (Entity.Kind == vsCMElement.vsCMElementEnum)
                {
                    CodeAttribute2 enumAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                    if (enumAttribute != null)
                    {
                        return enumAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("Schema"))?.Value.Trim(new char[] { '"' });
                    }
                    else
                        return string.Empty;
                }
                else
                    return string.Empty;
            }
        }

        public string TableName
        {
            get
            {
                if (Entity.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeAttribute2 tableAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                    if (tableAttribute != null)
                    {
                        return tableAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""))?.Value.Trim(new char[] { '"' });
                    }
                    else
                    {
                        CodeAttribute2 compositeAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                        if (compositeAttribute != null)
                        {
                            return compositeAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""))?.Value.Trim(new char[] { '"' });
                        }
                        else
                            return string.Empty;
                    }
                }
                else if (Entity.Kind == vsCMElement.vsCMElementEnum)
                {
                    CodeAttribute2 enumAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                    if (enumAttribute != null)
                    {
                        return enumAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals(""))?.Value.Trim(new char[] { '"' });
                    }
                    else
                        return string.Empty;
                }
                else
                    return string.Empty;
            }
        }

        public DBServerType ServerType
        {
            get
            {
                if (Entity.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeAttribute2 tableAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                    if (tableAttribute != null)
                    {
                        var dbType = tableAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("DBType"))?.Value.Trim(new char[] { '"' });

                        if (dbType != null)
                            return (DBServerType)Enum.Parse(typeof(DBServerType), dbType);
                        else
                            return DBServerType.SQLSERVER;
                    }
                    else
                    {
                        CodeAttribute2 compositeAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                        if (compositeAttribute != null)
                        {
                            var dbType = compositeAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("DBType"))?.Value.Trim(new char[] { '"' });

                            if (dbType != null)
                                return (DBServerType)Enum.Parse(typeof(DBServerType), dbType);
                            else
                                return DBServerType.SQLSERVER;
                        }
                        else
                            return DBServerType.SQLSERVER;
                    }
                }
                else if (Entity.Kind == vsCMElement.vsCMElementEnum)
                {
                    CodeAttribute2 enumAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgEnum"));

                    if (enumAttribute != null)
                    {
                        var dbType = enumAttribute.Children.OfType<CodeAttributeArgument>().FirstOrDefault(a => a.Name.Equals("DBType"))?.Value.Trim(new char[] { '"' });

                        if (dbType != null)
                            return (DBServerType)Enum.Parse(typeof(DBServerType), dbType);
                        else
                            return DBServerType.SQLSERVER;
                    }
                    else
                        return DBServerType.SQLSERVER;
                }
                else
                    return DBServerType.SQLSERVER;
            }
        }

        public ElementType ElementType
        {
            get
            {
                if (Entity.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeAttribute2 tableAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("Table"));

                    if (tableAttribute != null)
                    {
                        return ElementType.Table;
                    }
                    else
                    {
                        CodeAttribute2 compositeAttribute = ((CodeClass2)Entity).Attributes.OfType<CodeAttribute2>().FirstOrDefault(a => a.Name.Equals("PgComposite"));

                        if (compositeAttribute != null)
                        {
                            return ElementType.Composite;
                        }
                        else
                            return ElementType.Table;
                    }
                }
                else if (Entity.Kind == vsCMElement.vsCMElementEnum)
                {
                    return ElementType.Enum;
                }
                else
                    return ElementType.Table;
            }
        }

        public ProjectItem ProjectItem
        {
            get
            {
                return Entity.ProjectItem;
            }
        }

        public string Namespace
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Entity.Kind == vsCMElement.vsCMElementClass)
                    return ((CodeClass2)Entity).Namespace.Name;
                else
                    return ((CodeEnum)Entity).Namespace.Name;
            }
        }

        public DBColumn[] Columns
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var codeService = COFRSServiceFactory.GetService<ICodeService>();

                if (Entity.Kind == vsCMElement.vsCMElementClass)
                    return codeService.LoadEntityColumns((CodeClass2)Entity);
                else
                    return codeService.LoadEntityEnumColumns((CodeEnum)Entity);
            }
        }

        public CodeElement2 Entity { get; set; }

        public override string ToString()
        {
            return Entity.Name;
        }
    }
}
