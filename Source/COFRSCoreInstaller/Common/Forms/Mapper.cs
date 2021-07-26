using COFRS.Template.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
    public partial class Mapper : Form
    {
        public ResourceClassFile resourceClassFile { get; set; }
        public EntityClassFile entityClassFile { get; set; }

        public Mapper()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ResourceClass_Lablel.Text = resourceClassFile.ClassName;
            EntityClass_Label.Text = entityClassFile.ClassName;

            foreach (var member in resourceClassFile.Members)
            {
                ResourceTree.Nodes.Add(member.ResourceMemberName);
            }

            foreach (var member in entityClassFile.Columns)
            {
                EntityTree.Nodes.Add(member.EntityName);
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            Explanation.Width = ClientRectangle.Right - 4 - Explanation.Left;
            ResourceClass_Lablel.Width = ClientRectangle.Right - 4 - ResourceClass_Lablel.Left;
            EntityClass_Label.Width = ClientRectangle.Right - 4 - EntityClass_Label.Left;
            EntityTree.Width = ClientRectangle.Right - 4 - EntityTree.Left;
            CancelBtn.Left = ClientRectangle.Right - 4 - CancelBtn.Width;
            OKButton.Left = CancelBtn.Left - 4 - OKButton.Width;
        }
    }
}
