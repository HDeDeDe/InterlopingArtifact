using System.Collections;

internal static class Settings {
//-----------------------------------------------------Customize--------------------------------------------------------
	// ReSharper disable once InconsistentNaming
	public const bool giveMePDBs = true;
	public const bool weave = false;

	public const string pluginName = HDeMods.InterlopingArtifactPlugin.PluginName;
	public const string pluginAuthor = HDeMods.InterlopingArtifactPlugin.PluginAuthor;
	public const string pluginVersion = HDeMods.InterlopingArtifactPlugin.PluginVersion;
	public const string changelog = "../CHANGELOG.md";
	public const string readme = "../README.md";
	public const string icon = "../Resources/icon.png";

	public const string riskOfRain2Install =
		@"C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\";

	public static readonly ArrayList extraFiles = new ArrayList {
		new FileInfo("../Resources/InterlopingSounds/GeneratedSoundBanks/Windows/Inter_TickTock.bnk")
	};

	public const string manifestWebsiteUrl = "";
	public const string manifestDescription = "";

	public const string manifestDependencies = "[\n" +
	                                           "\t\t\"bbepis-BepInExPack-5.4.2108\",\n" +
	                                           "\t\t\"RiskofThunder-HookGenPatcher-1.2.3\",\n" +
	                                           "\t]";
}