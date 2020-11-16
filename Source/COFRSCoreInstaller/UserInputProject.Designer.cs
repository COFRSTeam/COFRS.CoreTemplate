namespace COFRSCoreInstaller
{
	partial class UserInputProject
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
			this.components = new System.ComponentModel.Container();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.framework = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.securityModel = new System.Windows.Forms.ComboBox();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this._okButton = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.companyMonikerTooltip = new System.Windows.Forms.ToolTip(this.components);
			this._cancelButton = new System.Windows.Forms.Button();
			this.databaseList = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.companyMoniker = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::COFRSCoreInstaller.Properties.Resources.ico128;
			this.pictureBox1.Location = new System.Drawing.Point(18, 54);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(128, 128);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(499, 33);
			this.label1.TabIndex = 1;
			this.label1.Text = "COFRS RESTful Service Template";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(162, 54);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(188, 60);
			this.label2.TabIndex = 2;
			this.label2.Text = "The Cookbook For RESTFul Services (COFRS) assists the developer in the cretion of" +
    " RESTful Services. ";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(162, 114);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(188, 125);
			this.label3.TabIndex = 3;
			this.label3.Text = "We recommend that you protect your service using the OAuth2 / OpenID Connect prot" +
    "ocol. However, this functionality can be added later if you choose not to do so " +
    "initially.";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(403, 54);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(59, 13);
			this.label4.TabIndex = 4;
			this.label4.Text = "Framework";
			// 
			// framework
			// 
			this.framework.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.framework.FormattingEnabled = true;
			this.framework.Items.AddRange(new object[] {
            ".NET Core 2.1",
            ".NET Core 3.1"});
			this.framework.Location = new System.Drawing.Point(406, 70);
			this.framework.Name = "framework";
			this.framework.Size = new System.Drawing.Size(216, 21);
			this.framework.TabIndex = 5;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(403, 101);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(77, 13);
			this.label5.TabIndex = 6;
			this.label5.Text = "Security Model";
			// 
			// securityModel
			// 
			this.securityModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.securityModel.FormattingEnabled = true;
			this.securityModel.Items.AddRange(new object[] {
            "None",
            "OAuth2 / Open Id Connect"});
			this.securityModel.Location = new System.Drawing.Point(406, 117);
			this.securityModel.Name = "securityModel";
			this.securityModel.Size = new System.Drawing.Size(216, 21);
			this.securityModel.TabIndex = 7;
			// 
			// pictureBox2
			// 
			this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBox2.Location = new System.Drawing.Point(-16, 253);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(713, 4);
			this.pictureBox2.TabIndex = 8;
			this.pictureBox2.TabStop = false;
			// 
			// _okButton
			// 
			this._okButton.Location = new System.Drawing.Point(456, 13);
			this._okButton.Name = "_okButton";
			this._okButton.Size = new System.Drawing.Size(75, 23);
			this._okButton.TabIndex = 9;
			this._okButton.Text = "OK";
			this._okButton.UseVisualStyleBackColor = true;
			this._okButton.Click += new System.EventHandler(this.OnOK);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(403, 149);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(112, 13);
			this.label6.TabIndex = 10;
			this.label6.Text = "Database Technology";
			// 
			// companyMonikerTooltip
			// 
			this.companyMonikerTooltip.ToolTipTitle = "Company Moniker";
			// 
			// _cancelButton
			// 
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.Location = new System.Drawing.Point(547, 13);
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.Size = new System.Drawing.Size(75, 23);
			this._cancelButton.TabIndex = 14;
			this._cancelButton.Text = "Cancel";
			this._cancelButton.UseVisualStyleBackColor = true;
			this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
			// 
			// databaseList
			// 
			this.databaseList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.databaseList.FormattingEnabled = true;
			this.databaseList.Items.AddRange(new object[] {
            "My SQL",
            "Postgresql",
            "SQL Server"});
			this.databaseList.Location = new System.Drawing.Point(406, 165);
			this.databaseList.Name = "databaseList";
			this.databaseList.Size = new System.Drawing.Size(216, 21);
			this.databaseList.TabIndex = 15;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(406, 200);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(92, 13);
			this.label7.TabIndex = 16;
			this.label7.Text = "Company Moniker";
			// 
			// companyMoniker
			// 
			this.companyMoniker.Location = new System.Drawing.Point(406, 216);
			this.companyMoniker.Name = "companyMoniker";
			this.companyMoniker.Size = new System.Drawing.Size(216, 20);
			this.companyMoniker.TabIndex = 17;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
			this.panel1.Controls.Add(this._okButton);
			this.panel1.Controls.Add(this._cancelButton);
			this.panel1.Location = new System.Drawing.Point(-5, 253);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(974, 100);
			this.panel1.TabIndex = 18;
			// 
			// UserInputProject
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(635, 302);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.companyMoniker);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.databaseList);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.securityModel);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.framework);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "UserInputProject";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "COFRS RESTful Service Template";
			this.Load += new System.EventHandler(this.OnLoad);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox framework;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox securityModel;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ToolTip companyMonikerTooltip;
		private System.Windows.Forms.Button _cancelButton;
		private System.Windows.Forms.ComboBox databaseList;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Panel panel1;
		public System.Windows.Forms.TextBox companyMoniker;
	}
}