using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static H3MP.H3MP_PlayerHitbox;
using static Valve.VR.SteamVR_ExternalCamera;

namespace H3MP
{
    internal class H3MP_ServerHandle
    {
        public static void WelcomeReceived(int clientID, H3MP_Packet packet)
        {
            int clientIDCheck = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();

            Debug.Log($"{H3MP_Server.clients[clientID].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {clientID}");

            if (clientID != clientIDCheck)
            {
                Debug.Log($"Player \"{username}\" (ID:{clientID}) has assumed wrong client ID ({clientIDCheck})");
            }

            // Spawn player to clients 
            H3MP_Server.clients[clientID].SendIntoGame(username, scene, instance);
        }

        public static void PlayerState(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            player.position = packet.ReadVector3();
            player.rotation = packet.ReadQuaternion();
            player.headPos = packet.ReadVector3();
            player.headRot = packet.ReadQuaternion();
            player.torsoPos = packet.ReadVector3();
            player.torsoRot = packet.ReadQuaternion();
            player.leftHandPos = packet.ReadVector3();
            player.leftHandRot = packet.ReadQuaternion();
            player.rightHandPos = packet.ReadVector3();
            player.rightHandRot = packet.ReadQuaternion();
            player.health = packet.ReadFloat();
            player.maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if(additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            H3MP_GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                               player.leftHandPos, player.leftHandRot,
                                               player.leftHandPos, player.leftHandRot,
                                               player.health, player.maxHealth, additionalData);
        }

        public static void PlayerScene(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(player.ID, scene);

            // Send to all other clients
            H3MP_ServerSend.PlayerScene(player.ID, scene);

            // Send the client all items it needs to instantiate from the scene
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(scene))
            {
                Debug.Log("Player "+clientID+" joined scene "+ scene);
                H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
            }
            Debug.Log("Synced with player who just joined scene");
        }

        public static void PlayerInstance(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            int instance = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerInstance(player.ID, instance);

            // Send to all other clients
            H3MP_ServerSend.PlayerInstance(player.ID, instance);

            // Send the client all items it needs to instantiate from the scene/instance
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(player.scene))
            {
                Debug.Log("Player "+clientID+" joined instance "+ instance);
                H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
            }
            Debug.Log("Synced with player who just joined instance");
        }

        public static void AddTNHInstance(int clientID, H3MP_Packet packet)
        {
            int hostID = packet.ReadInt();
            bool letPeopleJoin = packet.ReadBool();

            // Send to all clients
            H3MP_ServerSend.AddTNHInstance(H3MP_GameManager.AddNewTNHInstance(hostID, letPeopleJoin));
        }

        public static void AddSyncScene(int clientID, H3MP_Packet packet)
        {
            string scene = packet.ReadString();

            H3MP_GameManager.synchronizedScenes.Add(scene, clientID);

            // Send to all other clients
            H3MP_ServerSend.AddSyncScene(clientID, scene);
        }

        public static void TrackedItems(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void TrackedSosigs(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TakeControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            // Update locally
            if (trackedItem.controller == 0)
            {
                FVRPhysicalObject physObj = trackedItem.physicalItem.GetComponent<FVRPhysicalObject>();
                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            trackedItem.controller = clientID;

            // Send to all other clients
            H3MP_ServerSend.GiveControl(trackedID, clientID);
        }

        public static void GiveControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            if (trackedItem.controller != 0 && newController == 0)
            {
                // Only want to active rigidbody if not parented to another tracked item
                if (trackedItem.parent == -1)
                {
                    FVRPhysicalObject physObj = trackedItem.physicalItem.GetComponent<FVRPhysicalObject>();
                    physObj.RecoverRigidbody();
                }
                trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(trackedItem);
            }
            else if(trackedItem.controller == 0 && newController != 0)
            {
                FVRPhysicalObject physObj = trackedItem.physicalItem.GetComponent<FVRPhysicalObject>();
                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            trackedItem.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveControl(trackedID, newController);
        }

        public static void GiveSosigControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

            if (trackedSosig.controller != 0 && newController == 0)
            {
                trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                if(trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                    trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                }
                H3MP_GameManager.sosigs.Add(trackedSosig);
            }
            else if(trackedSosig.controller == 0 && newController != 0)
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
            trackedSosig.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveSosigControl(trackedID, newController);
        }

        public static void DestroySosig(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

            if (trackedSosig.physicalObject != null)
            {
                H3MP_GameManager.trackedSosigBySosig.Remove(trackedSosig.physicalObject.physicalSosigScript);
                trackedSosig.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedSosig.physicalObject.gameObject);
            }

            if (trackedSosig.localTrackedID != -1)
            {
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
            }

            if (removeFromList)
            {
                H3MP_Server.sosigs[trackedID] = null;
                H3MP_Server.availableSosigIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroySosig(trackedID, removeFromList, clientID);
        }

        public static void DestroyItem(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];
            Debug.Log("Received destroy order for " + trackedItem.itemID);

            if (trackedItem.physicalItem != null)
            {
                trackedItem.physicalItem.sendDestroy = false;
                GameObject.Destroy(trackedItem.physicalItem.gameObject);
            }

            if (trackedItem.localTrackedID != -1)
            {
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
            }

            if (removeFromList)
            {
                H3MP_Server.items[trackedID] = null;
                H3MP_Server.availableItemIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroyItem(trackedID, removeFromList, clientID);
        }

        public static void TrackedItem(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedItem(packet.ReadTrackedItem(true), packet.ReadString(), clientID);
        }

        public static void TrackedSosig(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedSosig(packet.ReadTrackedSosig(true), packet.ReadString(), clientID);
        }

        public static void ItemParent(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            H3MP_Server.items[trackedID].SetParent(newParentID);

            // Send to all other clients
            H3MP_ServerSend.ItemParent(trackedID, newParentID, clientID);
        }

        public static void WeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Server.items[trackedID].physicalItem.fireFunc();
            }

            // Send to other clients
            H3MP_ServerSend.WeaponFire(clientID, trackedID);
        }

        public static void PlayerDamage(int clientID, H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            H3MP_PlayerHitbox.Part part = (H3MP_PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            if (ID == 0)
            {
                H3MP_GameManager.ProcessPlayerDamage(part, damage);
            }
            else
            {
                H3MP_ServerSend.PlayerDamage(ID, (byte)part, damage);
            }
        }

        public static void SosigPickUpItem(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
            }

            H3MP_ServerSend.SosigPickUpItem(sosigTrackedID, itemTrackedID, primaryHand, clientID);
        }

        public static void SosigPlaceItemIn(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].PlaceObjectIn(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
            }

            H3MP_ServerSend.SosigPlaceItemIn(sosigTrackedID, slotIndex, itemTrackedID, clientID);
        }

        public static void SosigDropSlot(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].DetachHeldObject();
            }

            H3MP_ServerSend.SosigDropSlot(sosigTrackedID, slotIndex, clientID);
        }

        public static void SosigHandDrop(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
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

            H3MP_ServerSend.SosigHandDrop(sosigTrackedID, primaryHand, clientID);
        }

        public static void SosigConfigure(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Debug.Log("server handle sosig configure got called from client: " + clientID + " for sosig tracked ID: " + sosigTrackedID);
            SosigConfigTemplate config = packet.ReadSosigConfig();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Debug.Log("\tFound trackedSosig, and it has physical, configuring ");
                trackedSosig.configTemplate = config;
                SosigConfigurePatch.skipConfigure = true;
                trackedSosig.physicalObject.physicalSosigScript.Configure(config);
            }

            H3MP_ServerSend.SosigConfigure(sosigTrackedID, config, clientID);
        }

        public static void SosigLinkRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if (trackedSosig.physicalObject != null)
                    {
                        foreach (SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while (trackedSosig.wearables.Count <= linkIndex)
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
            else // We could receive a register wearable packet before the sosig is instantiated
            {
                // Keep the wearables in a dictionary that we will query once we instantiate the sosig
                if (H3MP_GameManager.waitingWearables.ContainsKey(sosigTrackedID))
                {
                    if (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                    {
                        while (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                        {
                            H3MP_GameManager.waitingWearables[sosigTrackedID].Add(new List<string>());
                        }
                    }
                    H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Add(wearableID);
                }
                else
                {
                    H3MP_GameManager.waitingWearables.Add(sosigTrackedID, new List<List<string>>());
                    while (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                    {
                        H3MP_GameManager.waitingWearables[sosigTrackedID].Add(new List<string>());
                    }
                    H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Add(wearableID);
                }
            }

            H3MP_ServerSend.SosigLinkRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigLinkDeRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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
            else if (H3MP_GameManager.waitingWearables.ContainsKey(sosigTrackedID)
                    && H3MP_GameManager.waitingWearables[sosigTrackedID].Count > linkIndex)
            {
                H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Remove(wearableID);
            }

            H3MP_ServerSend.SosigLinkDeRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigSetIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigSetIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigSetOriginalIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigSetOriginalIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigLinkDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].Damage(damage);
                        --SosigLinkDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigLinkDamage(trackedSosig, linkIndex, damage);
                }
            }
        }

        public static void SosigWearableDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigWearableDamagePatch.skip;
                        (Mod.SosigLink_m_wearables.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigWearableDamage(trackedSosig, linkIndex, wearableIndex, damage);
                }
            }
        }

        public static void SosigDamageData(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller != 0 && trackedSosig.physicalObject != null)
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

            packet.readPos = 0;
            H3MP_ServerSend.SosigLinkDamageData(packet);
        }

        public static void SosigLinkExplodes(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }

            H3MP_ServerSend.SosigLinkExplodes(packet, clientID);
        }

        public static void SosigDies(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }

            H3MP_ServerSend.SosigDies(packet, clientID);
        }

        public static void SosigClear(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosigScript.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
                }
            }

            H3MP_ServerSend.SosigClear(sosigTrackedID, clientID);
        }

        public static void SosigSetBodyState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigSetBodyStateSkip;
                    Mod.Sosig_SetBodyState.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { bodyState });
                    --SosigActionPatch.sosigSetBodyStateSkip;
                }
            }

            H3MP_ServerSend.SosigSetBodyState(sosigTrackedID, bodyState, clientID);
        }

        public static void PlaySosigFootStepSound(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (H3MP_Server.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = H3MP_Server.sosigs[sosigTrackedID].physicalObject.physicalSosigScript.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }

            H3MP_ServerSend.PlaySosigFootStepSound(packet, clientID);
        }

        public static void SosigSpeakState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigSpeakState(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigSetCurrentOrder(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigSetCurrentOrderSkip;
                trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                --SosigActionPatch.sosigSetCurrentOrderSkip;
            }

            H3MP_ServerSend.SosigSetCurrentOrder(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigVaporize(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosigScript.Vaporize(trackedSosig.physicalObject.physicalSosigScript.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }

            H3MP_ServerSend.SosigVaporize(sosigTrackedID, iff, clientID);
        }

        public static void SosigRequestHitDecal(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigRequestHitDecal(packet, clientID);
        }

        public static void SosigLinkBreak(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }

            H3MP_ServerSend.SosigLinkBreak(sosigTrackedID, linkIndex, isStart, damClass, clientID);
        }

        public static void SosigLinkSever(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                Mod.SosigLink_SeverJoint.Invoke(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex], new object[] { damClass, isPullApart });
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }

            H3MP_ServerSend.SosigLinkSever(sosigTrackedID, linkIndex, damClass, isPullApart, clientID);
        }

        public static void UpToDateItems(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date items packet");
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedItemData trackedItem = packet.ReadTrackedItem(true);
                Debug.Log("\tItem: " +trackedItem.trackedID+", updating");
                H3MP_GameManager.UpdateTrackedItem(trackedItem, true);
                Debug.Log("\tInstantiating");
                AnvilManager.Run(H3MP_Server.items[trackedItem.trackedID].Instantiate());
            }
        }

        public static void UpToDateSosigs(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date sosigs packet");
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedSosigData trackedSosig = packet.ReadTrackedSosig(true);
                Debug.Log("\tSosig: " + trackedSosig.trackedID + ", updating");
                H3MP_GameManager.UpdateTrackedSosig(trackedSosig, true);
                Debug.Log("\tInstantiating");
                AnvilManager.Run(H3MP_Server.sosigs[trackedSosig.trackedID].Instantiate());
            }
        }

        public static void AddTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances == null || !H3MP_GameManager.TNHInstances.ContainsKey(instance))
            {
                Debug.LogError("H3MP_ServerHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                ++H3MP_GameManager.TNHInstances[instance].currentlyPlaying;

                H3MP_ServerSend.AddTNHCurrentlyPlaying(instance, clientID);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances == null || !H3MP_GameManager.TNHInstances.ContainsKey(instance))
            {
                Debug.LogError("H3MP_ServerHandle: Received RemoveTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                --H3MP_GameManager.TNHInstances[instance].currentlyPlaying;

                H3MP_ServerSend.RemoveTNHCurrentlyPlaying(instance, clientID);
            }
        }
    }
}
