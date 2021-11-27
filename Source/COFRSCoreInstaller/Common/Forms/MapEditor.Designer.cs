
namespace COFRS.Template.Common.Wizards
{
    partial class MapEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapEditor));
            this.label2 = new System.Windows.Forms.Label();
            this.DestinationMemberLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.MappingFunctionTextBox = new System.Windows.Forms.TextBox();
            this.MappedResourcesLabel = new System.Windows.Forms.Label();
            this.MappedList = new System.Windows.Forms.ListBox();
            this.UnmappedResourcesLabel = new System.Windows.Forms.Label();
            this.UnmappedList = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.UnmapButton = new System.Windows.Forms.Button();
            this.MapButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(165, 127);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Destination Member";
            // 
            // DestinationMemberLabel
            // 
            this.DestinationMemberLabel.AutoSize = true;
            this.DestinationMemberLabel.Location = new System.Drawing.Point(282, 127);
            this.DestinationMemberLabel.Name = "DestinationMemberLabel";
            this.DestinationMemberLabel.Size = new System.Drawing.Size(35, 13);
            this.DestinationMemberLabel.TabIndex = 3;
            this.DestinationMemberLabel.Text = "label3";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 148);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Mapping Function";
            // 
            // MappingFunctionTextBox
            // 
            this.MappingFunctionTextBox.Location = new System.Drawing.Point(12, 164);
            this.MappingFunctionTextBox.Multiline = true;
            this.MappingFunctionTextBox.Name = "MappingFunctionTextBox";
            this.MappingFunctionTextBox.Size = new System.Drawing.Size(624, 120);
            this.MappingFunctionTextBox.TabIndex = 5;
            // 
            // MappedResourcesLabel
            // 
            this.MappedResourcesLabel.AutoSize = true;
            this.MappedResourcesLabel.Location = new System.Drawing.Point(12, 298);
            this.MappedResourcesLabel.Name = "MappedResourcesLabel";
            this.MappedResourcesLabel.Size = new System.Drawing.Size(129, 13);
            this.MappedResourcesLabel.TabIndex = 6;
            this.MappedResourcesLabel.Text = "Mapped Source Members";
            // 
            // MappedList
            // 
            this.MappedList.FormattingEnabled = true;
            this.MappedList.Location = new System.Drawing.Point(12, 314);
            this.MappedList.Name = "MappedList";
            this.MappedList.Size = new System.Drawing.Size(274, 147);
            this.MappedList.TabIndex = 7;
            // 
            // UnmappedResourcesLabel
            // 
            this.UnmappedResourcesLabel.AutoSize = true;
            this.UnmappedResourcesLabel.Location = new System.Drawing.Point(355, 298);
            this.UnmappedResourcesLabel.Name = "UnmappedResourcesLabel";
            this.UnmappedResourcesLabel.Size = new System.Drawing.Size(142, 13);
            this.UnmappedResourcesLabel.TabIndex = 10;
            this.UnmappedResourcesLabel.Text = "Unmapped Source Members";
            // 
            // UnmappedList
            // 
            this.UnmappedList.FormattingEnabled = true;
            this.UnmappedList.Location = new System.Drawing.Point(358, 314);
            this.UnmappedList.Name = "UnmappedList";
            this.UnmappedList.Size = new System.Drawing.Size(278, 147);
            this.UnmappedList.TabIndex = 11;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this._okButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Location = new System.Drawing.Point(-5, 480);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1234, 106);
            this.panel1.TabIndex = 85;
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(479, 12);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 26;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(560, 12);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 27;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // UnmapButton
            // 
            this.UnmapButton.Location = new System.Drawing.Point(300, 330);
            this.UnmapButton.Name = "UnmapButton";
            this.UnmapButton.Size = new System.Drawing.Size(41, 23);
            this.UnmapButton.TabIndex = 86;
            this.UnmapButton.Text = ">>";
            this.UnmapButton.UseVisualStyleBackColor = true;
            // 
            // MapButton
            // 
            this.MapButton.Location = new System.Drawing.Point(300, 375);
            this.MapButton.Name = "MapButton";
            this.MapButton.Size = new System.Drawing.Size(41, 23);
            this.MapButton.TabIndex = 87;
            this.MapButton.Text = "<<";
            this.MapButton.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::COFRS.Template.Properties.Resources.ico128;
            this.pictureBox1.Location = new System.Drawing.Point(15, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.TabIndex = 88;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(264, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(195, 31);
            this.label1.TabIndex = 89;
            this.label1.Text = "Mapping Editor";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(168, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(462, 70);
            this.label4.TabIndex = 90;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // MapEditor
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(648, 527);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.MapButton);
            this.Controls.Add(this.UnmapButton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.UnmappedList);
            this.Controls.Add(this.UnmappedResourcesLabel);
            this.Controls.Add(this.MappedList);
            this.Controls.Add(this.MappedResourcesLabel);
            this.Controls.Add(this.MappingFunctionTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.DestinationMemberLabel);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MapEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Maping Editor";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox MappingFunctionTextBox;
        public System.Windows.Forms.ListBox MappedList;
        public System.Windows.Forms.Label DestinationMemberLabel;
        public System.Windows.Forms.ListBox UnmappedList;
        public System.Windows.Forms.Label MappedResourcesLabel;
        public System.Windows.Forms.Label UnmappedResourcesLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button UnmapButton;
        private System.Windows.Forms.Button MapButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
    }
}