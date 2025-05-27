using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using RoR2;
using BepInEx.Configuration;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace HDeMods {
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
    public static class InterlopingArtifact {
        internal static bool musicLoaded;
        // Artifact variables
        public static AssetBundle InterBundle;
        public static readonly ArtifactDef Artifact = ScriptableObject.CreateInstance<ArtifactDef>();
        private static GameObject InterInfo;
        private static GameObject m_interInfo;
        internal static SoundAPI.Music.CustomMusicTrackDef interlopeTrack;

        // Variables exposed for Hurricane
        public static bool HurricaneRun;
        public static int StagesUntilWaveringBegins = 5;
        
        // Set up variables
        internal static bool artifactEnabled;

        // In run Loiter variables
        private static bool teleporterExists;
        internal static float tickingTimer = 1f;
        internal static float tickingTimerHalfway = 1f;
        internal static sbyte halfwayFuse;
        private static bool teleporterHit;
        internal static int totalBlindPest;
        internal static float artifactChallengeMult = 1;
        internal static bool artifactTrial;
        
#if DEBUG
        private static bool preventSpawns;
#endif

        // Config options
        public static ConfigEntry<bool> forceUnlock { get; set; }
        public static ConfigEntry<bool> disableCodeHint { get; set; }
        public static ConfigEntry<float> timeUntilLoiterPenalty { get; set; }
        public static ConfigEntry<float> loiterPenaltyFrequency { get; set; }
        public static ConfigEntry<float> loiterPenaltySeverity { get; set; }
        public static ConfigEntry<bool> limitPest { get; set; }
        public static ConfigEntry<float> limitPestAmount { get; set; }
        public static ConfigEntry<float> warningSoundVolume { get; set; }
        public static ConfigEntry<bool> useTickingNoise { get; set; }
        public static ConfigEntry<bool> enableHalfwayWarning { get; set; }
        public static ConfigEntry<float> timeBeforeLoiterPenalty { get; set; }
        public static ConfigEntry<bool> respectEnemyCap { get; set; }
        public static ConfigEntry<bool> aggressiveCulling { get; set; }
        public static ConfigEntry<float> aggressiveCullingRadius { get; set; }
        public static ConfigEntry<bool> showCullingRadius { get; set; }
        public static ConfigEntry<float> warningMusicVolume { get; set; }
        public static ConfigEntry<bool> enableOnEclipse { get; set; }

        private static void RefreshAndClamp() {
            InterlopingArtifactPlugin.instance.Config.Reload();
            timeUntilLoiterPenalty.Value = Math.Clamp(timeUntilLoiterPenalty.Value, 60f, 600f);
            loiterPenaltyFrequency.Value = Math.Clamp(loiterPenaltyFrequency.Value, 0f, 60f);
            loiterPenaltySeverity.Value = Math.Clamp(loiterPenaltySeverity.Value, 10f, 400f);
            limitPestAmount.Value = Math.Clamp(limitPestAmount.Value, 0f, 100f);
            warningSoundVolume.Value = Math.Clamp(warningSoundVolume.Value, 0f, 100f);
            warningMusicVolume.Value = Math.Clamp(warningMusicVolume.Value, 0f, 100f);
            timeBeforeLoiterPenalty.Value = Math.Clamp(timeBeforeLoiterPenalty.Value, 2f, 60f);
            aggressiveCullingRadius.Value = Math.Clamp(aggressiveCullingRadius.Value, 65f, 200f);
        }

        internal static void Startup() {
            if (!File.Exists(Assembly.GetExecutingAssembly().Location
                    .Replace("InterlopingArtifact.dll", "interloperassets"))) {
                INTER.Log.Fatal("Could not load asset bundle, aborting!");
                return;
            }

            InterBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location
                .Replace("InterlopingArtifact.dll", "interloperassets"));

            CreateNetworkObject();
            InterloperCullingZone.InitZone();
            LoadMusic();
            AddHooks();
        }

        private static void LoadMusic() {
            //INTER.Log.Fatal(Assembly.GetExecutingAssembly().Location);
            SoundAPI.Music.CustomMusicData musicData = new SoundAPI.Music.CustomMusicData();
            musicData.BanksFolderPath = Assembly.GetExecutingAssembly().Location
                .Replace("\\InterlopingArtifact.dll", "");
            musicData.BepInPlugin = InterlopingArtifactPlugin.instance.Info.Metadata;
            musicData.InitBankName = "InterlopingArtifact_init";
            musicData.PlayMusicSystemEventName = "Play_Inter_Music";
            musicData.SoundBankName = "Inter_WarningSounds";
            
            interlopeTrack = ScriptableObject.CreateInstance<SoundAPI.Music.CustomMusicTrackDef>();
            interlopeTrack.cachedName = "interlopeTrack";
            interlopeTrack.SoundBankName = musicData.SoundBankName;
            interlopeTrack.CustomStates = new List<SoundAPI.Music.CustomMusicTrackDef.CustomState>();

            SoundAPI.Music.CustomMusicTrackDef.CustomState trackState =
                new SoundAPI.Music.CustomMusicTrackDef.CustomState() {
                    GroupId = InterRefs.musGroupInter,
                    StateId = InterRefs.musStateInter
                };
            SoundAPI.Music.CustomMusicTrackDef.CustomState gameplayState =
                new SoundAPI.Music.CustomMusicTrackDef.CustomState() {
                    GroupId = InterRefs.musSystemMusic,
                    StateId = InterRefs.musStateGameplay
                };
            interlopeTrack.CustomStates.Add(trackState);
            interlopeTrack.CustomStates.Add(gameplayState);

            if (!SoundAPI.Music.Add(musicData)) {
                INTER.Log.Error("Failed to add music, disabling music options.");
                return;
            }
            musicLoaded = true;
        }

        private static void AddHooks() {
            Run.onRunSetRuleBookGlobal += Run_onRunSetRuleBookGlobal;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginStage += Run_BeginStage;
            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
            On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin += OnInteractTeleporter;
            On.RoR2.CombatDirector.Simulate += CombatDirector_Simulate;
            RoR2Application.onLoad += InterRefs.CacheIndexes;
            On.RoR2.ArtifactTrialMissionController.SetCurrentArtifact += InterArtifactTrial.CheckArtifactTrial;
            On.RoR2.ArtifactTrialMissionController.CombatState.OnEnter += InterArtifactTrial.BeginTrial;
            //InterDoNotCull.CreateHook();
            ArtifactTrialMissionController.onShellTakeDamageServer += InterArtifactTrial.OnShellTakeDamage;
            ArtifactTrialMissionController.onShellDeathServer += InterArtifactTrial.OnShellDeath;
            On.RoR2.PlatformSystems.Init += CheckForChunk;
            CharacterBody.onBodyStartGlobal += CharacterBody_onStartGlobal;
            CharacterBody.onBodyDestroyGlobal += TrackVerminRemove;
            RoR2Application.onLoad += FinalHooks;
        }

        private static void RemoveHooks() {
            Run.onRunSetRuleBookGlobal -= Run_onRunSetRuleBookGlobal;
            Run.onRunStartGlobal -= Run_onRunStartGlobal;
            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            RoR2Application.onLoad -= InterRefs.CacheIndexes;
            On.RoR2.Run.BeginStage -= Run_BeginStage;
            On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;
            On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin -= OnInteractTeleporter;
            On.RoR2.CombatDirector.Simulate -= CombatDirector_Simulate;
            //InterDoNotCull.RemoveHook();
            On.RoR2.ArtifactTrialMissionController.SetCurrentArtifact -= InterArtifactTrial.CheckArtifactTrial;
            On.RoR2.ArtifactTrialMissionController.CombatState.OnEnter -= InterArtifactTrial.BeginTrial;
            ArtifactTrialMissionController.onShellTakeDamageServer -= InterArtifactTrial.OnShellTakeDamage;
            ArtifactTrialMissionController.onShellDeathServer -= InterArtifactTrial.OnShellDeath;
            CharacterBody.onBodyStartGlobal -= CharacterBody_onStartGlobal;
            CharacterBody.onBodyDestroyGlobal -= TrackVerminRemove;
            RoR2Application.onLoad -= FinalHooks;
        }

        public static void CheckForChunk(On.RoR2.PlatformSystems.orig_Init init) {
            if (InterOptionalMods.ChunkyMode.Enabled && InterOptionalMods.ChunkyMode.PluginVersion.Minor < 4) {
                INTER.Log.Fatal(
                    "Artifact of Interloper is not compatible with ChunkyMode versions prior to 0.4.0, aborting!");
                RemoveHooks();
                init();
                return;
            }
            
            BindSettings();

            if (!AddArtifact()) {
                INTER.Log.Fatal("Could not add artifact, aborting!");
                RemoveHooks();
                init();
                return;
            }
            
            if (InterOptionalMods.RoO.Enabled) AddOptions();
            if (InterOptionalMods.ProperSaves.Enabled) InterOptionalMods.ProperSaves.SetUp();
            
            InterlopingArtifactPlugin.startupSuccess = true;
            init();
        }


        private static bool AddArtifact() {
            Artifact.nameToken = "INTERLOPINGARTIFACT_NAME";
            Artifact.descriptionToken = "INTERLOPINGARTIFACT_DESCRIPTION";
            Artifact.smallIconDeselectedSprite = InterBundle.LoadAsset<Sprite>("texInterDeselectedIcon");
            Artifact.smallIconSelectedSprite = InterBundle.LoadAsset<Sprite>("texInterSelectedIcon");
            Artifact.cachedName = "Interloper";
            
            Sha256HashAsset codeEncrypted = ScriptableObject.CreateInstance<Sha256HashAsset>();
            codeEncrypted.value =
                Sha256Hash.FromHexString("F8C1F5810E6270F0259B663AA8578FAC6D77CA599AB0F4699C5FD2C565286201");

            LanguageAPI.Add("INTERLOPINGARTIFACT_UNLOCK_NAME", "Interloper");
            Artifact.unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            Artifact.unlockableDef.nameToken = "INTERLOPINGARTIFACT_UNLOCK_NAME";
            Artifact.unlockableDef.cachedName = "Artifacts.Interloper";
            Artifact.unlockableDef.achievementIcon = InterBundle.LoadAsset<Sprite>("texObtainArtifactInterloperIcon");
            Artifact.pickupModelPrefab = InterBundle.LoadAsset<GameObject>("PickupInterloper");

            if (!ContentAddition.AddUnlockableDef(Artifact.unlockableDef)) return false;
            if (!ContentAddition.AddArtifactDef(Artifact)) return false;
            ArtifactCodeAPI.AddCode(Artifact, codeEncrypted);
            return true;
        }

        private static void BindSettings() {
            timeUntilLoiterPenalty = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Loitering",
                "Time until loiter penalty",
                300f,
                "The amount of time from the start of the stage until the loiter penalty is enforced. " +
                "Minimum of 60 seconds.");
            loiterPenaltyFrequency = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Loitering",
                "Loiter penalty frequency",
                5f,
                "The amount of time between forced enemy spawns.");
            loiterPenaltySeverity = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Loitering",
                "Loiter penalty severity",
                40f,
                "The strength of spawned enemies. 40 is equal to 1 combat shrine.");
            limitPest = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Limit Enemies",
                "Limit Blind Pest",
                false,
                "Enable Blind Pest limit.");
            limitPestAmount = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Limit Enemies",
                "Blind Pest Amount",
                10f,
                "The percentage of enemies that are allowed to be blind pest. " +
                "Only affects the Loitering penalty.");
            warningSoundVolume = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Warning",
                "Warning Sound Volume",
                100f,
                "Volume of the warning sound. Set to 0 to disable.");
            useTickingNoise = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Warning",
                "Use Ticking Sound",
                false,
                "Use a clock ticking sound instead of a bell.");
            enableHalfwayWarning = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Warning",
                "Enable Halfway Warning",
                true,
                "Play the warning sound when the loiter timer reaches half way.");
            timeBeforeLoiterPenalty = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Warning",
                "Time Before Loiter Penalty",
                15f,
                "How long before the Loiter Penalty the bells start tolling.");
            respectEnemyCap = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Enemy Cap",
                "Respect Enemy Cap",
                false,
                "Prevent additional enemies from spawning if the enemy cap is reached.");
            aggressiveCulling = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Enemy Cap",
                "Aggressive Culling",
                false,
                "Aggressively cull enemies that are not near living players.");
            aggressiveCullingRadius = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Enemy Cap",
                "Culling Radius",
                75f,
                "The distance from players before enemies are considered for culling.");
            forceUnlock = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Artifact",
                "Force Unlock",
                false,
                "Force artifact to be available. This will not grant the achievement. Requires restart.");
            showCullingRadius = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Enemy Cap",
                "Show Culling Radius",
                false,
                "Display a radius around players to indicate where enemies will be culled.");
            disableCodeHint = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Artifact",
                "Disable Code Hints",
                false,
                "Prevent artifact code hints from appearing in game. Requires restart.");
            warningMusicVolume = InterlopingArtifactPlugin.instance.Config.Bind<float>(
                "Experimental",
                "Warning Music Volume",
                0f,
                "Replace stage music with a different track when time is up. Set to 0 to disable. Requires map change to toggle.");
            enableOnEclipse = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
                "Experimental",
                "Enable on Eclipse",
                false,
                "Enables default ruleset for artifact when playing on Eclipse mode. Not recommended.");
        }

        internal static void SetVolumeEwEwEwEw() {
            AkSoundEngine.SetRTPCValue("Inter_Volume_MSX", warningMusicVolume.Value);
        }

        private static void AddOptions() {
            InterOptionalMods.RoO.Init(InterlopingArtifactPlugin.PluginGUID, InterlopingArtifactPlugin.PluginName, INTER.Log.Error, INTER.Log.Debug);
            InterOptionalMods.RoO.AddFloatStep(timeUntilLoiterPenalty, 60f, 600f, 1f, "{0}");
            InterOptionalMods.RoO.AddFloatStep(loiterPenaltyFrequency, 0f, 60f, 0.5f, "{0}");
            InterOptionalMods.RoO.AddFloatStep(loiterPenaltySeverity, 10f, 400f, 0.5f, "{0}");
            InterOptionalMods.RoO.AddButton("Reset to Default", "Loitering", InterOptionalMods.RoO.ResetToDefault);
            InterOptionalMods.RoO.AddCheck(limitPest);
            InterOptionalMods.RoO.AddFloat(limitPestAmount, 0f, 100f);
            InterOptionalMods.RoO.AddButton("Reset to Default", "Limit Enemies", InterOptionalMods.RoO.ResetToDefault);
            InterOptionalMods.RoO.AddFloatStep(warningSoundVolume, 0f, 100f, 0.5f);
            InterOptionalMods.RoO.AddCheck(useTickingNoise);
            InterOptionalMods.RoO.AddCheck(enableHalfwayWarning);
            InterOptionalMods.RoO.AddFloatStep(timeBeforeLoiterPenalty, 2f, 60f, 1f, "{0}");
            InterOptionalMods.RoO.AddButton("Reset to Default", "Warning", InterOptionalMods.RoO.ResetToDefault);
            InterOptionalMods.RoO.AddCheck(respectEnemyCap);
            InterOptionalMods.RoO.AddCheck(aggressiveCulling);
            InterOptionalMods.RoO.AddFloatStep(aggressiveCullingRadius, 65f, 200f, 0.25f, "{0}m");
            InterOptionalMods.RoO.AddCheck(showCullingRadius);
            InterOptionalMods.RoO.AddButton("Reset to Default", "Enemy Cap", InterOptionalMods.RoO.ResetToDefault);
            InterOptionalMods.RoO.AddCheck(forceUnlock, true);
            InterOptionalMods.RoO.AddCheck(disableCodeHint, true);
            InterOptionalMods.RoO.AddButton("Reset to Default", "Artifact", InterOptionalMods.RoO.ResetToDefault);
            if (musicLoaded) {
                InterOptionalMods.RoO.AddFloatStep(warningMusicVolume, 0f, 100f, 0.5f);
                InterOptionalMods.RoO.AddButton("Set Volume", "Experimental", SetVolumeEwEwEwEw);
            }
            InterOptionalMods.RoO.AddCheck(enableOnEclipse);
            InterOptionalMods.RoO.AddButton("Reset to Default", "Experimental", InterOptionalMods.RoO.ResetToDefault);
#if DEBUG
            InterOptionalMods.RoO.AddButton("Revoke Artifact", "Artifact", RevokeArtifact);
            InterOptionalMods.RoO.AddButton("Toggle Supression", "Loitering", () => {
                preventSpawns = !preventSpawns;
                if (preventSpawns) Chat.AddMessage("Interloping Spawns off");
                else Chat.AddMessage("Interloping Spawns on");
            });
            LanguageAPI.Add("RISK_OF_OPTIONS.COM.HDEDEDE.INTERLOPINGARTIFACT.LOITERING.TOGGLE_SUPRESSION.GENERIC_BUTTON.NAME",
                "Toggle Supression");
            LanguageAPI.Add("RISK_OF_OPTIONS.COM.HDEDEDE.INTERLOPINGARTIFACT.LOITERING.TOGGLE_SUPRESSION.GENERIC_BUTTON.DESCRIPTION",
                "Toggles enemy supression");
            LanguageAPI.Add("RISK_OF_OPTIONS.COM.HDEDEDE.INTERLOPINGARTIFACT.LOITERING.TOGGLE_SUPRESSION.GENERIC_BUTTON.SUB_BUTTON.NAME",
                "Toggle");
#endif
            
            InterOptionalMods.RoO.SetSprite(Artifact.unlockableDef.achievementIcon);
            InterOptionalMods.RoO.SetDescriptionToken("INTERLOPINGARTIFACT_RISK_OF_OPTIONS_DESCRIPTION");
        }
        
        public static void RevokeArtifact() {
            foreach (LocalUser user in LocalUserManager.localUsersList) {
                user.userProfile.RevokeUnlockable(Artifact.unlockableDef);
                user.userProfile.RevokeAchievement("INTERLOPER_ARTIFACT");
            }
            Application.Quit();
        }

        private static void FinalHooks() {
            RefreshAndClamp();
            if (!disableCodeHint.Value) SceneManager.activeSceneChanged += InterFormula.SceneChanged;
            if (!forceUnlock.Value) return;
            RuleCatalog.ruleChoiceDefsByGlobalName["Artifacts.Interloper.On"].requiredUnlockable = null;
            RuleCatalog.ruleChoiceDefsByGlobalName["Artifacts.Interloper.Off"].requiredUnlockable = null;
        }

        private static void CreateNetworkObject() {
            GameObject temp = new GameObject("thing");
            temp.AddComponent<NetworkIdentity>();
            InterInfo = temp.InstantiateClone("InterloperRunInfo");
            UnityEngine.Object.Destroy(temp);
            InterInfo.AddComponent<InterRunInfo>();
        }

        internal static void Run_onRunSetRuleBookGlobal(Run arg1, RuleBook arg2) {
            artifactEnabled = false;
            if (!RunArtifactManager.instance.IsArtifactEnabled(Artifact)) return;
            artifactEnabled = true;
        }

        internal static void Run_onRunStartGlobal(Run run) {
#if DEBUG
            reportErrorAnyway = true;
#endif

            teleporterHit = false;
            teleporterExists = false;
            totalBlindPest = 0;
            artifactTrial = false;

            if (!NetworkServer.active) return;
            
            //If enableOnEclipse is true, pretend this is a hurricane run when playing on eclipse 
            if (enableOnEclipse.Value && run.selectedDifficulty >= DifficultyIndex.Eclipse1) HurricaneRun = true;

            m_interInfo = UnityEngine.Object.Instantiate(InterInfo);
            NetworkServer.Spawn(m_interInfo);

            if (InterRunInfo.preSet) return;
            RefreshAndClamp();

            InterRunInfo.instance.limitPestsThisRun = limitPest.Value;
            InterRunInfo.instance.limitPestsAmountThisRun = limitPestAmount.Value;

            if (!HurricaneRun) {
                InterRunInfo.instance.loiterPenaltyTimeThisRun = timeUntilLoiterPenalty.Value;
                InterRunInfo.instance.loiterPenaltyFrequencyThisRun = loiterPenaltyFrequency.Value;
                InterRunInfo.instance.loiterPenaltySeverityThisRun = loiterPenaltySeverity.Value;
                return;
            }

            if (!artifactEnabled) {
                InterRunInfo.instance.loiterPenaltyTimeThisRun = (float)timeUntilLoiterPenalty.DefaultValue;
                InterRunInfo.instance.loiterPenaltyFrequencyThisRun = (float)loiterPenaltyFrequency.DefaultValue;
                InterRunInfo.instance.loiterPenaltySeverityThisRun = (float)loiterPenaltySeverity.DefaultValue;
                return;
            }

            InterRunInfo.instance.loiterPenaltyTimeThisRun = Math.Min(timeUntilLoiterPenalty.Value,
                (float)timeUntilLoiterPenalty.DefaultValue);
            InterRunInfo.instance.loiterPenaltyFrequencyThisRun = Math.Min(loiterPenaltyFrequency.Value,
                (float)loiterPenaltyFrequency.DefaultValue);
            InterRunInfo.instance.loiterPenaltySeverityThisRun = Math.Max(loiterPenaltySeverity.Value,
                (float)loiterPenaltySeverity.DefaultValue);
        }

        internal static void Run_onRunDestroyGlobal(Run run) {
            artifactEnabled = false;
            HurricaneRun = false;
            teleporterHit = false;
            teleporterExists = false;
            totalBlindPest = 0;
            artifactTrial = false;
            InterRunInfo.instance.StopMusic();
            InterRunInfo.preSet = false;
            UnityEngine.Object.Destroy(m_interInfo);
        }

        internal static void CharacterBody_onStartGlobal(CharacterBody body) {
            body.gameObject.AddComponent<CullingTracker>();
            if (body.bodyIndex == InterRefs.FlyingVermin) totalBlindPest++;
        }

        internal static void TrackVerminRemove(CharacterBody body) {
            if (body.bodyIndex == InterRefs.FlyingVermin) totalBlindPest--;
        }

        // Set up Loitering Punishment
        internal static void Run_BeginStage(On.RoR2.Run.orig_BeginStage beginStage, Run self) {
            if (!artifactEnabled && !HurricaneRun) {
                beginStage(self);
                return;
            }

            InterRunInfo.instance.loiterTick = 0f;
            teleporterHit = false;
            teleporterExists = false;
            halfwayFuse = 0;
            InterRunInfo.instance.allyCurse = 0;
            InterRunInfo.instance.loiterPenaltyActive = false;
            INTER.Log.Info("Stage begin! Waiting for Teleporter to be created.");
            beginStage(self);
        }

        // If a teleporter does not exist on the stage the loitering penalty should not be applied
        internal static void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced teleporterPlaced,
            Run self, SceneDirector sceneDirector, GameObject thing) {
            if ((!artifactEnabled && !HurricaneRun) || artifactTrial) {
                teleporterPlaced(self, sceneDirector, thing);
                return;
            }

            teleporterExists = true;

            float loiterTime = InterRunInfo.instance.loiterPenaltyTimeThisRun;
            if (HurricaneRun && !artifactEnabled) loiterTime -= loiterTime * 0.1f * Math.Max(self.stageClearCount - StagesUntilWaveringBegins, 0);
#if DEBUG
            if(!Mathf.Approximately(loiterTime, InterRunInfo.instance.loiterPenaltyTimeThisRun)) 
                INTER.Log.Debug($"{InterRunInfo.instance.loiterPenaltyTimeThisRun - loiterTime} seconds removed from the clock.");
#endif
            InterRunInfo.instance.stagePunishTimer = self.NetworkfixedTime + Math.Max(loiterTime, 60f);
            
            INTER.Log.Info("Teleporter created! Timer set to " + InterRunInfo.instance.stagePunishTimer);
            
            InterRunInfo.instance.RpcCalcWarningTimer();
            
            tickingTimerHalfway = InterRunInfo.instance.stagePunishTimer -
                                  (InterRunInfo.instance.stagePunishTimer - Run.instance.NetworkfixedTime) / 2;
            teleporterPlaced(self, sceneDirector, thing);
        }

        // The loitering penalty
        internal static void CombatDirector_Simulate(On.RoR2.CombatDirector.orig_Simulate simulate, CombatDirector self,
            float deltaTime) {
            if (!InterRunInfo.instance.loiterPenaltyActive || teleporterHit ||
                Run.instance.NetworkfixedTime < InterRunInfo.instance.loiterTick
                || (!artifactEnabled && !HurricaneRun && !artifactTrial)) {
                simulate(self, deltaTime);
                return;
            }
            
#if DEBUG
            if (preventSpawns) {
                INTER.Log.Error("Spawns are currently being suppressed!");
                InterRunInfo.instance.loiterTick =
                    Run.instance.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyFrequencyThisRun;
                simulate(self, deltaTime);
                return;
            }
#endif

            bool enemiesCulled = false;

            if (aggressiveCulling.Value) {
#if DEBUG
                INTER.Log.Warning("Culling Enemies!");
#endif
                foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Monster)) {
                    CullingTracker ct = tc.gameObject.GetComponent<CullingTracker>();
                    if (ct.Player || ct.isMinion) continue;
                    if (!ct.canBeCulled || tc.body.isBoss) continue;
                    tc.body.healthComponent.Die(true);
                    enemiesCulled = true;
                }
                /*foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void)) {
                    CullingTracker ct = tc.gameObject.GetComponent<CullingTracker>();
                    if (ct.Player || ct.isMinion) continue;
                    bool isInfested = tc.body.inventory.currentEquipmentIndex == InterRefs.VoidAspect || ct.voidCampSpawn;
                    if (!ct.canBeCulled || tc.body.isBoss || tc.body.bodyIndex == InterRefs.Scavenger || isInfested) continue;
                    tc.body.healthComponent.Die(true);
                    enemiesCulled = true;
                }
                foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Lunar)) {
                    CullingTracker ct = tc.gameObject.GetComponent<CullingTracker>();
                    if (ct.Player || ct.isMinion) continue;
                    if (!ct.canBeCulled || tc.body.isBoss || tc.body.bodyIndex == InterRefs.Scavenger) continue;
                    tc.body.healthComponent.Die(true);
                    enemiesCulled = true;
                }*/
            }
#if DEBUG
            INTER.Log.Warning("Attempting to spawn enemy wave");
#endif
            if (respectEnemyCap.Value && !enemiesCulled) {
                bool enemyCapReached = false;
                
                // ReSharper disable once ConvertIfToOrExpression
                if (TeamComponent.GetTeamMembers(TeamIndex.Monster).Count
                    >= TeamCatalog.GetTeamDef(TeamIndex.Monster)!.softCharacterLimit) enemyCapReached = true;
                if (TeamComponent.GetTeamMembers(TeamIndex.Void).Count
                    >= TeamCatalog.GetTeamDef(TeamIndex.Void)!.softCharacterLimit) enemyCapReached = true;
                if (TeamComponent.GetTeamMembers(TeamIndex.Lunar).Count
                    >= TeamCatalog.GetTeamDef(TeamIndex.Lunar)!.softCharacterLimit) enemyCapReached = true;

                if (enemyCapReached) {
#if DEBUG
                INTER.Log.Warning("Too many enemies are present, skipping loiter tick.");
#endif 
                    InterRunInfo.instance.loiterTick =
                        Run.instance.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyFrequencyThisRun;
                    simulate(self, deltaTime);
                    return;
                }
            }
            
            
            float gougeCount = artifactChallengeMult;

            if (artifactEnabled && HurricaneRun)
                gougeCount += Util.GetItemCountForTeam(TeamIndex.Player,
                    RoR2Content.Items.MonstersOnShrineUse.itemIndex, false);

            float newCreditBalance = InterRunInfo.instance.loiterPenaltySeverityThisRun *
                                     Stage.instance.entryDifficultyCoefficient * gougeCount;
            float oldTimer = self.monsterSpawnTimer - deltaTime;
            DirectorCard oldEnemy = self.currentMonsterCard;
            DirectorCard newEnemy = self.SelectMonsterCardForCombatShrine(newCreditBalance);

            if (newEnemy == null) {
#if DEBUG
                INTER.Log.Error("Invalid enemy. Retrying next update.");
#endif
                simulate(self, deltaTime);
                return;
            }
#if DEBUG
            INTER.Log.Warning("Checking if " + newEnemy.spawnCard.prefab.name + " is a Blind Pest.");
#endif
            if (newEnemy.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab ==
                BodyCatalog.GetBodyPrefab(InterRefs.FlyingVermin) && InterRunInfo.instance.limitPestsThisRun) {
#if DEBUG
                INTER.Log.Warning("Blind Pest detected, checking if we have too many.");
#endif
                int totalEnemies = 0;

                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Monster).Count;
                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Void).Count;
                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Lunar).Count;

#if DEBUG
                INTER.Log.Warning("Total enemies: " + totalEnemies);
                INTER.Log.Warning("Too many? " +
                                  (totalBlindPest >=
                                   totalEnemies * (InterRunInfo.instance.limitPestsAmountThisRun / 100f)));
#endif
                if (totalBlindPest >= totalEnemies * (InterRunInfo.instance.limitPestsAmountThisRun / 100f)) {
                    INTER.Log.Warning("Too many Blind Pest. Retrying in the next update.");
                    simulate(self, deltaTime);
                    return;
                }
            }
#if DEBUG
            INTER.Log.Debug("Spawning enemy wave");
#endif
            InterRunInfo.instance.loiterTick =
                Run.instance.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyFrequencyThisRun;
            if (artifactEnabled && HurricaneRun) {
                InterRunInfo.instance.allyCurse += 0.035f;
                InterRunInfo.instance.RpcDirtyStats();
            }

            //Thank you .score for pointing out CombatDirector.CombatShrineActivation
            self.monsterSpawnTimer = 0f;
            self.monsterCredit = +newCreditBalance;
            self.OverrideCurrentMonsterCard(newEnemy);

            simulate(self, deltaTime);

            self.monsterSpawnTimer = oldTimer;
            if (oldEnemy != null) self.OverrideCurrentMonsterCard(oldEnemy);
        }

        // Disable loitering penalty when the teleporter is interacted with
        // ReSharper disable once IdentifierTypo
        internal static void OnInteractTeleporter(
            On.RoR2.TeleporterInteraction.IdleState.orig_OnInteractionBegin interact,
            EntityStates.BaseState teleporterState, Interactor interactor) {
            if (!artifactEnabled && !HurricaneRun) {
                interact(teleporterState, interactor);
                return;
            }

            InterRunInfo.instance.loiterPenaltyActive = false;
            teleporterHit = true;
            InterRunInfo.instance.allyCurse = 0f;
            InterRunInfo.instance.RpcDirtyStats();
            InterRunInfo.instance.RpcStopMusic();
            interact(teleporterState, interactor);
        }

        // Enforcing loitering penalty
        internal static void EnforceLoiter() {
            if (!artifactEnabled && !HurricaneRun) return;

            if (Run.instance.isGameOverServer) {
#if DEBUG
                ReportLoiterError("Game Over");
#endif
                return;
            }

            if (artifactTrial) {
#if DEBUG
                ReportLoiterError("In Artifact Trial");
#endif
                return;
            }

            if (teleporterHit) {
#if DEBUG
                ReportLoiterError("Teleporter hit");
#endif
                return;
            }

            if (!teleporterExists) {
#if DEBUG
                ReportLoiterError("Teleporter does not exist");
#endif
                return;
            }

            if (InterRunInfo.instance.stagePunishTimer >= Run.instance.NetworkfixedTime) {
#if DEBUG
                ReportLoiterError("Not time yet");
#endif
                if (Run.instance.NetworkfixedTime >= tickingTimer) {
                    tickingTimer += 1f;
                    InterRunInfo.instance.PlayWarningSound();
                }
                
                if (!NetworkServer.active) return;

                if (Run.instance.NetworkfixedTime >= tickingTimerHalfway && halfwayFuse < 3) {
                    tickingTimerHalfway += 1f;
                    halfwayFuse += 1;
                    InterRunInfo.instance.RpcPlayHalfwaySound();
                }
                return;
            }
            
            if (InterRunInfo.instance.loiterPenaltyActive) {
#if DEBUG
                ReportLoiterError("Time's up");
#endif
                return;
            }
            
            if (!NetworkServer.active) {
#if DEBUG
                ReportLoiterError("Client can not enforce loiter penalty");
#endif
                return;
            }

            INTER.Log.Info("Time's up! Loitering penalty has been applied. StagePunishTimer " +
                           InterRunInfo.instance.stagePunishTimer);
            InterRunInfo.instance.loiterPenaltyActive = true;
            halfwayFuse = 0;
#if DEBUG
            INTER.Log.Debug("Warning now");
#endif
            InterRunInfo.instance.RpcPlayFinalSound();
            Chat.SendBroadcastChat(new Chat.NpcChatMessage() {
                baseToken = "INTERLOPINGARTIFACT_WARNING",
                formatStringToken = "INTERLOPINGARTIFACT_WARNING_FORMAT",
                sender = null,
                sound = null
            });
        }

#if DEBUG
        // Report why loitering hasn't been enabled every 5 seconds
        private static float reportErrorTime;
        private static bool reportErrorAnyway;
        private static void ReportLoiterError(string err) {
            if (reportErrorTime >= Run.instance.NetworkfixedTime && !reportErrorAnyway) return;
            INTER.Log.Debug(err);
            reportErrorTime = Run.instance.NetworkfixedTime + 5f;
            reportErrorAnyway = false;
        }
#endif
    }
}