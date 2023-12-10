using FistVR;
using H3MP.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedEncryption : TrackedObject
    {
        public TNH_EncryptionTarget physicalEncryption;
        public TrackedEncryptionData encryptionData;

        // Unknown tracked ID queues
        public static Dictionary<uint, KeyValuePair<List<int>, List<Vector3>>> unknownInit = new Dictionary<uint, KeyValuePair<List<int>, List<Vector3>>>();
        public static Dictionary<uint, List<int>> unknownSpawnSubTarg = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, List<int>> unknownSpawnSubTargGeo = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, List<int>> unknownDisableSubTarg = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, List<KeyValuePair<int, Vector3>>> unknownSpawnGrowth = new Dictionary<uint, List<KeyValuePair<int, Vector3>>>();
        public static Dictionary<uint, List<KeyValuePair<int, Vector3>>> unknownResetGrowth = new Dictionary<uint, List<KeyValuePair<int, Vector3>>>();
        public static Dictionary<uint, int> unknownUpdateDisplay = new Dictionary<uint, int>();

        // TrackedEncryptionReferences array
        // Used by Encryptions who need to get access to their TrackedEncryption very often (On Update for example)
        // This is used to bypass having to find the item in a slow datastructure too often
        // TODO: Improvement: Remove this, use generic tracked object reference system instead
        public static TrackedEncryption[] trackedEncryptionReferences = new TrackedEncryption[100];
        public static List<int> availableTrackedEncryptionRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                        10,11,12,13,14,15,16,17,18,19,
                                                                                        20,21,22,23,24,25,26,27,28,29,
                                                                                        30,31,32,33,34,35,36,37,38,39,
                                                                                        40,41,42,43,44,45,46,47,48,49,
                                                                                        50,51,52,53,54,55,56,57,58,59,
                                                                                        60,61,62,63,64,65,66,67,68,69,
                                                                                        70,71,72,73,74,75,76,77,78,79,
                                                                                        80,81,82,83,84,85,86,87,88,89,
                                                                                        90,91,92,93,94,95,96,97,98,99};

        public override void Awake()
        {
            base.Awake();

            GameManager.OnInstanceJoined += OnInstanceJoined;

            TNH_EncryptionTarget targetScript = GetComponent<TNH_EncryptionTarget>();
            if (targetScript.SpawnPoints == null)
            {
                targetScript.SpawnPoints = new List<Transform>();
            }
            GameObject trackedEncryptionRef = new GameObject();
            trackedEncryptionRef.SetActive(false);
            if (availableTrackedEncryptionRefIndices.Count == 0)
            {
                TrackedEncryption[] tempEncryptions = trackedEncryptionReferences;
                trackedEncryptionReferences = new TrackedEncryption[tempEncryptions.Length + 100];
                for (int i = 0; i < tempEncryptions.Length; ++i)
                {
                    trackedEncryptionReferences[i] = tempEncryptions[i];
                }
                for (int i = tempEncryptions.Length; i < trackedEncryptionReferences.Length; ++i)
                {
                    availableTrackedEncryptionRefIndices.Add(i);
                }
            }
            int refIndex = availableTrackedEncryptionRefIndices[availableTrackedEncryptionRefIndices.Count - 1];
            availableTrackedEncryptionRefIndices.RemoveAt(availableTrackedEncryptionRefIndices.Count - 1);
            trackedEncryptionReferences[refIndex] = this;
            trackedEncryptionRef.name = refIndex.ToString();
            targetScript.SpawnPoints.Add(trackedEncryptionRef.transform);
        }

        protected override void OnDestroy()
        {
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedEncryptionByEncryption.Remove(physicalEncryption);
            GameManager.trackedObjectByDamageable.Remove(physicalEncryption);
            for (int i = 0; i < physicalEncryption.SubTargs.Count; ++i)
            {
                IFVRDamageable damageable = physicalEncryption.SubTargs[i].GetComponent<IFVRDamageable>();
                if (damageable != null)
                {
                    GameManager.trackedObjectByDamageable.Remove(damageable);
                }
            }

            // Type specific destruction
            // In the case of encryptions, upon destruction, TNH_EncryptionTarget.Destroy gets called, doing some final things
            // Destroy will not be called on non controller because action that call it only happen on controller side
            // There are some things in Destroy that we still want to make surewe do on our side, this is what we do here
            if (data.controller != GameManager.ID)
            {
                if (physicalEncryption.UsesRegenerativeSubTarg)
                {
                    for (int i = 0; i < physicalEncryption.Tendrils.Count; i++)
                    {
                        Destroy(physicalEncryption.Tendrils[i]);
                        Destroy(physicalEncryption.SubTargs[i]);
                    }
                }
                if (physicalEncryption.m_returnToSpawnLine != null)
                {
                    Destroy(physicalEncryption.m_returnToSpawnLine.gameObject);
                }
                if (physicalEncryption.FlashOnDestroy)
                {
                    FXM.InitiateMuzzleFlash(transform.position, Vector3.up, physicalEncryption.FlashIntensity, physicalEncryption.FlashColor, physicalEncryption.FlashRange);
                }
                if(physicalEncryption.m_point != null)
                {
                    physicalEncryption.m_point.m_activeTargets.Remove(physicalEncryption);
                }
            }

            base.OnDestroy();
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            if (!GameManager.sceneLoading)
            {
                TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
                data.ShouldBring(false, ref bring);

                ++GameManager.giveControlOfDestroyed;

                // Note: Encryptions cannot be interacted with, so no need to check that case with IsControlled
                if (bring == TrackedObjectData.ObjectBringType.Yes)
                {
                    DestroyImmediate(this);

                    GameManager.SyncTrackedObjects(transform, true, null);
                }
                else // Don't want to bring, destroy
                {
                    DestroyImmediate(gameObject);
                }

                --GameManager.giveControlOfDestroyed;
            }
        }
    }
}
