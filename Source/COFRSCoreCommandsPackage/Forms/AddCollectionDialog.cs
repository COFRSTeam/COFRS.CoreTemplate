using COFRSCoreCommon.Models;
using COFRSCoreCommon.Utilities;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSCoreCommandsPackage.Forms
{
	//--------------------------------------------------------------------------------------------------------------------------------
	//	The idea of this dialog is to present the user with a list of child resources that can be included into the main source
	//	resource as a collection.
	//
	//	Imagine that we have a resource called Assembly. An assembly is a collection of sub parts, all put together. An assemply
	//	class might look like:
	//
	//	public clsss Assembly
	//	{
	//		public Uri HRef { get; set; }
	//	    public string AssemblyName { get; set; }
	//	}
	//
	//	Now, assume in our system we also have a resource called Part. The Part is a single item that is one of the things that makes
	//	up an assembly. The Part class might look like...
	//
	//	public class Part
	//	{
	//		public Uri HRef { get; set; }
	//		public Uri Assembly { get; set; }
	//		public string PartName { get; set; }
	//	}
	//
	//	As you can see from the above classes, an individual assembly is referenced by it's HRef. Likewise, an individual part is
	//	referenced by it's HRef. So, Assembly #1 would have an HRef of /assembly/id/1, and part #7 would have an HRef of
	//	/parts/id/7.
	//
	//	Each part belongs to an assembly, and so each part has a reference to the Assembly that it belongs to. So, if Part #7 
	//	is a part for Assembly #1, then it's Assembly reference would be /assembly/id/1
	//
	//	This relationship, in database terminaology, is call a foreign key reference. The Assembly reference in the Part class is 
	//	a foreign key reference to the Assembly object.
	//
	//	Because this relationship exists, we might like to include all the parts that belong to the Assembly in the Assembly object
	//	itself. This would change the Assembly class to look like this:
	//
	//	public clsss Assembly
	//	{
	//		public Uri HRef { get; set; }
	//	    public string AssemblyName { get; set; }
	//		public Part[] Parts { get; set; }
	//	}
	//
	//	Now, you see that the assembly has a new member, which is a collection of parts.
	//
	//	That is what this dialog does. Given a class, it searches for other classes that have a foreign key reference pointing to it.
	//	It presents those "child" classes in a list to the user. 
	//	The user selects one of those classes, and this dialog modifies the original (source) class to include a collection of the
	//	child classes.
	//
	//	Of course, just adding the collection to the source class isn't enought. We also need to change the Orchestration layer to 
	//	process that collection.
	//
	//	This means, we need to modify the Orchestration Layer to...
	//
	//		GetSingle => include code to read the collection of child objects and populate the new collection
	//		GetCollection => Same as GetSingle, but we have to do it for each source resource in the collection
	//		Add => after adding a new source resource, we need to add any child resources that it contains
	//		Update => add,update or delete child resources as appropriate
	//		Patch => include code to patch any of the child objects in the array
	//		Delete => delete any child objects in the array before deleting the source resource.
	//--------------------------------------------------------------------------------------------------------------------------------
	public partial class AddCollectionDialog : Form
	{
		public DTE2 _dte2;
		private ResourceMap resourceMap;
		private ResourceModel parentModel;

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
			resourceMap = COFRSCommonUtilities.LoadResourceMap(_dte2);         //	The resource map contains all the resource classes in the project.

			//	The parent resource is the resource selected by the user. This is the resource we will be adding the collection to.
			//	Get the resource model for the parent resource.
			parentModel = resourceMap.Maps.FirstOrDefault(r => r.ClassName.Equals(ResourceName.Text, StringComparison.OrdinalIgnoreCase));

			//	Now scan all the resource models in the resource map
			foreach (var resourceModel in resourceMap.Maps)
			{
				//	SKip the parent resource, it can't reference itself.
				if (!resourceModel.ClassName.Equals(parentModel.ClassName, StringComparison.OrdinalIgnoreCase) && 
					                                resourceModel.EntityModel != null)
				{
					//	See if there is a member in this resource that references the parent resource
					var referenceColumn = resourceModel.Columns.Where(c => c.IsForeignKey && c.ForeignTableName.Equals(parentModel.EntityModel.TableName, StringComparison.OrdinalIgnoreCase));

					if (referenceColumn != null && referenceColumn.Count() == 1)
					{
						//	Ther is, so add it to the child resource list
						ChildResourceList.Items.Add(resourceModel.ClassName);
					}
				}
			}
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

