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

        public int trackedID = -1;
        public int controller;
        public TNH_EncryptionType type;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public H3MP_TrackedEncryption physicalObject;
        public int localTrackedID;
        public uint localWaitingIndex = uint.MaxValue;
        public bool previousActive;
        public bool active;

        public TNH_EncryptionTarget_SubTarget[] subTargets;
        public bool[] tendrilsActive;
        public Vector3[] growthPoints;
        public Vector3[] subTargsPos;
        public bool[] subTargsActive;
        public float[] tendrilFloats;
        public Quaternion[] tendrilsRot;
        public Vector3[] tendrilsScale;
        public bool removeFromListOnDestroy = true;
        public string scene;
        public int instance;

        public IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating encryption " + trackedID);
            GameObject prefab = null;
            if (GM.TNH_Manager == null)
            {
                yield return IM.OD[EncryptionTypeToID(type)].GetGameObjectAsync();
                prefab = IM.OD[EncryptionTypeToID(type)].GetGameObject();
            }
            else
            {
                prefab = GM.TNH_Manager.GetEncryptionPrefab(type).GetGameObject();
            }
            if(prefab == null)
            {
                Mod.LogError($"Attempted to instantiate encryption {type} sent from {controller} but failed to get prefab.");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject encryptionInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalObject = encryptionInstance.AddComponent<H3MP_TrackedEncryption>();
            physicalObject.data = this;

            physicalObject.physicalEncryptionScript = encryptionInstance.GetComponent<TNH_EncryptionTarget>();

            H3MP_GameManager.trackedEncryptionByEncryption.Add(physicalObject.physicalEncryptionScript, physicalObject);

            // Register to hold
            if (GM.TNH_Manager != null)
            {
                TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(GM.TNH_Manager);
                physicalObject.physicalEncryptionScript.SetHoldPoint(curHoldPoint);
                curHoldPoint.RegisterNewTarget(physicalObject.physicalEncryptionScript);
            }

            // Keep references to sub targets
            subTargets = new TNH_EncryptionTarget_SubTarget[physicalObject.physicalEncryptionScript.SubTargs.Count];
            for(int i=0; i < subTargets.Length; ++i)
            {
                subTargets[i] = physicalObject.physicalEncryptionScript.SubTargs[i].GetComponent<TNH_EncryptionTarget_SubTarget>();
            }

            // Init growths
            int numSubTargsLeft = 0;
            if (physicalObject.physicalEncryptionScript.UsesRegenerativeSubTarg)
            {
                for (int i = 0; i < tendrilsActive.Length; ++i)
                {
                    if (tendrilsActive[i])
                    {
                        physicalObject.physicalEncryptionScript.Tendrils[i].SetActive(true);
                        physicalObject.physicalEncryptionScript.GrowthPoints[i] = growthPoints[i];
                        physicalObject.physicalEncryptionScript.SubTargs[i].transform.position = subTargsPos[i];
                        physicalObject.physicalEncryptionScript.SubTargs[i].SetActive(true);
                        physicalObject.physicalEncryptionScript.TendrilFloats[i] = 1f;
                        physicalObject.physicalEncryptionScript.Tendrils[i].transform.rotation = tendrilsRot[i];
                        physicalObject.physicalEncryptionScript.Tendrils[i].transform.localScale = tendrilsScale[i];
                        physicalObject.physicalEncryptionScript.SubTargs[i].transform.rotation = UnityEngine.Random.rotation;
                        ++numSubTargsLeft;
                    }
                }
            }
            else if (physicalObject.physicalEncryptionScript.UsesRecursiveSubTarg)
            {
                for (int i = 0; i < subTargsActive.Length; ++i)
                {
                    if (subTargsActive[i])
                    {
                        physicalObject.physicalEncryptionScript.SubTargs[i].SetActive(true);
                        ++numSubTargsLeft;
                    }
                }
            }
            Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(physicalObject.physicalEncryptionScript, numSubTargsLeft);

            // Initially set itself
            Update(this);
        }

        public void Update(H3MP_TrackedEncryptionData updatedItem, bool full = false)
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
                if (physicalObject.physicalEncryptionScript.RB != null)
                {
                    physicalObject.physicalEncryptionScript.RB.position = position;
                    physicalObject.physicalEncryptionScript.RB.rotation = rotation;
                }
                else
                {
                    physicalObject.physicalEncryptionScript.transform.position = position;
                    physicalObject.physicalEncryptionScript.transform.rotation = rotation;
                }

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

                if (full)
                {
                    int numSubTargsLeft = 0;
                    if (physicalObject.physicalEncryptionScript.UsesRegenerativeSubTarg)
                    {
                        for (int i = 0; i < tendrilsActive.Length; ++i)
                        {
                            if (tendrilsActive[i])
                            {
                                physicalObject.physicalEncryptionScript.Tendrils[i].SetActive(true);
                                physicalObject.physicalEncryptionScript.GrowthPoints[i] = growthPoints[i];
                                physicalObject.physicalEncryptionScript.SubTargs[i].transform.position = subTargsPos[i];
                                physicalObject.physicalEncryptionScript.SubTargs[i].SetActive(true);
                                physicalObject.physicalEncryptionScript.TendrilFloats[i] = 1f;
                                physicalObject.physicalEncryptionScript.Tendrils[i].transform.rotation = tendrilsRot[i];
                                physicalObject.physicalEncryptionScript.Tendrils[i].transform.localScale = tendrilsScale[i];
                                physicalObject.physicalEncryptionScript.SubTargs[i].transform.rotation = UnityEngine.Random.rotation;
                                ++numSubTargsLeft;
                            }
                        }
                    }
                    else if (physicalObject.physicalEncryptionScript.UsesRecursiveSubTarg)
                    {
                        for (int i = 0; i < subTargsActive.Length; ++i)
                        {
                            if (subTargsActive[i])
                            {
                                physicalObject.physicalEncryptionScript.SubTargs[i].SetActive(true);
                                ++numSubTargsLeft;
                            }
                        }
                    }
                    Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(physicalObject.physicalEncryptionScript, numSubTargsLeft);
                }
            }
        }

        public bool Update(bool full = false)
        {
            previousPos = position;
            previousRot = rotation;
            if (physicalObject.physicalEncryptionScript.RB != null)
            {
                position = physicalObject.physicalEncryptionScript.RB.position;
                rotation = physicalObject.physicalEncryptionScript.RB.rotation;
            }
            else
            {
                position = physicalObject.physicalEncryptionScript.transform.position;
                rotation = physicalObject.physicalEncryptionScript.transform.rotation;
            }

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            if (full)
            {
                if (physicalObject.physicalEncryptionScript.UsesRegenerativeSubTarg)
                {
                    for (int i = 0; i < physicalObject.physicalEncryptionScript.Tendrils.Count; ++i)
                    {
                        if (physicalObject.physicalEncryptionScript.Tendrils[i].activeSelf)
                        {
                            tendrilsActive[i] = true;
                            growthPoints[i] = physicalObject.physicalEncryptionScript.GrowthPoints[i];
                            subTargsPos[i] = physicalObject.physicalEncryptionScript.SubTargs[i].transform.position;
                            subTargsActive[i] = physicalObject.physicalEncryptionScript.SubTargs[i];
                            tendrilFloats[i] = physicalObject.physicalEncryptionScript.TendrilFloats[i];
                            tendrilsRot[i] = physicalObject.physicalEncryptionScript.Tendrils[i].transform.rotation;
                            tendrilsScale[i] = physicalObject.physicalEncryptionScript.Tendrils[i].transform.localScale;
                        }
                    }
                }
                else if (physicalObject.physicalEncryptionScript.UsesRecursiveSubTarg)
                {
                    for (int i = 0; i < physicalObject.physicalEncryptionScript.SubTargs.Count; ++i)
                    {
                        if (physicalObject.physicalEncryptionScript.SubTargs[i] != null && physicalObject.physicalEncryptionScript.SubTargs[i].activeSelf)
                        {
                            subTargsActive[i] = physicalObject.physicalEncryptionScript.SubTargs[i].activeSelf;
                        }
                    }
                }
            }

            return NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active;
        }

        public static string EncryptionTypeToID(TNH_EncryptionType type)
        {
            switch (type)
            {
                case TNH_EncryptionType.Agile:
                    return "TNH_EncryptionTarget_6_Agile";
                case TNH_EncryptionType.Cascading:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Hardened:
                    return "TNH_EncryptionTarget_2_Hardened";
                case TNH_EncryptionType.Orthagonal:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Polymorphic:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Recursive:
                    return "TNH_EncryptionTarget_4_Recursive";
                case TNH_EncryptionType.Refractive:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Regenerative:
                    return "TNH_EncryptionTarget_7_Regenerative";
                case TNH_EncryptionType.Static:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Stealth:
                    return "TNH_EncryptionTarget_5_Stealth";
                case TNH_EncryptionType.Swarm:
                    return "TNH_EncryptionTarget_3_Swarm";
                default:
                    Mod.LogError("EncryptionTypeToID unhandled type: "+type);
                    return "TNH_EncryptionTarget_1_Static";
            }
        }

        public void OnTrackedIDReceived()
        {
            if (H3MP_TrackedEncryption.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                H3MP_ClientSend.DestroyEncryption(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                H3MP_Client.encryptions[trackedID] = null;

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = H3MP_TrackedEncryption.unknownControlTrackedIDs[localWaitingIndex];

                H3MP_ClientSend.GiveEncryptionControl(trackedID, newController);

                // Also change controller locally
                controller = newController;

                H3MP_TrackedEncryption.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (H3MP_GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownInit.ContainsKey(localWaitingIndex))
            {
                List<int> indices = H3MP_TrackedEncryption.unknownInit[localWaitingIndex];

                H3MP_ClientSend.EncryptionInit(trackedID, indices);

                H3MP_TrackedEncryption.unknownInit.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownSpawnSubTarg.ContainsKey(localWaitingIndex))
            {
                List<int> indices = H3MP_TrackedEncryption.unknownSpawnSubTarg[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    H3MP_ClientSend.EncryptionRespawnSubTarg(trackedID, indices[i]);
                }

                H3MP_TrackedEncryption.unknownSpawnSubTarg.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownDisableSubTarg.ContainsKey(localWaitingIndex))
            {
                List<int> indices = H3MP_TrackedEncryption.unknownDisableSubTarg[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    H3MP_ClientSend.EncryptionDisableSubtarg(trackedID, indices[i]);
                }

                H3MP_TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownSpawnGrowth.ContainsKey(localWaitingIndex))
            {
                List<KeyValuePair<int, Vector3>> indices = H3MP_TrackedEncryption.unknownSpawnGrowth[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    H3MP_ClientSend.EncryptionSpawnGrowth(trackedID, indices[i].Key, indices[i].Value);
                }

                H3MP_TrackedEncryption.unknownSpawnGrowth.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedEncryption.unknownResetGrowth.ContainsKey(localWaitingIndex))
            {
                List<KeyValuePair<int, Vector3>> indices = H3MP_TrackedEncryption.unknownResetGrowth[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    H3MP_ClientSend.EncryptionResetGrowth(trackedID, indices[i].Key, indices[i].Value);
                }

                H3MP_TrackedEncryption.unknownResetGrowth.Remove(localWaitingIndex);
            }

            if (localTrackedID != -1)
            {
                // Add to encryption tracking list
                if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(instance, out List<int> encryptionList))
                    {
                        encryptionList.Add(trackedID);
                    }
                    else
                    {
                        relevantInstances.Add(instance, new List<int>() { trackedID });
                    }
                }
                else
                {
                    Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                    newInstances.Add(instance, new List<int>() { trackedID });
                    H3MP_GameManager.encryptionsByInstanceByScene.Add(scene, newInstances);
                }
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            H3MP_TrackedEncryption.unknownControlTrackedIDs.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownDestroyTrackedIDs.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownInit.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownSpawnGrowth.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownResetGrowth.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownSpawnSubTarg.Remove(localWaitingIndex);
            H3MP_TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);

            // Remove from actual local encryptions list and update the localTrackedID of the encryption we are moving
            H3MP_GameManager.encryptions[localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
            H3MP_GameManager.encryptions[localTrackedID].localTrackedID = localTrackedID;
            H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
            localTrackedID = -1;
        }
    }
}
