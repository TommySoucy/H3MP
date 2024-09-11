using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace H3MP.Tracking
{
    /// <summary>
    /// An oversimplified tracked type data class. 
    /// This contains minimum requirements and suggestions for making your own implementation of a tracked type.
    /// </summary>
    public class TrackedTemplateData : TrackedObjectData
    {
        /// <summary>
        /// I suggest keeping your own reference to the physical script for direct access
        /// so you don't have to cast from TrackedObjectData.physical everytime you want to access it. This is completely optional though.
        /// </summary>
        public TrackedTemplate physicalTemplate;

        /// <summary>
        /// An empty constructor should be added to make an empty instance when you track an object locally.
        /// Refer to MakeTracked() where this should be used.
        /// </summary>
        public TrackedTemplateData()
        {

        }

        /// <summary>
        /// A constructor that takes in a packet is required.
        /// Note that is must also call base(packet).
        /// This is what H3MP will use to instantiate the object data received from another client.
        /// </summary>
        /// <param name="packet">The packet containing all the data required to instantiate an tracked object of this type</param>
        public TrackedTemplateData(Packet packet, string typeID) : base(packet, typeID)
        {
            /// Here, you should read any data you had written to full packet and set corresponding variables.
            /// Refer to other existing tracked types for specific examples.
        }

        /// <summary>
        /// This method is required.
        /// Apart from the access modifier, which can be public or non-public, the rest of the signature must be exactly as it is here.
        /// This method will be used by H3MP to know if the object corresponding to the given transform is of this tracked type.
        /// </summary>
        /// <param name="t">The transform we want to know if corresponding object corresponds to this tracked type</param>
        /// <returns>True if object corresponding to given transform corresponds to this tracked type</returns>
        public static bool IsOfType(Transform t)
        {
            /// Refer to other existing tracked types for specific examples.
            return false;
        }

        /// <summary>
        /// This method is optional.
        /// If multiple types returned true for IsOfType, H3MP must choose 1 to track the object with
        /// For example, if a type inherits from TrackedItem, it should override TrackedItem because it obviously wants to be chosen
        /// as the tracked type instead, but both will have returned true on IsOfType
        /// </summary>
        /// <returns>Array of types that should be ignored</returns>
        public static Type[] GetTypeOverrides()
        {
            return null;
        }

        /// <summary>
        /// Optional method
        /// This will be called if the object is not being tracked on our side
        /// Can be used to decide whether you want the object to subsequently be destroyed or not
        /// Useful if the tracked object is a scene object and can't necessarily be instantiated
        /// See TrackedGatlngGunData's implementation for example
        /// </summary>
        /// <param name="t">The transform of the object</param>
        /// <returns>True if want to destroy object</returns>
        public static bool TrackSkipped(Transform t)
        {
            /// Refer to other existing tracked types for specific examples.
            return false;
        }

        /// <summary>
        /// This overrides TrackedObjectData.IsIdentifiable()
        /// Note: This is not absolutely required but if missing TrackedObjectData.IsIdentifiable() will always return true.
        /// This method will be used by H3MP to check if an object's data is identifiable locally.
        /// This is because an object tracked on one client may not be installed on another (Item mods for example).
        /// This means that on the other client, IsIdentifiable should return false, meaning it cannot possibly instantiate this object.
        /// This will prevent H3MP from attempting to instantiate the item.
        /// </summary>
        /// <returns>True if this tracked object instance is identifiable (can be instantiated) locally</returns>
        public override bool IsIdentifiable()
        {
            /// Refer to other existing tracked types for specific examples.
            return false;
        }

        /// <summary>
        /// This method is required.
        /// H3MP will use this method to track an object corresponding to given transform.
        /// In the example here I left TrackedItem code to help explain the method's requirement in greater detail.
        /// Some of the method definition MUST be included. See definition to in depth explanations.
        /// </summary>
        /// <param name="root">The root transform of the object we want to track</param>
        /// <param name="parent">The data of the tracked object parent corresponding to the object if applicable</param>
        /// <returns>The physical script of the tracked type</returns>
        public static TrackedTemplate MakeTracked(Transform root, TrackedObjectData parent)
        {
            /// Physical tracked script must be added to root's gameobject
            TrackedTemplate trackedTemplate = root.gameObject.AddComponent<TrackedTemplate>();

            /// Make empty instance of this data class. This will be the data coresponding to the physical tracked script
            TrackedTemplateData data = new TrackedTemplateData();

            /// Set reference(s) to data instantiated above
            /// Note: .templateData is optional but should be included for convenience and performance. See comment above its definition.
            /// Reference .data MUST be set
            trackedTemplate.data = data;
            trackedTemplate.templateData = data;

            /// Set reference(s) to physical script
            /// Note: .physicalTemplate is optional but should be included for convenience and performance. See comment above its definition.
            /// Reference .physical MUST be set
            data.physicalTemplate = trackedTemplate;
            data.physical = trackedTemplate;

            /// Set reference(s) to original physical script. This the MonoBehaviour of the object you want to have tracked.
            /// Note: .physicalTemplate is optional but should be included for convenience and performance. See comment above its definition.
            /// Reference .physical MUST be set
            data.physicalTemplate.physicalTemplate = root.GetComponent<MonoBehaviour>();
            data.physical.physical = data.physicalTemplate.physicalTemplate;

            /// Set typeIdentifier
            /// This MUST be set
            /// See comment above variable's definition to explanation
            data.typeIdentifier = "TrackedItemData";

            /// Add reference to tracking dictionary
            /// Addition to trackedObjectByObject MUST be done here
            /// H3MP will use it to get faster access to tracking scripts directly from the original physical object itself
            /// Addition to trackedObjectByInteractive is only necessary if applicaple. So if your original physical object is somehow interactive, like an item for example.
            /// Note: trackedTemplateByTemplate is just an example. If you ever need to access the tracked script corresponding 
            ///       to an original physical script, you should keep it somewhere like we do here. See GameManager.trackedItemByItem and
            ///       TrackedItemData.MakeTracked for specific example.
            GameManager.trackedObjectByObject.Add(data.physicalTemplate.physicalTemplate, trackedTemplate);
            GameManager.trackedObjectByInteractive.Add(data.physicalTemplate.physicalTemplate, trackedTemplate);
            GameManager.trackedTemplateByTemplate.Add(data.physicalTemplate.physicalTemplate, trackedTemplate);

            /// Processing the parent is only necessary if your tracked object's parent is ever relevant
            /// In H3MP this is the case for TrackedItem but not for TrackedSosig for example
            /// This is because we don't care what Sosigs are parented to, it shouldn't matter
            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<TrackedObjectData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);
            }

            /// Here you should then set the starting data directly from the original physical script of the object
            /// The following variables MUST be set as these are used directly by H3MP in processing of every TrackedObjectData
            data.active = trackedTemplate.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || GameManager.inPostSceneLoadTrack;

            /// Adding to local objects list MUST be done here
            /// As well as setting the localTrackedID
            /// H3MP will use this to keep track of TrackedObjects controlled locally
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            /// This may not be necessary unless you have a more complex tracking case like TrackedItem where the data depends on
            /// the item type. See TrackedItem update process as an example.
            /// This is left here for documentation. You might want to take a look at TrackedSosig/Encryption/Automeater as well, though
            /// I'm honestly not sure if this initial update is necessary in those other types.
            if (trackedTemplate.awoken)
            {
                trackedTemplate.data.Update(true);
            }

            /// In the end you must return your TrackedObject
            return trackedTemplate;
        }

        /// <summary>
        /// Coroutine used by H3MP to instantiate a TrackedObject received from another client
        /// See comments within definition for necessary implementation details
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        public override IEnumerator Instantiate()
        {
            /// First step would be to get the item prefab
            /// Note: This should be possible if data IsIdentifiable()
            /// As this fetching of the gameobject may take time, as it does in vanilla assets fetches from FVRObject, we yield while waiting
            /// Once we get the prefab we check if we actually got one
            /// If this is not the case, there is a system that H3MP uses to know if the instantiation was cancelled:
            /// When instantiating a tracked object, H3MP will set data.awaitingInstantiation to true
            /// If after the coroutine returns but our prefab is still null, we want to cancel instantiation because we have failed to identify the object defined by the data
            /// We can cancel instantiation by setting the awaitingInstantiation flag to false
            /// See next comment
            GameObject templatePrefab = GetItemPrefab();
            if (IM.OD.TryGetValue("SomePrefabID", out FVRObject obj))
            {
                yield return obj.GetGameObjectAsync();
                templatePrefab = obj.GetGameObject();
            }
            if (templatePrefab == null)
            {
                awaitingInstantiation = false;
                yield break;
            }

            /// If while we were waiting, for any reason, the awaitingInstantiation flag was set to false, we want to cancel the instantiation
            if (!awaitingInstantiation)
            {
                yield break;
            }

            /// You must now make the actual instance
            /// H3MP patches UnityEngine.Instantiate methods to keep track of every instantiation
            /// Here we are instantiating an already tracked object, so we want to prevent these patches from tracking it
            /// We do this by incrementing Mod.skipAllInstantiates before and decrementing it after instantiation
            /// You MUST do this here
            ++Mod.skipAllInstantiates;
            GameObject templateObject = GameObject.Instantiate(templatePrefab, position, rotation);
            --Mod.skipAllInstantiates;

            /// Similarly to what was done in MakeTracked, the references to the data, physical, and original physical scripts MUSt be set here
            physicalTemplate = templateObject.AddComponent<TrackedTemplate>();
            physical = physicalTemplate;
            physicalTemplate.templateData = this;
            physical.data = this;
            physicalTemplate.physicalTemplate = templateObject.GetComponent<MonoBehaviour>();
            physical.physical = physicalTemplate.physicalTemplate;

            /// We then MUST set awaitingInstantiation to false because we are actually done with instantiation by now
            /// Note: This MUST be done after the last yield return, the instantiation must come to a complete end one this is set to false
            awaitingInstantiation = false;

            /// Similarly to MakeTracked, we then need to add our tracked scripts to the tracking dictionaries
            if (GameManager.trackedObjectByObject.TryGetValue(physicalItem.physicalItem, out TrackedObject to))
            {
                Mod.LogError("Error at instantiation of template: Template's physical object already exists in trackedObjectByObject\n\tTrackedID: " + to.data.trackedID);
            }
            else
            {
                GameManager.trackedObjectByObject.Add(physicalItem.physicalItem, physicalItem);
            }
            GameManager.trackedObjectByInteractive.Add(physicalItem.physicalItem, physicalItem);

            if (GameManager.trackedTemplateByTemplate.TryGetValue(physicalTemplate.physicalTemplate, out TrackedTemplate t))
            {
                Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical item object already exists in trackedItemByItem\n\tTrackedID: " + t.data.trackedID);
            }
            else
            {
                GameManager.trackedItemByItem.Add(physicalItem.physicalItem, physicalItem);
            }

            /// Similarly to MakeTracked, you only need to include the parent processing here if parent is relevant to your tracked type
            if (parent != -1)
            {
                // Add ourselves to the parent's children
                TrackedObjectData parentObject = (ThreadManager.host ? Server.objects : Client.objects)[parent];

                if (parentObject.physical == null)
                {
                    parentObject.childrenToParent.Add(trackedID);
                }
                else
                {
                    // Physically parent
                    ++ignoreParentChanged;
                    templateObject.transform.parent = parentObject.physical.transform;
                    --ignoreParentChanged;
                }
            }

            /// Process control
            /// If anything special must be done when you do/don't control an object, so it here
            /// This example comes from TrackedItem where we want to set the RigidBody of the item to kinematic if we are not in control
            if (controller != GameManager.ID)
            {
                //Mod.SetKinematicRecursive(physical.transform, true);
            }

            /// You MUST do an initial update here so the physical can set itself corresponding to data
            UpdateFromData(this);

            /// Process childrenToParent list, see comment above the variable's definition for explanation
            /// This MUST be done no matter what, since other tracked types may be parented to this once even if this one doesn't care about its own parent
            for (int i = 0; i < childrenToParent.Count; ++i)
            {
                TrackedObjectData childObject = (ThreadManager.host ? Server.objects : Client.objects)[childrenToParent[i]];
                if (childObject != null && childObject.parent == trackedID && childObject.physical != null)
                {
                    // Physically parent
                    ++childObject.ignoreParentChanged;
                    childObject.physical.transform.parent = physical.transform;
                    --childObject.ignoreParentChanged;

                    // Call update on child in case it needs to process its new parent somehow
                    // This is needed for attachments that did their latest update before we got their parent's phys
                    // Calling this update will let them mount themselves to their mount properly
                    childObject.UpdateFromData(childObject);
                }
            }
            childrenToParent.Clear();
        }

        /// <summary>
        /// Updates this tracked object using data from given data instance
        /// Used mainly to do an initial update after instantiating the tracked object
        /// See comments within definition to more details
        /// </summary>
        /// <param name="updatedObject">Data instance to update with</param>
        /// <param name="full">Whether this update should also consider full data</param>
        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            /// Call to base MUST be made here
            base.UpdateFromData(updatedObject, full);

            /// Cast to your type so you can access the data
            TrackedTemplateData updatedTemplate = updatedObject as TrackedTemplateData;

            /// Process updated data. The following is 
            /// Process full data
            if (full)
            {
                itemID = updatedItem.itemID;
                identifyingData = updatedItem.identifyingData;
                additionalData = updatedItem.additionalData;
            }

            /// Process update data
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedItem.rotation;
            previousActiveControl = underActiveControl;
            underActiveControl = updatedItem.underActiveControl;

            /// If you have physical tracked object, set its state according to data
            if (physical != null)
            {
                if (!TrackedItem.interpolated)
                {
                    if (parent == -1)
                    {
                        physical.transform.position = updatedItem.position;
                        physical.transform.rotation = updatedItem.rotation;
                    }
                    else
                    {
                        // If parented, the position and rotation are relative, so set it now after parenting
                        physical.transform.localPosition = updatedItem.position;
                        physical.transform.localRotation = updatedItem.rotation;
                    }
                }
            }
        }

        /// <summary>
        /// Updates this tracked object using data from given packet
        /// H3MP uses this to update tracked objects
        /// </summary>
        /// <param name="packet">Packet to update with</param>
        /// <param name="full">Whether this update should also consider full data</param>
        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            /// Call to base MUST be made here
            base.UpdateFromPacket(packet, full);

            /// Update this tracked object's data using data read directly from packet in the same order it was written in WriteToPacket()
        }

        /// <summary>
        /// Updates itself based on physical state
        /// </summary>
        /// <param name="full">Whether to also update full data</param>
        /// <returns>True if something changed</returns>
        public override bool Update(bool full = false)
        {
            /// MUST make call to base here
            /// Note: Some of the other tracked type so not consider the base return value right away
            ///       This is because in the return statement below they instead make a call to NeedsUpdate()
            ///       which itself makes its own call to base
            ///       Refer to TrackedEncryptionData.Update() for example
            bool updated = base.Update(full);

            /// Should make sure you have physical before updating, be cause you obviously can't update based on 
            /// physical state if there is not physical state to update from
            /// Note: This should never happen unless something went wrong with H3MP
            if (physical == null)
            {
                return false;
            }

            /// Update data here

            return updated || oldData != data;
        }

        /// <summary>
        /// Checks if data has changed since latest update
        /// Used by H3MP to figure out which tracked object data it must send an update for to other clients
        /// </summary>
        /// <returns>True if data has changed since last update</returns>
        public override bool NeedsUpdate()
        {
            /// Base MUST be called here
            return base.NeedsUpdate() || oldData != data;
        }

        /// <summary>
        /// Checks whether the object corresponding to the given transform is under active control
        /// Note: This must only be implemented if the tracked object CAN be under control
        ///       Like an item that can be held or put into a quick belt slot for example
        ///       Refer to TrackedItemData.IsControlled() for an example
        /// Used by H3MP to manage control of tracked objects
        /// </summary>
        /// <param name="root">The transform of the object for which we want to check control</param>
        /// <returns>True if object corresponding to given transform is under active control</returns>
        public static bool IsControlled(Transform root)
        {
            return false;
        }

        /// <summary>
        /// Checks whether the tracked object is under active control
        /// Note: This must only be implemented if the tracked object CAN be under control
        ///       Like an item that can be held or put into a quick belt slot for example
        ///       Refer to TrackedItemData.IsControlled() for an example
        /// Used by H3MP to manage control of tracked objects
        /// </summary>
        /// <returns>True if tracked object is under active control</returns>
        public override bool IsControlled()
        {
            return false;
        }

        /// <summary>
        /// Will be called once a client receives the tracked ID for this tracked object
        /// Note: Implementing this is optional
        /// This is used by other tracked types to process events that must be sent to other clients that happened
        /// before we had the trackedID making it impossible to send
        /// So we go through the "unknown" lists only once we receive the trackedID
        /// Refer to other tracked types' implementation of this method for examples
        /// </summary>
        public override void OnTrackedIDReceived()
        {
            /// Base MUST be called here, before doing anything else
            base.OnTrackedIDReceived();
        }

        /// <summary>
        /// Will be called upon the object being tracked
        /// See TrackedItemData implementation for example. It uses this in case an item was grabbed by a Sosig before the item was even tracked to track
        /// the event and send it to others once it can
        /// </summary>
        public override void OnTracked()
        {
        }

        /// <summary>
        /// Called by H3MP when the controller of the tracked object changes
        /// This is to be used if you needto do something specific to the tracked object when gaining/losing control
        /// See other tracked types' implementation for examples
        /// Note: This will get called when instantiating a new TrackedObjectData with packet constructor if we are the init tracker
        ///       So when you receive an tracked object back from server after having tracked it, this may get called without
        ///       all data being initialized yet. See TrackedSosigData implementation, where we need to check if inventory exists
        /// Note: This will get called OnDestroy if we give control when giveControlOfDestroyed > 0. This means you cannot assume
        ///       you still have physical
        /// </summary>
        /// <param name="newController">The cliend ID of the new controller</param>
        public override void OnControlChanged(int newController)
        {
            /// Base MUST be called here
            base.OnControlChanged(newController);

            /// Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
            }
            else if (controller == GameManager.ID) // Lose control
            {
            }
        }

        /// <summary>
        /// Writes this tracked object's data to given packet
        /// </summary>
        /// <param name="packet">The packet to write data to</param>
        /// <param name="incrementOrder">Whether to increment the object's order. Used for updates.</param>
        /// <param name="full">Whether to write full data</param>
        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            /// Base MUST be called here BEFORE this type's data is written
            base.WriteToPacket(packet, incrementOrder, full);

            /// Refer to other tracked types' implementation for examples
        }

        /// <summary>
        /// Will be called upon the parent of the object changing
        /// Refer to TrackedItemData implementation for example
        /// Note: Implementing this is only necessary if this tracked type's parent is relevant
        /// </summary>
        public override void ParentChanged()
        {
        }

        /// <summary>
        /// Should be used to remove any local references to this object
        /// This will usually be called by H3MP upon the object's destruction or when we lose control of it for example
        /// See other tracked types' implementation of this for examples
        /// Note: Implementation is only necessary if you have something like "unknown" list to remove this object from
        /// </summary>
        public override void RemoveFromLocal()
        {
            /// Base MUST be called here
            base.RemoveFromLocal();
        }

        /// <summary>
        /// Will be called when the object get destroyed across the network
        /// When this is called it is because this object will cease to exist, this gives you the oppostunity to get rid of any remaining references to it
        /// Refer to TrackedObjectData.RemoveFromLists() for an example
        /// Note: Implementation is optional
        /// </summary>
        public override void RemoveFromLists()
        {
            /// Base MUST be called
            base.RemoveFromLists();
        }
    }
}
