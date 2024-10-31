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
        public static bool ChunkyModeRun;
        private static bool shouldRun;
        
        // In run Loiter variables
        private static bool teleporterExists;
        internal static float tickingTimer;
        internal static bool tock;
        private static bool teleporterHit;
        internal static int totalBlindPest;
		
		// Config options
		public static ConfigEntry<float> timeUntilLoiterPenalty { get; set; }
		public static ConfigEntry<float> loiterPenaltyFrequency { get; set; }
		public static ConfigEntry<float> loiterPenaltySeverity { get; set; }
		public static ConfigEntry<bool> limitPest { get; set; }
		public static ConfigEntry<float> limitPestAmount { get; set; }
		public static ConfigEntry<bool> playTickingSound { get; set; }
		
		internal static void Startup() {
			if (!File.Exists(Assembly.GetExecutingAssembly().Location.Replace("InterlopingArtifact.dll", "intericons"))) {
				INTER.Log.Fatal("Could not load asset bundle, aborting!");
				return;
			}
			InterBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("InterlopingArtifact.dll", "intericons"));
            
			CreateNetworkObject();
			
			Run.onRunSetRuleBookGlobal += Run_onRunSetRuleBookGlobal;
			Run.onRunStartGlobal += Run_onRunStartGlobal;
			Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
			RoR2Application.onLoad += InterRefs.CacheBlindPest;
			On.RoR2.RoR2Content.Init += CheckForChunk;
		}
		
		public static void CheckForChunk(On.RoR2.RoR2Content.orig_Init init) {
			if (InterOptionalMods.ChunkyMode.Enabled && InterOptionalMods.ChunkyMode.PluginVersion.Minor < 4) {
				INTER.Log.Fatal("Artifact of Interloper is not compatible with ChunkyMode versions prior to 0.4.0, aborting!");
				Run.onRunSetRuleBookGlobal -= Run_onRunSetRuleBookGlobal;
				Run.onRunStartGlobal -= Run_onRunStartGlobal;
				Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
				RoR2Application.onLoad -= InterRefs.CacheBlindPest;
				init();
				return;
			}
			
			if (!AddArtifact()) {
				INTER.Log.Fatal("Could not add artifact, aborting!");
				Run.onRunSetRuleBookGlobal -= Run_onRunSetRuleBookGlobal;
				Run.onRunStartGlobal -= Run_onRunStartGlobal;
				Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
				RoR2Application.onLoad -= InterRefs.CacheBlindPest;
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

			if (!ContentAddition.AddArtifactDef(Artifact)) return false;
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
			playTickingSound = InterlopingArtifactPlugin.instance.Config.Bind<bool>(
				"Ticking",
				"Play Ticking Sound",
				true,
				"Enable ticking sound before loiter penalty occurs.");
			if (!InterOptionalMods.RoO.Enabled) return;
			InterOptionalMods.RoO.AddFloat(timeUntilLoiterPenalty, 15f, 600f, "{0}");
			InterOptionalMods.RoO.AddFloat(loiterPenaltyFrequency, 0f, 60f, "{0}");
			InterOptionalMods.RoO.AddFloat(loiterPenaltySeverity, 10f, 100f);
			InterOptionalMods.RoO.AddCheck(limitPest);
			InterOptionalMods.RoO.AddFloat(limitPestAmount, 0f, 100f);
			InterOptionalMods.RoO.AddCheck(playTickingSound);
		}

		private static void CreateNetworkObject() {
			GameObject temp = new GameObject("thing");
			temp.AddComponent<NetworkIdentity>();
			InterInfo = temp.InstantiateClone("ChunkyRunInfo");
			UnityEngine.Object.Destroy(temp);
			InterInfo.AddComponent<InterRunInfo>();
		}

		internal static void Run_onRunSetRuleBookGlobal(Run arg1, RuleBook arg2) {
			shouldRun = false;
			if (!RunArtifactManager.instance.IsArtifactEnabled(Artifact)) return;
			shouldRun = true;
		}

		internal static void Run_onRunStartGlobal(Run run) {
			teleporterHit = false;
			teleporterExists = false;
			totalBlindPest = 0;
			if(!shouldRun && !ChunkyModeRun) return;
			
			if (shouldRun) INTER.Log.Info("Run started with Artifact of Loitering.");
			
			if (!NetworkServer.active) return;

			m_interInfo = UnityEngine.Object.Instantiate(InterInfo);
			NetworkServer.Spawn(m_interInfo);

			if (InterRunInfo.preSet) goto HOOKS;
			
			if (!shouldRun) {
				InterRunInfo.instance.loiterPenaltyTimeThisRun = (float)timeUntilLoiterPenalty.DefaultValue;
				InterRunInfo.instance.loiterPenaltyFrequencyThisRun = (float)loiterPenaltyFrequency.DefaultValue;
				InterRunInfo.instance.loiterPenaltySeverityThisRun = (float)loiterPenaltySeverity.DefaultValue;
				InterRunInfo.instance.limitPestsThisRun = (bool)limitPest.DefaultValue;
				InterRunInfo.instance.limitPestsAmountThisRun = (float)limitPestAmount.DefaultValue;
				goto HOOKS;
			}
			
			InterRunInfo.instance.limitPestsThisRun = limitPest.Value;
			InterRunInfo.instance.limitPestsAmountThisRun = limitPestAmount.Value;

			if (!ChunkyModeRun) {
				InterRunInfo.instance.loiterPenaltyTimeThisRun = timeUntilLoiterPenalty.Value;
				InterRunInfo.instance.loiterPenaltyFrequencyThisRun = loiterPenaltyFrequency.Value;
				InterRunInfo.instance.loiterPenaltySeverityThisRun = loiterPenaltySeverity.Value;
				goto HOOKS;
			}
			
			InterRunInfo.instance.loiterPenaltyTimeThisRun = Math.Min(timeUntilLoiterPenalty.Value, (float)timeUntilLoiterPenalty.DefaultValue);
			InterRunInfo.instance.loiterPenaltyFrequencyThisRun = Math.Min(loiterPenaltyFrequency.Value, (float)loiterPenaltyFrequency.DefaultValue);
			InterRunInfo.instance.loiterPenaltySeverityThisRun = Math.Max(loiterPenaltySeverity.Value, (float)loiterPenaltySeverity.DefaultValue);
			
			HOOKS:
			if (InterRunInfo.instance.limitPestsThisRun) {
				CharacterBody.onBodyStartGlobal += TrackVerminAdd;
				CharacterBody.onBodyDestroyGlobal += TrackVerminRemove;
			}
			
			On.RoR2.Run.BeginStage += Run_BeginStage;
			On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
			On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin += OnInteractTeleporter;
			On.RoR2.CombatDirector.Simulate += CombatDirector_Simulate;
		}

		internal static void Run_onRunDestroyGlobal(Run run) {
			if (!shouldRun && !ChunkyModeRun) return;
			if (shouldRun) INTER.Log.Info("Run ended with Artifact of Loitering.");
			shouldRun = false;
			tock = false;
			ChunkyModeRun = false;
			teleporterHit = false;
			teleporterExists = false;
			totalBlindPest = 0;
			InterRunInfo.preSet = false;
			UnityEngine.Object.Destroy(m_interInfo);
			
			On.RoR2.Run.BeginStage -= Run_BeginStage;
			On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;
			On.RoR2.TeleporterInteraction.IdleState.OnInteractionBegin -= OnInteractTeleporter;
			On.RoR2.CombatDirector.Simulate -= CombatDirector_Simulate;
			CharacterBody.onBodyStartGlobal -= TrackVerminAdd;
			CharacterBody.onBodyDestroyGlobal -= TrackVerminRemove;
		}
		
		internal static void TrackVerminAdd(CharacterBody body) {
			if (body.bodyIndex == InterRefs.FlyingVermin) totalBlindPest++;
		}
		internal static void TrackVerminRemove(CharacterBody body) {
			if (body.bodyIndex == InterRefs.FlyingVermin) totalBlindPest--;
		}
		
		// Set up Loitering Punishment
        internal static void Run_BeginStage(On.RoR2.Run.orig_BeginStage beginStage, Run self) {
	        InterRunInfo.instance.loiterTick = 0f;
            teleporterHit = false;
            teleporterExists = false;
            InterRunInfo.instance.allyCurse = 0;
            InterRunInfo.instance.loiterPenaltyActive = false;
            INTER.Log.Info("Stage begin! Waiting for Teleporter to be created.");
            beginStage(self);
        }
        
        // If a teleporter does not exist on the stage the loitering penalty should not be applied
        internal static void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced teleporterPlaced, Run self, SceneDirector sceneDirector, GameObject thing) {
            teleporterExists = true;
            InterRunInfo.instance.stagePunishTimer = self.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyTimeThisRun;
            INTER.Log.Info("Teleporter created! Timer set to " + InterRunInfo.instance.stagePunishTimer);
            InterRunInfo.instance.RpcSetTickTock();
            teleporterPlaced(self, sceneDirector, thing);
        }
        
        // The loitering penalty
        internal static void CombatDirector_Simulate(On.RoR2.CombatDirector.orig_Simulate simulate, CombatDirector self, float deltaTime) {
            if (!InterRunInfo.instance.loiterPenaltyActive || teleporterHit || Run.instance.NetworkfixedTime < InterRunInfo.instance.loiterTick) {
                simulate(self, deltaTime);
                return;
            }
#if DEBUG
            INTER.Log.Warning("Attempting to spawn enemy wave");
#endif
            int gougeCount = 1;

            if (shouldRun && ChunkyModeRun) {
                gougeCount += Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.MonstersOnShrineUse.itemIndex, false);
            }

            float newCreditBalance = InterRunInfo.instance.loiterPenaltySeverityThisRun * Stage.instance.entryDifficultyCoefficient * gougeCount;
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
            if (newEnemy.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab == BodyCatalog.GetBodyPrefab(InterRefs.FlyingVermin) && InterRunInfo.instance.limitPestsThisRun) {
#if DEBUG
                INTER.Log.Warning("Blind Pest detected, checking if we have too many.");
#endif
                int totalEnemies = 0;
                
                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Monster).Count;
                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Void).Count;
                totalEnemies += TeamComponent.GetTeamMembers(TeamIndex.Lunar).Count;

#if DEBUG
                INTER.Log.Warning("Total enemies: " + totalEnemies);
                INTER.Log.Warning("Too many? " + (totalBlindPest >= totalEnemies * (InterRunInfo.instance.limitPestsAmountThisRun / 100f)));
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
	        InterRunInfo.instance.loiterTick = Run.instance.NetworkfixedTime + InterRunInfo.instance.loiterPenaltyFrequencyThisRun;
            if (shouldRun && ChunkyModeRun) {
	            InterRunInfo.instance.allyCurse += 0.035f;
	            InterRunInfo.instance.RpcDirtyStats();
            }
            
            //Thank you .score for pointing out CombatDirector.CombatShrineActivation
            self.monsterSpawnTimer = 0f;
            self.monsterCredit =+ newCreditBalance;
            self.OverrideCurrentMonsterCard(newEnemy);
            
            simulate(self, deltaTime);

            self.monsterSpawnTimer = oldTimer;
            if (oldEnemy != null) self.OverrideCurrentMonsterCard(oldEnemy);
        }
		
		// Disable loitering penalty when the teleporter is interacted with
		// ReSharper disable once IdentifierTypo
		internal static void OnInteractTeleporter(On.RoR2.TeleporterInteraction.IdleState.orig_OnInteractionBegin interact, EntityStates.BaseState teleporterState, Interactor interactor) {
			InterRunInfo.instance.loiterPenaltyActive = false;
			teleporterHit = true;
			InterRunInfo.instance.allyCurse = 0f;
			InterRunInfo.instance.RpcDirtyStats();
			interact(teleporterState, interactor);
		}
		
		// Enforcing loitering penalty
        internal static void EnforceLoiter() {
            if (!shouldRun && !ChunkyModeRun) return;
            
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
            if (InterRunInfo.instance.loiterPenaltyActive){
#if DEBUG
                ReportLoiterError("Time's up");
#endif
                return;
            }
            if (InterRunInfo.instance.stagePunishTimer >= Run.instance.NetworkfixedTime) {
	            if (Run.instance.NetworkfixedTime >= tickingTimer ) {
		            tickingTimer += 1f;
		            InterRunInfo.instance.PlayTickTock();
	            }
#if DEBUG
                ReportLoiterError("Not time yet");
#endif
                return;
            }
            INTER.Log.Info("Time's up! Loitering penalty has been applied. StagePunishTimer " + InterRunInfo.instance.stagePunishTimer);
            InterRunInfo.instance.loiterPenaltyActive = true;
#if DEBUG
            INTER.Log.Debug("Warning now");
#endif
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
            if ((reportErrorTime >= Run.instance.NetworkfixedTime) && !reportErrorAnyway) return;
            INTER.Log.Debug(err);
            reportErrorTime = Run.instance.NetworkfixedTime + 5f;
            reportErrorAnyway = false;
        }
#endif
	}
}