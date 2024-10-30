using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Logging;

namespace HDeMods {
	[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public sealed class InterlopingArtifactPlugin : BaseUnityPlugin {
		public const string PluginGUID = PluginAuthor + "." + PluginName;
		public const string PluginAuthor = "HDeDeDe";
		public const string PluginName = "InterlopingArtifact";
		public const string PluginVersion = "0.0.1";

		private void Awake() {
			INTER.Log.Init(Logger);
			InterlopingArtifact.Startup();
		}
	}

	namespace INTER {
		internal static class Log {
			private static ManualLogSource? logMe;

			internal static void Init(ManualLogSource logSource) {
				logMe = logSource;
			}

			internal static void Debug(object data) => logMe!.LogDebug(data);
			internal static void Error(object data) => logMe!.LogError(data);
			internal static void Fatal(object data) => logMe!.LogFatal(data);
			internal static void Info(object data) => logMe!.LogInfo(data);
			internal static void Message(object data) => logMe!.LogMessage(data);
			internal static void Warning(object data) => logMe!.LogWarning(data);
		}
	}
	
}