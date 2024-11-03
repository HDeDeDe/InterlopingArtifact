using RoR2;
using RoR2.Achievements;
using RoR2.Achievements.Artifacts;

namespace HDeMods {
	[RegisterAchievement("INTER_ARTIFACT_ACHIEVEMENT", "Artifacts.Interloper", "", 3)]
	public class InterAchievement : BaseObtainArtifactAchievement {
		public override ArtifactDef artifactDef => InterlopingArtifact.Artifact;
	}
}