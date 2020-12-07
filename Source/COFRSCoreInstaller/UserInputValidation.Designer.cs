namespace COFRSCoreInstaller
{
	partial class UserInputValidation
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
            this._removeServerButton = new System.Windows.Forms.Button();
            this._addServerButton = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this._titleLabel = new System.Windows.Forms.Label();
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
            this._resourceModelList = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this._entityModelList = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this._InstructionsLabel = new System.Windows.Forms.Label();
            this._portNumber = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this._profileModelList = new System.Windows.Forms.ComboBox();
            this._tableList = new System.Windows.Forms.ListBox();
            this.label8 = new System.Windows.Forms.Label();
            this._dbList = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _removeServerButton
            // 
            this._removeServerButton.Location = new System.Drawing.Point(222, 176);
            this._removeServerButton.Name = "_removeServerButton";
            this._removeServerButton.Size = new System.Drawing.Size(110, 23);
            this._removeServerButton.TabIndex = 52;
            this._removeServerButton.Text = "Remove Server";
            this._removeServerButton.UseVisualStyleBackColor = true;
            this._removeServerButton.Click += new System.EventHandler(this.OnRemoveServer);
            // 
            // _addServerButton
            // 
            this._addServerButton.Location = new System.Drawing.Point(103, 176);
            this._addServerButton.Name = "_addServerButton";
            this._addServerButton.Size = new System.Drawing.Size(113, 23);
            this._addServerButton.TabIndex = 51;
            this._addServerButton.Text = "Add New Server";
            this._addServerButton.UseVisualStyleBackColor = true;
            this._addServerButton.Click += new System.EventHandler(this.OnAddServer);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::COFRSCoreInstaller.Properties.Resources.ico128;
            this.pictureBox1.Location = new System.Drawing.Point(440, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.TabIndex = 50;
            this.pictureBox1.TabStop = false;
            // 
            // _titleLabel
            // 
            this._titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._titleLabel.Location = new System.Drawing.Point(574, 6);
            this._titleLabel.Name = "_titleLabel";
            this._titleLabel.Size = new System.Drawing.Size(259, 94);
            this._titleLabel.TabIndex = 49;
            this._titleLabel.Text = "COFRS AutoMapper Profile Generator";
            // 
            // _rememberPassword
            // 
            this._rememberPassword.AutoSize = true;
            this._rememberPassword.Location = new System.Drawing.Point(103, 151);
            this._rememberPassword.Name = "_rememberPassword";
            this._rememberPassword.Size = new System.Drawing.Size(126, 17);
            this._rememberPassword.TabIndex = 48;
            this._rememberPassword.Text = "Remember Password";
            this._rememberPassword.UseVisualStyleBackColor = true;
            this._rememberPassword.CheckedChanged += new System.EventHandler(this.OnSavePasswordChanged);
            // 
            // _password
            // 
            this._password.Font = new System.Drawing.Font("Wingdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this._password.Location = new System.Drawing.Point(103, 113);
            this._password.Name = "_password";
            this._password.PasswordChar = 'l';
            this._password.Size = new System.Drawing.Size(268, 20);
            this._password.TabIndex = 47;
            this._password.Leave += new System.EventHandler(this.OnPasswordChanged);
            // 
            // _passwordLabel
            // 
            this._passwordLabel.AutoSize = true;
            this._passwordLabel.Location = new System.Drawing.Point(12, 115);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(53, 13);
            this._passwordLabel.TabIndex = 46;
            this._passwordLabel.Text = "Password";
            // 
            // _userName
            // 
            this._userName.Location = new System.Drawing.Point(103, 87);
            this._userName.Name = "_userName";
            this._userName.Size = new System.Drawing.Size(268, 20);
            this._userName.TabIndex = 45;
            this._userName.Leave += new System.EventHandler(this.OnUserNameChanged);
            // 
            // _userNameLabel
            // 
            this._userNameLabel.AutoSize = true;
            this._userNameLabel.Location = new System.Drawing.Point(12, 90);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(60, 13);
            this._userNameLabel.TabIndex = 44;
            this._userNameLabel.Text = "User Name";
            // 
            // _authenticationList
            // 
            this._authenticationList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._authenticationList.FormattingEnabled = true;
            this._authenticationList.Items.AddRange(new object[] {
            "Windows Authority",
            "SQL Server Authority"});
            this._authenticationList.Location = new System.Drawing.Point(103, 60);
            this._authenticationList.Name = "_authenticationList";
            this._authenticationList.Size = new System.Drawing.Size(327, 21);
            this._authenticationList.TabIndex = 43;
            this._authenticationList.SelectedIndexChanged += new System.EventHandler(this.OnAuthenticationChanged);
            // 
            // _authenticationLabel
            // 
            this._authenticationLabel.AutoSize = true;
            this._authenticationLabel.Location = new System.Drawing.Point(12, 63);
            this._authenticationLabel.Name = "_authenticationLabel";
            this._authenticationLabel.Size = new System.Drawing.Size(75, 13);
            this._authenticationLabel.TabIndex = 42;
            this._authenticationLabel.Text = "Authentication";
            // 
            // _serverList
            // 
            this._serverList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverList.FormattingEnabled = true;
            this._serverList.Location = new System.Drawing.Point(103, 33);
            this._serverList.Name = "_serverList";
            this._serverList.Size = new System.Drawing.Size(327, 21);
            this._serverList.TabIndex = 41;
            this._serverList.SelectedIndexChanged += new System.EventHandler(this.OnServerChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 40;
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
            this._serverTypeList.Location = new System.Drawing.Point(103, 6);
            this._serverTypeList.Name = "_serverTypeList";
            this._serverTypeList.Size = new System.Drawing.Size(327, 21);
            this._serverTypeList.TabIndex = 39;
            this._serverTypeList.SelectedIndexChanged += new System.EventHandler(this.OnServerTypeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 38;
            this.label1.Text = "Server Type";
            // 
            // _resourceModelList
            // 
            this._resourceModelList.FormattingEnabled = true;
            this._resourceModelList.Location = new System.Drawing.Point(103, 244);
            this._resourceModelList.Name = "_resourceModelList";
            this._resourceModelList.Size = new System.Drawing.Size(317, 21);
            this._resourceModelList.TabIndex = 57;
            this._resourceModelList.SelectedIndexChanged += new System.EventHandler(this.OnResourceModelChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 247);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(85, 13);
            this.label9.TabIndex = 56;
            this.label9.Text = "Resource Model";
            // 
            // _entityModelList
            // 
            this._entityModelList.FormattingEnabled = true;
            this._entityModelList.Location = new System.Drawing.Point(103, 217);
            this._entityModelList.Name = "_entityModelList";
            this._entityModelList.Size = new System.Drawing.Size(317, 21);
            this._entityModelList.TabIndex = 55;
            this._entityModelList.SelectedIndexChanged += new System.EventHandler(this.OnEntityModelChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 220);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 13);
            this.label6.TabIndex = 54;
            this.label6.Text = "Entity Model";
            // 
            // _InstructionsLabel
            // 
            this._InstructionsLabel.Location = new System.Drawing.Point(440, 154);
            this._InstructionsLabel.Name = "_InstructionsLabel";
            this._InstructionsLabel.Size = new System.Drawing.Size(393, 61);
            this._InstructionsLabel.TabIndex = 53;
            // 
            // _portNumber
            // 
            this._portNumber.Location = new System.Drawing.Point(443, 245);
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
            this._portNumber.TabIndex = 58;
            this._portNumber.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._portNumber.ValueChanged += new System.EventHandler(this.OnPortNumberChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 274);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 59;
            this.label3.Text = "Profile";
            // 
            // _profileModelList
            // 
            this._profileModelList.FormattingEnabled = true;
            this._profileModelList.Location = new System.Drawing.Point(103, 271);
            this._profileModelList.Name = "_profileModelList";
            this._profileModelList.Size = new System.Drawing.Size(317, 21);
            this._profileModelList.TabIndex = 60;
            this._profileModelList.SelectedIndexChanged += new System.EventHandler(this.OnProfileChanged);
            // 
            // _tableList
            // 
            this._tableList.FormattingEnabled = true;
            this._tableList.Location = new System.Drawing.Point(437, 315);
            this._tableList.Name = "_tableList";
            this._tableList.Size = new System.Drawing.Size(393, 160);
            this._tableList.TabIndex = 64;
            this._tableList.SelectedIndexChanged += new System.EventHandler(this.OnSelectedTableChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(434, 299);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(39, 13);
            this.label8.TabIndex = 63;
            this.label8.Text = "Tables";
            // 
            // _dbList
            // 
            this._dbList.FormattingEnabled = true;
            this._dbList.Location = new System.Drawing.Point(12, 315);
            this._dbList.Name = "_dbList";
            this._dbList.Size = new System.Drawing.Size(402, 160);
            this._dbList.TabIndex = 62;
            this._dbList.SelectedIndexChanged += new System.EventHandler(this.OnSelectedDatabaseChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 299);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 13);
            this.label7.TabIndex = 61;
            this.label7.Text = "Databases";
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(673, 15);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 22;
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
            this._cancelButton.TabIndex = 26;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this._okButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Location = new System.Drawing.Point(-12, 490);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(902, 100);
            this.panel1.TabIndex = 65;
            // 
            // UserInputValidation
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(845, 539);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._tableList);
            this.Controls.Add(this.label8);
            this.Controls.Add(this._dbList);
            this.Controls.Add(this.label7);
            this.Controls.Add(this._profileModelList);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._portNumber);
            this.Controls.Add(this._resourceModelList);
            this.Controls.Add(this.label9);
            this.Controls.Add(this._entityModelList);
            this.Controls.Add(this.label6);
            this.Controls.Add(this._InstructionsLabel);
            this.Controls.Add(this._removeServerButton);
            this.Controls.Add(this._addServerButton);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this._titleLabel);
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
            this.Name = "UserInputValidation";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Add Validation";
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _removeServerButton;
		private System.Windows.Forms.Button _addServerButton;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label _titleLabel;
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
		public System.Windows.Forms.ComboBox _resourceModelList;
		private System.Windows.Forms.Label label9;
		public System.Windows.Forms.ComboBox _entityModelList;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label _InstructionsLabel;
		private System.Windows.Forms.NumericUpDown _portNumber;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.ComboBox _profileModelList;
		private System.Windows.Forms.ListBox _tableList;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.ListBox _dbList;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Button _cancelButton;
		private System.Windows.Forms.Panel panel1;
	}
}