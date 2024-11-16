using System.Diagnostics.CodeAnalysis;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HDeMods {
    public class CullingTracker : MonoBehaviour {
        private CharacterBody m_body;
        private bool m_canBeCulled;
        private bool m_deferredCheck;
        private GameObject m_rangeDisplay;
        private MeshRenderer m_rangeDisplayRenderer;
        
        public bool Player { get; private set; }
        public bool CanBeCulled {
            get {
                if (!InterRunInfo.instance.loiterPenaltyActive) return false;
                return m_canBeCulled;
            }
        }

        private void Awake() {
            if (!NetworkServer.active) {
                Destroy(this);
                return;
            }
            if (!InterlopingArtifact.artifactEnabled && !InterlopingArtifact.HurricaneRun) {
                Destroy(this);
                return;
            }
            m_body = GetComponent<CharacterBody>();
            if (!m_body.master) {
                INTER.Log.Warning(m_body.name + " has no master! Deferring check until later.");
                m_deferredCheck = true;
                return;
            }
            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                if (m_body.master != pcmc.master) continue;
                Player = true;
#if DEBUG
                INTER.Log.Fatal(m_body.name + " is a player.");
                m_rangeDisplay = Instantiate(InterloperCullingZone.rangeDisplayPrefab);
                m_rangeDisplayRenderer = m_rangeDisplay.GetComponentInChildren<MeshRenderer>();
#endif
            }
            
        }
        private void FixedUpdate() {
            if (m_deferredCheck) {
                m_deferredCheck = false;
#if DEBUG
                INTER.Log.Debug(m_body.name + " is checking for master.");
#endif
                if(!m_body.master) {
                    INTER.Log.Warning(m_body.name + " master not found. Assuming this is not a player.");
                    return;
                }
                foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                    if (m_body.master != pcmc.master) continue;
                    Player = true;
#if DEBUG
                    INTER.Log.Fatal(m_body.name + " is a player.");
                    m_rangeDisplay = Instantiate(InterloperCullingZone.rangeDisplayPrefab);
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    m_rangeDisplayRenderer = m_rangeDisplay.GetComponentInChildren<MeshRenderer>();
#endif
                }
            }
            
            if (Player) {
#if DEBUG
                m_rangeDisplay.transform.localPosition = transform.localPosition;
                m_rangeDisplay.transform.localScale = Vector3.one * (InterlopingArtifact.aggressiveCullingRadius.Value * 2);
                if (!InterRunInfo.instance.loiterPenaltyActive || !InterlopingArtifact.aggressiveCulling.Value) m_rangeDisplayRenderer.enabled = false;
                else m_rangeDisplayRenderer.enabled = true;
#endif
                return;
            }
            if (!InterRunInfo.instance.loiterPenaltyActive) return;
            m_canBeCulled = !m_body.NearAnyPlayers(InterlopingArtifact.aggressiveCullingRadius.Value);
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
    internal class InterloperCullingZone : MonoBehaviour {
        private static InterloperCullingZone instance;
        public static GameObject rangeDisplayPrefab;
        private static Material material;
        private static Texture2D texRampCulling;
        private static Texture2D texRampCulling2;
        private static Texture2D texRampTeleporter;
        
        [Range(0f, 30f)] public static float softFactor = 0.25f;
        [Range(0.1f, 20f)] public static float softPower = 1f;
        [Range(0f, 5f)] public static float brightnessBoost = 0.1f;
        [Range(0f, 20f)] public static float alphaBoost = 0.5f;
        [Range(0f, 20f)] public static float intersectionStrength = 20f;

        public static void ResetToDefault() {
            softFactor = 0.78f;
            softPower = 1f;
            brightnessBoost = 0.34f;
            alphaBoost = 0.42f;
            intersectionStrength = 9.16f;
        }

        public static void SetTextureOg() => material.SetTexture(InterRefs.remapTex, texRampTeleporter);
        public static void SetTextureNew() => material.SetTexture(InterRefs.remapTex, texRampCulling);
        
        public static void SetTextureNew2() => material.SetTexture(InterRefs.remapTex, texRampCulling2);
        private void Awake() {
            if (instance != null) {
                Destroy(this);
                return;
            }
            instance = this;
            
            GameObject temp = InterlopingArtifact.InterBundle.LoadAsset<GameObject>("PickupInterloper");
            
            material = Instantiate(Addressables
                .LoadAssetAsync<Material>("RoR2/Base/Teleporters/matTeleporterRangeIndicator.mat")
                .WaitForCompletion());
            material.name = "matInterloperCullingZone";
            texRampTeleporter = material.GetTexture(InterRefs.remapTex) as Texture2D;
            texRampCulling = InterlopingArtifact.InterBundle.LoadAsset<Texture2D>("texRampCulling");
            texRampCulling2 = InterlopingArtifact.InterBundle.LoadAsset<Texture2D>("texRampCulling2");
            SetTextureNew2();
            
            temp.GetComponentInChildren<MeshRenderer>().material = material;
            temp.GetComponentInChildren<MeshRenderer>().enabled = false;
            
            rangeDisplayPrefab = temp.InstantiateClone("Interloper_CullZone", false);
            Destroy(temp);
        }

        private void Update() {
            material.SetFloat(InterRefs.softFactor, softFactor);
            material.SetFloat(InterRefs.softPower, softPower);
            material.SetFloat(InterRefs.brightnessBoost, brightnessBoost);
            material.SetFloat(InterRefs.alphaBoost, alphaBoost);
            material.SetFloat(InterRefs.intersectionStrength, intersectionStrength);
        }
    }
}