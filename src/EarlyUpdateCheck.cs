using KeePass.Plugins;
using KeePass.Util;
using KeePass.Forms;
using KeePass.UI;
using System.Reflection;
using System.Windows.Forms;
using System;
using System.Threading;

using PluginTools;
using PluginTranslation;
using KeePassLib.Interfaces;
using System.Collections.Generic;
using KeePassLib.Utility;
using System.Drawing;
using System.Text.RegularExpressions;
using KeePassLib.Translation;
using KeePass.Util.XmlSerialization;

namespace EarlyUpdateCheck
{
	public sealed class EarlyUpdateCheckExt : Plugin
	{
		private IPluginHost m_host = null;
		private ToolStripMenuItem m_tsMenu = null;

		private enum UpdateCheckStatus
		{
			NotChecked,
			Checking,
			Checked,
			Error
		};
		private UpdateCheckStatus m_UpdateCheckStatus = UpdateCheckStatus.NotChecked;
		private object m_lock = new object();

		private IStatusLogger m_slUpdateCheck = null;
		private Form m_CheckProgress = null;
		private bool m_bRestartInvoke = false;
		private bool m_bRestartTriggered = false;
		private IStatusLogger m_slUpdatePlugins = null;
		private string m_LanguageIso = null;
		private bool m_bUpdateCheckDone = false;

		private KeyPromptForm m_kpf = null;

		private List<UpdateInfo> m_lPluginUpdateInfo = new List<UpdateInfo>();
		public List<UpdateInfo> Plugins { get { return m_lPluginUpdateInfo.Count > 0 ? m_lPluginUpdateInfo : GetInstalledPlugins(); } }
		public static string m_PluginsFolder = string.Empty;
		private string m_PluginsTranslationsFolder = string.Empty;

		public static bool ShouldShieldify = false;

		public override bool Initialize(IPluginHost host)
		{
			m_host = host;
			PluginTranslate.TranslationChanged += delegate (object sender, TranslationChangedEventArgs e) 
			{
				if (!string.IsNullOrEmpty(KeePass.Program.Translation.Properties.Iso6391Code))
					m_LanguageIso = KeePass.Program.Translation.Properties.Iso6391Code;
				else
					m_LanguageIso = e.NewLanguageIso6391;
			};
			PluginTranslate.Init(this, KeePass.Program.Translation.Properties.Iso6391Code);
			Tools.DefaultCaption = PluginTranslate.PluginName;
			Tools.PluginURL = "https://github.com/rookiestyle/earlyupdatecheck/";
			PluginConfig.Read();

			GlobalWindowManager.WindowAdded += WindowAdded;
			GlobalWindowManager.WindowRemoved += WindowRemoved;
			m_tsMenu = new ToolStripMenuItem(PluginTranslate.PluginName + "...");
			m_tsMenu.Click += (o, e) => Tools.ShowOptions();
			ToolStripMenuItem tsmiUpdate = Tools.FindToolStripMenuItem(m_host.MainWindow.MainMenu.Items, "m_menuHelpCheckForUpdates", true);
			if (tsmiUpdate != null) m_tsMenu.Image = tsmiUpdate.Image;
			else m_tsMenu.Image = m_host.MainWindow.ClientIcons.Images[(int)KeePassLib.PwIcon.World];
			m_host.MainWindow.ToolsMenu.DropDownItems.Add(m_tsMenu);

			Tools.OptionsFormShown += OptionsFormShown;
			Tools.OptionsFormClosed += OptionsFormClosed;

			m_PluginsFolder = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), true, true);
			m_PluginsFolder = UrlUtil.EnsureTerminatingSeparator(m_PluginsFolder + KeePass.App.AppDefs.PluginsDir, false);
			m_PluginsTranslationsFolder = UrlUtil.EnsureTerminatingSeparator(m_PluginsFolder + "Translations", false);
			PluginDebug.AddInfo("Plugins folder detected", 0, m_PluginsFolder);
			PluginDebug.AddInfo("Translations folder detected", 0, m_PluginsTranslationsFolder);
			m_host.MainWindow.FormLoadPost += MainWindow_FormLoadPost;

			CheckShieldify();

			return true;
		}

		private void CheckShieldify()
		{
			List<string> lShieldify = new List<string>();
			try
			{
				ShouldShieldify = false;
				if (KeePassLib.Native.NativeLib.IsUnix())
				{
					lShieldify.Add("Detected Unix");
					return;
				}
				if (!WinUtil.IsAtLeastWindows7)
				{
					lShieldify.Add("Detected Windows < 7");
					return;
				}
				string sPF86 = EnsureNonNull(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
				string sPF86_2 = string.Empty;
				try { sPF86_2 = EnsureNonNull(Environment.GetFolderPath((Environment.SpecialFolder)42)); } //Environment.SpecialFolder.ProgramFilesX86
				catch { sPF86_2 = sPF86; }
				string sPF = EnsureNonNull(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
				string sKP = EnsureNonNull(UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), true, false));
				ShouldShieldify = sKP.StartsWith(sPF86) || sKP.StartsWith(sPF) || sKP.StartsWith(sPF86_2);
				lShieldify.Add("KeePass folder inside ProgramFiles(x86): " + sKP.StartsWith(sPF86));
				lShieldify.Add("KeePass folder inside Environment.SpecialFolder.ProgramFilesX86: " + sKP.StartsWith(sPF86_2));
				lShieldify.Add("KeePass folder inside Environment.SpecialFolder.ProgramFiles: " + sKP.StartsWith(sPF));
			}
			catch (Exception ex) { lShieldify.Add("Exception: " + ex.Message); }
			finally
			{
				lShieldify.Insert(0, "Shieldify: " + ShouldShieldify.ToString());
				PluginDebug.AddInfo("Check Shieldify", 0, lShieldify.ToArray());
			}
		}

		private string EnsureNonNull(string v)
		{
			if (v == null) return string.Empty;
			return v;
		}

		/// <summary>
		/// Reset indicator for invoking the restart after installing plugin updates
		/// </summary>
		private void MainWindow_FormLoadPost(object sender, EventArgs e)
		{
			m_bRestartInvoke = false;
			m_host.MainWindow.FormLoadPost -= MainWindow_FormLoadPost;
			PluginDebug.AddInfo("All plugins loaded", 0, DebugPrint);
		}

		private void WindowAdded(object sender, GwmWindowEventArgs e)
		{
			if (!PluginConfig.Active) return;
			PluginDebug.AddInfo("Form added", 0, e.Form.Name, e.Form.GetType().FullName, DebugPrint);
			if (e.Form is UpdateCheckForm)
			{
				if (m_CheckProgress != null)
				{
					m_CheckProgress.Hide();
					lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checked; }
				}
				if (PluginConfig.OneClickUpdate)
					e.Form.Shown += OnUpdateCheckFormShown;
				return;
			}
			if (e.Form is KeyPromptForm) KeyPromptFormAdded();
			if (e.Form is LanguageForm) e.Form.Shown += LanguageFormAdded;
		}

		private void LanguageFormAdded(object sender, EventArgs e)
		{
			LanguageForm fLang = sender as LanguageForm;
			if (fLang == null) return;
			ListView m_lvLanguages = Tools.GetControl("m_lvLanguages", fLang) as ListView;
			if (m_lvLanguages == null) return;
			m_lvLanguages.BeginUpdate();
			int[] aWidths = StrUtil.DeserializeIntArray(UIUtil.GetColumnWidths(m_lvLanguages) + " " + DpiUtil.ScaleIntX(60).ToString());
			int iCol = m_lvLanguages.Columns.Add("L-ID").Index;
			foreach (ListViewItem i in m_lvLanguages.Items)
			{
				try
				{
					XmlSerializerEx xs = new XmlSerializerEx(typeof(KPTranslation));
					KPTranslation t = KPTranslation.Load(i.Tag as string, xs);
					i.SubItems.Add(t.Properties.Iso6391Code);
				}
				catch { if (string.IsNullOrEmpty(i.Tag as string)) i.SubItems.Add("en"); }
			}
			UIUtil.ResizeColumns(m_lvLanguages, aWidths, true);
			m_lvLanguages.EndUpdate();
		}

		private void WindowRemoved(object sender, GwmWindowEventArgs e)
		{
			if ((m_kpf != null) && (e.Form is KeyPromptForm))	m_kpf = null;
		}

		#region Check for updates
		/// <summary>
		/// Only perform update check if last used database has to be opened automatically
		/// If this is not the case, use KeePass standard check for updates
		/// </summary>
		private void KeyPromptFormAdded()
		{
			if (!PluginConfig.Active) return;
			UpdateCheckType uct = UpdateCheckRequired();
			if (uct == UpdateCheckType.NotRequired) return;
			if (uct == UpdateCheckType.OnlyTranslations)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(CheckPluginLanguages));
				return;
			}
			m_kpf = GlobalWindowManager.TopWindow as KeyPromptForm;
			UpdateCheckEx.EnsureConfigured(m_host.MainWindow);
			//Try calling the internal method UpdateCheckEx.RunPriv 
			//==> Running this in a seperate threads does not force the user to wait 
			//    in case of e. g. connection issues
			//==> Creating this seperate thread manually allows
			//      - to check for completion of the update check
			//      - show the update window BEFORE "Open Database" window is shown
			//      - Skip waiting in case of e. g. connection issues
			//
			//As fallback the public method UpdateCheckRunEx.Run is called
			//This method runs in a separate thread 
			//==> Update window might be shown AFTER "Open Database" window is shown
			if (PluginConfig.CheckSync)
			{
				PluginDebug.AddInfo("UpdateCheck start", 0, DebugPrint);
				m_bRestartInvoke = true;
				try
				{
					m_slUpdateCheck = CreateUpdateCheckLogger();
				}
				catch (Exception ex)
				{
					PluginDebug.AddError("UpdateCheck error", 0, "Initialising StatusLogger failed", ex.Message, DebugPrint);
				}
				ThreadPool.QueueUserWorkItem(new WaitCallback(CheckForUpdates));
				while (true)
				{
					if ((m_slUpdateCheck != null) && !m_slUpdateCheck.ContinueWork()) break;
					lock (m_lock)
					{
						if (m_UpdateCheckStatus == UpdateCheckStatus.Checked) break;
						if (m_UpdateCheckStatus == UpdateCheckStatus.Error) break;
					}
				}
				if (m_slUpdateCheck != null) m_slUpdateCheck.EndLogging();
				PluginDebug.AddInfo("UpdateCheck finished ", 0, DebugPrint);
			}
			if ((m_UpdateCheckStatus == UpdateCheckStatus.NotChecked) || (m_UpdateCheckStatus == UpdateCheckStatus.Error)) UpdateCheckEx.Run(false, null);
			if (m_bRestartTriggered)
			{
				m_bRestartTriggered = false;
				return;
			}
			ThreadPool.QueueUserWorkItem(new WaitCallback(CheckPluginLanguages));
		}

		/// <summary>
		/// Check for available updates
		/// </summary>
		private void CheckForUpdates(object o)
		{
			string sBackup = KeePass.Program.Config.Application.LastUpdateCheck;
			try
			{
				lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checking; }
				KeePass.Program.Config.Application.LastUpdateCheck = TimeUtil.SerializeUtc(DateTime.UtcNow);
				MethodInfo mi = typeof(UpdateCheckEx).GetMethod("RunPriv", BindingFlags.Static | BindingFlags.NonPublic);
				Type t = typeof(UpdateCheckEx).GetNestedType("UpdateCheckParams", BindingFlags.NonPublic);
				ConstructorInfo c = t.GetConstructor(new Type[] { typeof(bool), typeof(Form) });
				object p = c.Invoke(new object[] { false, null });
				PluginDebug.AddInfo("UpdateCheck start RunPriv ", DebugPrint);
				mi.Invoke(null, new object[] { p });
				PluginDebug.AddInfo("UpdateCheck finish RunPriv ", DebugPrint);
				lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checked; }
			}
			catch (Exception)
			{
				KeePass.Program.Config.Application.LastUpdateCheck = sBackup;
				lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Error; }
			}
		}

		/// <summary>
		/// Check whether searching for updates of plugins or translations shall be done
		/// </summary>
		/// <returns>Type of required update check</returns>
		private UpdateCheckType UpdateCheckRequired()
		{
			UpdateCheckType result = UpdateCheckType.NotRequired;
			if (m_bUpdateCheckDone) return result;

			m_bUpdateCheckDone = true;
			if (!KeePass.Program.Config.Application.Start.CheckForUpdate) return result;
			if (!KeePass.Program.Config.Application.Start.OpenLastFile) return result;
			if (KeePass.Program.Config.Application.Start.MinimizedAndLocked) return result;

			DateTime dtNow = DateTime.UtcNow, dtLast;
			string strLast = KeePass.Program.Config.Application.LastUpdateCheck;
			if ((strLast.Length > 0) && TimeUtil.TryDeserializeUtc(strLast, out dtLast))
			{
				if (dtNow.Date != dtLast.Date) return UpdateCheckType.Required;
				return UpdateCheckType.NotRequired;
			}
			return UpdateCheckType.Required;
		}

		private IStatusLogger CreateUpdateCheckLogger()
		{
			IStatusLogger sl = StatusUtil.CreateStatusDialog(null, out m_CheckProgress,
				KeePass.Resources.KPRes.UpdateCheck, KeePass.Resources.KPRes.CheckingForUpd + "...", true, true);
			Button btnCancel = (Button)Tools.GetControl("m_btnCancel", m_CheckProgress);
			if (btnCancel != null)
			{
				int x = btnCancel.Right;
				btnCancel.AutoSize = true;
				btnCancel.Text = PluginTranslate.EnterBackgroundMode;
				btnCancel.Left = x - btnCancel.Width;
			}
			return sl;
		}
		#endregion

		#region Check for new translations
		private void CheckPluginLanguages(object o)
		{
			PluginDebug.AddInfo("Check for updated translations - Start");
			Dictionary<string, long> dTranslationsInstalled = GetInstalledTranslations();

			Dictionary<string, long> dTranslationsAvailable = new Dictionary<string, long>();
			try
			{
				dTranslationsAvailable = GetAvailableTranslations();
			}
			catch (Exception ex)
			{
				PluginDebug.AddError("Could not load available translations", 0, ex.Message);
				return;
			}

			bool bNew = false;
			string CurrentLanguage = "." + m_LanguageIso.ToLowerInvariant() + ".language.xml";
			string translations = string.Empty;
			List<string> lPlugins = new List<string>();
			foreach (KeyValuePair<string, long> kvp in dTranslationsAvailable)
			{
				//Check plugin version
				UpdateInfo ui = Plugins.Find(x => x.NameLowerInvariant == kvp.Key.Substring(0, kvp.Key.IndexOf(".")));
				if (ui == null) continue;

				if (!VersionsEqual(ui.VersionAvailable, ui.VersionInstalled)) continue;

				//Current translation is installed AND a newer version is available
				bNew = dTranslationsInstalled.ContainsKey(kvp.Key) && (dTranslationsInstalled[kvp.Key] < kvp.Value);

				//Current translation is not installed but available and DownloadActiveLanguage is true
				bNew |= !dTranslationsInstalled.ContainsKey(kvp.Key) && PluginConfig.DownloadActiveLanguage && kvp.Key.EndsWith(CurrentLanguage);

				if (bNew && !lPlugins.Contains(ui.Name)) lPlugins.Add(ui.Name);
			}
			if (lPlugins.Count == 0) return;
			using (TranslationUpdateForm t = new TranslationUpdateForm())
			{
				t.InitEx(lPlugins);
				if (t.ShowDialog() == DialogResult.OK)
				{
					lPlugins = t.SelectedPlugins;
					UpdatePluginTranslations(PluginConfig.DownloadActiveLanguage, lPlugins);
				}
			}
		}

		/// <summary>
		/// Compare two version
		/// Ignore MinorRevision if it's 0
		/// 
		/// 1.2.3.0 and 1.2.3 are considered equal
		/// </summary>
		/// <param name="vA"></param>
		/// <param name="vB"></param>
		/// <returns></returns>
		private bool VersionsEqual(Version vA, Version vB)
		{
			if (vA == null) return false;
			if (vB == null) return false;
			if (vA.Major != vB.Major) return false;
			if (vA.Minor != vB.Minor) return false;
			if ((vA.Build <= 0) && (vB.Build <= 0)) return true;
			if (vA.Build != vB.Build) return false;
			if ((vA.Revision <= 0) && (vB.Revision <= 0)) return true;
			if (vA.Revision != vB.Revision) return false;
			return true;
		}

		private Dictionary<string, long> GetInstalledTranslations()
		{
			Dictionary<string, long> dTranslationsInstalled = new Dictionary<string, long>();
			if (System.IO.Directory.Exists(m_PluginsTranslationsFolder))
			{
				try
				{
					Regex r = new Regex(@"\<TranslationVersion\>(\d+)\<\/TranslationVersion\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
					List<string> lTranslationsInstalled = UrlUtil.GetFilePaths(m_PluginsTranslationsFolder, "*.language.xml", System.IO.SearchOption.TopDirectoryOnly);
					foreach (string lang in lTranslationsInstalled)
					{
						dTranslationsInstalled[UrlUtil.GetFileName(lang).ToLowerInvariant()] = 0;
						string translation = System.IO.File.Exists(lang) ? System.IO.File.ReadAllText(lang) : string.Empty;
						Match m = r.Match(translation);
						if (m.Groups.Count != 2) continue;
						long lVerInstalled = 0;
						if (!long.TryParse(m.Groups[1].Value, out lVerInstalled)) continue;
						dTranslationsInstalled[UrlUtil.GetFileName(lang).ToLowerInvariant()] = lVerInstalled;
					}
				}
				catch (Exception) { }
			}
			if (PluginDebug.DebugMode)
			{
				List<string> lT = new List<string>();
				foreach (KeyValuePair<string, long> kvp in dTranslationsInstalled)
					lT.Add(kvp.Key + " - " + kvp.Value.ToString());
				PluginDebug.AddInfo("Installed languages", 0, lT.ToArray());
			}
			return dTranslationsInstalled;
		}

		private Dictionary<string, long> GetAvailableTranslations()
		{
			Dictionary<string, List<UpdateComponentInfo>> dUpdateInfo = new Dictionary<string, List<UpdateComponentInfo>>();
			Dictionary<string, long> dResult = new Dictionary<string, long>();
			Type t = typeof(KeePass.Program).Assembly.GetType("KeePass.Util.UpdateCheckEx");
			if (t == null)
			{
				PluginDebug.AddError("Could not locate class 'UpdateCheckEx'", 0);
				return dResult;
			}
			MethodInfo mi = t.GetMethod("DownloadInfoFiles", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Static);
			if (mi == null)
			{
				PluginDebug.AddError("Could not locate method 'DownloadInfoFiles'", 0);
				return dResult;
			}

			List<string> lUpdateUrls = new List<string>();
			foreach (UpdateInfo uiPlugin in Plugins)
			{
				string sUrl = uiPlugin.VersionInfoURL.ToLowerInvariant();
				if (!lUpdateUrls.Contains(sUrl)) lUpdateUrls.Add(sUrl);
			}
			if (lUpdateUrls.Count == 0)
			{
				PluginDebug.AddError("Could not read plugin update url", 0);
				return dResult;
			}

			dUpdateInfo = mi.Invoke(null, new object[] { lUpdateUrls, null }) as Dictionary<string, List<UpdateComponentInfo>>;

			List<string> lTranslationsNew = new List<string>();
			string[] cSplit = new string[] { "!", "!!!" };
			foreach (KeyValuePair<string, List<UpdateComponentInfo>> kvp in dUpdateInfo)
			{
				if (kvp.Value == null) continue;
				foreach (UpdateComponentInfo uci in kvp.Value)
				{
					//Plugins will be migrated from Sourceforge to Github one by one
					//Format for translation version:
					//Sourceforge: Lang!<Plugin>!!!<language identifier>
					//Github: <Plugin>!<language identifier>
					string[] sParts = null;
					if (uci.Name.StartsWith("Lang!"))
						sParts = uci.Name.Substring(5).Split(cSplit, StringSplitOptions.RemoveEmptyEntries);
					else
						sParts = uci.Name.Split(cSplit, StringSplitOptions.RemoveEmptyEntries);
					if (sParts.Length == 1)
					{
						UpdateInfo p = Plugins.Find(x => x.Title == sParts[0]);
						if (p == null) continue;
						p.VersionAvailable = new Version(StrUtil.VersionToString(uci.VerAvailable, 2));
					}
					if (sParts.Length != 2) continue;
					UpdateInfo ui = Plugins.Find(x => x.Title == sParts[0]);
					if (ui == null) continue;
					long lVer = 0;
					if (!long.TryParse(StrUtil.VersionToString(uci.VerAvailable), out lVer)) continue;
					dResult[(ui.NameLowerInvariant + "." + sParts[1] + ".language.xml").ToLowerInvariant()] = lVer;
				}
			}
			if (PluginDebug.DebugMode)
			{
				List<string> lT = new List<string>();
				foreach (KeyValuePair<string, long> kvp in dResult)
					lT.Add(kvp.Key + " - " + kvp.Value.ToString());
				PluginDebug.AddInfo("Available languages", 0, lT.ToArray());
			}
			return dResult;
		}
		#endregion

		#region Adjust UpdateCheckForm if required
		/// <summary>
		/// Show update indicator if plugins can be updated
		/// </summary>
		private List<Delegate> m_lEventHandlerItemActivate = null;
		private void OnUpdateCheckFormShown(object sender, EventArgs e)
		{
			m_lEventHandlerItemActivate = null;
			if (!PluginConfig.Active || !PluginConfig.OneClickUpdate) return;
			CustomListViewEx lvPlugins = (CustomListViewEx)Tools.GetControl("m_lvInfo", sender as UpdateCheckForm);
			if (lvPlugins == null)
			{
				PluginDebug.AddError("m_lvInfo not found", 0);
				return;
			}
			else PluginDebug.AddSuccess("m_lvInfo found", 0);
			if (Plugins.Count == 0) return;
			SetPluginSelectionStatus(false);
			bool bColumnAdded = false;
			m_lEventHandlerItemActivate = EventHelper.GetItemActivateHandlers(lvPlugins);
			if (m_lEventHandlerItemActivate.Count > 0)
			{
				EventHelper.RemoveItemActivateEventHandlers(lvPlugins, m_lEventHandlerItemActivate);
				lvPlugins.ItemActivate += LvPlugins_ItemActivate;
			}
			Image NoUpdate = UIUtil.CreateGrayImage(lvPlugins.SmallImageList.Images[1]);
			lvPlugins.SmallImageList.Images.Add("EUCCheckMarkImage", NoUpdate);

			foreach (ListViewItem item in lvPlugins.Items)
			{
				PluginDebug.AddInfo("Check plugin update status", 0, item.SubItems[0].Text, item.SubItems[1].Text);
				if (!item.SubItems[1].Text.Contains(KeePass.Resources.KPRes.NewVersionAvailable)) continue;
				foreach (UpdateInfo upd in Plugins)
				{
					if (item.SubItems[0].Text != upd.Title) continue;
					if (!upd.OwnPlugin && upd.UpdateMode == UpdateOtherPluginMode.Unknown) continue;
					if (!bColumnAdded)
					{
						lvPlugins.Columns.Add(PluginTranslate.PluginUpdate);
						bColumnAdded = true;
					}
					ListViewItem.ListViewSubItem lvsiUpdate = new ListViewItem.ListViewSubItem(item, PluginTranslate.PluginUpdate);
					lvsiUpdate.Tag = upd;
					item.SubItems.Add(lvsiUpdate);
					upd.Selected = true;
					try
					{
						upd.VersionAvailable = new Version(item.SubItems[3].Text);
					}
					catch (Exception ex)
					{
						PluginDebug.AddError("Could not parse new version", 0, upd.Name, item.SubItems[3].Text, ex.Message);
					}
					break;
				}
			}
			if (bColumnAdded)
			{
				UIUtil.ResizeColumns(lvPlugins, new int[] { 3, 3, 2, 2, 1 }, true);
				lvPlugins.MouseClick += OnUpdateCheckFormPluginMouseClick;
				lvPlugins.OwnerDraw = true;
				lvPlugins.DrawSubItem += LvPlugins_DrawSubItem;
				lvPlugins.DrawColumnHeader += LvPlugins_DrawColumnHeader;
				ShowUpdateButton(sender as Form, true);
			}
			if (m_lEventHandlerItemActivate.Count == 0)
			{
				if (lvPlugins.ContextMenuStrip == null)
				{
					lvPlugins.ContextMenuStrip = new ContextMenuStrip();
					string sMenuText = KeePass.Resources.KPRes.PluginsDesc;
					try { sMenuText = Tools.GetControl("m_linkPlugins", sender as UpdateCheckForm).Text; }
					catch { }
					lvPlugins.ContextMenuStrip.Items.Add(new ToolStripMenuItem(sMenuText, null, OnReleasePageClick));
					lvPlugins.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
				}
				else PluginDebug.AddWarning("m_lvEntries.ContextMenuStrip already defined, special handling for added 'go to release page' to be defined", 0);
			}
		}

		private void LvPlugins_ItemActivate(object sender, EventArgs e)
		{
			try
			{
				var lv = sender as CustomListViewEx;
				var lvi = lv.SelectedItems[0];
				UpdateInfo upd = Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				if (upd == null)
				{
					foreach (Delegate d in m_lEventHandlerItemActivate)
						d.DynamicInvoke(new object[] { sender, e });
				}
				else
				{
					string url = upd.GetPluginUrl();
					try { WinUtil.OpenUrl(url, null, true); }
					catch (Exception exUrl) { Tools.ShowError(url + "\n\n" + exUrl.Message); }
				}
			}
			catch { }

		}

		private void OnReleasePageClick(object sender, EventArgs e)
		{
			try
			{
				var lv = ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl as CustomListViewEx;
				var lvi = lv.SelectedItems[0];
				UpdateInfo upd = Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				if (upd == null) return;
				string url = upd.GetPluginUrl();
				try { System.Diagnostics.Process.Start(url); }
				catch (Exception exUrl)	{ Tools.ShowError(url + "\n\n" + exUrl.Message);
				}

				WinUtil.OpenUrl(url, null, true);
			}
			catch { }
		}

		private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			try
			{
				var lv = (sender as ContextMenuStrip).SourceControl as ListView;
				var lvi = lv.SelectedItems[0];
				UpdateInfo upd = Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				e.Cancel = upd == null;
			}
			catch { }
		}

		private void OnUpdateCheckFormPluginMouseClick(object sender, MouseEventArgs e)
		{
			ListViewHitTestInfo info = (sender as ListView).HitTest(e.X, e.Y);
			UpdateInfo upd = info.Item.SubItems[info.Item.SubItems.Count - 1].Tag as UpdateInfo;
			if (upd == null) return;
			upd.Selected = !upd.Selected;
			if (!ShowUpdateButton((sender as Control).Parent as Form, Plugins.Find(x => x.Selected == true) != null))
			{
				upd.Selected = !upd.Selected;
				bUpdatePlugins_Click(sender, e);
			}
			else
				(sender as ListView).Parent.Refresh();
		}

		/// <summary>
		///	Show and enable Update button if possible
		///	Definiton of 'possible':
		///		- Update button can be added to UpdateCheckForm
		///		- At least one plugin can be updated
		///		- At least one updateable plugin is selected
		/// </summary>
		/// <param name="form">Reference to UpdateCheckForm</param>
		/// <param name="enable">True, if Update button shall be enabled</param>
		/// <returns>True, if successful</returns>
		private bool ShowUpdateButton(Form form, bool enable)
		{
			Button bUpdate = Tools.GetControl("EUCUpdateButton", form) as Button;
			if (bUpdate == null)
				try
				{
					Button bClose = Tools.GetControl("m_btnClose", form) as Button;
					bUpdate = new Button();
					bUpdate.Text = PluginTranslate.PluginUpdateSelected;
					bUpdate.Left = 0;
					bUpdate.Top = bClose.Top;
					bUpdate.Height = bClose.Height;
					bUpdate.AutoSize = true;
					bUpdate.Name = "EUCUpdateButton";
					bUpdate.Click += bUpdatePlugins_Click;
					bClose.Parent.Controls.Add(bUpdate);
					if (ShouldShieldify)
					{
						bUpdate.Width += DpiUtil.ScaleIntX(16);
						UIUtil.SetShield(bUpdate, true);
					}
					bUpdate.Left = bClose.Left - bUpdate.Width - 15;
				}
				catch (Exception) { }
			bUpdate = Tools.GetControl("EUCUpdateButton", form) as Button;
			if (bUpdate == null)
			{
				foreach (UpdateInfo upd in Plugins)
					upd.Selected = false;
				CustomListViewEx lvPlugins = (CustomListViewEx)Tools.GetControl("m_lvInfo", form);
				lvPlugins.OwnerDraw = false;
				return false;
			}
			bUpdate.Enabled = enable;
			return true;
		}

		private void LvPlugins_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
		}

		private void LvPlugins_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			ListView lvPlugins = sender as ListView;
			e.DrawDefault = true;
			if (e.ColumnIndex != lvPlugins.Items[0].SubItems.Count) return;
			UpdateInfo upd = m_lPluginUpdateInfo.Find(x => x.Title == e.Item.SubItems[0].Text);
			if (upd == null) return;
			e.DrawDefault = false;
			e.DrawBackground();
			int i = upd.Selected ? 1 : lvPlugins.SmallImageList.Images.IndexOfKey("EUCCheckMarkImage");
			UIUtil.CreateGrayImage((sender as ListView).SmallImageList.Images[1]);
			var imageRect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Height, e.Bounds.Height);
			e.Graphics.DrawImage((sender as ListView).SmallImageList.Images[i], imageRect);
		}
		#endregion

		#region Update plugins
		/// <summary>
		/// Get list of installed plugins
		/// </summary>
		/// <returns></returns>
		public List<UpdateInfo> GetInstalledPlugins()
		{
			m_lPluginUpdateInfo = new List<UpdateInfo>();
			BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
			try
			{
				var PluginManager = Tools.GetField("m_pluginManager", KeePass.Program.MainForm);
				var PluginList = Tools.GetField("m_vPlugins", PluginManager);
				MethodInfo IteratorMethod = PluginList.GetType().GetMethod("System.Collections.Generic.IEnumerable<T>.GetEnumerator", bf);
				IEnumerator<object> PluginIterator = (IEnumerator<object>)(IteratorMethod.Invoke(PluginList, null));
				while (PluginIterator.MoveNext())
				{
					Plugin result = (Plugin)Tools.GetField("m_pluginInterface", PluginIterator.Current);
					if (result == null) continue;
					UpdateInfo upd;
					if (GetPluginUpdateInfo(result, out upd)) m_lPluginUpdateInfo.Add(upd);
				}
			}
			catch (Exception ex) { PluginDebug.AddError(ex.Message); }
			PluginDebug.AddInfo("Installed updateable plugins", m_lPluginUpdateInfo.ConvertAll(new Converter<UpdateInfo, string>(UpdateInfo.GetName)).ToArray());
			return m_lPluginUpdateInfo;
		}

		private static Dictionary<string, Version> m_Plugins = new Dictionary<string, Version>();
		private bool GetPluginUpdateInfo(Plugin result, out UpdateInfo upd)
		{
			if (m_Plugins.Count == 0) m_Plugins = Tools.GetLoadedPluginsName();
			upd = null;
			AssemblyCompanyAttribute[] comp = (AssemblyCompanyAttribute[])result.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			AssemblyTitleAttribute[] title = (AssemblyTitleAttribute[])result.GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if ((comp.Length != 1) || (title.Length != 1)) return false;

			Version v;
			if (!Tools.GetLoadedPluginsName().TryGetValue(result.ToString(), out v)) v = result.GetType().Assembly.GetName().Version;
			//One of my plugins
			if (string.Compare("rookiestyle", comp[0].Company, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				Type tools = result.GetType().Assembly.GetType("PluginTools.Tools");
				if (tools == null) return false;
				FieldInfo fURL = tools.GetField("PluginURL");
				if (fURL == null) return false;
				string URL = (string)fURL.GetValue(result);
				if (string.IsNullOrEmpty(URL)) return false;
				upd = new UpdateInfo(result.GetType().Namespace, title[0].Title, URL, result.UpdateUrl, v);
			}
			else
			{
				upd = new UpdateInfo(result.GetType().Namespace, title[0].Title, result.UpdateUrl, v, false);
			}
			return upd != null && upd.UpdatePossible;
		}

		/// <summary>
		/// Update installed plugin translations
		/// </summary>
		/// <param name="bDownloadActiveLanguage">Download translation for currently used language even if not installed yet</param>
		private delegate void UpdatePluginsDelegate(bool bUpdateTranslationsOnly);
		public void UpdatePluginTranslations(bool bDownloadActiveLanguage, List<string> lUpdateTranslations)
		{
			foreach (var upd in Plugins)
				upd.Selected = (lUpdateTranslations == null) || lUpdateTranslations.Contains(upd.Name);
			bool bBackup = PluginConfig.DownloadActiveLanguage;
			PluginConfig.DownloadActiveLanguage = bDownloadActiveLanguage;

			//If called from CheckPluginLanguages, we're running in adifferent thread
			//Use Invoke because the IStatusLogger will attach to tho KeyPromptForm within the UI thread
			UpdatePluginsDelegate delUpdate = UpdatePlugins;
			m_host.MainWindow.Invoke(delUpdate, new object[] { true });
			PluginConfig.DownloadActiveLanguage = bBackup;
			foreach (var upd in Plugins)
			{
				if (!upd.Selected) continue;
				Plugin p = (Plugin)Tools.GetPluginInstance(upd.Name);
				if (p == null) continue;
				Type t = p.GetType().Assembly.GetType("PluginTranslation.PluginTranslate");
				if (t == null) continue;
				MethodInfo miInit = t.GetMethod("Init", BindingFlags.Static | BindingFlags.Public);
				if (miInit == null) continue;
				try
				{
					miInit.Invoke(p, new object[] { p, m_LanguageIso });
				}
				catch (Exception ex)
				{
					PluginDebug.AddError("Refresh translation failed", 0, "Plugin: " + upd.Name, "Error: " + ex.Message);
				}
			}
			SetPluginSelectionStatus(false);
		}

		private void bUpdatePlugins_Click(object sender, EventArgs e)
		{
			UpdatePlugins(false);
		}

		private void UpdatePlugins(bool bUpdateTranslationsOnly)
		{ 
			PluginDebug.AddInfo("UpdatePlugins start ", DebugPrint);
			Form fUpdateLog = null;
			m_slUpdatePlugins = StatusUtil.CreateStatusDialog(GlobalWindowManager.TopWindow, out fUpdateLog, PluginTranslate.PluginUpdateCaption, string.Empty, true, true);

			bool bUseTemp = false;
			bool success = false;
			string sTempPluginsFolder = m_PluginsFolder;
			string sTempPluginsTranslationsFolder = m_PluginsTranslationsFolder;

			ThreadStart ts = new ThreadStart(() =>
			{
				m_slUpdatePlugins.StartLogging(PluginTranslate.PluginUpdateCaption, false);
				//try writing to plugin folder
				string sTempFile = m_PluginsFolder + "EarlyUpdateCheckTest.txt";
				try
				{
					System.IO.File.WriteAllText(sTempFile, "Test file to check for write access");
				}
				catch (Exception) { bUseTemp = true; }
				if (!bUseTemp) System.IO.File.Delete(sTempFile);

				//define working folders 
				if (bUseTemp)
				{
					sTempPluginsFolder = UrlUtil.GetTempPath();
					sTempPluginsFolder = UrlUtil.EnsureTerminatingSeparator(sTempPluginsFolder, false);
					sTempPluginsFolder += System.IO.Path.GetRandomFileName();
					sTempPluginsFolder = UrlUtil.EnsureTerminatingSeparator(sTempPluginsFolder, false);
					System.IO.Directory.CreateDirectory(sTempPluginsFolder);
					sTempPluginsTranslationsFolder = sTempPluginsFolder + "Translations";
					sTempPluginsTranslationsFolder = UrlUtil.EnsureTerminatingSeparator(sTempPluginsTranslationsFolder, false);
					System.IO.Directory.CreateDirectory(sTempPluginsTranslationsFolder);
				}
				PluginDebug.AddInfo("Use temp folder", bUseTemp.ToString(), sTempPluginsFolder, DebugPrint);

				//Download files
				foreach (UpdateInfo upd in Plugins)
				{
					if (!upd.Selected) continue;
					success |= UpdatePlugin(upd, sTempPluginsFolder, sTempPluginsTranslationsFolder, bUpdateTranslationsOnly);
				}
			});
			Thread t = new Thread(ts);
			t.Start();
			while (true && t.IsAlive)
			{
				if (!m_slUpdatePlugins.ContinueWork())
				{
					t.Abort();
					break;
				}
			}
			if (m_slUpdatePlugins != null)
			{
				m_slUpdatePlugins.EndLogging();
				m_slUpdatePlugins = null;
			}
			if (fUpdateLog != null) fUpdateLog.Dispose();
			
			//Move files from temp folder to plugin folder
			if (bUseTemp) 
			{
				success = false;
				if (WinUtil.IsAtLeastWindowsVista &&
					(NativeMethods.ShieldifyNativeDialog(DialogResult.Yes, () => Tools.AskYesNo(PluginTranslate.TryUAC, PluginTranslate.PluginUpdateCaption)) == DialogResult.Yes))
				{
					success = FileCopier.CopyFiles(sTempPluginsFolder, m_PluginsFolder);
					if (!success)
					{
						if (Tools.AskYesNo(PluginTranslate.PluginUpdateFailed, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes)
						{
							System.Diagnostics.Process.Start(sTempPluginsFolder);
						}
					}
				}
				else if (Tools.AskYesNo(PluginTranslate.OpenTempFolder, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes)
				{
					System.Diagnostics.Process.Start(sTempPluginsFolder);
				}
			}

			//Restart KeePass to use new plugin versions
			PluginDebug.AddInfo("Update finished", "Succes: " + success.ToString(), DebugPrint);
			if (success && !bUpdateTranslationsOnly)
			{
				if (Tools.AskYesNo(PluginTranslate.PluginUpdateSuccess, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes)
				{
					if (m_bRestartInvoke)
					{
						m_host.MainWindow.Invoke(new KeePassLib.Delegates.VoidDelegate(Restart));
					}
					else
						Restart();
				}
			}
		}

		private string GetUpdateUrl(string baseurl, UpdateInfo upd, string language)
		{
			if (!upd.OwnPlugin)
			{
				if (upd.UpdateInfoExtern == null) return upd.URL;
				return upd.UpdateInfoExtern.PluginUpdateURL;
			}
			if (string.IsNullOrEmpty(language))
				return baseurl + "releases/download/v" + upd.VersionAvailable.ToString() + "/" + upd.Name + ".plgx";
			return baseurl.Replace("github.com", "raw.githubusercontent.com") + "master/Translations/" + language;
		}

		/// <summary>
		/// Update a single plugin
		/// </summary>
		/// <param name="upd">Update information for plugin</param>
		/// <param name="sPluginFolder">Target folder for plugin file</param>
		/// <param name="sTranslationFolder">Target folder for plugin translations</param>
		/// <param name="bTranslationsOnly">Only download newest translations</param>
		/// <returns></returns>
		public bool UpdatePlugin(UpdateInfo upd, string sPluginFolder, string sTranslationFolder, bool bTranslationsOnly)
		{
			string url = UrlUtil.EnsureTerminatingSeparator(upd.URL, true);

			if (!upd.OwnPlugin && upd.UpdateMode == UpdateOtherPluginMode.Unknown)
			{
				PluginDebug.AddError("Plugin url not supported", 0, upd.URL);
				return false;
			}

			//Get installed translations
			List<string> lTranslations = new List<string>();
			if (System.IO.Directory.Exists(m_PluginsTranslationsFolder))
			{
				try
				{
					lTranslations = UrlUtil.GetFilePaths(m_PluginsTranslationsFolder, upd.Name + "*.language.xml", System.IO.SearchOption.TopDirectoryOnly);
				}
				catch (Exception) { }
			}
			bool ok = false;

			//Download plugin
			if (!bTranslationsOnly)
			{
				m_slUpdatePlugins.SetText(string.Format(PluginTranslate.PluginUpdating, upd.Title, upd.Name + ".plgx"), LogStatusType.Info);
				if (!m_slUpdatePlugins.ContinueWork()) return ok;
				if (!DownloadFile(GetUpdateUrl(url, upd, null), sPluginFolder + upd.Name + ".plgx", upd))
				{
					Tools.ShowError(string.Format(PluginTranslate.PluginUpdateFailedSpecific, upd.Title), PluginTranslate.PluginUpdateCaption);
				}
				else
					ok = true;
			}
			if (!m_slUpdatePlugins.ContinueWork()) return ok;

			//Download translations
			if (upd.OwnPlugin)
			{
				foreach (string lang in lTranslations)
				{
					string langUrl = lang.Substring(m_PluginsTranslationsFolder.Length);
					m_slUpdatePlugins.SetText(string.Format(PluginTranslate.PluginUpdating, upd.Title, langUrl), LogStatusType.Info);
					if (!m_slUpdatePlugins.ContinueWork()) return ok;
					if (!DownloadFile(GetUpdateUrl(url, upd, langUrl), sTranslationFolder + langUrl))
						Tools.ShowError(string.Format(PluginTranslate.PluginTranslationUpdateFailed, upd.Title, langUrl), PluginTranslate.PluginUpdateCaption);
				}

				//If required, download translation for currently used language
				string CurrentTranslation = m_PluginsTranslationsFolder + upd.Name + "." + m_LanguageIso + ".language.xml";
				if (PluginConfig.DownloadActiveLanguage && !lTranslations.Contains(CurrentTranslation))
				{
					string langUrl = CurrentTranslation.Substring(m_PluginsTranslationsFolder.Length);
					m_slUpdatePlugins.SetText(string.Format(PluginTranslate.PluginUpdating, upd.Title, langUrl), LogStatusType.Info);
					if (!m_slUpdatePlugins.ContinueWork()) return ok;
					DownloadFile(GetUpdateUrl(url, upd, langUrl), sTranslationFolder + langUrl);
				}
			}

			return ok;
		}

		/// <summary>
		/// Download single file
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		private bool DownloadFile(string source, string target)
		{
			return DownloadFile(source, target, null);
		}

		private bool DownloadFile(string source, string target, UpdateInfo upd)
		{
			const int MAXATTEMPTS = 3;
			int iAttempts = 0;
			//if (upd == null && !source.ToLowerInvariant().EndsWith(".plgx")) return true;
			while (iAttempts++ < MAXATTEMPTS)
			{
				try
				{
					KeePassLib.Serialization.IOConnectionInfo ioc = KeePassLib.Serialization.IOConnectionInfo.FromPath(source);
					System.IO.Stream s = KeePassLib.Serialization.IOConnection.OpenRead(ioc);
					if (s == null) throw new InvalidOperationException();
					System.IO.MemoryStream ms = new System.IO.MemoryStream();
					MemUtil.CopyStream(s, ms);
					s.Close();
					byte[] pb = ms.ToArray();
					ms.Close();
					string sTargetDir = UrlUtil.GetShortestAbsolutePath(UrlUtil.GetFileDirectory(target, false, true));
					if (!System.IO.Directory.Exists(sTargetDir)) System.IO.Directory.CreateDirectory(sTargetDir);
					if (upd != null && !upd.OwnPlugin)
					{
						pb = ProcessPluginDownload(source, pb, upd);
						if (upd.IsDll) target = target.Replace(".plgx", ".dll");
					}
					if (pb == null || pb.Length == 0) throw new ArgumentException("No special handling defined for " + upd.Title);
					System.IO.File.WriteAllBytes(target, pb);
					PluginDebug.AddInfo("Download success", 0, "Source: " + source, "Target: " + target, "Download attempt: " + iAttempts.ToString());
					return true;
				}
				catch (Exception ex)
				{
					PluginDebug.AddInfo("Download failed", 0, "Source: " + source, "Target: " + target, "Download attempt: " + iAttempts.ToString(), ex.Message);

					System.Net.WebException exWeb = ex as System.Net.WebException;
					if (exWeb == null) continue;
					System.Net.HttpWebResponse wrResponse = exWeb.Response as System.Net.HttpWebResponse;
					if ((wrResponse == null) || (wrResponse.StatusCode != System.Net.HttpStatusCode.NotFound)) continue;
					iAttempts = MAXATTEMPTS;
				}
			}
			return false;
		}

		private byte[] ProcessPluginDownload(string source, byte[] pb, UpdateInfo upd)
		{
			UpdateInfoExtern uie = upd.UpdateInfoExtern;
			if (uie == null) return null;
			if (uie.UpdateMode == UpdateOtherPluginMode.PlgxDirect)
			{
				return pb;
			}
			if (uie.UpdateMode == UpdateOtherPluginMode.ZipExtractPlgx)
			{
				using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
				{
					ms.Write(pb, 0, pb.Length);
					ms.Position = 0;
					pb = null;
					using (Ionic.Zip.ZipFile z = Ionic.Zip.ZipFile.Read(ms))
					{
						var f = z.SelectEntries("*.plgx");
						foreach (var p in f)
						{
							using (System.IO.MemoryStream msTarget = new System.IO.MemoryStream())
							{
								p.Extract(msTarget);
								return msTarget.ToArray();
							}
						}
					}
				}
				return null;
			}
			else return null;
		}

		private void Restart()
		{
			PluginDebug.AddInfo("Restart started", DebugPrint);
			if (m_kpf != null)
			{
				PluginDebug.AddInfo("Closing KeyPromptForm", 0, DebugPrint);
				m_kpf.DialogResult = DialogResult.Cancel;
				m_kpf.Close();
				m_kpf.Dispose();
				Application.DoEvents();
				GlobalWindowManager.RemoveWindow(m_kpf);
			}
			if (m_slUpdateCheck != null)
			{
				PluginDebug.AddInfo("Closing update check progress form", 0, DebugPrint);
				m_slUpdateCheck.EndLogging();
			}
			FieldInfo fi = m_host.MainWindow.GetType().GetField("m_bRestart", BindingFlags.NonPublic | BindingFlags.Instance);
			if (fi != null)
			{
				PluginDebug.AddInfo("Restart started, m_bRestart found", DebugPrint);
				HandleMutex(true);
				fi.SetValue(m_host.MainWindow, true);
				m_bRestartTriggered = true;
				m_host.MainWindow.ProcessAppMessage((IntPtr)KeePass.Program.AppMessage.Exit, IntPtr.Zero);
				HandleMutex(false);
			}
			else
				PluginDebug.AddError("Restart started, m_bRestart not found" + DebugPrint);
		}

		/// <summary>
		/// Release (and potentially restore) the mutex used for limiting KeePass to a single instance
		/// In some cases the restart with WinUtil.Restart is done while the global mutex is not yet released
		/// </summary>
		/// <param name="release"></param>
		private void HandleMutex(bool release)
		{
			List<string> lStrings = new List<string>();
			lStrings.Add("Mutex: " + KeePass.App.AppDefs.MutexName);
			lStrings.Add("Release: " + release.ToString());
			lStrings.Add("Single Instance: " + KeePass.Program.Config.Integration.LimitToSingleInstance.ToString());

			//Get number of loaded databases
			//KeePass creates an initial PwDocument during startup which must NOT be considered here
			//
			//Initial = No LockedIoc and PwDatabase is not opened (!IsOpen)
			int iOpenedDB = 0;
			foreach (PwDocument pd in KeePass.Program.MainForm.DocumentManager.Documents)
			{
				if (!string.IsNullOrEmpty(pd.LockedIoc.Path)) iOpenedDB++;
				else if ((pd.Database != null) && pd.Database.IsOpen) iOpenedDB++;
			}
			lStrings.Add("Opened databases: " + iOpenedDB.ToString());

			if (!KeePass.Program.Config.Integration.LimitToSingleInstance)
			{
				lStrings.Add("Action required: No");
				PluginDebug.AddInfo("Handle global mutex", 0, lStrings.ToArray());
				return;
			}
			if (release)
			{
				bool bSuccess = GlobalMutexPool.ReleaseMutex(KeePass.App.AppDefs.MutexName);
				lStrings.Add("Action required: Yes");
				lStrings.Add("Success: " + bSuccess.ToString());
				PluginDebug.AddInfo("Handle global mutex", 0, lStrings.ToArray());
				return;
			}
			else if (iOpenedDB > 0)
			{
				//Only recreate mutex if at least document is loaded (lock flag is not relevant here)
				//If no db is open, Restart is in progress already
				lStrings.Add("Action required: Yes");
				lStrings.Add("Success: See next entry");
				PluginDebug.AddInfo("Handle global mutex", 0, lStrings.ToArray());
				Thread t = new Thread(new ThreadStart(() =>
				  {
					  Thread.Sleep(PluginConfig.RestoreMutexThreshold);
					  bool bSuccess = GlobalMutexPool.CreateMutex(KeePass.App.AppDefs.MutexName, true);
					  PluginDebug.AddInfo("Handle global mutex", 0, "Recreate mutex sucessful: " + bSuccess.ToString());
				  }));
				t.Start();
				return;
			}
			lStrings.Add("Action required: No");
			PluginDebug.AddInfo("Handle global mutex", 0, lStrings.ToArray());
		}

		private void SetPluginSelectionStatus(bool select)
		{
			foreach (var p in Plugins) p.Selected = select;
		}
		#endregion

		#region Configuration
		private void OptionsFormShown(object sender, Tools.OptionsFormsEventArgs e)
		{
			Options options = new Options();
			options.gCheckSync.Checked = PluginConfig.Active;
			options.cbCheckSync.Checked = PluginConfig.CheckSync;
			options.gOneClickUpdate.Checked = PluginConfig.OneClickUpdate;
			options.cbDownloadCurrentTranslation.Checked = PluginConfig.DownloadActiveLanguage;
			options.Plugin = this;
			Tools.AddPluginToOptionsForm(this, options);
		}

		private void OptionsFormClosed(object sender, Tools.OptionsFormsEventArgs e)
		{
			if (e.form.DialogResult != DialogResult.OK) return;
			bool shown = false;
			Options options = (Options)Tools.GetPluginFromOptions(this, out shown);
			if (!shown) return;
			PluginConfig.Active = options.gCheckSync.Checked;
			PluginConfig.CheckSync = options.cbCheckSync.Checked;
			PluginConfig.OneClickUpdate = options.gOneClickUpdate.Checked;
			PluginConfig.DownloadActiveLanguage = options.cbDownloadCurrentTranslation.Checked;
			PluginConfig.Write();
		}
		#endregion

		public override void Terminate()
		{
			if (m_host == null) return;
			Deactivate();
			PluginDebug.SaveOrShow();
			m_host = null;
		}

		public void Deactivate()
		{
			GlobalWindowManager.WindowAdded -= WindowAdded;
			m_host.MainWindow.ToolsMenu.DropDownItems.Remove(m_tsMenu);
			m_tsMenu.Dispose();

			Tools.OptionsFormShown -= OptionsFormShown;
			Tools.OptionsFormClosed -= OptionsFormClosed;
		}

		public override string UpdateUrl
		{
			get { return "https://raw.githubusercontent.com/rookiestyle/earlyupdatecheck/master/version.info"; }
		}

		public override Image SmallIcon
		{
			get { return m_tsMenu.Image; }
		}

		private string DebugPrint
		{
			get
			{
				string result = "DifferentThread: {0}, Check status: {1}, UI interaction blocked: {2}";
				lock(m_lock)
				{
					result = string.Format(result, m_bRestartInvoke.ToString(), m_UpdateCheckStatus.ToString(), KeePass.Program.MainForm.UIIsInteractionBlocked().ToString());
				}
				return result;
			}
		}
	}
}