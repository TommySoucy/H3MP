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
        public UpdateData updateFunc; // Update the item's data based on its physical state since we are the controller
        public UpdateDataWithGiven updateGivenFunc; // Update the item's data based on data provided by another client
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

            TODO Update data about chambers, attachments(?), mag(?), etc.

            return false;
        }

        private bool UpdateGivenFirearm(byte[] newData)
        {
            FVRFireArm asFirearm = dataObject as FVRFireArm;

            TODO Update data about chambers, attachments(?), mag(?), etc.

            return false;
        }

        private bool UpdateMagazine()
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            int necessarySize = asMag.m_numRounds * 2 + 3;

            if(data.data == null || data.data.Length < necessarySize)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];
            byte preval1 = data.data[1];

            // Write count of loaded rounds
            BitConverter.GetBytes((short)asMag.m_numRounds).CopyTo(data.data, 0);

            modified |= (preval0 != data.data[0] || preval1 != data.data[1]);

            // Write loaded round classes
            for (int i=0; i < asMag.m_numRounds; ++i)
            {
                preval0 = data.data[i * 2 + 2];
                preval1 = data.data[i * 2 + 3];

                BitConverter.GetBytes((short)asMag.LoadedRounds[i].LR_Class).CopyTo(data.data, i * 2 + 2);

                modified |= (preval0 != data.data[i * 2 + 2] || preval1 != data.data[i * 2 + 3]);
            }

            // Write loaded into firearm
            BitConverter.GetBytes(asMag.FireArm != null).CopyTo(data.data, necessarySize - 1);

            return modified;
        }

        private bool UpdateGivenMagazine(byte[] newData)
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            if (data.data == null || data.data.Length != newData.Length)
            {
                modified = true;
            }

            asMag.m_numRounds = 0;
            short numRounds = BitConverter.ToInt16(newData, 0);

            for (int i = 0; i < numRounds; ++i)
            {
                int first = i * 2 + 2;
                FireArmRoundClass newClass = (FireArmRoundClass)BitConverter.ToInt16(newData, first);
                if(newClass != asMag.LoadedRounds[i].LR_Class)
                {
                    asMag.AddRound(newClass, false, false);
                    modified = true;
                }
            }

            data.data = newData;

            if (modified)
            {
                asMag.UpdateBulletDisplay();
            }

            return modified;
        }

        private bool UpdateClip()
        {
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            TODO Update data about contained rounds

            return false;
        }

        private bool UpdateGivenClip(byte[] newData)
        {
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            TODO Update data about contained rounds

            return false;
        }

        private bool UpdateSpeedloader()
        {
            Speedloader asSpeedloader = dataObject as Speedloader;

            TODO Update data about contained rounds

            return false;
        }

        private bool UpdateGivenSpeedloader(byte[] newData)
        {
            Speedloader asSpeedloader = dataObject as Speedloader;

            TODO Update data about contained rounds

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
