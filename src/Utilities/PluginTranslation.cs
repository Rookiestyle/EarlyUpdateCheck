using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using KeePass.Plugins;
using KeePass.Util;
using KeePassLib.Utility;
using PluginTools;

namespace PluginTranslation
{
  public class TranslationChangedEventArgs : EventArgs
  {
    public string OldLanguageIso6391 = string.Empty;
    public string NewLanguageIso6391 = string.Empty;

    public TranslationChangedEventArgs(string OldLanguageIso6391, string NewLanguageIso6391)
    {
      this.OldLanguageIso6391 = OldLanguageIso6391;
      this.NewLanguageIso6391 = NewLanguageIso6391;
    }
  }

  public static class PluginTranslate
  {
    public static long TranslationVersion = 0;
    public static event EventHandler<TranslationChangedEventArgs> TranslationChanged = null;
    private static string LanguageIso6391 = string.Empty;
    #region Definitions of translated texts go here
    public const string PluginName = "Early Update Check";
    /// <summary>
    /// Active
    /// </summary>
    public static readonly string Active = @"Active";
    /// <summary>
    /// Update check in foreground
    /// </summary>
    public static readonly string CheckSync = @"Update check in foreground";
    /// <summary>
    /// EarlyUpdateCheck will always start the update check at the earliest possible time.
    /// 
    /// If this option is active, EarlyUpdateCheck will additionally ensure the update check is finished BEFORE the 'Open Database' window is shown.
    /// You may have the upgrade check continue in background if you don't want to wait for its completion.
    /// </summary>
    public static readonly string CheckSyncDesc = @"EarlyUpdateCheck will always start the update check at the earliest possible time.

If this option is active, EarlyUpdateCheck will additionally ensure the update check is finished BEFORE the 'Open Database' window is shown.
You may have the upgrade check continue in background if you don't want to wait for its completion.";
    /// <summary>
    /// Continue in background
    /// </summary>
    public static readonly string EnterBackgroundMode = @"Continue in background";
    /// <summary>
    /// One-Click plugin update
    /// </summary>
    public static readonly string PluginUpdateOneClick = @"One-Click plugin update";
    /// <summary>
    /// Install new versions of plugins by clicking the plugin in the 'Update Check' window.
    /// This requires write access to KeePass' plugin folder.
    /// KeePass needs to be restarted afterwards in order to use the new version.
    /// 
    /// Maintain file ExternalPluginUpdates.xml to include other authors' plugins, see the wiki for more details
    /// </summary>
    public static readonly string PluginUpdateOneClickDesc = @"Install new versions of plugins by clicking the plugin in the 'Update Check' window.
This requires write access to KeePass' plugin folder.
KeePass needs to be restarted afterwards in order to use the new version.

Maintain file ExternalPluginUpdates.xml to include other authors' plugins, see the wiki for more details";
    /// <summary>
    /// Update
    /// </summary>
    public static readonly string PluginUpdate = @"Update";
    /// <summary>
    /// Update KeePass
    /// </summary>
    public static readonly string PluginUpdateKeePass = @"Update KeePass";
    /// <summary>
    /// Update plugins
    /// </summary>
    public static readonly string PluginUpdateSelected = @"Update plugins";
    /// <summary>
    /// Early Update Check - Plugin Updater
    /// </summary>
    public static readonly string PluginUpdateCaption = @"Early Update Check - Plugin Updater";
    /// <summary>
    /// Updating {0}, please wait...
    /// </summary>
    public static readonly string PluginUpdating = @"Updating {0}, please wait...";
    /// <summary>
    /// The update was successful, the new version(s) will be active after a restart of KeePass.
    /// Restart now?
    /// </summary>
    public static readonly string PluginUpdateSuccess = @"The update was successful, the new version(s) will be active after a restart of KeePass.
Restart now?";
    /// <summary>
    /// Moving downloaded files failed. Open temporary folder instead?
    /// </summary>
    public static readonly string PluginUpdateFailed = @"Moving downloaded files failed. Open temporary folder instead?";
    /// <summary>
    /// Plugin: {0}
    /// The update failed
    /// </summary>
    public static readonly string PluginUpdateFailedSpecific = @"Plugin: {0}
The update failed";
    /// <summary>
    /// Plugin: {0}
    /// Update of language file {1} failed
    /// </summary>
    public static readonly string PluginTranslationUpdateFailed = @"Plugin: {0}
Update of language file {1} failed";
    /// <summary>
    /// Update could not be finished. Try alternative method?
    /// This might show the UAC prompt.
    /// </summary>
    public static readonly string TryUAC = @"Update could not be finished. Try alternative method?
This might show the UAC prompt.";
    /// <summary>
    /// Open temporary folder containing the updated files?
    /// </summary>
    public static readonly string OpenTempFolder = @"Open temporary folder containing the updated files?";
    /// <summary>
    /// Manually update translations
    /// </summary>
    public static readonly string TranslationDownload_Update = @"Manually update translations";
    /// <summary>
    /// Always download translations for active language: {0}
    /// </summary>
    public static readonly string TranslationDownload_DownloadCurrent = @"Always download translations for active language: {0}";
    /// <summary>
    /// Early Update Check - Update translations
    /// </summary>
    public static readonly string TranslationUpdateForm = @"Early Update Check - Update translations";
    /// <summary>
    /// Please select plugins for which translations shall be updated
    /// </summary>
    public static readonly string SelectPluginsForTranslationUpdate = @"Please select plugins for which translations shall be updated";
    /// <summary>
    /// A new version of ExternalPluginUpdates.xml is available.
    /// Download to update more 3rd party plugins?
    /// </summary>
    public static readonly string UpdateExternalInfo = @"A new version of ExternalPluginUpdates.xml is available.
Download to update more 3rd party plugins?";
    /// <summary>
    /// EarlyUpdateCheck can update selected 3rd party plugins as well.
    /// This is facilitated by a file called ExternalPluginUpdates.xml.
    /// Download this file now to allow updating those plugins?
    /// 
    /// This question will not be asked again.
    /// If you decide to not use this feature now, you can download this file in EarlyUpdateCheck's options.
    /// </summary>
    public static readonly string UpdateExternalInfoInitialDownload = @"EarlyUpdateCheck can update selected 3rd party plugins as well.
This is facilitated by a file called ExternalPluginUpdates.xml.
Download this file now to allow updating those plugins?

This question will not be asked again.
If you decide to not use this feature now, you can download this file in EarlyUpdateCheck's options.";
    /// <summary>
    /// Download ExternalPluginUpdates.xml
    /// </summary>
    public static readonly string UpdateExternalInfoDownload = @"Download ExternalPluginUpdates.xml";
    /// <summary>
    /// EarlyUpdateCheck downloaded the newest portable version of KeePass.
    /// 
    /// The downloaded files will open in a new window.
    /// To perform the update, close KeePass and copy these files to your current KeePass location.
    /// </summary>
    public static readonly string KeePassUpdate_InstallZip = @"EarlyUpdateCheck downloaded the newest portable version of KeePass.

The downloaded files will open in a new window.
To perform the update, close KeePass and copy these files to your current KeePass location.";
    /// <summary>
    /// EarlyUpdateCheck downloaded the newest version of KeePass.
    /// To perform the update, you need to close KeePass.
    /// 
    /// Click {0} to run the update now.
    /// Click {1} to open the folder containing the downloaded file instead.
    /// </summary>
    public static readonly string KeePassUpdate_InstallSetupOrMsi = @"EarlyUpdateCheck downloaded the newest version of KeePass.
To perform the update, you need to close KeePass.

Click {0} to run the update now.
Click {1} to open the folder containing the downloaded file instead.";
    /// <summary>
    /// EarlyUpdateCheck can help in updating KeePass as well.
    /// Since KeePass can be installed in different ways, please confirm your installation type.
    /// 
    /// Different installation types offered:
    /// - Setup: KeePass-<Version>-Setup.exe is used to install KeePass
    /// - MSI: KeePass-<Version>.msi is used to install KeePass
    /// - Portable: KeePass-<Version>.zip is used to download and extract KeePass
    /// </summary>
    public static readonly string KeePassUpdate_RequestInstallType = @"EarlyUpdateCheck can help in updating KeePass as well.
Since KeePass can be installed in different ways, please confirm your installation type.

Different installation types offered:
- Setup: KeePass-<Version>-Setup.exe is used to install KeePass
- MSI: KeePass-<Version>.msi is used to install KeePass
- Portable: KeePass-<Version>.zip is used to download and extract KeePass";
    /// <summary>
    /// EarlyUpdateCheck detected available updates of plugins and/or KeePass.
    /// 
    /// As option '{0}' is active, the update form cannot be displayed now.
    /// It will be shown AFTER you close the '{1}' form.
    /// </summary>
    public static readonly string SecureDesktopMode = @"EarlyUpdateCheck detected available updates of plugins and/or KeePass.

As option '{0}' is active, the update form cannot be displayed now.
It will be shown AFTER you close the '{1}' form.";
    #endregion

    #region NO changes in this area
    private static StringDictionary m_translation = new StringDictionary();

    public static void Init(Plugin plugin, string LanguageCodeIso6391)
    {
      List<string> lDebugStrings = new List<string>();
      m_translation.Clear();
      bool bError = true;
      LanguageCodeIso6391 = InitTranslation(plugin, lDebugStrings, LanguageCodeIso6391, out bError);
      if (bError && (LanguageCodeIso6391.Length > 2))
      {
        LanguageCodeIso6391 = LanguageCodeIso6391.Substring(0, 2);
        lDebugStrings.Add("Trying fallback: " + LanguageCodeIso6391);
        LanguageCodeIso6391 = InitTranslation(plugin, lDebugStrings, LanguageCodeIso6391, out bError);
      }
      if (bError)
      {
        PluginDebug.AddError("Reading translation failed", 0, lDebugStrings.ToArray());
        LanguageCodeIso6391 = "en";
      }
      else
      {
        List<FieldInfo> lTranslatable = new List<FieldInfo>(
          typeof(PluginTranslate).GetFields(BindingFlags.Static | BindingFlags.Public)
          ).FindAll(x => x.IsInitOnly);
        lDebugStrings.Add("Parsing complete");
        lDebugStrings.Add("Translated texts read: " + m_translation.Count.ToString());
        lDebugStrings.Add("Translatable texts: " + lTranslatable.Count.ToString());
        foreach (FieldInfo f in lTranslatable)
        {
          if (m_translation.ContainsKey(f.Name))
          {
            lDebugStrings.Add("Key found: " + f.Name);
            f.SetValue(null, m_translation[f.Name]);
          }
          else
            lDebugStrings.Add("Key not found: " + f.Name);
        }
        PluginDebug.AddInfo("Reading translations finished", 0, lDebugStrings.ToArray());
      }
      if (TranslationChanged != null)
      {
        TranslationChanged(null, new TranslationChangedEventArgs(LanguageIso6391, LanguageCodeIso6391));
      }
      LanguageIso6391 = LanguageCodeIso6391;
      lDebugStrings.Clear();
    }

    private static string InitTranslation(Plugin plugin, List<string> lDebugStrings, string LanguageCodeIso6391, out bool bError)
    {
      if (string.IsNullOrEmpty(LanguageCodeIso6391))
      {
        lDebugStrings.Add("No language identifier supplied, using 'en' as fallback");
        LanguageCodeIso6391 = "en";
      }
      string filename = GetFilename(plugin.GetType().Namespace, LanguageCodeIso6391);
      lDebugStrings.Add("Translation file: " + filename);

      if (!File.Exists(filename)) //If e. g. 'plugin.zh-tw.language.xml' does not exist, try 'plugin.zh.language.xml'
      {
        lDebugStrings.Add("File does not exist");
        bError = true;
        return LanguageCodeIso6391;
      }
      else
      {
        string translation = string.Empty;
        try { translation = File.ReadAllText(filename); }
        catch (Exception ex)
        {
          lDebugStrings.Add("Error reading file: " + ex.Message);
          LanguageCodeIso6391 = "en";
          bError = true;
          return LanguageCodeIso6391;
        }
        XmlSerializer xs = new XmlSerializer(m_translation.GetType());
        lDebugStrings.Add("File read, parsing content");
        try
        {
          m_translation = (StringDictionary)xs.Deserialize(new StringReader(translation));
        }
        catch (Exception ex)
        {
          string sException = ex.Message;
          if (ex.InnerException != null) sException += "\n" + ex.InnerException.Message;
          lDebugStrings.Add("Error parsing file: " + sException);
          LanguageCodeIso6391 = "en";
          MessageBox.Show("Error parsing translation file\n\n" + sException, PluginName, MessageBoxButtons.OK, MessageBoxIcon.Error);
          bError = true;
          return LanguageCodeIso6391;
        }
        bError = false;
        return LanguageCodeIso6391;
      }
    }

    private static string GetFilename(string plugin, string lang)
    {
      string filename = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), true, true);
      filename += KeePass.App.AppDefs.PluginsDir + UrlUtil.LocalDirSepChar + "Translations" + UrlUtil.LocalDirSepChar;
      filename += plugin + "." + lang + ".language.xml";
      return filename;
    }
    #endregion
  }

  #region NO changes in this area
  [XmlRoot("Translation")]
  public class StringDictionary : Dictionary<string, string>, IXmlSerializable
  {
    public System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(XmlReader reader)
    {
      bool wasEmpty = reader.IsEmptyElement;
      reader.Read();
      if (wasEmpty) return;
      bool bFirst = true;
      while (reader.NodeType != XmlNodeType.EndElement)
      {
        if (bFirst)
        {
          bFirst = false;
          try
          {
            reader.ReadStartElement("TranslationVersion");
            PluginTranslate.TranslationVersion = reader.ReadContentAsLong();
            reader.ReadEndElement();
          }
          catch { }
        }
        reader.ReadStartElement("item");
        reader.ReadStartElement("key");
        string key = reader.ReadContentAsString();
        reader.ReadEndElement();
        reader.ReadStartElement("value");
        string value = reader.ReadContentAsString();
        reader.ReadEndElement();
        this.Add(key, value);
        reader.ReadEndElement();
        reader.MoveToContent();
      }
      reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
      writer.WriteStartElement("TranslationVersion");
      writer.WriteString(PluginTranslate.TranslationVersion.ToString());
      writer.WriteEndElement();
      foreach (string key in this.Keys)
      {
        writer.WriteStartElement("item");
        writer.WriteStartElement("key");
        writer.WriteString(key);
        writer.WriteEndElement();
        writer.WriteStartElement("value");
        writer.WriteString(this[key]);
        writer.WriteEndElement();
        writer.WriteEndElement();
      }
    }
  }
  #endregion
}
