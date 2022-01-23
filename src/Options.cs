using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using PluginTranslation;
using KeePass.Resources;

namespace EarlyUpdateCheck
{
	public partial class Options : UserControl
	{
		public EarlyUpdateCheckExt Plugin = null;

		internal KeePassLib.Delegates.GAction<EarlyUpdateCheckExt.UpdateFlags, bool, KeePass_Update> UpdateExternalPluginUpdates;

		internal KeePass_Update.KeePassInstallType kpit
		{
			get 
			{
				if (cbKeePassInstallType.SelectedIndex == 0) return KeePass_Update.KeePassInstallType.Setup;
				if (cbKeePassInstallType.SelectedIndex == 1) return KeePass_Update.KeePassInstallType.MSI;
				return KeePass_Update.KeePassInstallType.Portable;
			}
            set
            {
				if (value == KeePass_Update.KeePassInstallType.Setup) cbKeePassInstallType.SelectedIndex = 0;
				else if (value == KeePass_Update.KeePassInstallType.MSI) cbKeePassInstallType.SelectedIndex = 1;
				else cbKeePassInstallType.SelectedIndex = 2;
			}
		}

		public Options()
		{
			InitializeComponent();
			gCheckSync.Text = PluginTranslate.Active;
			cbCheckSync.Text = PluginTranslate.CheckSync;
			tbCheckSyncDesc.Lines = PluginTranslate.CheckSyncDesc.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			gOneClickUpdate.Text = PluginTranslate.PluginUpdateOneClick;
			tbOneClickUpdateDesc.Lines = PluginTranslate.PluginUpdateOneClickDesc.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			if (string.IsNullOrEmpty(KeePass.Program.Translation.Properties.Iso6391Code))
				cbDownloadCurrentTranslation.Text = string.Format(PluginTranslate.TranslationDownload_DownloadCurrent, "English");
			else
				cbDownloadCurrentTranslation.Text = string.Format(PluginTranslate.TranslationDownload_DownloadCurrent, KeePass.Program.Translation.Properties.NameNative);
			bUpdateTranslations.Text = PluginTranslate.TranslationDownload_Update;
			if (PluginUpdateHandler.Shieldify) KeePass.UI.UIUtil.SetShield(bUpdateTranslations, true);
			bDownloadExternalPluginUpdates.Text = PluginTranslate.UpdateExternalInfoDownload;

			tpKeePass.Text = PluginTranslate.PluginUpdateKeePass;
			cgKeePassUpdate.Text = PluginTranslate.Active;
			tbKeePassUpdateInfo.Lines = PluginTranslate.KeePassUpdate_RequestInstallType.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			tbKeePassFolder.Text = KeePassLib.Utility.UrlUtil.GetFileDirectory(KeePass.Util.WinUtil.GetExecutable(), true, true);
			cbKeePassInstallType.Items.Add(KeePass_Update.KeePassInstallType.Setup);
			cbKeePassInstallType.Items.Add(KeePass_Update.KeePassInstallType.MSI);
			cbKeePassInstallType.Items.Add(KeePass_Update.KeePassInstallType.Portable);
			lKeePassFolder.Text = KPRes.Folder + ":";
			lKeePassInstallType.Text = KPRes.Type + ":";
		}

		private void bUpdateTranslations_Click(object sender, EventArgs e)
		{
			List<OwnPluginUpdate> lPlugins = new List<OwnPluginUpdate>();
			foreach (PluginUpdate pu in PluginUpdateHandler.Plugins)
			{
				OwnPluginUpdate opu = pu as OwnPluginUpdate;
				if (opu == null) continue;
				if (!PluginUpdateHandler.VersionsEqual(pu.VersionInstalled, pu.VersionAvailable)) continue;
				if (opu.Translations.Count == 0) continue;
				if (!lPlugins.Contains(opu)) lPlugins.Add(opu);
			}
			if (lPlugins.Count == 0)
			{
				PluginTools.PluginDebug.AddInfo("No plugins where translations can be updated", 0);
				return;
			}
			using (TranslationUpdateForm t = new TranslationUpdateForm())
			{
				t.InitEx(lPlugins);
				if (t.ShowDialog() == DialogResult.OK)
				{
					Plugin.UpdatePluginTranslations(PluginConfig.DownloadActiveLanguage, t.SelectedPlugins);
				}
			}
		}

		private void gCheckSync_CheckedChanged(object sender, RookieUI.CheckedGroupCheckEventArgs e)
		{
			cbCheckSync.Enabled = gCheckSync.Checked;
		}

		private void Options_Load(object sender, EventArgs e)
		{
			tpEUCOptions.Text = KPRes.Options;
			tpEUC3rdParty.Text = KPRes.More;
			lFile.Text = KPRes.File;
			tbFile.Text = UpdateInfoExternParser.PluginInfoFile;
			RefreshPluginList();
		}

		private void RefreshPluginList()
		{
			if (System.IO.File.Exists(UpdateInfoExternParser.PluginInfoFile))
			{
				lFile.Links.Add(0, lFile.Text.Length);
				lFile.LinkClicked += LFile_LinkClicked;
			}
			else lFile.LinkArea = new LinkArea(0,0);
			lv3rdPartyPlugins.Items.Clear();
			foreach (var p in PluginUpdateHandler.Plugins)
			{
				string s = p.Title + (p is OtherPluginUpdate ? " - " + p.UpdateMode.ToString() : string.Empty);
				if (p.Ignore)
				{
					ListViewItem lvi = new ListViewItem(p.Title);
					lvi.Font = new Font(lvi.Font, lvi.Font.Style | FontStyle.Strikeout);
					lv3rdPartyPlugins.Items.Add(lvi);
				}
				else lv3rdPartyPlugins.Items.Add(s);
			}
			bDownloadExternalPluginUpdates.Visible = UpdateInfoExternParser.VersionInstalled < 0;
		}

		private void OnShow3rdPartyTab(object sender, EventArgs e)
        {
			tbFile.Width = tbFile.Parent.ClientSize.Width - 2 * tbFile.Parent.Padding.Left - 2 * lFile.Left - lFile.Width - 20;
			tbFile.Left = lFile.Left + lFile.Width + 10;
        }

        private void LFile_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			PluginTools.Tools.OpenUrl(UpdateInfoExternParser.PluginInfoFile);
		}

        private void bDownloadExternalPluginUpdates_Click(object sender, EventArgs e)
        {
			UpdateExternalPluginUpdates(EarlyUpdateCheckExt.UpdateFlags.ExternalUpdateInfo, true, null);
			RefreshPluginList();
		}

        internal void AdjustControls(object sender, EventArgs e)
        {
			int iDelta = lKeePassFolder.Width;
			if (lKeePassInstallType.Width > iDelta) iDelta = lKeePassInstallType.Width;
			iDelta += lKeePassFolder.Left + 10;
			tbKeePassFolder.Left = cbKeePassInstallType.Left = iDelta;
			tbKeePassFolder.Width = tbKeePassUpdateInfo.Width - iDelta + 20;
			cbKeePassInstallType.Width = tbKeePassFolder.Width;
        }

		internal void ActivateKeePassUpdateTab(object sender, EventArgs e)
		{
			TabControl tcMain = null;
			TabControl tcPluginOptions = null;
			TabPage tpEUC = null;
			TabPage tpOptions = null;
			Control c = tcEUC.Parent;
			while (c != null)
            {
				if (c is TabPage)
				{
					if (tpEUC == null) tpEUC = c as TabPage;
					else if (tpOptions == null) tpOptions = c as TabPage;
				}
				else if (c is TabControl)
				{
					if (tcPluginOptions == null) tcPluginOptions = c as TabControl;
					else if (tcMain == null) tcMain = c as TabControl;
				}
				c = c.Parent;
            }
			if (tcMain != null && tpOptions != null) tcMain.SelectedTab = tpOptions;
			if (tcPluginOptions != null && tpEUC != null) tcPluginOptions.SelectedTab = tpEUC;
			tcEUC.SelectedTab = tpKeePass;
		}

		private void lKeePassFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
			PluginTools.Tools.OpenUrl(tbKeePassFolder.Text);
        }
    }
}
