
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
            this.label2 = new System.Windows.Forms.Label();
            this.DestinationMemberLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.MappingFunctionTextBox = new System.Windows.Forms.TextBox();
            this.MappedResourcesLabel = new System.Windows.Forms.Label();
            this.MappedList = new System.Windows.Forms.ListBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.UnmappedResourcesLabel = new System.Windows.Forms.Label();
            this.UnmappedList = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Destination Member";
            // 
            // DestinationMemberLabel
            // 
            this.DestinationMemberLabel.AutoSize = true;
            this.DestinationMemberLabel.Location = new System.Drawing.Point(119, 9);
            this.DestinationMemberLabel.Name = "DestinationMemberLabel";
            this.DestinationMemberLabel.Size = new System.Drawing.Size(35, 13);
            this.DestinationMemberLabel.TabIndex = 3;
            this.DestinationMemberLabel.Text = "label3";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Mapping Function";
            // 
            // MappingFunctionTextBox
            // 
            this.MappingFunctionTextBox.Location = new System.Drawing.Point(12, 55);
            this.MappingFunctionTextBox.Multiline = true;
            this.MappingFunctionTextBox.Name = "MappingFunctionTextBox";
            this.MappingFunctionTextBox.Size = new System.Drawing.Size(624, 120);
            this.MappingFunctionTextBox.TabIndex = 5;
            // 
            // MappedResourcesLabel
            // 
            this.MappedResourcesLabel.AutoSize = true;
            this.MappedResourcesLabel.Location = new System.Drawing.Point(12, 189);
            this.MappedResourcesLabel.Name = "MappedResourcesLabel";
            this.MappedResourcesLabel.Size = new System.Drawing.Size(129, 13);
            this.MappedResourcesLabel.TabIndex = 6;
            this.MappedResourcesLabel.Text = "Mapped Source Members";
            // 
            // MappedList
            // 
            this.MappedList.FormattingEnabled = true;
            this.MappedList.Location = new System.Drawing.Point(12, 205);
            this.MappedList.Name = "MappedList";
            this.MappedList.Size = new System.Drawing.Size(305, 147);
            this.MappedList.TabIndex = 7;
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(480, 370);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 8;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            // 
            // _cancelButton
            // 
            this._cancelButton.Location = new System.Drawing.Point(561, 370);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 9;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // UnmappedResourcesLabel
            // 
            this.UnmappedResourcesLabel.AutoSize = true;
            this.UnmappedResourcesLabel.Location = new System.Drawing.Point(328, 189);
            this.UnmappedResourcesLabel.Name = "UnmappedResourcesLabel";
            this.UnmappedResourcesLabel.Size = new System.Drawing.Size(142, 13);
            this.UnmappedResourcesLabel.TabIndex = 10;
            this.UnmappedResourcesLabel.Text = "Unmapped Source Members";
            // 
            // UnmappedList
            // 
            this.UnmappedList.FormattingEnabled = true;
            this.UnmappedList.Location = new System.Drawing.Point(331, 205);
            this.UnmappedList.Name = "UnmappedList";
            this.UnmappedList.Size = new System.Drawing.Size(305, 147);
            this.UnmappedList.TabIndex = 11;
            // 
            // MapEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(652, 414);
            this.Controls.Add(this.UnmappedList);
            this.Controls.Add(this.UnmappedResourcesLabel);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
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
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        public System.Windows.Forms.TextBox MappingFunctionTextBox;
        public System.Windows.Forms.ListBox MappedList;
        public System.Windows.Forms.Label DestinationMemberLabel;
        public System.Windows.Forms.ListBox UnmappedList;
        public System.Windows.Forms.Label MappedResourcesLabel;
        public System.Windows.Forms.Label UnmappedResourcesLabel;
    }
}