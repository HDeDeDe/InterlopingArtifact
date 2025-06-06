using System.Collections;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Settings {
//-----------------------------------------------------Customize--------------------------------------------------------
    public const bool giveMePDBs = true;
    public const bool weave = true;

    public const string pluginName = HDeMods.InterlopingArtifactPlugin.PluginName;
    public const string pluginAuthor = HDeMods.InterlopingArtifactPlugin.PluginAuthor;
    public const string pluginVersion = HDeMods.InterlopingArtifactPlugin.PluginVersion;
    public const string changelog = "../CHANGELOG.md";
    public const string readme = "../README.md";
    public const string icon = "../Resources/icon.png";

    public const string riskOfRain2Install =
        @"C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\";

    public static readonly ArrayList extraFiles = [
        new FileInfo("../Resources/InterlopingSounds/GeneratedSoundBanks/Windows/init.bnk"),
        new FileInfo("../Resources/InterlopingSounds/GeneratedSoundBanks/Windows/Inter_WarningSounds.bnk"),
        new FileInfo("../Resources/InterlopingSounds/GeneratedSoundBanks/Windows/Media/737693030.wem"),
        new FileInfo("../Resources/InterlopingSounds/GeneratedSoundBanks/Windows/Media/466803387.wem"),
        new FileInfo("../Resources/InterlopingArtifact.language"),
        new FileInfo("../Resources/load_bearing_dog.png"),
        new FileInfo("../InterlopingArtifactUnity/Assets/AssetBundle/interloperassets")
    ];

    public const string manifestWebsiteUrl = "https://github.com/HDeDeDe/InterlopingArtifact";
    public const string manifestDescription = "Enemies will begin to swarm you after a period of time.";

    public const string manifestDependencies = "[\n" +
                                               "\t\t\"bbepis-BepInExPack-5.4.2117\",\n" +
                                               "\t\t\"RiskofThunder-HookGenPatcher-1.2.5\",\n" +
                                               "\t\t\"RiskofThunder-R2API_Language-1.0.1\",\n" +
                                               "\t\t\"RiskofThunder-R2API_Networking-1.0.2\",\n" +
                                               "\t\t\"RiskofThunder-R2API_Prefab-1.0.4\",\n" +
                                               "\t\t\"RiskofThunder-R2API_Sound-1.0.3\",\n" +
                                               "\t\t\"RiskofThunder-R2API_ArtifactCode-1.0.1\",\n" +
                                               "\t\t\"RiskofThunder-R2API_Director-2.3.1\"\n" +
                                               "\t]";
}