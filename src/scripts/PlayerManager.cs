using FistVR;
using H3MP.Networking;
using H3MP.Tracking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSetPlayerColor event
        /// </summary>
        /// <param name="colorIndex">The index of the color we want to set this player to</param>
        /// <param name="set">Custom override for whether color was set or not</param>
        public delegate void OnSetPlayerColorDelegate(int colorIndex, ref bool set);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we set the player's color so you can set color for your own materials
        /// </summary>
        public static event OnSetPlayerColorDelegate OnSetPlayerColor;

        public void Init(int ID, string username, string scene, int instance)
        {
            this.ID = ID;
            this.username = username;
            this.scene = scene;
            this.instance = instance;

            GameObject headObject = new GameObject("Head");
            headObject.transform.parent = transform;
            head = headObject.transform;

            GameObject leftHandObject = new GameObject("LeftHand");
            leftHandObject.transform.parent = transform;
            leftHand = leftHandObject.transform;

            GameObject rightHandObject = new GameObject("RightHand");
            rightHandObject.transform.parent = transform;
            rightHand = rightHandObject.transform;
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

            playerBody.physicalPlayerBody.SetIFF(IFF);

            if (GameManager.colorByIFF && spawned)
            {
                SetColor(IFF);
            }

            playerBody.physicalPlayerBody.SetCanvasesEnabled(visible && (GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF)));

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
            bool set = false;
            if (OnSetPlayerColor != null)
            {
                OnSetPlayerColor(colorIndex, ref set);
            }

            if (!set && this.colorIndex != colorIndex)
            {
                this.colorIndex = Mathf.Abs(colorIndex % GameManager.colors.Length);

                playerBody.physicalPlayerBody.SetColor(GameManager.colors[colorIndex]);
            }

            if (!GameManager.radarColor && reticleContact != null)
            {
                reticleContact.R_Arrow.material.color = GameManager.colors[colorIndex];
                reticleContact.R_Icon.material.color = GameManager.colors[colorIndex];
            }
        }
    }
}
