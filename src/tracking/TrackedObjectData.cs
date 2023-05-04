using FFmpeg.AutoGen;
using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP.Tracking
{
    public abstract class TrackedObjectData
    {
        public TrackedObject physical;

        // An identifier for which type this data is for
        public string typeIdentifier;

        public bool latestUpdateSent = false; // Whether the latest update of this data was sent
        public byte order; // The index of this object's data packet used to ensure we process this data in the correct order

        public int trackedID = -1; // This object's unique ID to identify it across systems (index in global objects arrays)
        public int localTrackedID = -1; // This object's index in local objects list
        public uint localWaitingIndex = uint.MaxValue; // The unique index this object had while waiting for its tracked ID
        public int initTracker; // The ID of the client who initially tracked this object
        private int _controller = 0; // Client controlling this object, 0 for host
        // TODO: Review: Perhaps do everything about control through this, like set kinematic and so on
        public int controller 
        { 
            get 
            { 
                return _controller;
            } 
            
            set 
            { 
                if (_controller != value)
                {
                    OnControlChanged();
                }

                _controller = value;
            }
        }
        public string scene; // Which scene this object is in
        public int instance; // Which instance this object is in
        public bool sceneInit; // Whether this object is a part of scene initialization
        public bool awaitingInstantiation; // Whether this object's instatiation is already underway
        public int parent = -1; // The tracked ID of object this object is parented to
        public List<TrackedObjectData> children; // The objects parented to this object
        public List<int> childrenToParent = new List<int>(); // The objects to parent to this object once we instantiate it
        public int childIndex = -1; // The index of this object in its parent's children list
        public int ignoreParentChanged;

        // Update data
        public bool active; // Whether the object is active
        private bool previousActive;

        public TrackedObjectData(Packet packet)
        {
            typeIdentifier = packet.ReadString();
            controller = packet.ReadInt();
            parent = packet.ReadInt();
            localTrackedID = packet.ReadInt();
            scene = packet.ReadString();
            instance = packet.ReadInt();
            sceneInit = packet.ReadBool();
            localWaitingIndex = packet.ReadUInt();
            initTracker = packet.ReadInt();

            trackedID = packet.ReadInt();
            active = packet.ReadBool();
        }

        public abstract IEnumerator Instantiate();

        public virtual void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            if (full)
            {
                packet.Write(typeIdentifier);
                packet.Write(controller);
                packet.Write(parent);
                packet.Write(localTrackedID);
                packet.Write(scene);
                packet.Write(instance);
                packet.Write(sceneInit);
                packet.Write(localWaitingIndex);
                packet.Write(initTracker);
            }
            else
            {
                if (incrementOrder)
                {
                    packet.Write(order++);
                }
                else
                {
                    packet.Write(order);
                }
            }

            packet.Write(trackedID);
            packet.Write(active);
        }

        public virtual bool IsIdentifiable()
        {
            return true;
        }

        public virtual void OnControlChanged()
        {
            latestUpdateSent = false;
        }

        public abstract void OnTrackedIDReceived();

        public virtual void OnTracked() { }

        public virtual void RemoveFromLocal()
        {
            if (localTrackedID > -1 && localTrackedID < GameManager.objects.Count)
            {
                // Remove from actual local items list and update the localTrackedID of the item we are moving
                GameManager.objects[localTrackedID] = GameManager.objects[GameManager.items.Count - 1];
                GameManager.objects[localTrackedID].localTrackedID = localTrackedID;
                GameManager.objects.RemoveAt(GameManager.objects.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID out of range!:\n" + Environment.StackTrace);
            }
        }
    }
}
