using KeePass.Resources;
using PluginTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EarlyUpdateCheck
{
	public partial class TranslationUpdateForm : Form
	{
		public TranslationUpdateForm()
		{
			InitializeComponent();
		}

		public void InitEx(List<string> lPlugins)
		{
			Text = PluginTranslate.TranslationUpdateForm;
			lSelectPlugins.Text = PluginTranslate.SelectPluginsForTranslationUpdate;
			bOK.Text = PluginTranslate.TranslationDownload_Update;
			bOK.Text = PluginTranslate.PluginUpdateSelected;
			bCancel.Text = KPRes.Cancel;

			clbPlugins.Items.Clear();
			lPlugins.Sort();
			foreach (string plugin in lPlugins)
				clbPlugins.Items.Add(plugin, true);
		}

		public List<string> SelectedPlugins
		{
			get
			{
				List<string> lPlugins = new List<string>();
				foreach (string p in clbPlugins.CheckedItems)
					lPlugins.Add(p);
				return lPlugins;
			}
		}

		private void clbPlugins_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bOK.Enabled = (e.NewValue == CheckState.Checked) || (clbPlugins.CheckedItems.Count > 1);
		}

		private void TranslationUpdateForm_Load(object sender, EventArgs e)
		{
			bOK.Left = bCancel.Left - bOK.Width - 15;
		}
	}
}
