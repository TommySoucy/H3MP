using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP
{
    public class H3MP_TrackedItemData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this item's data packet used to ensure we process this data in the correct order

        public int trackedID = -1; // This item's unique ID to identify it across systems (index in global items arrays)
        public int localtrackedID = -1; // This item's index in local items list 
        public int controller = 0; // Client controlling this item, 0 for host
        public bool active;
        private bool previousActive;

        // Data
        public string itemID; // The ID of this item so it can be spawned by clients and host
        public byte[] previousData;
        public byte[] data;

        // Item type specific data strcuture:
        /**
         * FVRFireArmMagazine
         *  0: 
         *  1: Loaded round count (n)
         *      0:
         *      1: Round class
         *      ...
         *      n * 2 - 2:
         *      n * 2 - 1: Round class
         *  2: Loaded in parent
         */
        /**
         * FVRFireArmClip
         *  0: 
         *  1: Loaded round count (n)
         *      0:
         *      1: Round class
         *      ...
         *      n * 2 - 2:
         *      n * 2 - 1: Round class
         *  2: Loaded in parent
         */
        /**
         * Speedloader
         *  0:
         *  1: Chamber Round class (-1 for null)
         *  ...
         *  Chambers.Count * 2 - 2:
         *  Chambers.Count * 2 - 1: Round class
         */
        /**
         * ClosedBoltWeapon
         *  0: m_fireSelectorMode index
         *  1: m_CamBurst
         *  2: m_isHammerCocked
         *  3:
         *  4: Chamber Round class (-1 for null)
         */
        /**
         * BoltActionRifle
         *  0: m_fireSelectorMode index
         *  1: m_isHammerCocked
         *  2:
         *  3: Chamber Round class (-1 for null)
         *  4: CurBoltHandleState
         *  5: BoltHandle.HandleRot
         */
        /**
         * Handgun
         *  0: m_fireSelectorMode index
         *  1: m_CamBurst
         *  2: m_isHammerCocked
         *  3:
         *  4: Chamber Round class (-1 for null)
         */
        /**
         * TubeFedShotgun
         *  0: m_fireSelectorMode index
         *  1: m_isHammerCocked
         *  2:
         *  3: Chamber Round class (-1 for null)
         *  4: Bolt.CurPos
         *  5: Handle.CurPos
         */
        /**
         * Speedloader
         *  0:
         *  1: Round class
         *  ...
         *  Chambers.Count * 2 - 2:
         *  Chambers.Count * 2 - 1: Round class
         */

        // State
        public Vector3 position;
        public Quaternion rotation;
        private Vector3 previousPos;
        private Quaternion previousRot;
        public H3MP_TrackedItem physicalObject;

        public int parent = -1; // The tracked ID of item this item is attached to
        public List<H3MP_TrackedItemData> children; // The items attached to this item
        public int childIndex = -1; // The index of this item in its parent's children list
        public bool ignoreParentChanged;

        public IEnumerator Instantiate()
        {
            GameObject itemPrefab = GetItemPrefab();
            if(itemPrefab == null)
            {
                yield return IM.OD[itemID].GetGameObjectAsync();
                itemPrefab = IM.OD[itemID].GetGameObject();
            }
            if(itemPrefab == null)
            {
                Debug.LogError($"Attempted to instantiate {itemID} sent from {controller} but failed to get item prefab.");
                yield break;
            }

            GameObject itemObject = GameObject.Instantiate(itemPrefab);
            physicalObject = itemObject.AddComponent<H3MP_TrackedItem>();
            physicalObject.data = this;

            // See Note in H3MP_GameManager.SyncTrackedItems
            if(parent != -1)
            {
                // Add ourselves to the prent's children
                H3MP_TrackedItemData parentItem = (H3MP_ThreadManager.host ? H3MP_Server.items : H3MP_Client.items)[parent];
                if(parentItem.children == null)
                {
                    parentItem.children = new List<H3MP_TrackedItemData>();
                }
                childIndex = parentItem.children.Count;
                parentItem.children.Add(this);

                // Physically parent
                ignoreParentChanged = true;
                itemObject.transform.parent = parentItem.physicalObject.transform;

                // If parented, the position and rotation are relative, so set it now after parenting
                itemObject.transform.localPosition = position;
                itemObject.transform.localRotation = rotation;
            }
            else
            {
                itemObject.transform.position = position;
                itemObject.transform.rotation = rotation;
            }

            // Initially set itself
            Update(this);
        }

        // MOD: If a mod keeps its item prefabs in a different location than IM.OD, this is what should be patched to find it
        //      If this returns null, it will try to find the item in IM.OD
        private GameObject GetItemPrefab()
        {
            return null;
        }

        public void Update(H3MP_TrackedItemData updatedItem)
        {
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            if (physicalObject != null)
            {
                if (parent == -1)
                {
                    physicalObject.transform.position = updatedItem.position;
                    physicalObject.transform.rotation = updatedItem.rotation;
                }
                else
                {
                    physicalObject.transform.localPosition = updatedItem.position;
                    physicalObject.transform.localRotation = updatedItem.rotation;
                }

                previousActive = active;
                active = updatedItem.active;
                if (active)
                {
                    if (!physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(false);
                    }
                }
            }

            UpdateData(updatedItem.data);
        }

        public bool Update()
        {
            previousPos = position;
            previousRot = rotation;
            if (parent == -1)
            {
                position = physicalObject.transform.position;
                rotation = physicalObject.transform.rotation;
            }
            else
            {
                position = physicalObject.transform.localPosition;
                rotation = physicalObject.transform.localRotation;
            }

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            return previousActive != active || !previousPos.Equals(position) || !previousRot.Equals(rotation) || UpdateData();
        }

        public bool NeedsUpdate()
        {
            return previousActive != active || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !DataEqual();
        }

        public void SetParent(H3MP_TrackedItemData newParent)
        {
            if (newParent == null)
            {
                if (parent != -1) // We had parent before, need to unparent
                {
                    H3MP_TrackedItemData previousParent = null;
                    if (H3MP_ThreadManager.host)
                    {
                        previousParent = H3MP_Server.items[parent];
                    }
                    else
                    {
                        previousParent = H3MP_Client.items[parent];
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                    parent = -1;
                    childIndex = -1;

                    // Physically unparent
                    if (physicalObject != null)
                    {
                        ignoreParentChanged = true;
                        physicalObject.transform.parent = GetGeneralParent();
                    }
                }
                // Already unparented, nothing changes
            }
            else // We have new parent
            {
                if (parent != -1) // We had parent before, need to unparent first
                {
                    H3MP_TrackedItemData previousParent = null;
                    if (H3MP_ThreadManager.host)
                    {
                        previousParent = H3MP_Server.items[parent];
                    }
                    else
                    {
                        previousParent = H3MP_Client.items[parent];
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                }

                // Set new parent
                parent = newParent.trackedID;
                if (newParent.children == null)
                {
                    newParent.children = new List<H3MP_TrackedItemData>();
                }
                childIndex = newParent.children.Count;
                newParent.children.Add(this);

                // Physically parent
                if (physicalObject != null)
                {
                    ignoreParentChanged = true;
                    physicalObject.transform.parent = newParent.physicalObject.transform;
                }
            }
        }

        public void SetParent(int trackedID)
        {
            if (trackedID == -1)
            {
                SetParent(null);
            }
            else
            {
                if (H3MP_ThreadManager.host)
                {
                    SetParent(H3MP_Server.items[trackedID]);
                }
                else
                {
                    SetParent(H3MP_Client.items[trackedID]);
                }
            }
        }

        // MOD: When unparented, an item will have its transform's parent set to null
        //      If you want it to be set to a specific transform, patch this to return the transform you want
        public Transform GetGeneralParent()
        {
            return null;
        }

        private bool DataEqual()
        {
            if(data == null && previousData == null)
            {
                return false;
            }
            if((data == null && previousData != null)||(data != null && previousData == null)||data.Length != previousData.Length)
            {
                return true;
            }
            for(int i=0; i < data.Length; ++i)
            {
                if (data[i] != previousData.Length)
                {
                    return true;
                }
            }
            return false;
        }

        private bool UpdateData(byte[] newData = null)
        {
            previousData = data;

            return physicalObject.UpdateItemData(newData);
        }
    }
}
