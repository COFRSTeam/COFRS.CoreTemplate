using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
    public partial class MapEditor : Form
    {
        public MapEditor()
        {
            InitializeComponent();
        }

        private void OnOK(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void TransferToMapped(object sender, EventArgs e)
        {
            var selectedItems = new List<string>();
            foreach ( var selectedItem in UnmappedList.SelectedItems)
                selectedItems.Add(selectedItem.ToString());

            foreach ( var selectedItem in selectedItems )
            {
                UnmappedList.Items.Remove(selectedItem.ToString());
                MappedList.Items.Add(selectedItem.ToString()); 
            }
        }

        private void TransferToUnmapped(object sender, EventArgs e)
        {
            var selectedItems = new List<string>();
            foreach (var selectedItem in MappedList.SelectedItems)
                selectedItems.Add(selectedItem.ToString());

            foreach (var selectedItem in selectedItems)
            {
                MappedList.Items.Remove(selectedItem.ToString());
                UnmappedList.Items.Add(selectedItem.ToString());
            }
        }
    }
}
