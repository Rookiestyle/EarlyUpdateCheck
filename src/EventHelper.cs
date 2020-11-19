using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using KeePass.UI;

namespace PluginTools
{
	public static class EventHelper
	{
		private static Dictionary<Type, List<FieldInfo>> m_dicEventFieldInfos = new Dictionary<Type, List<FieldInfo>>();

		private const BindingFlags AllBindings = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		private const string EVENTNAME_ItemActivate = "ItemActivate";
		private const string EVENTNAME_FormLoadPost = "FormLoadPost";
		private static Type tListView = typeof(ListView);
		private static Type tMainform = typeof(KeePass.Forms.MainForm);

		private static FieldInfo GetEventField(string e, Type t)
		{
			EventInfo ei = t.GetEvent(e, AllBindings);
			FieldInfo fi = t.GetField(e, AllBindings);
			if (fi == null) fi = t.GetField("Event" + ei.Name, AllBindings);
			
			if (fi == null) fi = t.GetField(ei.Name + "Event", AllBindings);

			if (fi == null) fi = t.GetField("on" + ei.Name, AllBindings);

			return fi;
		}

		private static EventHandlerList GetStaticEventHandlerList(object obj)
		{
			MethodInfo mi = tListView.GetMethod("get_Events", AllBindings);
			if (mi == null) return null;
			return (EventHandlerList)mi.Invoke(obj, new object[] { });
		}

		internal static void RemoveItemActivateEventHandlers(CustomListViewEx lvPlugins, List<Delegate> m_lEventHandlerItemActivate)
		{
			FieldInfo fi = GetEventField(EVENTNAME_ItemActivate, tListView);
			if (fi == null) return;
			EventInfo ei = tListView.GetEvent(EVENTNAME_ItemActivate, AllBindings);
			if (ei == null) ei = fi.DeclaringType.GetEvent(fi.Name, AllBindings);
			if (ei == null) return;

			foreach (Delegate del in m_lEventHandlerItemActivate) ei.RemoveEventHandler(lvPlugins, del);
		}

		internal static List<Delegate> GetItemActivateHandlers(CustomListViewEx lvPlugins)
		{
			List<Delegate> lResult = new List<Delegate>();
			FieldInfo fi = GetEventField(EVENTNAME_ItemActivate, tListView);
			if (fi == null) return lResult;
			if (fi.IsStatic) //Unix (Mono)
			{
				EventHandlerList static_event_handlers = GetStaticEventHandlerList(lvPlugins);

				object idx = fi.GetValue(lvPlugins);
				Delegate eh = static_event_handlers[idx];
				if (eh != null)
				{
					Delegate[] dels = eh.GetInvocationList();
					if (dels != null) lResult.AddRange(dels);
				}
			}
			else //Windows
			{
				EventInfo ei = tListView.GetEvent(EVENTNAME_ItemActivate, AllBindings); //Windows
				if (ei != null)
				{
					object val = fi.GetValue(lvPlugins);
					Delegate mdel = (val as Delegate);
					if (mdel != null)	lResult.AddRange(mdel.GetInvocationList());
				}
			}
			return lResult;
		}


		internal static List<Delegate> GetFormLoadPostHandlers()
		{
			List<Delegate> lResult = new List<Delegate>();
			FieldInfo fi = GetEventField(EVENTNAME_FormLoadPost, tMainform);
			if (fi == null) return lResult;
			if (fi.IsStatic) //Unix (Mono)
			{
				EventHandlerList static_event_handlers = GetStaticEventHandlerList(KeePass.Program.MainForm);

				object idx = fi.GetValue(KeePass.Program.MainForm);
				Delegate eh = static_event_handlers[idx];
				if (eh != null)
				{
					Delegate[] dels = eh.GetInvocationList();
					if (dels != null) lResult.AddRange(dels);
				}
			}
			else //Windows
			{
				EventInfo ei = tMainform.GetEvent(EVENTNAME_FormLoadPost, AllBindings); //Windows
				if (ei != null)
				{
					object val = fi.GetValue(KeePass.Program.MainForm);
					Delegate mdel = (val as Delegate);
					if (mdel != null) lResult.AddRange(mdel.GetInvocationList());
				}
			}
			return lResult;
		}

		internal static void RemoveFormLoadPostEventHandlers(List<Delegate> handlers)
		{
			FieldInfo fi = GetEventField(EVENTNAME_FormLoadPost, tMainform);
			if (fi == null) return;
			EventInfo ei = tMainform.GetEvent(EVENTNAME_ItemActivate, AllBindings);
			if (ei == null) ei = fi.DeclaringType.GetEvent(fi.Name, AllBindings);
			if (ei == null) return;

			if (KeePass.Program.MainForm != null)
			lock (KeePass.Program.MainForm)
			{
				foreach (Delegate del in handlers) ei.RemoveEventHandler(KeePass.Program.MainForm, del);
			}
		}

		internal static void RestoreFormLoadPostEventHandlers(List<Delegate> handlers)
		{
			FieldInfo fi = GetEventField(EVENTNAME_FormLoadPost, tMainform);
			if (fi == null) return;

			if (fi.IsStatic)
			{
				EventInfo ei = tMainform.GetEvent(fi.Name, AllBindings);
				if (ei == null)
					ei = tMainform.GetEvent(EVENTNAME_FormLoadPost, AllBindings);
				if (ei == null)
					ei = fi.DeclaringType.GetEvent(fi.Name, AllBindings);
				if (ei == null)
					ei = fi.DeclaringType.GetEvent(EVENTNAME_FormLoadPost, AllBindings);
				if (ei == null) return;

				foreach (var del in handlers)
					ei.AddEventHandler(KeePass.Program.MainForm, del);
			}
			else
			{
				EventInfo ei = tMainform.GetEvent(fi.Name, AllBindings);
				if (ei == null)
					ei = tMainform.GetEvent(EVENTNAME_FormLoadPost, AllBindings);
				if (ei == null)
					ei = fi.DeclaringType.GetEvent(fi.Name, AllBindings);
				if (ei == null)
					ei = fi.DeclaringType.GetEvent(EVENTNAME_FormLoadPost, AllBindings);
				if (ei != null)
				{
					foreach (var del in handlers)
						ei.AddEventHandler(KeePass.Program.MainForm, del);
				}
			}			
		}
	}
}

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
			 | AttributeTargets.Method)]
	public sealed class ExtensionAttribute : Attribute { }
}