using System;
using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Logging;
using R2API;
using UnityEngine;
using RoR2;

namespace HDeMods {
    [BepInDependency(DirectorAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(SoundAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(ArtifactCodeAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public sealed class InterlopingArtifactPlugin : BaseUnityPlugin {
        public const string PluginGUID = "com." + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "HDeDeDe";
        public const string PluginName = "InterlopingArtifact";
        public const string PluginVersion = "0.1.0";

        public static InterlopingArtifactPlugin instance;
        public static bool startupSuccess = false;

        private void Awake() {
            if (instance != null) {
                INTER.Log.Error("Only one instance of InterlopingArtifactPlugin can exist at a time!");
                Destroy(this);
                return;
            }

            instance = this;
            INTER.Log.Init(Logger);
            InterlopingArtifact.Startup();
        }

        private void FixedUpdate() {
            InterlopingArtifact.EnforceLoiter();
        }
    }

    namespace INTER {
        internal static class Log {
            private static ManualLogSource logMe;

            internal static void Init(ManualLogSource logSource) => logMe = logSource;
            internal static void Debug(object data) => logMe!.LogDebug(data);
            internal static void Error(object data) => logMe!.LogError(data);
            internal static void Fatal(object data) => logMe!.LogFatal(data);
            internal static void Info(object data) => logMe!.LogInfo(data);
            internal static void Message(object data) => logMe!.LogMessage(data);
            internal static void Warning(object data) => logMe!.LogWarning(data);
        }
    }
}