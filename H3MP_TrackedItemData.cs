using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public H3MP_TrackedItemData parent; // The item this item is attached to
        public List<H3MP_TrackedItemData> children; // The items attached to this item
        public int childIndex = -1; // The index of this item in its parent's children list

        // MOD: Mods with custom items with custom data that will be used to instantiate them 
        //      should postfix this method to add whatever data they want to object
        public GameObject Instantiate()
        {
            TODO

            SetData();
        }

        public void Update(H3MP_TrackedItemData updatedItem)
        {
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            if (physicalObject != null)
            {
                physicalObject.transform.position = updatedItem.position;
                physicalObject.transform.rotation = updatedItem.rotation;

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

            SetData(updatedItem.data);
        }

        public bool Update()
        {
            previousPos = position;
            previousRot = rotation;
            if (parent == null)
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

        // MOD: Mods with custom items with custom data that will be needed to instantiate them 
        //      should postfix this method to update the data based on the physical state of the item
        //      The return value should be set to true if data was modified
        private bool UpdateData()
        {
            previousData = data;
            return false;
        }

        // MOD: Mods with custom items with custom data that will be needed to instantiate them 
        //      should postfix this method to add whatever data they want to the data
        //      or to update the physicalObject depending on newData
        public void SetData(byte[] newData = null)
        {
            if (newData != null)
            {
                previousData = data;
                data = newData;
            }
        }
    }
}
