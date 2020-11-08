using KeePass.Plugins;
using KeePass.Util;
using KeePassLib.Utility;
using PluginTools;
using PluginTranslation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace EarlyUpdateCheck
{
	internal static class PluginUpdateHandler
	{
		internal static List<PluginUpdate> Plugins = new List<PluginUpdate>();
		private static Dictionary<string, Version> m_Plugins = new Dictionary<string, Version>();

		internal static string LanguageIso = string.Empty;

		private static bool? m_bShieldify;
		public static bool Shieldify
		{
			get
			{
				if (m_bShieldify.HasValue) return m_bShieldify.Value;
				List<string> lShieldify = new List<string>();
				try
				{
					m_bShieldify = false;
					if (KeePassLib.Native.NativeLib.IsUnix())
					{
						lShieldify.Add("Detected Unix");
						return m_bShieldify.Value;
					}
					if (!WinUtil.IsAtLeastWindows7)
					{
						lShieldify.Add("Detected Windows < 7");
						return m_bShieldify.Value;
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
					return m_bShieldify.Value;
				}
				catch (Exception ex) { lShieldify.Add("Exception: " + ex.Message); return m_bShieldify.Value; }
				finally
				{
					lShieldify.Insert(0, "Shieldify: " + m_bShieldify.ToString());
					PluginDebug.AddInfo("Check Shieldify", 0, lShieldify.ToArray());
				}
				/*
				string sTempFile = PluginsFolder + "EarlyUpdateCheckTest.txt";
				try
				{
					File.WriteAllText(sTempFile, "Test file to check for write access");
					File.Delete(sTempFile);
					m_bShieldify = false;
				}
				catch (Exception) { m_bShieldify = true; }
				return m_bShieldify.Value;
				*/
			}
		}

		private static string EnsureNonNull(string v)
		{
			if (v == null) return string.Empty;
			return v;
		}

		public static string PluginsFolder { get; private set; }
		public static string PluginsTranslationsFolder { get; private set; }

		public static string GetTempFolder()
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
			List<string> lMsg = new List<string>();
			lMsg.Add("Plugins folder: " + PluginsFolder);
			lMsg.Add("Plugins translation folder: " + PluginsTranslationsFolder);
			lMsg.Add("Shieldify: " + Shieldify.ToString());
			PluginDebug.AddInfo("PluginUpdateHandler initialized", 0, lMsg.ToArray());
		}

		internal static void LoadPlugins(bool bReload)
		{
			if (!bReload && Plugins.Count > 0) return;
			Plugins.Clear();
			if (m_Plugins.Count == 0) m_Plugins = Tools.GetLoadedPluginsName();
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
					if (bOwnPlugin) pu = new OwnPluginUpdate(p.GetType().Namespace);
					else
					{
						try { pu = new OtherPluginUpdate(p.GetType().Namespace); } catch { }
					}
					if (pu != null)
					{
						Plugins.Add(pu);
						if (pu.UpdatePossible) lPluginnames.Add(pu.Name);
					}
				}
				catch (Exception ex) { PluginDebug.AddError(ex.Message); }
			}
			PluginDebug.AddInfo("Installed updateable plugins", lPluginnames.ToArray());
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

		public static bool MoveAll(string sTempFolder)
		{
			string sTargetFolder = PluginUpdateHandler.PluginsFolder;
			bool bSuccess = false;
			if (Shieldify)
			{
				if (WinUtil.IsAtLeastWindowsVista &&
					(NativeMethods.ShieldifyNativeDialog(System.Windows.Forms.DialogResult.Yes, () => Tools.AskYesNo(PluginTranslate.TryUAC, PluginTranslate.PluginUpdateCaption)) == System.Windows.Forms.DialogResult.Yes))
				{
					bSuccess = FileCopier.CopyFiles(sTempFolder, sTargetFolder);
					if (!bSuccess)
					{
						if (Tools.AskYesNo(PluginTranslate.PluginUpdateFailed, PluginTranslate.PluginUpdateCaption) == System.Windows.Forms.DialogResult.Yes)
						{
							System.Diagnostics.Process.Start(sTempFolder);
						}
					}
				}
				else if (Tools.AskYesNo(PluginTranslate.OpenTempFolder, PluginTranslate.PluginUpdateCaption) == System.Windows.Forms.DialogResult.Yes)
				{
					System.Diagnostics.Process.Start(sTempFolder);
				}
			}
			else
			{
				bSuccess = true;
				List<string> lFiles = UrlUtil.GetFilePaths(sTempFolder, "*", SearchOption.AllDirectories);
				List<string> lMsg = new List<string>();
				foreach (string sFile in lFiles)
				{
					string sTargetFile = sFile.Replace(sTempFolder, sTargetFolder);
					try { File.Copy(sFile, sTargetFile, true); }
					catch (Exception ex)
					{
						bSuccess = false;
						lMsg.Add(ex.Message);
					}
				}
				if (!bSuccess)
				{
					PluginDebug.AddError("Error moving files", 0, lMsg.ToArray());
					if (Tools.AskYesNo(PluginTranslate.PluginUpdateFailed, PluginTranslate.PluginUpdateCaption) == System.Windows.Forms.DialogResult.Yes)
					{
						System.Diagnostics.Process.Start(sTempFolder);
					}
				}
			}
			return bSuccess;
		}

		internal static void Cleanup(string sTempPluginsFolder)
		{
			try { Directory.Delete(sTempPluginsFolder, true); } catch { }
		}
	}

	internal abstract class PluginUpdate
	{
		private static Dictionary<string, Version> m_Plugins = new Dictionary<string, Version>();
		internal string Name { get; private set; }
		internal string Title { get; private set; }
		internal Version VersionInstalled { get; private set; }
		internal Version VersionAvailable { get; set; }
		internal List<TranslationVersionCheck> Translations { get; private set; }
		internal string URL { get; set; }	//URL for plugin homepage
		internal string PluginUpdateURL { get; set; }	//direct link to download newest version
		public string VersionURL { get; private set; } //string to version file, used for translation checks
		internal UpdateOtherPluginMode UpdateMode { get; set; }
		internal bool AllowVersionStripping { get; set; }
		internal bool Selected;
		public override string ToString()
		{
			return Title + " - " + UpdateMode.ToString() + " (" + VersionInstalled.ToString() + " / " + VersionAvailable.ToString() + ")";
		}

		internal bool UpdatePossible { get { return !string.IsNullOrEmpty(PluginUpdateURL) && UpdateMode != UpdateOtherPluginMode.Unknown; } }

		internal PluginUpdate(string PluginName)
		{
			if (m_Plugins.Count == 0) m_Plugins = Tools.GetLoadedPluginsName();
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

			Version v;
			if (!m_Plugins.TryGetValue(p.ToString(), out v)) v = p.GetType().Assembly.GetName().Version;
			VersionInstalled = v;
			VersionAvailable = new Version(0, 0);

			AllowVersionStripping = false;
			UpdateMode = UpdateOtherPluginMode.Unknown;

			URL = string.Empty;
			PluginUpdateURL = string.Empty;

			Translations = new List<TranslationVersionCheck>();
		}

		internal virtual bool Download(string sTempFolder)
		{
			if (string.IsNullOrEmpty(sTempFolder)) return false;
			string sFile = MergeInVersion();
			return DownloadFile(sFile, sTempFolder);
		}

		protected virtual string MergeInVersion()
		{
			string sResult = PluginUpdateURL;
			//VersionAvailable = new Version(0, 4, 0, 1);
			//sResult = "a{MAJOR}-{.MINOR}-{.BUILD}-{.revision}b";
			bool bStripping = AllowVersionStripping && VersionAvailable.Revision < 1;
			Regex r = new Regex(@"\{([^}]*)REVISION\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + VersionAvailable.Revision.ToString());
			

			bStripping &= VersionAvailable.Build < 1;
			r = new Regex(@"\{([^}]*)BUILD\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + VersionAvailable.Build.ToString());

			bStripping &= VersionAvailable.Minor < 1;
			r = new Regex(@"\{([^}]*)MINOR\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + VersionAvailable.Minor.ToString());
			

			bStripping &= VersionAvailable.Major < 1;
			r = new Regex(@"\{([^}]*)MAJOR\}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (bStripping) sResult = r.Replace(sResult, string.Empty);
			else sResult = r.Replace(sResult, "$1\n" + VersionAvailable.Major.ToString());

			sResult = sResult.Replace("\n", string.Empty);
			
			return sResult;
		}

		protected bool DownloadFile(string sFile, string sTargetFolder)
		{
			const int MAXATTEMPTS = 3;
			int iAttempts = 0;
			while (iAttempts++ < MAXATTEMPTS)
			{
				try
				{
					KeePassLib.Serialization.IOConnectionInfo ioc = KeePassLib.Serialization.IOConnectionInfo.FromPath(sFile);
					Stream s = KeePassLib.Serialization.IOConnection.OpenRead(ioc);
					if (s == null) throw new InvalidOperationException();
					MemoryStream ms = new MemoryStream();
					MemUtil.CopyStream(s, ms);
					s.Close();
					byte[] pb = ms.ToArray();
					ms.Close();
					string sTarget = sTargetFolder + UrlUtil.GetFileName(sFile);
					File.WriteAllBytes(sTarget, pb);
					PluginDebug.AddInfo("Download success", 0, "Source: " + sFile, "Target: " + sTargetFolder, "Download attempt: " + iAttempts.ToString());
					return true;
				}
				catch (Exception ex)
				{
					PluginDebug.AddInfo("Download failed", 0, "Source: " + sFile, "Target: " + sTargetFolder, "Download attempt: " + iAttempts.ToString(), ex.Message);

					System.Net.WebException exWeb = ex as System.Net.WebException;
					if (exWeb == null) continue;
					System.Net.HttpWebResponse wrResponse = exWeb.Response as System.Net.HttpWebResponse;
					if ((wrResponse == null) || (wrResponse.StatusCode != System.Net.HttpStatusCode.NotFound)) continue;
					iAttempts = MAXATTEMPTS;
				}
			}
			return false;
		}

		internal virtual bool ProcessDownload(string sTargetFolder)
		{
			//Added for plugin specific file movements (future)
			return true;
		}

		internal virtual void Cleanup()
		{
			//Added for plugin specific file cleanups (future)
		}
	}

	internal class OwnPluginUpdate : PluginUpdate
	{
		internal OwnPluginUpdate(string PluginName) : base(PluginName)
		{
			GetOwnPluginURL();
			AllowVersionStripping = false;
			UpdateMode = UpdateOtherPluginMode.PlgxDirect;

			PluginUpdateURL = URL + "latest/download/" + Name + ".plgx";
			UpdateTranslationInfo();
		}

		internal bool DownloadTranslations(string sTempFolder, bool bDownloadCurrentLangue)
		{
			string sTempTranslationsFolder = UrlUtil.EnsureTerminatingSeparator(sTempFolder + "Translations", false);
			Directory.CreateDirectory(sTempTranslationsFolder);

			bool bOK = true;
			foreach (var t in Translations)
			{
				string sFile = URL.Replace("github.com", "raw.githubusercontent.com") + "../master/Translations/" + t.LangugageFile;
				if (!t.NewTranslationAvailable && !(bDownloadCurrentLangue && t.TranslationForCurrentLanguageAvailable)) continue;
				if (DownloadFile(sFile, sTempTranslationsFolder)) continue;
				bOK = false;
				Tools.ShowError(string.Format(PluginTranslate.PluginTranslationUpdateFailed, Title, t.LangugageFile), PluginTranslate.PluginUpdateCaption);
			}
			return bOK;
		}

		internal void UpdateTranslationInfo()
		{
			Translations.Clear();
			
			UpdateInstalledTranslations();
			UpdateAvailableTranslations();

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
					Translations.Add(new TranslationVersionCheck() { LangugageFile = UrlUtil.GetFileName(lang), Installed = lVerInstalled });
				}
			}
			catch (Exception) { }
		}

		private void UpdateAvailableTranslations()
		{
			Dictionary<string, List<UpdateComponentInfo>> dUpdateInfo = new Dictionary<string, List<UpdateComponentInfo>>();
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

			dUpdateInfo = mi.Invoke(null, new object[] { new List<string>() { VersionURL }, null }) as Dictionary<string, List<UpdateComponentInfo>>;

			List<string> lTranslationsNew = new List<string>();
			string[] cSplit = new string[] { "!", "!!!" };
			foreach (KeyValuePair<string, List<UpdateComponentInfo>> kvp in dUpdateInfo)
			{
				if (kvp.Value == null) continue;
				Version vCheck = null;
				foreach (UpdateComponentInfo uci in kvp.Value)
				{
					//Github: <Plugin>!<language identifier>
					string[] sParts = uci.Name.Split(cSplit, StringSplitOptions.RemoveEmptyEntries);
					if (sParts.Length == 1)
					{
						vCheck = new Version(StrUtil.VersionToString(uci.VerAvailable, 2));
						if (VersionAvailableIsUnknown()) VersionAvailable = vCheck;
					}
					if (!PluginUpdateHandler.VersionsEqual(VersionInstalled, vCheck)) return; //Different version might require different translation files
					if (sParts.Length != 2) continue;
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
			}
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

	internal class OtherPluginUpdate : PluginUpdate
	{
		internal OtherPluginUpdate(string PluginName) : base(PluginName)
		{
			UpdateInfoExtern uie;
			if (!UpdateInfoParser.Get(Title, out uie) && !UpdateInfoParser.Get(Name, out uie)) throw new ArgumentException("No update information available for " + PluginName);

			URL = uie.PluginURL;
			PluginUpdateURL = uie.PluginUpdateURL;
			UpdateMode = uie.UpdateMode;
			AllowVersionStripping = uie.AllowVersionStripping;
		}

		internal override bool Download(string sTempFolder)
		{
			if (!base.Download(sTempFolder)) return false;

			switch (UpdateMode)
			{
				case UpdateOtherPluginMode.PlgxDirect:
				case UpdateOtherPluginMode.DllDirect:
					PluginDebug.AddInfo("Other plugin update", 0, "Nothing to do");
					return true;
				case UpdateOtherPluginMode.ZipExtractPlgx:
				case UpdateOtherPluginMode.ZipExtractDll:
					string sSourceFile = sTempFolder + UrlUtil.GetFileName(PluginUpdateURL);
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
								string sTargetFile = sTempFolder + f[0].FileName;
								File.WriteAllBytes(sTargetFile, pb);
								PluginDebug.AddInfo("Other plugin update", 0, "Extracted file: " + f[0].FileName);
							}
						}
						return true;
					}
				default: return false;
			}
		}

		internal override bool ProcessDownload(string sTargetFolder)
		{
			if (Name.ToLowerInvariant() == "KeePassSyncForDrive".ToLowerInvariant())
			{
				string sSource = sTargetFolder + UrlUtil.GetFileName(MergeInVersion());
				string sTarget = sTargetFolder + "KeePassSyncForDrive.plgx";

				//Don't copy sSource -> sTarget and delete sSource afterwards - could be the same...
				PluginDebug.AddInfo("Process plugin download", 0,
					"Plugin: " + Name, "Action: Rename", "From: " + UrlUtil.GetFileName(sSource), "To: " + UrlUtil.GetFileName(sTarget));
				byte[] b = File.ReadAllBytes(sSource);
				File.Delete(sSource);
				File.WriteAllBytes(sTarget, b);
			}
			return base.ProcessDownload(sTargetFolder);
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
	}

	internal static class UpdateInfoParser
	{
		private static UpdateInfoExternList m_Info = new UpdateInfoExternList();

		static UpdateInfoParser()
		{
			m_Info.Clear();
			string sFilename = PluginUpdateHandler.PluginsFolder + "ExternalPluginUpdates.xml";
			List<string> lMsg = new List<string>();
			lMsg.Add("Expected filename for update information: " + sFilename);
			try
			{
				if (!File.Exists(sFilename))
				{
					lMsg.Add("File does not exist");
					return;
				}

				try
				{
					string s = File.ReadAllText(sFilename);
					XmlSerializer xs = new XmlSerializer(m_Info.GetType());
					m_Info = (UpdateInfoExternList)xs.Deserialize(new StringReader(s));
				}
				catch (Exception ex)
				{
					lMsg.Add("Error parsing file: " + ex.Message);
					return;
				}
				lMsg.Add("Update information file parsed successfully, parsed data can be found in the following log entries");
				lMsg.Add("Parsed entries: " + m_Info.Count.ToString());
				foreach (var uie in m_Info) lMsg.Add("3rd party plugin: " + uie.ToString());
			}
			finally
			{
				PluginDebug.AddInfo("Loading update information for 3rd party plugins", 0, lMsg.ToArray());
			}
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

				reader.ReadStartElement("UpdateInfoExtern");

				string sElement = reader.Name;
				while (sElement != "UpdateInfoExtern")
				{
					while (reader.NodeType == XmlNodeType.Comment)
					{
						reader.Skip();
						sElement = reader.Name;
					}
					reader.ReadStartElement(sElement);
					if (sElement == "PluginName") uie.PluginTitle = reader.ReadContentAsString();
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
					else if (sElement == "AllowVersionStripping") uie.AllowVersionStripping = reader.ReadContentAsBoolean();
					reader.ReadEndElement();
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
				writer.WriteString(uie.AllowVersionStripping.ToString());
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

		public override string ToString()
		{
			return PluginTitle + " - " + UpdateMode.ToString();
		}
	}
}
