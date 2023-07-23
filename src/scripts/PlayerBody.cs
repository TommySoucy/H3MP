using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;

namespace H3MP.Scripts
{
    public class PlayerBody : MonoBehaviour
    {
        public string playerPrefabID;

        [Header("Head settings")]
        public Vector3 headOffset;
        public Transform headTransform;
        [NonSerialized]
        public Transform headToFollow;
        public Renderer[] headRenderers;

        [Header("Hand settings")]
        public Transform[] handTransforms;
        [NonSerialized]
        public Transform[] handsToFollow; // Left, Right

        [Header("Other")]
        public Collider[] colliders;
        public Renderer[] controlRenderers; 

        [Header("Optionals")]
        public Renderer[] bodyRenderers;
        public Renderer[] handRenderers;
        public Renderer[] coloredParts;

        public void Awake()
        {
            if(Mod.managerObject == null)
            {
                headToFollow = GM.CurrentPlayerBody.Head.transform;
                handsToFollow = new Transform[2];
                handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                SetHeadVisible(false);
                SetCollidersEnabled(false);
            }
            //else Connected, let TrackedPlayerBody handle what transform to follow based on controller
        }

        public void Update()
        {
            // These could only be null briefly if connected until TrackedPlayerBody sets them appropriately
            if (headToFollow != null)
            {
                headTransform.position = headToFollow.position;
                headTransform.localPosition += headOffset;
                headTransform.rotation = headToFollow.rotation;
            }
            if(handsToFollow != null)
            {
                for(int i=0; i < handsToFollow.Length; ++i)
                {
                    handTransforms[i].position = handsToFollow[i].position;
                    handTransforms[i].rotation = handsToFollow[i].rotation;
                }
            }
        }

        public void SetHeadVisible(bool visible)
        {
            if (headRenderers != null)
            {
                for (int i = 0; i < headRenderers.Length; ++i)
                {
                    if (headRenderers[i] != null)
                    {
                        headRenderers[i].enabled = visible;
                    }
                }
            }
        }

        public void SetColor(Color newColor)
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

        public void SetBodyVisible(bool visible)
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

        public void SetHandsVisible(bool visible)
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

        public void SetCollidersEnabled(bool enabled)
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
    }
}
