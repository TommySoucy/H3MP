using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.CameraController;

namespace H3MP.Tracking
{
    public class TrackedNodeData : TrackedObjectData
    {
        public TrackedNode physicalNode;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;
        public bool underActiveControl;
        public bool previousActiveControl;

        public List<Vector3> points;
        public List<Vector3> ups;

        public TrackedNodeData()
        {

        }

        public TrackedNodeData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Full
            points = new List<Vector3>();
            int pointCount = packet.ReadByte();
            for (int i = 0; i < pointCount; ++i)
            {
                points.Add(packet.ReadVector3());
            }
            ups = new List<Vector3>();
            int upsCount = packet.ReadByte();
            for (int i = 0; i < upsCount; ++i)
            {
                ups.Add(packet.ReadVector3());
            }

            // Update
            underActiveControl = packet.ReadBool();
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Node>() != null;
        }

        public override bool IsControlled(out int interactionID)
        {
            interactionID = -1;
            return physicalNode.physicalNode.m_hand != null;
        }

        private static TrackedNode MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedNode trackedNode = root.gameObject.AddComponent<TrackedNode>();
            TrackedNodeData data = new TrackedNodeData();
            trackedNode.nodeData = data;
            trackedNode.data = data;
            data.physicalNode = trackedNode;
            data.physical = trackedNode;
            Construct_Node nodeScript = root.GetComponent<Construct_Node>();
            data.physicalNode.physicalNode = nodeScript;
            data.physical.physical = nodeScript;

            data.typeIdentifier = "TrackedNodeData";
            data.active = trackedNode.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedNodeByNode.Add(trackedNode.physicalNode, trackedNode);
            GameManager.trackedObjectByObject.Add(trackedNode.physicalNode, trackedNode);
            GameManager.trackedObjectByInteractive.Add(trackedNode.physicalNode, trackedNode);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedNode.awoken)
            {
                trackedNode.data.Update(true);
            }

            Mod.LogInfo("Made node " + trackedNode.name + " tracked", false);

            return trackedNode;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            if (full)
            {
                if (points == null || points.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)points.Count);
                    for (int i = 0; i < points.Count; ++i)
                    {
                        packet.Write(points[i]);
                    }
                }
                if (ups == null || ups.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)ups.Count);
                    for (int i = 0; i < ups.Count; ++i)
                    {
                        packet.Write(ups[i]);
                    }
                }
            }

            packet.Write(underActiveControl);
            packet.Write(position);
            packet.Write(rotation);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating node at " + trackedID, false);
            GameObject prefab = null;
            Construct_Node_Volume nodeVolume = GameObject.FindObjectOfType<Construct_Node_Volume>();
            if (nodeVolume == null)
            {
                Mod.LogError("Failed to instantiate node: " + trackedID + ": Could not find suitable node path to get prefab from");
                yield break;
            }
            else
            {
                prefab = nodeVolume.Node_Prefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject nodeInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalNode = nodeInstance.AddComponent<TrackedNode>();
            physical = physicalNode;
            physicalNode.physicalNode = nodeInstance.GetComponent<Construct_Node>();
            physical.physical = physicalNode.physicalNode;
            awaitingInstantiation = false;
            physicalNode.nodeData = this;
            physicalNode.data = this;

            GameManager.trackedNodeByNode.Add(physicalNode.physicalNode, physicalNode);
            GameManager.trackedObjectByObject.Add(physicalNode.physicalNode, physicalNode);
            GameManager.trackedObjectByInteractive.Add(physicalNode.physicalNode, physicalNode);

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedNodeData updatedNode = updatedObject as TrackedNodeData;

            if (full)
            {
                points = updatedNode.points;
                ups = updatedNode.ups;
            }

            previousActiveControl = underActiveControl;
            underActiveControl = updatedNode.underActiveControl;
            previousPos = position;
            position = updatedNode.position;
            previousRot = rotation;
            rotation = updatedNode.rotation;

            // Set physically
            if (physicalNode != null)
            {
                physicalNode.physicalNode.transform.position = position;
                physicalNode.physicalNode.transform.rotation = rotation;

                if (full)
                {
                    physicalNode.physicalNode.initialPos = position;
                    physicalNode.physicalNode.m_center = position;
                    physicalNode.physicalNode.UpdateCageStems();
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            if (full)
            {
                points = new List<Vector3>();
                int pointCount = packet.ReadByte();
                for (int i = 0; i < pointCount; ++i)
                {
                    points.Add(packet.ReadVector3());
                }
                ups = new List<Vector3>();
                int upsCount = packet.ReadByte();
                for (int i = 0; i < upsCount; ++i)
                {
                    ups.Add(packet.ReadVector3());
                }
            }

            previousActiveControl = underActiveControl;
            underActiveControl = packet.ReadBool();
            previousPos = position;
            position = packet.ReadVector3();
            previousRot = rotation;
            rotation = packet.ReadQuaternion();

            // Set physically
            if (physicalNode != null)
            {
                physicalNode.physicalNode.transform.position = position;
                physicalNode.physicalNode.transform.rotation = rotation;

                if (full)
                {
                    physicalNode.physicalNode.initialPos = position;
                    physicalNode.physicalNode.m_center = position;
                    physicalNode.physicalNode.UpdateCageStems();
                }
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalNode == null)
            {
                return false;
            }

            if (full)
            {
                points = physicalNode.physicalNode.Points;
                ups = physicalNode.physicalNode.Ups;
            }


            previousActiveControl = underActiveControl;
            underActiveControl = IsControlled(out int interactionID);
            previousPos = position;
            previousRot = rotation;
            position = physicalNode.physicalNode.transform.position;
            rotation = physicalNode.physicalNode.transform.rotation;

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation)|| previousActiveControl != underActiveControl;
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (localTrackedID != -1 && TrackedNode.unknownInit.Contains(localWaitingIndex))
            {
                ClientSend.NodeInit(trackedID, physicalNode.physicalNode.Points, physicalNode.physicalNode.Ups);

                TrackedNode.unknownInit.Remove(localWaitingIndex);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalNode != null && physicalNode.physicalNode != null)
                {
                    GameManager.trackedNodeByNode.Remove(physicalNode.physicalNode);
                    GameManager.trackedObjectByInteractive.Remove(physicalNode.physicalNode);
                }
            }
        }
    }
}
