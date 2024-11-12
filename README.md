# Interloping Artifact
![Artifact](https://github.com/HDeDeDe/InterlopingArtifact/blob/main/Resources/icon.png?raw=true)

A new artifact which causes enemies to swarm you after a period of time.

## Config
This mod includes support for Risk of Options, which exposes a few settings to tweak.

| Section       | Option                     | Description                                                                                                                                                                                                        |
|---------------|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Loitering     | Time until loiter penalty  | The amount of time from the start of the stage until the loiter penalty is enforced. Minimum of 60 seconds. Only Applies when artifact is enabled. Can not be changed mid run. Default is 5 minutes (300 seconds). |
| Loitering     | Loiter penalty frequency   | he amount of time between forced enemy spawns. Only Applies when artifact is enabled. Can not be changed mid run. Default is 5 seconds.                                                                            |
| Loitering     | Loiter penalty severity    | The strength of spawned enemies. 40 is equal to 1 combat shrine. Only Applies when artifact is enabled. Can not be changed mid run. Default is 40.                                                                 |
| Limit Enemies | Limit Blind Pest           | Enable Blind Pest limit. Can not be changed mid run.                                                                                                                                                               |
| Limit Enemies | Blind Pest Amount          | The percentage of enemies that are allowed to be blind pest. Only affects the Loitering penalty. Can not be changed mid run. Default is 10%.                                                                       |
| Warning       | Warning Sound Volume       | Volume of the warning sound. Set to 0 to disable.                                                                                                                                                                  |
| Warning       | Use Ticking Sound          | Use a clock ticking sound instead of a bell. Recommended if you have Kampanaphobia.                                                                                                                                |
| Warning       | Enable Halfway Warning     | Play the warning sound when the loiter timer reaches half way.                                                                                                                                                     |
| Warning       | Time Before Loiter Penalty | How long before the Loiter Penalty the bells start tolling. Requires a scene change to update. Default is 15 seconds.                                                                                              |
| Artifact      | Force Unlock               | Force artifact to be available. This will not grant the achievement. Requires restart. It's also no fun :(                                                                                                         |
| Artifact      | Disable Code Hints         | Prevent artifact code hints from appearing in game. Requires restart.                                                                                                                                              |

## Installation
To install with r2modman automatically, either click the "Install with Mod Manager" button at the top of the page on [Thunderstore](https://thunderstore.io/package/HDeDeDe/InterlopingArtifact/) or search for InterlopingArtifact within r2modman.

To install manually, either click the "Manual Download" button at the top of the page on [Thunderstore](https://thunderstore.io/package/HDeDeDe/InterlopingArtifact/), or download the latest release from [GitHub](https://github.com/HDeDeDe/InterlopingArtifact/releases). Once downloaded, if you are using r2modman, launch r2modman and navigate to your preferred Risk of Rain 2 profile. From there, go to Settings > Profile > Import local mod, and then select `HDeDeDe-InterlopingArtifact-{Version_Number}.zip` (or just `InterlopingArtifact.zip`) when prompted. Confirm the settings and you should be good to go.

If you do not have r2modman, then make sure you have the required dependencies (they can be found at the top of the page on [Thunderstore](https://thunderstore.io/package/HDeDeDe/InterlopingArtifact/)), navigate to your Risk of Rain 2 install folder > BepInEx > plugins and extract the following files from `InterlopingArtifact.zip` to the same subfolder: `InterlopingArtifact.dll, InterlopingArtifact.language, InterlopingArtifact.pdb, Inter_WarningSounds.sound, interloperassets`

## Feedback
Any and all feedback is appreciated, if you want to let me know anything please feel free to open an issue on the [GitHub Page](https://github.com/HDeDeDe/InterlopingArtifact) or @ me on the modding discord (hdedede).

## Artifact hints
<details>
<summary>Artifact 1:</summary>
<details>
<summary>Hint 1</summary>
&nbsp;&nbsp;&nbsp;&nbsp;The stars told us the way.
</details>
<details>
<summary>Hint 2</summary>
&nbsp;&nbsp;&nbsp;&nbsp;Before the fall of humanity.
</details>
<details>
<summary>Hint 3</summary>
&nbsp;&nbsp;&nbsp;&nbsp;We could not back away from our fate.
</details>
</details>

<details>
<summary>Artifact 2:</summary>
<details>
<summary>Hint 1</summary>
&nbsp;&nbsp;&nbsp;&nbsp;We reached for the sky.
</details>
<details>
<summary>Hint 2</summary>
&nbsp;&nbsp;&nbsp;&nbsp;The roots followed in pursuit.
</details>
<details>
<summary>Hint 3</summary>
&nbsp;&nbsp;&nbsp;&nbsp;The tree of mycelium led us astray.
</details>
</details>

## Creds

Ants - Made the Artifact Icon

HIFU - Provided base game icon backgrounds

.score, Bubbet, iDeathHD, and others on the RoR2 Mod Discord - Helped me figure out issues and learn more along the way

anartoast - suggestion for code formula location