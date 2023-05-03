using FistVR;
using H3MP.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedEncryptionData
    {
        public bool latestUpdateSent = false; // Whether the latest update of this data was sent
        public byte order; // The index of this Encryption's data packet used to ensure we process this data in the correct order

        public int trackedID = -1;
        public int controller;
        public TNH_EncryptionType type;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public TrackedEncryption physicalObject;
        public int localTrackedID;
        public uint localWaitingIndex = uint.MaxValue;
        public int initTracker;
        public bool previousActive;
        public bool active;

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
        public bool sceneInit;
        public bool awaitingInstantiation;

        public IEnumerator Instantiate()
        {
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
                awaitingInstantiation = false;
                yield break;
            }

            if (!awaitingInstantiation)
            {
                yield break;
            }

            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at encryption instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
            GameObject encryptionInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalObject = encryptionInstance.AddComponent<TrackedEncryption>();
            awaitingInstantiation = false;
            physicalObject.data = this;

            physicalObject.physicalEncryptionScript = encryptionInstance.GetComponent<TNH_EncryptionTarget>();

            GameManager.trackedEncryptionByEncryption.Add(physicalObject.physicalEncryptionScript, physicalObject);

            // Register to hold
            if (GM.TNH_Manager != null)
            {
                physicalObject.physicalEncryptionScript.SetHoldPoint(GM.TNH_Manager.m_curHoldPoint);
                GM.TNH_Manager.m_curHoldPoint.RegisterNewTarget(physicalObject.physicalEncryptionScript);
            }

            // Init growths
            physicalObject.physicalEncryptionScript.m_numSubTargsLeft = 0;
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
                        ++physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
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
                        ++physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
                    }
                }
            }

            // Initially set itself
            Update(this);
        }

        public void Update(TrackedEncryptionData updatedItem, bool full = false)
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
                    physicalObject.physicalEncryptionScript.m_numSubTargsLeft = 0;
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
                                ++physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
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
                                ++physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
                            }
                        }
                    }
                }
            }
        }

        public bool Update(bool full = false)
        {
            if (physicalObject == null)
            {
                return false;
            }

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
            if (TrackedEncryption.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                ClientSend.DestroyEncryption(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                Client.encryptions[trackedID] = null;

                // Remove from encryptionsByInstanceByScene
                GameManager.encryptionsByInstanceByScene[scene][instance].Remove(trackedID);

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = TrackedEncryption.unknownControlTrackedIDs[localWaitingIndex];

                ClientSend.GiveEncryptionControl(trackedID, newController, null);

                // Also change controller locally
                controller = newController;

                TrackedEncryption.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownInit.ContainsKey(localWaitingIndex))
            {
                List<int> indices = TrackedEncryption.unknownInit[localWaitingIndex].Key;
                List<Vector3> points = TrackedEncryption.unknownInit[localWaitingIndex].Value;

                ClientSend.EncryptionInit(trackedID, indices, points);

                TrackedEncryption.unknownInit.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownSpawnSubTarg.ContainsKey(localWaitingIndex))
            {
                List<int> indices = TrackedEncryption.unknownSpawnSubTarg[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    ClientSend.EncryptionRespawnSubTarg(trackedID, indices[i]);
                }

                TrackedEncryption.unknownSpawnSubTarg.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownDisableSubTarg.ContainsKey(localWaitingIndex))
            {
                List<int> indices = TrackedEncryption.unknownDisableSubTarg[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    ClientSend.EncryptionDisableSubtarg(trackedID, indices[i]);
                }

                TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownSpawnGrowth.ContainsKey(localWaitingIndex))
            {
                List<KeyValuePair<int, Vector3>> indices = TrackedEncryption.unknownSpawnGrowth[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    ClientSend.EncryptionSpawnGrowth(trackedID, indices[i].Key, indices[i].Value);
                }

                TrackedEncryption.unknownSpawnGrowth.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownResetGrowth.ContainsKey(localWaitingIndex))
            {
                List<KeyValuePair<int, Vector3>> indices = TrackedEncryption.unknownResetGrowth[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    ClientSend.EncryptionResetGrowth(trackedID, indices[i].Key, indices[i].Value);
                }

                TrackedEncryption.unknownResetGrowth.Remove(localWaitingIndex);
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            if (trackedID == -1)
            {
                TrackedEncryption.unknownControlTrackedIDs.Remove(localWaitingIndex);
                TrackedEncryption.unknownDestroyTrackedIDs.Remove(localWaitingIndex);
                TrackedEncryption.unknownInit.Remove(localWaitingIndex);
                TrackedEncryption.unknownSpawnGrowth.Remove(localWaitingIndex);
                TrackedEncryption.unknownResetGrowth.Remove(localWaitingIndex);
                TrackedEncryption.unknownSpawnSubTarg.Remove(localWaitingIndex);
                TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);
            }

            if (localTrackedID > -1 && localTrackedID < GameManager.encryptions.Count)
            {
                // Remove from actual local encryptions list and update the localTrackedID of the encryption we are moving
                GameManager.encryptions[localTrackedID] = GameManager.encryptions[GameManager.encryptions.Count - 1];
                GameManager.encryptions[localTrackedID].localTrackedID = localTrackedID;
                GameManager.encryptions.RemoveAt(GameManager.encryptions.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID out of range!:\n" + Environment.StackTrace);
            }
        }
    }
}
