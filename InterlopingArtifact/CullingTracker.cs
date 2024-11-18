using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace HDeMods {
    public class CullingTracker : MonoBehaviour {
        private CharacterBody m_body;
        public bool canBeCulled;
        public bool voidCampSpawn;
        public bool isMinion;
        private bool m_isVoidTeam;
        
        private InterloperCullingZone m_rangeDisplayController;
        
        public bool Player { get; private set; }

        private void Awake() {
            if (!NetworkServer.active) {
                INTER.Log.Warning("Culling Tracker created on Client.");
                return;
            }
            if (!InterlopingArtifact.artifactEnabled && !InterlopingArtifact.HurricaneRun) {
                Destroy(this);
                return;
            }
            m_body = GetComponent<CharacterBody>();
            m_isVoidTeam = m_body.teamComponent.teamIndex == TeamIndex.Void;
            if (!m_body.master) {
                INTER.Log.Warning(m_body.name + " master not found. Assuming this is not a player or a minion.");
                return;
            }
            if (m_body.master.minionOwnership.ownerMaster != null) {
                isMinion = true;
                return;
            }
            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                if (m_body.master != pcmc.master) continue;
                Player = true;
                INTER.Log.Message(m_body.name + " is a player.");
                GameObject temp = Instantiate(InterloperCullingZone.rangeDisplayPrefab);
                NetworkServer.Spawn(temp);
                m_rangeDisplayController = temp.GetComponent<InterloperCullingZone>();
            }
        }
        private void FixedUpdate() {
            if (!NetworkServer.active) return;
            float cullingRadius = InterlopingArtifact.aggressiveCullingRadius.Value;
            
            if (Player) {
                if (!InterRunInfo.instance.loiterPenaltyActive || !InterlopingArtifact.aggressiveCulling.Value) {
                    m_rangeDisplayController.RpcSetVisibility(false);
                    return;
                }
                if (InterlopingArtifact.voidMajority) cullingRadius *= 2;
                m_rangeDisplayController.RpcSetPosition(transform.localPosition);
                m_rangeDisplayController.RpcSetSize(Vector3.one * (cullingRadius * 2));
                m_rangeDisplayController.RpcSetVisibility(true);
                return;
            }
            if (!InterRunInfo.instance.loiterPenaltyActive) return;
            if (m_isVoidTeam) cullingRadius *= 2;
            canBeCulled = !m_body.NearAnyPlayers(cullingRadius);
        }
        
        private void OnDestroy() {
            if (!NetworkServer.active) return;
            if (Player) {
                NetworkServer.Destroy(m_rangeDisplayController.gameObject);
            }
        }
    }

    internal class InterDoNotCull : MonoBehaviour {
        private static Hook voidCampSpawnEnemyHook;
        private static MethodInfo voidCampSpawnEnemy;
        private delegate void VoidCampSpawnEnemySig(Action<CampDirector, GameObject> orig,
            CampDirector self, GameObject masterObject);
        private static VoidCampSpawnEnemySig hookEvent;
        
        
        public static void CreateHook() {
            voidCampSpawnEnemy = AccessTools.Method(typeof(CampDirector), 
                "<PopulateCamp>g__OnMonsterSpawnedServer|18_0", new Type[] {typeof(GameObject)});
            hookEvent += CheckForVoidSpawn;
            voidCampSpawnEnemyHook = new Hook(voidCampSpawnEnemy, hookEvent);
        }
        
        public static void RemoveHook() {
            voidCampSpawnEnemyHook.Undo();
        }

        private static void CheckForVoidSpawn(Action<CampDirector, GameObject> orig, CampDirector self, GameObject masterObject) {
            CharacterMaster meMaster = masterObject.GetComponent<CharacterMaster>();
            GameObject body = meMaster.GetBodyObject();
            if (self.campCenterTransform.name == "VoidCamp(Clone)") body.AddComponent<InterDoNotCull>();

            orig(self, masterObject);
        }
        
        private void FixedUpdate() {
            CullingTracker tracker = GetComponent<CullingTracker>();
            tracker.voidCampSpawn = true;
            enabled = false;
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
    internal class InterloperCullingZone : NetworkBehaviour {
        public static GameObject rangeDisplayPrefab;
        private static Material material;
        
        private ObjectScaleCurve m_rangeDisplayRenderer;
        private bool m_visible;
        private bool m_visiblePreviousFrame;
        private bool m_enabledPreviousFrame;

        public static void InitZone() {
            GameObject temp = InterlopingArtifact.InterBundle.LoadAsset<GameObject>("InterloperCullingZoneVisual");
            rangeDisplayPrefab = temp.InstantiateClone("Interloper_CullZone");
            Destroy(temp);
            
            material = Instantiate(Addressables
                .LoadAssetAsync<Material>("RoR2/Base/Teleporters/matTeleporterRangeIndicator.mat")
                .WaitForCompletion());
            material.name = "matInterloperCullingZone";
            material.SetTexture(InterRefs.remapTex, InterlopingArtifact.InterBundle.LoadAsset<Texture2D>("texRampCulling2"));
            material.SetFloat(InterRefs.softFactor, 0.25f);
            material.SetFloat(InterRefs.softPower, 1f);
            material.SetFloat(InterRefs.brightnessBoost, 0.1f);
            material.SetFloat(InterRefs.alphaBoost, 0.5f);
            material.SetFloat(InterRefs.intersectionStrength, 20f);
            
            rangeDisplayPrefab.GetComponentInChildren<MeshRenderer>().material = material;
            rangeDisplayPrefab.AddComponent<InterloperCullingZone>();
        }
        
        private void Awake() {
            m_rangeDisplayRenderer = GetComponent<ObjectScaleCurve>();
            m_rangeDisplayRenderer.enabled = false;
        }

        private void Update() {
            if (m_visible != m_visiblePreviousFrame 
                || InterlopingArtifact.showCullingRadius.Value != m_enabledPreviousFrame) SetVisible(m_visible);

            m_enabledPreviousFrame = InterlopingArtifact.showCullingRadius.Value;
            m_visiblePreviousFrame = m_visible;
        }

        private void SetVisible(bool visibility) {
            if (visibility && InterlopingArtifact.showCullingRadius.Value) {
                m_rangeDisplayRenderer.enabled = true;
                return;
            }
            m_rangeDisplayRenderer.Reset();
            m_rangeDisplayRenderer.enabled = false;
        }

        [ClientRpc]
        public void RpcSetPosition(Vector3 position) => SetPosition(position);
        
        [ClientRpc]
        public void RpcSetSize(Vector3 size) => SetSize(size);
        
        [ClientRpc]
        public void RpcSetVisibility(bool visibility) => SetVisibility(visibility);

        private void SetPosition(Vector3 position) => transform.localPosition = position;
        
        private void SetSize(Vector3 size) => m_rangeDisplayRenderer.baseScale = size;

        private void SetVisibility(bool visibility) => m_visible = visibility;
    }
}