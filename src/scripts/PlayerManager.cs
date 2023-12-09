using System;
using System.Collections.Generic;
using FistVR;
using H3MP.Networking;
using H3MP.Tracking;
using UnityEngine;

namespace H3MP.Scripts
{
    public class PlayerManager : MonoBehaviour
    {
        public int ID;
        public string username;

        // Player transforms and state data
        public TrackedPlayerBody playerBody;
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public int IFF;
        public int colorIndex;
        public float health;
        public int maxHealth;
        public TAH_ReticleContact reticleContact;

        public string scene;
        public int instance;
        public bool firstInSceneInstance;

        public bool visible;

        public void Init(int ID, string username, string scene, int instance, int IFF, int colorIndex)
        {
            this.ID = ID;
            this.username = username;
            this.scene = scene;
            this.instance = instance;
            this.IFF = IFF;
            this.colorIndex = colorIndex;

            GameObject headObject = new GameObject("Head");
            headObject.transform.parent = transform;
            head = headObject.transform;

            GameObject leftHandObject = new GameObject("LeftHand");
            leftHandObject.transform.parent = transform;
            leftHand = leftHandObject.transform;

            GameObject rightHandObject = new GameObject("RightHand");
            rightHandObject.transform.parent = transform;
            rightHand = rightHandObject.transform;

            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;
        }

        public void Damage(float damageMultiplier, bool head, Damage damage)
        {
            if (ID != -1)
            {
                if (ThreadManager.host)
                {
                    ServerSend.PlayerDamage(ID, damageMultiplier, head, damage);
                }
                else
                {
                    ClientSend.PlayerDamage(ID, damageMultiplier, head, damage);
                }
            }
            else
            {
                Mod.LogInfo("Dummy player has receive damage with mult: " + damageMultiplier);
            }
        }

        public void SetVisible(bool visible)
        {
            this.visible = visible;

            if (playerBody != null)
            {
                playerBody.gameObject.SetActive(visible);
            }

            if (visible && reticleContact == null &&
                Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null &&
                Mod.currentTNHInstance.manager.TAHReticle != null)
            {
                reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : colorIndex - 4));
            }
            else if(!visible && reticleContact != null)
            {
                for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                {
                    if (GM.TNH_Manager.TAHReticle.Contacts[i] == reticleContact)
                    {
                        GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                        UnityEngine.Object.Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                        GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                        reticleContact = null;
                        break;
                    }
                }
            }
        }

        public void SetIFF(int IFF, bool spawned = true)
        {
            int preIFF = this.IFF;
            this.IFF = IFF;

            // This process is dependent on GM.CurrentPlayerBody being set
            // Will recall this when we set the current player
            if(GM.CurrentPlayerBody == null || playerBody == null)
            {
                return;
            }

            playerBody.physicalPlayerBody.SetIFF(IFF);

            if (GameManager.colorByIFF && spawned)
            {
                SetColor(IFF);
            }

            playerBody.physicalPlayerBody.SetCanvasesEnabled(GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF));

            if (GameManager.radarMode == 1)
            {
                if (visible &&
                    Mod.currentTNHInstance != null &&
                    Mod.currentTNHInstance.manager != null &&
                    Mod.currentTNHInstance.currentlyPlaying.Contains(ID) &&
                    reticleContact == null)
                {
                    reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : colorIndex - 4));
                }
                else if(!visible ||
                        Mod.currentTNHInstance == null ||
                        Mod.currentTNHInstance.manager == null ||
                        !Mod.currentTNHInstance.currentlyPlaying.Contains(ID) ||
                        reticleContact != null)
                {
                    for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                    {
                        if (GM.TNH_Manager.TAHReticle.Contacts[i] == reticleContact)
                        {
                            GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                            Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                            GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                            reticleContact = null;
                            break;
                        }
                    }
                }
            }

            if (GameManager.radarColor && reticleContact != null)
            {
                reticleContact.R_Arrow.material.color = IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
                reticleContact.R_Icon.material.color = IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
            }
        }

        public void SetColor(int colorIndex)
        {
            if (this.colorIndex != colorIndex)
            {
                this.colorIndex = Mathf.Abs(colorIndex % GameManager.colors.Length);

                if (playerBody != null)
                {
                    playerBody.physicalPlayerBody.SetColor(GameManager.colors[colorIndex]);
                }
            }

            if (!GameManager.radarColor && reticleContact != null)
            {
                reticleContact.R_Arrow.material.color = GameManager.colors[colorIndex];
                reticleContact.R_Icon.material.color = GameManager.colors[colorIndex];
            }
        }

        private void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            SetIFF(IFF);
        }

        private void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;
        }

        private float t = 0;
        
        private void Update()
        {
            if (PositionData.Count < 2) return;

            int tickRate = ThreadManager.host ? Server.tickRate : Client.singleton.tickRate;
            float multiplier = Mathf.Clamp(0.5f + 0.25f * (PositionData.Count - 2), 0.5f, 2f);
            t += Time.unscaledDeltaTime * tickRate * multiplier;
            
            TemporalPositionData a = PositionData[0];
            TemporalPositionData b = PositionData[1];
            TemporalPositionData pos = TemporalPositionData.Lerp(a, b, t);
            
            transform.position = pos.BodyPosition;
            transform.rotation = pos.BodyRotation;
            head.position = pos.HeadPosition;
            head.rotation = pos.HeadRotation;
            leftHand.position = pos.LeftHandPosition;
            leftHand.rotation = pos.LeftHandRotation;
            rightHand.position = pos.RightHandPosition;
            rightHand.rotation = pos.RightHandRotation;
            
            if (t > 1f)
            {
                t = 0;
                PositionData.RemoveAt(0);
            }
        }

        public void EnqueuePositionData(Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot,
            Vector3 torsoPos, Quaternion torsoRot, Vector3 leftHandPos, Quaternion leftHandRot, Vector3 rightHandPos,
            Quaternion rightHandRot)
        {
            PositionData.Add(new TemporalPositionData
            {
                BodyPosition = position,
                BodyRotation = rotation,
                HeadPosition = headPos,
                HeadRotation = headRot,
                LeftHandPosition = leftHandPos,
                LeftHandRotation = leftHandRot,
                RightHandPosition = rightHandPos,
                RightHandRotation = rightHandRot
            });
        }

        private List<TemporalPositionData> PositionData = new List<TemporalPositionData>();
        
        private class TemporalPositionData
        {
            public Vector3 BodyPosition;
            public Quaternion BodyRotation;

            public Vector3 HeadPosition;
            public Quaternion HeadRotation;

            public Vector3 LeftHandPosition;
            public Quaternion LeftHandRotation;

            public Vector3 RightHandPosition;
            public Quaternion RightHandRotation;

            public static TemporalPositionData Lerp(TemporalPositionData a, TemporalPositionData b, float t)
            {
                return new TemporalPositionData()
                {
                    BodyPosition = Vector3.Lerp(a.BodyPosition, b.BodyPosition, t),
                    BodyRotation = Quaternion.Lerp(a.BodyRotation, b.BodyRotation, t),
                    HeadPosition = Vector3.Lerp(a.HeadPosition, b.HeadPosition, t),
                    HeadRotation = Quaternion.Lerp(a.HeadRotation, b.HeadRotation, t),
                    LeftHandPosition = Vector3.Lerp(a.LeftHandPosition, b.LeftHandPosition, t),
                    LeftHandRotation = Quaternion.Lerp(a.LeftHandRotation, b.LeftHandRotation, t),
                    RightHandPosition = Vector3.Lerp(a.RightHandPosition, b.RightHandPosition, t),
                    RightHandRotation = Quaternion.Lerp(a.RightHandRotation, b.RightHandRotation, t),
                };
            }
        }
    }
}
