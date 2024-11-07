using System;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace HDeMods {
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class InterFormula {
        public static bool scrambleCode = false;
        private static GameObject CreateFormulaStones(bool doIt) {
            GameObject formulaBottom = Addressables
                .LoadAssetAsync<GameObject>("RoR2/Base/artifactworld/ArtifactFormulaDisplay (Bottom Half).prefab")
                .WaitForCompletion();
            GameObject formulaTop = Addressables
                .LoadAssetAsync<GameObject>("RoR2/Base/artifactworld/ArtifactFormulaDisplay (Top Half).prefab")
                .WaitForCompletion();

            formulaBottom.name = DecryptButWorse("RGV2aWNlU2VyaWFsTWFuYWdlcg==");
            if (scrambleCode)formulaBottom.transform.position = new Vector3(
                DecryptF("Mi4wNzkx"), DecryptF("LTAuMDE5"), DecryptF("My4wNjE1"));
            else formulaBottom.transform.position = new Vector3(
                DecryptF("Mi4xMzY0"), DecryptF("LTAuMDE5"), DecryptF("My4wNjE1"));
            if (scrambleCode)formulaBottom.transform.rotation = Quaternion.Euler(
                DecryptF("Mjcw"), DecryptF("MTU0Ljk2NjI="), DecryptF("MA=="));
            else formulaBottom.transform.rotation = Quaternion.Euler(
                DecryptF("Mjcw"), DecryptF("MTUwLjg5MTY="), DecryptF("MA=="));
            formulaBottom.transform.localScale = new Vector3(
                DecryptF("MC4x"), DecryptF("MC4x"), DecryptF("MC4x"));

            formulaTop.name = DecryptButWorse("bWRsQ2hlc3Q0");
            formulaTop.transform.position = new Vector3(
                DecryptF("LTIxMi4xNDQ1"), DecryptF("MjAxLjc3NTc="), DecryptF("LTE5Ny40Nzk0"));
            formulaTop.transform.rotation = Quaternion.Euler(
                DecryptF("MzA5LjExNTM="), DecryptF("MjM4Ljc4MTc="), DecryptF("MzU0LjczOTE="));
            formulaTop.transform.localScale = new Vector3(
                DecryptF("MC41"), DecryptF("MC41"), DecryptF("MC41"));
            
            ArtifactCompoundDef circle = Addressables
                .LoadAssetAsync<ArtifactCompoundDef>(Decrypt("YWNkVHJpYW5nbGUuYXNzZXQ="))
                .WaitForCompletion();
            ArtifactCompoundDef pink = Addressables
                .LoadAssetAsync<ArtifactCompoundDef>(Decrypt("YWNkQ2lyY2xlLmFzc2V0"))
                .WaitForCompletion();
            ArtifactCompoundDef donkeykongismyfavoritemarvelsuperhero = Addressables
                .LoadAssetAsync<ArtifactCompoundDef>(Decrypt("YWNkRGlhbW9uZC5hc3NldA=="))
                .WaitForCompletion();
            ArtifactCompoundDef ford = Addressables
                .LoadAssetAsync<ArtifactCompoundDef>(Decrypt("YWNkU3F1YXJlLmFzc2V0"))
                .WaitForCompletion();
            ArtifactCompoundDef waitForCompletion = Addressables
                .LoadAssetAsync<ArtifactCompoundDef>(Decrypt("YWNkRW1wdHkuYXNzZXQ="))
                .WaitForCompletion();
            
            ArtifactFormulaDisplay bottomDisplay = formulaBottom.GetComponent<ArtifactFormulaDisplay>();
            ArtifactFormulaDisplay topDisplay = formulaTop.GetComponent<ArtifactFormulaDisplay>();
            
            // Good luck deciphering this shit
            bottomDisplay.artifactCompoundDisplayInfos[3].artifactCompoundDef = circle;
            bottomDisplay.artifactCompoundDisplayInfos[4].artifactCompoundDef = pink;
            bottomDisplay.artifactCompoundDisplayInfos[8].artifactCompoundDef = donkeykongismyfavoritemarvelsuperhero;
            topDisplay.artifactCompoundDisplayInfos[8].artifactCompoundDef = donkeykongismyfavoritemarvelsuperhero;
            topDisplay.artifactCompoundDisplayInfos[1].artifactCompoundDef = circle;
            topDisplay.artifactCompoundDisplayInfos[0].artifactCompoundDef = donkeykongismyfavoritemarvelsuperhero;
            topDisplay.artifactCompoundDisplayInfos[2].artifactCompoundDef = donkeykongismyfavoritemarvelsuperhero;
            topDisplay.artifactCompoundDisplayInfos[3].artifactCompoundDef = ford;
            bottomDisplay.artifactCompoundDisplayInfos[7].artifactCompoundDef = circle;
            topDisplay.artifactCompoundDisplayInfos[4].artifactCompoundDef = pink;
            bottomDisplay.artifactCompoundDisplayInfos[0].artifactCompoundDef = waitForCompletion;
            topDisplay.artifactCompoundDisplayInfos[5].artifactCompoundDef = circle;
            bottomDisplay.artifactCompoundDisplayInfos[5].artifactCompoundDef = ford;
            topDisplay.artifactCompoundDisplayInfos[6].artifactCompoundDef = waitForCompletion;
            bottomDisplay.artifactCompoundDisplayInfos[2].artifactCompoundDef = waitForCompletion;
            topDisplay.artifactCompoundDisplayInfos[7].artifactCompoundDef = circle;
            bottomDisplay.artifactCompoundDisplayInfos[1].artifactCompoundDef = ford;
            bottomDisplay.artifactCompoundDisplayInfos[6].artifactCompoundDef = donkeykongismyfavoritemarvelsuperhero;
            if (doIt) return formulaTop;
            return formulaBottom;
        }

        public static void SceneChanged(Scene old, Scene next) {
            if (next.name == DecryptButWorse("bG9iYnk=")) 
                GameObject.Instantiate(CreateFormulaStones(false));
            if (next.name == DecryptButWorse("cm9vdGp1bmdsZQ==")) 
                GameObject.Instantiate(CreateFormulaStones(true));
        }

        private static string Decrypt(string encodedString) =>
            Encoding.UTF8.GetString(Convert.FromBase64String("Um9SMi9CYXNlL0FydGlmYWN0Q29tcG91bmRzLw==")) 
            + Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
        
        
        private static string DecryptButWorse(string encodedString) => 
            Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
        
        private static float DecryptF(string encodedString) =>
            float.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encodedString)), 
                CultureInfo.InvariantCulture.NumberFormat);
    }
}