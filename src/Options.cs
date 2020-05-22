using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using PluginTranslation;

namespace EarlyUpdateCheck
{
	public partial class Options : UserControl
	{
		public EarlyUpdateCheckExt Plugin = null;
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
			if (EarlyUpdateCheckExt.ShouldShieldify)
				KeePass.UI.UIUtil.SetShield(bUpdateTranslations, true);
		}

		private void bUpdateTranslations_Click(object sender, EventArgs e)
		{
			List<string> lPlugins = new List<string>();
			foreach (UpdateInfo ui in Plugin.Plugins)
			{	if (ui == null) continue;
				if (!lPlugins.Contains(ui.Name)) lPlugins.Add(ui.Name);
			}
			using (TranslationUpdateForm t = new TranslationUpdateForm())
			{
				t.InitEx(lPlugins);
				if (t.ShowDialog() == DialogResult.OK)
				{
					lPlugins = t.SelectedPlugins;
					Plugin.UpdatePluginTranslations(PluginConfig.DownloadActiveLanguage, lPlugins);
				}
			}
		}

		private void gCheckSync_CheckedChanged(object sender, RookieUI.CheckedGroupCheckEventArgs e)
		{
			cbCheckSync.Enabled = gCheckSync.Checked;
		}
	}
}
