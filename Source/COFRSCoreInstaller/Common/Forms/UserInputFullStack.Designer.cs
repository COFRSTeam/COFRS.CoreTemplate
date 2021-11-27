
using System;
using System.Windows.Forms;

namespace COFRS.Template.Common.Forms
{
    partial class UserInputFullStack : Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInputFullStack));
            this.PluralName = new System.Windows.Forms.TextBox();
            this.SingularName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._portNumber = new System.Windows.Forms.NumericUpDown();
            this._removeServerButton = new System.Windows.Forms.Button();
            this._addServerButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this._InstructionsLabel = new System.Windows.Forms.Label();
            this._titleLabel = new System.Windows.Forms.Label();
            this._tableList = new System.Windows.Forms.ListBox();
            this.label8 = new System.Windows.Forms.Label();
            this._dbList = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this._rememberPassword = new System.Windows.Forms.CheckBox();
            this._password = new System.Windows.Forms.TextBox();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._userName = new System.Windows.Forms.TextBox();
            this._userNameLabel = new System.Windows.Forms.Label();
            this._authenticationList = new System.Windows.Forms.ComboBox();
            this._authenticationLabel = new System.Windows.Forms.Label();
            this._serverList = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this._serverTypeList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.policyCombo = new System.Windows.Forms.ComboBox();
            this.policyLabel = new System.Windows.Forms.Label();
            this.EntityModelCheckBox = new System.Windows.Forms.CheckBox();
            this.ResourceModelCheckBox = new System.Windows.Forms.CheckBox();
            this.MappingProfileCheckBox = new System.Windows.Forms.CheckBox();
            this.ValidatorCheckBox = new System.Windows.Forms.CheckBox();
            this.ExampleCheckBox = new System.Windows.Forms.CheckBox();
            this.ControllerCheckbox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // PluralName
            // 
            this.PluralName.Location = new System.Drawing.Point(216, 342);
            this.PluralName.Name = "PluralName";
            this.PluralName.Size = new System.Drawing.Size(198, 20);
            this.PluralName.TabIndex = 24;
            // 
            // SingularName
            // 
            this.SingularName.Location = new System.Drawing.Point(216, 313);
            this.SingularName.Name = "SingularName";
            this.SingularName.Size = new System.Drawing.Size(198, 20);
            this.SingularName.TabIndex = 23;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(150, 345);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "Plural";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(150, 316);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 21;
            this.label4.Text = "Singular";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 316);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "Resource Name";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this._okButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Location = new System.Drawing.Point(-12, 564);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(902, 100);
            this.panel1.TabIndex = 83;
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(673, 15);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 31;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(766, 15);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 32;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _portNumber
            // 
            this._portNumber.Location = new System.Drawing.Point(577, 117);
            this._portNumber.Maximum = new decimal(new int[] {
            65534,
            0,
            0,
            0});
            this._portNumber.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._portNumber.Name = "_portNumber";
            this._portNumber.Size = new System.Drawing.Size(126, 20);
            this._portNumber.TabIndex = 8;
            this._portNumber.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._portNumber.ValueChanged += new System.EventHandler(this.OnPortNumberChanged);
            // 
            // _removeServerButton
            // 
            this._removeServerButton.Location = new System.Drawing.Point(219, 182);
            this._removeServerButton.Name = "_removeServerButton";
            this._removeServerButton.Size = new System.Drawing.Size(110, 23);
            this._removeServerButton.TabIndex = 13;
            this._removeServerButton.Text = "Remove Server";
            this._removeServerButton.UseVisualStyleBackColor = true;
            this._removeServerButton.Click += new System.EventHandler(this.OnRemoveServer);
            // 
            // _addServerButton
            // 
            this._addServerButton.Location = new System.Drawing.Point(100, 182);
            this._addServerButton.Name = "_addServerButton";
            this._addServerButton.Size = new System.Drawing.Size(113, 23);
            this._addServerButton.TabIndex = 12;
            this._addServerButton.Text = "Add New Server";
            this._addServerButton.UseVisualStyleBackColor = true;
            this._addServerButton.Click += new System.EventHandler(this.OnAddNewServer);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::COFRS.Template.Properties.Resources.ico128;
            this.pictureBox1.Location = new System.Drawing.Point(437, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.TabIndex = 84;
            this.pictureBox1.TabStop = false;
            // 
            // _InstructionsLabel
            // 
            this._InstructionsLabel.Location = new System.Drawing.Point(437, 148);
            this._InstructionsLabel.Name = "_InstructionsLabel";
            this._InstructionsLabel.Size = new System.Drawing.Size(393, 76);
            this._InstructionsLabel.TabIndex = 27;
            this._InstructionsLabel.Text = resources.GetString("_InstructionsLabel.Text");
            // 
            // _titleLabel
            // 
            this._titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._titleLabel.Location = new System.Drawing.Point(571, 12);
            this._titleLabel.Name = "_titleLabel";
            this._titleLabel.Size = new System.Drawing.Size(259, 127);
            this._titleLabel.TabIndex = 26;
            this._titleLabel.Text = "COFRS Controller Class Generator Full Stack";
            // 
            // _tableList
            // 
            this._tableList.FormattingEnabled = true;
            this._tableList.Location = new System.Drawing.Point(437, 374);
            this._tableList.Name = "_tableList";
            this._tableList.Size = new System.Drawing.Size(393, 173);
            this._tableList.TabIndex = 30;
            this._tableList.SelectedIndexChanged += new System.EventHandler(this.OnTableChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(437, 358);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(39, 13);
            this.label8.TabIndex = 81;
            this.label8.Text = "Tables";
            // 
            // _dbList
            // 
            this._dbList.FormattingEnabled = true;
            this._dbList.Location = new System.Drawing.Point(12, 374);
            this._dbList.Name = "_dbList";
            this._dbList.Size = new System.Drawing.Size(402, 173);
            this._dbList.TabIndex = 25;
            this._dbList.SelectedIndexChanged += new System.EventHandler(this.OnDatabaseChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 358);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 77;
            this.label7.Text = "Databases";
            // 
            // _rememberPassword
            // 
            this._rememberPassword.AutoSize = true;
            this._rememberPassword.Location = new System.Drawing.Point(100, 157);
            this._rememberPassword.Name = "_rememberPassword";
            this._rememberPassword.Size = new System.Drawing.Size(126, 17);
            this._rememberPassword.TabIndex = 11;
            this._rememberPassword.Text = "Remember Password";
            this._rememberPassword.UseVisualStyleBackColor = true;
            this._rememberPassword.CheckedChanged += new System.EventHandler(this.OnRememberPasswordChanged);
            // 
            // _password
            // 
            this._password.Font = new System.Drawing.Font("Wingdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this._password.Location = new System.Drawing.Point(100, 119);
            this._password.Name = "_password";
            this._password.PasswordChar = 'l';
            this._password.Size = new System.Drawing.Size(268, 20);
            this._password.TabIndex = 10;
            this._password.TextChanged += new System.EventHandler(this.OnPasswordChanged);
            // 
            // _passwordLabel
            // 
            this._passwordLabel.AutoSize = true;
            this._passwordLabel.Location = new System.Drawing.Point(9, 119);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(53, 13);
            this._passwordLabel.TabIndex = 9;
            this._passwordLabel.Text = "Password";
            // 
            // _userName
            // 
            this._userName.Location = new System.Drawing.Point(100, 93);
            this._userName.Name = "_userName";
            this._userName.Size = new System.Drawing.Size(268, 20);
            this._userName.TabIndex = 8;
            this._userName.TextChanged += new System.EventHandler(this.OnUserNameChanged);
            // 
            // _userNameLabel
            // 
            this._userNameLabel.AutoSize = true;
            this._userNameLabel.Location = new System.Drawing.Point(9, 96);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(60, 13);
            this._userNameLabel.TabIndex = 7;
            this._userNameLabel.Text = "User Name";
            // 
            // _authenticationList
            // 
            this._authenticationList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._authenticationList.FormattingEnabled = true;
            this._authenticationList.Items.AddRange(new object[] {
            "Windows Authority",
            "SQL Server Authority"});
            this._authenticationList.Location = new System.Drawing.Point(100, 66);
            this._authenticationList.Name = "_authenticationList";
            this._authenticationList.Size = new System.Drawing.Size(327, 21);
            this._authenticationList.TabIndex = 6;
            this._authenticationList.SelectedIndexChanged += new System.EventHandler(this.OnAuthenticationChanged);
            // 
            // _authenticationLabel
            // 
            this._authenticationLabel.AutoSize = true;
            this._authenticationLabel.Location = new System.Drawing.Point(9, 69);
            this._authenticationLabel.Name = "_authenticationLabel";
            this._authenticationLabel.Size = new System.Drawing.Size(75, 13);
            this._authenticationLabel.TabIndex = 5;
            this._authenticationLabel.Text = "Authentication";
            // 
            // _serverList
            // 
            this._serverList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverList.FormattingEnabled = true;
            this._serverList.Location = new System.Drawing.Point(100, 39);
            this._serverList.Name = "_serverList";
            this._serverList.Size = new System.Drawing.Size(327, 21);
            this._serverList.TabIndex = 4;
            this._serverList.SelectedIndexChanged += new System.EventHandler(this.OnServerChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Server";
            // 
            // _serverTypeList
            // 
            this._serverTypeList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverTypeList.FormattingEnabled = true;
            this._serverTypeList.Items.AddRange(new object[] {
            "MySql",
            "Postgresql",
            "SQL Server"});
            this._serverTypeList.Location = new System.Drawing.Point(100, 12);
            this._serverTypeList.Name = "_serverTypeList";
            this._serverTypeList.Size = new System.Drawing.Size(327, 21);
            this._serverTypeList.TabIndex = 2;
            this._serverTypeList.SelectedIndexChanged += new System.EventHandler(this.OnServerTypeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Server Type";
            // 
            // policyCombo
            // 
            this.policyCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.policyCombo.FormattingEnabled = true;
            this.policyCombo.Location = new System.Drawing.Point(561, 337);
            this.policyCombo.Name = "policyCombo";
            this.policyCombo.Size = new System.Drawing.Size(269, 21);
            this.policyCombo.Sorted = true;
            this.policyCombo.TabIndex = 29;
            // 
            // policyLabel
            // 
            this.policyLabel.AutoSize = true;
            this.policyLabel.Location = new System.Drawing.Point(520, 340);
            this.policyLabel.Name = "policyLabel";
            this.policyLabel.Size = new System.Drawing.Size(35, 13);
            this.policyLabel.TabIndex = 28;
            this.policyLabel.Text = "Policy";
            // 
            // EntityModelCheckBox
            // 
            this.EntityModelCheckBox.AutoSize = true;
            this.EntityModelCheckBox.Checked = true;
            this.EntityModelCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EntityModelCheckBox.Location = new System.Drawing.Point(12, 230);
            this.EntityModelCheckBox.Name = "EntityModelCheckBox";
            this.EntityModelCheckBox.Size = new System.Drawing.Size(84, 17);
            this.EntityModelCheckBox.TabIndex = 14;
            this.EntityModelCheckBox.Text = "Entity Model";
            this.EntityModelCheckBox.UseVisualStyleBackColor = true;
            // 
            // ResourceModelCheckBox
            // 
            this.ResourceModelCheckBox.AutoSize = true;
            this.ResourceModelCheckBox.Checked = true;
            this.ResourceModelCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ResourceModelCheckBox.Location = new System.Drawing.Point(12, 253);
            this.ResourceModelCheckBox.Name = "ResourceModelCheckBox";
            this.ResourceModelCheckBox.Size = new System.Drawing.Size(104, 17);
            this.ResourceModelCheckBox.TabIndex = 15;
            this.ResourceModelCheckBox.Text = "Resource Model";
            this.ResourceModelCheckBox.UseVisualStyleBackColor = true;
            // 
            // MappingProfileCheckBox
            // 
            this.MappingProfileCheckBox.AutoSize = true;
            this.MappingProfileCheckBox.Checked = true;
            this.MappingProfileCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MappingProfileCheckBox.Location = new System.Drawing.Point(12, 276);
            this.MappingProfileCheckBox.Name = "MappingProfileCheckBox";
            this.MappingProfileCheckBox.Size = new System.Drawing.Size(99, 17);
            this.MappingProfileCheckBox.TabIndex = 16;
            this.MappingProfileCheckBox.Text = "Mapping Profile";
            this.MappingProfileCheckBox.UseVisualStyleBackColor = true;
            // 
            // ValidatorCheckBox
            // 
            this.ValidatorCheckBox.AutoSize = true;
            this.ValidatorCheckBox.Checked = true;
            this.ValidatorCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ValidatorCheckBox.Location = new System.Drawing.Point(216, 230);
            this.ValidatorCheckBox.Name = "ValidatorCheckBox";
            this.ValidatorCheckBox.Size = new System.Drawing.Size(67, 17);
            this.ValidatorCheckBox.TabIndex = 17;
            this.ValidatorCheckBox.Text = "Validator";
            this.ValidatorCheckBox.UseVisualStyleBackColor = true;
            // 
            // ExampleCheckBox
            // 
            this.ExampleCheckBox.AutoSize = true;
            this.ExampleCheckBox.Checked = true;
            this.ExampleCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ExampleCheckBox.Location = new System.Drawing.Point(216, 253);
            this.ExampleCheckBox.Name = "ExampleCheckBox";
            this.ExampleCheckBox.Size = new System.Drawing.Size(116, 17);
            this.ExampleCheckBox.TabIndex = 18;
            this.ExampleCheckBox.Text = "Swagger Examples";
            this.ExampleCheckBox.UseVisualStyleBackColor = true;
            // 
            // ControllerCheckbox
            // 
            this.ControllerCheckbox.AutoSize = true;
            this.ControllerCheckbox.Checked = true;
            this.ControllerCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ControllerCheckbox.Location = new System.Drawing.Point(216, 276);
            this.ControllerCheckbox.Name = "ControllerCheckbox";
            this.ControllerCheckbox.Size = new System.Drawing.Size(70, 17);
            this.ControllerCheckbox.TabIndex = 19;
            this.ControllerCheckbox.Text = "Controller";
            this.ControllerCheckbox.UseVisualStyleBackColor = true;
            // 
            // UserInputFullStack
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(843, 616);
            this.Controls.Add(this.ControllerCheckbox);
            this.Controls.Add(this.ExampleCheckBox);
            this.Controls.Add(this.ValidatorCheckBox);
            this.Controls.Add(this.MappingProfileCheckBox);
            this.Controls.Add(this.ResourceModelCheckBox);
            this.Controls.Add(this.EntityModelCheckBox);
            this.Controls.Add(this.policyLabel);
            this.Controls.Add(this.policyCombo);
            this.Controls.Add(this.PluralName);
            this.Controls.Add(this.SingularName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._portNumber);
            this.Controls.Add(this._removeServerButton);
            this.Controls.Add(this._addServerButton);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this._InstructionsLabel);
            this.Controls.Add(this._titleLabel);
            this.Controls.Add(this._tableList);
            this.Controls.Add(this.label8);
            this.Controls.Add(this._dbList);
            this.Controls.Add(this.label7);
            this.Controls.Add(this._rememberPassword);
            this.Controls.Add(this._password);
            this.Controls.Add(this._passwordLabel);
            this.Controls.Add(this._userName);
            this.Controls.Add(this._userNameLabel);
            this.Controls.Add(this._authenticationList);
            this.Controls.Add(this._authenticationLabel);
            this.Controls.Add(this._serverList);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._serverTypeList);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "UserInputFullStack";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "COFRS Controller Generator Full Stack";
            this.Load += new System.EventHandler(this.OnLoad);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox PluralName;
        private System.Windows.Forms.TextBox SingularName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.NumericUpDown _portNumber;
        private System.Windows.Forms.Button _removeServerButton;
        private System.Windows.Forms.Button _addServerButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label _InstructionsLabel;
        private System.Windows.Forms.Label _titleLabel;
        private System.Windows.Forms.ListBox _tableList;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ListBox _dbList;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox _rememberPassword;
        private System.Windows.Forms.TextBox _password;
        private System.Windows.Forms.Label _passwordLabel;
        private System.Windows.Forms.TextBox _userName;
        private System.Windows.Forms.Label _userNameLabel;
        private System.Windows.Forms.ComboBox _authenticationList;
        private System.Windows.Forms.Label _authenticationLabel;
        private System.Windows.Forms.ComboBox _serverList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox _serverTypeList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox policyCombo;
        private System.Windows.Forms.Label policyLabel;
        public CheckBox EntityModelCheckBox;
        public CheckBox ResourceModelCheckBox;
        public CheckBox MappingProfileCheckBox;
        public CheckBox ValidatorCheckBox;
        public CheckBox ExampleCheckBox;
        public CheckBox ControllerCheckbox;
    }
}