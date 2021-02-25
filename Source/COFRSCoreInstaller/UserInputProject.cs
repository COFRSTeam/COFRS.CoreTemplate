using System;
using System.Windows.Forms;

namespace COFRSCoreInstaller
{
	public partial class UserInputProject : Form
	{
		public string Framework { get; set; }
		public string SecurityModel { get; set; }
		public string DatabaseTechnology { get; set; }

		public UserInputProject()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			framework.SelectedIndex = 1;
			securityModel.SelectedIndex = 1;
			databaseList.SelectedIndex = 2;
		}

		private void OnOK(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(companyMoniker.Text))
			{
				MessageBox.Show("Company moniker cannot be blank.\r\nThe company moniker is a short name for your company or organization, similiar to a stock market ticker symbol. Select a 6 to twelve character name that describes your company or organization.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			switch (framework.SelectedIndex)
			{
				case 0:
					Framework = "netcoreapp3.1";
					break;

				case 1:
					Framework = "net5.0";
					break;
			}

			switch (securityModel.SelectedIndex)
			{
				case 0:
					SecurityModel = "None";
					break;

				case 1:
					SecurityModel = "OAuth";
					break;
			}

			switch (databaseList.SelectedIndex)
			{
				case 0:
					DatabaseTechnology = "MySQL";
					break;

				case 1:
					DatabaseTechnology = "Postgresql";
					break;

				case 2:
					DatabaseTechnology = "SQLServer";
					break;
			}
			
			DialogResult = DialogResult.OK;
			Close();
		}

		private void _cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
