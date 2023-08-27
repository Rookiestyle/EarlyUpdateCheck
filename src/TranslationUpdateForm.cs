using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using KeePass.Resources;
using PluginTranslation;

namespace EarlyUpdateCheck
{
  public partial class TranslationUpdateForm : Form
  {
    public TranslationUpdateForm()
    {
      InitializeComponent();
    }

    internal void InitEx(List<OwnPluginUpdate> lPlugins)
    {
      Text = PluginTranslate.TranslationUpdateForm;
      lSelectPlugins.Text = PluginTranslate.SelectPluginsForTranslationUpdate;
      bOK.Text = PluginTranslate.TranslationDownload_Update;
      bOK.Text = PluginTranslate.PluginUpdateSelected;
      bCancel.Text = KPRes.Cancel;

      clbPlugins.Items.Clear();
      lPlugins.Sort(SortOwnPluginUpdate);
      foreach (OwnPluginUpdate plugin in lPlugins)
        clbPlugins.Items.Add(plugin.Name, PluginUpdateHandler.VersionsEqual(plugin.VersionInstalled, plugin.VersionAvailable) ? CheckState.Checked : CheckState.Indeterminate);
    }

    private int SortOwnPluginUpdate(OwnPluginUpdate a, OwnPluginUpdate b)
    {
      return -1 * string.Compare(a.Name, b.Name);
    }

    public List<string> SelectedPlugins
    {
      get
      {
        List<string> lPlugins = new List<string>();
        foreach (int i in clbPlugins.CheckedIndices)
        {
          if (clbPlugins.GetItemCheckState(i) == CheckState.Checked) lPlugins.Add(clbPlugins.Items[i] as string);
        }
        return lPlugins;
      }
    }

    private void clbPlugins_ItemCheck(object sender, ItemCheckEventArgs e)
    {
      if (e.CurrentValue == CheckState.Indeterminate)
      {
        e.NewValue = CheckState.Indeterminate;
      }
      bool bChecked = e.NewValue == CheckState.Checked;
      foreach (int i in clbPlugins.CheckedIndices)
      {
        if (i == e.Index) continue;
        else bChecked |= clbPlugins.GetItemCheckState(i) == CheckState.Checked;
        if (bChecked) break;
      }
      bOK.Enabled = bChecked; // (e.NewValue == CheckState.Checked) || (clbPlugins.CheckedItems.Count > 1);
    }

    private void TranslationUpdateForm_Load(object sender, EventArgs e)
    {
      bOK.Left = bCancel.Left - bOK.Width - 15;
    }
  }
}
