using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RoR2;
using UnityEngine;

namespace HDeMods {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    internal static class InterRefs {
        internal const uint sfxTickTock = 1626947733;
        internal const uint sfxGetOut = 3106412111;
        internal const uint sfxBellToll = 2239523887;
        internal const uint sfxBellFinal = 1844540516;
        internal static BodyIndex FlyingVermin;
        internal static BodyIndex Gup;
        internal static BodyIndex Geep;
        internal static BodyIndex [] VeryBigMen;
        internal static BodyIndex[] BigMen;
        //internal static EquipmentIndex VoidAspect = EquipmentIndex.None;
        internal static readonly int remapTex = Shader.PropertyToID("_RemapTex");
        internal static readonly int softFactor = Shader.PropertyToID("_InvFade");
        internal static readonly int softPower = Shader.PropertyToID("_SoftPower");
        internal static readonly int brightnessBoost = Shader.PropertyToID("_Boost");
        internal static readonly int alphaBoost = Shader.PropertyToID("_AlphaBoost");
        internal static readonly int intersectionStrength = Shader.PropertyToID("_IntersectionStrength");

        internal static void CacheIndexes() {
            FlyingVermin = BodyCatalog.FindBodyIndex("FlyingVerminBody");
            Gup = BodyCatalog.FindBodyIndex("GupBody");
            Geep = BodyCatalog.FindBodyIndex("GeepBody");
            
            VeryBigMen = new[] {
                BodyCatalog.FindBodyIndex("ScavBody"),
                BodyCatalog.FindBodyIndex("ElectricWormBody"),
                BodyCatalog.FindBodyIndex("ArtifactShellBody"),
                BodyCatalog.FindBodyIndex("BrotherBody"),
                BodyCatalog.FindBodyIndex("BrotherHurtBody"),
                BodyCatalog.FindBodyIndex("FalseSonBossBody"),
                BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyBase"),
                BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase3"),
                BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase2"),
                BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase1"),
                BodyCatalog.FindBodyIndex("ScavLunar1Body"),
                BodyCatalog.FindBodyIndex("ScavLunar2Body"),
                BodyCatalog.FindBodyIndex("ScavLunar3Body"),
                BodyCatalog.FindBodyIndex("ScavLunar4Body"),
                BodyCatalog.FindBodyIndex("TitanGoldBody"),
                BodyCatalog.FindBodyIndex("SuperRoboBallBossBody"),
            };
            
            BigMen = new[] {
                BodyCatalog.FindBodyIndex("VoidJailerBody"),
                BodyCatalog.FindBodyIndex("VoidDevastatorBody"),
                BodyCatalog.FindBodyIndex("LunarGolemBody"),
                BodyCatalog.FindBodyIndex("LunarWhispBody"),
                BodyCatalog.FindBodyIndex("BeetleQueenBody"),
                BodyCatalog.FindBodyIndex("TitanBody"),
                BodyCatalog.FindBodyIndex("ClayBossBody"),
                BodyCatalog.FindBodyIndex("MagmaWormBody"),
                BodyCatalog.FindBodyIndex("ImpBossBody"),
                BodyCatalog.FindBodyIndex("RoboBallBossBody"),
                BodyCatalog.FindBodyIndex("MegaConstructBody"),
                BodyCatalog.FindBodyIndex("GrandparentBody"),
                BodyCatalog.FindBodyIndex("GupBody"),
            };
            
            //VoidAspect = EquipmentCatalog.FindEquipmentIndex(DLC1Content.Elites.Void.eliteEquipmentDef.nameToken);
        }

        internal static bool IsBigMan(BodyIndex body) => BigMen.Any(index => index == body);
        internal static bool IsVeryBigMan(BodyIndex body) => VeryBigMen.Any(index => index == body);
    }
}