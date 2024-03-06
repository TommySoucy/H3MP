using FistVR;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class PlayerBody : MonoBehaviour
    {
        public enum HeadDisplayMode
        {
            RendererToggle,
            LayerToggle,
            Physical
        }

        public string playerPrefabID;

        [Header("Head settings")]
        public Transform headTransform;
        [Tooltip("RendererToggle: Head renderers will be toggled depending on whether the player body is yours or another player's.\n" +
                 "LayerToggle: Head renderers' layers will be toggled between ExternalCamOnly and default depending on whether the player body is yours or another player's.\n" +
                 "Physical: Will assume that head will be out of view of VR camera.")]
        public HeadDisplayMode headDisplayMode;
        [Tooltip("If headDisplayMode is set to Physical, headRenderers will not be used.")]
        public Renderer[] headRenderers;
        [NonSerialized]
        public Transform headToFollow;

        [Header("Hand settings")]
        [Tooltip("0: Left, 1: Right.")]
        public Transform[] handTransforms;
        [NonSerialized]
        public Transform[] handsToFollow;

        [Header("Other")]
        [Tooltip("All colliders. These will be disabled if the body is yours, since the vanilla colliders and hitboxes should be used instead.")]
        public Collider[] colliders;
        [Tooltip("See colliders tooltip above. Put in this array colliders you want to have enabled when the body is yours, and disabled when it isn't. " +
                 "For example, a trigger collider that acts as a toggle for some custom functionality with your body. You would want that enabled when the " +
                 "body is the player's because you want them to be able to toggle that functionality on their own body. On the other hand, you want it disabled " +
                 "when the body is another player's because you don't want other players to be able to toggle the functionality on everyone else's bodies.")]
        public Collider[] negativeColliders;
        [Tooltip("All colliders you want to have ignored. See colliders tooltip above. Put in this array any collider that you want to have ignored.")]
        public Collider[] ignoredColliders;
        [Tooltip("All AIEntities. These will be de/registered as necessary so remote players' bodies can be detected by AI.")]
        public AIEntity[] entities;
        [Tooltip("All UI canvases. These will be disabled if the body is yours, since vanilla health UI for example should be used instead.")]
        public Canvas[] canvases;
        [Tooltip("All hitboxes. These will have their manager intialized if this is another player's body.")]
        public PlayerHitbox[] hitboxes;

        [Header("Optionals")]
        [Tooltip("If set, will enable wristmenu option to toggle body.")]
        public Renderer[] bodyRenderers;
        [NonSerialized]
        public int[] bodyRendererLayers;
        [Tooltip("If set, will enable wristmenu option to toggle hands.")]
        public Renderer[] handRenderers;
        [NonSerialized]
        public int[] handRendererLayers;
        [Tooltip("All parts that you want to have change color with the color the player has set.")]
        public Renderer[] coloredParts;
        public Text usernameLabel;
        public Text healthLabel;

        // for self-hiding
        public static bool optionAutoHideSelf = true;
        private const float SELF_HIDE_PITCH = 54;
        private const float SELF_UNHIDE_PITCH = 50;
        private const float SELF_UNHIDE_DELAY = 0.5f;
        private bool selfIsHidden = false;
        private bool selfIsUnhiding = false;
        public FVRViveHand[] handScripts;

        public virtual void Awake()
        {
            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;

            Verify();

            if (bodyRenderers != null) 
            {
                bodyRendererLayers = new int[bodyRenderers.Length];
                for (int i = 0; i < bodyRenderers.Length; ++i) 
                {
                    if (bodyRenderers[i] != null)
                    {
                        bodyRendererLayers[i] = bodyRenderers[i].gameObject.layer;
                    }
                }
            }

            if (handRenderers != null) 
            {
                handRendererLayers = new int[handRenderers.Length];
                for (int i = 0; i < handRenderers.Length; ++i) 
                {
                    if (handRenderers[i] != null)
                    {
                        handRendererLayers[i] = handRenderers[i].gameObject.layer;
                    }
                }
            }

            if(Mod.managerObject == null && GM.CurrentPlayerBody != null)
            {
                headToFollow = GM.CurrentPlayerBody.Head.transform;
                handsToFollow = new Transform[2];
                handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                handScripts = new FVRViveHand[2]
                { 
                    GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(), 
                    GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>() 
                };
                if (headDisplayMode != HeadDisplayMode.Physical)
                {
                    SetHeadVisible(false);
                }
                SetCollidersEnabled(false);
                SetCanvasesEnabled(false);
                SetEntitiesRegistered(false);

                Init();
            }
            //else Connected, let TrackedPlayerBody handle what transform to follow based on controller
            //     OR not connected but no current player body. Setting these will be handled when a player body is initialized
        }

        public virtual void Verify()
        {
            bool correct = true;

            if(headTransform == null)
            {
                Debug.LogError("PlayerBody " + playerPrefabID+": missing head transform");
                correct = false;
            }
            if(handTransforms == null || handTransforms.Length < 2 || handTransforms[0] == null || handTransforms[1] == null)
            {
                Debug.LogError("PlayerBody " + playerPrefabID+": missing hand transforms");
                correct = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            if(this.colliders != null && this.colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; ++i)
                {
                    if (!colliders[i].enabled)
                    {
                        continue;
                    }
                    bool[] found = new bool[3];
                    int foundCount = 0;
                    for (int j = 0; j < this.colliders.Length; ++j)
                    {
                        if (colliders[i] == this.colliders[j])
                        {
                            found[0] = true;
                            ++foundCount;
                            break;
                        }
                    }
                    for (int j = 0; j < negativeColliders.Length; ++j)
                    {
                        if (colliders[i] == negativeColliders[j])
                        {
                            found[1] = true;
                            ++foundCount;
                            break;
                        }
                    }
                    for (int j = 0; j < ignoredColliders.Length; ++j)
                    {
                        if (colliders[i] == ignoredColliders[j])
                        {
                            found[3] = true;
                            ++foundCount;
                            break;
                        }
                    }
                    if(foundCount > 1)
                    {
                        string arrayNames = "";
                        if (found[0])
                        {
                            arrayNames += "colliders, ";
                        }
                        if (found[1])
                        {
                            arrayNames += "negativeColliders, ";
                        }
                        if (found[2])
                        {
                            arrayNames += "ignoredColliders";
                        }
                        Debug.LogError("PlayerBody " + playerPrefabID + ": Collider " + colliders[i].name + " was found in multiple colliders arrays: "+ arrayNames);
                        correct = false;
                    }
                    else if (foundCount == 0)
                    {
                        Debug.LogError("PlayerBody " + playerPrefabID + ": Collider " + colliders[i].name + " was not added to any colliders array");
                        correct = false;
                    }
                }
            }

            AIEntity[] entities = GetComponentsInChildren<AIEntity>();
            if(this.entities != null && this.entities.Length > 0)
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    if (!entities[i].enabled)
                    {
                        continue;
                    }
                    bool found = false;
                    for (int j = 0; j < this.entities.Length; ++j)
                    {
                        if (entities[i] == this.entities[j])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogError("PlayerBody " + playerPrefabID + ": AIEntity " + entities[i].name + " was not added to entities array");
                        correct = false;
                    }
                }
            }
            else // No entities array set yet, set it ourselves
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": Entities array set automatically");
                this.entities = entities;
                correct = false;
            }

            Canvas[] canvases = GetComponentsInChildren<Canvas>();
            if(this.canvases != null && this.canvases.Length > 0)
            {
                for (int i = 0; i < canvases.Length; ++i)
                {
                    if (!canvases[i].enabled)
                    {
                        continue;
                    }
                    bool found = false;
                    for (int j = 0; j < this.canvases.Length; ++j)
                    {
                        if (canvases[i] == this.canvases[j])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogError("PlayerBody " + playerPrefabID + ": Canvas " + canvases[i].name + " was not added to canvases array");
                        correct = false;
                    }
                }
            }
            else // No canvases array set yet, set it ourselves
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": Canvases array set automatically");
                this.canvases = canvases;
                correct = false;
            }

            PlayerHitbox[] hitboxes = GetComponentsInChildren<PlayerHitbox>();
            if(this.hitboxes != null && this.hitboxes.Length > 0)
            {
                for (int i = 0; i < hitboxes.Length; ++i)
                {
                    if (!hitboxes[i].enabled)
                    {
                        continue;
                    }
                    bool found = false;
                    for (int j = 0; j < this.hitboxes.Length; ++j)
                    {
                        if (hitboxes[i] == this.hitboxes[j])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogError("PlayerBody " + playerPrefabID + ": Hitbox " + hitboxes[i].name + " was not added to hitboxes array");
                        correct = false;
                    }
                }
            }
            else // No hitboxes array set yet, set it ourselves
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": Hitboxes array set automatically");
                this.hitboxes = hitboxes;
                correct = false;
            }

            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": BodyRenderers array was not set. Player will not be able to toggle body visibility.");
                correct = false;
            }

            if (handRenderers == null || handRenderers.Length == 0)
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": HandRenderers array was not set. Player will not be able to toggle hand visibility.");
                correct = false;
            }

            if (coloredParts == null || coloredParts.Length == 0)
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": ColoredParts array was not set. Player will not be able to set their color.");
                correct = false;
            }

            if (usernameLabel == null)
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": UsernameLabel was not set. Other players will not be able to see this player's name.");
                correct = false;
            }

            if (healthLabel == null)
            {
                Debug.LogWarning("PlayerBody " + playerPrefabID + ": HealthLabel was not set. Other players will not be able to see this player's health.");
                correct = false;
            }

            if (correct)
            {
                Debug.Log("PlayerBody " + playerPrefabID + " verified and is setup properly.");
            }
        }

        public virtual void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            if (Mod.managerObject == null)
            {
                headToFollow = playerBody.Head.transform;
                handsToFollow = new Transform[2];
                handsToFollow[0] = playerBody.LeftHand;
                handsToFollow[1] = playerBody.RightHand;
                handScripts = new FVRViveHand[2]
                {
                    GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(),
                    GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>()
                };
                if (headDisplayMode != HeadDisplayMode.Physical)
                {
                    SetHeadVisible(false);
                }
                SetCollidersEnabled(false);
                SetCanvasesEnabled(false);
                SetEntitiesRegistered(false);

                Init();
            }
            //else Connected, let TrackedPlayerBody handle what transform to follow based on controller
        }

        public virtual void Init()
        {
            SetColor(GameManager.colors[GameManager.colorIndex]);
            SetBodyVisible(GameManager.bodyVisible);
            SetHandsVisible(GameManager.handsVisible);
        }

        public virtual void Update()
        {
            // These could only be null briefly if connected until TrackedPlayerBody sets them appropriately
            if (headToFollow != null)
            {
                headTransform.position = headToFollow.position;
                headTransform.rotation = headToFollow.rotation;
            }
            if(handsToFollow != null)
            {
                for(int i=0; i < handsToFollow.Length; ++i)
                {
                    if (handsToFollow[i] != null)
                    {
                        handTransforms[i].position = handsToFollow[i].position;
                        handTransforms[i].rotation = handsToFollow[i].rotation;
                    }
                }

                // (Un)hide self when using quickbelt //    
                if (GameManager.currentPlayerBody == this && optionAutoHideSelf)
                {
                    bool usingQB = IsUsingQuickbelt();
                    float pitch = headToFollow.rotation.eulerAngles.x;
                    if (!selfIsHidden)
                    {
                        if (usingQB)
                        {
                            SetPOVBodyVisible(false);
                            SetPOVHandsVisible(false);
                            selfIsHidden = true;
                        }
                    }
                    else if (!selfIsUnhiding)
                    {
                        if (pitch < SELF_UNHIDE_PITCH || (180 < pitch && pitch < 360))
                        {
                            AnvilManager.Run(SelfUnhideCoroutine());
                        }
                    }
                }
            }
        }

        public void ToggleSelfHide()
        {
            optionAutoHideSelf = !optionAutoHideSelf;
            if (!optionAutoHideSelf)
            {
                selfIsHidden = false;
                selfIsUnhiding = false;
                SetPOVBodyVisible(true);
                SetPOVHandsVisible(true);
            }
        }

        private bool IsUsingQuickbelt()
        {
            float pitch = headToFollow.rotation.eulerAngles.x;
            if (SELF_HIDE_PITCH < pitch && pitch < 90)
            {
                for(int i=0; i < handScripts.Length; ++i)
                {
                    if (handScripts[i] == null)
                    {
                        continue;
                    }

                    if ((handScripts[i].CurrentInteractable == null && handScripts[i].CurrentHoveredQuickbeltSlotDirty != null || handScripts[i].ClosestPossibleInteractable != null)
                        || (handScripts[i].CurrentInteractable != null && handScripts[i].CurrentHoveredQuickbeltSlot != null))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerator SelfUnhideCoroutine()
        {
            if (selfIsUnhiding) yield break;
            selfIsUnhiding = true;

            yield return new WaitForSeconds(SELF_UNHIDE_DELAY);
            if (!optionAutoHideSelf || !IsUsingQuickbelt())
            {
                SetPOVBodyVisible(true);
                SetPOVHandsVisible(true);
                selfIsHidden = false;
            }

            selfIsUnhiding = false;
        }

        public virtual void SetHeadVisible(bool visible)
        {
            if (headRenderers != null)
            {
                for (int i = 0; i < headRenderers.Length; ++i)
                {
                    if (headRenderers[i] != null)
                    {
                        if (headDisplayMode == HeadDisplayMode.RendererToggle)
                        {
                            headRenderers[i].enabled = visible;
                        }
                        else // LayerToggle
                        {
                            headRenderers[i].gameObject.layer = visible ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("ExternalCamOnly");
                        }
                    }
                }
            }
        }

        public virtual void SetColor(Color newColor)
        {
            if (coloredParts != null)
            {
                for (int i = 0; i < coloredParts.Length; ++i)
                {
                    if (coloredParts[i] != null && coloredParts[i].materials != null)
                    {
                        for (int j = 0; j < coloredParts[i].materials.Length; ++j)
                        {
                            if (coloredParts[i].materials[j] != null)
                            {
                                coloredParts[i].materials[j].color = newColor;
                            }
                        }
                    }
                }
            }
        }

        public virtual void SetBodyVisible(bool visible)
        {
            if (bodyRenderers != null)
            {
                for (int i = 0; i < bodyRenderers.Length; ++i)
                {
                    if (bodyRenderers[i] != null)
                    {
                        bodyRenderers[i].enabled = visible;
                    }
                }
            }
        }

        public virtual void SetHandsVisible(bool visible)
        {
            if (handRenderers != null) 
            {
                for (int i = 0; i < handRenderers.Length; ++i)
                {
                    if (handRenderers[i] != null)
                    {
                        handRenderers[i].enabled = visible;
                    }
                }
            }
        }

        private void SetPOVBodyVisible(bool visible)
        {
            if (bodyRenderers != null)
            {
                for (int i = 0; i < bodyRenderers.Length; ++i)
                {
                    if (bodyRenderers[i] != null)
                    {
                        bodyRenderers[i].gameObject.layer = visible ? bodyRendererLayers[i] : LayerMask.NameToLayer("ExternalCamOnly");
                    }
                }
            }
        }

        private void SetPOVHandsVisible(bool visible)
        {
            if (handRenderers != null)
            {
                for (int i = 0; i < handRenderers.Length; ++i)
                {
                    if (handRenderers[i] != null)
                    {
                        handRenderers[i].gameObject.layer = visible ? handRendererLayers[i] : LayerMask.NameToLayer("ExternalCamOnly");
                    }
                }
            }
        }

        public virtual void SetCollidersEnabled(bool enabled)
        {
            if(colliders != null)
            {
                for(int i=0; i < colliders.Length; ++i)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = enabled;
                    }
                }
            }
            if(negativeColliders != null)
            {
                for(int i=0; i < negativeColliders.Length; ++i)
                {
                    if (negativeColliders[i] != null)
                    {
                        negativeColliders[i].enabled = !enabled;
                    }
                }
            }
        }

        public virtual void SetCanvasesEnabled(bool enabled)
        {
            if(canvases != null)
            {
                for(int i=0; i < canvases.Length; ++i)
                {
                    if (canvases[i] != null)
                    {
                        canvases[i].enabled = enabled;
                    }
                }
            }
        }

        public virtual void SetEntitiesRegistered(bool registered)
        {
            if (GM.CurrentAIManager != null && entities != null)
            {
                if (registered)
                {
                    for(int i=0; i < entities.Length; ++i)
                    {
                        if (entities[i] != null)
                        {
                            GM.CurrentAIManager.RegisterAIEntity(entities[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < entities.Length; ++i)
                    {
                        if (entities[i] != null)
                        {
                            GM.CurrentAIManager.DeRegisterAIEntity(entities[i]);
                        }
                    }
                }
            }
        }

        public virtual void SetIFF(int IFF)
        {
            if (entities != null)
            {
                for(int i=0; i < entities.Length; ++i)
                {
                    if (entities[i] != null)
                    {
                        entities[i].IFFCode = IFF;
                    }
                }
            }
        }

        public virtual void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;
        }
    }
}
