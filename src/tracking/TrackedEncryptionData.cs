using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedEncryptionData : TrackedObjectData
    {
        public TrackedEncryption physicalEncryption;

        public TNH_EncryptionType type;
        public bool simple;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 refractivePreviewPos;
        public Quaternion refractiveShieldRot;

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
            simple = packet.ReadBool();
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
            refractivePreviewPos = packet.ReadVector3();
            refractiveShieldRot = packet.ReadQuaternion();
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
            data.sceneInit = GameManager.InSceneInit();

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
            if (prefab == null)
            {
                if (GM.TNH_Manager == null)
                {
                    if(IM.OD.TryGetValue(EncryptionTypeToID(type, simple), out FVRObject obj))
                    {
                        yield return obj.GetGameObjectAsync();
                        prefab = obj.GetGameObject();
                    }
                }
                else
                {
                    if (simple)
                    {
                        // TODO: Future: Change to GM.TNH_Manager.GetEncryptionPrefabSimple whenever Nathan decides to update gamelibs
                        MethodInfo GetEncryptionPrefabSimple = typeof(TNH_Manager).GetMethod("GetEncryptionPrefabSimple", BindingFlags.Public | BindingFlags.Instance);
                        FVRObject simpleEncryption = GetEncryptionPrefabSimple.Invoke(GM.TNH_Manager, new object[] { type }) as FVRObject;
                        if (simpleEncryption != null)
                        {
                            prefab = simpleEncryption.GetGameObject();
                        }
                    }
                    else
                    {
                        if (GM.TNH_Manager.GetEncryptionPrefab(type) != null)
                        {
                            prefab = GM.TNH_Manager.GetEncryptionPrefab(type).GetGameObject();
                        }
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
            if (subTargsActive == null)
            {
                subTargsActive = new bool[physicalEncryption.physicalEncryption.SubTargs.Count];
            }
            if (subTargGeosActive == null)
            {
                subTargGeosActive = new bool[physicalEncryption.physicalEncryption.SubTargGeo.Count];
            }

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
                simple = updatedEncryption.simple;
                subTargsActive = updatedEncryption.subTargsActive;
                subTargGeosActive = updatedEncryption.subTargGeosActive;
                numHitsLeft = updatedEncryption.numHitsLeft;
                initialPos = updatedEncryption.initialPos;
                cascadingIndex = updatedEncryption.cascadingIndex;
                cascadingDepth = updatedEncryption.cascadingDepth;
                refractivePreviewPos = updatedEncryption.refractivePreviewPos;
                refractiveShieldRot = updatedEncryption.refractiveShieldRot;
            }

            previousAgilePointerScale = agilePointerScale;
            agilePointerScale = updatedEncryption.agilePointerScale;
            previousIsOrthagonalBeamFiring = isOrthagonalBeamFiring;
            isOrthagonalBeamFiring = updatedEncryption.isOrthagonalBeamFiring;
            previousPos = position;
            previousRot = rotation;
            position = updatedEncryption.position;
            rotation = updatedEncryption.rotation;
            if (physicalEncryption != null)
            {
                physicalEncryption.physicalEncryption.AgilePointerScale = agilePointerScale.x;
                if (physicalEncryption.physicalEncryption.AgilePointer != null)
                {
                    physicalEncryption.physicalEncryption.AgilePointer.localScale = agilePointerScale;
                }

                if (type == TNH_EncryptionType.Orthagonal && (full || previousIsOrthagonalBeamFiring != isOrthagonalBeamFiring))
                {
                    physicalEncryption.physicalEncryption.isOrthagonalBeamFiring = isOrthagonalBeamFiring;
                    if (isOrthagonalBeamFiring)
                    {
                        foreach (Transform transform in physicalEncryption.physicalEncryption.OrthagonalBeams)
                        {
                            transform.gameObject.SetActive(true);
                        }
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
                        foreach (Transform transform in physicalEncryption.physicalEncryption.OrthagonalBeams)
                        {
                            transform.gameObject.SetActive(false);
                        }
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
                    if (physicalEncryption.physicalEncryption.UsesMultipleDisplay
                        && physicalEncryption.physicalEncryption.DisplayList.Count > numHitsLeft
                        && physicalEncryption.physicalEncryption.DisplayList[numHitsLeft] != null)
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

                    if (type == TNH_EncryptionType.Refractive)
                    {
                        physicalEncryption.physicalEncryption.RefractivePreview.position = refractivePreviewPos;
                        physicalEncryption.physicalEncryption.RefractiveShield.rotation = refractiveShieldRot;
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
                simple = packet.ReadBool();
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
                refractivePreviewPos = packet.ReadVector3();
                refractiveShieldRot = packet.ReadQuaternion();
            }

            if (physicalEncryption != null)
            {
                physicalEncryption.physicalEncryption.AgilePointerScale = agilePointerScale.x;
                if (physicalEncryption.physicalEncryption.AgilePointer != null)
                {
                    physicalEncryption.physicalEncryption.AgilePointer.localScale = agilePointerScale;
                }

                if (type == TNH_EncryptionType.Orthagonal && (full || previousIsOrthagonalBeamFiring != isOrthagonalBeamFiring))
                {
                    physicalEncryption.physicalEncryption.isOrthagonalBeamFiring = isOrthagonalBeamFiring;
                    if (isOrthagonalBeamFiring)
                    {
                        foreach (Transform transform in physicalEncryption.physicalEncryption.OrthagonalBeams)
                        {
                            transform.gameObject.SetActive(true);
                        }
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
                        foreach (Transform transform in physicalEncryption.physicalEncryption.OrthagonalBeams)
                        {
                            transform.gameObject.SetActive(false);
                        }
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
                    if (physicalEncryption.physicalEncryption.UsesMultipleDisplay
                        && physicalEncryption.physicalEncryption.DisplayList.Count > numHitsLeft
                        && physicalEncryption.physicalEncryption.DisplayList[numHitsLeft] != null)
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

                    if (type == TNH_EncryptionType.Refractive)
                    {
                        physicalEncryption.physicalEncryption.RefractivePreview.position = refractivePreviewPos;
                        physicalEncryption.physicalEncryption.RefractiveShield.rotation = refractiveShieldRot;
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
                simple = physicalEncryption.name.Contains("_Simple");
                subTargsActive = new bool[physicalEncryption.physicalEncryption.SubTargs == null ? 0 : physicalEncryption.physicalEncryption.SubTargs.Count];
                subTargGeosActive = new bool[physicalEncryption.physicalEncryption.SubTargGeo == null ? 0 : physicalEncryption.physicalEncryption.SubTargGeo.Count];
                if(physicalEncryption.physicalEncryption.SubTargs != null)
                {
                    for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargs.Count; ++i)
                    {
                        subTargsActive[i] = physicalEncryption.physicalEncryption.SubTargs[i].activeSelf;
                    }
                }
                if(physicalEncryption.physicalEncryption.SubTargGeo != null)
                {
                    for (int i = 0; i < physicalEncryption.physicalEncryption.SubTargGeo.Count; ++i)
                    {
                        subTargGeosActive[i] = physicalEncryption.physicalEncryption.SubTargGeo[i].gameObject.activeSelf;
                    }
                }
                numHitsLeft = physicalEncryption.physicalEncryption.m_numHitsLeft;
                initialPos = physicalEncryption.physicalEncryption.initialPos;
                cascadingIndex = (byte)EncryptionPatch.cascadingDestroyIndex;
                cascadingDepth = (byte)EncryptionPatch.cascadingDestroyDepth;
                if(type == TNH_EncryptionType.Refractive)
                {
                    refractivePreviewPos = physicalEncryption.physicalEncryption.RefractivePreview.position;
                    refractiveShieldRot = physicalEncryption.physicalEncryption.RefractiveShield.rotation;
                }
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
                packet.Write(simple);
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
                packet.Write(refractivePreviewPos);
                packet.Write(refractiveShieldRot);
            }
        }

        public static string EncryptionTypeToID(TNH_EncryptionType type, bool simple)
        {
            switch (type)
            {
                case TNH_EncryptionType.Static:
                    return "TNH_EncryptionTarget_1_Static";
                case TNH_EncryptionType.Hardened:
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
                    return "TNH_EncryptionTarget_7_Regenerative"+(simple ? "_Simple":"");
                case TNH_EncryptionType.Polymorphic:
                    return "TNH_EncryptionTarget_8_Polymorphic" + (simple ? "_Simple" : "");
                case TNH_EncryptionType.Cascading:
                    return "TNH_EncryptionTarget_9_Cascading" + (simple ? "_Simple" : "");
                case TNH_EncryptionType.Orthagonal:
                    return "TNH_EncryptionTarget_10_Orthagonal" + (simple ? "_Simple" : "");
                case TNH_EncryptionType.Refractive:
                    return "TNH_EncryptionTarget_11_Refractive";
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
                List<int> indices = TrackedEncryption.unknownInit[localWaitingIndex];

                ClientSend.EncryptionInit(trackedID, indices, initialPos, this.numHitsLeft);

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
            if (localTrackedID != -1 && TrackedEncryption.unknownPreviewPos.TryGetValue(localWaitingIndex, out Vector3 previewPos))
            {
                ClientSend.EncryptionNextPos(trackedID, previewPos);

                TrackedEncryption.unknownPreviewPos.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedEncryption.unknownShieldRot.TryGetValue(localWaitingIndex, out Quaternion shieldRot))
            {
                ClientSend.EncryptionShieldRot(trackedID, shieldRot);

                TrackedEncryption.unknownShieldRot.Remove(localWaitingIndex);
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
                TrackedEncryption.unknownPreviewPos.Remove(localWaitingIndex);
                TrackedEncryption.unknownShieldRot.Remove(localWaitingIndex);

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
