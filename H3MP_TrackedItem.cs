using FistVR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public H3MP_TrackedItemData data;

        // Update
        public delegate bool UpdateData(); // The updateFunc and updateGivenFunc should return a bool indicating whether data has been modified
        public delegate bool UpdateDataWithGiven(byte[] newData);
        public UpdateData updateFunc;
        public UpdateDataWithGiven updateGivenFunc;
        public UnityEngine.Object dataObject;

        public bool sendDestroy = true; // To prevent feeback loops

        private void Awake()
        {
            InitItemType();
        }

        // MOD: This will check which type this item is so we can keep track of its data more efficiently
        //      A mod with a custom item type which has custom data should postfix this to check if this item is of custom type
        //      to keep a ref to both the type and object itself
        private void InitItemType()
        {
            FVRPhysicalObject physObj = GetComponent<FVRPhysicalObject>();

            // For each relevant type for which we may want to store additional data, we set a specific update function and the object ref
            if(physObj is FVRFireArm)
            {
                updateFunc = UpdateFirearm;
                updateGivenFunc = UpdateGivenFirearm;
                dataObject = physObj as FVRFireArm;
            }
            else if(physObj is FVRFireArmMagazine)
            {
                updateFunc = UpdateMagazine;
                updateGivenFunc = UpdateGivenMagazine;
                dataObject = physObj as FVRFireArmMagazine;
            }
            else if(physObj is FVRFireArmClip)
            {
                updateFunc = UpdateClip;
                updateGivenFunc = UpdateGivenClip;
                dataObject = physObj as FVRFireArmClip;
            }
            else if(physObj is Speedloader)
            {
                updateFunc = UpdateSpeedloader;
                updateGivenFunc = UpdateGivenSpeedloader;
                dataObject = physObj as Speedloader;
            }
        }

        public bool UpdateItemData(byte[] newData = null)
        {
            if(dataObject != null)
            {
                if(newData != null)
                {
                    return updateGivenFunc(newData);
                }
                else
                {
                    return updateFunc();
                }
            }

            return false;
        }

        private bool UpdateFirearm()
        {
            FVRFireArm asFirearm = dataObject as FVRFireArm;

            // TODO Update data about chambers, attachments(?), mag(?), etc.

            return false;
        }

        private bool UpdateGivenFirearm(byte[] newData)
        {
            FVRFireArm asFirearm = dataObject as FVRFireArm;

            // TODO Update data about chambers, attachments(?), mag(?), etc.

            return false;
        }

        private bool UpdateMagazine()
        {
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            // TODO Update data about contained rounds

            return false;
        }

        private bool UpdateGivenMagazine(byte[] newData)
        {
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            TODO Update data about contained rounds and about it attachment state

            return false;
        }

        private bool UpdateClip()
        {
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            // TODO Update data about contained rounds

            return false;
        }

        private bool UpdateGivenClip(byte[] newData)
        {
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            // TODO Update data about contained rounds

            return false;
        }

        private bool UpdateSpeedloader()
        {
            Speedloader asSpeedloader = dataObject as Speedloader;

            // TODO Update data about contained rounds

            return false;
        }

        private bool UpdateGivenSpeedloader(byte[] newData)
        {
            Speedloader asSpeedloader = dataObject as Speedloader;

            // TODO Update data about contained rounds

            return false;
        }

        private void OnDestroy()
        {
            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ServerSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Server.items[data.trackedID] = null;
                    H3MP_Server.availableItemIndices.Add(data.trackedID);
                }
                if(data.controller == 0)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
            else
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ClientSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ClientSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Client.items[data.trackedID] = null;
                }
                if (data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
        }

        private void OnTransformParentChanged()
        {
            if (data.ignoreParentChanged)
            {
                data.ignoreParentChanged = false;
                return;
            }

            if(data.controller == (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID))
            {
                Transform currentParent = transform.parent;
                H3MP_TrackedItem parentTrackedItem = null;
                while (currentParent != null)
                {
                    parentTrackedItem = currentParent.GetComponent<H3MP_TrackedItem>();
                    if(parentTrackedItem != null)
                    {
                        break;
                    }
                }
                if(parentTrackedItem != null)
                {
                    if(parentTrackedItem.data.trackedID != data.parent)
                    {
                        // We have a parent trackedItem and it is new
                        // Update other clients
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                        }
                        else
                        {
                            H3MP_ClientSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                        }

                        // Update local
                        data.SetParent(parentTrackedItem.data);
                    }
                }
                else if(data.parent != -1)
                {
                    // We were detached from current parent
                    // Update other clients
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                    }
                    else
                    {
                        H3MP_ClientSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                    }

                    // Update locally
                    data.SetParent(null);
                }
            }
        }
    }
}
