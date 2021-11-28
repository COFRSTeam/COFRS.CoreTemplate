
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

namespace COFRSCoreCommandsPackage.Forms
{
    public partial class Mapper : Form
    {
        public ResourceModel resourceModel { get; set; }
        public List<ResourceModel> resourceModels { get; set; }
        public List<EntityModel> entityModels { get; set; }
        public ProfileMap profileMap { get; set; }
        public DTE2 _dte2 { get; set; }

        public Mapper()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            OnResize(sender, e);
            ResourceClass_Lablel.Text = resourceModel.ClassName;
            EntityClass_Label.Text = resourceModel.EntityModel.ClassName;

            entityModels = COFRSCommonUtilities.LoadEntityMap(_dte2).Maps.ToList();

            var nn = new NameNormalizer(resourceModel.ClassName);

            profileMap = LoadProfileMap();

            if ( profileMap == null)
                profileMap = GenerateProfileMap(nn);
        }

        private ProfileMap LoadProfileMap()
        {
            var unmappedColumns = new List<DBColumn>();
            foreach (var entityColumn in resourceModel.EntityModel.Columns)
            {
                unmappedColumns.Add(entityColumn);
            }

            profileMap = COFRSCommonUtilities.LoadResourceMapping(_dte2, resourceModel);

            if (profileMap != null)
            {
                foreach (ResourceProfile resourceProfile in profileMap.ResourceProfiles)
                {
                    var dataRowIndex = resourceGrid.Rows.Add();
                    var dataRow = resourceGrid.Rows[dataRowIndex];

                    dataRow.Cells[0].Value = resourceProfile.ResourceColumnName;
                    dataRow.Cells[1].Value = resourceProfile.MapFunction;
                    dataRow.Cells[3].Value = resourceProfile.EntityColumnNames?.ToCSV();

                    if (resourceProfile.EntityColumnNames != null)
                    {
                        foreach (var entityColumn in resourceProfile.EntityColumnNames)
                        {
                            var ec = unmappedColumns.FirstOrDefault(c => c.ColumnName.Equals(entityColumn));

                            if (ec != null)
                                unmappedColumns.Remove(ec);
                        }
                    }
                }

                foreach ( var entityColumn in unmappedColumns)
                {
                    EntityList.Items.Add(entityColumn.ColumnName);
                }

                unmappedColumns.Clear();
                foreach (var entityColumn in resourceModel.Columns)
                {
                    unmappedColumns.Add(entityColumn);
                }

                foreach (EntityProfile entityProfile in profileMap.EntityProfiles)
                {
                    var dataRowIndex = EntityGrid.Rows.Add();
                    var dataRow = EntityGrid.Rows[dataRowIndex];

                    dataRow.Cells[0].Value = entityProfile.EntityColumnName;
                    dataRow.Cells[1].Value = entityProfile.MapFunction;
                    dataRow.Cells[3].Value = entityProfile.ResourceColumns?.ToCSV();

                    if (entityProfile.ResourceColumns != null)
                    {
                        foreach (var resourceColumn in entityProfile.ResourceColumns)
                        {
                            var ec = unmappedColumns.FirstOrDefault(c => c.ColumnName.Equals(resourceColumn));

                            if (ec != null)
                                unmappedColumns.Remove(ec);
                        }
                    }
                }
                foreach (var resourceColumn in unmappedColumns)
                {
                    ResourceList.Items.Add(resourceColumn.ColumnName);
                }

            }

            return profileMap;
        }

        private ProfileMap GenerateProfileMap(NameNormalizer nn)
        {
            var unmappedColumns = new List<DBColumn>();
            foreach (var entityColumn in resourceModel.EntityModel.Columns)
            {
                unmappedColumns.Add(entityColumn);
            }
            profileMap = new ProfileMap
            {
                ResourceClassName = resourceModel.ClassName,
                EntityClassName = resourceModel.EntityModel.ClassName,
                ResourceProfiles = new List<ResourceProfile>(),
                EntityProfiles = new List<EntityProfile>()
            };


            GenerateResourceFromEntityMapping(nn, unmappedColumns);

            foreach (DataGridViewRow row in resourceGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new ResourceProfile()
                {
                    ResourceColumnName = columnName,
                    MapFunction = mappingFunction
                };

                profileMap.ResourceProfiles.Add(profile);
            }

            unmappedColumns.Clear();
            GetResourceModelList(resourceModel, string.Empty, unmappedColumns);
            GenerateEntityFromResourceMapping(unmappedColumns, resourceModel);

            foreach (DataGridViewRow row in EntityGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new EntityProfile()
                {
                    EntityColumnName = columnName,
                    MapFunction = mappingFunction
                };

                profileMap.EntityProfiles.Add(profile);
            }

            return profileMap;
        }

        private void GetResourceModelList(ResourceModel source, string ParentName, List<DBColumn> unmappedColumns)
        {
            foreach (var column in source.Columns)
            {
                var resourceModel = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, column.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                if (resourceModel != null && resourceModel.ResourceType != ResourceType.Enum)
                {
                    string newParent = string.Empty;

                    if (string.IsNullOrWhiteSpace(ParentName))
                        newParent = column.ColumnName;
                    else
                        newParent = $"{ParentName}.{column.ColumnName}";

                    GetResourceModelList(resourceModel, newParent, unmappedColumns);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(ParentName))
                        column.ColumnName = $"{ParentName}.{column.ColumnName}";

                    unmappedColumns.Add(column);
                }
            }
        }


        /// <summary>
        /// Generates a mapping to construct the entity member from the corresponding resource members
        /// </summary>
        /// <param name="unmappedColumns"></param>
        /// <param name="resourceModel"></param>
        private void GenerateEntityFromResourceMapping(List<DBColumn> unmappedColumns, ResourceModel resourceModel)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            //  Let's create a mapping for each entity member
            foreach (var entityMember in this.resourceModel.EntityModel.Columns)
            {
                //  Construct a data row for this entity member, and populate the column name
                var dataRowIndex = EntityGrid.Rows.Add();
                var dataRow = EntityGrid.Rows[dataRowIndex];
                dataRow.Cells[0].Value = entityMember.ColumnName;

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
                    var primaryKeys = this.resourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey);

                    if (primaryKeys.Count() == 1)
                    {
                        var primaryKey = primaryKeys.First();
                        //  There is only one primary key element, so we don't need to bother with the count.
                        var formula = new StringBuilder($"source.HRef == null ? default : source.HRef.GetId<{dataType}>()");
                        dataRow.Cells[1].Value = formula.ToString();
                        dataRow.Cells[3].Value = primaryKey.ColumnName;
                    }
                    else
                    {
                        var formula = new StringBuilder($"source.HRef == null ? default : source.HRef.GetId<{dataType}>(");
                        var primaryKeyList = new StringBuilder();
                        bool first = true;
                        //  Compute the index and append it to the above formula.
                        if (primaryKeys.Count() > 1)
                        {
                            var index = primaryKeys.Count() - 1;

                            foreach (var pk in primaryKeys)
                            {
                                if (first)
                                    first = false;
                                else
                                    primaryKeyList.Append(',');

                                primaryKeyList.Append(primaryKeys.ToList()[index].ColumnName);

                                if (pk == entityMember)
                                    formula.Append(index.ToString());
                                else
                                    index--;
                            }
                        }

                        //  Close the formula
                        formula.Append(")");
                        dataRow.Cells[1].Value = formula.ToString();
                        dataRow.Cells[3].Value = primaryKeyList.ToString();
                    }

                    var resourceMember = unmappedColumns.FirstOrDefault(u =>
                        string.Equals(u.ColumnName, "HRef", StringComparison.OrdinalIgnoreCase));

                    if (resourceMember != null)
                        unmappedColumns.Remove(resourceMember);
                }
                else if (entityMember.IsForeignKey)
                {
                    //  This is a special case of a foreign key. The first challenge is to discover which resource member
                    //  is used to represent this foreign key. In all likelyhood, it will be the resource member that has
                    //  the same name as the foreign table that this foreign key represents. It's probably going to be the
                    //  single form, but it could be the plural form. Look for either one.
                    var nnx = new NameNormalizer(entityMember.ForeignTableName);
                    DBColumn resourceMember = null;

                    foreach (DataGridViewRow resourceMap in resourceGrid.Rows )
                    {
                        var entityColumnsUsed = resourceMap.Cells[3].Value.ToString().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach ( var entityColumnUsed in entityColumnsUsed )
                        {
                            if ( string.Equals(entityColumnUsed, entityMember.ColumnName, StringComparison.OrdinalIgnoreCase))
                            {
                                var resourceMemberName = resourceMap.Cells[0].Value.ToString();
                                resourceMember = resourceModel.Columns.FirstOrDefault(c =>
                                    string.Equals(c.ColumnName, resourceMemberName, StringComparison.OrdinalIgnoreCase));

                                if (resourceMember != null)
                                    unmappedColumns.Remove(resourceMember);
                                break;
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

                            var foreignKeys = this.resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                                string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                            if (foreignKeys.Count() == 1)
                            {
                                var formula = new StringBuilder($"source.{resourceMember.ColumnName}.GetId<{dataType}>()");
                                dataRow.Cells[1].Value = formula.ToString();
                            }
                            else
                            {
                                var formula = new StringBuilder($"source.{resourceMember.ColumnName}.GetId<{dataType}>(");

                                if (foreignKeys.Count() > 1)
                                {
                                    var index = foreignKeys.Count() - 1;

                                    foreach (var pk in foreignKeys)
                                    {
                                        if (pk == entityMember)
                                            formula.Append(index.ToString());
                                        else
                                            index--;
                                    }
                                }

                                formula.Append(")");
                                dataRow.Cells[1].Value = formula.ToString();
                                unmappedColumns.Remove(resourceMember);
                            }
                        }
                        else
                        {
                            //  The resource member is not a URI. It should be an enum. If it is, we sould be able to
                            //  find it in our resource models list.

                            var referenceModel = resourceModels.FirstOrDefault(r =>
                                            string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.Ordinal));

                            if ( referenceModel != null )
                            {
                                if ( referenceModel.ResourceType == ResourceType.Enum )
                                { 
                                    var foreignKeys = this.resourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
                                        string.Equals(c.ForeignTableName, entityMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                                    //  If the resource member is an enum that represents the value of the primary key of a foreign table,
                                    //  then that foreign key can only have one member. If it has more than one member, then this 
                                    //  is not the proper mapping.
                                    if (foreignKeys.Count() == 1)
                                    {
                                        string dataType = entityMember.ModelDataType;

                                        var formula = $"Convert.ChangeType(source.{resourceMember.ColumnName}, source.{resourceMember.ColumnName}.GetTypeCode())";
                                        dataRow.Cells[1].Value = formula.ToString();
                                        unmappedColumns.Remove(resourceMember);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //  Is there a corresponding Entity Column for this resource Column?
                    var resourceMember = unmappedColumns.FirstOrDefault(u =>
                                                            string.Equals(u.ColumnName,
                                                                          entityMember.ColumnName,
                                                                          StringComparison.OrdinalIgnoreCase));

                    if (resourceMember != null)
                    {
                        MapResourceDestinationFromSource(entityMember, dataRow, resourceMember, ref unmappedColumns);
                        unmappedColumns.Remove(resourceMember);
                    }
                    else
                    {
                        var rp = profileMap.ResourceProfiles.FirstOrDefault(c => c.MapFunction.IndexOf(entityMember.ColumnName, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

                        if (rp != null)
                        {
                            if (rp.ResourceColumnName.Contains("."))
                            {
                                var parts = rp.ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                StringBuilder mapFunction = new StringBuilder();
                                StringBuilder parent = new StringBuilder("");
                                string NullValue = "null";

                                var parentModel = COFRSCommonUtilities.GetParentModel(resourceModels, this.resourceModel, parts);

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
                                dataRow.Cells[1].Value = mapFunction.ToString();

                                StringBuilder childColumn = new StringBuilder();

                                foreach (var p in parts)
                                {
                                    if (childColumn.Length > 0)
                                        childColumn.Append(".");
                                    childColumn.Append(p);
                                }

                                var cc = unmappedColumns.FirstOrDefault(c => string.Equals(c.ColumnName, childColumn.ToString(), StringComparison.OrdinalIgnoreCase));
                                if (cc != null)
                                {
                                    dataRow.Cells[3].Value = cc.ColumnName;
                                    unmappedColumns.Remove(cc);
                                }
                            }
                            else
                            {
                                StringBuilder mc = new StringBuilder();

                                resourceMember = resourceModel.Columns.FirstOrDefault(c =>
                                    string.Equals(c.ModelDataType.ToString(), rp.ResourceColumnName, StringComparison.OrdinalIgnoreCase));

                                MapResourceDestinationFromSource(entityMember, dataRow, resourceMember, ref unmappedColumns);
                            }
                        }
                    }
                }
            }

            foreach (var member in unmappedColumns)
            {
                ResourceList.Items.Add(member.ColumnName);
            }
        }

        private void GenerateResourceFromEntityMapping(NameNormalizer nn, List<DBColumn> unmappedColumns)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var resourceMember in resourceModel.Columns)
            {
                var dataRowIndex = resourceGrid.Rows.Add();
                var dataRow = resourceGrid.Rows[dataRowIndex];

                dataRow.Cells[0].Value = resourceMember.ColumnName;
                StringBuilder entityColumns = new StringBuilder();

                //  If this is the HRef (the primary key), then the columns that comprise it are all the primary
                //  key values of the entity in the order in which they appear in the entity
                if (resourceMember.IsPrimaryKey)
                {
                    var primaryKeys = unmappedColumns.FindAll(c => c.IsPrimaryKey);
                    var formula = new StringBuilder($"new Uri(rootUrl, $\"{nn.PluralCamelCase}/id");
                    StringBuilder sourceColumns = new StringBuilder();

                    foreach (var entityMember in primaryKeys)
                    {
                        formula.Append($"/{{source.{entityMember.ColumnName}}}");
                        unmappedColumns.Remove(entityMember);

                        if (sourceColumns.Length > 0)
                            sourceColumns.Append(", ");
                        sourceColumns.Append(entityMember.ColumnName);
                    }

                    formula.Append("\")");
                    dataRow.Cells[1].Value = formula.ToString();
                    dataRow.Cells[3].Value = sourceColumns.ToString();
                }

                //  If this column represents a foreign key, then the entity members that comprise it will be
                //  those entity members that are foreign keys, that have the same table name as this members name.
                else if (resourceMember.IsForeignKey)
                {
                    //  A foreign key is commonly represented in one of two forms. Either it is a hypertext reference
                    //  (an href), in which case the resource member should be a Uri, or it is an enum.

                    //  If it is an enum, there will be a resource model of type enum, whose corresponding entity model
                    //  will point to the foreign table.

                    var enumResource = resourceModels.FirstOrDefault(r =>
                                       r.ResourceType == ResourceType.Enum &&
                                       r.EntityModel != null && 
                                       string.Equals(r.EntityModel.TableName, resourceMember.ForeignTableName, StringComparison.OrdinalIgnoreCase));

                    if (enumResource != null)
                    {
                        StringBuilder sourceColumns = new StringBuilder();
                        var formula = $"({enumResource.ClassName})source.{resourceMember.ColumnName}";
                        dataRow.Cells[1].Value = formula.ToString();
                        dataRow.Cells[3].Value = sourceColumns.ToString();
                    }
                    else
                    {
                        if (string.Equals(resourceMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
                        {
                            var nnn = new NameNormalizer(resourceMember.ColumnName);
                            var foreignKeys = unmappedColumns.FindAll(c => c.IsForeignKey &&
                               (string.Equals(c.ForeignTableName, nnn.SingleForm, StringComparison.Ordinal) ||
                                 string.Equals(c.ForeignTableName, nnn.PluralForm, StringComparison.Ordinal)));

                            var formula = new StringBuilder($"new Uri(rootUrl, $\"{nnn.PluralCamelCase}/id");
                            StringBuilder sourceColumns = new StringBuilder();

                            foreach (var entityMember in foreignKeys)
                            {
                                formula.Append($"/{{source.{entityMember.ColumnName}}}");
                                unmappedColumns.Remove(entityMember);

                                if (sourceColumns.Length > 0)
                                    sourceColumns.Append(", ");
                                sourceColumns.Append(entityMember.ColumnName);
                            }

                            formula.Append("\")");
                            dataRow.Cells[1].Value = formula.ToString();
                            dataRow.Cells[3].Value = sourceColumns.ToString();
                        }
                        else
                        {
                            //  This is probably an Enum. If it is, we should be able to find it in the list of 
                            //  resource models.
                            var referenceModel = resourceModels.FirstOrDefault(r =>
                                    string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.Ordinal));

                            if (referenceModel != null)
                            {
                                if (referenceModel.ResourceType == ResourceType.Enum)
                                {
                                    var nnn = new NameNormalizer(resourceMember.ColumnName);
                                    var foreignKeys = unmappedColumns.FindAll(c => c.IsForeignKey &&
                                             (string.Equals(c.ForeignTableName, nnn.SingleForm, StringComparison.Ordinal) ||
                                               string.Equals(c.ForeignTableName, nnn.PluralForm, StringComparison.Ordinal)));

                                    if (foreignKeys.Count() == 1)
                                    {
                                        var entityMember = foreignKeys[0];

                                        var formula = $"({referenceModel.ClassName}) source.{entityMember.ColumnName}";
                                        dataRow.Cells[1].Value = formula.ToString();
                                        dataRow.Cells[3].Value = entityMember.ColumnName;

                                        unmappedColumns.Remove(entityMember);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //  Is there an existing entityMember whos column name matches the resource member?
                    var entityMember = unmappedColumns.FirstOrDefault(u =>
                        string.Equals(u.ColumnName, resourceMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (entityMember != null)
                    {
                        //  There is, just assign it.
                        MapEntityDestinationFromSource(resourceMember, dataRow, entityMember, ref unmappedColumns);
                    }
                    else
                    {
                        //  Is this resource member a class?
                        var model = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, resourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                        if (model != null)
                        {
                            //  It is a class, instantiate the class
                            dataRow.Cells[1].Value = $"new {model.ClassName}()";

                            //  Now, go map all of it's children
                            MapEntityChildMembers(unmappedColumns, resourceMember, model, resourceMember.ColumnName);
                        }
                    }
                }
            }

            foreach (var member in unmappedColumns)
            {
                EntityList.Items.Add(member.ColumnName);
            }
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
            profileMap = new ProfileMap
            {
                ResourceClassName = resourceModel.ClassName,
                EntityClassName = resourceModel.EntityModel.ClassName,
                ResourceProfiles = new List<ResourceProfile>(),
                EntityProfiles = new List<EntityProfile>()
            };

            foreach (DataGridViewRow row in resourceGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();
                var entityColumns = (row.Cells[3] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new ResourceProfile()
                {
                    ResourceColumnName = columnName,
                    MapFunction = mappingFunction,
                    EntityColumnNames = entityColumns?.Split(',')
                };

                profileMap.ResourceProfiles.Add(profile);
            }

            foreach (DataGridViewRow row in EntityGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();
                var resourceColumns = (row.Cells[3] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new EntityProfile()
                {
                    EntityColumnName = columnName,
                    MapFunction = mappingFunction,
                    ResourceColumns = resourceColumns?.Split(',')
                };

                profileMap.EntityProfiles.Add(profile);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #region Mapping Helper Functions
        private void MapResourceChildMembers(List<DBColumn> unmappedColumns, DBColumn member, ResourceModel model, string parent)
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
                var entityMember = unmappedColumns.FirstOrDefault(u =>
                    string.Equals(u.ColumnName, childMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (entityMember != null)
                {
                    //  We do, just assign it
                    MapResourceDestinationFromSource(childMember, dataRow, entityMember, ref unmappedColumns);
                }
                else
                {
                    var childModel = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, member.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (model != null)
                    {
                        dataRow.Cells[1].Value = $"new {model.ClassName}()";
                        MapResourceChildMembers(unmappedColumns, member, model, $"{parent}?.{childMember.ColumnName}");
                    }
                }
            }
        }
        private void MapEntityChildMembers(List<DBColumn> unmappedColumns, DBColumn member, ResourceModel model, string parent)
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
                var entityMember = unmappedColumns.FirstOrDefault(u =>
                    string.Equals(u.ColumnName, childMember.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (entityMember != null)
                {
                    //  We do, just assign it
                    MapEntityDestinationFromSource(childMember, dataRow, entityMember, ref unmappedColumns);
                }
                else
                {
                    var childModel = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, member.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (model != null)
                    {
                        dataRow.Cells[1].Value = $"new {model.ClassName}()";
                        MapEntityChildMembers(unmappedColumns, member, model, $"{parent}?.{childMember.ColumnName}");
                    }
                }
            }
        }

        private void MapEntityDestinationFromSource(DBColumn destinationMember, DataGridViewRow dataRow, DBColumn sourceMember, ref List<DBColumn> unmappedColumns)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var model = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, destinationMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
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
                        dataRow.Cells[1].Value = $"({model.ClassName}) source.{sourceMember.ColumnName}";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        dataRow.Cells[1].Value = $"Enum.Parse<{model.ClassName}>(source.{sourceMember.ColumnName})";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else
                    {
                        dataRow.Cells[1].Value = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                        dataRow.Cells[1].Style.ForeColor = Color.Red;
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                }
                else
                {
                    if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ContainsParseFunction(model))
                        {
                            dataRow.Cells[1].Value = $"{model.ClassName}.Parse(source.{sourceMember.ColumnName})";
                            dataRow.Cells[3].Value = sourceMember.ColumnName;
                            unmappedColumns.Remove(sourceMember);
                        }
                        else
                        {
                            dataRow.Cells[1].Value = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                            dataRow.Cells[1].Style.ForeColor = Color.Red;
                            dataRow.Cells[3].Value = sourceMember.ColumnName;
                            unmappedColumns.Remove(sourceMember);
                        }
                    }
                    else
                    {
                        dataRow.Cells[1].Value = $"({model.ClassName}) AFunc(source.{sourceMember.ColumnName})";
                        dataRow.Cells[1].Style.ForeColor = Color.Red;
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                }
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToLong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToULong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToChar(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToString(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToImage(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUri(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else
            {
                dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
        }
        private void MapResourceDestinationFromSource(DBColumn destinationMember, DataGridViewRow dataRow, DBColumn sourceMember, ref List<DBColumn> unmappedColumns)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var model = resourceModels.FirstOrDefault(r => string.Equals(r.ClassName, sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.ModelDataType.ToString(), sourceMember.ModelDataType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
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
                        dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) source.{sourceMember.ColumnName}";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else if (string.Equals(destinationMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString())";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else
                    {
                        dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                        dataRow.Cells[1].Style.ForeColor = Color.Red;
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                }
                else
                {
                    if (string.Equals(sourceMember.ModelDataType, "string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ContainsParseFunction(model))
                        {
                            dataRow.Cells[1].Value = $"{model.ClassName}.Parse(source.{sourceMember.ColumnName})";
                            dataRow.Cells[3].Value = sourceMember.ColumnName;
                            unmappedColumns.Remove(sourceMember);
                        }
                        else
                        {
                            dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                            dataRow.Cells[1].Style.ForeColor = Color.Red;
                            dataRow.Cells[3].Value = sourceMember.ColumnName;
                            unmappedColumns.Remove(sourceMember);
                        }
                    }
                    else
                    {
                        dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                        dataRow.Cells[1].Style.ForeColor = Color.Red;
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                }
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToSByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "sbyte?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableSByte(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "short?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ushort?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableUShort(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "int?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "uint?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableUInt(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToLong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "long?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableLong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToULong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "ulong?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableULong(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDecimal(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "decimal?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDecimal(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToFloat(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "float?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableFloat(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDouble(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "double?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDouble(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToBoolean(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "bool?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableBoolean(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToChar(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "char?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableChar(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDateTime(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDateTime(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToDateTimeOffset(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableDateTimeOffset(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToTimeSpan(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableTimeSpan(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToString(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByteArray(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToEnumerableBytes(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToByteList(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToImage(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToGuid(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToNullableGuid(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.ModelDataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = SourceConverter.ToUri(sourceMember, out bool isUndefined);
                dataRow.Cells[1].Style.ForeColor = isUndefined ? Color.Red : Color.Black;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else
            {
                dataRow.Cells[1].Value = $"({destinationMember.ModelDataType}) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
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

                var sourceColumnNames = resourceGrid.Rows[e.RowIndex].Cells[3].Value.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var destinationColumnName = resourceGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                mapEditor.DestinationMemberLabel.Text = destinationColumnName;
                mapEditor.MappingFunctionTextBox.Text = resourceGrid.Rows[e.RowIndex].Cells[1].Value.ToString();
                mapEditor.MappedList.Items.AddRange(sourceColumnNames);
                mapEditor.UnmappedList.Items.AddRange(EntityList.Items);

                mapEditor.MappedResourcesLabel.Text = "Mapped Resources";
                mapEditor.UnmappedResourcesLabel.Text = "Unmapped Resources";

                if ( mapEditor.ShowDialog() == DialogResult.OK)
                {
                    resourceGrid.Rows[e.RowIndex].Cells[1].Value = mapEditor.MappingFunctionTextBox.Text;
                }
            }
        }

        private void OnEntityCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2)
            {
                MapEditor mapEditor = new MapEditor();

                var sourceColumnNames = EntityGrid.Rows[e.RowIndex].Cells[3].Value.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var destinationColumnName = EntityGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                mapEditor.DestinationMemberLabel.Text = destinationColumnName;
                mapEditor.MappingFunctionTextBox.Text = EntityGrid.Rows[e.RowIndex].Cells[1].Value.ToString();
                mapEditor.MappedList.Items.AddRange(sourceColumnNames);
                mapEditor.UnmappedList.Items.AddRange(EntityList.Items);

                mapEditor.MappedResourcesLabel.Text = "Mapped Resources";
                mapEditor.UnmappedResourcesLabel.Text = "Unmapped Resources";

                if (mapEditor.ShowDialog() == DialogResult.OK)
                {
                    EntityGrid.Rows[e.RowIndex].Cells[1].Value = mapEditor.MappingFunctionTextBox.Text;
                }
            }
        }
    }
}
