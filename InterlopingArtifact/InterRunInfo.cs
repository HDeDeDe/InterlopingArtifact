using RoR2;
using System.Runtime.Serialization;
using UnityEngine.Networking;

namespace HDeMods {
	internal class InterRunInfo : NetworkBehaviour {
		public static InterRunInfo instance;
		// This should only be true if ProperSave is present and added settings
		public static bool preSet;
		public static InterSaveData saveData;
		
		[SyncVar]
		public float loiterPenaltyTimeThisRun;
		[SyncVar]
		public float loiterPenaltyFrequencyThisRun;
		[SyncVar]
		public float loiterPenaltySeverityThisRun;
		[SyncVar]
		public bool limitPestsThisRun;
		[SyncVar]
		public float limitPestsAmountThisRun;
		//These values are only synced not saved
		[SyncVar]
		public float allyCurse;
		[SyncVar]
		public float loiterTick;
		[SyncVar] 
		public bool loiterPenaltyActive;
		[SyncVar] 
		public float stagePunishTimer;

		public void Awake() {
			instance = this;
			DontDestroyOnLoad(this);
			if (!preSet) return;
			loiterPenaltyTimeThisRun = saveData.loiterPenaltyTimeThisRun;
			loiterPenaltyFrequencyThisRun = saveData.loiterPenaltyFrequencyThisRun;
			loiterPenaltySeverityThisRun = saveData.loiterPenaltySeverityThisRun;
			limitPestsThisRun = saveData.limitPestsThisRun;
			limitPestsAmountThisRun = saveData.limitPestsAmountThisRun;
		}
		
		[ClientRpc]
		public void RpcPlayTickTock() {
			PlayTickTock();
		}

		public void PlayTickTock() {
			if (InterlopingArtifact.playTickingSound.Value) {
#if DEBUG
				INTER.Log.Warning("Playing Sound!");
#endif
				foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Player)) {
					if (InterlopingArtifact.tock) AkSoundEngine.PostEvent(InterRefs.sfxTock, teamComponent.gameObject);
					else AkSoundEngine.PostEvent(InterRefs.sfxTick, teamComponent.gameObject);
				}
				
			}
			InterlopingArtifact.tock = !InterlopingArtifact.tock;
		}
		
		[ClientRpc]
		public void RpcSetTickTock() {
			SetTickTock();
		}

		public void SetTickTock() {
			InterlopingArtifact.tickingTimer = instance.stagePunishTimer - 15f;
		}
		
		[ClientRpc]
		public void RpcDirtyStats() {
			DirtyStats();
		}

		public void DirtyStats() {
			foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Player)) {
				teamComponent.body.MarkAllStatsDirty();
			}
		}
	}

	internal struct InterSaveData {
		[DataMember(Name = "validCheck")]
		public bool isValidSave;
		[DataMember(Name = "loiterTime")]
		public float loiterPenaltyTimeThisRun;
		[DataMember(Name = "loiterFrequency")]
		public float loiterPenaltyFrequencyThisRun;
		[DataMember(Name = "loiterSeverity")]
		public float loiterPenaltySeverityThisRun;
		[DataMember(Name = "limitPest")]
		public bool limitPestsThisRun;
		[DataMember(Name = "pestCount")]
		public float limitPestsAmountThisRun;
	}
}