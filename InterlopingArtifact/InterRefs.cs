using RoR2;

namespace HDeMods {
	internal static class InterRefs {
		internal const uint sfxTock = 255157130;
		internal const uint sfxTick = 4014226528;
		internal static BodyIndex FlyingVermin;

		internal static void CacheBlindPest() {
			FlyingVermin = BodyCatalog.FindBodyIndex("FlyingVerminBody");
		}
	}
}