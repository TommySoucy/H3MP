using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSentinelData : TrackedObjectData
    {
        public TrackedSentinel physicalSentinel;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;

        public int currentPointIndex;
        public int targetPointIndex;
        public bool isMovingUpIndicies;
        public List<Vector3> patrolPoints;

        public TrackedSentinelData()
        {

        }

        public TrackedSentinelData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();

            // Full
            patrolPoints = new List<Vector3>();
            int pointCount = packet.ReadByte();
            for(int i=0; i < pointCount; ++i)
            {
                patrolPoints.Add(packet.ReadVector3());
            }
            currentPointIndex = packet.ReadByte();
            targetPointIndex = packet.ReadByte();
            isMovingUpIndicies = packet.ReadBool();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Sentinel>() != null;
        }

        private static TrackedSentinel MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedSentinel trackedSentinel = root.gameObject.AddComponent<TrackedSentinel>();
            TrackedSentinelData data = new TrackedSentinelData();
            trackedSentinel.sentinelData = data;
            trackedSentinel.data = data;
            data.physicalSentinel = trackedSentinel;
            data.physical = trackedSentinel;
            Construct_Sentinel sentinelScript = root.GetComponent<Construct_Sentinel>();
            data.physicalSentinel.physicalSentinel = sentinelScript;
            data.physical.physical = sentinelScript;

            data.typeIdentifier = "TrackedSentinelData";
            data.active = trackedSentinel.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedSentinelBySentinel.Add(trackedSentinel.physicalSentinel, trackedSentinel);
            GameManager.trackedObjectByObject.Add(trackedSentinel.physicalSentinel, trackedSentinel);
            GameManager.trackedObjectByShatterable.Add(trackedSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>(), trackedSentinel);
            for (int i = 0; i < trackedSentinel.physicalSentinel.Plates.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Add(trackedSentinel.physicalSentinel.Plates[i], trackedSentinel);
            }

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedSentinel.awoken)
            {
                trackedSentinel.data.Update(true);
            }

            Mod.LogInfo("Made sentinel " + trackedSentinel.name + " tracked", false);

            return trackedSentinel;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);

            if (full)
            {
                if(patrolPoints == null || patrolPoints.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)patrolPoints.Count);
                    for(int i=0; i < patrolPoints.Count; ++i)
                    {
                        packet.Write(patrolPoints[i]);
                    }
                }
                packet.Write((byte)currentPointIndex);
                packet.Write((byte)targetPointIndex);
                packet.Write(isMovingUpIndicies);
            }
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating sentinel at " + trackedID, false);
            GameObject prefab = null;
            Construct_Sentinel_Path sentinelVolume = GameObject.FindObjectOfType<Construct_Sentinel_Path>();
            if (sentinelVolume == null)
            {
                Mod.LogError("Failed to instantiate sentinel: " + trackedID + ": Could not find suitable sentinel path to get prefab from");
                yield break;
            }
            else
            {
                prefab = sentinelVolume.SentinelPrefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject sentinelInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalSentinel = sentinelInstance.AddComponent<TrackedSentinel>();
            physical = physicalSentinel;
            physicalSentinel.physicalSentinel = sentinelInstance.GetComponent<Construct_Sentinel>();
            physical.physical = physicalSentinel.physicalSentinel;
            awaitingInstantiation = false;
            physicalSentinel.sentinelData = this;
            physicalSentinel.data = this;

            GameManager.trackedSentinelBySentinel.Add(physicalSentinel.physicalSentinel, physicalSentinel);
            GameManager.trackedObjectByObject.Add(physicalSentinel.physicalSentinel, physicalSentinel);
            GameManager.trackedObjectByShatterable.Add(physicalSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>(), physicalSentinel);
            for (int i = 0; i < physicalSentinel.physicalSentinel.Plates.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Add(physicalSentinel.physicalSentinel.Plates[i], physicalSentinel);
            }

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedSentinelData updatedSentinel = updatedObject as TrackedSentinelData;

            previousPos = position;
            position = updatedSentinel.position;
            previousRot = rotation;
            rotation = updatedSentinel.rotation;

            if (full)
            {
                patrolPoints = updatedSentinel.patrolPoints;
                currentPointIndex = updatedSentinel.currentPointIndex;
                targetPointIndex = updatedSentinel.targetPointIndex;
                isMovingUpIndicies = updatedSentinel.isMovingUpIndicies;
            }

            // Set physically
            if (physicalSentinel != null)
            {
                physicalSentinel.physicalSentinel.transform.position = position;
                physicalSentinel.physicalSentinel.transform.rotation = rotation;

                if (full)
                {
                    if(physicalSentinel.physicalSentinel.PatrolPoints == null)
                    {
                        physicalSentinel.physicalSentinel.PatrolPoints = new List<Transform>();
                    }
                    else
                    {
                        for (int i = 0; i < physicalSentinel.physicalSentinel.PatrolPoints.Count; ++i)
                        {
                            GameObject.Destroy(physicalSentinel.physicalSentinel.PatrolPoints[i].gameObject);
                        }
                        physicalSentinel.physicalSentinel.PatrolPoints.Clear();
                    }
                    for (int i=0; i < patrolPoints.Count; ++i)
                    {
                        GameObject newPatrolPoint = new GameObject("Sentinel "+trackedID+" patrol point "+i);
                        newPatrolPoint.transform.position = patrolPoints[i];
                        physicalSentinel.physicalSentinel.PatrolPoints.Add(newPatrolPoint.transform);
                    }
                    physicalSentinel.physicalSentinel.curPointIndex = currentPointIndex;
                    physicalSentinel.physicalSentinel.tarPointIndex = targetPointIndex;
                    physicalSentinel.physicalSentinel.isMovingUpIndicies = isMovingUpIndicies;
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            position = packet.ReadVector3();
            previousRot = rotation;
            rotation = packet.ReadQuaternion();

            if (full)
            {
                if(patrolPoints == null)
                {
                    patrolPoints = new List<Vector3>();
                }
                else
                {
                    patrolPoints.Clear();
                }
                int patrolPointCount = packet.ReadByte();
                for (int i = 0; i < patrolPointCount; ++i)
                {
                    patrolPoints.Add(packet.ReadVector3());
                }
                currentPointIndex = packet.ReadByte();
                targetPointIndex = packet.ReadByte();
                isMovingUpIndicies = packet.ReadBool();
            }

            // Set physically
            if (physicalSentinel != null)
            {
                physicalSentinel.physicalSentinel.transform.position = position;
                physicalSentinel.physicalSentinel.transform.rotation = rotation;

                if (full)
                {
                    if (physicalSentinel.physicalSentinel.PatrolPoints == null)
                    {
                        physicalSentinel.physicalSentinel.PatrolPoints = new List<Transform>();
                    }
                    else
                    {
                        for (int i = 0; i < physicalSentinel.physicalSentinel.PatrolPoints.Count; ++i)
                        {
                            GameObject.Destroy(physicalSentinel.physicalSentinel.PatrolPoints[i].gameObject);
                        }
                        physicalSentinel.physicalSentinel.PatrolPoints.Clear();
                    }
                    for (int i = 0; i < patrolPoints.Count; ++i)
                    {
                        GameObject newPatrolPoint = new GameObject("Sentinel " + trackedID + " patrol point " + i);
                        newPatrolPoint.transform.position = patrolPoints[i];
                        physicalSentinel.physicalSentinel.PatrolPoints.Add(newPatrolPoint.transform);
                    }
                    physicalSentinel.physicalSentinel.curPointIndex = currentPointIndex;
                    physicalSentinel.physicalSentinel.tarPointIndex = targetPointIndex;
                    physicalSentinel.physicalSentinel.isMovingUpIndicies = isMovingUpIndicies;
                }
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalSentinel == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalSentinel.physicalSentinel.transform.position;
            rotation = physicalSentinel.physicalSentinel.transform.rotation;

            if (full)
            {
                if (patrolPoints == null)
                {
                    patrolPoints = new List<Vector3>();
                }
                else
                {
                    patrolPoints.Clear();
                }
                if(physicalSentinel.physicalSentinel.PatrolPoints != null)
                {
                    for (int i = 0; i < physicalSentinel.physicalSentinel.PatrolPoints.Count; ++i)
                    {
                        patrolPoints.Add(physicalSentinel.physicalSentinel.PatrolPoints[i].position);
                    }
                }
                currentPointIndex = physicalSentinel.physicalSentinel.curPointIndex;
                targetPointIndex = physicalSentinel.physicalSentinel.tarPointIndex;
                isMovingUpIndicies = physicalSentinel.physicalSentinel.isMovingUpIndicies;
            }

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (localTrackedID != -1 && TrackedSentinel.unknownInit.Contains(localWaitingIndex))
            {
                ClientSend.SentinelInit(trackedID, patrolPoints, currentPointIndex, targetPointIndex, isMovingUpIndicies);

                TrackedSentinel.unknownInit.Remove(localWaitingIndex);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalSentinel != null && physicalSentinel.physicalSentinel != null)
                {
                    GameManager.trackedSentinelBySentinel.Remove(physicalSentinel.physicalSentinel);
                    GameManager.trackedObjectByShatterable.Remove(physicalSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>());
                    for (int i = 0; i < physicalSentinel.physicalSentinel.Plates.Count; ++i)
                    {
                        GameManager.trackedObjectByShatterable.Remove(physicalSentinel.physicalSentinel.Plates[i]);
                    }
                }
            }
        }
    }
}
