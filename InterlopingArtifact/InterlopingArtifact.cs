using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using RoR2;
using BepInEx.Configuration;
using R2API;
using UnityEngine;
using UnityEngine.Networking;

namespace HDeMods {
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class InterlopingArtifact {
        // Artifact variables
        public static AssetBundle InterBundle;
        public static readonly ArtifactDef Artifact = ScriptableObject.CreateInstance<ArtifactDef>();
        private static GameObject InterInfo;
        private static GameObject m_interInfo;

        // Set up variables
        public static bool HurricaneRun;
        private static bool artifactEnabled;

        // In run Loiter variables
        private static bool teleporterExists;
        internal static float tickingTimer = 1f;
        internal static float tickingTimerHalfway = 1f;
        internal static sbyte halfwayFuse;
        private static bool teleporterHit;
        internal static int totalBlindPest;
        internal static int artifactChallengeMult = 1;
        internal static bool artifactTrial;

        // Config options
        public static ConfigEntry<float> timeUntilLoiterPenalty { get; set; }
        public static ConfigEntry<float> loiterPenaltyFrequency { get; set; }
        public static ConfigEntry<float> loiterPenaltySeverity { get; set; }
        public static ConfigEntry<bool> limitPest { get; set; }
        public static ConfigEntry<float> limitPestAmount { get; set; }
        public static ConfigEntry<float> warningSoundVolume { get; set; }
        public static ConfigEntry<bool> useTickingNoise { get; set; }
        public static ConfigEntry<bool> enableHalfwayWarning { get; set; }
        public static ConfigEntry<float> timeBeforeLoiterPenalty { get; set; }

        internal static void Startup() {
            if (!File.Exists(Assembly.GetExecutingAssembly().Location
                    .Replace("InterlopingArtifact.dll", "interloperassets"))) {
                INTER.Log.Fatal("Could not load asset bundle, aborting!");
                return;
            }

            InterBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location
                .Replace("InterlopingArtifact.dll", "interloperassets"));

            CreateNetworkObject();
            AddHooks();
        }

        private static void AddHooks() {
            Run.onRunSetRuleBookGlobal += Run_onRunSetRuleBookGlobal;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginStage += Run_BeginStage;
            On.RoR2.Run.EndStage += Run_EndStage;
            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
            On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin += OnInteractTeleporter;
            On.RoR2.CombatDirector.Simulate += CombatDirector_Simulate;
            RoR2Application.onLoad += InterRefs.CacheBlindPest;
            On.RoR2.ArtifactTrialMissionController.SetCurrentArtifact += InterArtifactTrial.CheckArtifactTrial;
            On.RoR2.ArtifactTrialMissionController.CombatState.OnEnter += InterArtifactTrial.BeginTrial;
            ArtifactTrialMissionController.onShellTakeDamageServer += InterArtifactTrial.OnShellTakeDamage;
            ArtifactTrialMissionController.onShellDeathServer += InterArtifactTrial.OnShellDeath;
            On.RoR2.RoR2Content.Init += CheckForChunk;
            CharacterBody.onBodyStartGlobal += TrackVerminAdd;
            CharacterBody.onBodyDestroyGlobal += TrackVerminRemove;
        }

        private static void RemoveHooks() {
            Run.onRunSetRuleBookGlobal -= Run_onRunSetRuleBookGlobal;
            Run.onRunStartGlobal -= Run_onRunStartGlobal;
            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            RoR2Application.onLoad -= InterRefs.CacheBlindPest;
            On.RoR2.Run.BeginStage -= Run_BeginStage;
            On.RoR2.Run.EndStage -= Run_EndStage;
            On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;
            On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin -= OnInteractTeleporter;
            On.RoR2.CombatDirector.Simulate -= CombatDirector_Simulate;
            On.RoR2.ArtifactTrialMissionController.SetCurrentArtifact -= InterArtifactTrial.CheckArtifactTrial;
            On.RoR2.ArtifactTrialMissionController.CombatState.OnEnter -= InterArtifactTrial.BeginTrial;
            ArtifactTrialMissionController.onShellTakeDamageServer -= InterArtifactTrial.OnShellTakeDamage;
            ArtifactTrialMissionController.onShellDeathServer -= InterArtifactTrial.OnShellDeath;
            CharacterBody.onBodyStartGlobal -= TrackVerminAdd;
            CharacterBody.onBodyDestroyGlobal -= TrackVerminRemove;
        }

        public static void CheckForChunk(On.RoR2.RoR2Content.orig_Init init) {
            if (InterOptionalMods.ChunkyMode.Enabled && InterOptionalMods.ChunkyMode.PluginVersion.Minor < 4) {
                INTER.Log.Fatal(
                    "Artifact of Interloper is not compatible with ChunkyMode versions prior to 0.4.0, aborting!");
                RemoveHooks();
                init();
                return;
            }

            if (!AddArtifact()) {
                INTER.Log.Fatal("Could not add artifact, aborting!");
                RemoveHooks();
                init();
                return;
            }

            BindSettings();
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
                "The amount of time from the start of the stage until the loiter penalty is enforced. Minimum of 60 seconds.");
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
                "The percentage of enemies that are allowed to be blind pest. Only affects the Loitering penalty.");
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
            if (!InterOptionalMods.RoO.Enabled) return;
            InterOptionalMods.RoO.AddFloat(timeUntilLoiterPenalty, 60f, 600f, "{0}");
            InterOptionalMods.RoO.AddFloat(loiterPenaltyFrequency, 0f, 60f, "{0}");
            InterOptionalMods.RoO.AddFloat(loiterPenaltySeverity, 10f, 100f);
            InterOptionalMods.RoO.AddCheck(limitPest);
            InterOptionalMods.RoO.AddFloat(limitPestAmount, 0f, 100f);
            InterOptionalMods.RoO.AddFloatStep(warningSoundVolume, 0f, 100f, 0.5f);
            InterOptionalMods.RoO.AddCheck(useTickingNoise);
            InterOptionalMods.RoO.AddCheck(enableHalfwayWarning);
            InterOptionalMods.RoO.AddFloat(timeBeforeLoiterPenalty, 2f, 60f, "{0}");
            InterOptionalMods.RoO.SetSprite(Artifact.unlockableDef.achievementIcon);
            InterOptionalMods.RoO.SetDescriptionToken("INTERLOPINGARTIFACT_RISK_OF_OPTIONS_DESCRIPTION");
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

            m_interInfo = UnityEngine.Object.Instantiate(InterInfo);
            NetworkServer.Spawn(m_interInfo);

            if (InterRunInfo.preSet) return;

            InterRunInfo.instance.limitPestsThisRun = limitPest.Value;
            InterRunInfo.instance.limitPestsAmountThisRun = limitPestAmount.Value;

            if (!HurricaneRun) {
                InterRunInfo.instance.loiterPenaltyTimeThisRun = timeUntilLoiterPenalty.Value;
                InterRunInfo.instance.loiterPenaltyFrequencyThisRun = loiterPenaltyFrequency.Value;
                InterRunInfo.instance.loiterPenaltySeverityThisRun = loiterPenaltySeverity.Value;
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
            InterRunInfo.preSet = false;
            UnityEngine.Object.Destroy(m_interInfo);
        }

        internal static void TrackVerminAdd(CharacterBody body) {
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

        // Fix for artifact trial
        //TODO: THAT DOESN'T WORK LMFAO
        internal static void Run_EndStage(On.RoR2.Run.orig_EndStage endStage, Run self) {
            artifactTrial = false;
            endStage(self);
        }

        // If a teleporter does not exist on the stage the loitering penalty should not be applied
        internal static void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced teleporterPlaced,
            Run self, SceneDirector sceneDirector, GameObject thing) {
            if ((!artifactEnabled && !HurricaneRun) || artifactTrial) {
                teleporterPlaced(self, sceneDirector, thing);
                return;
            }

            teleporterExists = true;
            InterRunInfo.instance.stagePunishTimer =
                self.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyTimeThisRun;
            INTER.Log.Info("Teleporter created! Timer set to " + InterRunInfo.instance.stagePunishTimer);
            tickingTimer = InterRunInfo.instance.stagePunishTimer - 15f;
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
            INTER.Log.Warning("Attempting to spawn enemy wave");
#endif
            int gougeCount = artifactChallengeMult;

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

            if (!NetworkServer.active) {
#if DEBUG
                ReportLoiterError("Client can not enforce loiter penalty");
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

            if (InterRunInfo.instance.loiterPenaltyActive) {
#if DEBUG
                ReportLoiterError("Time's up");
#endif
                return;
            }

            if (InterRunInfo.instance.stagePunishTimer >= Run.instance.NetworkfixedTime) {
                if (Run.instance.NetworkfixedTime >= tickingTimer) {
                    tickingTimer += 1f;
                    InterRunInfo.instance.RpcPlayWarningSound();
                }

                if (Run.instance.NetworkfixedTime >= tickingTimerHalfway && halfwayFuse < 3) {
                    tickingTimerHalfway += 1f;
                    halfwayFuse += 1;
                    InterRunInfo.instance.RpcPlayHalfwaySound();
                }
#if DEBUG
                ReportLoiterError("Not time yet");
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