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
            GameManager.OnSceneJoined += OnSceneJoined;
            GameManager.OnInstanceJoined += OnInstanceJoined;

            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                if (GM.CurrentPlayerBody != null)
                {
                    physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                    physicalPlayerBody.handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                    physicalPlayerBody.handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                    physicalPlayerBody.SetHeadVisible(false);
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
                physicalPlayerBody.SetHeadVisible(true);
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
                physicalPlayerBody.SetHeadVisible(false);
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
                        physicalPlayerBody.SetHeadVisible(false);
                    }
                }
                else
                {
                    physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                    physicalPlayerBody.SetHeadVisible(true);
                }
            }
        }

        private void OnSceneJoined(string scene)
        {
            // If this is our body, need to track scene change
            if(playerBodyData.controller == GameManager.ID)
            {
                TODO:
            }
        }

        private void OnInstanceJoined(int instance)
        {
            // If this is our body, need to track instance change
            if (playerBodyData.controller == GameManager.ID)
            {
                TODO: // Do we want to handle this on SetInstance, or here in the specific tracked objects
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;
            GameManager.OnSceneJoined -= OnSceneJoined;
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            base.OnDestroy();
        }
    }
}
