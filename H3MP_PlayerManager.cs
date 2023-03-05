using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_PlayerManager : MonoBehaviour
    {
        public int ID;
        public string username;

        // Player transforms and state data
        public Transform head;
        public H3MP_PlayerHitbox headHitBox;
        public Material headMat;
        public AIEntity headEntity;
        public Transform torso;
        public Material torsoMat;
        public H3MP_PlayerHitbox torsoHitBox;
        public AIEntity torsoEntity;
        public Transform leftHand;
        public H3MP_PlayerHitbox leftHandHitBox;
        public Material leftHandMat;
        public Transform rightHand;
        public H3MP_PlayerHitbox rightHandHitBox;
        public Material rightHandMat;
        public H3MP_Billboard overheadDisplayBillboard;
        public Text usernameLabel;
        public Text healthIndicator;
        public int IFF;
        public int colorIndex;
        public TAH_ReticleContact reticleContact;
        public float health;

        public string scene;
        public int instance;

        public bool visible;

        private void Awake()
        {
            head = transform.GetChild(0);
            headEntity = head.GetChild(1).gameObject.AddComponent<AIEntity>();
            headEntity.Beacons = new List<AIEntityIFFBeacon>();
            headEntity.IFFCode = IFF;
            headHitBox = head.gameObject.AddComponent<H3MP_PlayerHitbox>();
            headHitBox.manager = this;
            headHitBox.part = H3MP_PlayerHitbox.Part.Head;
            torso = transform.GetChild(1);
            torsoEntity = torso.GetChild(0).gameObject.AddComponent<AIEntity>();
            torsoEntity.Beacons = new List<AIEntityIFFBeacon>();
            torsoEntity.IFFCode = IFF;
            torsoHitBox = torso.gameObject.AddComponent<H3MP_PlayerHitbox>();
            torsoHitBox.manager = this;
            torsoHitBox.part = H3MP_PlayerHitbox.Part.Torso;
            leftHand = transform.GetChild(2);
            leftHandHitBox = leftHand.gameObject.AddComponent<H3MP_PlayerHitbox>();
            leftHandHitBox.manager = this;
            leftHandHitBox.part = H3MP_PlayerHitbox.Part.LeftHand;
            rightHand = transform.GetChild(3);
            rightHandHitBox = rightHand.gameObject.AddComponent<H3MP_PlayerHitbox>();
            rightHandHitBox.manager = this;
            rightHandHitBox.part = H3MP_PlayerHitbox.Part.RightHand;
            overheadDisplayBillboard = transform.GetChild(4).GetChild(0).GetChild(0).gameObject.AddComponent<H3MP_Billboard>();
            usernameLabel = transform.GetChild(4).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            healthIndicator = transform.GetChild(4).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
            headMat = head.GetComponent<Renderer>().material;
            torsoMat = torso.GetComponent<Renderer>().material;
            leftHandMat = leftHand.GetComponent<Renderer>().material;
            rightHandMat = rightHand.GetComponent<Renderer>().material;
        }

        public void Damage(H3MP_PlayerHitbox.Part part, Damage damage)
        {
            if (ID != -1)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.PlayerDamage(ID, (byte)part, damage);
                }
                else
                {
                    H3MP_ClientSend.PlayerDamage(ID, (byte)part, damage);
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
            overheadDisplayBillboard.gameObject.SetActive(visible && (H3MP_GameManager.nameplateMode == 0 || (H3MP_GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF)));
        
            SetEntitiesRegistered(visible);
        }

        public void SetIFF(int IFF)
        {
            this.IFF = IFF;
            if (headEntity != null)
            {
                headEntity.IFFCode = IFF;
                torsoEntity.IFFCode = IFF;
            }

            if (H3MP_GameManager.colorByIFF)
            {
                SetColor(IFF);
            }

            overheadDisplayBillboard.gameObject.SetActive(visible && (H3MP_GameManager.nameplateMode == 0 || (H3MP_GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == IFF)));
        }

        public void SetColor(int colorIndex)
        {
            this.colorIndex = colorIndex % H3MP_GameManager.colors.Length;

            headMat.color = H3MP_GameManager.colors[colorIndex];
            torsoMat.color = H3MP_GameManager.colors[colorIndex];
            leftHandMat.color = H3MP_GameManager.colors[colorIndex];
            rightHandMat.color = H3MP_GameManager.colors[colorIndex];
        }
    }
}
