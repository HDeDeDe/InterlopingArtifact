using RiskOfOptions.OptionConfigs;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HDeMods {namespace InterOptionalMods {
	internal static class RoO {
        public static bool Enabled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
			
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void AddCheck(ConfigEntry<bool> option) {
			InterCheckBoxOption boxOption = new InterCheckBoxOption(option);
			ModSettingsManager.AddOption(boxOption, InterlopingArtifactPlugin.PluginGUID, InterlopingArtifactPlugin.PluginName);
#if DEBUG
			INTER.Log.Debug(boxOption.GetNameToken());
			INTER.Log.Debug(boxOption.GetDescriptionToken());
#endif
		}
		
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void AddInt(ConfigEntry<int> option, int minimum, int maximum) {
			InterIntSliderOption sliderOption = new InterIntSliderOption(option, new IntSliderConfig() {min = minimum, max = maximum});
			ModSettingsManager.AddOption(sliderOption, InterlopingArtifactPlugin.PluginGUID, InterlopingArtifactPlugin.PluginName);
			
#if DEBUG
			INTER.Log.Debug(sliderOption.GetNameToken());
			INTER.Log.Debug(sliderOption.GetDescriptionToken());
#endif
		}
		
		public static void AddFloat(ConfigEntry<float> option, float minimum, float maximum, string format = "{0:0}%") {
			InterSliderOption sliderOption = new InterSliderOption(option, new SliderConfig() {min = minimum, max = maximum, FormatString = format});
			ModSettingsManager.AddOption(sliderOption, InterlopingArtifactPlugin.PluginGUID, InterlopingArtifactPlugin.PluginName);
			
#if DEBUG
			INTER.Log.Debug(sliderOption.GetNameToken());
			INTER.Log.Debug(sliderOption.GetDescriptionToken());
#endif
		}
		
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void SetSprite(Sprite sprite) {
			ModSettingsManager.SetModIcon(sprite);
		}
		
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void SetDescriptionToken(string description) {
			ModSettingsManager.SetModDescriptionToken(description);
		}
	}} 
	
	// Thanks to Bubbet for the suggestion to do this
	internal class InterCheckBoxOption : CheckBoxOption {
		public InterCheckBoxOption(ConfigEntry<bool> configEntry) : base(configEntry) {
			RoR2.Language.onCurrentLanguageChanged+= ResetDescription;
		}

		public override void RegisterTokens() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}

		private void ResetDescription() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}
	}
	
	internal class InterIntSliderOption : IntSliderOption {

		public InterIntSliderOption(ConfigEntry<int> configEntry, IntSliderConfig config) : base(configEntry, config) {
			RoR2.Language.onCurrentLanguageChanged+= ResetDescription;
		}

		public override void RegisterTokens() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}
		
		private void ResetDescription() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}
	}
	
	internal class InterSliderOption : SliderOption {

		public InterSliderOption(ConfigEntry<float> configEntry, SliderConfig config) : base(configEntry, config) {
			RoR2.Language.onCurrentLanguageChanged+= ResetDescription;
		}

		public override void RegisterTokens() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}
		
		private void ResetDescription() {
			Description = RoR2.Language.GetString(GetDescriptionToken());
		}
	}
}