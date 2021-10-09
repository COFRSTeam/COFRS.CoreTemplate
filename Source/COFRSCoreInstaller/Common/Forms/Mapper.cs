using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using COFRS.Template.Common.Wizards;
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
        public List<ResourceModel> ResourceModels { get; set; }
        public List<EntityModel> EntityModels { get; set; }
        public ProfileMap ProfileMap { get; set; }

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

            var nn = new NameNormalizer(ResourceModel.ClassName);

            ProfileMap = new ProfileMap
            {
                ResourceClassName = ResourceModel.ClassName,
                EntityClassName = ResourceModel.EntityModel.ClassName,
                ResourceProfiles = new List<ResourceProfile>(),
                EntityProfiles = new List<EntityProfile>()
            };

            var unmappedColumns = new List<DBColumn>();
            GetEntityModelList(ResourceModel.EntityModel, string.Empty, unmappedColumns);
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

                ProfileMap.ResourceProfiles.Add(profile);
            }

            unmappedColumns.Clear();
            GetResourceModelList(ResourceModel, string.Empty, unmappedColumns);
            GenerateEntityFromResourceMapping(unmappedColumns, ResourceModel);

            foreach (DataGridViewRow row in EntityGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new EntityProfile()
                {
                    EntityColumnName = columnName,
                    MapFunction = mappingFunction
                };

                ProfileMap.EntityProfiles.Add(profile);
            }
        }

        private void GetResourceModelList(ResourceModel source, string ParentName, List<DBColumn> unmappedColumns)
        {
            foreach (var column in source.Columns)
            {
                var resourceModel = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, column.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

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
        private void GetEntityModelList(EntityModel source, string ParentName, List<DBColumn> unmappedColumns)
        {
            foreach (var column in source.Columns)
            {
                var entityModel = EntityModels.FirstOrDefault(r => string.Equals(r.ClassName, column.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

                if (entityModel != null && entityModel.ElementType != ElementType.Enum)
                {
                    string newParent = string.Empty;

                    if (string.IsNullOrWhiteSpace(ParentName))
                        newParent = column.ColumnName;
                    else
                        newParent = $"{ParentName}.{column.ColumnName}";

                    GetEntityModelList(entityModel, newParent, unmappedColumns);
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
            foreach (var entityMember in ResourceModel.EntityModel.Columns)
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
                    if (ResourceModel.EntityModel.ServerType == DBServerType.MYSQL)
                        dataType = DBHelper.GetNonNullableMySqlDataType(entityMember);
                    else if (ResourceModel.EntityModel.ServerType == DBServerType.POSTGRESQL)
                        dataType = DBHelper.GetNonNullablePostgresqlDataType(entityMember);
                    else if (ResourceModel.EntityModel.ServerType == DBServerType.SQLSERVER)
                        dataType = DBHelper.GetNonNullableSqlServerDataType(entityMember);

                    //  We need the list of all the primary keys in the entity model, so that we know the
                    //  position of this member.
                    var primaryKeys = ResourceModel.EntityModel.Columns.Where(c => c.IsPrimaryKey);

                    if (primaryKeys.Count() == 1)
                    {
                        //  There is only one primary key element, so we don't need to bother with the count.
                        var formula = new StringBuilder($"source.HRef == null ? default : source.HRef.GetId<{dataType}>()");
                        dataRow.Cells[1].Value = formula.ToString();
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
                        dataRow.Cells[1].Value = formula.ToString();
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

                    foreach ( DataGridViewRow resourceMap in resourceGrid.Rows )
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

                        if (string.Equals(resourceMember.DataType.ToString(), "uri", StringComparison.OrdinalIgnoreCase))
                        {
                            //  This is an href. First, get the data type.
                            string dataType = "Unknown";

                            if (ResourceModel.EntityModel.ServerType == DBServerType.MYSQL)
                                dataType = DBHelper.GetNonNullableMySqlDataType(entityMember);
                            else if (ResourceModel.EntityModel.ServerType == DBServerType.POSTGRESQL)
                                dataType = DBHelper.GetNonNullablePostgresqlDataType(entityMember);
                            else if (ResourceModel.EntityModel.ServerType == DBServerType.SQLSERVER)
                                dataType = DBHelper.GetNonNullableSqlServerDataType(entityMember);

                            //  Now, we need the list of entity members that correspond to this resource member.
                            //  To get that, we need to look at the resource mapping.

                            //  This is an href. Very much like the primary key, there can be more than one single 
                            //  element in this href. 

                            var foreignKeys = ResourceModel.EntityModel.Columns.Where(c => c.IsForeignKey &&
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

                            var referenceModel = ResourceModels.FirstOrDefault(r =>
                                            string.Equals(r.ClassName, resourceMember.DataType.ToString(), StringComparison.Ordinal));

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
                                        string dataType = "Unknown";

                                        if (ResourceModel.EntityModel.ServerType == DBServerType.MYSQL)
                                            dataType = DBHelper.GetNonNullableMySqlDataType(entityMember);
                                        else if (ResourceModel.EntityModel.ServerType == DBServerType.POSTGRESQL)
                                            dataType = DBHelper.GetNonNullablePostgresqlDataType(entityMember);
                                        else if (ResourceModel.EntityModel.ServerType == DBServerType.SQLSERVER)
                                            dataType = DBHelper.GetNonNullableSqlServerDataType(entityMember);

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
                        var rp = ProfileMap.ResourceProfiles.FirstOrDefault(c => c.MapFunction.IndexOf(entityMember.ColumnName, 0, StringComparison.CurrentCultureIgnoreCase) != -1);

                        if (rp != null)
                        {
                            if (rp.ResourceColumnName.Contains("."))
                            {
                                var parts = rp.ResourceColumnName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                StringBuilder mapFunction = new StringBuilder();
                                StringBuilder parent = new StringBuilder("");
                                string NullValue = "null";

                                var parentModel = GetParentModel(ResourceModels, ResourceModel, parts);


                                if (parentModel != null)
                                {
                                    var parentColumn = parentModel.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[parts.Count() - 1], StringComparison.OrdinalIgnoreCase));

                                    if (parentColumn != null)
                                    {
                                        if (string.Equals(parentColumn.DataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
                                        {
                                            NullValue = "string.Empty";
                                        }
                                        else
                                        {
                                            var theDataType = Type.GetType(parentColumn.DataType.ToString());

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
                                    string.Equals(c.DataType.ToString(), rp.ResourceColumnName, StringComparison.OrdinalIgnoreCase));

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
            foreach (var resourceMember in ResourceModel.Columns)
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

                    if (string.Equals(resourceMember.DataType.ToString(), "Uri", StringComparison.OrdinalIgnoreCase))
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
                        var referenceModel = ResourceModels.FirstOrDefault(r =>
                                string.Equals(r.ClassName, resourceMember.DataType.ToString(), StringComparison.Ordinal));

                        if (referenceModel != null)
                        {
                            if (referenceModel.ResourceType == ResourceType.Enum)
                            {
                                var nnn = new NameNormalizer(resourceMember.ColumnName);
                                var foreignKeys = unmappedColumns.FindAll(c => c.IsForeignKey &&
                                         (string.Equals(c.ForeignTableName, nnn.SingleForm, StringComparison.Ordinal) ||
                                           string.Equals(c.ForeignTableName, nnn.PluralForm, StringComparison.Ordinal)));

                                if ( foreignKeys.Count() == 1 )
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
                        var model = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, resourceMember.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

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
            ProfileMap = new ProfileMap
            {
                ResourceClassName = ResourceModel.ClassName,
                EntityClassName = ResourceModel.EntityModel.ClassName,
                ResourceProfiles = new List<ResourceProfile>(),
                EntityProfiles = new List<EntityProfile>()
            };

            foreach (DataGridViewRow row in resourceGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new ResourceProfile()
                {
                    ResourceColumnName = columnName,
                    MapFunction = mappingFunction
                };

                ProfileMap.ResourceProfiles.Add(profile);
            }

            foreach (DataGridViewRow row in EntityGrid.Rows)
            {
                var columnName = (row.Cells[0] as DataGridViewTextBoxCell).Value.ToString();
                var mappingFunction = (row.Cells[1] as DataGridViewTextBoxCell).Value?.ToString();

                var profile = new EntityProfile()
                {
                    EntityColumnName = columnName,
                    MapFunction = mappingFunction
                };

                ProfileMap.EntityProfiles.Add(profile);
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
        private ResourceModel GetParentModel(List<ResourceModel> resourceModels, ResourceModel parent, string[] parts)
        {
            ResourceModel result = parent;

            for (int i = 0; i < parts.Count() - 1; i++)
            {
                var column = result.Columns.FirstOrDefault(c => string.Equals(c.ColumnName, parts[i], StringComparison.OrdinalIgnoreCase));

                if (column != null)
                {
                    result = ResourceModels.FirstOrDefault(p => string.Equals(p.ClassName, column.DataType.ToString(), StringComparison.OrdinalIgnoreCase));
                }
            }

            return result;
        }

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
                    var childModel = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, member.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

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
                    var childModel = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, member.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

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
            var model = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, destinationMember.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.DataType.ToString(), sourceMember.EntityType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (model != null)
            {
                if (model.ResourceType == ResourceType.Enum)
                {
                    if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase))
                    {
                        dataRow.Cells[1].Value = $"({model.ClassName}) source.{sourceMember.ColumnName}";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
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
                    if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(destinationMember.DataType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByte(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToSByte(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToShort(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToUShort(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToInt(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToUInt(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToLong(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToULong(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDecimal(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToFloat(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDouble(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToBoolean(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToChar(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDateTime(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDateTimeOffset(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToTimeSpan(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToString(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByteArray(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToEnumerableBytes(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByteList(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToImage(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.DataType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToGuid(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else
            {
                dataRow.Cells[1].Value = $"({destinationMember.DataType}) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
        }
        private void MapResourceDestinationFromSource(DBColumn destinationMember, DataGridViewRow dataRow, DBColumn sourceMember, ref List<DBColumn> unmappedColumns)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var model = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, destinationMember.DataType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (string.Equals(destinationMember.EntityType.ToString(), sourceMember.DataType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (model != null)
            {
                if (model.ResourceType == ResourceType.Enum)
                {
                    if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase))
                    {
                        dataRow.Cells[1].Value = $"({model.ClassName}) source.{sourceMember.ColumnName}";
                        dataRow.Cells[3].Value = sourceMember.ColumnName;
                        unmappedColumns.Remove(sourceMember);
                    }
                    else if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
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
                    if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(destinationMember.EntityType.ToString(), "byte", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByte(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "sbyte", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToSByte(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "short", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToShort(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "ushort", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToUShort(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "int", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToInt(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "uint", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToUInt(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "long", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToLong(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "ulong", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToULong(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "decimal", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDecimal(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "float", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToFloat(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "double", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDouble(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "bool", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToBoolean(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "char", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToChar(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDateTime(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToDateTimeOffset(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToTimeSpan(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "string", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToString(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByteArray(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToEnumerableBytes(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToByteList(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "Image", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToImage(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else if (string.Equals(destinationMember.EntityType.ToString(), "Guid", StringComparison.OrdinalIgnoreCase))
            {
                ConvertSourceToGuid(dataRow, sourceMember);
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
            else
            {
                dataRow.Cells[1].Value = $"({destinationMember.DataType}) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
                dataRow.Cells[3].Value = sourceMember.ColumnName;
                unmappedColumns.Remove(sourceMember);
            }
        }

        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Guid"/>
        /// </summary>
        /// <param name="dataRow">The data row to place the mapping function</param>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        private static void ConvertSourceToGuid(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Guid.Parse(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(Guid) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// Converts a source <see cref="DBColumn"/> value to an <see cref="Image"/>
        /// </summary>
        /// <param name="dataRow">The data row to place the mapping function</param>
        /// <param name="sourceMember">The <see cref="DBColumn>"/> source member to convert.</param>
        private static void ConvertSourceToImage(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ImageEx.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ImageEx.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ImageEx.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ImageEx.Parse(source.{sourceMember.ColumnName}.ToArray())";
            }
            else
            {
                dataRow.Cells[1].Value = $"(Image) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToByteList(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64String(source.{sourceMember.ColumnName}.ToList())";
            }
            else if (string.Equals(sourceMember.EntityType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length).ToList()";
            }
            else if (string.Equals(sourceMember.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? null : source.{sourceMember.ColumnName}.GetBytes().ToList()";
            }
            else if (string.Equals(sourceMember.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToList()";
            }
            else if (string.Equals(sourceMember.EntityType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToList()";
            }
            else
            {
                dataRow.Cells[1].Value = $"(List<byte>) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToEnumerableBytes(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length)";
            }
            else if (string.Equals(sourceMember.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? null : ImageEx.Parse(source.{sourceMember.ColumnName}.GetBytes())";
            }
            else if (string.Equals(sourceMember.EntityType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToArray()";
            }
            else if (string.Equals(sourceMember.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
            }
            else
            {
                dataRow.Cells[1].Value = $"(IEnumerable<byte>) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToByteArray(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.FromBase64CharArray(source.{sourceMember.ColumnName},0,source.{sourceMember.ColumnName}.Length)";
            }
            else if (string.Equals(sourceMember.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? null : source.{sourceMember.ColumnName}.GetBytes()";
            }
            else if (string.Equals(sourceMember.EntityType, "List<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToArray()";
            }
            else if (string.Equals(sourceMember.EntityType, "IEnumerable<byte>", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToArray()";
            }
            else
            {
                dataRow.Cells[1].Value = $"(byte[]) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToString(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}";
            }
            else if (string.Equals(sourceMember.EntityType, "Guid", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString()";
            }
            else if (string.Equals(sourceMember.EntityType, "Guid?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.HasValue() ? source.{sourceMember.ColumnName}.ToString() : string.Empty";
            }
            else if (string.Equals(sourceMember.EntityType, "char[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? string.Empty : new string(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte[]", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? string.Empty : Convert.ToBase64String(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "Image", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? string.Empty : Convert.ToBase64String(source.{sourceMember.ColumnName}.GetBytes())";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString()";
            }
            else if (string.Equals(sourceMember.EntityType, "byte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double?", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.Value.ToString() : string.Empty";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture)";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture)";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString(@\"dd\\.hh\\:mm\\:ss\\.fffffff\")";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTime?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture) : string.Empty";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(\"o\", CultureInfo.CurrentCulture) : string.Empty";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.HasValue ? source.{sourceMember.ColumnName}.ToString(@\"dd\\.hh\\:mm\\:ss\\.fffffff\") : string.Empty";
            }
            else if (string.Equals(sourceMember.EntityType, "BitArray", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName} == null ? string.Empty : source.{sourceMember.ColumnName}.ToString()";
            }
            else
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToString()";
            }
        }

        private static void ConvertSourceToTimeSpan(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"TimeSpan.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"TimeSpan.FromMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"new TimeSpan(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"TimeSpan.FromMilliseconds((double) source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(TimeSpan) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToDateTimeOffset(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"DateTimeOffset.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"DateTimeOffset.FromUnixTimeMilliseconds(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"(DateTimeOffset) Convert.ToDateTime(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToDateTime(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"DateTime.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"DateTime.FromBinary(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToDateTime(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(DateTime) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToChar(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"char.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToChar(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(char) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToBoolean(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"bool.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToBoolean(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(bool) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToDouble(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"double.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.TotalMilliseconds";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToDouble(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(double) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToFloat(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"float.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToSingle(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(float) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToDecimal(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"decimal.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToDecimal(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToDecimal(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(decimal) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToULong(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ulong.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt64(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt64(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(ulong) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToLong(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"long.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTime", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToBinary()";
            }
            else if (string.Equals(sourceMember.EntityType, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.ToUnixTimeMilliseconds()";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"source.{sourceMember.ColumnName}.Ticks";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToInt64(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(long) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToUInt(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"uint.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt32(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt32(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(uint) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private void ConvertSourceToInt(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (sourceMember.EntityType == null )
            {
                var resourceModel = ResourceModels.FirstOrDefault(r => string.Equals(r.ClassName, sourceMember.DataType.ToString(), StringComparison.Ordinal));

                if (resourceModel != null)
                {
                    if (resourceModel.ResourceType == ResourceType.Enum)
                    {
                        dataRow.Cells[1].Value = $"Convert.ToInt32(source.{sourceMember.ColumnName})";
                    }
                    else
                    {
                        dataRow.Cells[1].Value = $"(int) AFunc(source.{sourceMember.ColumnName})";
                        dataRow.Cells[1].Style.ForeColor = Color.Red;
                    }
                }
                else
                { 
                    var entityModel = EntityModels.FirstOrDefault(e => string.Equals(e.ClassName, sourceMember.DataType.ToString(), StringComparison.Ordinal));

                    if (entityModel != null)
                    {
                        if (entityModel.ElementType == ElementType.Enum)
                        {
                            dataRow.Cells[1].Value = $"Convert.ToInt32(source.{sourceMember.ColumnName})";
                        }
                        else
                        {
                            dataRow.Cells[1].Value = $"(int) AFunc(source.{sourceMember.ColumnName})";
                            dataRow.Cells[1].Style.ForeColor = Color.Red;
                        }
                    }
                }
            }
            else if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"int.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToInt32(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToInt32(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(int) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToUShort(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"ushort.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt16(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToUInt16(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(ushort) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToShort(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"short.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToInt16(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToInt16(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(short) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToSByte(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"sbyte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToSByte(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToSByte(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(sbyte) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
            }
        }

        private static void ConvertSourceToByte(DataGridViewRow dataRow, DBColumn sourceMember)
        {
            if (string.Equals(sourceMember.EntityType, "string", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"byte.Parse(source.{sourceMember.ColumnName})";
            }
            else if (string.Equals(sourceMember.EntityType, "TimeSpan", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToByte(source.{sourceMember.ColumnName}.TotalMilliseconds)";
            }
            else if (string.Equals(sourceMember.EntityType, "byte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "sbyte", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "short", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ushort", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "int", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "uint", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "long", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "ulong", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "char", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "decimal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "double", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "float", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sourceMember.EntityType, "object", StringComparison.OrdinalIgnoreCase))
            {
                dataRow.Cells[1].Value = $"Convert.ToByte(source.{sourceMember.ColumnName})";
            }
            else
            {
                dataRow.Cells[1].Value = $"(byte) AFunc(source.{sourceMember.ColumnName})";
                dataRow.Cells[1].Style.ForeColor = Color.Red;
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
