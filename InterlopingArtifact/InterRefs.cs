using System.Diagnostics.CodeAnalysis;
using RoR2;

namespace HDeMods {
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal static class InterRefs {
		internal const uint sfxTickTock = 1626947733;
		internal static BodyIndex FlyingVermin;

		internal static void CacheBlindPest() {
			FlyingVermin = BodyCatalog.FindBodyIndex("FlyingVerminBody");
		}
	}
}