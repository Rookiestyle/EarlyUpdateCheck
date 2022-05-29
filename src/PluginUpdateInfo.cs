using KeePass.Plugins;
using KeePass.Util;
using KeePassLib.Delegates;
using KeePassLib.Utility;
using PluginTools;
using PluginTranslation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace EarlyUpdateCheck
{
	internal static class PluginUpdateHandler
	{
		internal static List<PluginUpdate> Plugins = new List<PluginUpdate>();
		internal static bool CheckTranslations
		{
			get
			{
				return !string.IsNullOrEmpty(m_sLastUpdateCheck) && KeePass.Program.Config.Application.Start.CheckForUpdate
					&& !KeePass.Program.Config.Application.Start.MinimizedAndLocked
					&& m_sLastUpdateCheck != KeePass.Program.Config.Application.LastUpdateCheck;
			}
		}
		internal static string LanguageIso = string.Empty;

		internal static bool Shieldify
		{
			get
			{
				if (m_bShieldify.HasValue) return m_bShieldify.Value;
				CheckShieldify();
				return m_bShieldify.Value;
			}
		}

		internal static string PluginsFolder { get; private set; }
		internal static string PluginsTranslationsFolder { get; private set; }

		internal static string GetTempFolder()
		{
			string sTempPluginsFolder = string.Empty;
			try
			{
				sTempPluginsFolder = UrlUtil.GetTempPath();
				sTempPluginsFolder = UrlUtil.EnsureTerminatingSeparator(sTempPluginsFolder, false);
				sTempPluginsFolder += Path.GetRandomFileName();
				sTempPluginsFolder = UrlUtil.EnsureTerminatingSeparator(sTempPluginsFolder, false);
				sTempPluginsFolder = UrlUtil.GetShortestAbsolutePath(sTempPluginsFolder);
				Directory.CreateDirectory(sTempPluginsFolder);
			}
			catch { }
			return sTempPluginsFolder;
		}

		internal static void Init()
		{
			PluginsFolder = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), true, true);
			PluginsFolder = UrlUtil.EnsureTerminatingSeparator(PluginsFolder + KeePass.App.AppDefs.PluginsDir, false);
			PluginsTranslationsFolder = UrlUtil.EnsureTerminatingSeparator(PluginsFolder + "Translations", false);
			m_sLastUpdateCheck = KeePass.Program.Config.Application.LastUpdateCheck;
			List<string> lMsg = new List<string>();
			lMsg.Add("Plugins folder: " + PluginsFolder);
			lMsg.Add("Plugins translation folder: " + PluginsTranslationsFolder);
			lMsg.Add("Shieldify: " + Shieldify.ToString());
			lMsg.Add("Last update check: " + m_sLastUpdateCheck);
			PluginDebug.AddInfo("PluginUpdateHandler initialized", 0, lMsg.ToArray());
		}

		private static bool m_bPluginsLoaded = false;
		public static bool PluginsLoaded { get { return m_bPluginsLoaded || Plugins.Count > 0; } }
		internal static void LoadPlugins(bool bReload)
		{
			if (PluginsLoaded && !bReload) return;
			lock (Plugins) //Might be called in multiple threads, ensure a plugin is listed only once
			{
				m_bPluginsLoaded = false;
				Plugins.Clear();
				List<PluginUpdate> lPlugins = new List<PluginUpdate>();
				if (m_Plugins.Count == 0) m_Plugins = Tools.GetLoadedPluginsName();
				if (Plugins.Count > 0)
				{
					m_bPluginsLoaded = true;
					return; //Might have been filled from different thread meanwhile
				}
				List<string> lPluginnames = new List<string>();
				foreach (string sPlugin in m_Plugins.Keys)
				{
					try
					{
						string s = sPlugin.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[0];
						Plugin p = (Plugin)Tools.GetPluginInstance(s);
						if (p == null) continue;

						AssemblyCompanyAttribute[] comp = (AssemblyCompanyAttribute[])p.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
						bool bOwnPlugin = string.Compare("rookiestyle", comp[0].Company, StringComparison.InvariantCultureIgnoreCase) == 0;

						PluginUpdate pu = null;
						if (bOwnPlugin) pu = OwnPluginUpdate.Factory(p.GetType().Namespace);
						else
						{
							try { pu = OtherPluginUpdate.Factory(p.GetType().Namespace); /* new OtherPluginUpdate(p.GetType().Namespace); */ } catch { }
						}
						if (pu != null && Plugins.Find(x => x.Name == pu.Name) == null)
						{
							lPlugins.Add(pu);
							if (pu.UpdatePossible) lPluginnames.Add(pu.Name);
						}
					}
					catch (Exception ex) { PluginDebug.AddError(ex.Message, 0); }
				}
				Plugins.Clear();
				Plugins.AddRange(lPlugins); //Many plugins in foreach => foreach might not be finished when update check form is shown
				PluginDebug.AddInfo("Installed updatable plugins", 0, lPluginnames.ToArray());
				m_bPluginsLoaded = true;
			}
		}

		internal static bool VersionsEqual(Version vA, Version vB)
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

		internal static bool MoveAll(string sTempFolder)
		{
			string sTargetFolder = PluginUpdateHandler.PluginsFolder;
			if (Shieldify) return MoveAllShieldified(sTempFolder, sTargetFolder, false);
			else return MoveAllNonShieldified(sTempFolder, sTargetFolder);
		}

		private static bool MoveAllNonShieldified(string sTempFolder, string sTargetFolder)
		{
			bool bSuccess = true;
			List<string> lFiles = UrlUtil.GetFilePaths(sTempFolder, "*", SearchOption.AllDirectories);
			List<string> lMsg = new List<string>();

			//dummy file name when move instead of delete is required
			//simply pverwrite this file over and over again if reuired
			string sTemp = GetTempFolder() + "old_plugin.dmy";

			foreach (string sFile in m_lFilesDelete)
			{
				try	{ File.Delete(sFile);	}
				catch (Exception ex) 
				{ 
					lMsg.Add(ex.Message);
					//In case of dll files try moving it to a temp folder and delete them from temp
					//cf. https://github.com/Rookiestyle/EarlyUpdateCheck/issues/37
					try
					{
						File.Delete(sTemp);
						File.Move(sFile, sTemp);
					}
					catch (Exception ex2) { lMsg.Add(ex2.Message); }
				}
			}

			foreach (string sFile in lFiles)
			{
				string sTargetFile = sFile.Replace(sTempFolder, sTargetFolder);
				try
				{
					string sFolder = UrlUtil.GetFileDirectory(sTargetFile, true, true);
					//Create target folder if required
					//CreateDirectory internally checks for existence
					//Thus, no need to check in our code
					Directory.CreateDirectory(sFolder);
					File.Copy(sFile, sTargetFile, true); 
				}
				catch (Exception ex)
				{
					bSuccess = false;
					lMsg.Add(ex.Message);
				}
			}
			if (!bSuccess)
			{
				PluginDebug.AddError("Error moving files", 0, lMsg.ToArray());
				if (WinUtil.IsAtLeastWindowsVista) return MoveAllShieldified(sTempFolder, sTargetFolder, true);
				if (Tools.AskYesNo(PluginTranslate.PluginUpdateFailed, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes)
				{
					System.Diagnostics.Process.Start(sTempFolder);
				}
			}

			m_lFilesDelete.Clear(); //Clear list after MovAllShieldified was tried as well

			return bSuccess;
		}

		private static bool MoveAllShieldified(string sTempFolder, string sTargetFolder, bool bOnlyTryShieldify)
		{
			bool bSuccess = false;
			bool bOpenTempFolder = false;
			GFunc<DialogResult> f = new GFunc<DialogResult>(() =>
			{
				return Tools.AskYesNo(PluginTranslate.TryUAC, PluginTranslate.PluginUpdateCaption);
			});

			if (WinUtil.IsAtLeastWindowsVista && (NativeMethods.ShieldifyNativeDialog(DialogResult.Yes, f) == DialogResult.Yes))
			{
				if (m_lFilesDelete.Count > 0)
				{
					int iDeleted = FileCopier.DeleteFiles(m_lFilesDelete.ToArray());
					if (iDeleted == 0)
					{
						m_lFilesDelete.Clear();
					}
					else
					{
						//In case of dll files try moving it to a temp folder and delete them from temp
						//cf. https://github.com/Rookiestyle/EarlyUpdateCheck/issues/37
						if (m_lFilesDelete.Count > 0 && FileCopier.MoveFilesToTemp(m_lFilesDelete.ToArray()) <= 0) m_lFilesDelete.Clear();
					}
				}

				bSuccess = FileCopier.CopyFiles(sTempFolder, sTargetFolder);
				if (!bSuccess) bOpenTempFolder = Tools.AskYesNo(PluginTranslate.PluginUpdateFailed, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes;
			}
			else if (!bOnlyTryShieldify) bOpenTempFolder = Tools.AskYesNo(PluginTranslate.OpenTempFolder, PluginTranslate.PluginUpdateCaption) == DialogResult.Yes;

			if (bOpenTempFolder) System.Diagnostics.Process.Start(sTempFolder);

			return bSuccess;
		}

		internal static void Cleanup(string sTempPluginsFolder)
		{
			try { Directory.Delete(sTempPluginsFolder, true); } catch { }
		}

		private static List<string> m_lFilesDelete = new List<string>();

		internal static void DeleteSpecialFile(string sFile)
		{
			DeleteSpecialFile(sFile, true);
		}

		internal static void DeleteSpecialFile(string sFile, bool bAddDebugInfo)
		{
			if (m_lFilesDelete.Contains(sFile)) return;
			m_lFilesDelete.Add(sFile);
			if (bAddDebugInfo) PluginDebug.AddInfo("Remove old plugin file :" + sFile, 0);
		}

		private static Dictionary<string, Version> m_Plugins = new Dictionary<string, Version>();
		private static string m_sLastUpdateCheck = string.Empty;
		private static bool? m_bShieldify;
		private static string EnsureNonNull(string v)
		{
			if (v == null) return string.Empty;
			return v;
		}

		private static void CheckShieldify()
		{
			List<string> lShieldify = new List<string>();
			try
			{
				m_bShieldify = false;
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
				m_bShieldify = sKP.StartsWith(sPF86) || sKP.StartsWith(sPF) || sKP.StartsWith(sPF86_2);
				lShieldify.Add("KeePass folder inside ProgramFiles(x86): " + sKP.StartsWith(sPF86));
				lShieldify.Add("KeePass folder inside Environment.SpecialFolder.ProgramFilesX86: " + sKP.StartsWith(sPF86_2));
				lShieldify.Add("KeePass folder inside Environment.SpecialFolder.ProgramFiles: " + sKP.StartsWith(sPF));
				return;
			}
			catch (Exception ex) { lShieldify.Add("Exception: " + ex.Message); return; }
			finally
			{
				lShieldify.Insert(0, "Shieldify: " + m_bShieldify.ToString());
				PluginDebug.AddInfo("Check Shieldify", 0, lShieldify.ToArray());
			}
		}
	}

	internal abstract class PluginUpdate
	{
		internal struct PluginUpdateInfo
		{
			internal Version PluginVersion;
			internal string PluginFile;
		}
		private static Dictionary<string, PluginUpdateInfo> m_Plugins = new Dictionary<string, PluginUpdateInfo>();
		internal string Name { get; private set; }

		protected string _title;
		internal string Title { get { return _title; } private set { SetTitle(value); } }
		internal Version VersionInstalled { get; private set; }
		internal Version VersionAvailable { get; set; }
		internal List<TranslationVersionCheck> Translations { get; private set; }
		internal string URL { get; set; }	//URL for plugin homepage
		internal string PluginUpdateURL { get; set; }	//direct link to download newest version
		public string VersionURL { get; private set; } //string to version file, used for translation checks
		internal UpdateOtherPluginMode UpdateMode { get; set; }
		internal bool AllowVersionStripping { get; set; }
		internal string PluginFile { get; private set; }
		internal bool Selected;

		internal bool Ignore = false;

		protected void SetVersionInstalled(Version vInstalled)
        {
			VersionInstalled = vInstalled;
        }

		public override string ToString()
		{
			return Title + " - " + UpdateMode.ToString() + " (" + VersionInstalled.ToString() + " / " + VersionAvailable.ToString() + ")";
		}

		internal bool UpdatePossible { get { return !Ignore && !string.IsNullOrEmpty(PluginUpdateURL) && UpdateMode != UpdateOtherPluginMode.Unknown; } }

		protected List<string> m_lDownloaded = new List<string>();

		protected virtual void SetTitle(string sTitle)
        {
			_title = sTitle;
        }

		internal PluginUpdate(string PluginName)
		{
			if (m_Plugins.Count == 0) m_Plugins = GetLoadedPluginsName();
			PluginName = PluginName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[0];
			Plugin p = (Plugin)Tools.GetPluginInstance(PluginName);
			if (p == null) throw new ArgumentException("Invalid plugin: " + PluginName);
			AssemblyCompanyAttribute[] comp = (AssemblyCompanyAttribute[])p.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			bool bOwnPlugin = string.Compare("rookiestyle", comp[0].Company, StringComparison.InvariantCultureIgnoreCase) == 0;

			AssemblyTitleAttribute[] title = (AssemblyTitleAttribute[])p.GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (title.Length != 1) throw new ArgumentException("Plugin title missing");
			Name = p.GetType().Namespace;
			Title = title[0].Title;
			VersionURL = p.UpdateUrl;

			PluginUpdateInfo pui;
			if (!m_Plugins.TryGetValue(p.ToString(), out pui))
			{
				pui.PluginVersion = p.GetType().Assembly.GetName().Version;
				pui.PluginFile = string.Empty;
			}
			VersionInstalled = pui.PluginVersion;
			VersionAvailable = new Version(0, 0);
			PluginFile = pui.PluginFile;

			AllowVersionStripping = false;
			UpdateMode = UpdateOtherPluginMode.Unknown;

			URL = string.Empty;
			PluginUpdateURL = string.Empty;

			Translations = new List<TranslationVersionCheck>();
		}

		private static Dictionary<string, PluginUpdateInfo> GetLoadedPluginsName()
		{
			Dictionary<string, PluginUpdateInfo> dPlugins = new Dictionary<string, PluginUpdateInfo>();
			BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
			try
			{
				var PluginManager = Tools.GetField("m_pluginManager", KeePass.Program.MainForm);
				var PluginList = Tools.GetField("m_vPlugins", PluginManager);
				MethodInfo IteratorMethod = PluginList.GetType().GetMethod("System.Collections.Generic.IEnumerable<T>.GetEnumerator", bf);
				IEnumerator<object> PluginIterator = (IEnumerator<object>)(IteratorMethod.Invoke(PluginList, null));
				while (PluginIterator.MoveNext())
				{
					object result = Tools.GetField("m_strDisplayFilePath", PluginIterator.Current);
					if (result == null) result = Tools.GetField("m_strFilePath", PluginIterator.Current);
					string sFile = string.Empty;
					if (result != null) sFile = (string)result;

					result = Tools.GetField("m_pluginInterface", PluginIterator.Current);
					var x = result.GetType().Assembly;
					object[] v = x.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
					Version ver = null;
					if ((v != null) && (v.Length > 0))
						ver = new Version(((AssemblyFileVersionAttribute)v[0]).Version);
					else
						ver = result.GetType().Assembly.GetName().Version;
					if (ver.Build < 0) ver = new Version(ver.Major, ver.Minor, 0, 0);
					if (ver.Revision < 0) ver = new Version(ver.Major, ver.Minor, ver.Build, 0);
					dPlugins[result.GetType().FullName] = new PluginUpdateInfo() { PluginVersion = ver, PluginFile = sFile };
				}
			}
			catch (Exception) { }
			return dPlugins;
		}

		internal virtual bool Download(string sTempFolder)
		{
			if (string.IsNullOrEmpty(sTempFolder)) return false;
			string sFile = MergeInVersion(true);
			sTempFolder = MergeInPluginFolder(sTempFolder);
			return DownloadFile(sFile, sTempFolder);
		}

		protected string MergeInPluginFolder(string sTempFolder)
		{
			if (string.IsNullOrEmpty(PluginFile)) return sTempFolder;
			string s = UrlUtil.GetFileDirectory(PluginFile, true, true);
			s = s.Substring(PluginUpdateHandler.PluginsFolder.Length);
			return UrlUtil.EnsureTerminatingSeparator(sTempFolder, sTempFolder.Contains("//")) + s;
		}

		protected virtual string MergeInVersion(bool bUseAvailableVersion)
		{
			string sResult = PluginUpdateURL;
			Version v = bUseAvailableVersion ? VersionAvailable : VersionInstalled;
			//VersionAvailable = new Version(0, 4, 0, 1);
			//sResult = "a{MAJOR}-{.MINOR}-{.BUILD}-{.revision}b";
			bool bStripping = AllowVersionStripping && v.Revision < 1;
			Regex r = new Regex(@"\{([^}]*)REVISION\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + v.Revision.ToString());

			bStripping &= v.Build < 1;
			r = new Regex(@"\{([^}]*)BUILD\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else
			{
				sResult = r.Replace(sResult, "$1\n" + v.Build.ToString());
				//Special handling for plugins that ALWAYS include the build
				r = new Regex(@"\{([^}]*)BUILD!\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				sResult = r.Replace(sResult, "$1\n0");
			}

			bStripping &= v.Minor < 1;
			r = new Regex(@"\{([^}]*)MINOR\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + v.Minor.ToString());
			

			bStripping &= v.Major < 1;
			r = new Regex(@"\{([^}]*)MAJOR\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + v.Major.ToString());

			sResult = sResult.Replace("\n", string.Empty);
			
			return sResult;
		}

		protected bool DownloadFile(string sFile, string sTargetFolder)
		{
			const int MAXATTEMPTS = 3;
			int iAttempts = 0;
			string sSource = GetShortestAbsolutePath(sFile);
			while (iAttempts++ < MAXATTEMPTS)
			{
				try
				{
					string sTarget = GetShortestAbsolutePath(sTargetFolder + UrlUtil.GetFileName(sSource));
					KeePassLib.Serialization.IOConnectionInfo ioc = KeePassLib.Serialization.IOConnectionInfo.FromPath(sSource);
					Stream s = KeePassLib.Serialization.IOConnection.OpenRead(ioc);
					if (s == null) throw new InvalidOperationException();
					MemoryStream ms = new MemoryStream();
					MemUtil.CopyStream(s, ms);
					s.Close();
					byte[] pb = ms.ToArray();
					ms.Close();
					//Create target folder, Directory.CreateDirectory internally checks for existence of the folder
					Directory.CreateDirectory(sTargetFolder);
					File.WriteAllBytes(sTarget, pb);
					m_lDownloaded.Add(sTarget);
					PluginDebug.AddInfo("Download success", 0, "Source: " + sSource, "Target: " + sTargetFolder, "Download attempt: " + iAttempts.ToString());
					return true;
				}
				catch (Exception ex)
				{
					PluginDebug.AddInfo("Download failed", 0, "Source: " + sSource, "Target: " + sTargetFolder, "Download attempt: " + iAttempts.ToString(), ex.Message);

					System.Net.WebException exWeb = ex as System.Net.WebException;
					if (exWeb == null) continue;
					System.Net.HttpWebResponse wrResponse = exWeb.Response as System.Net.HttpWebResponse;
					if ((wrResponse == null) || (wrResponse.StatusCode != System.Net.HttpStatusCode.NotFound)) continue;
					iAttempts = MAXATTEMPTS;
				}
			}
			return false;
		}

		private string GetShortestAbsolutePath(string sFile)
		{
			string sAbsolute = sFile;
			if (UrlUtil.IsUncPath(sFile)) sAbsolute = UrlUtil.GetShortestAbsolutePath(sFile);
			else
			{
				sAbsolute = UrlUtil.GetShortestAbsolutePath((sAbsolute.Contains("\\") ? "\\\\" : "//") + sFile);
				sAbsolute = sAbsolute.Substring(2, sAbsolute.Length - 2);
			}
			if (sFile != sAbsolute)
				PluginDebug.AddInfo("Shorten filename", 0, "Old: " + sFile, "New: " + sAbsolute);
			return sAbsolute;
		}

		internal virtual bool ProcessDownload(string sTargetFolder)
		{
			//Added for plugin specific file movements (future)
			return true;
		}

		internal virtual void Cleanup()
		{
			m_lDownloaded.Clear();
			//Added for plugin specific file cleanups (future)
		}
	}

	internal class KeePass_Update: PluginUpdate
    {
		internal enum KeePassInstallType
        {
			Setup,
			MSI,
			Portable
        }

		private KeePassInstallType _kpit;
		internal KeePass_Update(Version vAvailable, KeePassInstallType kpit) : base("EarlyUpdateCheck")
        {
			SetVersionInstalled(Tools.KeePassVersion);
			VersionAvailable = vAvailable;
			AllowVersionStripping = true;
			_kpit = kpit;
			PluginUpdateURL = "https://sourceforge.net/projects/keepass/files/KeePass%202.x/{MAJOR}{.MINOR}{.BUILD}/KeePass-{MAJOR}{.MINOR}{.BUILD}";
			if (_kpit == KeePassInstallType.Setup) PluginUpdateURL += "-Setup.exe";
			else if (_kpit == KeePassInstallType.MSI) PluginUpdateURL += ".msi";
			else PluginUpdateURL += ".zip";
			UpdateMode = UpdateOtherPluginMode.ZipExtractAll;
		}

        protected override void SetTitle(string sTitle)
        {
            base.SetTitle(sTitle);
			_title = "KeePass";
        }

        private string sDownloaded = string.Empty;
		internal override bool ProcessDownload(string sTargetFolder)
		{
			if (!base.ProcessDownload(sTargetFolder)) return false;

			if (_kpit != KeePassInstallType.Portable)
			{
				sDownloaded = m_lDownloaded[0];
				return true;
			}
			string sSourceZip = m_lDownloaded[0];
			byte[] pbZip = File.ReadAllBytes(sSourceZip);
			File.Delete(sSourceZip);
			using (MemoryStream ms = new MemoryStream())
			{
				ms.Write(pbZip, 0, pbZip.Length);
				ms.Position = 0;
				sDownloaded = UrlUtil.GetFileDirectory(sSourceZip, true, true);
				using (Ionic.Zip.ZipFile z = Ionic.Zip.ZipFile.Read(ms))
				{
					z.ExtractAll(sDownloaded);
					PluginDebug.AddInfo("Extracted zip file", 0, UrlUtil.GetFileName(sSourceZip), sDownloaded);
				}
				return true;
			}
		}

		private bool DoUpdate()
        {
			if (_kpit == KeePassInstallType.Portable)
            {
				Tools.ShowInfo(PluginTranslate.KeePassUpdate_InstallZip);
				return true;
            }
			return Tools.AskYesNo(string.Format(PluginTranslate.KeePassUpdate_InstallSetupOrMsi, KeePass.Resources.KPRes.Yes, KeePass.Resources.KPRes.No)) == DialogResult.Yes;
        }

		internal bool Execute()
        {
			try
			{
				string sURL = sDownloaded;
				if (!DoUpdate()) sURL = UrlUtil.GetFileDirectory(sDownloaded, true, true);
				Tools.OpenUrl(sURL);
				return true;
			}
			catch { return false; }
		}
    }

	internal class OwnPluginUpdate : PluginUpdate
	{
		internal bool IsRenamed { get; private set; }
		internal string NewName { get; private set; }
		internal static PluginUpdate Factory(string sPluginNamespace)
		{
			switch (sPluginNamespace.ToLowerInvariant())
			{
				case "earlyupdatecheck": return new EarlyUpdateCheckUpdate();
				default: return new OwnPluginUpdate(sPluginNamespace);
			}
			throw new NotImplementedException();
		}

		protected OwnPluginUpdate(string PluginName) : base(PluginName)
		{
			NewName = string.Empty;
			GetOwnPluginURL();
			AllowVersionStripping = false;
			UpdateMode = UpdateOtherPluginMode.PlgxDirect;

			PluginUpdateURL = URL + "latest/download/" + Name + ".plgx";
			UpdateTranslationInfo(false);
		}

		internal bool DownloadTranslations(string sTempFolder, bool bDownloadCurrentLangue, bool bCheckVersion)
		{
			string sTempTranslationsFolder = UrlUtil.EnsureTerminatingSeparator(sTempFolder + "Translations", false);
			Directory.CreateDirectory(sTempTranslationsFolder);

			bool bOK = true;
			foreach (var t in Translations)
			{
				if (bCheckVersion && !t.NewTranslationAvailable && !(bDownloadCurrentLangue && t.TranslationForCurrentLanguageAvailable)) continue;
				string sFile = URL.Replace("github.com", "raw.githubusercontent.com") + "../master/Translations/" + t.LangugageFile;
				if (DownloadFile(sFile, sTempTranslationsFolder)) continue;
				bOK = false;
				Tools.ShowError(string.Format(PluginTranslate.PluginTranslationUpdateFailed, Title, t.LangugageFile), PluginTranslate.PluginUpdateCaption);
			}
			return bOK;
		}

		internal void UpdateTranslationInfo(bool bOnlyInstalled)
		{
			if (!bOnlyInstalled) Translations.Clear();
			
			UpdateInstalledTranslations();
			if (!bOnlyInstalled) UpdateAvailableTranslations();

			if (PluginDebug.DebugMode)
			{
				List<string> lT = new List<string>();
				foreach (var t in Translations)
					lT.Add(t.ToString());
				PluginDebug.AddInfo("Plugin languages - " + Name, 0, lT.ToArray());
			}
		}

		private void UpdateInstalledTranslations()
		{
			try
			{
				Regex r = new Regex(@"\<TranslationVersion\>(\d+)\<\/TranslationVersion\>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
				List<string> lTranslationsInstalled = UrlUtil.GetFilePaths(PluginUpdateHandler.PluginsTranslationsFolder, Name + "*.language.xml", SearchOption.TopDirectoryOnly);
				foreach (string lang in lTranslationsInstalled)
				{
					string translation = File.Exists(lang) ? File.ReadAllText(lang) : string.Empty;
					Match m = r.Match(translation);
					if (m.Groups.Count != 2) continue;
					long lVerInstalled = 0;
					if (!long.TryParse(m.Groups[1].Value, out lVerInstalled)) continue;
					string sLang = UrlUtil.GetFileName(lang);
					TranslationVersionCheck tvc = Translations.Find(x => x.LangugageFile == sLang);
					if (tvc == null)
					{
						tvc = new TranslationVersionCheck() { LangugageFile = sLang };
						Translations.Add(tvc);
					}
					tvc.Installed = lVerInstalled;
				}
			}
			catch (Exception) { }
		}

		protected Dictionary<string, List<UpdateComponentInfo>> m_dUpdateInfo = new Dictionary<string, List<UpdateComponentInfo>>();
		private void UpdateAvailableTranslations()
		{
			m_dUpdateInfo.Clear();
			Dictionary<string, long> dResult = new Dictionary<string, long>();
			Type t = typeof(KeePass.Program).Assembly.GetType("KeePass.Util.UpdateCheckEx");
			if (t == null)
			{
				PluginDebug.AddError("Could not locate class 'UpdateCheckEx'", 0);
				return;
			}
			MethodInfo mi = t.GetMethod("DownloadInfoFiles", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Static);
			if (mi == null)
			{
				PluginDebug.AddError("Could not locate method 'DownloadInfoFiles'", 0);
				return;
			}

			if (string.IsNullOrEmpty(VersionURL))
			{
				PluginDebug.AddError("Could not read plugin update url", 0);
				return;
			}

			m_dUpdateInfo = mi.Invoke(null, new object[] { new List<string>() { VersionURL }, null }) as Dictionary<string, List<UpdateComponentInfo>>;

			List<string> lTranslationsNew = new List<string>();
			string[] cSplit = new string[] { "!", "!!!" };
			CheckRenamed(m_dUpdateInfo, cSplit);
			foreach (KeyValuePair<string, List<UpdateComponentInfo>> kvp in m_dUpdateInfo)
			{
				if (kvp.Value == null) continue;
				Version vCheck = null;
				foreach (UpdateComponentInfo uci in kvp.Value)
				{
					//Github: <Plugin>!<language identifier>
					string[] sParts = uci.Name.Split(cSplit, StringSplitOptions.RemoveEmptyEntries);
					if (sParts.Length == 1 && Title == sParts[0])
					{
						vCheck = new Version(StrUtil.VersionToString(uci.VerAvailable, 2));
						if (VersionAvailableIsUnknown()) VersionAvailable = vCheck;
					}
					if (!IsRenamed && vCheck != null && !PluginUpdateHandler.VersionsEqual(VersionInstalled, vCheck)) return; //Different version might require different translation files
					if (sParts.Length != 2 || sParts[0] != Title) continue;
					long lVer = 0;
					if (!long.TryParse(StrUtil.VersionToString(uci.VerAvailable), out lVer)) continue;
					string sLang = Name + "." + sParts[1].ToLowerInvariant() + ".language.xml";
					TranslationVersionCheck tvc = Translations.Find(x => x.LangugageFile == sLang);
					if (tvc == null)
					{
						tvc = new TranslationVersionCheck() { LangugageFile = sLang };
						Translations.Add(tvc);
					}
					tvc.Available = lVer;
				}
				if (IsRenamed) ProcessRename();
			}
		}

		private void CheckRenamed(Dictionary<string, List<UpdateComponentInfo>> m_dUpdateInfo, string[] cSplit)
		{
			if (IsRenamed) return;
			if (m_dUpdateInfo == null) return;
			foreach (var v in m_dUpdateInfo)
			{
				//Check for renaming
				if (v.Value == null) continue;
				var uciRename = v.Value.Find(x => x.Name.StartsWith("RenamedTo!"));
				if (uciRename == null) continue;
				string[] sParts = uciRename.Name.Split(cSplit, StringSplitOptions.RemoveEmptyEntries);
				if (sParts.Length != 2) continue;
				if (Title == sParts[1]) return;
				IsRenamed = true;
				NewName = sParts[1];
				PluginDebug.AddInfo("New plugin name detected", 0, "Old: " + Name, "New: " + NewName);
				return;
			}
		}

		/// <summary>
		/// All files related to the previous version of the plugin
		/// Only filled if plugin was renamed
		/// </summary>
		private List<string> m_lOldPluginFiles = new List<string>();
		private void ProcessRename()
		{
			List<string> lMsg = new List<string>();
			
			lMsg.Add("Plugin update URL - old: " + PluginUpdateURL);
			Regex reg = new Regex(@"\W");
			string sNewNameCleaned = reg.Replace(NewName, string.Empty);
			PluginUpdateURL = PluginUpdateURL.Replace(sNewNameCleaned.ToLowerInvariant() + "/", sNewNameCleaned.ToLowerInvariant() + "/");
			PluginUpdateURL = PluginUpdateURL.Replace(Name, sNewNameCleaned);
			lMsg.Add("Plugin update URL - new: " + PluginUpdateURL);

			lMsg.Add("Plugin URL - old: " + URL);
			URL = URL.Replace(Name.ToLowerInvariant(), sNewNameCleaned.ToLowerInvariant());
			lMsg.Add("Plugin URL - new: " + URL);

			foreach (var t in Translations)
			{
				lMsg.Add("Translation file - old: " + t.LangugageFile);
				m_lOldPluginFiles.Add(PluginUpdateHandler.PluginsTranslationsFolder + t.LangugageFile);
				//PluginUpdateHandler.DeleteSpecialFile(PluginUpdateHandler.PluginsTranslationsFolder + t.LangugageFile, false);
				t.LangugageFile = t.LangugageFile.Replace(Name, sNewNameCleaned);
				lMsg.Add("Translation file - new: " + t.LangugageFile);

				//Decrease installed version to ensure the new file is downloaded
				t.Installed--;
			}
			m_lOldPluginFiles.Add(PluginFile);
			//PluginUpdateHandler.DeleteSpecialFile(PluginFile, false);

			PluginDebug.AddInfo("Process new plugin name", 0, lMsg.ToArray());
		}

        internal override bool ProcessDownload(string sTargetFolder)
        {
			if (!IsRenamed) return true;
			foreach (var f in m_lOldPluginFiles)
				PluginUpdateHandler.DeleteSpecialFile(f);
			return true;
        }

        private bool VersionAvailableIsUnknown()
		{
			if (VersionAvailable.Major > 0) return false;
			if (VersionAvailable.Minor > 0) return false;
			if (VersionAvailable.Build > 0) return false;
			if (VersionAvailable.Revision > 0) return false;
			return true;
		}

		private void GetOwnPluginURL()
		{
			try
			{
				Plugin p = (Plugin)Tools.GetPluginInstance(Name);
				Type tools = p.GetType().Assembly.GetType("PluginTools.Tools");
				if (tools == null) return;
				FieldInfo fURL = tools.GetField("PluginURL");
				if (fURL == null) return;
				URL = (string)fURL.GetValue(p);
				if (URL == null) URL = string.Empty;
				URL = UrlUtil.EnsureTerminatingSeparator(URL, true) + "releases/";
			}
			catch { URL = Tools.PluginURL.ToLowerInvariant().Replace("earlyupdatecheck", Name.ToLowerInvariant()); }
		}
	}

	internal class EarlyUpdateCheckUpdate : OwnPluginUpdate
	{
		internal EarlyUpdateCheckUpdate() : base("EarlyUpdateCheck")
		{
			foreach(var l in m_dUpdateInfo)
			{
				var u = l.Value.Find(x => x.Name == "ExternalPluginUpdates");
				if (u != null)
				{
					var v = new Version(StrUtil.VersionToString(u.VerAvailable, 2));
					UpdateInfoExternParser.VersionAvailable = v.Major;
					break;
				}
			}
		}

		internal bool DownloadExternalPluginUpdates(string sTempFolder, bool bForceDownload)
		{
			if (!bForceDownload 
				&& (UpdateInfoExternParser.VersionInstalled < 0 || UpdateInfoExternParser.VersionInstalled >= UpdateInfoExternParser.VersionAvailable)) return true;
			Directory.CreateDirectory(sTempFolder);
			string sFile = URL.Replace("github.com", "raw.githubusercontent.com") + "../master/ExternalPluginUpdates/ExternalPluginUpdates.xml";
			return DownloadFile(sFile, sTempFolder);
		}
	}

	internal class OtherPluginUpdate : PluginUpdate
	{
		protected OtherPluginUpdate(string PluginName) : base(PluginName)
		{
			UpdateInfoExtern uie;
			if (!UpdateInfoExternParser.Get(Title, out uie) && !UpdateInfoExternParser.Get(Name, out uie)) throw new ArgumentException("No update information available for " + PluginName);

			URL = uie.PluginURL;
			PluginUpdateURL = uie.PluginUpdateURL;
			UpdateMode = uie.UpdateMode;
			AllowVersionStripping = uie.AllowVersionStripping;
			Ignore = uie.Ignore;
		}

		internal override bool ProcessDownload(string sTargetFolder)
		{
			if (!PreProcessDownload(sTargetFolder)) return false;
			bool bOK = base.ProcessDownload(sTargetFolder);
			bOK = PostProcessDownload(sTargetFolder, bOK);

			return bOK;
		}

		private bool PreProcessDownload(string sTargetFolder)
		{
			switch (UpdateMode)
			{
				case UpdateOtherPluginMode.PlgxDirect:
				case UpdateOtherPluginMode.DllDirect:
					PluginDebug.AddInfo("Other plugin update", 0, "Nothing to do");
					return true;
				case UpdateOtherPluginMode.ZipExtractPlgx:
				case UpdateOtherPluginMode.ZipExtractDll:
					string sSourceFile = m_lDownloaded[0];
					byte[] pb = File.ReadAllBytes(sSourceFile);
					File.Delete(sSourceFile);
					string sPattern = UpdateMode == UpdateOtherPluginMode.ZipExtractDll ? "*.dll" : "*.plgx";
					using (MemoryStream ms = new MemoryStream())
					{
						ms.Write(pb, 0, pb.Length);
						ms.Position = 0;
						pb = null;
						using (Ionic.Zip.ZipFile z = Ionic.Zip.ZipFile.Read(ms))
						{
							List<Ionic.Zip.ZipEntry> f = z.SelectEntries(sPattern) as List<Ionic.Zip.ZipEntry>;
							using (MemoryStream msTarget = new MemoryStream())
							{
								f[0].Extract(msTarget);
								pb = msTarget.ToArray();
								string sTargetFile = UrlUtil.GetFileDirectory(m_lDownloaded[0], true, true) + f[0].FileName;
								//target filename might contain directory seperators that need to be converted
								sTargetFile = UrlUtil.ConvertSeparators(sTargetFile);
								//Create target folder if required
								//CreateDirectory internally checks for existence
								//Thus, no need to check in our codd
								Directory.CreateDirectory(UrlUtil.GetFileDirectory(sTargetFile, true, true));
								File.WriteAllBytes(sTargetFile, pb);
								m_lDownloaded[0] = sTargetFile;
								PluginDebug.AddInfo("Other plugin update", 0, "Extracted file: " + f[0].FileName);
							}
						}
						return true;
					}
				case UpdateOtherPluginMode.ZipExtractAll:
					string sSourceZip = m_lDownloaded[0];
					byte[] pbZip = File.ReadAllBytes(sSourceZip);
					File.Delete(sSourceZip);
					using (MemoryStream ms = new MemoryStream())
					{
						ms.Write(pbZip, 0, pbZip.Length);
						ms.Position = 0;
						pb = null;
						using (Ionic.Zip.ZipFile z = Ionic.Zip.ZipFile.Read(ms))
						{
							string sTargetFileZip = GetTargetFolder(z, m_lDownloaded[0]);

							z.ExtractAll(sTargetFileZip, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
							List<string> lFiles = new List<string>();
							foreach (var f in z.SelectEntries("*")) lFiles.Add(f.FileName + (f.IsDirectory ? " (directory)" : string.Empty));
							PluginDebug.AddInfo("Other plugin update", 0, lFiles.ToArray());
						}
						return true;
					}
				default: return false;
			}
		}

		public string GetTargetFolder(Ionic.Zip.ZipFile z, string sDownload)
		{
			/*
			sDownload already contains the plugin-specific folder (if any)
			If the zip file contains the plugin folder as well, we need to avoid duplication
			*/
			string sDownloadFolder = UrlUtil.GetFileDirectory(sDownload, true, true);
			if (z.EntriesSorted.Count == 0) return sDownloadFolder;
			
			string[] aSep = new string[] { "/", "\\", UrlUtil.LocalDirSepChar.ToString() };
			string sPluginFileFolder = UrlUtil.GetFileDirectory(PluginFile, true, true);
			if (sPluginFileFolder == PluginUpdateHandler.PluginsFolder) return sDownloadFolder;

			List<string> lPluginFolders = new List<string>();
			lPluginFolders.AddRange(sPluginFileFolder.Split(aSep, StringSplitOptions.RemoveEmptyEntries));

			List<string> lDownloadFolders = new List<string>();
			lDownloadFolders.AddRange(sDownloadFolder.Split(aSep, StringSplitOptions.RemoveEmptyEntries));
			Ionic.Zip.ZipEntry zeEntryRoot = null;
			foreach (var zeEntryFile in z.EntriesSorted)
			{
				if (zeEntryRoot == null)
				{
					zeEntryRoot = zeEntryFile;
					if (!zeEntryRoot.IsDirectory) return sDownloadFolder;
					string sHelp = zeEntryFile.FileName.Replace("/", string.Empty).Replace("\\", string.Empty);
					if (lDownloadFolders[lDownloadFolders.Count - 1] != sHelp) return sDownloadFolder;
					continue;
				}
				if (!zeEntryFile.FileName.StartsWith(zeEntryRoot.FileName)) return sDownloadFolder;
			}
			return UrlUtil.GetShortestAbsolutePath(sDownloadFolder + "..");
		}

		private bool PostProcessDownload(string sTargetFolder, bool bProcessOK)
		{
			if (!bProcessOK) return false;

			if (UpdateMode == UpdateOtherPluginMode.ZipExtractAll) return true;
			/* 
			Do NOT remove everything
			Might delete all plugins in worst case
			Wont't work for dll files anyhow
			{
				//Remove everything, we replace it anyhow
				string s = MergeInPluginFolder(PluginUpdateHandler.PluginsFolder);
				List<string> lFiles = UrlUtil.GetFilePaths(s, "*", SearchOption.AllDirectories);
				foreach (var sFile in lFiles) PluginUpdateHandler.DeleteSpecialFile(sFile);
				return true;
			}
			*/

			//Some plugins contain the plugin version in the filename
			//Identify this case and trigger deletion of old version
			string sNewFile = UrlUtil.GetFileName(MergeInVersion(true));
			string sOldFile = UrlUtil.GetFileName(MergeInVersion(false));

			if (string.Compare(sNewFile, sOldFile, true) == 0) sOldFile = UrlUtil.GetFileName(PluginFile);
			if (string.Compare(sNewFile, sOldFile, true) == 0) return true;

			string sNewFileFull = MergeInPluginFolder(PluginUpdateHandler.PluginsFolder) + sNewFile;
			string sOldFileFull = MergeInPluginFolder(PluginUpdateHandler.PluginsFolder) + sOldFile;
			
			//File.Exists is case sensitive if the OS is case sensitive
			//As ExternalPluginUpdates.xml may contain filenames that don't match because of this, don't rely on File.Exists here
			bool bExists = false;
			if (!KeePassLib.Native.NativeLib.IsUnix()) bExists = File.Exists(sOldFileFull);
			else bExists = UrlUtil.GetFilePaths(MergeInPluginFolder(PluginUpdateHandler.PluginsFolder), "*", SearchOption.TopDirectoryOnly).Find(x => string.Compare(UrlUtil.GetFileName(x), sOldFileFull, true) == 0) != null;
			if (bExists && (string.Compare(sOldFileFull, PluginFile, true) == 0)) PluginUpdateHandler.DeleteSpecialFile(PluginFile);

			sOldFileFull = this.PluginFile;
			bExists = false;
			if (!KeePassLib.Native.NativeLib.IsUnix()) bExists = File.Exists(sOldFileFull);
			else bExists = UrlUtil.GetFilePaths(MergeInPluginFolder(PluginUpdateHandler.PluginsFolder), "*", SearchOption.TopDirectoryOnly).Find(x => string.Compare(UrlUtil.GetFileName(x), sOldFileFull, true) == 0) != null;
			if (bExists && (string.Compare(sOldFileFull, PluginFile, true) == 0)) PluginUpdateHandler.DeleteSpecialFile(PluginFile);

			return true;
		}

        internal static PluginUpdate Factory(string sPluginNamespace)
        {
			switch (sPluginNamespace.ToLowerInvariant())
            {
				//case "kpsyncfordrive": return new OPU_EnforceBuild(sPluginNamespace);
				default: return new OtherPluginUpdate(sPluginNamespace);
			}
            throw new NotImplementedException();
        }
    }

	internal class OPU_EnforceBuild : OtherPluginUpdate
    {
		internal OPU_EnforceBuild(string PluginName) : base(PluginName) { }
        protected override string MergeInVersion(bool bUseAvailableVersion)
        {
			//KPSyn for Drive always uses major, minor and revision
			//Replace -1 by 0 if required
			if (VersionAvailable.Build < 0) VersionAvailable = new Version(VersionAvailable.Major, VersionAvailable.Minor, 0);
			if (VersionInstalled.Build < 0) SetVersionInstalled(new Version(VersionInstalled.Major, VersionInstalled.Minor, 0)); 
			return base.MergeInVersion(bUseAvailableVersion);
        }
    }

	internal class TranslationVersionCheck
	{
		internal string LangugageFile = string.Empty;
		internal long Installed = -1;
		internal long Available = -1;

		internal bool NewTranslationAvailable {  get { return Available > Installed && Installed > -1; } }
		internal bool TranslationForCurrentLanguageAvailable
		{
			get
			{
				if (!LangugageFile.EndsWith(PluginUpdateHandler.LanguageIso + ".language.xml")) return false;
				return Available > Installed && Installed == -1;
			}
		}

		public override string ToString() { return LangugageFile + ": " + Installed.ToString() + "/" + Available.ToString(); }
	}

	public enum UpdateOtherPluginMode
	{
		Unknown = 0,
		PlgxDirect = 2,
		ZipExtractPlgx = 1,
		DllDirect = 3,
		ZipExtractDll = 4,
		ZipExtractAll = 5,
	}

	internal static class UpdateInfoExternParser
	{
		private static UpdateInfoExternList m_Info = new UpdateInfoExternList();

		public static int VersionInstalled = -1;
		public static int VersionAvailable = -1;
		public static UpdateInfoExternList ExternalPluginList { get { return m_Info; } }
		public static string PluginInfoFile { get; private set; }
		static UpdateInfoExternParser()
		{
			Init();
		}

		public static void Init()
		{
			m_Info.Clear();
			PluginInfoFile = PluginUpdateHandler.PluginsFolder + "ExternalPluginUpdates.xml";
			m_Info = ParseInfoFile(PluginInfoFile, true);

			string sPluginInfoFileUser = PluginUpdateHandler.PluginsFolder + "ExternalPluginUpdatesUser.xml";
			var l = ParseInfoFile(sPluginInfoFileUser, false);
			foreach (var i in l)
			{
				m_Info.RemoveAll(x => x.PluginTitle == i.PluginTitle);
				m_Info.Add(i);
			}
		}

		private static UpdateInfoExternList ParseInfoFile(string sPluginInfoFile, bool bDefault)
		{
			List<string> lMsg = new List<string>();
			UpdateInfoExternList lReturn = new UpdateInfoExternList();
			lMsg.Add("Expected filename for update information: " + sPluginInfoFile);
			try
			{
				if (!File.Exists(sPluginInfoFile))
				{
					lMsg.Add("File does not exist");
					return lReturn;
				}

				if (bDefault) UpdateInfoExternParser.VersionInstalled = 0;

				try
				{
					string s = File.ReadAllText(sPluginInfoFile);
					XmlSerializer xs = new XmlSerializer(m_Info.GetType());
					lReturn = (UpdateInfoExternList)xs.Deserialize(new StringReader(s));
				}
				catch (Exception ex)
				{
					lMsg.Add("Error parsing "+ sPluginInfoFile+": " + ex.Message);
					Tools.ShowError("Error parsing "+sPluginInfoFile + ": " + ex.Message);
					return lReturn;
				}
				lMsg.Add("Update information file parsed successfully, parsed data can be found in the following log entries");
				lMsg.Add("Parsed entries: " + lReturn.Count.ToString());
				foreach (var uie in lReturn) lMsg.Add("3rd party plugin: " + uie.ToString());
			}
			finally
			{
				PluginDebug.AddInfo("Loading update information for 3rd party plugins" + (bDefault ? string.Empty : " (user-specific)"), 0, lMsg.ToArray());
			}
			return lReturn;
		}

		internal static bool Get(string PluginName, out UpdateInfoExtern upd)
		{
			upd = null;
			try
			{
				upd = m_Info.Find(x => x.PluginTitle.ToLowerInvariant() == PluginName.ToLowerInvariant());
				if (upd != null) return true;
			}
			catch { }
			return false;
		}
	}

	[XmlRoot("UpdateInfoExternList")]
	public class UpdateInfoExternList : List<UpdateInfoExtern>, IXmlSerializable
	{
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			Clear();
			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();
			if (wasEmpty) return;

			while (reader.NodeType != XmlNodeType.EndElement)
			{
				UpdateInfoExtern uie = new UpdateInfoExtern();
				while (reader.NodeType == XmlNodeType.Comment) reader.Skip();

				string sElement = reader.Name;
				if (sElement == "Version")
				{
					int v = -1;
					if (int.TryParse(reader.ReadElementContentAsString(), out v)) UpdateInfoExternParser.VersionInstalled = v;
					sElement = reader.Name;
				}
				if (sElement != "UpdateInfoExtern") break;
				reader.ReadStartElement("UpdateInfoExtern");

				sElement = reader.Name;
				while (sElement != "UpdateInfoExtern")
				{
					while (reader.NodeType == XmlNodeType.Comment)
					{
						reader.Skip();
						sElement = reader.Name;
					}
					reader.ReadStartElement(sElement);
					if (sElement == "Ignore") uie.Ignore = true;
					else if (sElement == "PluginName") uie.PluginTitle = reader.ReadContentAsString();
					else if (sElement == "PluginURL") uie.PluginURL = reader.ReadContentAsString();
					else if (sElement == "PluginUpdateURL") uie.PluginUpdateURL = reader.ReadContentAsString();
					else if (sElement == "UpdateMode")
					{
						try
						{
							string key = reader.ReadContentAsString();
							uie.UpdateMode = (UpdateOtherPluginMode)Enum.Parse(typeof(UpdateOtherPluginMode), key);
						}
						catch { uie.UpdateMode = UpdateOtherPluginMode.Unknown; }
					}
					else if (sElement == "AllowVersionStripping")
					{
						uie.AllowVersionStripping = StrUtil.StringToBool(reader.ReadContentAsString());
					}
					if (sElement != "Ignore") reader.ReadEndElement();
					sElement = reader.Name;
				}

				if (!string.IsNullOrEmpty(uie.PluginTitle)) Add(uie);

				reader.ReadEndElement();
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			List<UpdateInfoExtern> l = this as List<UpdateInfoExtern>;
			foreach (UpdateInfoExtern uie in l)
			{
				writer.WriteStartElement("UpdateInfoExtern");

				writer.WriteStartElement("PluginName");
				writer.WriteString(uie.PluginTitle);
				writer.WriteEndElement();

				writer.WriteStartElement("PluginURL");
				writer.WriteString(uie.PluginURL);
				writer.WriteEndElement();

				writer.WriteStartElement("PluginUpdateURL");
				writer.WriteString(uie.PluginUpdateURL);
				writer.WriteEndElement();

				writer.WriteStartElement("UpdateMode");
				writer.WriteString(uie.UpdateMode.ToString());
				writer.WriteEndElement();

				writer.WriteStartElement("AllowVersionStripping");
				writer.WriteString(StrUtil.BoolToString(uie.AllowVersionStripping));
				writer.WriteEndElement();

				writer.WriteEndElement();
			}
		}

	}

	public class UpdateInfoExtern
	{
		internal string PluginTitle;
		internal string PluginURL;
		internal string PluginUpdateURL;
		internal UpdateOtherPluginMode UpdateMode;
		internal bool AllowVersionStripping = true;
		internal bool Ignore = false;
		public override string ToString()
		{
			return PluginTitle + " - " + UpdateMode.ToString() + (Ignore ? " (ignore)" : string.Empty);
		}
	}
}
