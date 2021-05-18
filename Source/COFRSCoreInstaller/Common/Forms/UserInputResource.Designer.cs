namespace COFRS.Template.Common.Forms
{
    partial class UserInputResource
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInputResource));
            this._serverList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._password = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._serverTypeList = new System.Windows.Forms.ComboBox();
            this._dbList = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this._tableList = new System.Windows.Forms.ListBox();
            this._okButton = new System.Windows.Forms.Button();
            this._rememberPassword = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this._entityClassList = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this._cancelButton = new System.Windows.Forms.Button();
            this._addServer = new System.Windows.Forms.Button();
            this._authenticationLabel = new System.Windows.Forms.Label();
            this._authenticationList = new System.Windows.Forms.ComboBox();
            this._userNameLabel = new System.Windows.Forms.Label();
            this._userName = new System.Windows.Forms.TextBox();
            this._removeServer = new System.Windows.Forms.Button();
            this._portNumber = new System.Windows.Forms.NumericUpDown();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _serverList
            // 
            this._serverList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverList.FormattingEnabled = true;
            this._serverList.Location = new System.Drawing.Point(93, 33);
            this._serverList.Name = "_serverList";
            this._serverList.Size = new System.Drawing.Size(356, 21);
            this._serverList.TabIndex = 3;
            this._serverList.SelectedIndexChanged += new System.EventHandler(this.OnServerChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Server";
            // 
            // _passwordLabel
            // 
            this._passwordLabel.AutoSize = true;
            this._passwordLabel.Location = new System.Drawing.Point(12, 113);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(53, 13);
            this._passwordLabel.TabIndex = 8;
            this._passwordLabel.Text = "Password";
            // 
            // _password
            // 
            this._password.Font = new System.Drawing.Font("Wingdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this._password.Location = new System.Drawing.Point(93, 113);
            this._password.Name = "_password";
            this._password.PasswordChar = 'l';
            this._password.Size = new System.Drawing.Size(268, 20);
            this._password.TabIndex = 9;
            this._password.Leave += new System.EventHandler(this.OnPasswordChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Server Type";
            // 
            // _serverTypeList
            // 
            this._serverTypeList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverTypeList.FormattingEnabled = true;
            this._serverTypeList.Items.AddRange(new object[] {
            "MySql",
            "Postgresql Server",
            "SQL Server"});
            this._serverTypeList.Location = new System.Drawing.Point(93, 6);
            this._serverTypeList.Name = "_serverTypeList";
            this._serverTypeList.Size = new System.Drawing.Size(356, 21);
            this._serverTypeList.TabIndex = 1;
            this._serverTypeList.SelectedIndexChanged += new System.EventHandler(this.OnServerTypeChanged);
            // 
            // _dbList
            // 
            this._dbList.FormattingEnabled = true;
            this._dbList.IntegralHeight = false;
            this._dbList.Location = new System.Drawing.Point(15, 267);
            this._dbList.Name = "_dbList";
            this._dbList.Size = new System.Drawing.Size(434, 170);
            this._dbList.TabIndex = 16;
            this._dbList.SelectedIndexChanged += new System.EventHandler(this.OnSelectedDatabaseChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 251);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Database";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(469, 251);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "Table";
            // 
            // _tableList
            // 
            this._tableList.IntegralHeight = false;
            this._tableList.Location = new System.Drawing.Point(472, 267);
            this._tableList.Name = "_tableList";
            this._tableList.Size = new System.Drawing.Size(422, 170);
            this._tableList.Sorted = true;
            this._tableList.TabIndex = 18;
            this._tableList.SelectedIndexChanged += new System.EventHandler(this.OnSelectedTableChanged);
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(741, 16);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 19;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _rememberPassword
            // 
            this._rememberPassword.AutoSize = true;
            this._rememberPassword.Location = new System.Drawing.Point(93, 145);
            this._rememberPassword.Name = "_rememberPassword";
            this._rememberPassword.Size = new System.Drawing.Size(126, 17);
            this._rememberPassword.TabIndex = 10;
            this._rememberPassword.Text = "Remember Password";
            this._rememberPassword.UseVisualStyleBackColor = true;
            this._rememberPassword.CheckedChanged += new System.EventHandler(this.OnSavePasswordChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 218);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(61, 13);
            this.label9.TabIndex = 13;
            this.label9.Text = "Entity Class";
            // 
            // _entityClassList
            // 
            this._entityClassList.FormattingEnabled = true;
            this._entityClassList.Location = new System.Drawing.Point(93, 215);
            this._entityClassList.Name = "_entityClassList";
            this._entityClassList.Size = new System.Drawing.Size(356, 21);
            this._entityClassList.TabIndex = 14;
            this._entityClassList.SelectedIndexChanged += new System.EventHandler(this.OnEntityClassFileChanged);
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(617, 9);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(267, 136);
            this.label10.TabIndex = 21;
            this.label10.Text = "COFRS Resource Model Generator";
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(469, 145);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(425, 91);
            this.label11.TabIndex = 22;
            this.label11.Text = resources.GetString("label11.Text");
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(827, 16);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 20;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _addServer
            // 
            this._addServer.Location = new System.Drawing.Point(93, 176);
            this._addServer.Name = "_addServer";
            this._addServer.Size = new System.Drawing.Size(113, 23);
            this._addServer.TabIndex = 11;
            this._addServer.Text = "Add New Server";
            this._addServer.UseVisualStyleBackColor = true;
            this._addServer.Click += new System.EventHandler(this.OnAddServer);
            // 
            // _authenticationLabel
            // 
            this._authenticationLabel.AutoSize = true;
            this._authenticationLabel.Location = new System.Drawing.Point(12, 63);
            this._authenticationLabel.Name = "_authenticationLabel";
            this._authenticationLabel.Size = new System.Drawing.Size(75, 13);
            this._authenticationLabel.TabIndex = 4;
            this._authenticationLabel.Text = "Authentication";
            // 
            // _authenticationList
            // 
            this._authenticationList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._authenticationList.FormattingEnabled = true;
            this._authenticationList.Items.AddRange(new object[] {
            "SQL Server Authentication",
            "Windows Authentication"});
            this._authenticationList.Location = new System.Drawing.Point(93, 60);
            this._authenticationList.Name = "_authenticationList";
            this._authenticationList.Size = new System.Drawing.Size(356, 21);
            this._authenticationList.TabIndex = 5;
            this._authenticationList.SelectedIndexChanged += new System.EventHandler(this.OnAuthenticationChanged);
            // 
            // _userNameLabel
            // 
            this._userNameLabel.AutoSize = true;
            this._userNameLabel.Location = new System.Drawing.Point(12, 90);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(60, 13);
            this._userNameLabel.TabIndex = 6;
            this._userNameLabel.Text = "User Name";
            // 
            // _userName
            // 
            this._userName.Location = new System.Drawing.Point(93, 87);
            this._userName.Name = "_userName";
            this._userName.Size = new System.Drawing.Size(268, 20);
            this._userName.TabIndex = 7;
            this._userName.Leave += new System.EventHandler(this.OnUserNameChanged);
            // 
            // _removeServer
            // 
            this._removeServer.Location = new System.Drawing.Point(212, 176);
            this._removeServer.Name = "_removeServer";
            this._removeServer.Size = new System.Drawing.Size(110, 23);
            this._removeServer.TabIndex = 12;
            this._removeServer.Text = "Remove Server";
            this._removeServer.UseVisualStyleBackColor = true;
            this._removeServer.Click += new System.EventHandler(this.OnRemoveServer);
            // 
            // _portNumber
            // 
            this._portNumber.Location = new System.Drawing.Point(472, 218);
            this._portNumber.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this._portNumber.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._portNumber.Name = "_portNumber";
            this._portNumber.Size = new System.Drawing.Size(120, 20);
            this._portNumber.TabIndex = 46;
            this._portNumber.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this._portNumber.ValueChanged += new System.EventHandler(this.OnPortNumberChanged);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::COFRS.Template.Properties.Resources.ico128;
            this.pictureBox2.Location = new System.Drawing.Point(472, 6);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(128, 128);
            this.pictureBox2.TabIndex = 45;
            this.pictureBox2.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this._okButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Location = new System.Drawing.Point(-8, 454);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(985, 100);
            this.panel1.TabIndex = 47;
            // 
            // UserInputResource
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(906, 505);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._portNumber);
            this.Controls.Add(this._removeServer);
            this.Controls.Add(this._userName);
            this.Controls.Add(this._userNameLabel);
            this.Controls.Add(this._authenticationList);
            this.Controls.Add(this._authenticationLabel);
            this.Controls.Add(this._addServer);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this._entityClassList);
            this.Controls.Add(this.label9);
            this.Controls.Add(this._rememberPassword);
            this.Controls.Add(this.label5);
            this.Controls.Add(this._tableList);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._dbList);
            this.Controls.Add(this._serverTypeList);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._password);
            this.Controls.Add(this._passwordLabel);
            this.Controls.Add(this._serverList);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "UserInputResource";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "COFRS Domain Model Generator";
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox _serverList;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label _passwordLabel;
		private System.Windows.Forms.TextBox _password;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox _serverTypeList;
		private System.Windows.Forms.ListBox _dbList;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ListBox _tableList;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.CheckBox _rememberPassword;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Button _cancelButton;
		public System.Windows.Forms.ComboBox _entityClassList;
		private System.Windows.Forms.Button _addServer;
		private System.Windows.Forms.Label _authenticationLabel;
		private System.Windows.Forms.ComboBox _authenticationList;
		private System.Windows.Forms.Label _userNameLabel;
		private System.Windows.Forms.TextBox _userName;
		private System.Windows.Forms.Button _removeServer;
		private System.Windows.Forms.NumericUpDown _portNumber;
		private System.Windows.Forms.Panel panel1;
	}
}