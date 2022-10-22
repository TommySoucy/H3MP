using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_ServerHandle
    {
        public static void WelcomeReceived(int clientID, H3MP_Packet packet)
        {
            int clientIDCheck = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();

            Debug.Log($"{H3MP_Server.clients[clientID].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {clientID}");

            if (clientID != clientIDCheck)
            {
                Debug.Log($"Player \"{username}\" (ID:{clientID}) has assumed wrong client ID ({clientIDCheck})");
            }

            // Spawn player to clients 
            H3MP_Server.clients[clientID].SendIntoGame(username, scene);
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
            player.leftHandTrackedID = packet.ReadInt();
            player.rightHandPos = packet.ReadVector3();
            player.rightHandRot = packet.ReadQuaternion();
            player.rightHandTrackedID = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                               player.leftHandPos, player.leftHandRot, player.leftHandTrackedID,
                                               player.leftHandPos, player.leftHandRot, player.leftHandTrackedID);
        }

        public static void PlayerScene(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(player.ID, scene);

            // Send to all other clients
            H3MP_ServerSend.PlayerScene(player.ID, scene);
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
            int count = packet.ReadInt();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItems(packet.ReadTrackedItem());
            }
        }

        public static void TakeControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            // Update locally
            if (trackedItem.controller == 0)
            {
                FVRPhysicalObject physObj = trackedItem.physicalObject.GetComponent<FVRPhysicalObject>();
                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items[trackedItem.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localtrackedID].localtrackedID = trackedItem.localtrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
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
            trackedItem.controller = newController;

            if (newController == 0)
            {
                FVRPhysicalObject physObj = trackedItem.physicalObject.GetComponent<FVRPhysicalObject>();
                physObj.RecoverRigidbody();
                trackedItem.localtrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(trackedItem);
            }

            // Send to all other clients
            H3MP_ServerSend.GiveControl(trackedID, newController);
        }

        public static void DestroyItem(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            if (trackedItem.physicalObject == null)
            {
                H3MP_Server.items[trackedID] = null;
                H3MP_Server.availableItemIndices.Add(trackedID);
                if (trackedItem.controller == 0)
                {
                    H3MP_GameManager.items[trackedItem.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[trackedItem.localtrackedID].localtrackedID = trackedItem.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
            else
            {
                trackedItem.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedItem.physicalObject.gameObject);
            }

            H3MP_ServerSend.DestroyItem(trackedID, clientID);
        }

        public static void TrackedItem(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedItem(packet.ReadTrackedItem(true));
        }
    }
}
