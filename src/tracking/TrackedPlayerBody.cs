using FistVR;
using H3MP.Scripts;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedPlayerBody : TrackedObject
    {
        public TrackedPlayerBodyData playerBodyData;
        public PlayerBody physicalPlayerBody;

        public PlayerManager playerManager;

        public override void Awake()
        {
            base.Awake();

            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;
            GameManager.OnInstanceJoined += OnInstanceJoined;
        }

        public virtual void Start()
        {
            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                if (GM.CurrentPlayerBody != null)
                {
                    physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                    physicalPlayerBody.handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                    physicalPlayerBody.handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                    physicalPlayerBody.handScripts = new FVRViveHand[2]
                    {
                        GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(),
                        GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>()
                    };
                    if (physicalPlayerBody.headDisplayMode != PlayerBody.HeadDisplayMode.Physical)
                    {
                        physicalPlayerBody.SetHeadVisible(false);
                    }
                    physicalPlayerBody.SetCollidersEnabled(false);
                    physicalPlayerBody.SetCanvasesEnabled(false);
                    physicalPlayerBody.SetEntitiesRegistered(false);

                    physicalPlayerBody.Init();
                }
            }
            else
            {
                playerManager = GameManager.players[playerBodyData.controller];
                playerManager.playerBody = this;

                physicalPlayerBody.headToFollow = playerManager.head;
                physicalPlayerBody.handsToFollow[0] = playerManager.leftHand;
                physicalPlayerBody.handsToFollow[1] = playerManager.rightHand;
                if (physicalPlayerBody.headDisplayMode != PlayerBody.HeadDisplayMode.Physical)
                {
                    physicalPlayerBody.SetHeadVisible(true);
                }
                physicalPlayerBody.SetCollidersEnabled(true);
                // Note: Canvas visibility being dependent on nameplate mode and CurrentPlayerBody being set, we instead use PlayerManager.SetIFF
                //       in our Init call which will also check that
                // physicalPlayerBody.SetCanvasesEnabled(true);
                physicalPlayerBody.SetEntitiesRegistered(true);

                Init();
            }
        }

        private void Init()
        {
            playerManager.SetIFF(playerManager.IFF);
            physicalPlayerBody.SetColor(GameManager.colors[playerManager.colorIndex]);

            if (physicalPlayerBody.usernameLabel != null)
            {
                physicalPlayerBody.usernameLabel.text = playerManager.username;
            }
            UpdateHealthLabel();

            if (physicalPlayerBody.hitboxes != null) 
            {
                for (int i = 0; i < physicalPlayerBody.hitboxes.Length; ++i)
                {
                    if(physicalPlayerBody.hitboxes[i] != null)
                    {
                        physicalPlayerBody.hitboxes[i].manager = playerManager;
                    }
                    
                }
            }
        }

        public void UpdateHealthLabel()
        {
            if (physicalPlayerBody.healthLabel != null)
            {
                physicalPlayerBody.healthLabel.text = ((int)playerManager.health) + "/" + playerManager.maxHealth;
            }
        }

        private void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            if (playerBodyData.controller == GameManager.ID)
            {
                physicalPlayerBody.headToFollow = playerBody.Head;
                if(physicalPlayerBody.handsToFollow == null)
                {
                    physicalPlayerBody.handsToFollow = new Transform[2];
                }
                physicalPlayerBody.handsToFollow[0] = playerBody.LeftHand;
                physicalPlayerBody.handsToFollow[1] = playerBody.RightHand;
                physicalPlayerBody.handScripts = new FVRViveHand[2]
                {
                    GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(),
                    GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>()
                };
                if (physicalPlayerBody.headDisplayMode != PlayerBody.HeadDisplayMode.Physical)
                {
                    physicalPlayerBody.SetHeadVisible(false);
                }
                physicalPlayerBody.SetCollidersEnabled(false);
                physicalPlayerBody.SetCanvasesEnabled(false);
                physicalPlayerBody.SetEntitiesRegistered(false);

                physicalPlayerBody.Init();
            }
            else if (playerManager != null)
            {
                playerManager.SetIFF(playerManager.IFF);
            }
        }

        public virtual void Update()
        {
            if(physicalPlayerBody.headToFollow == null)
            {
                if (playerBodyData.controller == GameManager.ID)
                {
                    if (GM.CurrentPlayerBody != null)
                    {
                        physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                        if (physicalPlayerBody.headDisplayMode != PlayerBody.HeadDisplayMode.Physical)
                        {
                            physicalPlayerBody.SetHeadVisible(false);
                        }
                    }
                }
                else
                {
                    physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                    if (physicalPlayerBody.headDisplayMode != PlayerBody.HeadDisplayMode.Physical)
                    {
                        physicalPlayerBody.SetHeadVisible(true);
                    }
                }
            }
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            // We want to bring our player body with us across instances no matter what
            if(data.controller == GameManager.ID)
            {
                data.SetInstance(instance, true);
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            for (int i = 0; i < physicalPlayerBody.hitboxes.Length; ++i)
            {
                GameManager.trackedObjectByDamageable.Remove(physicalPlayerBody.hitboxes[i]);
            }

            base.OnDestroy();
        }
    }
}
