using System;
using UnityEngine;
using UnityModManagerNet;
using XLShredLib;

namespace XLSkinEditor {
	class Main {
		public static bool enabled;
		public static String modId;
		public static UnityModManager.ModEntry modEntry;

		private static SkinEditor skinEditor;

		static void Load(UnityModManager.ModEntry modEntry) {
			Main.modEntry = modEntry;
			Main.modId = modEntry.Info.Id;

			modEntry.OnToggle = OnToggle;
		}

		static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
			if (value == enabled) return true;
			enabled = value;

			if (enabled) {
				skinEditor = ModMenu.Instance.gameObject.AddComponent<SkinEditor>();
			} else {
				if (skinEditor != null)
					UnityEngine.Object.Destroy(ModMenu.Instance.GetComponent<SkinEditor>());
			}

			return true;
		}
	}
}
