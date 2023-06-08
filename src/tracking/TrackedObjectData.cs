using H3MP.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

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
                    OnControlChanged(value);
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
        public bool removeFromListOnDestroy = true;

        // Update data
        public bool active; // Whether the object is active
        private bool previousActive;

        public TrackedObjectData()
        {

        }

        public TrackedObjectData(Packet packet, string typeID, int trackedID)
        {
            // Full
            typeIdentifier = typeID;
            controller = packet.ReadInt();
            parent = packet.ReadInt();
            localTrackedID = packet.ReadInt();
            scene = packet.ReadString();
            instance = packet.ReadInt();
            sceneInit = packet.ReadBool();
            localWaitingIndex = packet.ReadUInt();
            initTracker = packet.ReadInt();

            // Update
            this.trackedID = trackedID;
            active = packet.ReadBool();
        }

        public abstract IEnumerator Instantiate();

        public virtual void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            if (full)
            {
                packet.Write(trackedID);
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
                packet.Write(trackedID);
            }

            packet.Write(active);
        }

        // Processes an update packet
        public static void Update(Packet packet, bool includesLength = true, bool full = false)
        {
            ushort length =  includesLength ? packet.ReadUShort() : (ushort)0;
            byte order = packet.ReadByte();
            int trackedID = packet.ReadInt();

            if (trackedID == -1)
            {
                if (includesLength)
                {
                    packet.readPos += (length - 5); // -5 because we read a byte and an int above
                }
                return;
            }

            TrackedObjectData trackedObjectData = null;
            if (ThreadManager.host)
            {
                if(trackedID >= Server.objects.Length)
                {
                    Mod.LogError("Server got update for object at "+trackedID+" which is out of range of objects array");
                    return;
                }
                trackedObjectData = Server.objects[trackedID];
            }
            else
            {
                if (trackedID >= Client.objects.Length)
                {
                    Mod.LogError("Client got update for object at " + trackedID + " which is out of range of objects array");
                    return;
                }
                trackedObjectData = Client.objects[trackedID];
            }

            // TODO: Review: Should we keep the up to date data for later if we dont have the tracked object yet?
            //               Concern is that if we send tracked item TCP packet, but before that arrives, we send the latest update
            //               meaning we don't have the object for that yet and so when we receive the object itself, we don't have the most up to date
            //               We could keep only the highest order in a dict by trackedID
            if (trackedObjectData != null)
            {
                // If we take control of an object, we could still receive an updated object from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this object
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedObjectData.controller != GameManager.ID && (full || (order > trackedObjectData.order || trackedObjectData.order - order > 128)))
                {
                    trackedObjectData.UpdateFromPacket(packet, full);
                    return;
                }
            }

            if (includesLength)
            {
                // If we make it here, it is because we didn't update from packet
                packet.readPos += (length - 5); // -5 because we read a byte and an int above
            }
        }

        // Updates the object using given data
        public virtual void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            if (full)
            {
                //typeIdentifier = updatedObject.typeIdentifier;
                controller = updatedObject.controller;
                parent = updatedObject.parent;
                //localTrackedID = updatedObject.localTrackedID;
                scene = updatedObject.scene;
                instance = updatedObject.instance;
                //sceneInit = updatedObject.sceneInit;
                localWaitingIndex = updatedObject.localWaitingIndex;
                //initTracker = updatedObject.initTracker;
            }

            order = updatedObject.order;
            if (physical != null)
            {
                previousActive = active;
                active = updatedObject.active;
                if (active)
                {
                    if (!physical.gameObject.activeSelf)
                    {
                        physical.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physical.gameObject.activeSelf)
                    {
                        physical.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Updates the object using given update packet
        public virtual void UpdateFromPacket(Packet packet, bool full = false)
        {
            if (full)
            {
                // NOTE: Some of these are commented out because we don't want to replace these values
                //       They are the ones that should never change
                //       In some cases, like localTrackedID for example, the receiver sets the value to -1 on their side if they are not in control of the object
                //       If we do a full update, we would overwrite the local value, which we don't want to do
                //       These have been left here for documentation
                /*typeIdentifier =*/ packet.ReadString();
                controller = packet.ReadInt();
                parent = packet.ReadInt();
                /*localTrackedID = packet.ReadInt();*/ packet.readPos += 4;
                scene = packet.ReadString();
                instance = packet.ReadInt();
                /*sceneInit = packet.ReadBool();*/ ++packet.readPos;
                localWaitingIndex = packet.ReadUInt();
                /*initTracker = packet.ReadInt();*/ packet.readPos += 4;
            }

            previousActive = active;
            active = packet.ReadBool();

            if (physical != null)
            {
                if (active)
                {
                    if (!physical.gameObject.activeSelf)
                    {
                        physical.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physical.gameObject.activeSelf)
                    {
                        physical.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Objects updates its data based on its state
        public virtual bool Update(bool full = false)
        {
            // Phys could be null if we were given control of the object while we were loading and we haven't instantiated it on our side yet
            if (physical == null)
            {
                return false;
            }

            previousActive = active;
            active = physical.gameObject.activeInHierarchy;

            return previousActive != active;
        }


        // Returns true if the object's new state is different than previous requiring for an update to be sent from server
        public virtual bool NeedsUpdate()
        {
            return previousActive != active;
        }

        public virtual bool IsIdentifiable()
        {
            return true;
        }

        public virtual bool IsControlled()
        {
            return false;
        }

        public virtual void OnControlChanged(int newController)
        {
            latestUpdateSent = false;
        }

        public virtual void OnTrackedIDReceived(TrackedObjectData newData)
        {
            if (TrackedObject.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                ClientSend.DestroyObject(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                Client.objects[trackedID] = null;

                // Remove from objectsByInstanceByScene
                GameManager.objectsByInstanceByScene[scene][instance].Remove(trackedID);

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && TrackedObject.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = TrackedObject.unknownControlTrackedIDs[localWaitingIndex];

                ClientSend.GiveObjectControl(trackedID, newController, null);

                // Also change controller locally
                SetController(newController, true);

                TrackedObject.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && TrackedObject.unknownTrackedIDs.ContainsKey(localWaitingIndex))
            {
                KeyValuePair<uint, bool> parentPair = TrackedObject.unknownTrackedIDs[localWaitingIndex];
                if (parentPair.Value)
                {
                    TrackedObjectData parentObjectData = null;
                    if (ThreadManager.host)
                    {
                        parentObjectData = Server.objects[parentPair.Key];
                    }
                    else
                    {
                        parentObjectData = Client.objects[parentPair.Key];
                    }
                    if (parentObjectData != null)
                    {
                        if (parentObjectData.trackedID != parent)
                        {
                            // We have a parent trackedItem and it is new
                            // Update other clients
                            if (ThreadManager.host)
                            {
                                ServerSend.ObjectParent(trackedID, parentObjectData.trackedID);
                            }
                            else
                            {
                                ClientSend.ObjectParent(trackedID, parentObjectData.trackedID);
                            }

                            // Update local
                            SetParent(parentObjectData, false);
                        }
                    }
                }
                else
                {
                    if (parentPair.Key == uint.MaxValue)
                    {
                        // We were detached from current parent
                        // Update other clients
                        if (ThreadManager.host)
                        {
                            ServerSend.ObjectParent(trackedID, -1);
                        }
                        else
                        {
                            ClientSend.ObjectParent(trackedID, -1);
                        }

                        // Update locally
                        SetParent(null, false);
                    }
                    else // We received our tracked ID but not our parent's
                    {
                        if (TrackedObject.unknownParentTrackedIDs.ContainsKey(parentPair.Key))
                        {
                            TrackedObject.unknownParentTrackedIDs[parentPair.Key].Add(trackedID);
                        }
                        else
                        {
                            TrackedObject.unknownParentTrackedIDs.Add(parentPair.Key, new List<int>() { trackedID });
                        }
                    }
                }

                if (!parentPair.Value && TrackedObject.unknownParentWaitList.TryGetValue(parentPair.Key, out List<uint> waitlist))
                {
                    waitlist.Remove(localWaitingIndex);
                }
                TrackedObject.unknownTrackedIDs.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedObject.unknownParentWaitList.ContainsKey(localWaitingIndex))
            {
                List<uint> waitlist = TrackedObject.unknownParentWaitList[localWaitingIndex];
                foreach (uint childID in waitlist)
                {
                    if (TrackedObject.unknownTrackedIDs.TryGetValue(childID, out KeyValuePair<uint, bool> childEntry))
                    {
                        TrackedObject.unknownTrackedIDs[childID] = new KeyValuePair<uint, bool>((uint)trackedID, true);
                    }
                }
                TrackedObject.unknownParentWaitList.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedObject.unknownParentTrackedIDs.ContainsKey(localWaitingIndex))
            {
                List<int> childrenList = TrackedObject.unknownParentTrackedIDs[localWaitingIndex];
                TrackedObjectData[] arrToUse = null;
                if (ThreadManager.host)
                {
                    arrToUse = Server.objects;
                }
                else
                {
                    arrToUse = Client.objects;
                }
                foreach (int childID in childrenList)
                {
                    if (arrToUse[childID] != null)
                    {
                        // Update other clients
                        if (ThreadManager.host)
                        {
                            ServerSend.ObjectParent(arrToUse[childID].trackedID, trackedID);
                        }
                        else
                        {
                            ClientSend.ObjectParent(arrToUse[childID].trackedID, trackedID);
                        }

                        // Update local
                        arrToUse[childID].SetParent(this, false);
                    }
                }
                TrackedObject.unknownParentTrackedIDs.Remove(localWaitingIndex);
            }
        }

        public virtual void OnTracked() { }

        public virtual void RemoveFromLocal()
        {
            if (trackedID == -1)
            {
                if (TrackedItem.unknownTrackedIDs.TryGetValue(localWaitingIndex, out KeyValuePair<uint, bool> entry))
                {
                    if (!entry.Value && TrackedItem.unknownParentWaitList.TryGetValue(entry.Key, out List<uint> waitlist))
                    {
                        waitlist.Remove(localWaitingIndex);
                    }
                }
                TrackedItem.unknownTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownParentTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownControlTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownDestroyTrackedIDs.Remove(localWaitingIndex);

                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if(physical != null && physical.physical != null)
                {
                    GameManager.trackedObjectByObject.Remove(physical.physical);
                }
            }

            if (localTrackedID > -1 && localTrackedID < GameManager.objects.Count)
            {
                // Remove from actual local objects list and update the localTrackedID of the item we are moving
                GameManager.objects[localTrackedID] = GameManager.objects[GameManager.objects.Count - 1];
                GameManager.objects[localTrackedID].localTrackedID = localTrackedID;
                GameManager.objects.RemoveAt(GameManager.objects.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID "+localTrackedID+" for object at "+trackedID+" out of range!:\n" + Environment.StackTrace);
            }
        }

        public void SetController(int newController, bool recursive = false)
        {
            if (recursive)
            {
                SetControllerRecursive(this, newController);
            }
            else
            {
                controller = newController;
            }
        }

        private void SetControllerRecursive(TrackedObjectData otherTrackedObject, int newController)
        {
            otherTrackedObject.controller = newController;

            if (otherTrackedObject.children != null)
            {
                foreach (TrackedObjectData child in otherTrackedObject.children)
                {
                    SetControllerRecursive(child, newController);
                }
            }
        }

        public void TakeControlRecursive(TrackedObjectData trackedObject = null)
        {
            TrackedObjectData currentTrackedObject = trackedObject == null ? this : trackedObject;

            // Note: we can return right away if we don't have tracked ID because not tracked ID implies taht this item is already under our control
            // So TakeControlRecursive should never be called without tracked ID in the first place
            if (currentTrackedObject.trackedID < 0)
            {
                return;
            }

            if (ThreadManager.host)
            {
                ServerSend.GiveObjectControl(currentTrackedObject.trackedID, GameManager.ID, null);
            }
            else
            {
                ClientSend.GiveObjectControl(currentTrackedObject.trackedID, GameManager.ID, null);
            }

            currentTrackedObject.SetController(GameManager.ID);
            if (currentTrackedObject.localTrackedID == -1)
            {
                currentTrackedObject.localTrackedID = GameManager.objects.Count;
                GameManager.objects.Add(currentTrackedObject);
            }

            if (currentTrackedObject.children != null)
            {
                foreach (TrackedItemData child in currentTrackedObject.children)
                {
                    TakeControlRecursive(child);
                }
            }
        }

        public void SetParent(int trackedID)
        {
            if (trackedID == -1)
            {
                SetParent(null, true);
            }
            else
            {
                if (ThreadManager.host)
                {
                    SetParent(Server.objects[trackedID], true);
                }
                else
                {
                    SetParent(Client.objects[trackedID], true);
                }
            }
        }

        public void SetParent(TrackedObjectData newParent, bool physicallyParent)
        {
            if (newParent == null)
            {
                if (parent != -1) // We had parent before, need to unparent
                {
                    TrackedObjectData previousParent = null;
                    int clientID = -1;
                    if (ThreadManager.host)
                    {
                        previousParent = Server.objects[parent];
                        clientID = 0;
                    }
                    else
                    {
                        previousParent = Client.objects[parent];
                        clientID = GameManager.ID;
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                    parent = -1;
                    childIndex = -1;

                    // Physically unparent if necessary
                    if (physicallyParent && physical != null)
                    {
                        ++ignoreParentChanged;
                        physical.transform.parent = null;
                        --ignoreParentChanged;

                        // If in control, we want to enable rigidbody
                        if (controller == clientID)
                        {
                            Mod.SetKinematicRecursive(physical.transform, false);
                        }

                        ParentChanged();
                    }
                }
                // Already unparented, nothing changes
            }
            else // We have new parent
            {
                if (parent != -1) // We had parent before, need to unparent first
                {
                    if (newParent.trackedID == parent)
                    {
                        // Already attached to correct parent
                        return;
                    }

                    TrackedObjectData previousParent = null;
                    if (ThreadManager.host)
                    {
                        previousParent = Server.objects[parent];
                    }
                    else
                    {
                        previousParent = Client.objects[parent];
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                }

                // Set new parent
                parent = newParent.trackedID;
                if (newParent.children == null)
                {
                    newParent.children = new List<TrackedObjectData>();
                }
                childIndex = newParent.children.Count;
                newParent.children.Add(this);

                // Physically parent
                if (physicallyParent && physical != null)
                {
                    if (newParent.physical == null)
                    {
                        newParent.childrenToParent.Add(trackedID);
                    }
                    else
                    {
                        ++ignoreParentChanged;
                        physical.transform.parent = newParent.physical.transform;
                        --ignoreParentChanged;

                        ParentChanged();
                    }
                }

                int preController = controller;

                // Set Controller to parent's
                SetController(newParent.controller, true);

                // If newly in control, we want to enable rigidbody and add to list
                if (controller == GameManager.ID)
                {
                    if (preController != controller)
                    {
                        localTrackedID = GameManager.objects.Count;
                        GameManager.objects.Add(this);
                    }
                }
            }
        }

        public virtual void ParentChanged() { }

        public virtual void RemoveFromLists()
        {
            if (ThreadManager.host)
            {
                Server.objects[trackedID] = null;
                Server.availableObjectIndices.Add(trackedID);
            }
            else
            {
                Client.objects[trackedID] = null;
            }

            GameManager.objectsByInstanceByScene[scene][instance].Remove(trackedID);
        }
    }
}
