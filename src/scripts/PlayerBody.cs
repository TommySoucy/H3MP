using FistVR;
using System;
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
        [Tooltip("All physical colliders. These will be disabled if the body if yours, since the vanilla colliders and hitboxes should be used instead.")]
        public Collider[] colliders;
        [Tooltip("All AIEntities. These will be de/registered as necessary so remote players' bodies can be detected by AI.")]
        public AIEntity[] entities;
        [Tooltip("All UI canvases. These will be disabled if the body is yours, since vanilla health UI for example should be used instead.")]
        public Canvas[] canvases;
        [Tooltip("All hitboxes. These will have their manager intialized if this is another player's body.")]
        public PlayerHitbox[] hitboxes;

        [Header("Optionals")]
        [Tooltip("If set, will enable wristmenu option to toggle body.")]
        public Renderer[] bodyRenderers;
        [Tooltip("If set, will enable wristmenu option to toggle hands.")]
        public Renderer[] handRenderers;
        [Tooltip("All parts that you want to have change color with the color the player has set.")]
        public Renderer[] coloredParts;
        public Text usernameLabel;
        public Text healthLabel;

        public virtual void Awake()
        {
            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;

            if(Mod.managerObject == null && GM.CurrentPlayerBody != null)
            {
                headToFollow = GM.CurrentPlayerBody.Head.transform;
                handsToFollow = new Transform[2];
                handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
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

        public virtual void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            if (Mod.managerObject == null)
            {
                headToFollow = playerBody.Head.transform;
                handsToFollow = new Transform[2];
                handsToFollow[0] = playerBody.LeftHand;
                handsToFollow[1] = playerBody.RightHand;
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
            GameManager.bodyVisible = bodyRenderers != null
                                      && bodyRenderers.Length > 0
                                      && bodyRenderers[0].enabled;
            SetBodyVisible(GameManager.bodyVisible);
            GameManager.handsVisible = handRenderers != null
                                       && handRenderers.Length > 0
                                       && handRenderers[0].enabled;
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
            }
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

        public void SetEntitiesRegistered(bool registered)
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

        public void SetIFF(int IFF)
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
