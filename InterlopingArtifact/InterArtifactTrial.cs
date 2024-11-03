using RoR2;

namespace HDeMods {
	internal static class InterArtifactTrial {
		internal static void CheckArtifactTrial(On.RoR2.ArtifactTrialMissionController.orig_SetCurrentArtifact setCurrentArtifact,
			ArtifactTrialMissionController self, int artifact) {
			if ((ArtifactIndex)artifact == InterlopingArtifact.Artifact.artifactIndex) {
#if DEBUG
				INTER.Log.Warning("Interloper Artifact Trial Entered!");
#endif
				InterlopingArtifact.artifactTrial = true;
			}
				

			setCurrentArtifact(self, artifact);
		}
		
		internal static void BeginTrial(On.RoR2.ArtifactTrialMissionController.CombatState.orig_OnEnter onEnter,
			EntityStates.EntityState self) {
			if (InterlopingArtifact.artifactTrial) {
#if DEBUG
				INTER.Log.Warning("Interloper Artifact Trial Started!");
#endif
				InterRunInfo.instance.loiterPenaltyActive = true;
			}
			
			onEnter(self);
		}

		internal static void OnShellTakeDamage(ArtifactTrialMissionController mc, DamageReport dr) {
			if (!InterlopingArtifact.artifactTrial) return;
#if DEBUG
            INTER.Log.Warning("Incrementing severity.");
#endif
			InterlopingArtifact.artifactChallengeMult += 1;
		}

		internal static void OnShellDeath(ArtifactTrialMissionController mc, DamageReport dr) {
			if (!InterlopingArtifact.artifactTrial) return;
#if DEBUG
			INTER.Log.Warning("Trial Complete!");
#endif
			InterlopingArtifact.artifactChallengeMult = 1;
			InterRunInfo.instance.loiterPenaltyActive = false;
		}
	}
}