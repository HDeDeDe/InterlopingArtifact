using System.Collections.Generic;
using ProperSave;
using System.Runtime.CompilerServices;
using RoR2;

namespace HDeMods { namespace InterOptionalMods {
	internal static class ProperSaves {
		public static bool Enabled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ProperSavePlugin.GUID);

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void SetUp() {
			Loading.OnLoadingStarted += LoadFromSave;
			SaveFile.OnGatherSaveData += SaveRunInfo;
		}
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void LoadFromSave(SaveFile save) {
			if (!Loading.IsLoading) return;
			
			if (save.ModdedData.TryGetValue("INTERLOPINGARTIFACT_RunInfo", out ProperSave.Data.ModdedData rawData) &&
			    rawData?.Value is InterSaveData saveData && saveData.isValidSave) {
				InterRunInfo.saveData = saveData;
				InterRunInfo.preSet = true;
			}
			
			INTER.Log.Warning("Interloper RunInfo not present, skipping step.");
		}
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void SaveRunInfo(Dictionary<string, object> save) {
			if (!RunArtifactManager.instance.IsArtifactEnabled(InterlopingArtifact.Artifact) && !InterlopingArtifact.ChunkyModeRun) return;
			InterSaveData tempRun = new InterSaveData {
				isValidSave = true,
				loiterPenaltyTimeThisRun = InterRunInfo.instance.loiterPenaltyTimeThisRun,
				loiterPenaltyFrequencyThisRun = InterRunInfo.instance.loiterPenaltyFrequencyThisRun,
				loiterPenaltySeverityThisRun = InterRunInfo.instance.loiterPenaltySeverityThisRun,
				limitPestsThisRun = InterRunInfo.instance.limitPestsThisRun,
				limitPestsAmountThisRun = InterRunInfo.instance.limitPestsAmountThisRun
			};

			save.Add("INTERLOPINGARTIFACT_RunInfo",tempRun);
		}
	}
} }