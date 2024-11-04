using System.Diagnostics.CodeAnalysis;
using RoR2;

namespace HDeMods {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class InterRefs {
        internal const uint sfxTickTock = 1626947733;
        internal const uint sfxGetOut = 3106412111;
        internal const uint sfxBellToll = 2239523887;
        internal const uint sfxBellFinal = 1844540516;
        internal static BodyIndex FlyingVermin;

        internal static void CacheBlindPest() {
            FlyingVermin = BodyCatalog.FindBodyIndex("FlyingVerminBody");
        }
    }
}