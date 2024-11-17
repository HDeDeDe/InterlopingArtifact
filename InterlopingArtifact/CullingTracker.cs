using System;
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
        
        private InterloperCullingZone m_rangeDisplayController;
        
        public bool Player { get; private set; }
        public bool CanBeCulled {
            get {
                if (!InterRunInfo.instance.loiterPenaltyActive) return false;
                return m_canBeCulled;
            }
        }

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
            if (!m_body.master) {
                INTER.Log.Warning(m_body.name + " master not found. Assuming this is not a player.");
                return;
            }
            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {
                if (m_body.master != pcmc.master) continue;
                Player = true;
                INTER.Log.Fatal(m_body.name + " is a player.");
                GameObject temp = Instantiate(InterloperCullingZone.rangeDisplayPrefab);
                NetworkServer.Spawn(temp);
                m_rangeDisplayController = temp.GetComponent<InterloperCullingZone>();
            }
            
        }
        private void FixedUpdate() {
            if (!NetworkServer.active) return;
            
            if (Player) {
                m_rangeDisplayController.RpcSetPosition(transform.localPosition);
                m_rangeDisplayController.RpcSetSize(Vector3.one * (InterlopingArtifact.aggressiveCullingRadius.Value * 2));
                if (!InterRunInfo.instance.loiterPenaltyActive || !InterlopingArtifact.aggressiveCulling.Value) 
                    m_rangeDisplayController.RpcSetVisibility(false);
                else m_rangeDisplayController.RpcSetVisibility(true);
                return;
            }
            if (!InterRunInfo.instance.loiterPenaltyActive) return;
            m_canBeCulled = !m_body.NearAnyPlayers(InterlopingArtifact.aggressiveCullingRadius.Value);
        }
        
        private void OnDestroy() {
            if (!NetworkServer.active) return;
            if (Player) {
                NetworkServer.Destroy(m_rangeDisplayController.gameObject);
            }
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
    internal class InterloperCullingZone : NetworkBehaviour {
        public static GameObject rangeDisplayPrefab;
        private static Material material;
        [Range(0f, 30f)] public const float softFactor = 0.25f;
        [Range(0.1f, 20f)] public const float softPower = 1f;
        [Range(0f, 5f)] public const float brightnessBoost = 0.1f;
        [Range(0f, 20f)] public const float alphaBoost = 0.5f;
        [Range(0f, 20f)] public const float intersectionStrength = 20f;
        
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
            material.SetFloat(InterRefs.softFactor, softFactor);
            material.SetFloat(InterRefs.softPower, softPower);
            material.SetFloat(InterRefs.brightnessBoost, brightnessBoost);
            material.SetFloat(InterRefs.alphaBoost, alphaBoost);
            material.SetFloat(InterRefs.intersectionStrength, intersectionStrength);
            
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