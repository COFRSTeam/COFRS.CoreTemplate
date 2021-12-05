using COFRS.Template.Common.Wizards;
using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
    public partial class Mapper : Form
    {
        public ResourceModel ResourceModel { get; set; }
        public ResourceMap ResourceMap { get; set; }
        public EntityMap EntityMap { get; set; }
        public ProfileMap ProfileMap { get; set; }

        public DTE2 Dte { get; set; }

        public Mapper()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            OnResize(sender, e);
            ResourceClass_Lablel.Text = ResourceModel.ClassName;
            EntityClass_Label.Text = ResourceModel.EntityModel.ClassName;

            ProfileMap = COFRSCommonUtilities.OpenProfileMap(Dte, ResourceModel);

            if (ProfileMap == null)
            {
                ProfileMap = GenerateProfileMap(ResourceModel);
            }

            PopulateUI();
        }

        private void PopulateUI()
        {
            resourceGrid.Rows.Clear();
            EntityGrid.Rows.Clear();
            EntityList.Items.Clear();
            ResourceList.Items.Clear();

            var unmappedColumns = new List<DBColumn>();
            unmappedColumns.AddRange(ResourceModel.EntityModel.Columns);

            foreach (var resourceMember in ProfileMap.ResourceProfiles)
            {
                var dataRowIndex = resourceGrid.Rows.Add();
                var dataRow = resourceGrid.Rows[dataRowIndex];

                dataRow.Cells[0].Value = resourceMember.ResourceColumnName;
                dataRow.Cells[1].Value = resourceMember.MapFunction;
                dataRow.Cells[3].Value = resourceMember.EntityColumnNames?.ToCSV();

                dataRow.Cells[1].Style = new DataGridViewCellStyle() { ForeColor = resourceMember.IsDefined ? Color.Black : Color.Red };

                if (resourceMember.EntityColumnNames != null)
                {
                    foreach (var entityColumn in resourceMember.EntityColumnNames)
                    {
                        var matchedColumn = unmappedColumns.FirstOrDefault(c => c.ColumnName.Equals(entityColumn));

                        if (matchedColumn != null)
                            unmappedColumns.Remove(matchedColumn);
                    }
                }
            }

            foreach (var column in unmappedColumns)
            {
                EntityList.Items.Add(column.ColumnName);
            }

            unmappedColumns = new List<DBColumn>();
            unmappedColumns.AddRange(ResourceModel.Columns);

            foreach (var entityColumn in ProfileMap.EntityProfiles)
            {
                var dataRowIndex = EntityGrid.Rows.Add();
                var dataRow = EntityGrid.Rows[dataRowIndex];

                dataRow.Cells[0].Value = entityColumn.EntityColumnName;
                dataRow.Cells[1].Value = entityColumn.MapFunction;
                dataRow.Cells[3].Value = entityColumn.ResourceColumns?.ToCSV();
                dataRow.Cells[1].Style = new DataGridViewCellStyle() { ForeColor = entityColumn.IsDefined ? Color.Black : Color.Red };

                if (entityColumn.ResourceColumns != null)
                {
                    foreach (var resourceColumn in entityColumn.ResourceColumns)
                    {
                        var matchedColumn = unmappedColumns.FirstOrDefault(c => c.ColumnName.Equals(resourceColumn));

                        if (matchedColumn != null)
                            unmappedColumns.Remove(matchedColumn);
                    }
                }
            }

            foreach (var column in unmappedColumns)
            {
                ResourceList.Items.Add(column.ColumnName);
            }
        }

        private ProfileMap GenerateProfileMap(ResourceModel resourceModel)
        {
            ProfileMap = new ProfileMap
            {
                ResourceClassName = ResourceModel.ClassName,
                EntityClassName = ResourceModel.EntityModel.ClassName,
                ResourceProfiles = new List<ResourceProfile>(),
                EntityProfiles = new List<EntityProfile>()
            };

            ProfileMap.ResourceProfiles.AddRange(GenerateResourceFromEntityMapping(resourceModel));
            ProfileMap.EntityProfiles.AddRange(GenerateEntityFromResourceMapping(resourceModel));

            return ProfileMap;
        }

        private List<DBColumn> GetResourceModelList(ResourceModel source, string ParentName = "")
        {
            List<DBColumn> result = new List<DBColumn>();  

            foreach (var column in source.Columns)
            {
                var resourceModel = ResourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, column.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                if (resourceModel != null && resourceModel.ResourceType != ResourceType.Enum)
                {
                    string newParent = string.Empty;

                    if (string.IsNullOrWhiteSpace(ParentName))
                        newParent = column.ColumnName;
                    else
                        newParent = $"{ParentName}.{column.ColumnName}";

                    result.AddRange(GetResourceModelList(resourceModel, newParent));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(ParentName))
                        column.ColumnName = $"{ParentName}.{column.ColumnName}";

                    result.Add(column);
                }
            }

            return result;
        }

        private List<DBColumn> GetEntityModelList(EntityModel source, string ParentName = "")
        {
            List<DBColumn> unmappedColumns = new List<DBColumn>();

            foreach (var column in source.Columns)
            {
                var entityModel = EntityMap?.Maps.FirstOrDefault(r => string.Equals(r.ClassName, column.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                if (entityModel != null && entityModel.ElementType != ElementType.Enum)
                {
                    string newParent = string.Empty;

                    if (string.IsNullOrWhiteSpace(ParentName))
                        newParent = column.ColumnName;
                    else
                        newParent = $"{ParentName}.{column.ColumnName}";

                    unmappedColumns.AddRange(GetEntityModelList(entityModel, newParent));
                }
                else 
                { 
                    if (!string.IsNullOrWhiteSpace(ParentName))
                        column.ColumnName = $"{ParentName}.{column.ColumnName}";

                    unmappedColumns.Add(column);
                }
            }

            return unmappedColumns;
        }

        /// <summary>
        /// Generates a mapping to construct the entity member from the corresponding resource members
        /// </summary>
        /// <param name="unmappedColumns"></param>
        /// <param name="resourceModel"></param>
        private List<EntityProfile> GenerateEntityFromResourceMapping(ResourceModel resourceModel)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var result = new List<EntityProfile>();

            //  Let's create a mapping for each entity member
            foreach (var entityMember in ResourceModel.EntityModel.Columns)
            {
                //  Construct a data row for this entity member, and populate the column name
                var entityProfile = new EntityProfile
                {
                    EntityColumnName = entityMember.ColumnName
                };

                //  Now, construct the mapping
                if (entityMember.IsPrimaryKey)
                {
                    //  This entity member is part of the primary key. In the resource model, the primary
                    //  key is contained in the hRef. Therefore, the formula to extract the value of this
                    //  entity member is going to take the form: source.HRef.GetId<datatype>(n), were datatype
                    //  is the datatype of this entity member, and n is the position of this member in the 
                    //  HRef, counting backwards.
                    //
                    //  For example, suppose we have the HRef = /resourcename/id/10/20
                    //  Where 10 is the dealer id and it is an int.
                    //  And 20 is the user id and it is a short.
                    //
                    //  To extract the dealer id, the formula would be: source.HRef.GetId<int>(1)
                    //  To extract the user id, the formula would be: source.HRef.GetId<short>(0)
                    string dataType = "Unknown";

                    //  We're going to need that datatype. Get it here.
                    dataType = entityMember.ModelDataType;

                    //  We need the list of all the primary keys in the entity model, so that we know the
                    //  position of this member.
                    var primaryKeys = ResourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey);

                    if (primaryKeys.Count() == 1)
                    {
                        //  There is only one primary key element, so we don't need to bother with the count.
                        entityProfile.MapFunction = $"source.HRef == null ? default : source.HRef.GetId<{dataType}>()";
                        entityProfile.ResourceColumns = new string[] { "HRef" };
                        entityProfile.IsDefined = true;
                    }
                    else
                    {
                        var formula = new StringBuilder($"source.HRef == null ? default : source.HRef.GetId<{dataType}>(");

                        //  Compute the index and append it to the above formula.
                        if (primaryKeys.Count() > 1)
                        {
                            var index = primaryKeys.Count() - 1;

                            foreach (var pk in primaryKeys)
                            {
                                if (pk == entityMember)
                                    formula.Append(index.ToString());
                                else
                                    index--;
                            }
                        }

                        //  Close the formula
                        formula.Append(")");
                        entityProfile.MapFunction = formula.ToString();
                        entityProfile.ResourceColumns = new string[] { "HRef" };
                        entityProfile.IsDefined = true;
                    }
                }
                else if (entityMember.IsForeignKey)
                {
                    //  This is a special case of a foreign key. The first challenge is to discover which resource member
                    //  is used to represent this foreign key. In all likelyhood, it will be the resource member that has
                    //  the same name as the foreign table that this foreign key represents. It's probably going to be the
                    //  single form, but it could be the plural form. Look for either one.
                    var nnx = new NameNormalizer(entityMember.ForeignTableName);
                    DBColumn resourceMember = null;

                    foreach ( var resourceMap in ProfileMap.ResourceProfiles)
                    {
                        if (resourceMap.EntityColumnNames != null)
                        {
                            foreach (var entityColumnUsed in resourceMap.EntityColumnNames)
                            {
                                if (entityColumnUsed.Equals(entityMember.ColumnName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var resourceMemberName = resourceMap.ResourceColumnName;
                                    resourceMember = resourceModel.Columns.FirstOrDefault(c =>
                                        string.Equals(c.ColumnName, resourceMemberName, StringComparison.OrdinalIgnoreCase));

                                    break;
                                }
                            }
                        }

                        if (resourceMember != null)
                            break;
                    }

                    if (resourceMember != null)
                    {
                        //  We found a resource member.
                        //
                        //  Foreign keys generally come in one of two forms. Either it is a reference to a primary key
                        //  in a foreign table, or it is an enumeration. If it is a reference to a primary key in a 
                        //  foreign table, then it will be a Uri.

                        if (string.Equals(resourceMember.ModelDataType.ToString(), "uri", StringComparison.OrdinalIgnoreCase))
                        {
                            //  This is an href. First, get the data type.
                            string dataType = entityMember.ModelDataType;

                            //  Now, we need the list of entity members that correspond to this resource member.
                            //  To get that, we need to look at the resource mapping.

                            //  This is an href. Very much like the primary key, there can be more than one single 
                            //  element in this href. 

                            var foreignKeys = ResourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                                string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                            if (foreignKeys.Count() == 1)
                            {
                                var foreignKey = foreignKeys.First();
                                entityProfile.MapFunction = $"source.{resourceMember.ColumnName}.GetId<{dataType}>()";
                                entityProfile.ResourceColumns = new string[] { resourceMember.ColumnName };
                                entityProfile.IsDefined = true;
                            }
                            else
                            {
                                var formula = new StringBuilder($"source.{resourceMember.ColumnName}.GetId<{dataType}>(");
                                var foreignKeyList = new List<string>();

                                if (foreignKeys.Count() > 1)
                                {
                                    var index = foreignKeys.Count() - 1;

                                    foreach (var pk in foreignKeys)
                                    {
                                        var foreignKey = foreignKeys.ToList()[index];
                                        foreignKeyList.Add(foreignKey.ColumnName);

                                        if (pk == entityMember)
                                            formula.Append(index.ToString());
                                        else
                                            index--;
                                    }
                                }

                                formula.Append(")");
                                entityProfile.MapFunction = formula.ToString();
                                entityProfile.ResourceColumns = foreignKeyList.ToArray();
                                entityProfile.IsDefined = true;
                            }
                        }
                        else
                        {
                            //  The resource member is not a URI. It should be an enum. If it is, we sould be able to
                            //  find it in our resource models list.

                            var referenceModel = ResourceMap.Maps.FirstOrDefault(r =>
                                            string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.Ordinal));

                            if ( referenceModel != null )
                            {
                                if ( referenceModel.ResourceType == ResourceType.Enum )
                                { 
                                    var foreignKeys = ResourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                        string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                                    //  If the resource member is an enum that represents the value of the primary key of a foreign table,
                                    //  then that foreign key can only have one member. If it has more than one member, then this 
                                    //  is not the proper mapping.
                                    if (foreignKeys.Count() == 1)
                                    {
                                        string dataType = entityMember.ModelDataType;

                                        var formula = $"Convert.ChangeType(source.{resourceMember.ColumnName}, source.{resourceMember.ColumnName}.GetTypeCode())";
                                        entityProfile.MapFunction = formula.ToString();
                                        entityProfile.ResourceColumns = new string[] { "unknown" };
                                        entityProfile.IsDefined = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //  Is there a corresponding Entity Column for this resource Column?
                    var resourceMember = resourceModel.Columns.FirstOrDefault(u =>
                                                            u.ColumnName.Equals(entityMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (resourceMember != null)
                    {
                        MapResourceDestinationFromSource(entityMember, entityProfile, resourceMember);
                    }
                    else
                    {
                        var rp = ProfileMap.ResourceProfiles.FirstOrDefault(c => c.MapFunction.IndexOf(entityMember.ColumnName, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

                        if (rp != null)
                        {
                            if (rp.ResourceColumnName.Contains("."))
                            {
                                var parts = rp.ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                StringBuilder mapFunction = new StringBuilder();
                                StringBuilder parent = new StringBuilder("");
                                string NullValue = "null";

                                var parentModel = GetParentModel(ResourceModel, parts);

                                if (parentModel != null)
                                {
                                    var parentColumn = parentModel.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[parts.Count() - 1], StringComparison.OrdinalIgnoreCase));

                                    if (parentColumn != null)
                                    {
                                        if (string.Equals(parentColumn.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
                                        {
                                            NullValue = "string.Empty";
                                        }
                                        else
                                        {
                                            var theDataType = Type.GetType(parentColumn.ModelDataType.ToString());

                                            if (theDataType != null)
                                            {
                                                NullValue = "default";
                                            }
                                            else
                                            {
                                                NullValue = "default";
                                            }
                                        }
                                    }
                                }

                                for (int i = 0; i < parts.Count() - 1; i++)
                                {
                                    var parentClass = parts[i];

                                    if (string.IsNullOrWhiteSpace(parent.ToString()))
                                        mapFunction.Append($"source.{parentClass} == null ? {NullValue} : ");
                                    else
                                        mapFunction.Append($"source.{parent}.{parentClass} == null ? {NullValue} : ");
                                    parent.Append($"source.{parentClass}");

                                }

                                mapFunction.Append($"{parent}.{parts[parts.Count() - 1]}");
                                entityProfile.MapFunction = mapFunction.ToString();

                                StringBuilder childColumn = new StringBuilder();

                                foreach (var p in parts)
                                {
                                    if (childColumn.Length > 0)
                                        childColumn.Append(".");
                                    childColumn.Append(p);
                                }

                                var cc = parentModel.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, childColumn.ToString(), StringComparison.OrdinalIgnoreCase));
                                if (cc != null)
                                {
                                    entityProfile.ResourceColumns = new string[] { cc.ColumnName };
                                }
                            }
                            else
                            {
                                StringBuilder mc = new StringBuilder();

                                resourceMember = resourceModel.Columns.FirstOrDefault(c =>
                                    string.Equals(c.ModelDataType.ToString(), rp.ResourceColumnName, StringComparison.OrdinalIgnoreCase));

                                MapResourceDestinationFromSource(entityMember, entityProfile, resourceMember);
                            }
                        }
                    }
                }

                result.Add(entityProfile);  
            }

            return result;
        }

        private List<ResourceProfile> GenerateResourceFromEntityMapping(ResourceModel resourceModel)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var nn = new NameNormalizer(resourceModel.ClassName);
            var result = new List<ResourceProfile>();

            foreach (var resourceMember in resourceModel.Columns)
            {
                var resourceProfile = new ResourceProfile
                {
                    ResourceColumnName = resourceMember.ColumnName
                };

                //  If this is the HRef (the primary key), then the columns that comprise it are all the primary
                //  key values of the entity in the order in which they appear in the entity
                if (resourceMember.IsPrimaryKey)
                {
                    var primaryKeys = resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey);
                    var formula = new StringBuilder($"new Uri(rootUrl, $\"{nn.PluralCamelCase}/id");
                    var sourceColumns = new List<string>();

                    foreach (var entityMember in primaryKeys)
                    {
                        formula.Append($"/{{source.{entityMember.ColumnName}}}");
                        sourceColumns.Add(entityMember.ColumnName);
                    }

                    formula.Append("\")");
                    resourceProfile.MapFunction = formula.ToString();
                    resourceProfile.EntityColumnNames = sourceColumns.ToArray();
                    resourceProfile.IsDefined = true;
                }

                //  If this column represents a foreign key, then the entity members that comprise it will be
                //  those entity members that are foreign keys, that have the same table name as this members name.
                else if (resourceMember.IsForeignKey)
                {
                    //  A foreign key is commonly represented in one of two forms. Either it is a hypertext reference
                    //  (an href), in which case the resource member should be a Uri, or it is an enum.

                    //  If it is an enum, there will be a resource model of type enum, whose corresponding entity model
                    //  will point to the foreign table.

                    var enumResource = ResourceMap.Maps.FirstOrDefault(r =>
                                       r.ResourceType == ResourceType.Enum &&
                                       r.EntityModel != null && 
                                       string.Equals(r.EntityModel.TableName, resourceMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                    if (enumResource != null)
                    {
                        var sourceColumns = new List<string>();
                        var formula = $"({enumResource.ClassName})source.{resourceMember.ColumnName}";
                        //  Need to think about this...
                        sourceColumns.Add(enumResource.EntityModel.ClassName);
                        resourceProfile.MapFunction = formula.ToString();
                        resourceProfile.EntityColumnNames = sourceColumns.ToArray();
                        resourceProfile.IsDefined = true;
                    }
                    else
                    {
                        if (string.Equals(resourceMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
                        {
                            var nnn = new NameNormalizer(resourceMember.ColumnName);
                            var foreignKeys = resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                string.Equals(c.ForeignTableName, resourceMember.ForeignTableName, StringComparison.Ordinal));

                            var formula = new StringBuilder($"new Uri(rootUrl, $\"{nnn.PluralCamelCase}/id");
                            var sourceColumns = new List<string>();

                            foreach (var entityMember in foreignKeys)
                            {
                                formula.Append($"/{{source.{entityMember.ColumnName}}}");
                                sourceColumns.Add(entityMember.ColumnName);
                            }

                            formula.Append("\")");
                            resourceProfile.MapFunction = formula.ToString();
                            resourceProfile.EntityColumnNames = sourceColumns.ToArray();
                            resourceProfile.IsDefined = true;
                        }
                        else
                        {
                            //  This is probably an Enum. If it is, we should be able to find it in the list of 
                            //  resource models.
                            var referenceModel = ResourceMap.Maps.FirstOrDefault(r =>
                                    string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.Ordinal));

                            if (referenceModel != null)
                            {
                                if (referenceModel.ResourceType == ResourceType.Enum)
                                {
                                    var nnn = new NameNormalizer(resourceMember.ColumnName);
                                    var foreignKeys = resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                             (string.Equals(c.ForeignTableName, nnn.SingleForm, StringComparison.Ordinal) ||
                                               string.Equals(c.ForeignTableName, nnn.PluralForm, StringComparison.Ordinal)));

                                    if (foreignKeys.Count() == 1)
                                    {
                                        var entityMember = foreignKeys.ToList()[0];

                                        var formula = $"({referenceModel.ClassName}) source.{entityMember.ColumnName}";
                                        resourceProfile.MapFunction = formula.ToString();
                                        resourceProfile.EntityColumnNames = new string[] { entityMember.ColumnName };
                                        resourceProfile.IsDefined = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //  Is there an existing entityMember whos column name matches the resource member?
                    var entityMember = resourceModel.EntityModel.Columns.FirstOrDefault(u =>
                        string.Equals(u.ColumnName, resourceMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (entityMember != null)
                    {
                        //  There is, just assign it.
                        MapEntityDestinationFromSource(resourceMember, resourceProfile, entityMember);
                    }
                    else
                    {
                        //  Is this resource member a class?
                        var model = ResourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                        if (model != null)
                        {
                            //  It is a class, instantiate the class
                            resourceProfile.MapFunction = $"new {model.ClassName}()";

                            //  Now, go map all of it's children
                            MapEntityChildMembers(resourceMember, resourceProfile, model, resourceMember.ColumnName);
                        }
                        else
                        {
                            if (resourceMember.ModelDataType.Contains("[]"))
                            {
                                var className = resourceMember.ModelDataType.Remove(resourceMember.ModelDataType.IndexOf('['), 2);
                                resourceProfile.MapFunction = $"Array.Empty<{className}>()";
                                resourceProfile.EntityColumnNames = Array.Empty<string>();
                                resourceProfile.IsDefined = true;
                            }
                            else if (resourceMember.ModelDataType.Contains("List<"))
                            {
                                var index = resourceMember.ModelDataType.IndexOf('<');
                                var count = resourceMember.ModelDataType.IndexOf('>') - index;
                                var className = resourceMember.ModelDataType.Substring(index + 1, count - 1);
                                resourceProfile.MapFunction = $"new List<{className}>()";
                                resourceProfile.EntityColumnNames = Array.Empty<string>();
                                resourceProfile.IsDefined = true;
                            }
                            else if (resourceMember.ModelDataType.Contains("IEnumerable<"))
                            {
                                var index = resourceMember.ModelDataType.IndexOf('<');
                                var count = resourceMember.ModelDataType.IndexOf('>') - index;
                                var className = resourceMember.ModelDataType.Substring(index + 1, count - 1);
                                resourceProfile.MapFunction = $"Array.Empty<{className}>()";
                                resourceProfile.EntityColumnNames = Array.Empty<string>();
                                resourceProfile.IsDefined = true;
                            }
                        }
                    }
                }

                result.Add(resourceProfile);
            }

            return result;
        }

        private void OnResize(object sender, EventArgs e)
        {
            Explanation.Width = ClientRectangle.Right - 12 - Explanation.Left;
            ResourceClass_Lablel.Width = ClientRectangle.Right - 4 - ResourceClass_Lablel.Left;
            EntityClass_Label.Width = ClientRectangle.Right - 4 - EntityClass_Label.Left;

            panel1.Top = ClientRectangle.Height - (725 - 676);
            panel1.Left = -10;
            panel1.Width = ClientRectangle.Width + 20;
            panel1.Height = ClientRectangle.Height - panel1.Top + 20;

            tabControl.Width = ClientRectangle.Right - 12 - tabControl.Left;
            tabControl.Height = panel1.Top - tabControl.Top - 12;

            _cancelButton.Top = (ClientRectangle.Height - panel1.Top - _cancelButton.Height) / 2;
            _cancelButton.Left = ClientRectangle.Right - 12 - _cancelButton.Width;

            _okButton.Top = (ClientRectangle.Height - panel1.Top - _okButton.Height) / 2;
            _okButton.Left = _cancelButton.Left - 12 - _okButton.Width;

            var _tabControlWidth = tabControl.Width - 16;
            var segmentWidth = _tabControlWidth / 12;

            var buttonWidth = 45;
            
            AssignButton.Top = 71;
            ResourceAssign.Top = 71;
            UnassignButton.Top = 118;
            ResourceUnassign.Top = 118;

            if ( buttonWidth < segmentWidth - 16 )
            {
                AssignButton.Width = buttonWidth;
                UnassignButton.Width = buttonWidth;
                ResourceAssign.Width = buttonWidth;
                ResourceUnassign.Width = buttonWidth;

                AssignButton.Left = 8 + segmentWidth * 8 + (segmentWidth - buttonWidth) / 2;
                UnassignButton.Left = 8 + segmentWidth * 8 + (segmentWidth - buttonWidth) / 2;
                ResourceAssign.Left = 8 + segmentWidth * 8 + (segmentWidth - buttonWidth) / 2;
                ResourceUnassign.Left = 8 + segmentWidth * 8 + (segmentWidth - buttonWidth) / 2;
            }
            else
            {
                AssignButton.Width = segmentWidth - 16;
                UnassignButton.Width = segmentWidth - 16;
                AssignButton.Left = 8 + segmentWidth * 8 + 8;
                UnassignButton.Left = 8 + segmentWidth * 8 + 8;

                ResourceAssign.Width = segmentWidth - 16;
                ResourceUnassign.Width = segmentWidth - 16;
                ResourceAssign.Left = 8 + segmentWidth * 8 + 8;
                ResourceUnassign.Left = 8 + segmentWidth * 8 + 8;
            }

            resourceGrid.Left = 8;
            resourceGrid.Top = 20;
            resourceGrid.Width = AssignButton.Left - 16;
            resourceGrid.Height = tabPage1.Height - 24;

            EntityGrid.Left = 8;
            EntityGrid.Top = 20;
            EntityGrid.Width = ResourceAssign.Left - 16;
            EntityGrid.Height = tabPage1.Height - 24;

            EntityList.Left = AssignButton.Left + AssignButton.Width + 8;
            EntityList.Top = 20;
            EntityList.Width = tabControl.Width - EntityList.Left - 16;
            EntityList.Height = tabPage1.Height - 24;

            ResourceList.Left = ResourceAssign.Left + ResourceAssign.Width + 8;
            ResourceList.Top = 20;
            ResourceList.Width = tabControl.Width - ResourceList.Left - 16;
            ResourceList.Height = tabPage1.Height - 24;

            ResourceMemberLabel.Left = resourceGrid.Left;
            ResourceMemberLabel.Top = 0;
            UnmappedEntityMembersLabel.Left = EntityList.Left;
            UnmappedEntityMembersLabel.Top = 0;

            EntityMembersLabel.Left = resourceGrid.Left;
            EntityMembersLabel.Top = 0;
            UnmappedResourceMembersLabel.Left = EntityList.Left;
            UnmappedResourceMembersLabel.Top = 0;
        }

        private void OnOK(object sender, EventArgs e)
        {
            COFRSCommonUtilities.SaveProfileMap(Dte, ProfileMap);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #region Mapping Helper Functions
        private ResourceModel GetParentModel(ResourceModel parent, string[] parts)
        {
            ResourceModel result = parent;

            for (int i = 0; i < parts.Count() - 1; i++)
            {
                var column = result.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[i], StringComparison.OrdinalIgnoreCase));

                if (column != null)
                {
                    result = ResourceMap.Maps.FirstOrDefault(p => string.Equals(p.ClassName, column.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));
                }
            }

            return result;
        }

        //private void MapResourceChildMembers(DBColumn member, ResourceModel model, string parent = "")
        //{
        //    Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        //    //  We have a model, and the parent column name.

        //    //  Map the children
        //    foreach (var childMember in model.Columns)
        //    {
        //        int dataRowIndex = resourceGrid.Rows.Add();
        //        var dataRow = resourceGrid.Rows[dataRowIndex];

        //        //  Include the child in the list...
        //        dataRow.Cells[0].Value = $"{parent}.{childMember.ColumnName}";

        //        //  Do we have an existing entity member that matches the child resource column name?
        //        var entityMember = model.EntityModel.Columns.FirstOrDefault(u =>
        //            string.Equals(u.ColumnName, childMember.ColumnName, StringComparison.OrdinalIgnoreCase));

        //        if (entityMember != null)
        //        {
        //            //  We do, just assign it
        //            MapResourceDestinationFromSource(childMember, entityProfile, entityMember);
        //        }
        //        else
        //        {
        //            var childModel = resourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, member.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

        //            if (model != null)
        //            {
        //                dataRow.Cells[1].Value = $"new {model.ClassName}()";
        //                MapResourceChildMembers(member, model, $"{parent}?.{childMember.ColumnName}");
        //            }
        //        }
        //    }
        //}

        private void MapEntityChildMembers(DBColumn member, ResourceProfile resourceProfile, ResourceModel model, string parent)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            //  We have a model, and the parent column name.

            //  Map the children
            foreach (var childMember in model.Columns)
            {
                int dataRowIndex = resourceGrid.Rows.Add();
                var dataRow = resourceGrid.Rows[dataRowIndex];

                //  Include the child in the list...
                dataRow.Cells[0].Value = $"{parent}.{childMember.ColumnName}";

                //  Do we have an existing entity member that matches the child resource column name?
                var entityMember = model.EntityModel.Columns.FirstOrDefault(u =>
                    string.Equals(u.ColumnName, childMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (entityMember != null)
                {
                    //  We do, just assign it
                    MapEntityDestinationFromSource(childMember, resourceProfile, entityMember);
                }
                else
                {
                    var childModel = ResourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, member.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (model != null)
                    {
                        dataRow.Cells[1].Value = $"new {model.ClassName}()";
                        MapEntityChildMembers(member, resourceProfile, model, $"{parent}?.{childMember.ColumnName}");
                    }
                }
            }
        }

        private void MapEntityDestinationFromSource(DBColumn destinationMember, ResourceProfile resourceProfile, DBColumn sourceMember)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var model = ResourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, destinationMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = $"source.{sourceMember.ColumnName}";
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = true;
            }
            else if (model != null)
            {
                if (model.ResourceType == ResourceType.Enum)
                {
                    if (string.Equals(sourceMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceProfile.MapFunction = $"({model.ClassName}) source.{sourceMember.ColumnName}";
                        resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                        resourceProfile.IsDefined = true;
                    }
                    else if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceProfile.MapFunction = $"Enum.Parse<{model.ClassName}>(source.{sourceMember.ColumnName})";
                        resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                        resourceProfile.IsDefined = true;
                    }
                    else
                    {
                        resourceProfile.MapFunction = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                        resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                        resourceProfile.IsDefined = false;
                    }
                }
                else
                {
                    if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ContainsParseFunction(model))
                        {
                            resourceProfile.MapFunction = $"{model.ClassName}.Parse(source.{sourceMember.ColumnName})";
                            resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                            resourceProfile.IsDefined = true;
                        }
                        else
                        {
                            resourceProfile.MapFunction = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                            resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                            resourceProfile.IsDefined = false;
                        }
                    }
                    else
                    {
                        resourceProfile.MapFunction = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                        resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                        resourceProfile.IsDefined = true;
                    }
                }
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToByte(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToShort(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToInt(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToLong(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToULong(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToChar(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToString(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToImage(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
            {
                resourceProfile.MapFunction = SourceConverter.ToUri(sourceMember, out bool isUndefined);
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = !isUndefined;
            }
            else
            {
                resourceProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                resourceProfile.EntityColumnNames = new string[] { sourceMember.ColumnName };
                resourceProfile.IsDefined = false;
            }
        }

        private void MapResourceDestinationFromSource(DBColumn destinationMember, EntityProfile entityProfile, DBColumn sourceMember)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var model = ResourceMap.Maps.FirstOrDefault(r => string.Equals(r.ClassName, sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = $"source.{sourceMember.ColumnName}";
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = true;
            }
            else if (model != null)
            {
                if (model.ResourceType == ResourceType.Enum)
                {
                    if (string.Equals(destinationMember.ModelDataType, "byte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "short", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "int", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "uint", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "long", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(destinationMember.ModelDataType, "ulong", StringComparison.OrdinalIgnoreCase))
                    {
                        entityProfile.MapFunction = $"({destinationMember.ModelDataType}) source.{sourceMember.ColumnName}";
                        entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                        entityProfile.IsDefined = true;
                    }
                    else if (string.Equals(destinationMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        entityProfile.MapFunction = $"source.{sourceMember.ColumnName}.ToString())";
                        entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                        entityProfile.IsDefined = true;
                    }
                    else
                    {
                        entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                        entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                        entityProfile.IsDefined = false;
                    }
                }
                else
                {
                    if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ContainsParseFunction(model))
                        {
                            entityProfile.MapFunction = $"{model.ClassName}.Parse(source.{sourceMember.ColumnName})";
                            entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                            entityProfile.IsDefined = true;
                        }
                        else
                        {
                            entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                            entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                            entityProfile.IsDefined = false;
                        }
                    }
                    else
                    {
                        entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                        entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                        entityProfile.IsDefined = true;
                    }
                }
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToByte(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = true;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToShort(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToInt(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToLong(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToULong(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToChar(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToString(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToImage(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
            {
                entityProfile.MapFunction = SourceConverter.ToUri(sourceMember, out bool isUndefined);
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = !isUndefined;
            }
            else
            {
                entityProfile.MapFunction = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                entityProfile.ResourceColumns = new string[] { sourceMember.ColumnName };
                entityProfile.IsDefined = false;
            }
        }

        private bool ContainsParseFunction(ResourceModel model)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            //  Search for a static function called Parse
            var theParseFunction = model.Functions.FirstOrDefault(f => f.IsShared && string.Equals(f.Name, "parse", StringComparison.OrdinalIgnoreCase));

            if ( theParseFunction != null)
            {
                CodeTypeRef functionType = theParseFunction.Type;

                //  It should return a code type of ClassName
                if (functionType.TypeKind == vsCMTypeRef.vsCMTypeRefCodeType &&
                    string.Equals(functionType.CodeType.Name, model.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    //  It should contain only one parameter

                    if (theParseFunction.Parameters.Count == 1)
                    {
                        //  And that parameter should be of type string
                        var theParameter = (CodeParameter2) theParseFunction.Parameters.Item(1);
                        var parameterType = theParameter.Type;

                        if ( parameterType.TypeKind == vsCMTypeRef.vsCMTypeRefString)
                            return true;
                    }
               }
            }

            return false;
        }
        #endregion

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if ( e.ColumnIndex == 2 )
            {
                MapEditor mapEditor = new MapEditor();
                string[] sourceColumnNames;

                if (resourceGrid.Rows[e.RowIndex].Cells[3].Value == null)
                    sourceColumnNames = Array.Empty<string>();
                else
                    sourceColumnNames = resourceGrid.Rows[e.RowIndex].Cells[3].Value.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                var destinationColumnName = resourceGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                mapEditor.DestinationMemberLabel.Text = destinationColumnName;
                mapEditor.MappingFunctionTextBox.Text = resourceGrid.Rows[e.RowIndex].Cells[1].Value?.ToString();
                mapEditor.MappedList.Items.AddRange(sourceColumnNames);
                mapEditor.UnmappedList.Items.AddRange(EntityList.Items);

                mapEditor.MappedResourcesLabel.Text = "Mapped Resources";
                mapEditor.UnmappedResourcesLabel.Text = "Unmapped Resources";

                if ( mapEditor.ShowDialog() == DialogResult.OK)
                {
                    var resourceMap = ProfileMap.ResourceProfiles.FirstOrDefault(m => m.ResourceColumnName.Equals(resourceGrid.Rows[e.RowIndex].Cells[0].Value));

                    if (resourceMap != null)
                    {
                        var items = new List<string>();
                        foreach (var item in mapEditor.MappedList.Items)
                            items.Add(item.ToString());

                        resourceMap.MapFunction = mapEditor.MappingFunctionTextBox.Text;
                        resourceMap.EntityColumnNames = items.ToArray();

                        resourceGrid.Rows[e.RowIndex].Cells[1].Value = mapEditor.MappingFunctionTextBox.Text;
                        resourceGrid.Rows[e.RowIndex].Cells[3].Value = resourceMap.EntityColumnNames?.ToCSV();

                        EntityList.Items.Clear();
                        foreach ( var item in mapEditor.UnmappedList.Items)
                            EntityList.Items.Add(item.ToString());
                    }
                }
            }
        }

        private void OnEntityCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                MapEditor mapEditor = new MapEditor();

                var sourceColumnNames = EntityGrid.Rows[e.RowIndex].Cells[3].Value?.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var destinationColumnName = EntityGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                mapEditor.DestinationMemberLabel.Text = destinationColumnName;
                mapEditor.MappingFunctionTextBox.Text = EntityGrid.Rows[e.RowIndex].Cells[1].Value?.ToString();
                mapEditor.MappedList.Items.AddRange(sourceColumnNames);
                mapEditor.UnmappedList.Items.AddRange(EntityList.Items);

                mapEditor.MappedResourcesLabel.Text = "Mapped Resources";
                mapEditor.UnmappedResourcesLabel.Text = "Unmapped Resources";

                if (mapEditor.ShowDialog() == DialogResult.OK)
                {
                    var entityMap = ProfileMap.EntityProfiles.FirstOrDefault(m => m.EntityColumnName.Equals(EntityGrid.Rows[e.RowIndex].Cells[0].Value));

                    if (entityMap != null)
                    {
                        var items = new List<string>();
                        foreach (var item in mapEditor.MappedList.Items)
                            items.Add(item.ToString());

                        entityMap.MapFunction = mapEditor.MappingFunctionTextBox.Text;
                        entityMap.ResourceColumns = items.ToArray();

                        EntityGrid.Rows[e.RowIndex].Cells[1].Value = mapEditor.MappingFunctionTextBox.Text;
                        EntityGrid.Rows[e.RowIndex].Cells[3].Value = entityMap.ResourceColumns?.ToCSV();

                        ResourceList.Items.Clear();
                        foreach (var item in mapEditor.UnmappedList.Items)
                            ResourceList.Items.Add(item.ToString());
                    }
                }
            }
        }

        private void OnReset(object sender, EventArgs e)
        {
            if ( MessageBox.Show(this, "Warning: Resetting the mapping will lose any edits that you have made, and will reset the mappings to the default computed values.\r\n\r\nAre you sure you want to reset the mappings?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProfileMap = GenerateProfileMap(ResourceModel);
                PopulateUI();   
            }
        }
    }
}
