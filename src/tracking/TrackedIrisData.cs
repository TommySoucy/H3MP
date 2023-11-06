using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedIrisData : TrackedObjectData
    {
        public TrackedIris physicalIris;

        public Vector3[] previousPositions;
        public Vector3[] positions;
        public Vector3[] previousAngles;
        public Vector3[] angles;
        public Vector3[] previousScales;
        public Vector3[] scales;
        public bool[] shattered;
        public Construct_Iris.IrisState state;

        public TrackedIrisData()
        {

        }

        public TrackedIrisData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Full
            state = (Construct_Iris.IrisState)packet.ReadByte();

            int count = packet.ReadByte();
            shattered = new bool[count];

            // Update
            previousPositions = new Vector3[count];
            previousAngles = new Vector3[count];
            previousScales = new Vector3[count];
            positions = new Vector3[count];
            angles = new Vector3[count];
            scales = new Vector3[count];
            for(int i = 0; i < positions.Length; ++i)
            {
                positions[i] = packet.ReadVector3();
                angles[i] = packet.ReadVector3();
                scales[i] = packet.ReadVector3();
            }
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Iris>() != null;
        }

        private static TrackedIris MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedIris trackedIris = root.gameObject.AddComponent<TrackedIris>();
            TrackedIrisData data = new TrackedIrisData();
            trackedIris.irisData = data;
            trackedIris.data = data;
            data.physicalIris = trackedIris;
            data.physical = trackedIris;
            Construct_Iris irisScript = root.GetComponent<Construct_Iris>();
            data.physicalIris.physicalIris = irisScript;
            data.physical.physical = irisScript;

            data.typeIdentifier = "TrackedIrisData";
            data.active = trackedIris.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedIrisByIris.Add(trackedIris.physicalIris, trackedIris);
            GameManager.trackedObjectByObject.Add(trackedIris.physicalIris, trackedIris);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedIris.awoken)
            {
                trackedIris.data.Update(true);
            }

            Mod.LogInfo("Made iris " + trackedIris.name + " tracked", false);

            return trackedIris;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            if (full)
            {
                packet.Write((byte)state);
            }

            packet.Write((byte)positions.Length);
            for(int i=0; i < positions.Length; ++i)
            {
                if (full)
                {
                    packet.Write(shattered[i]);
                }
                packet.Write(positions[i]);
                packet.Write(angles[i]);
                packet.Write(scales[i]);
            }
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating iris at " + trackedID, false);
            GameObject prefab = null;
            SosigSpawner sosigSpawner = GameObject.FindObjectOfType<SosigSpawner>();
            if (sosigSpawner != null)
            {
                prefab = sosigSpawner.SpawnerGroups[19].Furnitures[2];
            }
            else
            {
                Mod.LogError("Failed to instantiate iris: " + trackedID + ": Could not find suitable sosig spawner to get prefab from");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject irisInstance = GameObject.Instantiate(prefab, positions[0], Quaternion.Euler(angles[0]));
            --Mod.skipAllInstantiates;
            physicalIris = irisInstance.AddComponent<TrackedIris>();
            physical = physicalIris;
            physicalIris.physicalIris = irisInstance.GetComponent<Construct_Iris>();
            physical.physical = physicalIris.physicalIris;
            awaitingInstantiation = false;
            physicalIris.irisData = this;
            physicalIris.data = this;

            GameManager.trackedIrisByIris.Add(physicalIris.physicalIris, physicalIris);
            GameManager.trackedObjectByObject.Add(physicalIris.physicalIris, physicalIris);

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedIrisData updatedIris = updatedObject as TrackedIrisData;

            if (full)
            {
                state = updatedIris.state;
                for (int i = 0; i < shattered.Length; ++i)
                {
                    shattered[i] = updatedIris.shattered[i];
                }
            }

            for (int i = 0; i < positions.Length; ++i) 
            {
                previousPositions[i] = positions[i];
                previousAngles[i] = angles[i];
                previousScales[i] = scales[i];
                positions[i] = updatedIris.positions[i];
                angles[i] = updatedIris.angles[i];
                scales[i] = updatedIris.scales[i];
            }

            // Set physically
            if (physicalIris != null)
            {
                if (full)
                {
                    physicalIris.physicalIris.SetState(state);
                    for (int i = 0; i < shattered.Length; ++i)
                    {
                        if (physicalIris.physicalIris.Rings[i] != null && shattered[i] && !physicalIris.physicalIris.Rings[i].HasShattered())
                        {
                            physicalIris.physicalIris.Rings[i].Shatter(positions[i], Vector3.zero, 0);
                        }
                    }
                }

                for (int i = 0; i < positions.Length; ++i)
                {
                    if (physicalIris.physicalIris.Rings[i] != null && physicalIris.physicalIris.Rings[i].gameObject.activeSelf)
                    {
                        physicalIris.physicalIris.Rings[i].transform.position = positions[i];
                        physicalIris.physicalIris.Rings[i].transform.rotation = Quaternion.Euler(angles[i]);
                        physicalIris.physicalIris.Rings[i].transform.localScale = scales[i];
                    }
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            if (full)
            {
                state = (Construct_Iris.IrisState)packet.ReadByte();
            }

            packet.ReadByte(); // Size will be written, but should always remain the same, so just ignore
            for (int i = 0; i < positions.Length; ++i)
            {
                previousPositions[i] = positions[i];
                previousAngles[i] = angles[i];
                previousScales[i] = scales[i];

                if (full)
                {
                    shattered[i] = packet.ReadBool();
                }
                positions[i] = packet.ReadVector3();
                angles[i] = packet.ReadVector3();
                scales[i] = packet.ReadVector3();
            }

            // Set physically
            if (physicalIris != null)
            {
                if (full)
                {
                    physicalIris.physicalIris.SetState(state);
                    for (int i = 0; i < shattered.Length; ++i)
                    {
                        if (physicalIris.physicalIris.Rings[i] != null && shattered[i] && !physicalIris.physicalIris.Rings[i].HasShattered())
                        {
                            physicalIris.physicalIris.Rings[i].Shatter(positions[i], Vector3.zero, 0);
                        }
                    }
                }

                for(int i=0; i < positions.Length; ++i)
                {
                    if (physicalIris.physicalIris.Rings[i] != null && physicalIris.physicalIris.Rings[i].gameObject.activeSelf)
                    {
                        physicalIris.physicalIris.Rings[i].transform.position = positions[i];
                        physicalIris.physicalIris.Rings[i].transform.rotation = Quaternion.Euler(angles[i]);
                        physicalIris.physicalIris.Rings[i].transform.localScale = scales[i];
                    }
                }
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalIris == null)
            {
                return false;
            }

            if (full)
            {
                state = physicalIris.physicalIris.IState;
                for (int i=0; i < shattered.Length; ++i)
                {
                    shattered[i] = physicalIris.physicalIris.Rings[i] == null || physicalIris.physicalIris.Rings[i].HasShattered();
                }
            }

            for (int i = 0; i < positions.Length; ++i)
            {
                previousPositions[i] = positions[i];
                previousAngles[i] = angles[i];
                previousScales[i] = scales[i];
                if (physicalIris.physicalIris.Rings[i] != null && physicalIris.physicalIris.Rings[i].gameObject.activeSelf)
                {
                    positions[i] = physicalIris.physicalIris.Rings[i].transform.position;
                    angles[i] = physicalIris.physicalIris.Rings[i].transform.rotation.eulerAngles;
                    scales[i] = physicalIris.physicalIris.Rings[i].transform.localScale;
                }
                updated |= (!previousPositions[i].Equals(positions[i]) || !previousAngles[i].Equals(angles[i]) || !previousScales[i].Equals(scales[i]));
            }

            return updated;
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (localTrackedID != -1 && TrackedIris.unknownIrisShatter.TryGetValue(localWaitingIndex, out List<object[]> shatterList))
            {
                for(int i=0; i< shatterList.Count; ++i)
                {
                    ClientSend.IrisShatter(trackedID, (byte)shatterList[i][0], (Vector3)shatterList[i][1], (Vector3)shatterList[i][2], (float)shatterList[i][3]);
                }

                TrackedIris.unknownIrisShatter.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedIris.unknownIrisSetState.TryGetValue(localWaitingIndex, out Construct_Iris.IrisState s))
            {
                ClientSend.IrisSetState(trackedID, s);

                TrackedIris.unknownIrisSetState.Remove(localWaitingIndex);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalIris != null && physicalIris.physicalIris != null)
                {
                    GameManager.trackedIrisByIris.Remove(physicalIris.physicalIris);
                }
            }
        }
    }
}
