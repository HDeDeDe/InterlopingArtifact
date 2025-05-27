using RoR2;
using System.Runtime.Serialization;
using UnityEngine.Networking;

namespace HDeMods {
    public class InterRunInfo : NetworkBehaviour {
        public static InterRunInfo instance;

        // This should only be true if ProperSave is present and added settings
        public static bool preSet;
        internal static InterSaveData saveData;
        internal MusicTrackOverride trackOverride;

        public float loiterPenaltyTimeThisRun;
        public float loiterPenaltyFrequencyThisRun;
        public float loiterPenaltySeverityThisRun;
        public bool limitPestsThisRun;
        public float limitPestsAmountThisRun;

        //These values are only synced not saved
        [SyncVar] public float allyCurse;
        [SyncVar] public float loiterTick;
        [SyncVar] public bool loiterPenaltyActive;
        [SyncVar] public float stagePunishTimer;

        public void Awake() {
            instance = this;
            DontDestroyOnLoad(this);
            trackOverride = gameObject.AddComponent<MusicTrackOverride>();
            trackOverride.enabled = false;
            trackOverride.track = InterlopingArtifact.interlopeTrack;
            trackOverride.priority = 10;
            if (!preSet) return;
            loiterPenaltyTimeThisRun = saveData.loiterPenaltyTimeThisRun;
            loiterPenaltyFrequencyThisRun = saveData.loiterPenaltyFrequencyThisRun;
            loiterPenaltySeverityThisRun = saveData.loiterPenaltySeverityThisRun;
            limitPestsThisRun = saveData.limitPestsThisRun;
            limitPestsAmountThisRun = saveData.limitPestsAmountThisRun;
        }

        [ClientRpc] public void RpcPlayFinalSound() => PlayFinalSound();
        [ClientRpc] public void RpcPlayHalfwaySound() => PlayHalfwaySound();

        public void PlayHalfwaySound() {
            if (!InterlopingArtifact.enableHalfwayWarning.Value) return;
            AkSoundEngine.SetRTPCValue("Inter_Volume_SFX", InterlopingArtifact.warningSoundVolume.Value);
#if DEBUG
            INTER.Log.Warning("Playing sound!");
#endif
            if (InterlopingArtifact.useTickingNoise.Value)
                AkSoundEngine.PostEvent(InterRefs.sfxTickTock, InterlopingArtifactPlugin.instance.gameObject);
            else AkSoundEngine.PostEvent(InterRefs.sfxBellToll, InterlopingArtifactPlugin.instance.gameObject);
        }

        public void PlayWarningSound() {
            AkSoundEngine.SetRTPCValue("Inter_Volume_SFX", InterlopingArtifact.warningSoundVolume.Value);
#if DEBUG
            INTER.Log.Warning("Playing sound!");
#endif
            if (InterlopingArtifact.useTickingNoise.Value)
                AkSoundEngine.PostEvent(InterRefs.sfxTickTock, InterlopingArtifactPlugin.instance.gameObject);
            else AkSoundEngine.PostEvent(InterRefs.sfxBellToll, InterlopingArtifactPlugin.instance.gameObject);
        }

        public void PlayFinalSound() {
            AkSoundEngine.SetRTPCValue("Inter_Volume_SFX", InterlopingArtifact.warningSoundVolume.Value);
#if DEBUG
            INTER.Log.Warning("Playing sound!");
#endif
            if (InterlopingArtifact.useTickingNoise.Value)
                AkSoundEngine.PostEvent(InterRefs.sfxGetOut, InterlopingArtifactPlugin.instance.gameObject);
            else AkSoundEngine.PostEvent(InterRefs.sfxBellFinal, InterlopingArtifactPlugin.instance.gameObject);
            
            if (InterlopingArtifact.warningMusicVolume.Value <= 0f || !InterlopingArtifact.musicLoaded) return;
#if DEBUG
            INTER.Log.Warning("Playing music!");
#endif
            InterlopingArtifact.SetVolumeEwEwEwEw();
            trackOverride.enabled = true;
        }

        [ClientRpc] public void RpcDirtyStats() => DirtyStats();

        public void DirtyStats() {
            foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Player))
                teamComponent.body.MarkAllStatsDirty();
        }
        
        [ClientRpc] public void RpcCalcWarningTimer() => CalcWarningTimer();

        public void CalcWarningTimer() {
            InterlopingArtifact.tickingTimer = instance.stagePunishTimer - InterlopingArtifact.timeBeforeLoiterPenalty.Value;
        }

        [ClientRpc] public void RpcStopMusic() => StopMusic();

        public void StopMusic() {
            trackOverride.enabled = false;
        }
    }

    internal struct InterSaveData {
        [DataMember(Name = "validCheck")] public bool isValidSave;
        [DataMember(Name = "loiterTime")] public float loiterPenaltyTimeThisRun;
        [DataMember(Name = "loiterFrequency")] public float loiterPenaltyFrequencyThisRun;
        [DataMember(Name = "loiterSeverity")] public float loiterPenaltySeverityThisRun;
        [DataMember(Name = "limitPest")] public bool limitPestsThisRun;
        [DataMember(Name = "pestCount")] public float limitPestsAmountThisRun;
    }
}