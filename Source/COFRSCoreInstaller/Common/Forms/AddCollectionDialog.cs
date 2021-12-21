using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using Microsoft.VisualStudio.Shell;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
    public partial class AddCollectionDialog : Form
    {
		public ResourceClass parentModel { get; set; }

        public AddCollectionDialog()
        {
            InitializeComponent();
        }

		/// <summary>
		/// Here we find all the resources that reference the source Resource (the one named in the ResourceName label control), and present them
		/// in the ChildResourceList list box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnLoad(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var codeService = COFRSServiceFactory.GetService<ICodeService>();

			//	The parent resource is the resource selected by the user. This is the resource we will be adding the collection to.
			//	Get the resource model for the parent resource.
			parentModel = codeService.GetResourceClass(ResourceName.Text);

			//	Now scan all the resource models in the resource map
			//foreach (var resourceModel in resourceMap.Maps)
			//{
			//	//	SKip the parent resource, it can't reference itself.
			//	if (!resourceModel.ClassName.Equals(parentModel.ClassName, StringComparison.OrdinalIgnoreCase) &&
			//										resourceModel.EntityModel != null)
			//	{
			//		//	See if there is a member in this resource that references the parent resource
			//		var referenceColumn = resourceModel.Columns.Where(c => c.IsForeignKey && c.ForeignTableName.Equals(parentModel.Entity.TableName, StringComparison.OrdinalIgnoreCase));

			//		if (referenceColumn != null && referenceColumn.Count() == 1)
			//		{
			//			//	Ther is, so add it to the child resource list
			//			ChildResourceList.Items.Add(resourceModel.ClassName);
			//		}
			//	}
			//}
		}

		/// <summary>
		/// Called when the user presses the OK button. 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnOK(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	Did the user select one of the child resources?
			if (ChildResourceList.CheckedItems.Count == 0)
			{
				MessageBox.Show("No child resource selected. You must select a resource from the list to generate a collection in the parent resource.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			//	Some stuff we need


			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
