using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedEncryptionData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this Encryption's data packet used to ensure we process this data in the correct order

        public int trackedID;
        public int controller;
        public TNH_EncryptionType type;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public H3MP_TrackedEncryption physicalObject;
        public int localTrackedID;
        public bool previousActive;
        public bool active;

        public IEnumerator Instantiate()
        {
            if(GM.TNH_Manager == null)
            {
                yield break;
            }
            GameObject prefab = GM.TNH_Manager.GetEncryptionPrefab(type).GetGameObject();

            ++Mod.skipAllInstantiates;
            GameObject encryptionInstance = GameObject.Instantiate(prefab);
            --Mod.skipAllInstantiates;
            physicalObject = encryptionInstance.AddComponent<H3MP_TrackedEncryption>();
            physicalObject.data = this;

            physicalObject.physicalEncryptionScript = encryptionInstance.GetComponent<TNH_EncryptionTarget>();

            H3MP_GameManager.trackedEncryptionByEncryption.Add(physicalObject.physicalEncryptionScript, physicalObject);

            // Register to hold
            TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(GM.TNH_Manager);
            TNH_EncryptionTarget encryptionTarget = encryptionInstance.GetComponent<TNH_EncryptionTarget>();
            encryptionTarget.SetHoldPoint(curHoldPoint);
            curHoldPoint.RegisterNewTarget(encryptionTarget);

            // Initially set itself
            Update(this);
        }

        public void Update(H3MP_TrackedEncryptionData updatedItem)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            previousActive = active;
            active = updatedItem.active;

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalEncryptionScript.RB.position = position;
                physicalObject.physicalEncryptionScript.RB.rotation = rotation;

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
        }

        public bool Update(bool full = false)
        {
            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalEncryptionScript.RB.position;
            rotation = physicalObject.physicalEncryptionScript.RB.rotation;

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            return NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active;
        }
    }
}
