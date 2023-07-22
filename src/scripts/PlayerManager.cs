using FistVR;
using H3MP.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP
{
    public class PlayerManager : MonoBehaviour
    {
        public int ID;
        public string username;

        // Player transforms and state data
        public Transform head;
        public PlayerHitbox headHitBox;
        public Material headMat;
        public AIEntity headEntity;
        public Transform torso;
        public Material torsoMat;
        public PlayerHitbox torsoHitBox;
        public AIEntity torsoEntity;
        public Transform leftHand;
        public PlayerHitbox leftHandHitBox;
        public Material leftHandMat;
        public Transform rightHand;
        public PlayerHitbox rightHandHitBox;
        public Material rightHandMat;
        public Billboard overheadDisplayBillboard;
        public Text usernameLabel;
        public Text healthIndicator;
        public int IFF;
        public int colorIndex;
        public TAH_ReticleContact reticleContact;
        public float health;
        public string playerPrefabID;

        public string scene;
        public int instance;
        public bool firstInSceneInstance;

        public bool visible;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSetPlayerPrefab event
        /// </summary>
        /// <param name="prefabID">The prefab identifier</param>
        /// <param name="set">Custom override for whether prefab was set or not</param>
        public delegate void OnSetPlayerPrefabDelegate(string prefabID, ref bool set);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we set the player's skin/model/prefab so you can set your own transforms and relevant objects
        /// </summary>
        public static event OnSetPlayerPrefabDelegate OnSetPlayerPrefab;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSetPlayerColor event
        /// </summary>
        /// <param name="colorIndex">The index of the color we want to set this player to</param>
        /// <param name="prefabID">The player's body prefab identifier</param>
        /// <param name="set">Custom override for whether color was set or not</param>
        public delegate void OnSetPlayerColorDelegate(int colorIndex, string prefabID, ref bool set);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we set the player's color so you can set color for your own materials
        /// </summary>
        public static event OnSetPlayerColorDelegate OnSetPlayerColor;

        private void Awake()
        {
        }

        public void Damage(PlayerHitbox.Part part, Damage damage)
        {
            if (ID != -1)
            {
                if (ThreadManager.host)
                {
                    ServerSend.PlayerDamage(ID, (byte)part, damage);
                }
                else
                {
                    ClientSend.PlayerDamage(ID, (byte)part, damage);
                }
            }
            else
            {
                Mod.LogInfo("Dummy player has receive damage on " + part);
            }
        }

        public void SetEntitiesRegistered(bool registered)
        {
            if(GM.CurrentAIManager != null)
            {
                if (registered)
                {
                    GM.CurrentAIManager.RegisterAIEntity(headEntity);
                    GM.CurrentAIManager.RegisterAIEntity(torsoEntity);
                }
                else
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(headEntity);
                    GM.CurrentAIManager.DeRegisterAIEntity(torsoEntity);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            this.visible = visible;

            head.gameObject.SetActive(visible);
            torso.gameObject.SetActive(visible);
            leftHand.gameObject.SetActive(visible);
            rightHand.gameObject.SetActive(visible);
            overheadDisplayBillboard.gameObject.SetActive(visible && (GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF)));

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

            SetEntitiesRegistered(visible);
        }

        public void SetIFF(int IFF, bool spawned = true)
        {
            int preIFF = this.IFF;
            this.IFF = IFF;
            if (headEntity != null)
            {
                headEntity.IFFCode = IFF;
                torsoEntity.IFFCode = IFF;
            }

            if (GameManager.colorByIFF && spawned)
            {
                SetColor(IFF);
            }

            overheadDisplayBillboard.gameObject.SetActive(visible && (GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF)));

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
                OnSetPlayerColor(colorIndex, playerPrefabID, ref set);
            }

            if (!set && this.colorIndex != colorIndex && playerPrefabID.Equals("Default"))
            {
                this.colorIndex = Mathf.Abs(colorIndex % GameManager.colors.Length);

                headMat.color = GameManager.colors[colorIndex];
                torsoMat.color = GameManager.colors[colorIndex];
                leftHandMat.color = GameManager.colors[colorIndex];
                rightHandMat.color = GameManager.colors[colorIndex];
            }

            if (!GameManager.radarColor && reticleContact != null)
            {
                reticleContact.R_Arrow.material.color = GameManager.colors[colorIndex];
                reticleContact.R_Icon.material.color = GameManager.colors[colorIndex];
            }
        }

        public void SetPlayerPrefab(string playerPrefabID)
        {
            TODO: // We would want this to handle: setting the ID, destroying old one, and instantiating new one if necessary. but currently using this only to set variables AND to do everything else, we need to decide what to do here
            bool set = false;
            if(OnSetPlayerPrefab != null)
            {
                OnSetPlayerPrefab(playerPrefabID, ref set);
            }

            if (!set && !this.playerPrefabID.Equals("Default"))
            {
                Destroy(transform.GetChild(0).gameObject);

                Instantiate(GameManager.playerPrefabs["Default"], transform);

                head = transform.GetChild(0).GetChild(0);
                headEntity = head.GetChild(1).gameObject.AddComponent<AIEntity>();
                headEntity.Beacons = new List<AIEntityIFFBeacon>();
                headEntity.IFFCode = IFF;
                headHitBox = head.gameObject.AddComponent<PlayerHitbox>();
                headHitBox.manager = this;
                headHitBox.part = PlayerHitbox.Part.Head;
                torso = transform.GetChild(0).GetChild(1);
                torsoEntity = torso.GetChild(0).gameObject.AddComponent<AIEntity>();
                torsoEntity.Beacons = new List<AIEntityIFFBeacon>();
                torsoEntity.IFFCode = IFF;
                torsoHitBox = torso.gameObject.AddComponent<PlayerHitbox>();
                torsoHitBox.manager = this;
                torsoHitBox.part = PlayerHitbox.Part.Torso;
                leftHand = transform.GetChild(0).GetChild(2);
                leftHandHitBox = leftHand.gameObject.AddComponent<PlayerHitbox>();
                leftHandHitBox.manager = this;
                leftHandHitBox.part = PlayerHitbox.Part.LeftHand;
                rightHand = transform.GetChild(0).GetChild(3);
                rightHandHitBox = rightHand.gameObject.AddComponent<PlayerHitbox>();
                rightHandHitBox.manager = this;
                rightHandHitBox.part = PlayerHitbox.Part.RightHand;
                overheadDisplayBillboard = transform.GetChild(0).GetChild(4).GetChild(0).GetChild(0).gameObject.AddComponent<Billboard>();
                usernameLabel = transform.GetChild(0).GetChild(4).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
                healthIndicator = transform.GetChild(0).GetChild(4).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
                headMat = head.GetComponent<Renderer>().material;
                torsoMat = torso.GetComponent<Renderer>().material;
                leftHandMat = leftHand.GetComponent<Renderer>().material;
                rightHandMat = rightHand.GetComponent<Renderer>().material;
            }
        }
    }
}
