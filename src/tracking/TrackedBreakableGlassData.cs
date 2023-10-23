using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBreakableGlassData : TrackedObjectData
    {
        public TrackedBreakableGlass physicalBreakableGlass;

        public BreakableGlassDamager damager;

        public List<Vector2> shape;
        public List<Vector2> baseShape;
        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;
        public float deletionTimer;
        public float deletionTimerStart;
        public bool tickingDownToDeletion;
        public float thickness;
        public bool isAttached;
        public int breakDepth;

        public bool hasWrapper;
        public int wrapperID = -1;
        public int localWrapperID = -1;
        public static int wrapperIDCounter = 0;
        public static Dictionary<int, DestructibleWindowWrapper> localWrappersByID = new Dictionary<int, DestructibleWindowWrapper>();
        public static Dictionary<DestructibleWindowWrapper, int> localWrapperIDsbyWrapper = new Dictionary<DestructibleWindowWrapper, int>();
        public static Dictionary<int, DestructibleWindowWrapper> wrappersByID = new Dictionary<int, DestructibleWindowWrapper>();
        public static Dictionary<DestructibleWindowWrapper, int> wrapperIDsbyWrapper = new Dictionary<DestructibleWindowWrapper, int>();

        public TrackedBreakableGlassData()
        {

        }

        public TrackedBreakableGlassData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();

            int length = packet.ReadByte();
            if (length > 0)
            {
                shape = new List<Vector2>();
                for (int i = 0; i < length; ++i)
                {
                    shape.Add(packet.ReadVector2());
                }
            }
            length = packet.ReadByte();
            if (length > 0)
            {
                baseShape = new List<Vector2>();
                for (int i = 0; i < length; ++i)
                {
                    baseShape.Add(packet.ReadVector2());
                }
            }
            deletionTimer = packet.ReadFloat();
            deletionTimerStart = packet.ReadFloat();
            tickingDownToDeletion = packet.ReadBool();
            thickness = packet.ReadFloat();
            isAttached = packet.ReadBool();
            breakDepth = packet.ReadInt();
            wrapperID = packet.ReadInt();
            localWrapperID = packet.ReadInt();
            hasWrapper = packet.ReadBool();
            if (ThreadManager.host)
            {
                if(wrapperID == -1 && hasWrapper)
                {
                    wrapperID = wrapperIDCounter++;
                }
            }
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<BreakableGlass>() != null && t.GetComponent<BreakableGlassDamager>() != null && t.GetComponent<MeshFilter>() != null;
        }

        public static TrackedBreakableGlass MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedBreakableGlass trackedBreakableGlass = root.gameObject.AddComponent<TrackedBreakableGlass>();
            TrackedBreakableGlassData data = new TrackedBreakableGlassData();
            trackedBreakableGlass.data = data;
            trackedBreakableGlass.breakableGlassData = data;
            data.physicalBreakableGlass = trackedBreakableGlass;
            data.physical = trackedBreakableGlass;
            data.physicalBreakableGlass.physicalBreakableGlass = root.GetComponent<BreakableGlass>();
            data.physical.physical = data.physicalBreakableGlass.physicalBreakableGlass;

            data.typeIdentifier = "TrackedBreakableGlassData";
            data.active = trackedBreakableGlass.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();
            data.damager = root.GetComponent<BreakableGlassDamager>();
            if (data.damager.m_wrapper != null)
            {
                data.hasWrapper = true;
                if (wrapperIDsbyWrapper.TryGetValue(data.damager.m_wrapper, out int currentID))
                {
                    data.wrapperID = currentID;
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        if (wrapperIDsbyWrapper.TryGetValue(data.damager.m_wrapper, out int currentWrapperID))
                        {
                            data.wrapperID = currentWrapperID;
                        }
                        else
                        {
                            data.wrapperID = wrapperIDCounter;

                            wrappersByID.Add(data.wrapperID, data.damager.m_wrapper);
                            wrapperIDsbyWrapper.Add(data.damager.m_wrapper, data.wrapperID);
                        }
                    }
                    else
                    {
                        if (localWrapperIDsbyWrapper.TryGetValue(data.damager.m_wrapper, out int currentLocalID))
                        {
                            data.localWrapperID = currentLocalID;
                        }
                        else
                        {
                            data.localWrapperID = wrapperIDCounter;

                            localWrappersByID.Add(data.localWrapperID, data.damager.m_wrapper);
                            localWrapperIDsbyWrapper.Add(data.damager.m_wrapper, data.localWrapperID);
                        }
                    }
                }
                ++wrapperIDCounter;
            }
            else
            {
                data.hasWrapper = false;
            }

            GameManager.trackedBreakableGlassByBreakableGlass.Add(data.physicalBreakableGlass.physicalBreakableGlass, trackedBreakableGlass);
            GameManager.trackedBreakableGlassByBreakableGlassDamager.Add(data.damager, trackedBreakableGlass);
            GameManager.trackedObjectByObject.Add(data.physicalBreakableGlass.physicalBreakableGlass, trackedBreakableGlass);
            GameManager.trackedObjectByDamageable.Add(data.damager, trackedBreakableGlass);

            List<Vector3> tempVerts = new List<Vector3>();
            root.GetComponent<MeshFilter>().mesh.GetVertices(tempVerts);
            data.shape = CynGlass.MeshToShape(tempVerts, data.physicalBreakableGlass.physicalBreakableGlass.thickness);
            data.baseShape = data.physicalBreakableGlass.physicalBreakableGlass.baseShape;
            data.position = trackedBreakableGlass.transform.position;
            data.rotation = trackedBreakableGlass.transform.rotation;
            data.deletionTimer = data.physicalBreakableGlass.physicalBreakableGlass.m_deletionTimer;
            data.deletionTimerStart = data.physicalBreakableGlass.physicalBreakableGlass.m_deletionTimerStart;
            data.tickingDownToDeletion = data.physicalBreakableGlass.physicalBreakableGlass.m_tickingDownToDeletion;
            data.thickness = data.physicalBreakableGlass.physicalBreakableGlass.thickness;
            data.isAttached = data.physicalBreakableGlass.physicalBreakableGlass.isAttached;
            data.breakDepth = data.physicalBreakableGlass.physicalBreakableGlass.breakDepth;

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            return trackedBreakableGlass;
        }

        public override IEnumerator Instantiate()
        {
            ++Mod.skipAllInstantiates;
            GameObject objectInstance = GameObject.Instantiate(Mod.glassPrefab);
            objectInstance.name = "TrackedBreakableGlassInstance" + trackedID;
            --Mod.skipAllInstantiates;
            physicalBreakableGlass = objectInstance.AddComponent<TrackedBreakableGlass>();
            physical = physicalBreakableGlass;
            physicalBreakableGlass.physicalBreakableGlass = objectInstance.GetComponent<BreakableGlass>();
            physical.physical = physicalBreakableGlass.physicalBreakableGlass;
            physicalBreakableGlass.breakableGlassData = this;
            physical.data = this;
            awaitingInstantiation = false;

            MeshFilter mf = objectInstance.GetComponent<MeshFilter>();
            Rigidbody rb = objectInstance.GetComponent<Rigidbody>();
            damager = objectInstance.GetComponent<BreakableGlassDamager>();
            MeshCollider collider = objectInstance.GetComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            CynGlass.ShapeToMesh(shape, thickness, shape[0], mesh);
            mf.sharedMesh = mesh;
            collider.enabled = true;
            collider.convex = true;
            collider.sharedMesh = mesh;
            rb.isKinematic = true;
            physicalBreakableGlass.physicalBreakableGlass.shardPrefab = Mod.glassPrefab;
            physicalBreakableGlass.physicalBreakableGlass.area = CynGlass.AreaOf(shape);
            rb.mass = Mathf.Lerp(0.05f, 0.2f, Mathf.InverseLerp(0.025f, 0.1f, physicalBreakableGlass.physicalBreakableGlass.area));
            if (hasWrapper)
            {
                if (wrappersByID.TryGetValue(wrapperID, out DestructibleWindowWrapper currentWrapper))
                {
                    damager.SetWrapper(currentWrapper);
                    objectInstance.transform.parent = currentWrapper.transform;
                }
                else
                {
                    GameObject wrapperObject = new GameObject("WindowWrapper");
                    wrapperObject.SetActive(false);
                    DestructibleWindowWrapper newWrapper = wrapperObject.AddComponent<DestructibleWindowWrapper>();

                    wrappersByID.Add(wrapperID, newWrapper);
                    wrapperIDsbyWrapper.Add(newWrapper, wrapperID);

                    objectInstance.transform.parent = wrapperObject.transform;

                    newWrapper.GlassDamager = damager;
                    wrapperObject.SetActive(true);
                }
            }

            physicalBreakableGlass.physicalBreakableGlass.shape = shape;
            physicalBreakableGlass.physicalBreakableGlass.thickness = thickness;
            physicalBreakableGlass.physicalBreakableGlass.isAttached = isAttached;
            physicalBreakableGlass.physicalBreakableGlass.breakDepth = breakDepth;
            if(breakDepth == 0)
            {
                physicalBreakableGlass.physicalBreakableGlass.baseShape = shape;
            }
            else
            {
                physicalBreakableGlass.physicalBreakableGlass.baseShape = baseShape;
            }
            physicalBreakableGlass.physicalBreakableGlass.m_deletionTimer = deletionTimer;
            physicalBreakableGlass.physicalBreakableGlass.m_deletionTimerStart = deletionTimerStart;
            physicalBreakableGlass.physicalBreakableGlass.m_tickingDownToDeletion = tickingDownToDeletion;


            GameManager.trackedBreakableGlassByBreakableGlass.Add(physicalBreakableGlass.physicalBreakableGlass, physicalBreakableGlass);
            GameManager.trackedBreakableGlassByBreakableGlassDamager.Add(damager, physicalBreakableGlass);
            GameManager.trackedObjectByObject.Add(physicalBreakableGlass.physicalBreakableGlass, physicalBreakableGlass);
            GameManager.trackedObjectByDamageable.Add(damager, physicalBreakableGlass);

            // Initially set itself
            UpdateFromData(this);

            yield break;
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedBreakableGlassData updatedBreakableGlass = updatedObject as TrackedBreakableGlassData;

            if (full)
            {
                shape = updatedBreakableGlass.shape;
                baseShape = updatedBreakableGlass.baseShape;
                deletionTimer = updatedBreakableGlass.deletionTimer;
                deletionTimerStart = updatedBreakableGlass.deletionTimerStart;
                tickingDownToDeletion = updatedBreakableGlass.tickingDownToDeletion;
                thickness = updatedBreakableGlass.thickness;
                isAttached = updatedBreakableGlass.isAttached;
                breakDepth = updatedBreakableGlass.breakDepth;
                wrapperID = updatedBreakableGlass.wrapperID;
                //localWrapperID = updatedBreakableGlass.localWrapperID;
                hasWrapper = updatedBreakableGlass.hasWrapper;
            }

            previousPos = position;
            previousRot = rotation;
            position = updatedBreakableGlass.position;
            rotation = updatedBreakableGlass.rotation;

            if (physicalBreakableGlass != null)
            {
                physicalBreakableGlass.transform.position = position;
                physicalBreakableGlass.transform.rotation = rotation;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();

            if (full)
            {
                int length = packet.ReadByte();
                if (length > 0)
                {
                    shape = new List<Vector2>();
                    for (int i = 0; i < length; ++i)
                    {
                        shape.Add(packet.ReadVector2());
                    }
                }
                length = packet.ReadByte();
                if (length > 0)
                {
                    baseShape = new List<Vector2>();
                    for (int i = 0; i < length; ++i)
                    {
                        baseShape.Add(packet.ReadVector2());
                    }
                }
                deletionTimer = packet.ReadFloat();
                deletionTimerStart = packet.ReadFloat();
                tickingDownToDeletion = packet.ReadBool();
                thickness = packet.ReadFloat();
                isAttached = packet.ReadBool();
                breakDepth = packet.ReadInt();
                wrapperID = packet.ReadInt();
                /*localWrapperID = packet.ReadInt();*/ packet.readPos += 4;
                hasWrapper = packet.ReadBool();
                if (ThreadManager.host)
                {
                    if (wrapperID == -1 && hasWrapper)
                    {
                        wrapperID = wrapperIDCounter++;
                    }
                }
            }

            if (physicalBreakableGlass != null)
            {
                physicalBreakableGlass.transform.position = position;
                physicalBreakableGlass.transform.rotation = rotation;
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            if (full)
            {
                List<Vector3> tempVerts = new List<Vector3>();
                physicalBreakableGlass.physicalBreakableGlass.GetComponent<MeshFilter>().mesh.GetVertices(tempVerts);
                shape = CynGlass.MeshToShape(tempVerts, physicalBreakableGlass.physicalBreakableGlass.thickness);
                baseShape = physicalBreakableGlass.physicalBreakableGlass.baseShape;
                deletionTimer = physicalBreakableGlass.physicalBreakableGlass.m_deletionTimer;
                deletionTimerStart = physicalBreakableGlass.physicalBreakableGlass.m_deletionTimerStart;
                tickingDownToDeletion = physicalBreakableGlass.physicalBreakableGlass.m_tickingDownToDeletion;
                thickness = physicalBreakableGlass.physicalBreakableGlass.thickness;
                isAttached = physicalBreakableGlass.physicalBreakableGlass.isAttached;
                breakDepth = physicalBreakableGlass.physicalBreakableGlass.breakDepth;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalBreakableGlass.transform.position;
            rotation = physicalBreakableGlass.transform.rotation;

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
                if (shape == null || shape.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)shape.Count);
                    for (int i = 0; i < shape.Count; ++i)
                    {
                        packet.Write(shape[i]);
                    }
                }
                if (baseShape == null || baseShape.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)baseShape.Count);
                    for (int i = 0; i < baseShape.Count; ++i)
                    {
                        packet.Write(baseShape[i]);
                    }
                }
                packet.Write(deletionTimer);
                packet.Write(deletionTimerStart);
                packet.Write(tickingDownToDeletion);
                packet.Write(thickness);
                packet.Write(isAttached);
                packet.Write(breakDepth);
                packet.Write(wrapperID);
                packet.Write(localWrapperID);
                packet.Write(hasWrapper);
            }
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            TrackedBreakableGlassData asBG = newData as TrackedBreakableGlassData;

            if(wrapperID == -1 && localWrapperID > -1 && localWrappersByID.TryGetValue(localWrapperID, out DestructibleWindowWrapper wrapper))
            {
                if(wrapperIDsbyWrapper.TryGetValue(wrapper, out int currentWrapperID))
                {
                    wrapperID = currentWrapperID;
                }
                else
                {
                    wrapperID = asBG.wrapperID;

                    wrappersByID.Add(wrapperID, wrapper);
                    wrapperIDsbyWrapper.Add(wrapper, wrapperID);
                }
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalBreakableGlass != null && physicalBreakableGlass.physicalBreakableGlass != null)
                {
                    GameManager.trackedBreakableGlassByBreakableGlass.Remove(physicalBreakableGlass.physicalBreakableGlass);
                    GameManager.trackedBreakableGlassByBreakableGlassDamager.Remove(damager);
                    GameManager.trackedObjectByDamageable.Remove(damager);
                }
            }
        }

        public static void ClearWrapperDicts(bool loading)
        {
            if (!loading)
            {
                wrappersByID.Clear();
                wrapperIDsbyWrapper.Clear();
                localWrappersByID.Clear();
                localWrapperIDsbyWrapper.Clear();
            }
        }
    }
}
