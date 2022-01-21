namespace EarlyUpdateCheck
{
	partial class Options
	{
		/// <summary> 
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Komponenten-Designer generierter Code

		/// <summary> 
		/// Erforderliche Methode für die Designerunterstützung. 
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
            this.tcEUC = new System.Windows.Forms.TabControl();
            this.tpEUCOptions = new System.Windows.Forms.TabPage();
            this.tpEUC3rdParty = new System.Windows.Forms.TabPage();
            this.bDownloadExternalPluginUpdates = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tbFile = new System.Windows.Forms.TextBox();
            this.lFile = new System.Windows.Forms.LinkLabel();
            this.lv3rdPartyPlugins = new System.Windows.Forms.ListView();
            this.tpKeePass = new System.Windows.Forms.TabPage();
            this.gOneClickUpdate = new RookieUI.CheckedGroupBox();
            this.bUpdateTranslations = new System.Windows.Forms.Button();
            this.cbDownloadCurrentTranslation = new System.Windows.Forms.CheckBox();
            this.tbOneClickUpdateDesc = new System.Windows.Forms.TextBox();
            this.gCheckSync = new RookieUI.CheckedGroupBox();
            this.tbCheckSyncDesc = new System.Windows.Forms.TextBox();
            this.cbCheckSync = new System.Windows.Forms.CheckBox();
            this.cgKeePassUpdate = new RookieUI.CheckedGroupBox();
            this.cbKeePassInstallType = new System.Windows.Forms.ComboBox();
            this.lKeePassInstallType = new System.Windows.Forms.Label();
            this.tbKeePassFolder = new System.Windows.Forms.TextBox();
            this.lKeePassFolder = new System.Windows.Forms.LinkLabel();
            this.tbKeePassUpdateInfo = new System.Windows.Forms.TextBox();
            this.tcEUC.SuspendLayout();
            this.tpEUCOptions.SuspendLayout();
            this.tpEUC3rdParty.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tpKeePass.SuspendLayout();
            this.gOneClickUpdate.SuspendLayout();
            this.gCheckSync.SuspendLayout();
            this.cgKeePassUpdate.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcEUC
            // 
            this.tcEUC.Controls.Add(this.tpEUCOptions);
            this.tcEUC.Controls.Add(this.tpEUC3rdParty);
            this.tcEUC.Controls.Add(this.tpKeePass);
            this.tcEUC.Dock = System.Windows.Forms.DockStyle.Top;
            this.tcEUC.Location = new System.Drawing.Point(0, 0);
            this.tcEUC.Margin = new System.Windows.Forms.Padding(5);
            this.tcEUC.Name = "tcEUC";
            this.tcEUC.SelectedIndex = 0;
            this.tcEUC.Size = new System.Drawing.Size(1339, 634);
            this.tcEUC.TabIndex = 12;
            // 
            // tpEUCOptions
            // 
            this.tpEUCOptions.Controls.Add(this.gOneClickUpdate);
            this.tpEUCOptions.Controls.Add(this.gCheckSync);
            this.tpEUCOptions.Location = new System.Drawing.Point(10, 48);
            this.tpEUCOptions.Margin = new System.Windows.Forms.Padding(5);
            this.tpEUCOptions.Name = "tpEUCOptions";
            this.tpEUCOptions.Padding = new System.Windows.Forms.Padding(5);
            this.tpEUCOptions.Size = new System.Drawing.Size(1319, 576);
            this.tpEUCOptions.TabIndex = 0;
            this.tpEUCOptions.Text = "Options";
            this.tpEUCOptions.UseVisualStyleBackColor = true;
            // 
            // tpEUC3rdParty
            // 
            this.tpEUC3rdParty.Controls.Add(this.bDownloadExternalPluginUpdates);
            this.tpEUC3rdParty.Controls.Add(this.panel1);
            this.tpEUC3rdParty.Controls.Add(this.lv3rdPartyPlugins);
            this.tpEUC3rdParty.Location = new System.Drawing.Point(10, 48);
            this.tpEUC3rdParty.Margin = new System.Windows.Forms.Padding(5);
            this.tpEUC3rdParty.Name = "tpEUC3rdParty";
            this.tpEUC3rdParty.Padding = new System.Windows.Forms.Padding(18, 15, 18, 15);
            this.tpEUC3rdParty.Size = new System.Drawing.Size(1319, 576);
            this.tpEUC3rdParty.TabIndex = 1;
            this.tpEUC3rdParty.Text = "3rdParty";
            this.tpEUC3rdParty.UseVisualStyleBackColor = true;
            this.tpEUC3rdParty.Resize += new System.EventHandler(this.OnShow3rdPartyTab);
            // 
            // bDownloadExternalPluginUpdates
            // 
            this.bDownloadExternalPluginUpdates.Dock = System.Windows.Forms.DockStyle.Top;
            this.bDownloadExternalPluginUpdates.Location = new System.Drawing.Point(18, 72);
            this.bDownloadExternalPluginUpdates.Name = "bDownloadExternalPluginUpdates";
            this.bDownloadExternalPluginUpdates.Size = new System.Drawing.Size(1283, 50);
            this.bDownloadExternalPluginUpdates.TabIndex = 4;
            this.bDownloadExternalPluginUpdates.Text = "Download";
            this.bDownloadExternalPluginUpdates.UseVisualStyleBackColor = true;
            this.bDownloadExternalPluginUpdates.Click += new System.EventHandler(this.bDownloadExternalPluginUpdates_Click);
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.Controls.Add(this.tbFile);
            this.panel1.Controls.Add(this.lFile);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(18, 15);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1283, 57);
            this.panel1.TabIndex = 3;
            // 
            // tbFile
            // 
            this.tbFile.Location = new System.Drawing.Point(65, 14);
            this.tbFile.Margin = new System.Windows.Forms.Padding(5);
            this.tbFile.Name = "tbFile";
            this.tbFile.ReadOnly = true;
            this.tbFile.Size = new System.Drawing.Size(1216, 38);
            this.tbFile.TabIndex = 3;
            // 
            // lFile
            // 
            this.lFile.AutoSize = true;
            this.lFile.Location = new System.Drawing.Point(1, 23);
            this.lFile.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lFile.Name = "lFile";
            this.lFile.Size = new System.Drawing.Size(69, 32);
            this.lFile.TabIndex = 2;
            this.lFile.TabStop = true;
            this.lFile.Text = "lFile";
            // 
            // lv3rdPartyPlugins
            // 
            this.lv3rdPartyPlugins.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lv3rdPartyPlugins.FullRowSelect = true;
            this.lv3rdPartyPlugins.HideSelection = false;
            this.lv3rdPartyPlugins.Location = new System.Drawing.Point(18, 130);
            this.lv3rdPartyPlugins.Margin = new System.Windows.Forms.Padding(5);
            this.lv3rdPartyPlugins.Name = "lv3rdPartyPlugins";
            this.lv3rdPartyPlugins.Size = new System.Drawing.Size(1283, 431);
            this.lv3rdPartyPlugins.TabIndex = 2;
            this.lv3rdPartyPlugins.UseCompatibleStateImageBehavior = false;
            this.lv3rdPartyPlugins.View = System.Windows.Forms.View.List;
            // 
            // tpKeePass
            // 
            this.tpKeePass.Controls.Add(this.cgKeePassUpdate);
            this.tpKeePass.Location = new System.Drawing.Point(10, 48);
            this.tpKeePass.Name = "tpKeePass";
            this.tpKeePass.Padding = new System.Windows.Forms.Padding(18, 15, 18, 15);
            this.tpKeePass.Size = new System.Drawing.Size(1319, 576);
            this.tpKeePass.TabIndex = 2;
            this.tpKeePass.Text = "KeePass";
            this.tpKeePass.UseVisualStyleBackColor = true;
            this.tpKeePass.ClientSizeChanged += new System.EventHandler(this.AdjustControls);
            // 
            // gOneClickUpdate
            // 
            this.gOneClickUpdate.CheckboxOffset = new System.Drawing.Point(5, 0);
            this.gOneClickUpdate.Checked = true;
            this.gOneClickUpdate.Controls.Add(this.bUpdateTranslations);
            this.gOneClickUpdate.Controls.Add(this.cbDownloadCurrentTranslation);
            this.gOneClickUpdate.Controls.Add(this.tbOneClickUpdateDesc);
            this.gOneClickUpdate.DisableControlsIfUnchecked = false;
            this.gOneClickUpdate.Dock = System.Windows.Forms.DockStyle.Top;
            this.gOneClickUpdate.Location = new System.Drawing.Point(5, 267);
            this.gOneClickUpdate.Margin = new System.Windows.Forms.Padding(5);
            this.gOneClickUpdate.Name = "gOneClickUpdate";
            this.gOneClickUpdate.Padding = new System.Windows.Forms.Padding(27, 8, 27, 8);
            this.gOneClickUpdate.Size = new System.Drawing.Size(1309, 299);
            this.gOneClickUpdate.TabIndex = 11;
            this.gOneClickUpdate.TabStop = false;
            this.gOneClickUpdate.Text = "SimpleUpdate";
            // 
            // bUpdateTranslations
            // 
            this.bUpdateTranslations.Dock = System.Windows.Forms.DockStyle.Top;
            this.bUpdateTranslations.Location = new System.Drawing.Point(27, 244);
            this.bUpdateTranslations.Margin = new System.Windows.Forms.Padding(5);
            this.bUpdateTranslations.Name = "bUpdateTranslations";
            this.bUpdateTranslations.Size = new System.Drawing.Size(1255, 46);
            this.bUpdateTranslations.TabIndex = 7;
            this.bUpdateTranslations.Text = "Translation update";
            this.bUpdateTranslations.UseVisualStyleBackColor = true;
            this.bUpdateTranslations.Click += new System.EventHandler(this.bUpdateTranslations_Click);
            // 
            // cbDownloadCurrentTranslation
            // 
            this.cbDownloadCurrentTranslation.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbDownloadCurrentTranslation.Location = new System.Drawing.Point(27, 207);
            this.cbDownloadCurrentTranslation.Margin = new System.Windows.Forms.Padding(5);
            this.cbDownloadCurrentTranslation.Name = "cbDownloadCurrentTranslation";
            this.cbDownloadCurrentTranslation.Size = new System.Drawing.Size(1255, 37);
            this.cbDownloadCurrentTranslation.TabIndex = 5;
            this.cbDownloadCurrentTranslation.Text = "Download selected language";
            this.cbDownloadCurrentTranslation.UseVisualStyleBackColor = true;
            // 
            // tbOneClickUpdateDesc
            // 
            this.tbOneClickUpdateDesc.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbOneClickUpdateDesc.Location = new System.Drawing.Point(27, 39);
            this.tbOneClickUpdateDesc.Margin = new System.Windows.Forms.Padding(5);
            this.tbOneClickUpdateDesc.Multiline = true;
            this.tbOneClickUpdateDesc.Name = "tbOneClickUpdateDesc";
            this.tbOneClickUpdateDesc.ReadOnly = true;
            this.tbOneClickUpdateDesc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbOneClickUpdateDesc.Size = new System.Drawing.Size(1255, 168);
            this.tbOneClickUpdateDesc.TabIndex = 6;
            // 
            // gCheckSync
            // 
            this.gCheckSync.CheckboxOffset = new System.Drawing.Point(5, 0);
            this.gCheckSync.Checked = true;
            this.gCheckSync.Controls.Add(this.tbCheckSyncDesc);
            this.gCheckSync.Controls.Add(this.cbCheckSync);
            this.gCheckSync.DisableControlsIfUnchecked = false;
            this.gCheckSync.Dock = System.Windows.Forms.DockStyle.Top;
            this.gCheckSync.Location = new System.Drawing.Point(5, 5);
            this.gCheckSync.Margin = new System.Windows.Forms.Padding(5);
            this.gCheckSync.Name = "gCheckSync";
            this.gCheckSync.Padding = new System.Windows.Forms.Padding(27, 8, 27, 8);
            this.gCheckSync.Size = new System.Drawing.Size(1309, 262);
            this.gCheckSync.TabIndex = 10;
            this.gCheckSync.TabStop = false;
            this.gCheckSync.Text = "CheckSync";
            // 
            // tbCheckSyncDesc
            // 
            this.tbCheckSyncDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbCheckSyncDesc.Location = new System.Drawing.Point(27, 75);
            this.tbCheckSyncDesc.Margin = new System.Windows.Forms.Padding(5);
            this.tbCheckSyncDesc.Multiline = true;
            this.tbCheckSyncDesc.Name = "tbCheckSyncDesc";
            this.tbCheckSyncDesc.ReadOnly = true;
            this.tbCheckSyncDesc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbCheckSyncDesc.Size = new System.Drawing.Size(1255, 179);
            this.tbCheckSyncDesc.TabIndex = 3;
            // 
            // cbCheckSync
            // 
            this.cbCheckSync.AutoSize = true;
            this.cbCheckSync.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbCheckSync.Location = new System.Drawing.Point(27, 39);
            this.cbCheckSync.Margin = new System.Windows.Forms.Padding(5);
            this.cbCheckSync.Name = "cbCheckSync";
            this.cbCheckSync.Size = new System.Drawing.Size(1255, 36);
            this.cbCheckSync.TabIndex = 2;
            this.cbCheckSync.Text = "CheckSync";
            this.cbCheckSync.UseVisualStyleBackColor = true;
            // 
            // cgKeePassUpdate
            // 
            this.cgKeePassUpdate.CheckboxOffset = new System.Drawing.Point(5, 0);
            this.cgKeePassUpdate.Checked = true;
            this.cgKeePassUpdate.Controls.Add(this.cbKeePassInstallType);
            this.cgKeePassUpdate.Controls.Add(this.lKeePassInstallType);
            this.cgKeePassUpdate.Controls.Add(this.tbKeePassFolder);
            this.cgKeePassUpdate.Controls.Add(this.lKeePassFolder);
            this.cgKeePassUpdate.Controls.Add(this.tbKeePassUpdateInfo);
            this.cgKeePassUpdate.Dock = System.Windows.Forms.DockStyle.Top;
            this.cgKeePassUpdate.Location = new System.Drawing.Point(18, 15);
            this.cgKeePassUpdate.Name = "cgKeePassUpdate";
            this.cgKeePassUpdate.Padding = new System.Windows.Forms.Padding(27, 8, 27, 8);
            this.cgKeePassUpdate.Size = new System.Drawing.Size(1283, 508);
            this.cgKeePassUpdate.TabIndex = 0;
            this.cgKeePassUpdate.Text = "cgKeePassUpdate";
            // 
            // cbKeePassInstallType
            // 
            this.cbKeePassInstallType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbKeePassInstallType.FormattingEnabled = true;
            this.cbKeePassInstallType.Location = new System.Drawing.Point(130, 410);
            this.cbKeePassInstallType.Name = "cbKeePassInstallType";
            this.cbKeePassInstallType.Size = new System.Drawing.Size(463, 39);
            this.cbKeePassInstallType.TabIndex = 5;
            // 
            // lKeePassInstallType
            // 
            this.lKeePassInstallType.AutoSize = true;
            this.lKeePassInstallType.Location = new System.Drawing.Point(27, 418);
            this.lKeePassInstallType.Name = "lKeePassInstallType";
            this.lKeePassInstallType.Size = new System.Drawing.Size(93, 32);
            this.lKeePassInstallType.TabIndex = 4;
            this.lKeePassInstallType.Text = "label2";
            // 
            // tbKeePassFolder
            // 
            this.tbKeePassFolder.Location = new System.Drawing.Point(130, 345);
            this.tbKeePassFolder.Name = "tbKeePassFolder";
            this.tbKeePassFolder.ReadOnly = true;
            this.tbKeePassFolder.Size = new System.Drawing.Size(463, 38);
            this.tbKeePassFolder.TabIndex = 3;
            // 
            // lKeePassFolder
            // 
            this.lKeePassFolder.AutoSize = true;
            this.lKeePassFolder.Location = new System.Drawing.Point(27, 351);
            this.lKeePassFolder.Name = "lKeePassFolder";
            this.lKeePassFolder.Size = new System.Drawing.Size(93, 32);
            this.lKeePassFolder.TabIndex = 2;
            this.lKeePassFolder.TabStop = true;
            this.lKeePassFolder.Text = "label1";
            this.lKeePassFolder.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lKeePassFolder_LinkClicked);
            // 
            // tbKeePassUpdateInfo
            // 
            this.tbKeePassUpdateInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbKeePassUpdateInfo.Location = new System.Drawing.Point(27, 39);
            this.tbKeePassUpdateInfo.Margin = new System.Windows.Forms.Padding(5);
            this.tbKeePassUpdateInfo.Multiline = true;
            this.tbKeePassUpdateInfo.Name = "tbKeePassUpdateInfo";
            this.tbKeePassUpdateInfo.ReadOnly = true;
            this.tbKeePassUpdateInfo.Size = new System.Drawing.Size(1229, 281);
            this.tbKeePassUpdateInfo.TabIndex = 1;
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tcEUC);
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "Options";
            this.Size = new System.Drawing.Size(1339, 744);
            this.Load += new System.EventHandler(this.Options_Load);
            this.tcEUC.ResumeLayout(false);
            this.tpEUCOptions.ResumeLayout(false);
            this.tpEUC3rdParty.ResumeLayout(false);
            this.tpEUC3rdParty.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tpKeePass.ResumeLayout(false);
            this.gOneClickUpdate.ResumeLayout(false);
            this.gOneClickUpdate.PerformLayout();
            this.gCheckSync.ResumeLayout(false);
            this.gCheckSync.PerformLayout();
            this.cgKeePassUpdate.ResumeLayout(false);
            this.cgKeePassUpdate.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		internal RookieUI.CheckedGroupBox gCheckSync;
		private System.Windows.Forms.TextBox tbCheckSyncDesc;
		internal System.Windows.Forms.CheckBox cbCheckSync;
		internal RookieUI.CheckedGroupBox gOneClickUpdate;
		private System.Windows.Forms.Button bUpdateTranslations;
		internal System.Windows.Forms.CheckBox cbDownloadCurrentTranslation;
		private System.Windows.Forms.TextBox tbOneClickUpdateDesc;
		private System.Windows.Forms.TabControl tcEUC;
		private System.Windows.Forms.TabPage tpEUCOptions;
		private System.Windows.Forms.TabPage tpEUC3rdParty;
		private System.Windows.Forms.ListView lv3rdPartyPlugins;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox tbFile;
        private System.Windows.Forms.LinkLabel lFile;
        private System.Windows.Forms.Button bDownloadExternalPluginUpdates;
        private System.Windows.Forms.TabPage tpKeePass;
        internal RookieUI.CheckedGroupBox cgKeePassUpdate;
        internal System.Windows.Forms.ComboBox cbKeePassInstallType;
        private System.Windows.Forms.Label lKeePassInstallType;
        internal System.Windows.Forms.TextBox tbKeePassFolder;
        private System.Windows.Forms.LinkLabel lKeePassFolder;
        private System.Windows.Forms.TextBox tbKeePassUpdateInfo;
    }
}
