
namespace COFRS.Template.Common.Forms
{
    partial class Mapper
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Mapper));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.Explanation = new System.Windows.Forms.Label();
            this.Resource_Label = new System.Windows.Forms.Label();
            this.Entity_Label = new System.Windows.Forms.Label();
            this.ResourceClass_Lablel = new System.Windows.Forms.Label();
            this.EntityClass_Label = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ResourceTree = new System.Windows.Forms.TreeView();
            this.EntityTree = new System.Windows.Forms.TreeView();
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::COFRS.Template.Properties.Resources.ico128;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // Explanation
            // 
            this.Explanation.Location = new System.Drawing.Point(155, 12);
            this.Explanation.Name = "Explanation";
            this.Explanation.Size = new System.Drawing.Size(633, 89);
            this.Explanation.TabIndex = 1;
            this.Explanation.Text = resources.GetString("Explanation.Text");
            // 
            // Resource_Label
            // 
            this.Resource_Label.AutoSize = true;
            this.Resource_Label.Location = new System.Drawing.Point(155, 101);
            this.Resource_Label.Name = "Resource_Label";
            this.Resource_Label.Size = new System.Drawing.Size(53, 13);
            this.Resource_Label.TabIndex = 2;
            this.Resource_Label.Text = "Resource";
            // 
            // Entity_Label
            // 
            this.Entity_Label.AutoSize = true;
            this.Entity_Label.Location = new System.Drawing.Point(155, 127);
            this.Entity_Label.Name = "Entity_Label";
            this.Entity_Label.Size = new System.Drawing.Size(33, 13);
            this.Entity_Label.TabIndex = 3;
            this.Entity_Label.Text = "Entity";
            // 
            // ResourceClass_Lablel
            // 
            this.ResourceClass_Lablel.Location = new System.Drawing.Point(223, 101);
            this.ResourceClass_Lablel.Name = "ResourceClass_Lablel";
            this.ResourceClass_Lablel.Size = new System.Drawing.Size(565, 23);
            this.ResourceClass_Lablel.TabIndex = 4;
            this.ResourceClass_Lablel.Text = "label1";
            // 
            // EntityClass_Label
            // 
            this.EntityClass_Label.Location = new System.Drawing.Point(223, 127);
            this.EntityClass_Label.Name = "EntityClass_Label";
            this.EntityClass_Label.Size = new System.Drawing.Size(565, 23);
            this.EntityClass_Label.TabIndex = 5;
            this.EntityClass_Label.Text = "label1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Resource Members";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(423, 167);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Entity Members";
            // 
            // ResourceTree
            // 
            this.ResourceTree.Location = new System.Drawing.Point(12, 183);
            this.ResourceTree.Name = "ResourceTree";
            this.ResourceTree.Size = new System.Drawing.Size(390, 213);
            this.ResourceTree.TabIndex = 8;
            // 
            // EntityTree
            // 
            this.EntityTree.Location = new System.Drawing.Point(417, 183);
            this.EntityTree.Name = "EntityTree";
            this.EntityTree.Size = new System.Drawing.Size(371, 213);
            this.EntityTree.TabIndex = 9;
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(632, 415);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 10;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(713, 415);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 11;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // Mapper
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.EntityTree);
            this.Controls.Add(this.ResourceTree);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.EntityClass_Label);
            this.Controls.Add(this.ResourceClass_Lablel);
            this.Controls.Add(this.Entity_Label);
            this.Controls.Add(this.Resource_Label);
            this.Controls.Add(this.Explanation);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Mapper";
            this.Text = "Map Resource Members";
            this.Load += new System.EventHandler(this.OnLoad);
            this.Resize += new System.EventHandler(this.OnResize);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label Explanation;
        private System.Windows.Forms.Label Resource_Label;
        private System.Windows.Forms.Label Entity_Label;
        private System.Windows.Forms.Label ResourceClass_Lablel;
        private System.Windows.Forms.Label EntityClass_Label;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TreeView ResourceTree;
        private System.Windows.Forms.TreeView EntityTree;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CancelBtn;
    }
}