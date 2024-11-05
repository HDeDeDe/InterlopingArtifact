using RoR2;
using UnityEngine.Networking;

namespace HDeMods {
    internal static class InterArtifactTrial {
        internal static void CheckArtifactTrial(
            On.RoR2.ArtifactTrialMissionController.orig_SetCurrentArtifact setCurrentArtifact,
            ArtifactTrialMissionController self, int artifact) {
            if ((ArtifactIndex)artifact == InterlopingArtifact.Artifact.artifactIndex && NetworkServer.active) {
#if DEBUG
                INTER.Log.Warning("Interloper Artifact Trial Entered!");
#endif
                InterlopingArtifact.artifactTrial = true;
            }


            setCurrentArtifact(self, artifact);
        }

        internal static void BeginTrial(On.RoR2.ArtifactTrialMissionController.CombatState.orig_OnEnter onEnter,
            EntityStates.EntityState self) {
            if (InterlopingArtifact.artifactTrial && NetworkServer.active) {
#if DEBUG
                INTER.Log.Warning("Interloper Artifact Trial Started!");
#endif
                InterRunInfo.instance.loiterPenaltyActive = true;
            }

            onEnter(self);
        }

        internal static void OnShellTakeDamage(ArtifactTrialMissionController mc, DamageReport dr) {
            if (!InterlopingArtifact.artifactTrial || !NetworkServer.active) return;
#if DEBUG
            INTER.Log.Warning("Incrementing severity.");
#endif
            InterlopingArtifact.artifactChallengeMult += 1;
        }

        internal static void OnShellDeath(ArtifactTrialMissionController mc, DamageReport dr) {
            if (!InterlopingArtifact.artifactTrial || !NetworkServer.active) return;
#if DEBUG
            INTER.Log.Warning("Trial Complete!");
#endif
            InterlopingArtifact.artifactChallengeMult = 1;
            InterRunInfo.instance.loiterPenaltyActive = false;
            InterlopingArtifact.artifactTrial = false;
        }
    }
}