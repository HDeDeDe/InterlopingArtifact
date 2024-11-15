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
#if DEBUG
        private static GameObject rangeDisplayPrefab;

        public static void DebugInit() {
            GameObject temp = InterlopingArtifact.InterBundle.LoadAsset<GameObject>("PickupInterloper");
            Material realMat = Instantiate(Addressables
                .LoadAssetAsync<Material>("RoR2/Base/Teleporters/matTeleporterRangeIndicator.mat")
                .WaitForCompletion());
            realMat.name = "inter_matCullingZone";
            realMat.SetTexture(InterRefs.remapTex, InterlopingArtifact.InterBundle.LoadAsset<Texture2D>("texRampCulling"));
            temp.GetComponentInChildren<MeshRenderer>().material = realMat;
            rangeDisplayPrefab = temp.InstantiateClone("Interloper_CullZone", false);
            Destroy(temp);
        }

        private GameObject m_rangeDisplay;
        
#endif
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
                INTER.Log.Warning(m_body.name + " has no master! Deferring check until later!");
                return;
            }
            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                if (m_body.master != pcmc.master) continue;
                Player = true;
#if DEBUG
                INTER.Log.Fatal(m_body.name + " is a player.");
                m_rangeDisplay = Instantiate(rangeDisplayPrefab);
#endif
            }
            
        }
        private void FixedUpdate() {
            if (m_deferredCheck) {
#if DEBUG
                INTER.Log.Debug(m_body.name + " is checking for master.");
#endif
                if(!m_body.master) return;
                foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                    if (m_body.master != pcmc.master) continue;
                    Player = true;
#if DEBUG
                    INTER.Log.Fatal(m_body.name + " is a player.");
                    m_rangeDisplay = Instantiate(rangeDisplayPrefab);
#endif
                }
                m_deferredCheck = false;
            }
            
            if (Player) {
#if DEBUG
                m_rangeDisplay.transform.localPosition = transform.localPosition;
                m_rangeDisplay.transform.localScale = Vector3.one * (InterlopingArtifact.aggressiveCullingRadius.Value * 2);
#endif
                return;
            }
            if (!InterRunInfo.instance.loiterPenaltyActive) return;
            m_canBeCulled = !m_body.NearAnyPlayers(InterlopingArtifact.aggressiveCullingRadius.Value);
        }
    }
}