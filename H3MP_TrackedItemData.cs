using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP
{
    public class H3MP_TrackedItemData
    {
        public int trackedID = -1; // This item's unique ID to identify it across systems (index in global items arrays)
        public int localtrackedID = -1; // This item's index in local items list 
        public int controller = 0; // Client controlling this item, 0 for host
        public bool active;
        private bool previousActive;

        // Data
        public string itemID; // The ID of this item so it can be spawned by clients and host
        public byte[] data; // MOD: This is what you would use to add custom data to items (Meatov dogtags have level, this is where it would be written)
        public byte[] previousData; 

        // State
        public Vector3 position;
        public Quaternion rotation;
        private Vector3 previousPos;
        private Quaternion previousRot;
        public H3MP_TrackedItem physicalObject;

        public int parent = -1; // The tracked ID of item this item is attached to
        public List<H3MP_TrackedItemData> children; // The items attached to this item
        public int childIndex = -1; // The index of this item in its parent's children list

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
