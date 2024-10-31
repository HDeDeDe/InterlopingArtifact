using System;

namespace HDeMods { namespace InterOptionalMods {
    internal static class ChunkyMode {
        public static bool Enabled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.HDeDeDe.ChunkyMode");

        public static Version PluginVersion =>
            BepInEx.Bootstrap.Chainloader.PluginInfos["com.HDeDeDe.ChunkyMode"].Metadata.Version;
    }
} }