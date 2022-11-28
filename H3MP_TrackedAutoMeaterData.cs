using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace H3MP
{
    public class H3MP_TrackedAutoMeaterData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this AutoMeater's data packet used to ensure we process this data in the correct order

        public int trackedID;
        public int controller;
        public byte ID; // 0: SMG, 1: Flak, 2: FlameThrower, 3: MachineGun, 4: Supression, 5: MF Blue, 6: MF Red
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion previousSideToSideRotation;
        public Quaternion sideToSideRotation;
        public float previousHingeTargetPos;
        public float hingeTargetPos;
        public Quaternion previousUpDownMotorRotation;
        public Quaternion upDownMotorRotation;
        public float previousUpDownJointTargetPos;
        public float upDownJointTargetPos;
        public H3MP_TrackedAutoMeater physicalObject;
        public int localTrackedID;
        public bool previousActive;
        public bool active;
        public byte previousIFF;
        public byte IFF;

        public Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone> hitZones = new Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone>();

        public IEnumerator Instantiate()
        {
            string itemID = AutoMeaterIDToItemID(ID);
            yield return IM.OD[itemID].GetGameObjectAsync();
            GameObject autoMeaterPrefab = IM.OD[itemID].GetGameObject();
            if (autoMeaterPrefab == null)
            {
                Debug.LogError($"Attempted to instantiate AutoMeater sent from {controller} but failed to get prefab.");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject autoMeaterInstance = GameObject.Instantiate(autoMeaterPrefab);
            --Mod.skipAllInstantiates;
            physicalObject = autoMeaterInstance.AddComponent<H3MP_TrackedAutoMeater>();
            physicalObject.data = this;

            physicalObject.physicalAutoMeaterScript = autoMeaterInstance.GetComponent<AutoMeater>();

            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Add(physicalObject.physicalAutoMeaterScript, physicalObject);

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != H3MP_GameManager.ID)
            {
                GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalAutoMeaterScript.E);
                physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
            }

            // Initially set IFF
            physicalObject.physicalAutoMeaterScript.E.IFFCode = IFF;

            // Get hitzones
            AutoMeaterHitZone[] hitZoneArr = physicalObject.GetComponentsInChildren<AutoMeaterHitZone>();
            foreach(AutoMeaterHitZone hitZone in hitZoneArr)
            {
                hitZones.Add(hitZone.Type, hitZone);
            }

            // Initially set itself
            Update(this);
        }

        public static string AutoMeaterIDToItemID(byte ID)
        {
            switch (ID)
            {
                case 0:
                    return "Turburgert_SMG";
                case 1:
                    return "Turburgert_Flak";
                case 2:
                    return "Turburgert_Flamethrower";
                case 3:
                    return "Turburgert_MachineGun";
                case 4:
                    return "Turburgert_Suppression";
                case 5:
                    return "TurburgertMFBlue";
                case 6:
                    return "TurburgertMFRed";
                default:
                    Debug.Log("AutoMeaterIDToItemID: Invalid auto meater ID: " + ID);
                    return "Turburgert_SMG";
            }
        }

        public void Update(H3MP_TrackedAutoMeaterData updatedItem)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            previousActive = active;
            active = updatedItem.active;
            previousIFF = IFF;
            IFF = updatedItem.IFF;
            previousSideToSideRotation = sideToSideRotation;
            sideToSideRotation = updatedItem.sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            hingeTargetPos = updatedItem.hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            upDownMotorRotation = updatedItem.upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            upDownJointTargetPos = updatedItem.upDownJointTargetPos;

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalAutoMeaterScript.RB.position = position;
                physicalObject.physicalAutoMeaterScript.RB.rotation = rotation;
                physicalObject.physicalAutoMeaterScript.E.IFFCode = IFF;
                physicalObject.physicalAutoMeaterScript.SideToSideTransform.localRotation = sideToSideRotation;
                HingeJoint hingeJoint = physicalObject.physicalAutoMeaterScript.SideToSideHinge;
                JointSpring spring = hingeJoint.spring;
                spring.targetPosition = hingeTargetPos;
                hingeJoint.spring = spring;
                physicalObject.physicalAutoMeaterScript.UpDownTransform.localRotation = upDownMotorRotation;
                HingeJoint upDownHingeJoint = physicalObject.physicalAutoMeaterScript.UpDownHinge;
                spring = upDownHingeJoint.spring;
                spring.targetPosition = upDownJointTargetPos;
                upDownHingeJoint.spring = spring;

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
            position = physicalObject.physicalAutoMeaterScript.RB.position;
            rotation = physicalObject.physicalAutoMeaterScript.RB.rotation;

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;
            previousIFF = IFF;
            IFF = (byte)physicalObject.physicalAutoMeaterScript.E.IFFCode;

            previousSideToSideRotation = sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            sideToSideRotation = physicalObject.physicalAutoMeaterScript.SideToSideTransform.localRotation;
            hingeTargetPos = physicalObject.physicalAutoMeaterScript.SideToSideHinge.spring.targetPosition;
            upDownMotorRotation = physicalObject.physicalAutoMeaterScript.UpDownTransform.localRotation;
            upDownJointTargetPos = physicalObject.physicalAutoMeaterScript.UpDownHinge.spring.targetPosition;

            return NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active || !previousSideToSideRotation.Equals(sideToSideRotation) ||
                   !previousUpDownMotorRotation.Equals(upDownMotorRotation) || previousHingeTargetPos != hingeTargetPos || previousUpDownJointTargetPos != upDownJointTargetPos;
        }
    }
}
