using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedEncryptionData : TrackedObjectData
    {
        public TrackedEncryption physicalEncryption;

        public TNH_EncryptionType type;
        /*
         * Static: Pos, Rot
         * Hardened: Pos, Rot
         * Swarm: Pos, Rot, Subtargs
         * Recursive: Pos, Rot, Subtargs
         * Stealth: Pos, Rot
         * Agile: Pos, Rot, Pointer scale
         * Regenerative: Pos, Rot, Subtargs
         * Polymorphic: Pos, Rot, Subtargs, Pointer scale
         * Cascading: Pos, Rot
         * Orthagonal: Pos, Rot, Subtargs, isOrthagonalBeamFiring
         * */
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;

        public bool[] subTargsActive;
        public float agilePointerScale;
        public bool isOrthagonalBeamFiring;

        public TrackedEncryptionData()
        {

        }

        public TrackedEncryptionData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            agilePointerScale = packet.ReadFloat();

            type = (TNH_EncryptionType)packet.ReadByte();
            int length = packet.ReadInt();
            if (length > 0)
            {
                subTargsActive = new bool[length];
                for (int i = 0; i < length; ++i)
                {
                    subTargsActive[i] = packet.ReadBool();
                }
            }
            isOrthagonalBeamFiring = packet.ReadBool();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<TNH_EncryptionTarget>() != null;
        }

        public static TrackedEncryption MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedEncryption trackedEncryption = root.gameObject.AddComponent<TrackedEncryption>();
            TrackedEncryptionData data = new TrackedEncryptionData();
            trackedEncryption.data = data;
            trackedEncryption.encryptionData = data;
            data.physicalEncryption = trackedEncryption;
            data.physical = trackedEncryption;
            data.physicalEncryption.physicalEncryption = root.GetComponent<TNH_EncryptionTarget>();
            data.physical.physical = data.physicalEncryption.physicalEncryption;

            data.typeIdentifier = "TrackedEncryptionData";
            data.active = trackedEncryption.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || GameManager.inPostSceneLoadTrack;

            GameManager.trackedEncryptionByEncryption.Add(data.physicalEncryption.physicalEncryption, trackedEncryption);
            GameManager.trackedObjectByObject.Add(data.physicalEncryption.physicalEncryption, trackedEncryption);
            GameManager.trackedObjectByDamageable.Add(data.physicalEncryption.physicalEncryption, trackedEncryption);

            data.type = data.physicalEncryption.physicalEncryption.Type;
            data.position = trackedEncryption.transform.position;
            data.rotation = trackedEncryption.transform.rotation;

            data.subTargsActive = new bool[data.physicalEncryption.physicalEncryption.SubTargs.Count];
            for (int i = 0; i < data.physicalEncryption.physicalEncryption.SubTargs.Count; ++i)
            {
                data.subTargsActive[i] = data.physicalEncryption.physicalEncryption.SubTargs[i].activeSelf;
            }
            data.agilePointerScale = data.physicalEncryption.physicalEncryption.AgilePointerScale;
            data.isOrthagonalBeamFiring = data.physicalEncryption.physicalEncryption.isOrthagonalBeamFiring;

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedEncryption.awoken)
            {
                trackedEncryption.data.Update(true);
            }

            return trackedEncryption;
        }

        public override IEnumerator Instantiate()
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
            physicalEncryption = encryptionInstance.AddComponent<TrackedEncryption>();
            physical = physicalEncryption;
            TNH_EncryptionTarget physicalEncryptionScript = encryptionInstance.GetComponent<TNH_EncryptionTarget>();
            physicalEncryption.physicalEncryption = physicalEncryptionScript;
            physical.physical = physicalEncryptionScript;
            awaitingInstantiation = false;
            physicalEncryption.encryptionData = this;
            physical.data = this;

            GameManager.trackedEncryptionByEncryption.Add(physicalEncryption.physicalEncryption, physicalEncryption);
            GameManager.trackedObjectByObject.Add(physicalEncryption.physicalEncryption, physicalEncryption);
            GameManager.trackedObjectByDamageable.Add(physicalEncryption.physicalEncryption, physicalEncryption);

            // Register to hold
            if (GM.TNH_Manager != null)
            {
                physicalEncryption.physicalEncryption.SetHoldPoint(GM.TNH_Manager.m_curHoldPoint);
                GM.TNH_Manager.m_curHoldPoint.RegisterNewTarget(physicalEncryption.physicalEncryption);
            }

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedEncryptionData updatedEncryption = updatedObject as TrackedEncryptionData;

            if (full)
            {
                type = updatedEncryption.type;
                subTargsActive = updatedEncryption.subTargsActive;
            }

            previousPos = position;
            previousRot = rotation;
            position = updatedEncryption.position;
            rotation = updatedEncryption.rotation;

            if (physicalEncryption != null)
            {
                if (physicalEncryption.physicalEncryption.RB != null)
                {
                    physicalEncryption.physicalEncryption.RB.position = position;
                    physicalEncryption.physicalEncryption.RB.rotation = rotation;
                }
                else
                {
                    physicalEncryption.physicalEncryption.transform.position = position;
                    physicalEncryption.physicalEncryption.transform.rotation = rotation;
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();

            if (full)
            {
                type = (TNH_EncryptionType)packet.ReadByte();
                int length = packet.ReadInt();
                if (length > 0)
                {
                    subTargsActive = new bool[length];
                    for (int i = 0; i < length; ++i)
                    {
                        subTargsActive[i] = packet.ReadBool();
                    }
                }
            }

            if (physicalEncryption != null)
            {
                if (physicalEncryption.physicalEncryption.RB != null)
                {
                    physicalEncryption.physicalEncryption.RB.position = position;
                    physicalEncryption.physicalEncryption.RB.rotation = rotation;
                }
                else
                {
                    physicalEncryption.physicalEncryption.transform.position = position;
                    physicalEncryption.physicalEncryption.transform.rotation = rotation;
                }
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            if (physicalEncryption.physicalEncryption.RB != null)
            {
                position = physicalEncryption.physicalEncryption.RB.position;
                rotation = physicalEncryption.physicalEncryption.RB.rotation;
            }
            else
            {
                position = physicalEncryption.physicalEncryption.transform.position;
                rotation = physicalEncryption.physicalEncryption.transform.rotation;
            }

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);

            if (full)
            {
                packet.Write((byte)type);
                if (subTargsActive == null || subTargsActive.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(subTargsActive.Length);
                    for (int i = 0; i < subTargsActive.Length; ++i)
                    {
                        packet.Write(subTargsActive[i]);
                    }
                }
            }
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

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

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

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                TrackedEncryption.unknownInit.Remove(localWaitingIndex);
                TrackedEncryption.unknownSpawnGrowth.Remove(localWaitingIndex);
                TrackedEncryption.unknownResetGrowth.Remove(localWaitingIndex);
                TrackedEncryption.unknownSpawnSubTarg.Remove(localWaitingIndex);
                TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);

                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalEncryption != null && physicalEncryption.physicalEncryption != null)
                {
                    GameManager.trackedEncryptionByEncryption.Remove(physicalEncryption.physicalEncryption);
                    GameManager.trackedObjectByDamageable.Remove(physicalEncryption.physicalEncryption);
                }
            }
        }
    }
}
