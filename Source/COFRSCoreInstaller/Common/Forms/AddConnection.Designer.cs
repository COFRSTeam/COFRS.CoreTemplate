namespace COFRS.Template.Common.Forms
{
    partial class AddConnection
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
            this.label1 = new System.Windows.Forms.Label();
            this._dbServerType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this._serverName = new System.Windows.Forms.TextBox();
            this._authenticationLabel = new System.Windows.Forms.Label();
            this._authentication = new System.Windows.Forms.ComboBox();
            this._userNameLabel = new System.Windows.Forms.Label();
            this._userName = new System.Windows.Forms.TextBox();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._password = new System.Windows.Forms.TextBox();
            this._rememberPassword = new System.Windows.Forms.CheckBox();
            this._checkConnectionButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._checkConnectionResult = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this._portNumber = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server Type";
            // 
            // _dbServerType
            // 
            this._dbServerType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._dbServerType.FormattingEnabled = true;
            this._dbServerType.Items.AddRange(new object[] {
            "MySql",
            "Postgresql",
            "SQL Server"});
            this._dbServerType.Location = new System.Drawing.Point(133, 10);
            this._dbServerType.Name = "_dbServerType";
            this._dbServerType.Size = new System.Drawing.Size(287, 21);
            this._dbServerType.TabIndex = 1;
            this._dbServerType.SelectedIndexChanged += new System.EventHandler(this.OnServerTypeChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Server Name";
            // 
            // _serverName
            // 
            this._serverName.Location = new System.Drawing.Point(133, 37);
            this._serverName.Name = "_serverName";
            this._serverName.Size = new System.Drawing.Size(287, 20);
            this._serverName.TabIndex = 3;
            // 
            // _authenticationLabel
            // 
            this._authenticationLabel.AutoSize = true;
            this._authenticationLabel.Location = new System.Drawing.Point(22, 66);
            this._authenticationLabel.Name = "_authenticationLabel";
            this._authenticationLabel.Size = new System.Drawing.Size(75, 13);
            this._authenticationLabel.TabIndex = 4;
            this._authenticationLabel.Text = "Authentication";
            // 
            // _authentication
            // 
            this._authentication.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._authentication.FormattingEnabled = true;
            this._authentication.Items.AddRange(new object[] {
            "SQL Server Authentication",
            "Windows Authentication"});
            this._authentication.Location = new System.Drawing.Point(133, 63);
            this._authentication.Name = "_authentication";
            this._authentication.Size = new System.Drawing.Size(287, 21);
            this._authentication.TabIndex = 5;
            this._authentication.SelectedIndexChanged += new System.EventHandler(this.OnAuthenticationChanged);
            // 
            // _userNameLabel
            // 
            this._userNameLabel.AutoSize = true;
            this._userNameLabel.Location = new System.Drawing.Point(22, 94);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(57, 13);
            this._userNameLabel.TabIndex = 6;
            this._userNameLabel.Text = "UserName";
            // 
            // _userName
            // 
            this._userName.Location = new System.Drawing.Point(133, 91);
            this._userName.Name = "_userName";
            this._userName.Size = new System.Drawing.Size(287, 20);
            this._userName.TabIndex = 7;
            // 
            // _passwordLabel
            // 
            this._passwordLabel.AutoSize = true;
            this._passwordLabel.Location = new System.Drawing.Point(22, 120);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(53, 13);
            this._passwordLabel.TabIndex = 8;
            this._passwordLabel.Text = "Password";
            // 
            // _password
            // 
            this._password.Font = new System.Drawing.Font("Wingdings", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this._password.Location = new System.Drawing.Point(133, 117);
            this._password.Name = "_password";
            this._password.PasswordChar = 'l';
            this._password.Size = new System.Drawing.Size(287, 20);
            this._password.TabIndex = 9;
            // 
            // _rememberPassword
            // 
            this._rememberPassword.AutoSize = true;
            this._rememberPassword.Location = new System.Drawing.Point(133, 157);
            this._rememberPassword.Name = "_rememberPassword";
            this._rememberPassword.Size = new System.Drawing.Size(126, 17);
            this._rememberPassword.TabIndex = 10;
            this._rememberPassword.Text = "Remember Password";
            this._rememberPassword.UseVisualStyleBackColor = true;
            // 
            // _checkConnectionButton
            // 
            this._checkConnectionButton.Location = new System.Drawing.Point(165, 229);
            this._checkConnectionButton.Name = "_checkConnectionButton";
            this._checkConnectionButton.Size = new System.Drawing.Size(75, 23);
            this._checkConnectionButton.TabIndex = 11;
            this._checkConnectionButton.Text = "Check Connection";
            this._checkConnectionButton.UseVisualStyleBackColor = true;
            this._checkConnectionButton.Click += new System.EventHandler(this.OnCheckConnection);
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(256, 229);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 12;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOKPressed);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(345, 229);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 13;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _checkConnectionResult
            // 
            this._checkConnectionResult.Location = new System.Drawing.Point(150, 182);
            this._checkConnectionResult.Name = "_checkConnectionResult";
            this._checkConnectionResult.Size = new System.Drawing.Size(270, 23);
            this._checkConnectionResult.TabIndex = 14;
            this._checkConnectionResult.Text = "label4";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(-130, 208);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(825, 4);
            this.pictureBox1.TabIndex = 15;
            this.pictureBox1.TabStop = false;
            // 
            // _portNumber
            // 
            this._portNumber.Location = new System.Drawing.Point(133, 64);
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
            this._portNumber.TabIndex = 16;
            this._portNumber.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            // 
            // AddConnection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(436, 269);
            this.Controls.Add(this._portNumber);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this._checkConnectionResult);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._checkConnectionButton);
            this.Controls.Add(this._rememberPassword);
            this.Controls.Add(this._password);
            this.Controls.Add(this._passwordLabel);
            this.Controls.Add(this._userName);
            this.Controls.Add(this._userNameLabel);
            this.Controls.Add(this._authentication);
            this.Controls.Add(this._authenticationLabel);
            this.Controls.Add(this._serverName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._dbServerType);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AddConnection";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add SQL Connection";
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._portNumber)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox _dbServerType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox _serverName;
		private System.Windows.Forms.Label _authenticationLabel;
		private System.Windows.Forms.ComboBox _authentication;
		private System.Windows.Forms.Label _userNameLabel;
		private System.Windows.Forms.TextBox _userName;
		private System.Windows.Forms.Label _passwordLabel;
		private System.Windows.Forms.TextBox _password;
		private System.Windows.Forms.CheckBox _rememberPassword;
		private System.Windows.Forms.Button _checkConnectionButton;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Button _cancelButton;
		private System.Windows.Forms.Label _checkConnectionResult;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.NumericUpDown _portNumber;
	}
}