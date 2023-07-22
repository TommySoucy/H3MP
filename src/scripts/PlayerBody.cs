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
        [NonSerialized]
        public Transform headToFollow;
        public Renderer[] headRenderers;

        [Header("Optionals")]
        public Renderer[] bodyRenderers;
        public Renderer[] handRenderers;
        public Renderer[] coloredParts;

        public void Awake()
        {
            if(Mod.managerObject == null)
            {
                headToFollow = GM.CurrentPlayerBody.Head.transform;
                SetHeadVisible(false);
            }
            //else Connected, let TrackedPlayerBody handle what transform to follow based on controller
        }

        public void Update()
        {
            if (headToFollow != null)
            {
                transform.position = headToFollow.position;
                transform.localPosition += headOffset;
                transform.rotation = headToFollow.rotation;
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

        public void SetVisible(bool visible)
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
    }
}
