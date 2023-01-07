using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

namespace H3MP
{
    internal class H3MP_ClientHandle
    {
        public static void Welcome(H3MP_Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();

            Debug.Log($"Message from server: {msg}");

            H3MP_Client.singleton.ID = ID;
            H3MP_GameManager.ID = ID;
            H3MP_ClientSend.WelcomeReceived();

            H3MP_Client.singleton.udp.Connect(((IPEndPoint)H3MP_Client.singleton.tcp.socket.Client.LocalEndPoint).Port);
        }

        public static void SpawnPlayer(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            int IFF = packet.ReadInt();

            H3MP_GameManager.singleton.SpawnPlayer(ID, username, scene, instance, position, rotation, IFF);
        }

        public static void ConnectSync(H3MP_Packet packet)
        {
            bool inControl = packet.ReadBool();

            // Just connected, sync if current scene is syncable
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(SceneManager.GetActiveScene().name))
            {
                H3MP_GameManager.SyncTrackedSosigs(true, inControl);
                H3MP_GameManager.SyncTrackedAutoMeaters(true, inControl);
                H3MP_GameManager.SyncTrackedItems(true, inControl);
                H3MP_GameManager.SyncTrackedEncryptions(true, inControl);
            }
        }

        public static void PlayerState(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            Vector3 headPos = packet.ReadVector3();
            Quaternion headRot = packet.ReadQuaternion();
            Vector3 torsoPos = packet.ReadVector3();
            Quaternion torsoRot = packet.ReadQuaternion();
            Vector3 leftHandPos = packet.ReadVector3();
            Quaternion leftHandRot = packet.ReadQuaternion();
            Vector3 rightHandPos = packet.ReadVector3();
            Quaternion rightHandRot = packet.ReadQuaternion();
            float health = packet.ReadFloat();
            int maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if (additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            H3MP_GameManager.UpdatePlayerState(ID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                               leftHandPos, leftHandRot,
                                               rightHandPos, rightHandRot,
                                               health, maxHealth, additionalData);
        }

        public static void PlayerScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(ID, scene);
        }

        public static void PlayerInstance(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerInstance(ID, instance);
        }

        public static void TrackedItems(H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void TrackedSosigs(H3MP_Packet packet)
        {
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TrackedAutoMeaters(H3MP_Packet packet)
        {
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedAutoMeater(packet.ReadTrackedAutoMeater());
            }
        }

        public static void TrackedEncryptions(H3MP_Packet packet)
        {
            // Reconstruct passed TrackedEncryptions from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedEncryption(packet.ReadTrackedEncryption());
            }
        }

        public static void TrackedItem(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedItem(packet.ReadTrackedItem(true), packet.ReadString(), packet.ReadInt());
        }

        public static void TrackedSosig(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedSosig(packet.ReadTrackedSosig(true), packet.ReadString(), packet.ReadInt());
        }

        public static void TrackedAutoMeater(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedAutoMeater(packet.ReadTrackedAutoMeater(true), packet.ReadString(), packet.ReadInt());
        }

        public static void TrackedEncryption(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedEncryption(packet.ReadTrackedEncryption(true), packet.ReadString(), packet.ReadInt());
        }

        public static void AddSyncScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.synchronizedScenes.Add(scene, ID);
        }

        public static void GiveControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if (trackedItem.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                FVRPhysicalObject physObj = trackedItem.physicalItem.GetComponent<FVRPhysicalObject>();

                H3MP_GameManager.EnsureUncontrolled(physObj);

                Mod.SetKinematicRecursive(physObj.transform, true);
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            else if(trackedItem.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedItem.controller = controllerID;
                if(trackedItem.physicalItem != null)
                {
                    Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
                }
                trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(trackedItem);
            }
            trackedItem.controller = controllerID;
        }

        public static void GiveSosigControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[trackedID];

            if (trackedSosig.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                trackedSosig.localTrackedID = -1;

                if (trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                    trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
                }
            }
            else if(trackedSosig.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedSosig.controller = controllerID;
                trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                H3MP_GameManager.sosigs.Add(trackedSosig);

                if (trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                    trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                }
            }
            trackedSosig.controller = controllerID;
        }

        public static void GiveAutoMeaterControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[trackedID];

            if (trackedAutoMeater.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID].localTrackedID = trackedAutoMeater.localTrackedID;
                H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                trackedAutoMeater.localTrackedID = -1;

                if (trackedAutoMeater.physicalObject != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                    trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
                }
            }
            else if(trackedAutoMeater.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedAutoMeater.controller = controllerID;
                trackedAutoMeater.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                H3MP_GameManager.autoMeaters.Add(trackedAutoMeater);

                if (trackedAutoMeater.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                    trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                }
            }
            trackedAutoMeater.controller = controllerID;
        }

        public static void GiveEncryptionControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Client.encryptions[trackedID];

            if (trackedEncryption.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID].localTrackedID = trackedEncryption.localTrackedID;
                H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
                trackedEncryption.localTrackedID = -1;
            }
            else if(trackedEncryption.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedEncryption.localTrackedID = H3MP_GameManager.encryptions.Count;
                H3MP_GameManager.encryptions.Add(trackedEncryption);
            }
            trackedEncryption.controller = controllerID;
        }

        public static void DestroyItem(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if (trackedItem != null)
            {
                if (trackedItem.physicalItem != null)
                {
                    H3MP_GameManager.trackedItemByItem.Remove(trackedItem.physicalItem.physicalObject);
                    if (trackedItem.physicalItem.physicalObject is SosigWeaponPlayerInterface)
                    {
                        H3MP_GameManager.trackedItemBySosigWeapon.Remove((trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W);
                    }
                    trackedItem.physicalItem.sendDestroy = false;
                    GameObject.Destroy(trackedItem.physicalItem.gameObject);
                }

                if (trackedItem.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }

                if (removeFromList)
                {
                    H3MP_Client.items[trackedID] = null;
                }
            }
        }

        public static void DestroySosig(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[trackedID];

            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    H3MP_GameManager.trackedSosigBySosig.Remove(trackedSosig.physicalObject.physicalSosigScript);
                    trackedSosig.physicalObject.sendDestroy = false;
                    GameObject.Destroy(trackedSosig.physicalObject.gameObject);
                }

                if (trackedSosig.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                    H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                    H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                }

                if (removeFromList)
                {
                    H3MP_Client.sosigs[trackedID] = null;

                    Mod.temporaryHoldSosigIDs.Remove(trackedID);
                    Mod.temporarySupplySosigIDs.Remove(trackedID);
                }
            }
        }

        public static void DestroyAutoMeater(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[trackedID];

            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.physicalObject != null)
                {
                    H3MP_GameManager.trackedAutoMeaterByAutoMeater.Remove(trackedAutoMeater.physicalObject.physicalAutoMeaterScript);
                    trackedAutoMeater.physicalObject.sendDestroy = false;
                    GameObject.Destroy(trackedAutoMeater.physicalObject.gameObject);
                }

                if (trackedAutoMeater.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                    H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID].localTrackedID = trackedAutoMeater.localTrackedID;
                    H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                }

                if (removeFromList)
                {
                    H3MP_Client.autoMeaters[trackedID] = null;

                    Mod.temporaryHoldTurretIDs.Remove(trackedID);
                    Mod.temporarySupplyTurretIDs.Remove(trackedID);
                }
            }
        }

        public static void DestroyEncryption(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Client.encryptions[trackedID];

            if (trackedEncryption != null)
            {
                if (trackedEncryption.physicalObject != null)
                {
                    H3MP_GameManager.trackedEncryptionByEncryption.Remove(trackedEncryption.physicalObject.physicalEncryptionScript);
                    trackedEncryption.physicalObject.sendDestroy = false;
                    GameObject.Destroy(trackedEncryption.physicalObject.gameObject);
                }

                if (trackedEncryption.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.encryptions[trackedEncryption.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                    H3MP_GameManager.encryptions[trackedEncryption.localTrackedID].localTrackedID = trackedEncryption.localTrackedID;
                    H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
                }

                if (removeFromList)
                {
                    H3MP_Client.encryptions[trackedID] = null;
                }
            }
        }

        public static void ItemParent(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            H3MP_Client.items[trackedID].SetParent(newParentID);
        }

        public static void WeaponFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Client.items[trackedID].physicalItem.fireFunc();
            }
        }

        public static void BreakActionWeaponFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int barrelIndex = packet.ReadByte();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Client.items[trackedID].physicalItem.fireFunc();
            }
        }

        public static void SosigWeaponFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            float recoilMult = packet.ReadFloat();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FireSosigWeaponPatch.positions = new List<Vector3>();
                FireSosigWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireSosigWeaponPatch.positions.Add(packet.ReadVector3());
                    FireSosigWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireSosigWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Client.items[trackedID].physicalItem.sosigWeaponfireFunc(recoilMult);
            }
        }

        public static void LAPD2019Fire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FireLAPD2019Patch.positions = new List<Vector3>();
                FireLAPD2019Patch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireLAPD2019Patch.positions.Add(packet.ReadVector3());
                    FireLAPD2019Patch.directions.Add(packet.ReadVector3());
                }
                FireLAPD2019Patch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Mod.LAPD2019_Fire.Invoke((LAPD2019)H3MP_Client.items[trackedID].physicalItem.physicalObject, null);
            }
        }
        
        public static void MinigunFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FireMinigunPatch.positions = new List<Vector3>();
                FireMinigunPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireMinigunPatch.positions.Add(packet.ReadVector3());
                    FireMinigunPatch.directions.Add(packet.ReadVector3());
                }
                FireMinigunPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Mod.Minigun_Fire.Invoke((LAPD2019)H3MP_Client.items[trackedID].physicalItem.physicalObject, null);
            }
        }
        
        public static void AttachableFirearmFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool firedFromInterface = packet.ReadBool();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                FireAttachableFirearmPatch.positions = new List<Vector3>();
                FireAttachableFirearmPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireAttachableFirearmPatch.positions.Add(packet.ReadVector3());
                    FireAttachableFirearmPatch.directions.Add(packet.ReadVector3());
                }
                FireAttachableFirearmPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Client.items[trackedID].physicalItem.attachableFirearmFunc(firedFromInterface);
            }
        }

        public static void LAPD2019LoadBattery(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Client.items[trackedID].physicalItem != null && H3MP_Client.items[batteryTrackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)H3MP_Client.items[trackedID].physicalItem.physicalObject).LoadBattery((LAPD2019Battery)H3MP_Client.items[batteryTrackedID].physicalItem.physicalObject);
                --LAPD2019ActionPatch.loadBatterySkip;
            }
        }

        public static void LAPD2019ExtractBattery(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)H3MP_Client.items[trackedID].physicalItem.physicalObject).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }
        }

        public static void SosigWeaponShatter(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID].physicalItem != null)
            {
                ++SosigWeaponShatterPatch.skip;
                typeof(SosigWeaponPlayerInterface).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance).Invoke((H3MP_Client.items[trackedID].physicalItem.physicalObject as SosigWeaponPlayerInterface).W, null);
                --SosigWeaponShatterPatch.skip;
            }
        }

        public static void AutoMeaterFirearmFireShot(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            if (H3MP_Client.autoMeaters[trackedID].physicalObject != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                Mod.AutoMeaterFirearm_FireShot.Invoke(H3MP_Client.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0], null);
                --AutoMeaterFirearmFireShotPatch.skip;
            }
        }

        public static void PlayerDamage(H3MP_Packet packet)
        {
            H3MP_PlayerHitbox.Part part = (H3MP_PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_GameManager.ProcessPlayerDamage(part, damage);
        }

        public static void SosigPickUpItem(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(H3MP_Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(H3MP_Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
            }
        }

        public static void SosigPlaceItemIn(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].PlaceObjectIn(H3MP_Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
            }
        }

        public static void SosigDropSlot(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].DetachHeldObject();
            }
        }

        public static void SosigHandDrop(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.DropHeldObject();
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.DropHeldObject();
                }
            }
        }

        public static void SosigConfigure(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;
                if (trackedSosig.physicalObject != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalObject.physicalSosigScript.Configure(config);
                }
            }
        }

        public static void SosigLinkRegisterWearable(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if(trackedSosig.physicalObject != null)
                    {
                        foreach(SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while(trackedSosig.wearables.Count <= linkIndex)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                }
                trackedSosig.wearables[linkIndex].Add(wearableID);

                if (trackedSosig.physicalObject != null)
                {
                    AnvilManager.Run(trackedSosig.EquipWearable(linkIndex, wearableID, true));
                }
            }
        }

        public static void SosigLinkDeRegisterWearable(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables != null)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                        {
                            if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                            {
                                trackedSosig.wearables[linkIndex].RemoveAt(i);
                                if (trackedSosig.physicalObject != null)
                                {
                                    ++SosigLinkActionPatch.skipDeRegisterWearable;
                                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].DeRegisterWearable((wearablesField.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[i]);
                                    --SosigLinkActionPatch.skipDeRegisterWearable;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        trackedSosig.wearables[linkIndex].Remove(wearableID);
                    }
                }
            }
        }

        public static void SosigSetIFF(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetIFF(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigSetOriginalIFF(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetOriginalIFFTeam(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigLinkDamage(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].Damage(damage);
                        --SosigLinkDamagePatch.skip;
                    }
                }
            }
        }

        public static void AutoMeaterDamage(H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalObject != null)
                    {
                        ++AutoMeaterDamagePatch.skip;
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Damage(damage);
                        --AutoMeaterDamagePatch.skip;
                    }
                }
            }
        }

        public static void AutoMeaterHitZoneDamage(H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            byte type = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalObject != null)
                    {
                        ++AutoMeaterHitZoneDamagePatch.skip;
                        trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)type].Damage(damage);
                        --AutoMeaterHitZoneDamagePatch.skip;
                    }
                }
            }
        }

        public static void EncryptionDamage(H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Client.encryptions[autoMeaterTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.Damage(damage);
                        --EncryptionDamagePatch.skip;
                    }
                }
            }
        }

        public static void EncryptionSubDamage(H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Client.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == H3MP_GameManager.ID)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigWeaponDamage(H3MP_Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[sosigWeaponTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == H3MP_GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++SosigWeaponDamagePatch.skip;
                        (trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W.Damage(damage);
                        --SosigWeaponDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigWearableDamage(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigWearableDamagePatch.skip;
                        (Mod.SosigLink_m_wearables.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigSetBodyState(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigSetBodyStateSkip;
                    Mod.Sosig_SetBodyState.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { bodyState });
                    --SosigActionPatch.sosigSetBodyStateSkip;
                }
            }
        }

        public static void SosigDamageData(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller != H3MP_Client.singleton.ID && trackedSosig.physicalObject != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalObject.physicalSosigScript;
                    Mod.Sosig_m_isStunned.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_stunTimeLeft = packet.ReadFloat();
                    physicalSosig.BodyState = (Sosig.SosigBodyState)packet.ReadByte();
                    Mod.Sosig_m_isOnOffMeshLinkField.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.Agent.autoTraverseOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.enabled = packet.ReadBool();
                    List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(physicalSosig);
                    byte jointCount = packet.ReadByte();
                    for (int i = 0; i < jointCount; ++i)
                    {
                        if (joints[i] != null)
                        {
                            SoftJointLimit softJointLimit = joints[i].lowTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].lowTwistLimit = softJointLimit;
                            softJointLimit = joints[i].highTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].highTwistLimit = softJointLimit;
                            softJointLimit = joints[i].swing1Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing1Limit = softJointLimit;
                            softJointLimit = joints[i].swing2Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing2Limit = softJointLimit;
                        }
                    }
                    Mod.Sosig_m_isCountingDownToStagger.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_staggerAmountToApply.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveringFromBallisticState.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_recoveryFromBallisticLerp.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_tickDownToWrithe.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveryFromBallisticTick.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_lastIFFDamageSource.SetValue(physicalSosig, packet.ReadByte());
                    Mod.Sosig_m_diedFromClass.SetValue(physicalSosig, (Damage.DamageClass)packet.ReadByte());
                    Mod.Sosig_m_isBlinded.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_blindTime.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isFrozen.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_debuffTime_Freeze.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_receivedHeadShot.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_timeSinceLastDamage.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isConfused.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_confusedTime = packet.ReadFloat();
                    Mod.Sosig_m_storedShudder.SetValue(physicalSosig, packet.ReadFloat());
                }
            }
        }

        public static void EncryptionDamageData(H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Client.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller != H3MP_Client.singleton.ID && trackedEncryption.physicalObject != null)
                {
                    //TODO
                }
            }
        }

        public static void AutoMeaterHitZoneDamageData(H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller != H3MP_Client.singleton.ID && trackedAutoMeater.physicalObject != null)
                {
                    AutoMeaterHitZone hitZone = trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)packet.ReadByte()];
                    hitZone.ArmorThreshold = packet.ReadFloat();
                    hitZone.LifeUntilFailure = packet.ReadFloat();
                    if (packet.ReadBool()) // Destroyed
                    {
                        hitZone.BlowUp();
                    }
                }
            }
        }

        public static void SosigLinkExplodes(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }
        }

        public static void SosigDies(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }
        }

        public static void SosigClear(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosigScript.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
                }
            }
        }

        public static void PlaySosigFootStepSound(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (H3MP_Client.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = H3MP_Client.sosigs[sosigTrackedID].physicalObject.physicalSosigScript.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }
        }

        public static void SosigSpeakState(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Investigate:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnInvestigate });
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnSearchingForGuns });
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnTakingCover });
                        break;
                    case Sosig.SosigOrder.Wander:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Assault:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnAssault });
                        break;
                }
            }
        }

        public static void SosigSetCurrentOrder(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigSetCurrentOrderSkip;
                trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                --SosigActionPatch.sosigSetCurrentOrderSkip;
            }
        }

        public static void SosigVaporize(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosigScript.Vaporize(trackedSosig.physicalObject.physicalSosigScript.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }
        }

        public static void SosigLinkBreak(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }
        }

        public static void SosigLinkSever(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                Mod.SosigLink_SeverJoint.Invoke(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex], new object[] { damClass, isPullApart });
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }
        }

        public static void SosigRequestHitDecal(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                ++SosigActionPatch.sosigRequestHitDecalSkip;
                trackedSosig.physicalObject.physicalSosigScript.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]);
                --SosigActionPatch.sosigRequestHitDecalSkip;
            }
        }

        public static void RequestUpToDateObjects(H3MP_Packet packet)
        {
            H3MP_ClientSend.UpToDateObjects(packet.ReadBool(), packet.ReadInt());
        }

        public static void AddTNHInstance(H3MP_Packet packet)
        {
            H3MP_GameManager.AddTNHInstance(packet.ReadTNHInstance());
        }

        public static void AddInstance(H3MP_Packet packet)
        {
            H3MP_GameManager.AddInstance(packet.ReadInt());
        }

        public static void AddTNHCurrentlyPlaying(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances == null || !H3MP_GameManager.TNHInstances.ContainsKey(instance))
            {
                Debug.LogError("H3MP_ClientHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                H3MP_GameManager.TNHInstances[instance].AddCurrentlyPlaying(false, ID);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.RemoveCurrentlyPlaying(false, ID);
                if (currentInstance.currentlyPlaying.Count == 0)
                {
                    currentInstance.Reset();
                }
            }
        }

        public static void SetTNHProgression(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].progressionTypeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.progressionSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Progression(i);
                GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)i;
                --TNH_UIManagerPatch.progressionSkip;
            }
        }

        public static void SetTNHEquipment(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].equipmentModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.equipmentSkip;
                Mod.currentTNHUIManager.OBS_EquipmentMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_EquipmentMode(i);
                GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)i;
                --TNH_UIManagerPatch.equipmentSkip;
            }
        }

        public static void SetTNHHealthMode(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].healthModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthModeSkip;
                Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMode(i);
                GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)i;
                --TNH_UIManagerPatch.healthModeSkip;
            }
        }

        public static void SetTNHTargetMode(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].targetModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.targetSkip;
                Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_TargetMode(i);
                GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)i;
                --TNH_UIManagerPatch.targetSkip;
            }
        }

        public static void SetTNHAIDifficulty(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].AIDifficultyModifier = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.AIDifficultySkip;
                Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIDifficulty(i);
                GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)i;
                --TNH_UIManagerPatch.AIDifficultySkip;
            }
        }

        public static void SetTNHRadarMode(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].radarModeModifier = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.radarSkip;
                Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIRadarMode(i);
                GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)i;
                --TNH_UIManagerPatch.radarSkip;
            }
        }

        public static void SetTNHItemSpawnerMode(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].itemSpawnerMode = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.itemSpawnerSkip;
                Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_ItemSpawner(i);
                GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)i;
                --TNH_UIManagerPatch.itemSpawnerSkip;
            }
        }

        public static void SetTNHBackpackMode(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].backpackMode = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.backpackSkip;
                Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Backpack(i);
                GM.TNHOptions.BackpackMode = (TNH_BackpackMode)i;
                --TNH_UIManagerPatch.backpackSkip;
            }
        }

        public static void SetTNHHealthMult(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].healthMult = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthMultSkip;
                Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMult(i);
                GM.TNHOptions.HealthMult = (TNH_HealthMult)i;
                --TNH_UIManagerPatch.healthMultSkip;
            }
        }

        public static void SetTNHSosigGunReload(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].sosiggunShakeReloading = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.sosigGunReloadSkip;
                Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_SosiggunShakeReloading(i);
                GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)i;
                --TNH_UIManagerPatch.sosigGunReloadSkip;
            }
        }

        public static void SetTNHSeed(H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].TNHSeed = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.seedSkip;
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_RunSeed(i);
                GM.TNHOptions.TNHSeed = i;
                --TNH_UIManagerPatch.seedSkip;
            }
        }

        public static void SetTNHLevelIndex(H3MP_Packet packet)
        {
            int levelIndex = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].levelIndex = levelIndex;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                Mod.TNH_UIManager_m_currentLevelIndex.SetValue(Mod.currentTNHUIManager, levelIndex);
                Mod.currentTNHUIManager.CurLevelID = Mod.currentTNHUIManager.Levels[levelIndex].LevelID;
                Mod.TNH_UIManager_UpdateLevelSelectDisplayAndLoader.Invoke(Mod.currentTNHUIManager, null);
                Mod.TNH_UIManager_UpdateTableBasedOnOptions.Invoke(Mod.currentTNHUIManager, null);
                Mod.TNH_UIManager_PlayButtonSound.Invoke(Mod.currentTNHUIManager, new object[] { 2 });
            }
        }

        public static void SetTNHController(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int newController = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID && newController != H3MP_GameManager.ID)
                {
                    H3MP_ClientSend.TNHData(newController, Mod.currentTNHInstance.manager);

                    //++SetTNHManagerPatch.skip;
                    //Mod.currentTNHInstance.manager.enabled = false;
                    //--SetTNHManagerPatch.skip;
                }
                //else if (newController == H3MP_GameManager.ID && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
                //{
                //    ++SetTNHManagerPatch.skip;
                //    Mod.currentTNHInstance.manager.enabled = true;
                //    --SetTNHManagerPatch.skip;
                //}
            }

            H3MP_GameManager.TNHInstances[instance].controller = newController;
        }

        public static void TNHData(H3MP_Packet packet)
        {
            int controller = packet.ReadInt();

            if (controller == H3MP_GameManager.ID && GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                H3MP_TNHData data = packet.ReadTNHData();

                if (TNH_ManagerPatch.doInit)
                {
                    Mod.initTNHData = data;
                }
                else
                {
                    Mod.InitTNHData(data);
                }
            }
            else
            {
                H3MP_ClientSend.TNHData(packet);
            }
        }

        public static void TNHPlayerDied(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int ID = packet.ReadInt();

            // Process dead
            bool allDead = false;
            H3MP_TNHInstance TNHinstance = H3MP_GameManager.TNHInstances[instance];
            TNHinstance.dead.Add(ID);
            if (TNHinstance.dead.Count >= TNHinstance.currentlyPlaying.Count)
            {
                // Set visibility of all of the previously dead players
                foreach (int playerID in TNHinstance.dead)
                {
                    if (H3MP_GameManager.players.TryGetValue(playerID, out H3MP_PlayerManager player))
                    {
                        player.SetVisible(true);
                    }
                }

                TNHinstance.Reset();
                allDead = true;
            }

            // Set player visibility if still necessary
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentlyPlayingTNH)
            {
                if (allDead)
                {
                    GM.TNH_Manager.PlayerDied();
                }
                else
                {
                    if (H3MP_GameManager.players.TryGetValue(ID, out H3MP_PlayerManager player))
                    {
                        player.SetVisible(false);
                    }
                }
            }
        }

        public static void TNHAddTokens(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int amount = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.tokenCount += amount;

                // Implies we are in-game in this instance 
                if (currentInstance.manager != null && !currentInstance.dead.Contains(H3MP_GameManager.ID))
                {
                    ++TNH_ManagerPatch.addTokensSkip;
                    currentInstance.manager.AddTokens(amount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
            }
        }

        public static void AutoMeaterSetState(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte state = packet.ReadByte();

            if (H3MP_Client.autoMeaters[trackedID] != null && H3MP_Client.autoMeaters[trackedID].physicalObject != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                Mod.AutoMeater_SetState.Invoke(H3MP_Client.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript, new object[] { (AutoMeater.AutoMeaterState)state });
                --AutoMeaterSetStatePatch.skip;
            }
        }

        public static void AutoMeaterSetBladesActive(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                if (active)
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].Reactivate();
                    }
                }
                else
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].ShutDown();
                    }
                }
            }
        }

        public static void AutoMeaterFirearmFireAtWill(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int firearmIndex = packet.ReadInt();
            bool fireAtWill = packet.ReadBool();
            float dist = packet.ReadFloat();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Client.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }
        }

        public static void TNHSosigKill(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[trackedID];
                if (trackedSosig != null && trackedSosig.physicalObject != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.TNH_Manager_OnSosigKill.Invoke(Mod.currentTNHInstance.manager, new object[] { trackedSosig.physicalObject.physicalSosigScript });
                    --TNH_ManagerPatch.sosigKillSkip;
                }
            }
        }

        public static void TNHHoldPointSystemNode(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int charIndex = packet.ReadInt();
            int progressionIndex = packet.ReadInt();
            int progressionEndlessIndex = packet.ReadInt();
            int levelIndex = packet.ReadInt();
            int holdPointIndex = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                TNH_CharacterDef C = null;
                try
                {
                    C = Mod.currentTNHInstance.manager.CharDB.GetDef((TNH_Char)charIndex);
                }
                catch
                {
                    C = Mod.currentTNHInstance.manager.CharDB.GetDef(TNH_Char.DD_BeginnerBlake);
                }
                TNH_Progression currentProgression = null;
                if (progressionIndex != -1)
                {
                    currentProgression = C.Progressions[progressionIndex];
                }
                else // progressionEndlessIndex != -1
                {
                    currentProgression = C.Progressions_Endless[progressionEndlessIndex];
                }
                TNH_Progression.Level curLevel = currentProgression.Levels[levelIndex];
                TNH_HoldPoint holdPoint = Mod.currentTNHInstance.manager.HoldPoints[holdPointIndex];

                if (Mod.currentTNHInstance.holdOngoing)
                {
                    Mod.TNH_HoldPoint_CompleteHold.Invoke((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                    Mod.currentTNHInstance.holdOngoing = false;
                }

                Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
                holdPoint.ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(holdPoint.SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);
            }

            // Update the hold index regardless if this is ours
            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdPointIndex;
            }
        }

        public static void TNHHoldBeginChallenge(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int barrierCount = packet.ReadInt();
            List<int> barrierIndices = new List<int>();
            List<int> barrierPrefabIndices = new List<int>();
            for (int i = 0; i < barrierCount; ++i)
            {
                barrierIndices.Add(packet.ReadInt());
            }
            for (int i = 0; i < barrierCount; ++i)
            {
                barrierPrefabIndices.Add(packet.ReadInt());
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.holdOngoing = true;
                Mod.currentTNHInstance.raisedBarriers = barrierIndices;
                Mod.currentTNHInstance.raisedBarrierPrefabIndices = barrierPrefabIndices;

                Mod.currentTNHInstance.manager.Phase = TNH_Phase.Hold;

                TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);

                // Raise barriers
                for (int i = 0; i < barrierIndices.Count; ++i)
                {
                    TNH_DestructibleBarrierPoint point = curHoldPoint.BarrierPoints[barrierIndices[i]];
                    TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[barrierPrefabIndices[i]];
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                    TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                    Mod.TNH_DestructibleBarrierPoint_m_curBarrier.SetValue(point, curBarrier);
                    curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                    curBarrier.SetBarrierPoint(point);
                    Mod.TNH_DestructibleBarrierPoint_SetCoverPointData.Invoke(point, new object[] { barrierPrefabIndices[i] });
                }

                // Begin hold on our side
                ++TNH_HoldPointPatch.beginHoldSkip;
                curHoldPoint.BeginHoldChallenge();
                --TNH_HoldPointPatch.beginHoldSkip;

                // If we received this it is because we are not the controller, TP to hold point
                GM.CurrentMovementManager.TeleportToPoint(curHoldPoint.SpawnPoint_SystemNode.position, true);
            }
            else if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.holdOngoing = true;
                actualInstance.raisedBarriers = barrierIndices;
                actualInstance.raisedBarrierPrefabIndices = barrierPrefabIndices;
            }
        }

        public static void ShatterableCrateDamage(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Debug.Log("ShatterableCrateDamage received, H3MP_Client.items[trackedID] == null?: " + (H3MP_Client.items[trackedID] == null));
            if(H3MP_Client.items[trackedID] != null)
            Debug.Log("H3MP_Client.items[trackedID].controller: " + H3MP_Client.items[trackedID].controller);

            if (H3MP_Client.items[trackedID] != null && H3MP_Client.items[trackedID].controller == H3MP_GameManager.ID)
            {
                ++TNH_ShatterableCrateDamagePatch.skip;
                H3MP_Client.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
                --TNH_ShatterableCrateDamagePatch.skip;
            }
        }

        public static void ShatterableCrateDestroy(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID] != null && H3MP_Client.items[trackedID].physicalItem != null)
            {
                ++TNH_ShatterableCrateDestroyPatch.skip;
                Mod.TNH_ShatterableCrate_Destroy.Invoke(H3MP_Client.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>(), new object[] { packet.ReadDamage() });
                --TNH_ShatterableCrateDestroyPatch.skip;
            }
        }

        public static void TNHSetLevel(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int level = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.level = level;
                Mod.TNH_Manager_m_level.SetValue(Mod.currentTNHInstance.manager, level);
                Mod.TNH_Manager_SetLevel.Invoke(Mod.currentTNHInstance.manager, new object[] { level });
            }
            else if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.level = level;
            }
        }

        public static void TNHSetPhaseTake(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int activeSupplyCount = packet.ReadInt();
            List<int> activeIndices = new List<int>();
            for (int i = 0; i < activeSupplyCount; ++i)
            {
                activeIndices.Add(packet.ReadInt());
            }
            List<TNH_SupplyPoint.SupplyPanelType> types = new List<TNH_SupplyPoint.SupplyPanelType>();
            for (int i = 0; i < activeSupplyCount; ++i)
            {
                types.Add((TNH_SupplyPoint.SupplyPanelType)packet.ReadByte());
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.activeSupplyPointIndices = activeIndices;
                Mod.currentTNHInstance.supplyPanelTypes = types;
                Mod.TNH_Manager_SetPhase_Take.Invoke(Mod.currentTNHInstance.manager, null);
            }
            else if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.activeSupplyPointIndices = activeIndices;
                actualInstance.supplyPanelTypes = types;
            }
        }

        public static void TNHHoldCompletePhase(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.TNH_HoldPoint_CompletePhase.Invoke(Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
            }
        }

        public static void TNHHoldShutDown(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                ((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager)).ShutDownHoldPoint();
            }
        }

        public static void TNHSetPhaseComplete(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.TNH_Manager_SetPhase_Completed.Invoke(Mod.currentTNHInstance.manager, null);
            }
        }

        public static void TNHSetPhase(H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            short p = packet.ReadShort();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.phase = (TNH_Phase)p;
            }
        }

        public static void EncryptionRespawnSubTarg(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Client.encryptions[trackedID] != null && H3MP_Client.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Client.encryptions[trackedID].subTargsActive[index] = true;

                H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(true);
                Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript, (int)Mod.TNH_EncryptionTarget_m_numSubTargsLeft.GetValue(H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript));
            }
        }

        public static void EncryptionSpawnGrowth(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Client.encryptions[trackedID] != null && H3MP_Client.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Client.encryptions[trackedID].tendrilsActive[index] = true;
                H3MP_Client.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Client.encryptions[trackedID].subTargsPos[index] = point;
                H3MP_Client.encryptions[trackedID].subTargsActive[index] = true;
                H3MP_Client.encryptions[trackedID].tendrilFloats[index] = 1f;
                Vector3 forward = point - H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                H3MP_Client.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                H3MP_Client.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                ++EncryptionSpawnGrowthPatch.skip;
                Mod.TNH_EncryptionTarget_SpawnGrowth.Invoke(H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { index, point });
                --EncryptionSpawnGrowthPatch.skip;
            }
        }

        public static void EncryptionRecursiveInit(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int count = packet.ReadInt();
            List<int> indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                indices.Add(packet.ReadInt());
            }

            if (H3MP_Client.encryptions[trackedID] != null && H3MP_Client.encryptions[trackedID].physicalObject != null)
            {
                for (int i = 0; i < count; ++i)
                {
                    H3MP_Client.encryptions[trackedID].subTargsActive[indices[i]] = true;
                    H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[indices[i]].SetActive(true);
                }
                Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript, count);
            }
        }

        public static void EncryptionResetGrowth(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Client.encryptions[trackedID] != null && H3MP_Client.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Client.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Client.encryptions[trackedID].tendrilFloats[index] = 0;
                Vector3 forward = point - H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                H3MP_Client.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                H3MP_Client.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                ++EncryptionResetGrowthPatch.skip;
                Mod.TNH_EncryptionTarget_ResetGrowth.Invoke(H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { index, point });
                --EncryptionResetGrowthPatch.skip;
            }
        }

        public static void EncryptionDisableSubtarg(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Client.encryptions[trackedID] != null && H3MP_Client.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Client.encryptions[trackedID].subTargsActive[index] = false;

                H3MP_Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(false);
            }
        }

        public static void InitTNHInstances(H3MP_Packet packet)
        {
            int count = packet.ReadInt();

            for(int i=0; i < count; i++)
            {
                int instance = packet.ReadInt();
                H3MP_TNHInstance TNHInstance = new H3MP_TNHInstance(instance);
                TNHInstance.controller = packet.ReadInt();
                int playerIDCount = packet.ReadInt();
                for(int j=0; j < playerIDCount; ++j)
                {
                    TNHInstance.playerIDs.Add(packet.ReadInt());
                }
                int currentlyPlayingCount = packet.ReadInt();
                for(int j=0; j < currentlyPlayingCount; ++j)
                {
                    TNHInstance.currentlyPlaying.Add(packet.ReadInt());
                }
                int playedCount = packet.ReadInt();
                for(int j=0; j < playedCount; ++j)
                {
                    TNHInstance.played.Add(packet.ReadInt());
                }
                int deadCount = packet.ReadInt();
                for(int j=0; j < deadCount; ++j)
                {
                    TNHInstance.dead.Add(packet.ReadInt());
                }
                TNHInstance.tokenCount = packet.ReadInt();
                TNHInstance.holdOngoing = packet.ReadBool();
                TNHInstance.curHoldIndex = packet.ReadInt();
                TNHInstance.level = packet.ReadInt();
                TNHInstance.phase = (TNH_Phase)packet.ReadShort();
                int activeSupplyPointIndicesCount = packet.ReadInt();
                for (int j = 0; j < activeSupplyPointIndicesCount; ++j)
                {
                    TNHInstance.activeSupplyPointIndices.Add(packet.ReadInt());
                }
                int supplyPanelTypesCount = packet.ReadInt();
                for (int j = 0; j < supplyPanelTypesCount; ++j)
                {
                    TNHInstance.supplyPanelTypes.Add((TNH_SupplyPoint.SupplyPanelType)packet.ReadByte());
                }
                int raisedBarriersCount = packet.ReadInt();
                for (int j = 0; j < raisedBarriersCount; ++j)
                {
                    TNHInstance.raisedBarriers.Add(packet.ReadInt());
                }
                int raisedBarrierPrefabIndicesCount = packet.ReadInt();
                for (int j = 0; j < raisedBarrierPrefabIndicesCount; ++j)
                {
                    TNHInstance.raisedBarrierPrefabIndices.Add(packet.ReadInt());
                }
                TNHInstance.letPeopleJoin = packet.ReadBool();
                TNHInstance.progressionTypeSetting = packet.ReadInt();
                TNHInstance.healthModeSetting = packet.ReadInt();
                TNHInstance.equipmentModeSetting = packet.ReadInt();
                TNHInstance.targetModeSetting = packet.ReadInt();
                TNHInstance.AIDifficultyModifier = packet.ReadInt();
                TNHInstance.radarModeModifier = packet.ReadInt();
                TNHInstance.itemSpawnerMode = packet.ReadInt();
                TNHInstance.backpackMode = packet.ReadInt();
                TNHInstance.healthMult = packet.ReadInt();
                TNHInstance.sosiggunShakeReloading = packet.ReadInt();
                TNHInstance.TNHSeed = packet.ReadInt();
                TNHInstance.levelIndex = packet.ReadInt();

                H3MP_GameManager.TNHInstances.Add(instance, TNHInstance);
            }
        }
    }
}
