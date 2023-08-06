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
        public bool[] subTargGeosActive;
        public Vector3 previousAgilePointerScale;
        public Vector3 agilePointerScale;
        public bool previousIsOrthagonalBeamFiring;
        public bool isOrthagonalBeamFiring;

        public int numHitsLeft;
        public Vector3 initialPos;
        public byte cascadingIndex;
        public byte cascadingDepth;

        public TrackedEncryptionData()
        {

        }

        public TrackedEncryptionData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            agilePointerScale = packet.ReadVector3();
            isOrthagonalBeamFiring = packet.ReadBool();

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
            length = packet.ReadInt();
            if (length > 0)
            {
                subTargGeosActive = new bool[length];
                for (int i = 0; i < length; ++i)
                {
                    subTargGeosActive[i] = packet.ReadBool();
                }
            }
            numHitsLeft = packet.ReadInt();
            initialPos = packet.ReadVector3();
            cascadingIndex = packet.ReadByte();
            cascadingDepth = packet.ReadByte();
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
            for (int i = 0; i < data.physicalEncryption.physicalEncryption.SubTargs.Count; ++i) 
            {
                IFVRDamageable damageable = data.physicalEncryption.physicalEncryption.SubTargs[i].GetComponent<IFVRDamageable>();
                if(damageable != null)
                {
                    GameManager.trackedObjectByDamageable.Add(damageable, trackedEncryption);
                }
            }

            data.subTargsActive = new bool[data.physicalEncryption.physicalEncryption.SubTargs.Count];
            data.subTargGeosActive = new bool[data.physicalEncryption.physicalEncryption.SubTargGeo.Count];

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
            // Handle new versions/types
            if ((int)type == 11)
            {
                SosigSpawner sosigSpawner = GameObject.FindObjectOfType<SosigSpawner>();
                if (sosigSpawner != null)
                {
                    prefab = sosigSpawner.SpawnerGroups[18].Furnitures[1];
                }
            }
            else if ((int)type == 12)
            {
                SosigSpawner sosigSpawner = GameObject.FindObjectOfType<SosigSpawner>();
                if (sosigSpawner != null)
                {
                    prefab = sosigSpawner.SpawnerGroups[18].Furnitures[6];
                }
            }
            else if((int)type > 6)
            {
                SosigSpawner sosigSpawner = GameObject.FindObjectOfType<SosigSpawner>();
                if (sosigSpawner != null)
                {
                    prefab = sosigSpawner.SpawnerGroups[18].Furnitures[(int)type];
                }
            }

            // Try to get through the usual way
            if (prefab == null)
            {
                if (GM.TNH_Manager == null)
                {
                    if(IM.OD.TryGetValue(EncryptionTypeToID(type), out FVRObject obj))
                    {
                        yield return obj.GetGameObjectAsync();
                        prefab = obj.GetGameObject();
                    }
                }
                else
                {
                    if(GM.TNH_Manager.GetEncryptionPrefab(type) != null)
                    {
                        prefab = GM.TNH_Manager.GetEncryptionPrefab(type).GetGameObject();
                    }
                }
            }

            if(prefab == null)
            {
                Mod.LogError($"Attempted to instantiate encryption {type} sent from {controller} but failed to get prefab.");
                awaitingInstantiation = false;
                yield break;
            }
            else // Got prefab
            {
                // Handle getting correct cascading prefab
                if (type == TNH_EncryptionType.Cascading)
                {
                    // Get corect prefab from depth if necessary
                    for (int i = 0; i < cascadingDepth; ++i)
                    {
                        TNH_EncryptionTarget encryptionScript = prefab.GetComponent<TNH_EncryptionTarget>();
                        if (encryptionScript == null)
                        {
                            break;
                        }
                        else
                        {
                            // This makes assumption that up to our depth, different subshard prefabs each have the same subshard prefabs in
                            // their SpawnOnDestruction
                            // Also makes assumption that subshard prefabs are before anything else in SpawnOnDestruction, guaranteeing that SpawnOnDestruction[0]
                            // is actually a subshard prefab and not something else
                            prefab = i == cascadingDepth - 1 ? encryptionScript.SpawnOnDestruction[cascadingIndex] : encryptionScript.SpawnOnDestruction[0];
                        }
                    }
                }
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
            for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargs.Count; ++i)
            {
                IFVRDamageable damageable = physicalEncryption.physicalEncryption.SubTargs[i].GetComponent<IFVRDamageable>();
                if (damageable != null)
                {
                    GameManager.trackedObjectByDamageable.Add(damageable, physicalEncryption);
                }
            }

            // Register to hold
            if (GM.TNH_Manager != null)
            {
                physicalEncryption.physicalEncryption.SetHoldPoint(GM.TNH_Manager.m_curHoldPoint);
                GM.TNH_Manager.m_curHoldPoint.RegisterNewTarget(physicalEncryption.physicalEncryption);
            }

            // Set defaults
            subTargsActive = new bool[physicalEncryption.physicalEncryption.SubTargs.Count];
            subTargGeosActive = new bool[physicalEncryption.physicalEncryption.SubTargGeo.Count];

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedEncryptionData updatedEncryption = updatedObject as TrackedEncryptionData;

            if (full)
            {
                type = updatedEncryption.type;
                subTargsActive = updatedEncryption.subTargsActive;
                subTargGeosActive = updatedEncryption.subTargGeosActive;
                numHitsLeft = updatedEncryption.numHitsLeft;
                initialPos = updatedEncryption.initialPos;
                cascadingIndex = updatedEncryption.cascadingIndex;
                cascadingDepth = updatedEncryption.cascadingDepth;
            }
            Mod.LogInfo("Encryption UpdateFromData");

            previousAgilePointerScale = agilePointerScale;
            agilePointerScale = updatedEncryption.agilePointerScale;
            previousIsOrthagonalBeamFiring = isOrthagonalBeamFiring;
            isOrthagonalBeamFiring = updatedEncryption.isOrthagonalBeamFiring;
            previousPos = position;
            previousRot = rotation;
            position = updatedEncryption.position;
            rotation = updatedEncryption.rotation;
            Mod.LogInfo("\t0");
            if (physicalEncryption != null)
            {
                Mod.LogInfo("\t1");
                physicalEncryption.physicalEncryption.AgilePointerScale = agilePointerScale.x;
                if (physicalEncryption.physicalEncryption.AgilePointer != null)
                {
                    Mod.LogInfo("\t\t2");
                    physicalEncryption.physicalEncryption.AgilePointer.localScale = agilePointerScale;
                }

                Mod.LogInfo("\t1");
                if (type == TNH_EncryptionType.Orthagonal && (full || previousIsOrthagonalBeamFiring != isOrthagonalBeamFiring))
                {
                    Mod.LogInfo("\t\t2");
                    if (isOrthagonalBeamFiring)
                    {
                        for (int i = 0; i < physicalEncryption.physicalEncryption.HitPoints.Count; i++)
                        {
                            physicalEncryption.physicalEncryption.HitPoints[i].Geo.SetActive(true);
                            ParticleSystem.EmissionModule emission = physicalEncryption.physicalEncryption.HitPoints[i].PSys.emission;
                            emission.enabled = true;
                        }
                        if (!physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.isPlaying)
                        {
                            physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.Play();
                        }
                        SM.PlayCoreSound(FVRPooledAudioType.Explosion, physicalEncryption.physicalEncryption.AudEvent_OrthagonalLaserStart, physicalEncryption.transform.position);
                    }
                    else
                    {
                        Mod.LogInfo("\t\t\t3");
                        for (int i = 0; i < physicalEncryption.physicalEncryption.HitPoints.Count; i++)
                        {
                            physicalEncryption.physicalEncryption.HitPoints[i].Geo.SetActive(false);
                            ParticleSystem.EmissionModule emission = physicalEncryption.physicalEncryption.HitPoints[i].PSys.emission;
                            emission.enabled = false;
                        }
                        if (physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.isPlaying)
                        {
                            physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.Stop();
                        }
                        SM.PlayCoreSound(FVRPooledAudioType.Explosion, physicalEncryption.physicalEncryption.AudEvent_OrthagonalLaserEnd, physicalEncryption.transform.position);
                    }
                    Mod.LogInfo("\t\t2");
                }

                Mod.LogInfo("\t1");
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

                Mod.LogInfo("\t1");
                if (full)
                {
                    Mod.LogInfo("\t\t2");
                    if (subTargsActive.Length == physicalEncryption.physicalEncryption.SubTargs.Count)
                    {
                        int subTargsLeft = 0;
                        for (int i = 0; i < subTargsActive.Length; ++i)
                        {
                            if (subTargsActive[i])
                            {
                                physicalEncryption.physicalEncryption.SubTargs[i].SetActive(true);
                                ++subTargsLeft;
                            }
                            else
                            {
                                physicalEncryption.physicalEncryption.SubTargs[i].SetActive(false);
                            }
                        }
                        physicalEncryption.physicalEncryption.m_numSubTargsLeft = subTargsLeft;
                    }
                    if (subTargGeosActive.Length == physicalEncryption.physicalEncryption.SubTargGeo.Count)
                    {
                        for (int i = 0; i < subTargGeosActive.Length; ++i)
                        {
                            if (subTargGeosActive[i])
                            {
                                physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.SetActive(true);
                            }
                            else
                            {
                                physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.SetActive(false);
                            }
                        }
                    }

                    physicalEncryption.physicalEncryption.m_numHitsLeft = numHitsLeft;
                    if (physicalEncryption.physicalEncryption.UsesMultipleDisplay)
                    {
                        ++EncryptionPatch.updateDisplaySkip;
                        physicalEncryption.physicalEncryption.UpdateDisplay();
                        --EncryptionPatch.updateDisplaySkip;
                    }

                    if (physicalEncryption.physicalEncryption.UseReturnToSpawnForce && physicalEncryption.physicalEncryption.m_returnToSpawnLine == null)
                    {
                        physicalEncryption.physicalEncryption.initialPos = initialPos;
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(physicalEncryption.physicalEncryption.ReturnToSpawnLineGO, physicalEncryption.transform.position, Quaternion.identity);
                        physicalEncryption.physicalEncryption.m_returnToSpawnLine = gameObject.transform;
                        physicalEncryption.physicalEncryption.UpdateLine();
                    }
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            previousRot = rotation;
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            previousAgilePointerScale = agilePointerScale;
            agilePointerScale = packet.ReadVector3();
            previousIsOrthagonalBeamFiring = isOrthagonalBeamFiring;
            isOrthagonalBeamFiring = packet.ReadBool();

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
                length = packet.ReadInt();
                if (length > 0)
                {
                    subTargGeosActive = new bool[length];
                    for (int i = 0; i < length; ++i)
                    {
                        subTargGeosActive[i] = packet.ReadBool();
                    }
                }
                numHitsLeft = packet.ReadInt();
                initialPos = packet.ReadVector3();
                cascadingIndex = packet.ReadByte();
                cascadingDepth = packet.ReadByte();
            }

            if (physicalEncryption != null)
            {
                physicalEncryption.physicalEncryption.AgilePointerScale = agilePointerScale.x;
                if (physicalEncryption.physicalEncryption.AgilePointer != null)
                {
                    physicalEncryption.physicalEncryption.AgilePointer.localScale = agilePointerScale;
                }

                if (full || previousIsOrthagonalBeamFiring != isOrthagonalBeamFiring)
                {
                    if (isOrthagonalBeamFiring)
                    {
                        for (int i = 0; i < physicalEncryption.physicalEncryption.HitPoints.Count; i++)
                        {
                            physicalEncryption.physicalEncryption.HitPoints[i].Geo.SetActive(true);
                            ParticleSystem.EmissionModule emission = physicalEncryption.physicalEncryption.HitPoints[i].PSys.emission;
                            emission.enabled = true;
                        }
                        if (!physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.isPlaying)
                        {
                            physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.Play();
                        }
                        SM.PlayCoreSound(FVRPooledAudioType.Explosion, physicalEncryption.physicalEncryption.AudEvent_OrthagonalLaserStart, physicalEncryption.transform.position);
                    }
                    else
                    {
                        for (int i = 0; i < physicalEncryption.physicalEncryption.HitPoints.Count; i++)
                        {
                            physicalEncryption.physicalEncryption.HitPoints[i].Geo.SetActive(false);
                            ParticleSystem.EmissionModule emission = physicalEncryption.physicalEncryption.HitPoints[i].PSys.emission;
                            emission.enabled = false;
                        }
                        if (physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.isPlaying)
                        {
                            physicalEncryption.physicalEncryption.AudSource_OrthagonalLaserLoop.Stop();
                        }
                        SM.PlayCoreSound(FVRPooledAudioType.Explosion, physicalEncryption.physicalEncryption.AudEvent_OrthagonalLaserEnd, physicalEncryption.transform.position);
                    }
                }

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

                if (full)
                {
                    if (subTargsActive.Length == physicalEncryption.physicalEncryption.SubTargs.Count)
                    {
                        int subTargsLeft = 0;
                        for (int i = 0; i < subTargsActive.Length; ++i)
                        {
                            if (subTargsActive[i])
                            {
                                physicalEncryption.physicalEncryption.SubTargs[i].SetActive(true);
                                ++subTargsLeft;
                            }
                            else
                            {
                                physicalEncryption.physicalEncryption.SubTargs[i].SetActive(false);
                            }
                        }
                        physicalEncryption.physicalEncryption.m_numSubTargsLeft = subTargsLeft;
                    }
                    if (subTargGeosActive.Length == physicalEncryption.physicalEncryption.SubTargGeo.Count)
                    {
                        for (int i = 0; i < subTargGeosActive.Length; ++i)
                        {
                            if (subTargGeosActive[i])
                            {
                                physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.SetActive(true);
                            }
                            else
                            {
                                physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.SetActive(false);
                            }
                        }
                    }

                    physicalEncryption.physicalEncryption.m_numHitsLeft = numHitsLeft;
                    if (physicalEncryption.physicalEncryption.UsesMultipleDisplay)
                    {
                        ++EncryptionPatch.updateDisplaySkip;
                        physicalEncryption.physicalEncryption.UpdateDisplay();
                        --EncryptionPatch.updateDisplaySkip;
                    }

                    if (physicalEncryption.physicalEncryption.UseReturnToSpawnForce && physicalEncryption.physicalEncryption.m_returnToSpawnLine == null)
                    {
                        physicalEncryption.physicalEncryption.initialPos = initialPos;
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(physicalEncryption.physicalEncryption.ReturnToSpawnLineGO, physicalEncryption.transform.position, Quaternion.identity);
                        physicalEncryption.physicalEncryption.m_returnToSpawnLine = gameObject.transform;
                        physicalEncryption.physicalEncryption.UpdateLine();
                    }
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

            if (full)
            {
                type = physicalEncryption.physicalEncryption.Type;
                if(type == TNH_EncryptionType.Hardened)
                {
                    if (physicalEncryption.physicalEncryption.name.Contains("Encryption_Target_2_Hardened_V2"))
                    {
                        type = (TNH_EncryptionType)11;
                    }
                }
                else if(type == TNH_EncryptionType.Regenerative)
                {
                    if (physicalEncryption.physicalEncryption.name.Contains("Encryption_7_RegenerativeV2"))
                    {
                        type = (TNH_EncryptionType)12;
                    }
                }
                subTargsActive = new bool[physicalEncryption.physicalEncryption.SubTargs.Count];
                subTargGeosActive = new bool[physicalEncryption.physicalEncryption.SubTargGeo.Count];
                for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargs.Count; ++i)
                {
                    subTargsActive[i] = physicalEncryption.physicalEncryption.SubTargs[i].activeSelf;
                }
                for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargGeo.Count; ++i)
                {
                    subTargGeosActive[i] = physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.activeSelf;
                }
                numHitsLeft = physicalEncryption.physicalEncryption.m_numHitsLeft;
                initialPos = physicalEncryption.physicalEncryption.initialPos;
                cascadingIndex = (byte)EncryptionPatch.cascadingDestroyIndex;
                cascadingDepth = (byte)EncryptionPatch.cascadingDestroyDepth;
            }

            previousAgilePointerScale = agilePointerScale;
            if (physicalEncryption.physicalEncryption.AgilePointer != null)
            {
                agilePointerScale = physicalEncryption.physicalEncryption.AgilePointer.localScale;
            }
            previousIsOrthagonalBeamFiring = isOrthagonalBeamFiring;
            isOrthagonalBeamFiring = physicalEncryption.physicalEncryption.isOrthagonalBeamFiring;
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
            return base.NeedsUpdate() || !previousPos.Equals(position) || !previousRot.Equals(rotation) || agilePointerScale != previousAgilePointerScale || isOrthagonalBeamFiring != previousIsOrthagonalBeamFiring;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(agilePointerScale);
            packet.Write(isOrthagonalBeamFiring);

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
                if (subTargGeosActive == null || subTargGeosActive.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(subTargGeosActive.Length);
                    for (int i = 0; i < subTargGeosActive.Length; ++i)
                    {
                        packet.Write(subTargGeosActive[i]);
                    }
                }
                packet.Write(numHitsLeft);
                packet.Write(initialPos);
                packet.Write(cascadingIndex);
                packet.Write(cascadingDepth);
            }
        }

        public static string EncryptionTypeToID(TNH_EncryptionType type, string name = null)
        {
            switch (type)
            {
                case TNH_EncryptionType.Static:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Hardened:
                    // TODO: Future: The new Hardened is named EncryptionTarget_2_Hardened_V2. Will need to replace once implemented if ID is similarly different
                    return "TNH_EncryptionTarget_2_Hardened";
                case TNH_EncryptionType.Swarm:
                    return "TNH_EncryptionTarget_3_Swarm";
                case TNH_EncryptionType.Recursive:
                    return "TNH_EncryptionTarget_4_Recursive";
                case TNH_EncryptionType.Stealth:
                    return "TNH_EncryptionTarget_5_Stealth";
                case TNH_EncryptionType.Agile:
                    return "TNH_EncryptionTarget_6_Agile";
                case TNH_EncryptionType.Regenerative:
                    // TODO: Future: The new regenerative is named EncryptionTarget_7_RegenerativeV2. Will need to replace once implemented if ID is similarly different
                    return "TNH_EncryptionTarget_7_Regenerative";
                case TNH_EncryptionType.Polymorphic:
                    // TODO: Future: Once implemented, check if returned ID is correct
                    return "TNH_EncryptionTarget_8_Polymorphic";
                case TNH_EncryptionType.Cascading:
                    // TODO: Future: Once implemented, check if returned ID is correct
                    return "TNH_EncryptionTarget_9_Cascading_Main";
                case TNH_EncryptionType.Orthagonal:
                    // TODO: Future: Once implemented, check if returned ID is correct
                    return "TNH_EncryptionTarget_10_Orthagonal";
                case TNH_EncryptionType.Refractive:
                    return "TNH_EncryptionTarget_1_Static";
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

                ClientSend.EncryptionInit(trackedID, indices, points, initialPos, this.numHitsLeft);

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
            if (localTrackedID != -1 && TrackedEncryption.unknownSpawnSubTargGeo.ContainsKey(localWaitingIndex))
            {
                List<int> indices = TrackedEncryption.unknownSpawnSubTargGeo[localWaitingIndex];

                for (int i = 0; i < indices.Count; ++i) 
                {
                    ClientSend.EncryptionRespawnSubTargGeo(trackedID, indices[i]);
                }

                TrackedEncryption.unknownSpawnSubTargGeo.Remove(localWaitingIndex);
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
            if (localTrackedID != -1 && TrackedEncryption.unknownUpdateDisplay.TryGetValue(localWaitingIndex, out int numHitsLeft))
            {
                ClientSend.UpdateEncryptionDisplay(trackedID, numHitsLeft);

                TrackedEncryption.unknownUpdateDisplay.Remove(localWaitingIndex);
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
                TrackedEncryption.unknownSpawnSubTargGeo.Remove(localWaitingIndex);
                TrackedEncryption.unknownDisableSubTarg.Remove(localWaitingIndex);
                TrackedEncryption.unknownUpdateDisplay.Remove(localWaitingIndex);

                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalEncryption != null && physicalEncryption.physicalEncryption != null)
                {
                    GameManager.trackedEncryptionByEncryption.Remove(physicalEncryption.physicalEncryption);
                    GameManager.trackedObjectByDamageable.Remove(physicalEncryption.physicalEncryption);
                    for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargs.Count; ++i)
                    {
                        IFVRDamageable damageable = physicalEncryption.physicalEncryption.SubTargs[i].GetComponent<IFVRDamageable>();
                        if (damageable != null)
                        {
                            GameManager.trackedObjectByDamageable.Remove(damageable);
                        }
                    }
                }
            }
        }
    }
}
