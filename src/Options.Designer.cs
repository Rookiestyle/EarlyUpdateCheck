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
            this.panel1 = new System.Windows.Forms.Panel();
            this.tbFile = new System.Windows.Forms.TextBox();
            this.lFile = new System.Windows.Forms.LinkLabel();
            this.lv3rdPartyPlugins = new System.Windows.Forms.ListView();
            this.gOneClickUpdate = new RookieUI.CheckedGroupBox();
            this.bUpdateTranslations = new System.Windows.Forms.Button();
            this.cbDownloadCurrentTranslation = new System.Windows.Forms.CheckBox();
            this.tbOneClickUpdateDesc = new System.Windows.Forms.TextBox();
            this.gCheckSync = new RookieUI.CheckedGroupBox();
            this.tbCheckSyncDesc = new System.Windows.Forms.TextBox();
            this.cbCheckSync = new System.Windows.Forms.CheckBox();
            this.tcEUC.SuspendLayout();
            this.tpEUCOptions.SuspendLayout();
            this.tpEUC3rdParty.SuspendLayout();
            this.panel1.SuspendLayout();
            this.gOneClickUpdate.SuspendLayout();
            this.gCheckSync.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcEUC
            // 
            this.tcEUC.Controls.Add(this.tpEUCOptions);
            this.tcEUC.Controls.Add(this.tpEUC3rdParty);
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
            this.tpEUCOptions.Text = "tabPage1";
            this.tpEUCOptions.UseVisualStyleBackColor = true;
            // 
            // tpEUC3rdParty
            // 
            this.tpEUC3rdParty.Controls.Add(this.panel1);
            this.tpEUC3rdParty.Controls.Add(this.lv3rdPartyPlugins);
            this.tpEUC3rdParty.Location = new System.Drawing.Point(10, 48);
            this.tpEUC3rdParty.Margin = new System.Windows.Forms.Padding(5);
            this.tpEUC3rdParty.Name = "tpEUC3rdParty";
            this.tpEUC3rdParty.Padding = new System.Windows.Forms.Padding(18, 15, 18, 15);
            this.tpEUC3rdParty.Size = new System.Drawing.Size(1319, 576);
            this.tpEUC3rdParty.TabIndex = 1;
            this.tpEUC3rdParty.Text = "tabPage2";
            this.tpEUC3rdParty.UseVisualStyleBackColor = true;
            this.tpEUC3rdParty.Resize += new System.EventHandler(this.OnShow3rdPartyTab);
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
            this.lv3rdPartyPlugins.Location = new System.Drawing.Point(18, 92);
            this.lv3rdPartyPlugins.Margin = new System.Windows.Forms.Padding(5);
            this.lv3rdPartyPlugins.Name = "lv3rdPartyPlugins";
            this.lv3rdPartyPlugins.Size = new System.Drawing.Size(1283, 469);
            this.lv3rdPartyPlugins.TabIndex = 2;
            this.lv3rdPartyPlugins.UseCompatibleStateImageBehavior = false;
            this.lv3rdPartyPlugins.View = System.Windows.Forms.View.List;
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
            this.gOneClickUpdate.ResumeLayout(false);
            this.gOneClickUpdate.PerformLayout();
            this.gCheckSync.ResumeLayout(false);
            this.gCheckSync.PerformLayout();
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
    }
}
