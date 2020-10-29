using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace EarlyUpdateCheck
{
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
 