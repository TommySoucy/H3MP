using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
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
            H3MP_ClientSend.WelcomeReceived();

            H3MP_Client.singleton.udp.Connect(((IPEndPoint)H3MP_Client.singleton.tcp.socket.Client.LocalEndPoint).Port);
        }

        public static void SpawnPlayer(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            H3MP_GameManager.singleton.SpawnPlayer(ID, username, scene, position, rotation);
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
            int leftHandTrackedID = packet.ReadInt();
            Vector3 rightHandPos = packet.ReadVector3();
            Quaternion rightHandRot = packet.ReadQuaternion();
            int rightHandTrackedID = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerState(ID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                               leftHandPos, leftHandRot, leftHandTrackedID,
                                               rightHandPos, rightHandRot, rightHandTrackedID);
        }

        public static void PlayerScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(ID, scene);
        }

        public static void TrackedItems(H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadInt();
            List<H3MP_TrackedItemData> trackedItems = new List<H3MP_TrackedItemData>();
            for (int i = 0; i < count; ++i)
            {
                trackedItems.Add(packet.ReadTrackedItem());
            }
            H3MP_GameManager.UpdateTrackedItems(trackedItems);
        }

        public static void TrackedItem(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedItem(packet.ReadTrackedItem());
        }

        public static void AddSyncScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.synchronizedScenes.Add(scene, ID);
        }

        public static void TakeControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_GameManager.items.ContainsKey(trackedID))
            {
                H3MP_TrackedItemData trackedItem = H3MP_GameManager.items[trackedID];
                FVRPhysicalObject physObj = trackedItem.physicalObject.GetComponent<FVRPhysicalObject>();
                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items.Remove(trackedID);

                trackedItem.controller = 0;
            }
            else
            {
                H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];  
                trackedItem.controller = 0;
            }
        }

        public static void GiveControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if (trackedItem.controller == H3MP_Client.singleton.ID)
            {
                FVRPhysicalObject physObj = trackedItem.physicalObject.GetComponent<FVRPhysicalObject>();
                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items.Remove(trackedID);
            }
            trackedItem.controller = controllerID;
        }

        public static void DestroyItem(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if(trackedItem.physicalObject == null)
            {
                H3MP_Client.items.Remove(trackedID);
                if (trackedItem.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items.Remove(trackedID);
                }
            }
            else
            {
                trackedItem.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedItem.physicalObject.gameObject);
            }
        }
    }
}
