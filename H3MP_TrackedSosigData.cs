using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedSosigData
    {
        private static readonly FieldInfo sosigInvAmmoStores = typeof(SosigInventory).GetField("m_ammoStores", BindingFlags.NonPublic | BindingFlags.Instance);

        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this sosig's data packet used to ensure we process this data in the correct order

        public int trackedID;
        public int controller;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public int[] previousAmmoStores;
        public int[] ammoStores;
        public SosigConfigTemplate configTemplate;
        public H3MP_TrackedSosig physicalObject;
        public int localTrackedID;
        public bool previousActive;
        public bool active;
        public List<List<string>> wearables;
        public float[][] linkData;
        public byte IFF;

        public IEnumerator Instantiate()
        {
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            if (sosigPrefab == null)
            {
                Debug.LogError($"Attempted to instantiate sosig sent from {controller} but failed to get prefab.");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject sosigInstance = GameObject.Instantiate(sosigPrefab);
            --Mod.skipAllInstantiates;
            physicalObject = sosigInstance.AddComponent<H3MP_TrackedSosig>();
            physicalObject.data = this;

            physicalObject.physicalSosig = sosigInstance.GetComponent<Sosig>();
            SosigConfigurePatch.skipConfigure = true;
            physicalObject.physicalSosig.Configure(configTemplate);

            H3MP_GameManager.trackedSosigBySosig.Add(physicalObject.physicalSosig, physicalObject);

            if (H3MP_GameManager.waitingWearables.ContainsKey(trackedID))
            {
                if (wearables == null || wearables.Count == 0)
                {
                    wearables = H3MP_GameManager.waitingWearables[trackedID];
                }
                else
                {
                    List<List<string>> newWearables = H3MP_GameManager.waitingWearables[trackedID];
                    for(int i = 0; i < newWearables.Count; ++i)
                    {
                        for (int j = 0; j < newWearables.Count; ++j)
                        {
                            wearables[i].Add(newWearables[i][j]);
                        }
                    }
                }
                H3MP_GameManager.waitingWearables.Remove(trackedID);
            }

            AnvilManager.Run(EquipWearables());

            // Deregister the AI from the manager if we are not in control
            // Also set CoreRB as kinematic
            if (H3MP_ThreadManager.host)
            {
                if(controller != 0)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosig.E);
                    physicalObject.physicalSosig.CoreRB.isKinematic = true;
                }
            }
            else if(controller != H3MP_Client.singleton.ID)
            {
                GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosig.E);
                physicalObject.physicalSosig.CoreRB.isKinematic = true;
            }

            // Initially set IFF
            ++SosigIFFPatch.skip;
            physicalObject.physicalSosig.SetIFF(IFF);
            --SosigIFFPatch.skip;

            // Initially set itself
            Update(this);
        }

        private IEnumerator EquipWearables()
        {
            if (wearables != null)
            {
                for (int i = 0; i < wearables.Count; ++i)
                {
                    for (int j = 0; j < wearables[i].Count; ++j)
                    {
                        if (IM.OD.ContainsKey(wearables[i][j]))
                        {
                            yield return IM.OD[wearables[i][j]].GetGameObjectAsync();
                            ++Mod.skipAllInstantiates;
                            GameObject outfitItemObject = GameObject.Instantiate(IM.OD[wearables[i][j]].GetGameObject(), physicalObject.physicalSosig.Links[i].transform.position, physicalObject.physicalSosig.Links[i].transform.rotation, physicalObject.physicalSosig.Links[i].transform);
                            --Mod.skipAllInstantiates;
                            SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                            ++SosigLinkActionPatch.skipRegisterWearable;
                            wearableScript.RegisterWearable(physicalObject.physicalSosig.Links[i]);
                            --SosigLinkActionPatch.skipRegisterWearable;
                        }
                        else
                        {
                            Debug.LogWarning("TrackedSosigData.EquipWearables: Wearable "+ wearables[i][j]+" not found in OD");
                        }
                    }
                }
            }
            yield break;
        }

        public IEnumerator EquipWearable(int linkIndex, string ID, bool skip = false)
        {
            if (IM.OD.ContainsKey(ID))
            {
                yield return IM.OD[ID].GetGameObjectAsync();
                ++Mod.skipAllInstantiates;
                GameObject outfitItemObject = GameObject.Instantiate(IM.OD[ID].GetGameObject(), physicalObject.physicalSosig.Links[linkIndex].transform.position, physicalObject.physicalSosig.Links[linkIndex].transform.rotation, physicalObject.physicalSosig.Links[linkIndex].transform);
                --Mod.skipAllInstantiates;
                SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                if (skip)
                {
                    ++SosigLinkActionPatch.skipRegisterWearable;
                }
                wearableScript.RegisterWearable(physicalObject.physicalSosig.Links[linkIndex]);
                if (skip)
                {
                    --SosigLinkActionPatch.skipRegisterWearable;
                }
            }
            else
            {
                Debug.LogWarning("TrackedSosigData.EquipWearables: Wearable " + ID + " not found in OD");
            }
            yield break;
        }

        public void Update(H3MP_TrackedSosigData updatedItem)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            previousAmmoStores = ammoStores;
            ammoStores = updatedItem.ammoStores;
            previousActive = active;
            active = updatedItem.active;

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalSosig.CoreRB.position = position;
                physicalObject.physicalSosig.CoreRB.rotation = rotation;
                sosigInvAmmoStores.SetValue(physicalObject.physicalSosig.Inventory, ammoStores);
                
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

        public bool Update()
        {
            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalSosig.CoreRB.position;
            rotation = physicalObject.physicalSosig.CoreRB.rotation;
            previousAmmoStores = ammoStores;
            ammoStores = (int[])sosigInvAmmoStores.GetValue(physicalObject.physicalSosig.Inventory);

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            return NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            bool ammoStoresModified = (previousAmmoStores == null && ammoStores != null) || (previousAmmoStores != null && ammoStores == null);
            if (ammoStoresModified)
            {
                return true;
            }
            else if (ammoStores != null) 
            { 
                for (int i = 0; i < ammoStores.Length; ++i)
                {
                    if (ammoStores[i] != previousAmmoStores[i])
                    {
                        return true;
                    }
                }
            }
            return previousActive != active || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }
    }
}
