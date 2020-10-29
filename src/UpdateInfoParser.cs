using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace EarlyUpdateCheck
{

	public enum UpdateOtherPluginMode
	{
		Unknown = 0,
		PlgxDirect = 2,
		ZipExtractPlgx = 1,
	}

	public class UpdateInfo
	{
		public string Name;
		public string Title;
		public string URL;
		public string VersionInfoURL;
		public bool Selected;
		public bool OwnPlugin;
		public UpdateInfoExtern UpdateInfoExtern;
		public UpdateOtherPluginMode UpdateMode
		{
			get { return UpdateInfoExtern == null ? UpdateOtherPluginMode.Unknown : UpdateInfoExtern.UpdateMode; }
		}
		public bool IsDll
		{
			get
			{
				if (OwnPlugin) return false;
				//return true based on UpdateMode - Currently dll is not implemented
				return false;
			}
		}

		public string NameLowerInvariant { get { return Name.ToLowerInvariant(); } }
		public Version VersionInstalled;
		public Version VersionAvailable;

		public UpdateInfo(string Name, string Title, string URL, string VersionInfoURL, Version Version)
		{
			this.Name = Name;
			this.Title = Title;
			this.URL = URL;
			this.VersionInfoURL = VersionInfoURL;
			this.VersionInstalled = Version;
			this.VersionAvailable = new Version(0, 0);
			this.Selected = false;
			this.OwnPlugin = true;
		}

		public UpdateInfo(string Name, string Title, string VersionInfoURL, Version Version, bool bOwnPlugin) //, UpdateOtherPluginMode UpdateMode)
		{
			this.Name = Name;
			this.Title = Title;
			this.URL = string.Empty;
			this.VersionInfoURL = VersionInfoURL;
			this.VersionInstalled = Version;
			this.VersionAvailable = new Version(0, 0);
			this.Selected = false;
			this.OwnPlugin = bOwnPlugin;
			if (UpdateInfoParser.Get(Title, out UpdateInfoExtern)) this.URL = UpdateInfoExtern.PluginURL;
		}

		public override string ToString()
		{
			return Name + " - " + VersionInstalled.ToString() + " / " + VersionAvailable.ToString();
		}

		public static string GetName(UpdateInfo ui)
		{
			return ui.Name;
		}

		internal string GetPluginUrl()
		{
			if (OwnPlugin) return URL + "releases";
			if (UpdateInfoExtern == null) return URL;
			return UpdateInfoExtern.PluginURL;
		}

		internal string GetDownloadUrl(string language)
		{
			if (OwnPlugin)
			{
				if (string.IsNullOrEmpty(language))
					return URL + "releases/download/v" + VersionAvailable.ToString() + "/" + Name + ".plgx";
				return URL.Replace("github.com", "raw.githubusercontent.com") + "master/Translations/" + language;
			}
			switch (Name.ToLowerInvariant())
			{
				case "webautotype": return "https://sourceforge.net/projects/webautotype/files/latest/download";
				default: return URL + "releases";
			}
		}

	}

	internal static class UpdateInfoParser
	{
		private static UpdateInfoExternList m_Info = new UpdateInfoExternList();

		static UpdateInfoParser()
		{
			m_Info.Clear();
			string sFilename = EarlyUpdateCheckExt.m_PluginsFolder + "ExternalPluginUpdates.xml";
			if (!File.Exists(sFilename)) return;

			try
			{
				string s = File.ReadAllText(sFilename);
				XmlSerializer xs = new XmlSerializer(m_Info.GetType());
				m_Info = (UpdateInfoExternList)xs.Deserialize(new StringReader(s));
			}
			catch { }
		}

		internal static bool Get(string PluginName, out UpdateInfoExtern upd)
		{
			upd = null;
			try
			{
				upd = m_Info.Find(x => x.PluginName.ToLowerInvariant() == PluginName.ToLowerInvariant());
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
				reader.ReadStartElement("UpdateInfoExtern");

				reader.ReadStartElement("PluginName");
				uie.PluginName = reader.ReadContentAsString();
				reader.ReadEndElement();

				reader.ReadStartElement("PluginURL");
				uie.PluginURL = reader.ReadContentAsString();
				reader.ReadEndElement();

				reader.ReadStartElement("PluginUpdateURL");
				uie.PluginUpdateURL = reader.ReadContentAsString();
				reader.ReadEndElement();

				reader.ReadStartElement("UpdateMode");
				string key = reader.ReadContentAsString();
				reader.ReadEndElement();
				try { uie.UpdateMode = (UpdateOtherPluginMode)Enum.Parse(typeof(UpdateOtherPluginMode), key); }
				catch { uie.UpdateMode = UpdateOtherPluginMode.Unknown; }

				Add(uie);

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
				writer.WriteString(uie.PluginName);
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

				writer.WriteEndElement();
			}
		}

	}

	public class UpdateInfoExtern
	{
		internal string PluginName;
		internal string PluginURL;
		internal string PluginUpdateURL;
		internal UpdateOtherPluginMode UpdateMode;
	}
}
 