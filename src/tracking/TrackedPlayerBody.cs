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

        public virtual void Start()
        {
            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;

            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                if (GM.CurrentPlayerBody != null)
                {
                    physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                    physicalPlayerBody.handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                    physicalPlayerBody.handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
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
                physicalPlayerBody.SetCanvasesEnabled(true);
                physicalPlayerBody.SetEntitiesRegistered(true);

                Init();
            }
        }

        private void Init()
        {
            physicalPlayerBody.SetIFF(playerManager.IFF);
            physicalPlayerBody.SetColor(GameManager.colors[playerManager.colorIndex]);

            physicalPlayerBody.usernameLabel.text = playerManager.username;
            physicalPlayerBody.healthLabel.text = playerManager.health+"/"+playerManager.maxHealth;
        }

        private void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                physicalPlayerBody.headToFollow = playerBody.Head;
                physicalPlayerBody.handsToFollow[0] = playerBody.LeftHand;
                physicalPlayerBody.handsToFollow[1] = playerBody.RightHand;
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

        protected override void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;

            base.OnDestroy();
        }

        protected override void OnSceneLeft(string scene, string destination) 
        {
            // We want to bring our body with us across scenes no matter what
            // In SP, this is handled by simply having bodies in DontDestroyOnLoad
            // In MP, we need to update data and let the network know of the change
            if(data.controller == GameManager.ID)
            {
                data.SetScene(destination, true);
            }
        }

        protected override void OnInstanceJoined(int instance, int source)
        {
            // We want to bring our body with us across instances no matter what
            // In SP, this is handled by simply having bodies in DontDestroyOnLoad
            // In MP, we need to update data and let the network know of the change
            if (data.controller == GameManager.ID)
            {
                data.SetInstance(instance, true);
            }
        }
    }
}
