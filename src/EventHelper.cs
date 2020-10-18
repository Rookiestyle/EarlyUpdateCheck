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
		private const string EVENTNAME = "ItemActivate";
		private static Type tListView = typeof(ListView);
		private static FieldInfo fEventField = null;
		private static FieldInfo GetEventField()
		{
			if (fEventField != null) return fEventField;
			EventInfo ei = tListView.GetEvent(EVENTNAME, AllBindings);
			FieldInfo fi = tListView.GetField(EVENTNAME);
			if (fi == null) fi = tListView.GetField("Event" + ei.Name, AllBindings);
			
			if (fi == null) fi = tListView.GetField(ei.Name + "Event", AllBindings);

			if (fi == null) fi = tListView.GetField("on" + ei.Name, AllBindings);

			fEventField = fi;
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
			FieldInfo fi = GetEventField();
			if (fi == null) return;
			EventInfo ei = tListView.GetEvent(EVENTNAME, AllBindings);
			if (ei == null) ei = fi.DeclaringType.GetEvent(fi.Name, AllBindings);
			if (ei == null) return;

			foreach (Delegate del in m_lEventHandlerItemActivate) ei.RemoveEventHandler(lvPlugins, del);
		}

		internal static List<Delegate> GetItemActivateHandlers(CustomListViewEx lvPlugins)
		{
			List<Delegate> lResult = new List<Delegate>();
			FieldInfo fi = GetEventField();
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
				EventInfo ei = tListView.GetEvent(EVENTNAME, AllBindings); //Windows
				if (ei != null)
				{
					object val = fi.GetValue(lvPlugins);
					Delegate mdel = (val as Delegate);
					if (mdel != null)	lResult.AddRange(mdel.GetInvocationList());
				}
			}
			return lResult;
		}
	}
}

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
			 | AttributeTargets.Method)]
	public sealed class ExtensionAttribute : Attribute { }
}