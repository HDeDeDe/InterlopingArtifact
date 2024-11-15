using System.Diagnostics.CodeAnalysis;
using RoR2;
using UnityEngine;

namespace HDeMods {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class InterRefs {
        internal const uint sfxTickTock = 1626947733;
        internal const uint sfxGetOut = 3106412111;
        internal const uint sfxBellToll = 2239523887;
        internal const uint sfxBellFinal = 1844540516;
        internal static BodyIndex FlyingVermin;
        internal static EquipmentIndex VoidAspect = EquipmentIndex.None;
        internal static readonly int remapTex = Shader.PropertyToID("_RemapTex");

        internal static void CacheIndexes() {
            FlyingVermin = BodyCatalog.FindBodyIndex("FlyingVerminBody");
            VoidAspect = EquipmentCatalog.FindEquipmentIndex(DLC1Content.Elites.Void.eliteEquipmentDef.nameToken);
        }
    }
}