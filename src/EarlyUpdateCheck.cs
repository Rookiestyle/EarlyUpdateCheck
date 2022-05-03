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
		private UpdateCheckStatus m_UpdateCheckStatus = UpdateCheckStatus.NotChecked;
		private object m_lock = new object();

		private IStatusLogger m_slUpdateCheck = null;
		private Form m_CheckProgress = null;
		private bool m_bRestartInvoke = false;
		private bool m_bRestartTriggered = false;
		private IStatusLogger m_slUpdatePlugins = null;
		private bool m_bUpdateCheckDone = false;

		private KeyPromptForm m_kpf = null;

		public override bool Initialize(IPluginHost host)
		{
			m_host = host;

			PluginTranslate.TranslationChanged += delegate (object sender, TranslationChangedEventArgs e)
			{
				if (!string.IsNullOrEmpty(KeePass.Program.Translation.Properties.Iso6391Code))
					PluginUpdateHandler.LanguageIso = KeePass.Program.Translation.Properties.Iso6391Code;
				else
					PluginUpdateHandler.LanguageIso = e.NewLanguageIso6391;
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

			m_host.MainWindow.FormLoadPost += MainWindow_FormLoadPost;

			PluginUpdateHandler.Init();

			return true;
		}

		List<Delegate> m_lFormLoadPostHandlers = new List<Delegate>();
		private void RemoveAndBackupFormLoadPostHandlers()
		{
			m_lFormLoadPostHandlers = EventHelper.GetFormLoadPostHandlers();
			m_host.MainWindow.Invoke(new KeePassLib.Delegates.GAction(() =>
			{
				if (KeePass.Program.MainForm == null || KeePass.Program.MainForm.Disposing || KeePass.Program.MainForm.IsDisposed) return;
				EventHelper.RemoveFormLoadPostEventHandlers(m_lFormLoadPostHandlers);
			}));
			m_host.MainWindow.FormLoadPost += RestoreFormLoadPostHandlers;
		}

		private void RestoreFormLoadPostHandlers(object sender, EventArgs e)
		{
			Thread t = new Thread(() =>
			{
				Thread.Sleep(PluginConfig.RestoreMutexThreshold);
				if (KeePass.Program.MainForm == null || KeePass.Program.MainForm.Disposing || KeePass.Program.MainForm.IsDisposed)
				{
					PluginDebug.AddInfo("Restore MainForm.FormLoadPost handlers not done");
					return;
				}
				EventHelper.RestoreFormLoadPostEventHandlers(m_lFormLoadPostHandlers);
				PluginDebug.AddInfo("Restore MainForm.FormLoadPost handlers done");
				foreach (var del in m_lFormLoadPostHandlers)
				{
					if (KeePass.Program.MainForm != null && !KeePass.Program.MainForm.Disposing && !KeePass.Program.MainForm.IsDisposed)
						del.DynamicInvoke(new object[] { sender, e });
				}
				PluginDebug.AddInfo("MainForm.FormLoadPost handlers executed");
			});
			t.IsBackground = true;
			t.Start();
		}

		private void MainWindow_FormLoadPost(object sender, EventArgs e)
		{
			//Can be null if restart after upgrade is in progress
			//In this case, EarlyUpdateCheckExt.Terminate() might have been called already
			//RemoveAndBackupFormLoadPostHandlers and RestoreFormLoadPostHandlers should work around that, but you never know...
			if (m_host == null || m_host.MainWindow == null) return;
			if (m_host.MainWindow.IsDisposed || m_host.MainWindow.Disposing) return;

			m_host.MainWindow.FormLoadPost -= MainWindow_FormLoadPost;

			//Load plugins and check check for new translations if not already done
			if (PluginUpdateHandler.CheckTranslations && !m_bRestartTriggered)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(CheckPluginLanguages));
			}
			else if (!m_bRestartTriggered)
			{
				//Only load plugins, do NOT check for new translations
				ThreadPool.QueueUserWorkItem(new WaitCallback((object o) =>
				{
					PluginUpdateHandler.LoadPlugins(false);
					CheckExternalPluginUpdate();
				}));
			}
			PluginDebug.AddInfo("All plugins loaded", 0, DebugPrint);
			m_bRestartInvoke = false;
			m_bRestartTriggered = false;
		}

		private void WindowAdded(object sender, GwmWindowEventArgs e)
		{
			if (!PluginConfig.Active) return;
			PluginDebug.AddInfo("Form added", 0, e.Form.Name, e.Form.GetType().FullName, DebugPrint);
			if (e.Form is UpdateCheckForm)
			{
				if (m_CheckProgress != null && !m_CheckProgress.IsDisposed && !m_CheckProgress.Disposing)
				{
					if (!KeePassLib.Native.NativeLib.IsUnix()) m_CheckProgress.Hide(); //Makes KeePass freeze sometimes... How I love randomness in development...
					lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checked; }
				}
				if (PluginConfig.OneClickUpdate)
				{
					e.Form.Shown += OnUpdateCheckFormShown;
				}
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
			if ((m_kpf != null) && (e.Form is KeyPromptForm)) m_kpf = null;
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
				UpdateCheckBackground();
			if ((m_UpdateCheckStatus == UpdateCheckStatus.NotChecked) || (m_UpdateCheckStatus == UpdateCheckStatus.Error)) UpdateCheckEx.Run(false, null);
		}

		private void UpdateCheckBackground()
		{
			List<string> lMsg = new List<string>();
			string sBackup = KeePass.Program.Config.Application.LastUpdateCheck;
			KeePass.Program.Config.Application.LastUpdateCheck = TimeUtil.SerializeUtc(DateTime.UtcNow);

			bool bOK = true;
			MethodInfo miGetInstalledComponents = typeof(UpdateCheckEx).GetMethod("GetInstalledComponents", BindingFlags.Static | BindingFlags.NonPublic);
			if (miGetInstalledComponents == null)
			{
				bOK = false;
				lMsg.Add("Could not locate UpdateCheckEx.GetInstalledComponents");
			}

			MethodInfo miGetUrls = typeof(UpdateCheckEx).GetMethod("GetUrls", BindingFlags.Static | BindingFlags.NonPublic);
			if (miGetUrls == null)
			{
				bOK = false;
				lMsg.Add("Could not locate UpdateCheckEx.GetUrls");
			}

			MethodInfo miDownloadInfoFiles = typeof(UpdateCheckEx).GetMethod("DownloadInfoFiles", BindingFlags.Static | BindingFlags.NonPublic);
			if (miDownloadInfoFiles == null)
			{
				bOK = false;
				lMsg.Add("Could not locate UpdateCheckEx.DownloadInfoFiles");
			}

			MethodInfo miMergeInfo = typeof(UpdateCheckEx).GetMethod("MergeInfo", BindingFlags.Static | BindingFlags.NonPublic);
			if (miMergeInfo == null)
			{
				bOK = false;
				lMsg.Add("Could not locate UpdateCheckEx.MergeInfo");
			}

			try
			{
				m_bRestartInvoke = true;
				KeePassLib.Delegates.GAction actUpdateCheck = new KeePassLib.Delegates.GAction(() =>
				{
					//taken from UpdateCheckExt.RunPriv
					//MainForm.InvokeRequired is not true on Mono :(
					try
					{
						lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checking; }
						List<UpdateComponentInfo> lInst = (List<UpdateComponentInfo>)miGetInstalledComponents.Invoke(null, null);
						List<string> lUrls = (List<string>)miGetUrls.Invoke(null, new object[] { lInst });
						Dictionary<string, List<UpdateComponentInfo>> dictAvail =
							(Dictionary<string, List<UpdateComponentInfo>>)miDownloadInfoFiles.Invoke(null, new object[] { lUrls, null /* m_slUpdateCheck */});
						if (dictAvail == null) return; // User cancelled

						miMergeInfo.Invoke(null, new object[] { lInst, dictAvail });

						bool bUpdAvail = false;
						foreach (UpdateComponentInfo uc in lInst)
						{
							if (uc.Status == UpdateComponentStatus.NewVerAvailable)
							{
								bUpdAvail = true;
								break;
							}
						}

						if (m_slUpdateCheck != null)
						{
							if (bUpdAvail) UpdateStatusLogger();
							EndLogging();
							m_slUpdateCheck = null;
						}

						KeePassLib.Delegates.GAction actShowUpdateForm_UIThread = new KeePassLib.Delegates.GAction(() =>
						{
							try
							{
								// Do not show the update dialog while auto-typing;
								// https://sourceforge.net/p/keepass/bugs/1265/
								if (SendInputEx.IsSending) return;

								UpdateCheckForm dlg = new UpdateCheckForm();
								dlg.InitEx(lInst, false);
								var dr = UIUtil.ShowDialogAndDestroy(dlg);
							}
							catch (Exception ex)
							{
								bOK = false;
								lMsg.Add(ex.Message);
							}
						});

						if (bUpdAvail) m_host.MainWindow.BeginInvoke(actShowUpdateForm_UIThread);
					}
					catch (Exception ex)
					{
						bOK = false;
						lMsg.Add(ex.Message);
					}
					finally
					{
						try { if (m_slUpdateCheck != null) EndLogging(); }
						catch (Exception ex) { lMsg.Add(ex.Message); }
						if (bOK)
							lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Checked; }
						else
							lock (m_lock) { m_UpdateCheckStatus = UpdateCheckStatus.Error; }
					}
				});
				if (bOK)
				{
					try
					{
						m_slUpdateCheck = CreateUpdateCheckLogger();
						lMsg.Add("Initialising StatusLogger create: " + DebugPrint);
					}
					catch (Exception ex)
					{
						lMsg.Add("Initialising StatusLogger failed:\n" + ex.Message + "\n" + DebugPrint);
					}
					ThreadPool.QueueUserWorkItem(new WaitCallback((object o) => { actUpdateCheck(); }));
				}
				while (true)
				{
					if ((m_slUpdateCheck != null) && !ContinueWork()) break;
					lock (m_lock)
					{
						if (m_UpdateCheckStatus == UpdateCheckStatus.Checked) break;
						if (m_UpdateCheckStatus == UpdateCheckStatus.Error) break;
					}
				}
				if (m_slUpdateCheck != null) EndLogging();
				if (bOK) return;
			}
			catch (Exception ex)
			{
				bOK = false;
				lMsg.Add(ex.Message);
			}
			finally
			{
				lMsg.Insert(0, "Successful: " + bOK.ToString());
				if (bOK) PluginDebug.AddSuccess("Run updatecheck in background", 0, lMsg.ToArray());
				else PluginDebug.AddError("Run updatecheck in background", 0, lMsg.ToArray());
			}
		}

		private bool ContinueWork()
		{
			try
			{
				var c = UIThread(() => 
				{
					bool cw = false;
					lock (m_slUpdateCheck)
					{
						if (m_slUpdateCheck == null) cw = false;
						else cw = m_slUpdateCheck.ContinueWork();
					}
					return cw;
				});
				if (c != null) return (bool)c;
			}
			catch { }
			return true;
		}

		private void EndLogging()
		{
			UIThread(() => 
			{
				lock (m_slUpdateCheck)
				{
					if (m_slUpdateCheck != null) m_slUpdateCheck.EndLogging();
					m_slUpdateCheck = null;
				}
				return null; 
			});
		}

		private object UIThread(KeePassLib.Delegates.GFunc<object> a)
		{
			try
			{
				return m_bCancel.Invoke(a);
			}
			catch (Exception ex)
			{
				PluginDebug.AddError("Show SecureDesktopHint", 0, ex.Message);
			}
			return null;
		}

		private void UpdateStatusLogger()
		{
			if (!m_kpf.SecureDesktopMode) return;
			UIThread(() => {
				Tools.ShowInfo(string.Format(PluginTranslate.SecureDesktopMode, KeePass.Resources.KPRes.MasterKeyOnSecureDesktop, KeePass.Resources.KPRes.OpenDatabase));
				return null;
			});
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

		private Button m_bCancel = null;
		private IStatusLogger CreateUpdateCheckLogger()
		{
			IStatusLogger sl = StatusUtil.CreateStatusDialog(null, out m_CheckProgress,
				KeePass.Resources.KPRes.UpdateCheck, KeePass.Resources.KPRes.CheckingForUpd + "...", true, true);
			m_bCancel = (Button)Tools.GetControl("m_btnCancel", m_CheckProgress);
			if (m_bCancel != null)
			{
				int x = m_bCancel.Right;
				m_bCancel.AutoSize = true;
				m_bCancel.Text = PluginTranslate.EnterBackgroundMode;
				m_bCancel.Left = x - m_bCancel.Width;
			}
			return sl;
		}
		#endregion

		#region Check for new translations
		private bool m_bPluginLanguagesChecked = false;
		private void CheckPluginLanguages(object o)
		{
			PluginUpdateHandler.LoadPlugins(false);
			if (!PluginConfig.Active) return;
			if (m_bPluginLanguagesChecked) return;

			CheckExternalPluginUpdate();
			m_bPluginLanguagesChecked = true;
			string translations = string.Empty;
			List<OwnPluginUpdate> lPlugins = new List<OwnPluginUpdate>();
			foreach (PluginUpdate pu in PluginUpdateHandler.Plugins)
			{
				if (!PluginUpdateHandler.VersionsEqual(pu.VersionInstalled, pu.VersionAvailable)) continue;
				foreach (var t in pu.Translations)
				{
					if (t.NewTranslationAvailable || (PluginConfig.DownloadActiveLanguage && t.TranslationForCurrentLanguageAvailable))
					{
						lPlugins.Add(pu as OwnPluginUpdate);
						break;
					}
				}
			}

			if (lPlugins.Count == 0) return;
			var arrPlugins = lPlugins.ConvertAll(x => x.ToString()).ToArray();
			PluginDebug.AddInfo("Available translation updates", 0, arrPlugins);
			KeePassLib.Delegates.GAction DisplayTranslationForm = () =>
			{
				using (TranslationUpdateForm t = new TranslationUpdateForm())
				{
					t.InitEx(lPlugins);
					if (t.ShowDialog() == DialogResult.OK)
					{
						UpdatePluginTranslations(PluginConfig.DownloadActiveLanguage, t.SelectedPlugins);
					}
				}
			};
			m_host.MainWindow.BeginInvoke(DisplayTranslationForm);
		}

		private bool m_bExternalPluginInfoChecked = false;
		private void CheckExternalPluginUpdate()
		{
			if (!PluginConfig.Active) return;
			if (!PluginConfig.OneClickUpdate) return;
			if (m_bExternalPluginInfoChecked) return;
			m_bExternalPluginInfoChecked = true;
			bool bDownloadFile = false;
			if (UpdateInfoExternParser.VersionInstalled < 0)
			{
				if (PluginConfig.ExternalUpdateFileAskedForInitialDownload) return;
				PluginConfig.ExternalUpdateFileAskedForInitialDownload = true;
				bDownloadFile = Tools.AskYesNo(PluginTranslate.UpdateExternalInfoInitialDownload) == DialogResult.Yes;
				if (!bDownloadFile) return;
			}
			if (UpdateInfoExternParser.VersionInstalled >= UpdateInfoExternParser.VersionAvailable) return;
			var pu = PluginUpdateHandler.Plugins.Find(x => x is EarlyUpdateCheckUpdate) as OwnPluginUpdate;
			if (pu == null) return;
			if (!bDownloadFile) bDownloadFile = Tools.AskYesNo(PluginTranslate.UpdateExternalInfo) == DialogResult.Yes;
			if (!bDownloadFile) return;
			m_host.MainWindow.Invoke(new KeePassLib.Delegates.GAction(() => { UpdatePlugins(UpdateFlags.ExternalUpdateInfo, null); }), null);
		}
		#endregion

		#region Adjust UpdateCheckForm if required
		/// <summary>
		/// Show update indicator if plugins can be updated
		/// </summary>
		private List<Delegate> m_lEventHandlerItemActivate = null;
		private Image m_ImgApply = null;
		private Image m_ImgUnselected = null;
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
			PluginUpdateHandler.LoadPlugins(false);
			if (PluginUpdateHandler.Plugins.Count == 0) return;
			CheckExternalPluginUpdate();
			SetPluginSelectionStatus(false);
			bool bColumnAdded = false;
			m_lEventHandlerItemActivate = EventHelper.GetItemActivateHandlers(lvPlugins);
			if (m_lEventHandlerItemActivate.Count > 0)
			{
				EventHelper.RemoveItemActivateEventHandlers(lvPlugins, m_lEventHandlerItemActivate);
				lvPlugins.ItemActivate += LvPlugins_ItemActivate;
			}
			//https://github.com/mono/mono/issues/17747
			//Do NOT use ListView.SmallImageList
			if (m_ImgApply == null) m_ImgApply = (Image)KeePass.Program.Resources.GetObject("B16x16_Apply");
			if (m_ImgUnselected == null) m_ImgUnselected = m_ImgApply == null ? null : UIUtil.CreateGrayImage(m_ImgApply);
			PluginDebug.AddInfo("Installed updatable plugins", 0, PluginUpdateHandler.Plugins.ConvertAll(x => x.Name + " / " + x.Title + " / " + x.UpdateMode.ToString() + " / " + x.UpdatePossible.ToString()).ToArray());
			KeePass_Update kpu = null;
			foreach (ListViewItem item in lvPlugins.Items)
			{
				PluginDebug.AddInfo("Check plugin update status", 0, item.SubItems[0].Text, item.SubItems[1].Text);
				string sHelp = "-" + item.SubItems[0].Text + "-" + item.SubItems[1].Text + "- " + (!item.SubItems[1].Text.Contains(KeePass.Resources.KPRes.NewVersionAvailable)).ToString();
				if (!item.SubItems[1].Text.Contains(KeePass.Resources.KPRes.NewVersionAvailable)) continue;
				if (item.Index == 0 && item.Text == "KeePass")
				{
					CheckKeePassInstallType(true);
					if (PluginConfig.KeePassUpdateActive)
					{
						Version v = null;
						try { v = new Version(item.SubItems[3].Text); }
						catch { }
						if (v != null)
						{
							kpu = new KeePass_Update(v, PluginConfig.KeePassInstallType);
							if (!bColumnAdded)
							{
								lvPlugins.Columns.Add(PluginTranslate.PluginUpdate);
								lvPlugins.KeyPress += LvPlugins_KeyPress;
								bColumnAdded = true;
							}
							ListViewItem.ListViewSubItem lvsiUpdate = new ListViewItem.ListViewSubItem(item, PluginTranslate.PluginUpdate);
							lvsiUpdate.Name = "EarlyUpdateCheck_UpdateColumn_Item" + item.Index.ToString();
							lvsiUpdate.Tag = kpu;
							item.SubItems.Add(lvsiUpdate);
							kpu.Selected = true;
						}
					}
				}
				foreach (PluginUpdate upd in PluginUpdateHandler.Plugins)
				{
					if (item.SubItems[0].Text != upd.Title) continue;
					if (upd.UpdateMode == UpdateOtherPluginMode.Unknown) continue;
					if (!bColumnAdded)
					{
						lvPlugins.Columns.Add(PluginTranslate.PluginUpdate);
						lvPlugins.KeyPress += LvPlugins_KeyPress;
						bColumnAdded = true;
					}
					ListViewItem.ListViewSubItem lvsiUpdate = new ListViewItem.ListViewSubItem(item, PluginTranslate.PluginUpdate);
					lvsiUpdate.Name = "EarlyUpdateCheck_UpdateColumn_Item" + item.Index.ToString();
					lvsiUpdate.Tag = upd;
					item.SubItems.Add(lvsiUpdate);
					upd.Selected = true;
					OwnPluginUpdate opu = upd as OwnPluginUpdate;
					if (opu != null && opu.IsRenamed)
					{
						item.SubItems[0].Text += "*";
						item.ToolTipText = opu.Name + " -> " + opu.NewName;
					}
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
				ShowUpdateButton(sender as Form, true, kpu);
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

		private bool m_bActivateKeePassUpdateTab = false;
		private void CheckKeePassInstallType(bool bShowOptions)
		{
			if (PluginConfig.KeePassInstallTypeConfigured) return;
			m_bActivateKeePassUpdateTab = true;
			if (!bShowOptions) Tools.ShowInfo(PluginTranslate.KeePassUpdate_RequestInstallType);
			if (bShowOptions) Tools.ShowOptions();
		}

		private void LvPlugins_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar != (char)Keys.Space) return;
			var lv = sender as ListView;
			if (lv == null || lv.SelectedItems.Count < 0) return;
			ListViewItem lvi = lv.SelectedItems[0];
			int idx = lvi.SubItems.IndexOfKey("EarlyUpdateCheck_UpdateColumn");
			if (idx >= 0) ToggleUpdateFlag(lv, lvi.SubItems[idx].Tag as PluginUpdate);
		}

		private void LvPlugins_ItemActivate(object sender, EventArgs e)
		{
			try
			{
				var lv = sender as CustomListViewEx;
				var lvi = lv.SelectedItems[0];
				PluginUpdate upd = PluginUpdateHandler.Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				if (upd == null)
				{
					foreach (Delegate d in m_lEventHandlerItemActivate)
						d.DynamicInvoke(new object[] { sender, e });
				}
				else
				{
					string url = upd.URL;
					try { Tools.OpenUrl(url); }
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
				PluginUpdate upd = PluginUpdateHandler.Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				if (upd == null) return;
				string url = upd.URL;
				Tools.OpenUrl(url);
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
				PluginUpdate upd = PluginUpdateHandler.Plugins.Find(x => x.Title == lvi.SubItems[0].Text);
				e.Cancel = upd == null;
			}
			catch { }
		}

		private void OnUpdateCheckFormPluginMouseClick(object sender, MouseEventArgs e)
		{
			ListViewHitTestInfo info = (sender as ListView).HitTest(e.X, e.Y);

			if (info.SubItem == null || info.SubItem.Tag == null) return;
			ToggleUpdateFlag(sender as ListView, info.SubItem.Tag as PluginUpdate);
		}

		private void ToggleUpdateFlag(ListView lv, PluginUpdate upd)
		{
			if (lv == null || upd == null) return;
			upd.Selected = !upd.Selected;
			bool bAtLeast1Selected = PluginUpdateHandler.Plugins.Find(x => x.Selected == true) != null;
			if (!bAtLeast1Selected)
			{
				//Check whether KeePass itself is selected for update
				bAtLeast1Selected = lv.Items[0].SubItems[lv.Items[0].SubItems.Count - 1].Tag is KeePass_Update;
				if (bAtLeast1Selected) bAtLeast1Selected = (lv.Items[0].SubItems[lv.Items[0].SubItems.Count - 1].Tag as KeePass_Update).Selected;
			}
			if (!ShowUpdateButton(lv.FindForm(), bAtLeast1Selected, lv.Items[0].SubItems[lv.Items[0].SubItems.Count - 1].Tag))
			{
				upd.Selected = !upd.Selected;
				bUpdatePlugins_Click(lv, null);
			}
			else lv.Parent.Refresh();

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
		private bool ShowUpdateButton(Form form, bool enable, object oKeePassUpdate)
		{
			Button bUpdate = Tools.GetControl("EUCUpdateButton", form) as Button;
			if (bUpdate == null)
				try
				{
					Button bClose = Tools.GetControl("m_btnClose", form) as Button;
					bUpdate = new Button();
					bUpdate.Text = PluginTranslate.PluginUpdate;
					bUpdate.Left = 0;
					bUpdate.Top = bClose.Top;
					bUpdate.Height = bClose.Height;
					bUpdate.AutoSize = true;
					bUpdate.Name = "EUCUpdateButton";
					bUpdate.Click += bUpdatePlugins_Click;
					bClose.Parent.Controls.Add(bUpdate);
					if (PluginUpdateHandler.Shieldify)
					{
						bUpdate.Width += DpiUtil.ScaleIntX(16);
						UIUtil.SetShield(bUpdate, true);
					}
					//Get max width
					int iWidth = bUpdate.Width;
					bUpdate.Text = PluginTranslate.PluginUpdateKeePass;
					if (bUpdate.Width > iWidth) iWidth = bUpdate.Width;
					bUpdate.Text = PluginTranslate.PluginUpdateSelected;
					if (bUpdate.Width > iWidth) iWidth = bUpdate.Width;
					bUpdate.AutoSize = false;
					bUpdate.Width = iWidth;
					bUpdate.Left = bClose.Left - bUpdate.Width - 15;
				}
				catch (Exception) { }
			bUpdate = Tools.GetControl("EUCUpdateButton", form) as Button;
			if (bUpdate == null)
			{
				foreach (PluginUpdate upd in PluginUpdateHandler.Plugins)
					upd.Selected = false;
				CustomListViewEx lvPlugins = (CustomListViewEx)Tools.GetControl("m_lvInfo", form);
				lvPlugins.OwnerDraw = false;
				return false;
			}
			bUpdate.Tag = oKeePassUpdate;
			bUpdate.Enabled = enable;

			SetUpdateButtonText(bUpdate, oKeePassUpdate);
			return true;
		}

		private void SetUpdateButtonText(Button bUpdate, object oKeePassUpdate)
		{
			bool bAtLeast1PluginSelected = PluginUpdateHandler.Plugins.FindAll(x => x.Selected).Count > 0;
			if (!bUpdate.Enabled)
			{
				bUpdate.Text = PluginTranslate.PluginUpdate;
				return;
			}

			KeePass_Update kpu = oKeePassUpdate is KeePass_Update ? oKeePassUpdate as KeePass_Update : null;
			if (bAtLeast1PluginSelected && kpu != null)
			{
				bUpdate.Text = kpu.Selected ? PluginTranslate.PluginUpdate : PluginTranslate.PluginUpdateSelected;
			}
			else if (bAtLeast1PluginSelected) bUpdate.Text = PluginTranslate.PluginUpdateSelected;
			else if (kpu != null & kpu.Selected) bUpdate.Text = PluginTranslate.PluginUpdateKeePass;
			else
			{
				bUpdate.Enabled = false;
				bUpdate.Text = PluginTranslate.PluginUpdate;
			}
		}

		private void LvPlugins_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
		}

		private void LvPlugins_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			ListView lvPlugins = sender as ListView;
			e.DrawDefault = true;

			int iMaxColumns = -1;
			foreach (ListViewItem lvi in lvPlugins.Items)
			{
				if (iMaxColumns < lvi.SubItems.Count) iMaxColumns = lvi.SubItems.Count;
			}
			if (e.ColumnIndex + 1 != iMaxColumns) return;
			PluginUpdate upd = PluginUpdateHandler.Plugins.Find(x => x.Title == e.Item.SubItems[0].Text);
			if (upd == null) upd = PluginUpdateHandler.Plugins.Find(x => x.Title + "*" == e.Item.SubItems[0].Text); //* is used to indicate a rename 
			if (upd == null && e.Item.Index == 0 && e.Item.SubItems[e.ColumnIndex].Tag is KeePass_Update) upd = e.Item.SubItems[e.ColumnIndex].Tag as KeePass_Update;
			if (upd == null) return;
			e.DrawDefault = false;
			var imageRect = new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Height, e.Bounds.Height);
			e.Graphics.DrawImage(upd.Selected ? m_ImgApply : m_ImgUnselected, e.Bounds.X, e.Bounds.Y);
		}
		#endregion

		#region Update plugins
		/// <summary>
		/// Update installed plugin translations
		/// </summary>
		/// <param name="bDownloadActiveLanguage">Download translation for currently used language even if not installed yet</param>
		public void UpdatePluginTranslations(bool bDownloadActiveLanguage, List<string> lUpdateTranslations)
		{
			foreach (var upd in PluginUpdateHandler.Plugins)
				upd.Selected = (lUpdateTranslations == null) || lUpdateTranslations.Contains(upd.Name);
			bool bBackup = PluginConfig.DownloadActiveLanguage;
			PluginConfig.DownloadActiveLanguage = bDownloadActiveLanguage;

			//If called from CheckPluginLanguages, we're running in a different thread
			//Use Invoke because the IStatusLogger will attach to the KeyPromptForm within the UI thread
			m_host.MainWindow.Invoke(new KeePassLib.Delegates.GAction(() => { UpdatePlugins(UpdateFlags.Translations, null); }), null);
			PluginConfig.DownloadActiveLanguage = bBackup;
			foreach (var upd in PluginUpdateHandler.Plugins)
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
					//Use Invoke because TranslationChangedEvent might be raised and can require running in UIThread
					m_host.MainWindow.Invoke(new KeePassLib.Delegates.GAction(() =>
					{
						miInit.Invoke(p, new object[] { p, PluginUpdateHandler.LanguageIso });
					}), null);
				}
				catch (Exception ex)
				{
					PluginDebug.AddError("Refresh translation failed", 0, "Plugin: " + upd.Name, "Error: " + ex.Message);
				}
			}
			SetPluginSelectionStatus(false);
		}

		[Flags]
		internal enum UpdateFlags
		{
			None = 0,
			Plugin = 1,
			Translations = 2,
			ExternalUpdateInfo = 4,
			All = Plugin | Translations | ExternalUpdateInfo,
		}

		private void DoUpdateKeePass(KeePass_Update kpu)
		{
			if (kpu == null || !kpu.Selected) return;
			PluginDebug.AddInfo("KeePass update/download start ", DebugPrint);
			Form fUpdateLog = null;
			m_slUpdatePlugins = StatusUtil.CreateStatusDialog(GlobalWindowManager.TopWindow, out fUpdateLog, PluginTranslate.PluginUpdateCaption, string.Empty, true, true);

			bool success = false;
			string sTempPluginsFolder = PluginUpdateHandler.GetTempFolder();

			ThreadStart ts = new ThreadStart(() =>
			{
				m_slUpdatePlugins.StartLogging(PluginTranslate.PluginUpdateCaption, false);

				PluginDebug.AddInfo("Use temp folder", PluginUpdateHandler.Shieldify.ToString(), sTempPluginsFolder, DebugPrint);

				bool bOK = true;
				if (m_slUpdatePlugins != null)
					m_slUpdatePlugins.SetText(string.Format(PluginTranslate.PluginUpdating, kpu.Title), LogStatusType.Info);
				bOK = kpu.Download(sTempPluginsFolder);
				if (bOK) bOK = kpu.ProcessDownload(sTempPluginsFolder);
				kpu.Cleanup();
				success = bOK;
			});
			Thread t = new Thread(ts);
			t.IsBackground = true;
			t.Start();
			while (true && t.IsAlive)
			{
				if (!m_slUpdatePlugins.ContinueWork())
				{
					t.Abort();
					break;
				}
			}
			if (t != null && t.IsAlive) t.Abort();
			if (m_slUpdatePlugins != null)
			{
				m_slUpdatePlugins.EndLogging();
				m_slUpdatePlugins = null;
			}
			if (fUpdateLog != null) fUpdateLog.Dispose();

			//Start installation of update or open download folder (zip / portable)
			success &= kpu.Execute();

			//Restart KeePass to use new plugin versions
			PluginDebug.AddInfo("KeePass update/download finished", "Succes: " + success.ToString(), DebugPrint);
		}

		private void bUpdatePlugins_Click(object sender, EventArgs e)
		{
			KeePass_Update kpu = null;
			Button bUpdate = sender as Button;
			if (bUpdate != null && bUpdate.Tag is KeePass_Update) kpu = bUpdate.Tag as KeePass_Update;
			UpdatePlugins(UpdateFlags.All, kpu);
		}

		private void UpdatePlugins(UpdateFlags uf, KeePass_Update kpu)
		{
			UpdatePlugins(uf, false, kpu);
		}
		private void UpdatePlugins(UpdateFlags uf, bool bForceExternalPluginUpdatesDownload, KeePass_Update kpu)
		{
			if (kpu != null) DoUpdateKeePass(kpu);

			//Check whether anything needs to be done
			if (uf != UpdateFlags.ExternalUpdateInfo && PluginUpdateHandler.Plugins.FindAll(x => x.Selected).Count == 0) return;

			PluginDebug.AddInfo("UpdatePlugins start ", DebugPrint);
			Form fUpdateLog = null;
			m_slUpdatePlugins = StatusUtil.CreateStatusDialog(GlobalWindowManager.TopWindow, out fUpdateLog, PluginTranslate.PluginUpdateCaption, string.Empty, true, true);

			bool success = false;
			string sTempPluginsFolder = PluginUpdateHandler.GetTempFolder();

			ThreadStart ts = new ThreadStart(() =>
			{
				m_slUpdatePlugins.StartLogging(PluginTranslate.PluginUpdateCaption, false);

				PluginDebug.AddInfo("Use temp folder", PluginUpdateHandler.Shieldify.ToString(), sTempPluginsFolder, DebugPrint);

				//Download files
				foreach (PluginUpdate upd in PluginUpdateHandler.Plugins)
				{
					if (uf == UpdateFlags.ExternalUpdateInfo && upd is EarlyUpdateCheckUpdate)
					{
						success |= UpdatePlugin(upd, sTempPluginsFolder, uf, bForceExternalPluginUpdatesDownload);
					}
					else
					{
						if (!upd.Selected) continue;
						success |= UpdatePlugin(upd, sTempPluginsFolder, uf, false);
					}
				}
			});
			Thread t = new Thread(ts);
			t.IsBackground = true;
			t.Start();
			while (true && t.IsAlive)
			{
				if (!m_slUpdatePlugins.ContinueWork())
				{
					t.Abort();
					break;
				}
			}
			if (t != null && t.IsAlive) t.Abort();
			if (m_slUpdatePlugins != null)
			{
				m_slUpdatePlugins.EndLogging();
				m_slUpdatePlugins = null;
			}
			if (fUpdateLog != null) fUpdateLog.Dispose();

			//Move files from temp folder to plugin folder
			success &= PluginUpdateHandler.MoveAll(sTempPluginsFolder);
			foreach (var pu in PluginUpdateHandler.Plugins)
			{
				if (!pu.Selected) continue;
				if (pu is OwnPluginUpdate) (pu as OwnPluginUpdate).UpdateTranslationInfo(true);
			}
			if (success) PluginUpdateHandler.Cleanup(sTempPluginsFolder);

			//Restart KeePass to use new plugin versions
			PluginDebug.AddInfo("Update finished", "Succes: " + success.ToString(), DebugPrint);
			if (success && uf == UpdateFlags.ExternalUpdateInfo)
			{
				KeePass.Program.Config.Application.LastUpdateCheck = TimeUtil.SerializeUtc(DateTime.UtcNow.AddDays(-1));
				UpdateInfoExternParser.Init();
				PluginUpdateHandler.LoadPlugins(true);
			}

			if (success && UpdateFlagSet(uf, UpdateFlags.Plugin))
			{
				if (Tools.AskYesNo(PluginTranslate.PluginUpdateSuccess, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes)
				{
					if (m_bRestartInvoke)
						m_host.MainWindow.Invoke(new KeePassLib.Delegates.GAction(() => { Restart(); }));
					else
						Restart();
				}
			}
		}

		/// <summary>
		/// Update a single plugin
		/// </summary>
		/// <param name="upd">Update information for plugin</param>
		/// <param name="sPluginFolder">Target folder for plugin file</param>
		/// <param name="sTranslationFolder">Target folder for plugin translations</param>
		/// <param name="uf">Only download newest translations</param>
		/// <returns></returns>
		private bool UpdatePlugin(PluginUpdate upd, string sPluginFolder, UpdateFlags uf, bool bForceExternalPluginUpdatesDownload)
		{
			bool bOK = true;
			if (m_slUpdatePlugins != null)
				m_slUpdatePlugins.SetText(string.Format(PluginTranslate.PluginUpdating, upd.Title), LogStatusType.Info);
			if (UpdateFlagSet(uf, UpdateFlags.Plugin)) bOK = upd.Download(sPluginFolder);
			if (upd is OwnPluginUpdate && UpdateFlagSet(uf, UpdateFlags.Translations))
			{
				bool bTranslationsOK = (upd as OwnPluginUpdate).DownloadTranslations(sPluginFolder, PluginConfig.DownloadActiveLanguage, !UpdateFlagSet(uf, UpdateFlags.Plugin));
				if (!UpdateFlagSet(uf, UpdateFlags.Plugin)) bOK = bTranslationsOK;
			}
			if (upd is EarlyUpdateCheckUpdate && UpdateFlagSet(uf, UpdateFlags.ExternalUpdateInfo))
			{
				var euc = upd as EarlyUpdateCheckUpdate;
				bool bExtOK = euc.DownloadExternalPluginUpdates(sPluginFolder, bForceExternalPluginUpdatesDownload);
				if (uf == UpdateFlags.ExternalUpdateInfo) bOK = bExtOK;
			}
			if (bOK) bOK = upd.ProcessDownload(sPluginFolder);
			upd.Cleanup();
			return bOK;
		}

		private bool UpdateFlagSet(UpdateFlags flags, UpdateFlags flag)
		{
			return (flags & flag) == flag;
		}

		private void Restart()
		{
			PluginDebug.AddInfo("Restart started", DebugPrint);
			if (m_kpf != null)
			{
				PluginDebug.AddInfo("Closing KeyPromptForm", 0, DebugPrint);
				m_kpf.DialogResult = DialogResult.Cancel;
				if (m_kpf != null) m_kpf.Close();
				if (m_kpf != null) m_kpf.Dispose();
				Application.DoEvents();
				if (m_kpf != null) GlobalWindowManager.RemoveWindow(m_kpf);
			}
			if (m_slUpdateCheck != null)
			{
				PluginDebug.AddInfo("Closing update check progress form", 0, DebugPrint);
				EndLogging();
			}

			if (MonoWorkarounds.IsRequired(620618))
			{
				MethodInfo miSetEnabled = typeof(MonoWorkarounds).GetMethod("SetEnabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (miSetEnabled == null) PluginDebug.AddError("Could not locate MonoWorkarounds.SetEnabled", 0);
				else miSetEnabled.Invoke(null, new object[] { "620618", false });
				if (MonoWorkarounds.IsRequired(620618)) PluginDebug.AddError("Could not disable MonoWorkaround 620618");
				else PluginDebug.AddSuccess("Disabled MonoWorkaround 620618");
			}
			else PluginDebug.AddSuccess("Disabling MonoWorkaround 620618 not required");

			FieldInfo fi = m_host.MainWindow.GetType().GetField("m_bRestart", BindingFlags.NonPublic | BindingFlags.Instance);
			if (fi != null)
			{
				PluginDebug.AddInfo("Restart started, m_bRestart found", DebugPrint);
				RemoveAndBackupFormLoadPostHandlers();
				HandleMutex(true);
				fi.SetValue(m_host.MainWindow, true);
				m_bRestartTriggered = true;
				m_host.MainWindow.ProcessAppMessage((IntPtr)KeePass.Program.AppMessage.Exit, IntPtr.Zero);
				HandleMutex(false);
			}
			else PluginDebug.AddError("Restart started, m_bRestart not found" + DebugPrint);
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
				t.IsBackground = true;
				t.Start();
				return;
			}
			lStrings.Add("Action required: No");
			PluginDebug.AddInfo("Handle global mutex", 0, lStrings.ToArray());
		}

		private void SetPluginSelectionStatus(bool select)
		{
			foreach (var p in PluginUpdateHandler.Plugins) p.Selected = select;
		}
		#endregion

		#region Configuration
		private void OptionsFormShown(object sender, Tools.OptionsFormsEventArgs e)
		{
			CheckKeePassInstallType(false);
			Options options = new Options();
			options.gCheckSync.Checked = PluginConfig.Active;
			options.cbCheckSync.Checked = PluginConfig.CheckSync;
			options.gOneClickUpdate.Checked = PluginConfig.OneClickUpdate;
			options.cbDownloadCurrentTranslation.Checked = PluginConfig.DownloadActiveLanguage;
			options.UpdateExternalPluginUpdates = UpdatePlugins;
			options.cgKeePassUpdate.Checked = PluginConfig.KeePassUpdateActive;
			options.kpit = PluginConfig.KeePassInstallType;
			options.Plugin = this;
			if (m_bActivateKeePassUpdateTab) e.form.Shown += options.ActivateKeePassUpdateTab;
			Tools.AddPluginToOptionsForm(this, options);
		}

		private void OptionsFormClosed(object sender, Tools.OptionsFormsEventArgs e)
		{
			m_bActivateKeePassUpdateTab = false;
			if (e.form.DialogResult != DialogResult.OK) return;
			bool shown = false;
			Options options = (Options)Tools.GetPluginFromOptions(this, out shown);
			if (!shown) return;
			PluginConfig.Active = options.gCheckSync.Checked;
			PluginConfig.CheckSync = options.cbCheckSync.Checked;
			PluginConfig.OneClickUpdate = options.gOneClickUpdate.Checked;
			PluginConfig.DownloadActiveLanguage = options.cbDownloadCurrentTranslation.Checked;
			PluginConfig.KeePassUpdateActive = options.cgKeePassUpdate.Checked;
			PluginConfig.KeePassInstallType = options.kpit;
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
				lock (m_lock)
				{
					result = string.Format(result, m_bRestartInvoke.ToString(), m_UpdateCheckStatus.ToString(), KeePass.Program.MainForm.UIIsInteractionBlocked().ToString());
				}
				return result;
			}
		}
	}
}