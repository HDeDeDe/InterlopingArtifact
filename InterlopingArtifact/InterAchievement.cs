using RoR2;
using RoR2.Achievements.Artifacts;

namespace HDeMods {
    [RegisterAchievement("INTERLOPER_ARTIFACT", "Artifacts.Interloper", "", 3)]
    public class InterAchievement : BaseObtainArtifactAchievement {
        public override ArtifactDef artifactDef => InterlopingArtifact.Artifact;
    }
}